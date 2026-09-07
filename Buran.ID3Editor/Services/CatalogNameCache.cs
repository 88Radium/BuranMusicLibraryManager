using System;
using System.Collections.Generic;
using System.Linq;
using Buran.SQLite;

namespace Buran.ID3Editor.Services;

public static class CatalogNameCache {
    public static IReadOnlyList<string> LoadArtistSuggestionNames() {
        var names = new List<string>();
        foreach (var artist in DBConnector.LoadTableContent_ArtistNames())
            Add(names, artist.PreferredArtistName);
        foreach (var variant in DBConnector.LoadTableContent_AlternativeArtistNameVariants())
            Add(names, variant.AlternativeArtistName);
        return DistinctSorted(names);
    }

    public static IReadOnlyList<string> LoadGenreSuggestionNames() =>
        DistinctSorted(DBConnector.LoadTableContent_GenreNames().Select(g => g.GenreName));

    public static IReadOnlyList<string> LoadMoodSuggestionNames() =>
        DistinctSorted(DBConnector.LoadTableContent_MoodNames().Select(m => m.MoodName));

    private static void Add(List<string> names, string? value) {
        if (!string.IsNullOrWhiteSpace(value))
            names.Add(value.Trim());
    }

    private static IReadOnlyList<string> DistinctSorted(IEnumerable<string?> values) =>
        values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
}
