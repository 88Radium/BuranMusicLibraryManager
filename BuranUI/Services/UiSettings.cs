using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BuranUI.Services;

public class UiSettings {
    public string?      LibraryRoot    { get; set; }
    public List<string> LibraryRoots   { get; set; } = [];
    public double  WindowOpacity  { get; set; } = 0.92;
    public string  Language       { get; set; } = "auto";
    public string  FontSize          { get; set; } = "medium";
    public bool    PlayerOnTop       { get; set; }
    public bool    IncludeSubfolders { get; set; }
    public string  Id3Columns        { get; set; } = "";

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

    public IReadOnlyList<string> ResolvedLibraryRoots() {
        var fromList = LibraryRoots
            .Where(p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (fromList.Count > 0)
            return fromList;
        if (!string.IsNullOrWhiteSpace(LibraryRoot) && Directory.Exists(LibraryRoot))
            return [Path.GetFullPath(LibraryRoot)];
        return [];
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
