using System.Text.Json;
using Buran.Player.Models;

namespace Buran.Player.Services;

public static class PlaylistStore {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = true
    };

    public static string FilePath {
        get {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Buran");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "playlists.json");
        }
    }

    public static PlaylistStoreData Load() {
        try {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<PlaylistStoreData>(File.ReadAllText(FilePath), JsonOptions)
                       ?? new PlaylistStoreData();
        }
        catch {
            // First run or a corrupted file.
        }

        return new PlaylistStoreData();
    }

    public static void Save(IEnumerable<Playlist> playlists, string? selectedId) {
        try {
            var data = new PlaylistStoreData {
                SelectedId = selectedId,
                Playlists  = playlists.Select(p => new PlaylistDto {
                    Id    = p.Id,
                    Name  = p.Name,
                    Paths = p.Paths.ToList()
                }).ToList()
            };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch {
            // Playlists are convenience, not a hard failure.
        }
    }

    public sealed class PlaylistStoreData {
        public string?            SelectedId { get; set; }
        public List<PlaylistDto>  Playlists  { get; set; } = [];
    }

    public sealed class PlaylistDto {
        public string       Id    { get; set; } = "";
        public string       Name  { get; set; } = "";
        public List<string> Paths { get; set; } = [];
    }
}
