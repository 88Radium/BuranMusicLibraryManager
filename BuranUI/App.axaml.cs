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
using System.Reflection;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Buran.SQLite;
using Buran.Types;
using MsBox.Avalonia;

namespace BuranUI;

public class App : Application {
    #region MEF Extension Loading Mechanism

    public  string          ExtensionsDirectory { get; set; }
    private CompositionHost _container;


    [ImportMany] public IEnumerable<IExtension> collection { get; set; }


    public App() {

        char separatorChar = Path.DirectorySeparatorChar;
        
        ExtensionsDirectory = Assembly.GetExecutingAssembly().Location;
        ExtensionsDirectory = ExtensionsDirectory.Substring(0, ExtensionsDirectory.LastIndexOf(separatorChar));

        // if (LoadExtensions(ExtensionsDirectory)) return;
        //  BuranMessageBox.Show(
        //     "Es konnten keine Komponenten geladen werden. Bitte stellen Sie sicher, dass sich die Komponenten im Pfad " +
        //     ExtensionsDirectory.ToString() + "befinden.", "Achtung!").Wait();
        //
        // // Shutdown in Avalonia korrekt durchführen
        // if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
        //     desktop.Shutdown();
        // }
    }

    /// <summary>
    /// Lädt alle Extensions aus dem angegebenen Verzeichnis mit MEF 2
    /// </summary>
    /// <param name="pExtensionsDir">Pfad zum Extensions-Verzeichnis</param>
    /// <returns>True wenn erfolgreich, sonst False</returns>
    private bool LoadExtensions(string pExtensionsDir) {
        bool everythingsFine = true;

        try {
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
                        Console.WriteLine($"Fehler beim Laden von {dllPath}: {ex.Message}");
                        everythingsFine = false;
                    }
                }
            }
            else {
                Console.WriteLine($"Verzeichnis {pExtensionsDir} existiert nicht!");
                everythingsFine = false;
            }

            // 4. CompositionHost erstellen (ersetzt CompositionContainer)
            _container = configuration.CreateContainer();

            // 5. Abhängigkeiten in dieser Instanz erfüllen (ersetzt container.ComposeParts(this))
            _container.SatisfyImports(this);

            // 6. Geladene Extensions verarbeiten
            if (collection.Any()) {
                MainWindowViewModel.Tabs ??= new List<TabItem>();

                // Jede Extension als Tab hinzufügen
                foreach (IExtension e in collection) {
                    MainWindowViewModel.Tabs.Add(e.Tab);
                }
            }
            else {
                Console.WriteLine("Keine Extensions gefunden!");
                everythingsFine = false;
            }
        }
        catch (CompositionFailedException ex) // ersetzt CompositionException
        {
            Console.WriteLine($"Kompositionsfehler: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            everythingsFine = false;
        }
        catch (Exception ex) {
            Console.WriteLine($"Allgemeiner Fehler: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            everythingsFine = false;
        }

        return everythingsFine;
    }

    #endregion

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splash = new SplashWindow();
            splash.Show();

            Dispatcher.UIThread.Post(async () => {
                var shownAt = DateTime.UtcNow;
                try {
                    splash.SetStatus("Datenbank wird geprüft …");
                    await Task.Run(DBConnector.TestConnection);

                    splash.SetStatus("Erweiterungen werden geladen …");
                    if (!LoadExtensions(ExtensionsDirectory)) {
                        splash.Close();
                        await ShowMissingExtensionsAndShutdown();
                        return;
                    }

                    var remaining = TimeSpan.FromMilliseconds(900) - (DateTime.UtcNow - shownAt);
                    if (remaining > TimeSpan.Zero)
                        await Task.Delay(remaining);

                    splash.SetStatus("Anwendung wird gestartet …");
                    var main = new MainWindow {
                        DataContext = new MainWindowViewModel(),
                    };
                    desktop.MainWindow = main;
                    desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    main.Show();
                    splash.Close();
                }
                catch (Exception ex) {
                    Console.WriteLine($"Startfehler: {ex}");
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
            "Es konnten keine Komponenten geladen werden. Bitte stellen Sie sicher, dass sich die Komponenten im Pfad " +
            ExtensionsDirectory + " befinden.",
            "Achtung!");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
    
}