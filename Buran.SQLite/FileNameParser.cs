using System.Collections.ObjectModel;
using Buran.Types;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Buran.SQLite;

public class FileNameParser {
    public ParsedMetadata Parse(string fileName, ObservableCollection<DBConnector.FileNamePattern> patterns) {
        string cleanName = CleanFileName(fileName);

        foreach (var pattern in patterns.OrderByDescending(p => p.Confidence)) {
            var result = TryPattern(cleanName, pattern.Pattern);
            if (result != null && IsPlausibleResult(result)) {
                result.MatchedPatternId = pattern.Id;
                CollaborationMarkers.PromoteFeaturedArtistsAndVersions(result, cleanName);
                AttachYearIfMissing(result, cleanName);
                return result;
            }
        }

        var fallback = FallbackParse(cleanName);
        CollaborationMarkers.PromoteFeaturedArtistsAndVersions(fallback, cleanName);
        AttachYearIfMissing(fallback, cleanName);
        return fallback;
    }

    private string CleanFileName(string fileName) {
        string name = Path.GetFileNameWithoutExtension(fileName);
        name = name.Replace('_', ' ')
            .Replace('\u2013', '-')
            .Replace('\u2014', '-');
        name = Regex.Replace(name, @"\s+", " ").Trim();
        return name;
    }

    private ParsedMetadata? TryPattern(string fileName, string pattern) {
        try {
            string regex = ConvertToRegex(pattern);
            var    match = Regex.Match(fileName, regex, RegexOptions.IgnoreCase);

            if (match.Success) {
                var metadata = new ParsedMetadata {
                    Title       = GetGroupValue(match, "title"),
                    Album       = GetGroupValue(match, "album"),
                    TrackNumber = GetGroupValue(match, "track"),
                    Year        = GetGroupValue(match, "year")
                };

                // {artist} and {artists} both become numbered groups artists_1..N
                var allArtists = new List<string>();
                for (int i = 1; i <= 10; i++) {
                    var artistN = GetGroupValue(match, $"artists_{i}");
                    if (!string.IsNullOrWhiteSpace(artistN))
                        allArtists.AddRange(ParseArtistsString(artistN));
                }

                // Drop duplicates (case-insensitive)
                metadata.Artists = allArtists
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Collect all comments (repeated {comment_N})
                for (int i = 1; i <= 10; i++) {
                    var comment = GetGroupValue(match, $"comment_{i}");
                    if (!string.IsNullOrWhiteSpace(comment)) {
                        metadata.Comments.Add(comment);
                    }
                }

                return metadata;
            }
        } catch (Exception ex) {
            Debug.WriteLine($"Pattern error: {pattern} - {ex.Message}");
        }

        return null;
    }

    private string ConvertToRegex(string pattern) {
        var artistCount  = 0;
        var commentCount = 0;

        // {artist} and {artists} are the same slot type; {Artist} is accepted too
        pattern = Regex.Replace(pattern, @"\{artists?\}",
            _ => $"___ARTISTS_{++artistCount}___",
            RegexOptions.IgnoreCase);
        pattern = Regex.Replace(pattern, @"\{comment\}",
            _ => $"___COMMENT_{++commentCount}___",
            RegexOptions.IgnoreCase);
        pattern = Regex.Replace(pattern, @"\{title\}",  "___TITLE___",  RegexOptions.IgnoreCase);
        pattern = Regex.Replace(pattern, @"\{album\}",  "___ALBUM___",  RegexOptions.IgnoreCase);
        pattern = Regex.Replace(pattern, @"\{track\}",  "___TRACK___",  RegexOptions.IgnoreCase);
        pattern = Regex.Replace(pattern, @"\{year\}",   "___YEAR___",   RegexOptions.IgnoreCase);

        string escaped = Regex.Escape(pattern);

        for (int i = 1; i <= artistCount; i++)
            escaped = ReplaceGroup(escaped, $"___ARTISTS_{i}___", $"artists_{i}");

        escaped = ReplaceGroup(escaped, "___TITLE___", "title");
        escaped = ReplaceGroup(escaped, "___ALBUM___", "album");
        escaped = escaped.Replace("___TRACK___", @"(?<track>.+?)")
            .Replace("___YEAR___",  @"(?<year>\d{4})");

        for (int i = 1; i <= commentCount; i++)
            escaped = ReplaceGroup(escaped, $"___COMMENT_{i}___", $"comment_{i}");

        escaped = escaped.Replace(@"\ \-\ ", @"\s*-\s*")
            .Replace(@"\ \-", @"\s*-\s*")
            .Replace(@"\-\ ", @"\s*-\s*");

        if (!escaped.StartsWith("^")) escaped = "^"     + escaped;
        if (!escaped.EndsWith("$")) escaped   = escaped + "$";

        return escaped;
    }

    private static string ReplaceGroup(string escaped, string token, string groupName) {
        var body = escaped.Contains(token + @"\)", StringComparison.Ordinal) ? "[^)]+" : ".+?";
        return escaped.Replace(token, $"(?<{groupName}>{body})");
    }

    private string? GetGroupValue(Match match, string groupName) {
        var group = match.Groups[groupName];
        return group.Success ? group.Value.Trim() : null;
    }

    private bool IsPlausibleResult(ParsedMetadata metadata) {
        // Simple plausibility check
        if (string.IsNullOrWhiteSpace(metadata.Title))
            return false;

        return true;
    }

    private ParsedMetadata FallbackParse(string fileName) {
        // 1. "Artist(s) - Title" (feat., &, etc.)
        var artistTitleMatch = Regex.Match(fileName,
            @"^(?<artist>.+?)\s*[-\u2013\u2014]\s*(?<title>.+)$",
            RegexOptions.IgnoreCase);

        if (artistTitleMatch.Success) {
            return new ParsedMetadata {
                Artists = ParseArtistsString(artistTitleMatch.Groups["artist"].Value.Trim()),
                Title   = artistTitleMatch.Groups["title"].Value.Trim()
            };
        }

        // 2. "Track - Title" (no artist)
        var trackMatch = Regex.Match(fileName,
            @"^(?<track>\d{1,3})(?:\.\d{1,2})?\s*[-\u2013\u2014\.]\s*(?<title>.+)$",
            RegexOptions.IgnoreCase);

        if (trackMatch.Success) {
            return new ParsedMetadata {
                TrackNumber = trackMatch.Groups["track"].Value.Trim(),
                Title       = trackMatch.Groups["title"].Value.Trim()
            };
        }

        // 3. Default: treat the whole name as the title
        return new ParsedMetadata { Title = fileName };
    }

    /// <summary>
    /// Splits an artist string into a list of artist names.
    /// Tokens come from <see cref="CollaborationMarkers"/> (case-insensitive, word-bounded).
    /// </summary>
    private List<string> ParseArtistsString(string artistsString) =>
        CollaborationMarkers.SplitArtistNames(artistsString);

    private static readonly Regex YearInName = new(@"\b((?:19|20)\d{2})\b", RegexOptions.Compiled);

    private static void AttachYearIfMissing(ParsedMetadata metadata, string fileName) {
        if (!string.IsNullOrWhiteSpace(metadata.Year) && Regex.IsMatch(metadata.Year, @"^\d{4}$"))
            return;

        var match = YearInName.Match(fileName);
        if (match.Success)
            metadata.Year = match.Groups[1].Value;
    }

    /// <summary>
    /// Learns a new pattern from a user correction.
    /// </summary>
    public void LearnNewPattern(string fileName, ParsedMetadata correctMetadata) {
        var pattern = GeneratePatternFromMetadata(fileName, correctMetadata);

        if (!string.IsNullOrEmpty(pattern)) {
            DBConnector.InsertFileNamePattern(pattern, fileName, confidence: 10);
        }
    }

    private string? GeneratePatternFromMetadata(string fileName, ParsedMetadata metadata) {
        // Simple heuristic: replace known values with placeholders
        string pattern = CleanFileName(fileName);

        string artistString = metadata.Artists != null ? string.Join(" & ", metadata.Artists) : "";

        if (!string.IsNullOrEmpty(artistString))
            pattern = pattern.Replace(artistString, "{artist}");

        if (!string.IsNullOrEmpty(metadata.Title))
            pattern = pattern.Replace(metadata.Title, "{title}");

        if (!string.IsNullOrEmpty(metadata.Album))
            pattern = pattern.Replace(metadata.Album, "{album}");

        if (!string.IsNullOrEmpty(metadata.TrackNumber))
            pattern = pattern.Replace(metadata.TrackNumber, "{track}");

        if (!string.IsNullOrEmpty(metadata.Year))
            pattern = pattern.Replace(metadata.Year, "{year}");

        // Keep only if at least two placeholders were substituted
        var placeholderCount = Regex.Matches(pattern, @"\{\w+\}").Count;
        return placeholderCount >= 2 ? pattern : null;
    }
}
