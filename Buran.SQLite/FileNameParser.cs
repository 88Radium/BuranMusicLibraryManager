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
                return result;
            }
        }

        return FallbackParse(cleanName);
    }

    private string CleanFileName(string fileName) {
        // File name only, without path or extension
        string name = Path.GetFileNameWithoutExtension(fileName);

        // Common replacements
        name = name.Replace('_', ' ')
            .Replace('[', ' ').Replace(']', ' ')
            .Replace('(', ' ').Replace(')', ' ')
            .Replace('{', ' ').Replace('}', ' ')
            .Trim();

        // Collapse consecutive whitespace
        name = Regex.Replace(name, @"\s+", " ");

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

                // Collect all artists ({artist} and {artists_N})
                var allArtists = new List<string>();

                // Single {artist}
                var artist = GetGroupValue(match, "artist");
                if (!string.IsNullOrWhiteSpace(artist)) {
                    allArtists.AddRange(ParseArtistsString(artist));
                }

                // Repeated {artists_N}
                for (int i = 1; i <= 10; i++) {
                    var artistN = GetGroupValue(match, $"artists_{i}");
                    if (!string.IsNullOrWhiteSpace(artistN)) {
                        allArtists.AddRange(ParseArtistsString(artistN));
                    }
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
        // Treat parentheses in the pattern as literals
        pattern = pattern.Replace("(", @"\(").Replace(")", @"\)");

        // Counters for placeholders that can appear more than once
        var artistCount  = 0;
        var commentCount = 0;

        // Repeated {artists} → artists_1, artists_2, etc.
        pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"\{artists\}",
            m => $"___ARTISTS_{++artistCount}___");

        // Repeated {comment} → comment_1, comment_2, etc.
        pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"\{comment\}",
            m => $"___COMMENT_{++commentCount}___");

        // Single {artist} (no counter; one per pattern)
        pattern = pattern.Replace("{artist}", "___ARTIST___")
            .Replace("{title}", "___TITLE___")
            .Replace("{album}", "___ALBUM___")
            .Replace("{track}", "___TRACK___")
            .Replace("{year}",  "___YEAR___");

        // Escape so remaining special characters are literal
        string escaped = System.Text.RegularExpressions.Regex.Escape(pattern);

        // Replace temporary markers with named regex groups
        escaped = escaped.Replace("___ARTIST___", "(?<artist>.+?)");

        // Repeated {artists_N} → matching groups
        for (int i = 1; i <= artistCount; i++) {
            escaped = escaped.Replace($"___ARTISTS_{i}___", $"(?<artists_{i}>.+?)");
        }

        escaped = escaped.Replace("___TITLE___", "(?<title>.+?)")
            .Replace("___ALBUM___", "(?<album>.+?)")
            .Replace("___TRACK___", "(?<track>.+?)")
            .Replace("___YEAR___",  "(?<year>.+?)");

        // Repeated {comment_N} → matching groups
        for (int i = 1; i <= commentCount; i++) {
            escaped = escaped.Replace($"___COMMENT_{i}___", $"(?<comment_{i}>.+?)");
        }

        // Hyphen with optional surrounding whitespace
        escaped = escaped.Replace(@"\ \- ", @"\s*-\s*");

        if (!escaped.StartsWith("^")) escaped = "^"     + escaped;
        if (!escaped.EndsWith("$")) escaped   = escaped + "$";

        return escaped;
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

        // Keep only if at least two placeholders were substituted
        var placeholderCount = Regex.Matches(pattern, @"\{\w+\}").Count;
        return placeholderCount >= 2 ? pattern : null;
    }
}
