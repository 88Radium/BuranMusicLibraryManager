using System.Collections.ObjectModel;
using System.Data;
using Buran.Types;
using Microsoft.Data.Sqlite;

namespace Buran.SQLite;

public partial class DBConnector {
    /// <summary>
    /// Raised after catalog entities (artists, alternatives, genres, moods) are inserted or deleted.
    /// Name edits and assignment toggles do not raise this.
    /// </summary>
    public static event EventHandler? CatalogChanged;

    public static void NotifyCatalogChanged() {
        CatalogChanged?.Invoke(null, EventArgs.Empty);
    }

    private static SqliteConnection OpenCatalogConnection() {
        var connection = new SqliteConnection("Data Source=CerberusMusicManager.db");
        connection.Open();
        return connection;
    }

    private static void Execute(string sql, params SqliteParameter[] parameters) {
        using var connection = OpenCatalogConnection();
        using var cmd        = new SqliteCommand(sql, connection);
        if (parameters.Length > 0)
            cmd.Parameters.AddRange(parameters);
        cmd.ExecuteNonQuery();
    }

    private static DataTable QueryTable(string sql, params SqliteParameter[] parameters) {
        using var connection = OpenCatalogConnection();
        using var cmd        = new SqliteCommand(sql, connection);
        if (parameters.Length > 0)
            cmd.Parameters.AddRange(parameters);
        using var reader = cmd.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    public static void UpdateArtistName(DatabaseTable_ArtistNames artist) {
        Execute(
            "UPDATE ArtistNames SET PreferredArtistName = $n, RealName = $r WHERE ID = $id",
            new SqliteParameter("$n", artist.PreferredArtistName ?? ""),
            new SqliteParameter("$r", artist.RealName ?? ""),
            new SqliteParameter("$id", artist.ID));
    }

    public static void DeleteArtist(int artistId) {
        Execute("DELETE FROM AlternativeArtistNameVariants WHERE RefersToArtistName = $id", new SqliteParameter("$id", artistId));
        Execute("DELETE FROM ArtistGenres WHERE ArtistId = $id", new SqliteParameter("$id", artistId));
        Execute("DELETE FROM ArtistMoods WHERE ArtistId = $id", new SqliteParameter("$id", artistId));
        Execute("DELETE FROM ArtistMemberships WHERE GroupArtistId = $id OR MemberArtistId = $id", new SqliteParameter("$id", artistId));
        Execute("DELETE FROM ArtistNames WHERE ID = $id", new SqliteParameter("$id", artistId));
        NotifyCatalogChanged();
    }

    public static void InsertAlternativeArtistName(string name, int artistId) {
        Execute(
            "INSERT INTO AlternativeArtistNameVariants (AlternativeArtistName, RefersToArtistName, IsMissSpelled) VALUES ($n, $a, 0)",
            new SqliteParameter("$n", name),
            new SqliteParameter("$a", artistId));
        NotifyCatalogChanged();
    }

    public static void UpdateAlternativeArtistName(DatabaseTable_AlternativeArtistNameVariants variant) {
        Execute(
            "UPDATE AlternativeArtistNameVariants SET AlternativeArtistName = $n, RefersToArtistName = $a WHERE ID = $id",
            new SqliteParameter("$n", variant.AlternativeArtistName ?? ""),
            new SqliteParameter("$a", variant.RefersToArtistName),
            new SqliteParameter("$id", variant.ID));
    }

    public static void DeleteAlternativeArtistName(int id) {
        Execute("DELETE FROM AlternativeArtistNameVariants WHERE ID = $id", new SqliteParameter("$id", id));
        NotifyCatalogChanged();
    }

    public static ObservableCollection<DatabaseTable_GenreNames> LoadTableContent_GenreNames() {
        var result = new ObservableCollection<DatabaseTable_GenreNames>();
        foreach (DataRow row in QueryTable("SELECT * FROM GenreNames ORDER BY GenreName COLLATE NOCASE").Rows)
            result.Add(new DatabaseTable_GenreNames(row));
        return result;
    }

    public static void InsertGenreName(string name) {
        Execute("INSERT INTO GenreNames (GenreName) VALUES ($n)", new SqliteParameter("$n", name));
        NotifyCatalogChanged();
    }

    public static void UpdateGenreName(DatabaseTable_GenreNames genre) {
        Execute(
            "UPDATE GenreNames SET GenreName = $n WHERE ID = $id",
            new SqliteParameter("$n", genre.GenreName ?? ""),
            new SqliteParameter("$id", genre.ID));
    }

    public static void DeleteGenre(int genreId) {
        Execute("DELETE FROM ArtistGenres WHERE GenreId = $id", new SqliteParameter("$id", genreId));
        Execute("DELETE FROM AlternativeGenreNameVariants WHERE RefersToGenreName = $id", new SqliteParameter("$id", genreId));
        Execute("DELETE FROM GenreNames WHERE ID = $id", new SqliteParameter("$id", genreId));
        NotifyCatalogChanged();
    }

    public static ObservableCollection<DatabaseTable_MoodNames> LoadTableContent_MoodNames() {
        var result = new ObservableCollection<DatabaseTable_MoodNames>();
        foreach (DataRow row in QueryTable("SELECT * FROM MoodNames ORDER BY MoodName COLLATE NOCASE").Rows)
            result.Add(new DatabaseTable_MoodNames(row));
        return result;
    }

    public static void InsertMoodName(string name) {
        Execute("INSERT INTO MoodNames (MoodName) VALUES ($n)", new SqliteParameter("$n", name));
        NotifyCatalogChanged();
    }

    public static void UpdateMoodName(DatabaseTable_MoodNames mood) {
        Execute(
            "UPDATE MoodNames SET MoodName = $n WHERE ID = $id",
            new SqliteParameter("$n", mood.MoodName ?? ""),
            new SqliteParameter("$id", mood.ID));
    }

    public static void DeleteMood(int moodId) {
        Execute("DELETE FROM ArtistMoods WHERE MoodId = $id", new SqliteParameter("$id", moodId));
        Execute("DELETE FROM AlternativeMoodNameVariants WHERE RefersToMoodName = $id", new SqliteParameter("$id", moodId));
        Execute("DELETE FROM MoodNames WHERE ID = $id", new SqliteParameter("$id", moodId));
        NotifyCatalogChanged();
    }

    public static HashSet<int> LoadGenreIdsForArtist(int artistId) {
        var ids = new HashSet<int>();
        foreach (DataRow row in QueryTable("SELECT GenreId FROM ArtistGenres WHERE ArtistId = $a", new SqliteParameter("$a", artistId)).Rows)
            ids.Add(Convert.ToInt32(row["GenreId"]));
        return ids;
    }

    public static HashSet<int> LoadMoodIdsForArtist(int artistId) {
        var ids = new HashSet<int>();
        foreach (DataRow row in QueryTable("SELECT MoodId FROM ArtistMoods WHERE ArtistId = $a", new SqliteParameter("$a", artistId)).Rows)
            ids.Add(Convert.ToInt32(row["MoodId"]));
        return ids;
    }

    public static HashSet<int> LoadArtistIdsForGenre(int genreId) {
        var ids = new HashSet<int>();
        foreach (DataRow row in QueryTable("SELECT ArtistId FROM ArtistGenres WHERE GenreId = $g", new SqliteParameter("$g", genreId)).Rows)
            ids.Add(Convert.ToInt32(row["ArtistId"]));
        return ids;
    }

    public static HashSet<int> LoadArtistIdsForMood(int moodId) {
        var ids = new HashSet<int>();
        foreach (DataRow row in QueryTable("SELECT ArtistId FROM ArtistMoods WHERE MoodId = $m", new SqliteParameter("$m", moodId)).Rows)
            ids.Add(Convert.ToInt32(row["ArtistId"]));
        return ids;
    }

    public static void SetArtistGenre(int artistId, int genreId, bool assigned) {
        if (assigned)
            Execute("INSERT OR IGNORE INTO ArtistGenres (ArtistId, GenreId) VALUES ($a, $g)",
                new SqliteParameter("$a", artistId), new SqliteParameter("$g", genreId));
        else
            Execute("DELETE FROM ArtistGenres WHERE ArtistId = $a AND GenreId = $g",
                new SqliteParameter("$a", artistId), new SqliteParameter("$g", genreId));
    }

    public static void SetArtistMood(int artistId, int moodId, bool assigned) {
        if (assigned)
            Execute("INSERT OR IGNORE INTO ArtistMoods (ArtistId, MoodId) VALUES ($a, $m)",
                new SqliteParameter("$a", artistId), new SqliteParameter("$m", moodId));
        else
            Execute("DELETE FROM ArtistMoods WHERE ArtistId = $a AND MoodId = $m",
                new SqliteParameter("$a", artistId), new SqliteParameter("$m", moodId));
    }

    public static HashSet<int> LoadMemberIdsForGroup(int groupArtistId) {
        var ids = new HashSet<int>();
        foreach (DataRow row in QueryTable(
                     "SELECT MemberArtistId FROM ArtistMemberships WHERE GroupArtistId = $g",
                     new SqliteParameter("$g", groupArtistId)).Rows)
            ids.Add(Convert.ToInt32(row["MemberArtistId"]));
        return ids;
    }

    public static HashSet<int> LoadGroupIdsForMember(int memberArtistId) {
        var ids = new HashSet<int>();
        foreach (DataRow row in QueryTable(
                     "SELECT GroupArtistId FROM ArtistMemberships WHERE MemberArtistId = $m",
                     new SqliteParameter("$m", memberArtistId)).Rows)
            ids.Add(Convert.ToInt32(row["GroupArtistId"]));
        return ids;
    }

    public static void SetArtistMembership(int groupArtistId, int memberArtistId, bool assigned) {
        if (groupArtistId == memberArtistId)
            return;

        if (assigned)
            Execute(
                "INSERT OR IGNORE INTO ArtistMemberships (GroupArtistId, MemberArtistId) VALUES ($g, $m)",
                new SqliteParameter("$g", groupArtistId),
                new SqliteParameter("$m", memberArtistId));
        else
            Execute(
                "DELETE FROM ArtistMemberships WHERE GroupArtistId = $g AND MemberArtistId = $m",
                new SqliteParameter("$g", groupArtistId),
                new SqliteParameter("$m", memberArtistId));
    }

    public static int InsertArtistName(string preferredName, string realName) {
        using var connection = OpenCatalogConnection();
        using var cmd = new SqliteCommand(
            "INSERT INTO ArtistNames (PreferredArtistName, RealName) VALUES ($n, $r); SELECT last_insert_rowid();",
            connection);
        cmd.Parameters.Add(new SqliteParameter("$n", preferredName ?? ""));
        cmd.Parameters.Add(new SqliteParameter("$r", realName ?? ""));
        var id = Convert.ToInt32(cmd.ExecuteScalar());
        NotifyCatalogChanged();
        return id;
    }

    public static ObservableCollection<DatabaseTable_FeatureKeywords> LoadTableContent_FeatureKeywords() {
        var result = new ObservableCollection<DatabaseTable_FeatureKeywords>();
        foreach (DataRow row in QueryTable("SELECT * FROM FeatureKeywords ORDER BY Type, Keyword COLLATE NOCASE").Rows)
            result.Add(new DatabaseTable_FeatureKeywords(row));
        return result;
    }

    public static void ReloadCollaborationMarkers() {
        CollaborationMarkers.Apply(
            LoadTableContent_FeatureKeywords().Select(k => (k.Keyword, k.Type)));
    }

    public static void InsertFeatureKeyword(string keyword, string type, int weight = 5) {
        Execute(
            "INSERT INTO FeatureKeywords (Keyword, Type, Weight) VALUES ($k, $t, $w)",
            new SqliteParameter("$k", keyword),
            new SqliteParameter("$t", type),
            new SqliteParameter("$w", weight));
        ReloadCollaborationMarkers();
        NotifyCatalogChanged();
    }

    public static void UpdateFeatureKeyword(DatabaseTable_FeatureKeywords keyword) {
        Execute(
            "UPDATE FeatureKeywords SET Keyword = $k, Type = $t, Weight = $w WHERE Id = $id",
            new SqliteParameter("$k", keyword.Keyword ?? ""),
            new SqliteParameter("$t", keyword.Type ?? CollaborationMarkers.TypeCollaboration),
            new SqliteParameter("$w", keyword.Weight),
            new SqliteParameter("$id", keyword.Id));
        ReloadCollaborationMarkers();
    }

    public static void DeleteFeatureKeyword(int id) {
        Execute("DELETE FROM FeatureKeywords WHERE Id = $id", new SqliteParameter("$id", id));
        ReloadCollaborationMarkers();
        NotifyCatalogChanged();
    }
}
