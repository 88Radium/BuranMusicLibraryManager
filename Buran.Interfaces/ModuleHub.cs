namespace Buran.Interfaces;

/// <summary>
/// Filled after MEF composition so modules can find optional siblings
/// without taking a project reference on each other.
/// </summary>
public static class ModuleHub {
    public static IReadOnlyList<object> Modules { get; private set; } = [];

    public static void Set(IEnumerable<object> modules) =>
        Modules = modules.ToList();

    public static T? Find<T>() where T : class =>
        Modules.OfType<T>().FirstOrDefault();

    public static event EventHandler? ActivateId3Editor;

    public static void ShowId3Editor() =>
        ActivateId3Editor?.Invoke(null, EventArgs.Empty);

    public static event EventHandler<string>? OpenLibraryFolder;

    public static void ShowLibraryFolder(string folderPath) {
        if (string.IsNullOrWhiteSpace(folderPath))
            return;
        OpenLibraryFolder?.Invoke(null, folderPath);
    }

    public static Func<bool>? RestoreLibraryFolderHandler;

    public static bool TryRestoreLibraryFolder() =>
        RestoreLibraryFolderHandler?.Invoke() ?? false;
}
