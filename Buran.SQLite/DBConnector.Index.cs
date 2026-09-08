using System.Data;
using Microsoft.Data.Sqlite;

namespace Buran.SQLite;

public sealed record IndexedTrackHit(
    string Path,
    string FileName,
    string Title,
    string Artists,
    string Album,
    int? Year);

public partial class DBConnector {
    public static void EnsureTrackIndex() {
        Execute("""
            CREATE TABLE IF NOT EXISTS IndexedTracks (
                Path TEXT PRIMARY KEY,
                Folder TEXT,
                FileName TEXT,
                Title TEXT,
                Artists TEXT,
                Album TEXT,
                Year INTEGER,
                Genres TEXT,
                Moods TEXT,
                Duration INTEGER,
                Bitrate INTEGER,
                SampleRate REAL,
                BitDepth INTEGER,
                ModifiedUtc INTEGER
            )
            """);
        Execute("CREATE INDEX IF NOT EXISTS IX_IndexedTracks_Artists ON IndexedTracks(Artists)");
        Execute("CREATE INDEX IF NOT EXISTS IX_IndexedTracks_Genres ON IndexedTracks(Genres)");
        Execute("CREATE INDEX IF NOT EXISTS IX_IndexedTracks_Moods ON IndexedTracks(Moods)");
    }

    public static void UpsertIndexedTrack(
        string path,
        string title,
        string artists,
        string album,
        int? year,
        string genres,
        string moods,
        int duration,
        int bitrate,
        double sampleRate,
        int bitDepth) {
        EnsureTrackIndex();
        long modified = 0;
        try { modified = File.GetLastWriteTimeUtc(path).Ticks; } catch { /* ignore */ }

        Execute("""
            INSERT INTO IndexedTracks
                (Path, Folder, FileName, Title, Artists, Album, Year, Genres, Moods, Duration, Bitrate, SampleRate, BitDepth, ModifiedUtc)
            VALUES
                ($path, $folder, $file, $title, $artists, $album, $year, $genres, $moods, $dur, $br, $sr, $bd, $mod)
            ON CONFLICT(Path) DO UPDATE SET
                Folder=$folder, FileName=$file, Title=$title, Artists=$artists, Album=$album, Year=$year,
                Genres=$genres, Moods=$moods, Duration=$dur, Bitrate=$br, SampleRate=$sr, BitDepth=$bd, ModifiedUtc=$mod
            """,
            new SqliteParameter("$path", path),
            new SqliteParameter("$folder", Path.GetDirectoryName(path) ?? ""),
            new SqliteParameter("$file", Path.GetFileName(path)),
            new SqliteParameter("$title", title ?? ""),
            new SqliteParameter("$artists", WrapList(artists)),
            new SqliteParameter("$album", album ?? ""),
            new SqliteParameter("$year", (object?)year ?? DBNull.Value),
            new SqliteParameter("$genres", WrapList(genres)),
            new SqliteParameter("$moods", WrapList(moods)),
            new SqliteParameter("$dur", duration),
            new SqliteParameter("$br", bitrate),
            new SqliteParameter("$sr", sampleRate),
            new SqliteParameter("$bd", bitDepth),
            new SqliteParameter("$mod", modified));
    }

    public static IReadOnlyList<IndexedTrackHit> FindIndexedTracks(string kind, string name) {
        EnsureTrackIndex();
        var column = kind.ToLowerInvariant() switch {
            "artist" => "Artists",
            "genre"  => "Genres",
            "mood"   => "Moods",
            _        => "Artists"
        };
        var table = QueryTable($"""
            SELECT Path, FileName, Title, Artists, Album, Year
            FROM IndexedTracks
            WHERE ';' || {column} || ';' LIKE $like
            COLLATE NOCASE
            ORDER BY Artists, Title
            """,
            new SqliteParameter("$like", "%;" + name.Trim() + ";%"));

        var hits = new List<IndexedTrackHit>();
        foreach (DataRow row in table.Rows) {
            hits.Add(new(
                row["Path"]?.ToString() ?? "",
                row["FileName"]?.ToString() ?? "",
                row["Title"]?.ToString() ?? "",
                row["Artists"]?.ToString() ?? "",
                row["Album"]?.ToString() ?? "",
                row["Year"] is DBNull or null ? null : Convert.ToInt32(row["Year"])));
        }

        return hits;
    }

    private static string WrapList(string? raw) {
        var value = (raw ?? "").Trim().Trim(';');
        return value.Length == 0 ? "" : ";" + value + ";";
    }

    public static int IndexedTrackCount() {
        EnsureTrackIndex();
        var table = QueryTable("SELECT COUNT(*) AS C FROM IndexedTracks");
        return table.Rows.Count == 0 ? 0 : Convert.ToInt32(table.Rows[0]["C"]);
    }
}
