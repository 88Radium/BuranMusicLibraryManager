using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Buran.ID3Editor.Models;
using Buran.Localization;
using Buran.SQLite;
using Buran.Types;

namespace Buran.ID3Editor.Services;

public static class CatalogSuggestionCollector {
    public static IReadOnlyList<CatalogSuggestionItem> Collect(IEnumerable<Mp3FileObject> files) {
        var fileList = files.ToList();
        if (fileList.Count == 0)
            return [];

        var artistSources = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var genreSources  = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var moodSources   = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        var parser   = new FileNameParser();
        var patterns = DBConnector.LoadFileNamePatterns();

        foreach (var file in fileList) {
            if (file.Id3ArtistCollection is { } artists)
                foreach (var artist in artists)
                    Add(artistSources, artist, L.Get("Catalog.SourceId3"));

            if (file.Id3GenreCollection is { } genres)
                foreach (var genre in genres)
                    Add(genreSources, genre, L.Get("Catalog.SourceId3"));

            if (file.Id3MoodCollection is { } moods)
                foreach (var mood in moods)
                    Add(moodSources, mood, L.Get("Catalog.SourceId3"));

            var parsed = parser.Parse(file.FileName, patterns);
            if (parsed.Artists is not { Count: > 0 })
                continue;

            foreach (var artist in parsed.Artists)
                Add(artistSources, artist, L.Get("Catalog.SourceFileName"));
        }

        var knownArtists = LoadKnownArtistNames();
        var knownGenres  = LoadKnownLabels(
            DBConnector.LoadTableContent_GenreNames().Select(g => g.GenreName));
        var knownMoods = LoadKnownLabels(
            DBConnector.LoadTableContent_MoodNames().Select(m => m.MoodName));

        var existingArtists = DBConnector.LoadTableContent_ArtistNames()
            .OrderBy(a => a.PreferredArtistName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var result = new List<CatalogSuggestionItem>();
        result.AddRange(ToItems(CatalogSuggestionKind.Artist, artistSources, knownArtists, existingArtists));
        result.AddRange(ToItems(CatalogSuggestionKind.Genre, genreSources, knownGenres, existingArtists));
        result.AddRange(ToItems(CatalogSuggestionKind.Mood, moodSources, knownMoods, existingArtists));
        return result;
    }

    private static IEnumerable<CatalogSuggestionItem> ToItems(
        CatalogSuggestionKind kind,
        Dictionary<string, HashSet<string>> sources,
        HashSet<string> known,
        IReadOnlyList<DatabaseTable_ArtistNames> existingArtists) {

        foreach (var (value, labels) in sources.OrderBy(p => p.Key, StringComparer.CurrentCultureIgnoreCase)) {
            if (known.Contains(value))
                continue;

            yield return new CatalogSuggestionItem(kind, value, string.Join(" + ", labels.OrderBy(s => s))) {
                ExistingArtists = existingArtists
            };
        }
    }

    private static void Add(Dictionary<string, HashSet<string>> bag, string? raw, string source) {
        var value = Normalize(raw);
        if (value is null)
            return;

        if (!bag.TryGetValue(value, out var labels)) {
            labels     = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bag[value] = labels;
        }

        labels.Add(source);
    }

    private static string? Normalize(string? raw) {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var value = Regex.Replace(raw.Trim(), @"\s+", " ");
        return value.Length == 0 ? null : value;
    }

    private static HashSet<string> LoadKnownArtistNames() {
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var artist in DBConnector.LoadTableContent_ArtistNames())
            AddKnown(known, artist.PreferredArtistName);
        foreach (var variant in DBConnector.LoadTableContent_AlternativeArtistNameVariants())
            AddKnown(known, variant.AlternativeArtistName);
        return known;
    }

    private static HashSet<string> LoadKnownLabels(IEnumerable<string?> names) {
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
            AddKnown(known, name);
        return known;
    }

    private static void AddKnown(HashSet<string> known, string? name) {
        var value = Normalize(name);
        if (value is not null)
            known.Add(value);
    }
}
