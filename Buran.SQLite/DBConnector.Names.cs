using System.Collections.ObjectModel;
using System.Data;
using Buran.Types;
using Microsoft.Data.Sqlite;

namespace Buran.SQLite;

public partial class DBConnector {
    public static CatalogNameResolution ResolveArtistName(string? raw) {
        var name = (raw ?? "").Trim();
        if (name.Length == 0)
            return new("", ArtistNameStatus.IsNonExistent);

        var preferred = LoadTableContent_ArtistNames()
            .FirstOrDefault(a => a.PreferredArtistName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (preferred is not null)
            return new(preferred.PreferredArtistName, ArtistNameStatus.AlreadyExisting);

        var alternative = LoadTableContent_AlternativeArtistNameVariants()
            .FirstOrDefault(a => a.AlternativeArtistName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (alternative is not null) {
            var owner = LoadTableContent_ArtistNames().FirstOrDefault(a => a.ID == alternative.RefersToArtistName);
            if (owner is not null)
                return new(owner.PreferredArtistName, ArtistNameStatus.IsAlternativeName);
        }

        return new(name, ArtistNameStatus.IsNonExistent);
    }

    public static CatalogNameResolution ResolveGenreName(string? raw) {
        var name = (raw ?? "").Trim();
        if (name.Length == 0)
            return new("", ArtistNameStatus.IsNonExistent);

        var preferred = LoadTableContent_GenreNames()
            .FirstOrDefault(g => g.GenreName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (preferred is not null)
            return new(preferred.GenreName, ArtistNameStatus.AlreadyExisting);

        var alternative = LoadTableContent_AlternativeGenreNameVariants()
            .FirstOrDefault(g => g.GenreNameVariant.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (alternative is not null) {
            var owner = LoadTableContent_GenreNames().FirstOrDefault(g => g.ID == alternative.RefersToGenreName);
            if (owner is not null)
                return new(owner.GenreName, ArtistNameStatus.IsAlternativeName);
        }

        return new(name, ArtistNameStatus.IsNonExistent);
    }

    public static CatalogNameResolution ResolveMoodName(string? raw) {
        var name = (raw ?? "").Trim();
        if (name.Length == 0)
            return new("", ArtistNameStatus.IsNonExistent);

        var preferred = LoadTableContent_MoodNames()
            .FirstOrDefault(m => m.MoodName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (preferred is not null)
            return new(preferred.MoodName, ArtistNameStatus.AlreadyExisting);

        var alternative = LoadTableContent_AlternativeMoodNameVariants()
            .FirstOrDefault(m => m.MoodNameVariant.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (alternative is not null) {
            var owner = LoadTableContent_MoodNames().FirstOrDefault(m => m.ID == alternative.RefersToMoodName);
            if (owner is not null)
                return new(owner.MoodName, ArtistNameStatus.IsAlternativeName);
        }

        return new(name, ArtistNameStatus.IsNonExistent);
    }

    public static ObservableCollection<DatabaseTable_AlternativeGenreNameVariants> LoadTableContent_AlternativeGenreNameVariants() {
        var result = new ObservableCollection<DatabaseTable_AlternativeGenreNameVariants>();
        foreach (DataRow row in QueryTable(
                     "SELECT * FROM AlternativeGenreNameVariants ORDER BY GenreNameVariant COLLATE NOCASE").Rows)
            result.Add(new DatabaseTable_AlternativeGenreNameVariants(row));
        return result;
    }

    public static void InsertAlternativeGenreName(string name, int genreId) {
        DeleteBlockedCatalogValueCore(DatabaseTable_BlockedCatalogValues.KindGenre, name);
        Execute(
            "INSERT INTO AlternativeGenreNameVariants (GenreNameVariant, IsMissSpelled, RefersToGenreName) VALUES ($n, 0, $g)",
            new SqliteParameter("$n", name),
            new SqliteParameter("$g", genreId));
        NotifyCatalogChanged();
    }

    public static void UpdateAlternativeGenreName(DatabaseTable_AlternativeGenreNameVariants variant) {
        Execute(
            "UPDATE AlternativeGenreNameVariants SET GenreNameVariant = $n, RefersToGenreName = $g WHERE ID = $id",
            new SqliteParameter("$n", variant.GenreNameVariant ?? ""),
            new SqliteParameter("$g", variant.RefersToGenreName),
            new SqliteParameter("$id", variant.Id));
    }

    public static void DeleteAlternativeGenreName(int id) {
        Execute("DELETE FROM AlternativeGenreNameVariants WHERE ID = $id", new SqliteParameter("$id", id));
        NotifyCatalogChanged();
    }

    public static ObservableCollection<DatabaseTable_AlternativeMoodNameVariants> LoadTableContent_AlternativeMoodNameVariants() {
        var result = new ObservableCollection<DatabaseTable_AlternativeMoodNameVariants>();
        foreach (DataRow row in QueryTable(
                     "SELECT * FROM AlternativeMoodNameVariants ORDER BY MoodNameVariant COLLATE NOCASE").Rows)
            result.Add(new DatabaseTable_AlternativeMoodNameVariants(row));
        return result;
    }

    public static void InsertAlternativeMoodName(string name, int moodId) {
        DeleteBlockedCatalogValueCore(DatabaseTable_BlockedCatalogValues.KindMood, name);
        Execute(
            "INSERT INTO AlternativeMoodNameVariants (MoodNameVariant, IsMissSpelled, RefersToMoodName) VALUES ($n, 0, $m)",
            new SqliteParameter("$n", name),
            new SqliteParameter("$m", moodId));
        NotifyCatalogChanged();
    }

    public static void UpdateAlternativeMoodName(DatabaseTable_AlternativeMoodNameVariants variant) {
        Execute(
            "UPDATE AlternativeMoodNameVariants SET MoodNameVariant = $n, RefersToMoodName = $m WHERE ID = $id",
            new SqliteParameter("$n", variant.MoodNameVariant ?? ""),
            new SqliteParameter("$m", variant.RefersToMoodName),
            new SqliteParameter("$id", variant.Id));
    }

    public static void DeleteAlternativeMoodName(int id) {
        Execute("DELETE FROM AlternativeMoodNameVariants WHERE ID = $id", new SqliteParameter("$id", id));
        NotifyCatalogChanged();
    }
}
