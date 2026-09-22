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

/// <summary>
/// A path row dropped because another indexed path differs only by case and that file is the one on disk.
/// </summary>
public sealed record IndexedTrackCaseGhost(string RemovedFileName, string SurvivingFileName);

public sealed record IndexedTrackLookup(
    IReadOnlyList<IndexedTrackHit> Hits,
    IReadOnlyList<IndexedTrackCaseGhost> RemovedGhosts);

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
        // A case-only rename is a different key. Drop the previous spelling when that file is gone.
        DropMissingCaseVariants(path);
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

    public static void RemoveIndexedTrack(string path) {
        if (string.IsNullOrWhiteSpace(path))
            return;
        EnsureTrackIndex();
        Execute("DELETE FROM IndexedTracks WHERE Path = $path", new SqliteParameter("$path", path));
    }

    /// <summary>
    /// Removes indexed paths that collide with another row only by case and whose file is no longer on disk.
    /// </summary>
    public static IReadOnlyList<IndexedTrackCaseGhost> PurgeMissingIndexedTrackCaseGhosts() {
        EnsureTrackIndex();
        return DeleteCaseGhosts(QueryPathColumn("SELECT Path FROM IndexedTracks"));
    }

    public static IndexedTrackLookup FindIndexedTracks(string kind, string name) {
        EnsureTrackIndex();
        var column = kind.ToLowerInvariant() switch {
            "artist" => "Artists",
            "genre"  => "Genres",
            "mood"   => "Moods",
            _        => "Artists"
        };

        try {
            return new IndexedTrackLookup(ReadHits(LoadIndexedTracks(column, name, caseSensitive: false)), []);
        }
        catch (ConstraintException) {
            // DataTable compares the Path key without regard to case. Two spellings of one file trip it.
            var ghosts = DeleteCaseGhosts(QueryPathColumn(PathFilterSql(column), LikeParameter(name)));
            if (ghosts.Count == 0)
                return new IndexedTrackLookup(ReadHits(LoadIndexedTracks(column, name, caseSensitive: true)), []);

            try {
                return new IndexedTrackLookup(ReadHits(LoadIndexedTracks(column, name, caseSensitive: false)), ghosts);
            }
            catch (ConstraintException) {
                return new IndexedTrackLookup(ReadHits(LoadIndexedTracks(column, name, caseSensitive: true)), ghosts);
            }
        }
    }

    private static void DropMissingCaseVariants(string path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var others = QueryPathColumn("""
            SELECT Path FROM IndexedTracks
            WHERE Path = $path COLLATE NOCASE AND Path <> $path
            """,
            new SqliteParameter("$path", path));

        foreach (var other in others) {
            if (File.Exists(other))
                continue;
            Execute("DELETE FROM IndexedTracks WHERE Path = $path", new SqliteParameter("$path", other));
        }
    }

    private static List<IndexedTrackCaseGhost> DeleteCaseGhosts(IReadOnlyList<string> paths) {
        var planned = new List<(string Path, IndexedTrackCaseGhost Ghost)>();
        foreach (var group in paths.Where(p => p.Length > 0)
                     .GroupBy(p => p, StringComparer.CurrentCultureIgnoreCase)) {
            var distinct = group.Distinct(StringComparer.Ordinal).ToList();
            if (distinct.Count < 2)
                continue;

            var living = distinct.Where(File.Exists).ToList();
            var dead   = distinct.Where(p => !File.Exists(p)).ToList();
            if (dead.Count == 0)
                continue;

            var survivor = living.Count > 0 ? Path.GetFileName(living[0]) : "";
            foreach (var stale in dead)
                planned.Add((stale, new IndexedTrackCaseGhost(Path.GetFileName(stale), survivor)));
        }

        foreach (var (path, _) in planned)
            Execute("DELETE FROM IndexedTracks WHERE Path = $path", new SqliteParameter("$path", path));

        return planned
            .Select(item => item.Ghost)
            .Where(ghost => ghost.SurvivingFileName.Length > 0)
            .ToList();
    }

    private static DataTable LoadIndexedTracks(string column, string name, bool caseSensitive) {
        lock (DbSync) {
            using var connection = OpenCatalogConnection();
            using var cmd        = new SqliteCommand(HitFilterSql(column), connection);
            cmd.Parameters.Add(LikeParameter(name));
            using var reader = cmd.ExecuteReader();
            // CaseSensitive matches SQLite's binary path key. The default folds case and rejects a renamed file.
            var table = new DataTable { CaseSensitive = caseSensitive };
            table.Load(reader);
            return table;
        }
    }

    private static List<string> QueryPathColumn(string sql, params SqliteParameter[] parameters) {
        lock (DbSync) {
            using var connection = OpenCatalogConnection();
            using var cmd        = new SqliteCommand(sql, connection);
            if (parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);
            using var reader = cmd.ExecuteReader();
            var paths = new List<string>();
            while (reader.Read()) {
                if (!reader.IsDBNull(0))
                    paths.Add(reader.GetString(0));
            }

            return paths;
        }
    }

    private static List<IndexedTrackHit> ReadHits(DataTable table) {
        var hits = new List<IndexedTrackHit>(table.Rows.Count);
        foreach (DataRow row in table.Rows) {
            hits.Add(new IndexedTrackHit(
                row["Path"]?.ToString() ?? "",
                row["FileName"]?.ToString() ?? "",
                row["Title"]?.ToString() ?? "",
                row["Artists"]?.ToString() ?? "",
                row["Album"]?.ToString() ?? "",
                row["Year"] is DBNull or null ? null : Convert.ToInt32(row["Year"])));
        }

        return hits;
    }

    private static string HitFilterSql(string column) => $"""
        SELECT Path, FileName, Title, Artists, Album, Year
        FROM IndexedTracks
        WHERE ';' || {column} || ';' LIKE $like
        COLLATE NOCASE
        ORDER BY Artists, Title
        """;

    private static string PathFilterSql(string column) => $"""
        SELECT Path
        FROM IndexedTracks
        WHERE ';' || {column} || ';' LIKE $like
        COLLATE NOCASE
        """;

    private static SqliteParameter LikeParameter(string name) =>
        new("$like", "%;" + name.Trim() + ";%");

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
