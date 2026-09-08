using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Buran.Interfaces;
using BuranUI.ViewModels;
using BuranUI.Views;
using System.Composition;
using System.Composition.Hosting;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Buran.Localization;
using Buran.SQLite;
using Buran.Types;
using BuranUI.Services;

namespace BuranUI;

public class App : Application {
    #region MEF Extension Loading Mechanism

    public  string           ExtensionsDirectory { get; set; } = "";
    private CompositionHost? _container;

    [ImportMany] public IEnumerable<IExtension> collection { get; set; } = [];


    public App() {

        char separatorChar = Path.DirectorySeparatorChar;
        
        ExtensionsDirectory = Assembly.GetExecutingAssembly().Location;
        ExtensionsDirectory = ExtensionsDirectory.Substring(0, ExtensionsDirectory.LastIndexOf(separatorChar));
        L.Initialize(UiSettings.Load().Language);
    }

    /// <summary>
    /// Loads all extensions from the given directory with MEF 2.
    /// </summary>
    /// <param name="pExtensionsDir">Path to the extensions directory</param>
    /// <returns>True when successful, otherwise false</returns>
    private bool LoadExtensions(string pExtensionsDir) {
        bool everythingsFine = true;

        try {
            EnablePluginDependencyResolution(pExtensionsDir);

            // 1. ContainerConfiguration erstellen (ersetzt AggregateCatalog)
            var configuration = new ContainerConfiguration();

            // 2. Aktuelle Assembly hinzufügen (ersetzt AssemblyCatalog)
            configuration.WithAssembly(Assembly.GetExecutingAssembly());

            // 3. Alle DLLs aus dem Extensions-Verzeichnis laden (ersetzt DirectoryCatalog)
            if (Directory.Exists(pExtensionsDir)) {
                foreach (var dllPath in Directory.GetFiles(pExtensionsDir + "/", "Buran.*.dll")) {
                    try {
                        // Assembly laden
                        var assembly = Assembly.LoadFrom(dllPath);

                        // Assembly zur Konfiguration hinzufügen
                        configuration.WithAssembly(assembly);
                    }
                    catch (Exception ex) {
                        Debug.WriteLine($"Failed to load {dllPath}: {ex.Message}");
                        everythingsFine = false;
                    }
                }
            }
            else {
                Debug.WriteLine($"Directory {pExtensionsDir} does not exist.");
                everythingsFine = false;
            }

            // 4. CompositionHost erstellen (ersetzt CompositionContainer)
            _container = configuration.CreateContainer();

            // 5. Abhängigkeiten in dieser Instanz erfüllen (ersetzt container.ComposeParts(this))
            _container.SatisfyImports(this);

            // 6. Geladene Extensions verarbeiten
            if (collection.Any()) {
                var loaded = collection.ToList();
                MainWindowViewModel.LoadedExtensions = loaded;
                ModuleHub.Set(loaded);
                MainWindowViewModel.Tabs ??= new List<TabItem>();

                var dock = ModuleHub.Find<IPlayerDock>();
                if (dock is not null)
                    MainWindowViewModel.PlayerDock = dock.TakeDock();

                foreach (IExtension e in collection) {
                    if (e is IPlayerDock && MainWindowViewModel.PlayerDock is not null)
                        continue;
                    MainWindowViewModel.Tabs.Add(e.Tab);
                }
            }
            else {
                Debug.WriteLine("No extensions found.");
                everythingsFine = false;
            }
        }
        catch (CompositionFailedException ex) // ersetzt CompositionException
        {
            Debug.WriteLine($"Composition error: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
            everythingsFine = false;
        }
        catch (Exception ex) {
            Debug.WriteLine($"Unexpected error: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
            everythingsFine = false;
        }

        return everythingsFine;
    }

    /// <summary>
    /// Plugin assemblies are loaded with LoadFrom. Their NuGet dependencies sit next to
    /// BuranUI.dll but are missing from the host .deps.json, so the default probe skips them.
    /// </summary>
    private static void EnablePluginDependencyResolution(string extensionsDir) {
        AssemblyLoadContext.Default.Resolving += (_, assemblyName) => {
            if (string.IsNullOrEmpty(assemblyName.Name))
                return null;

            var path = Path.Combine(extensionsDir, assemblyName.Name + ".dll");
            return File.Exists(path)
                ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path))
                : null;
        };
    }

    #endregion

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
        UiFontScale.Apply(UiSettings.Load().FontSize);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splash = new SplashWindow();
            splash.Show();

            Dispatcher.UIThread.Post(async () => {
                var shownAt = DateTime.UtcNow;
                try {
                    splash.SetStatus(L.Get("Splash.CheckingDatabase"));
                    await Task.Run(DBConnector.TestConnection);

                    splash.SetStatus(L.Get("Splash.LoadingExtensions"));
                    if (!LoadExtensions(ExtensionsDirectory)) {
                        splash.Close();
                        await ShowMissingExtensionsAndShutdown();
                        return;
                    }

                    var remaining = TimeSpan.FromMilliseconds(900) - (DateTime.UtcNow - shownAt);
                    if (remaining > TimeSpan.Zero)
                        await Task.Delay(remaining);

                    splash.SetStatus(L.Get("Splash.Starting"));
                    var main = new MainWindow {
                        DataContext = new MainWindowViewModel(),
                    };
                    desktop.MainWindow = main;
                    desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    main.Show();
                    splash.Close();
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Startup error: {ex}");
                    splash.Close();
                    desktop.Shutdown();
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private async Task ShowMissingExtensionsAndShutdown()
    {
        await BuranMessageBox.Show(
            L.Format("App.NoExtensions", ExtensionsDirectory),
            L.Get("Common.Warning"));

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
    
}