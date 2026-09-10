namespace Buran.SQLite;

public partial class DBConnector {
    public static string DatabaseFile { get; } = InitDatabaseFile();

    public static string ConnectionString => $"Data Source={DatabaseFile}";

    private static string InitDatabaseFile() {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Buran");
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, "CerberusMusicManager.db");
        if (File.Exists(dest))
            return dest;

        foreach (var src in new[] {
                     Path.Combine(Directory.GetCurrentDirectory(), "CerberusMusicManager.db"),
                     Path.Combine(AppContext.BaseDirectory, "CerberusMusicManager.db")
                 }) {
            if (!File.Exists(src))
                continue;
            if (string.Equals(Path.GetFullPath(src), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
                continue;
            File.Copy(src, dest);
            break;
        }

        return dest;
    }
}
