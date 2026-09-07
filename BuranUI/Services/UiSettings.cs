using System;
using System.IO;
using System.Text.Json;

namespace BuranUI.Services;

public class UiSettings {
    public string? LibraryRoot    { get; set; }
    public double  WindowOpacity  { get; set; } = 0.92;
    public string  Language       { get; set; } = "auto";
    public string  FontSize       { get; set; } = "medium";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string FilePath {
        get {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Buran");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "ui-settings.json");
        }
    }

    public static UiSettings Load() {
        try {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<UiSettings>(File.ReadAllText(FilePath)) ?? new UiSettings();
        }
        catch {
            // First run or a corrupted settings file.
        }

        return new UiSettings();
    }

    public void Save() {
        try {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch {
            // Settings are convenience, not a hard failure.
        }
    }
}
