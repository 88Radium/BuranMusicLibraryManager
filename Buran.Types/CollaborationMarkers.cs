using System.Text.RegularExpressions;

namespace Buran.Types;

/// <summary>
/// Collaboration / version tokens used when splitting artist strings and reading filenames.
/// Defaults live here; at runtime the list is loaded from FeatureKeywords and can be edited in the DB editor.
/// Matching is case-insensitive. Word tokens use word boundaries, so "feat" matches "FEAT" / "feat."
/// without splitting "Soft" on "ft".
/// </summary>
public static class CollaborationMarkers {
    public const string TypeCollaboration = "COLLABORATION";
    public const string TypeVersion       = "VERSION";

    public readonly record struct Seed(string Keyword, string Type, int Weight);

    public static readonly string[] DefaultCollaborationWords = [
        "featuring",
        "feat",
        "ft",
        "vs",
        "with",
        "and"
    ];

    public static readonly string[] DefaultVersionWords = [
        "live",
        "remix",
        "extended",
        "remaster",
        "remastered"
    ];

    private static readonly object Sync = new();
    private static Regex _splitRegex = MatchNothing();
    private static Regex _collaborationDetectRegex = MatchNothing();
    private static Regex _versionDetectRegex = MatchNothing();

    static CollaborationMarkers() => ApplyDefaults();

    public static IEnumerable<Seed> DefaultSeeds() {
        yield return new Seed("&", TypeCollaboration, 10);
        foreach (var word in DefaultCollaborationWords) {
            yield return new Seed(word, TypeCollaboration, WeightFor(word));
        }

        foreach (var word in DefaultVersionWords)
            yield return new Seed(word, TypeVersion, 6);
    }

    public static void ApplyDefaults() =>
        Apply(DefaultSeeds().Select(s => (s.Keyword, s.Type)));

    public static void Apply(IEnumerable<(string Keyword, string Type)> rows) {
        var collabWords   = new List<string>();
        var versionTokens = new List<string>();
        var symbols       = new HashSet<char> { ',', ';', '/', '+', '&' };

        foreach (var (raw, type) in rows) {
            var token = Normalize(raw);
            if (token.Length == 0)
                continue;

            if (IsVersion(type)) {
                versionTokens.Add(token);
                continue;
            }

            if (token.Length == 1 && !char.IsLetterOrDigit(token[0]))
                symbols.Add(token[0]);
            else
                collabWords.Add(token);
        }

        if (collabWords.Count == 0)
            collabWords.AddRange(DefaultCollaborationWords);
        if (versionTokens.Count == 0)
            versionTokens.AddRange(DefaultVersionWords);

        collabWords   = DistinctLongestFirst(collabWords);
        versionTokens = DistinctLongestFirst(versionTokens);

        var split     = BuildSplit(collabWords, symbols);
        var collabDet = BuildDetect(collabWords.Concat(symbols.Select(c => c.ToString())));
        var versionDet = BuildDetect(versionTokens);

        lock (Sync) {
            _splitRegex                = split;
            _collaborationDetectRegex  = collabDet;
            _versionDetectRegex        = versionDet;
        }
    }

    public static bool IndicatesCollaboration(string? text) {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var regex = _collaborationDetectRegex;
        return regex.IsMatch(text);
    }

    public static bool IndicatesVersion(string? text) {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var regex = _versionDetectRegex;
        return regex.IsMatch(text);
    }

    public static List<string> SplitArtistNames(string? artistsString) {
        if (string.IsNullOrWhiteSpace(artistsString))
            return [];

        var regex = _splitRegex;
        return regex.Split(artistsString)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();
    }

    public static string Normalize(string? raw) {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        var token = raw.Trim();
        while (token.EndsWith('.'))
            token = token[..^1].TrimEnd();
        return token;
    }

    public static bool IsVersion(string? type) =>
        string.Equals(type, TypeVersion, StringComparison.OrdinalIgnoreCase);

    private static List<string> DistinctLongestFirst(IEnumerable<string> tokens) =>
        tokens
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(t => t.Length)
            .ThenBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static Regex BuildSplit(IReadOnlyList<string> words, IEnumerable<char> symbols) {
        var symbolClass = string.Concat(symbols.Select(EscapeCharClass));
        var wordAlt     = string.Join("|", words.Select(Regex.Escape));
        var pattern = string.IsNullOrEmpty(wordAlt)
            ? $@"(?:\s*[{symbolClass}]\s*)"
            : $@"(?:\s*[{symbolClass}]\s*|\s*\b(?:{wordAlt})\b\.?\s*)";
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    private static Regex BuildDetect(IEnumerable<string> tokens) {
        var parts = tokens
            .Where(t => t.Length > 0)
            .Select(t => {
                var escaped = Regex.Escape(t);
                return t.All(char.IsLetterOrDigit) ? $@"\b{escaped}\b" : escaped;
            })
            .ToList();

        if (parts.Count == 0)
            return MatchNothing();

        return new Regex(
            $@"(?:{string.Join("|", parts)})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    private static Regex MatchNothing() =>
        new("(?!)", RegexOptions.Compiled);

    private static string EscapeCharClass(char c) => c switch {
        ']'  => @"\]",
        '\\' => @"\\",
        '^'  => @"\^",
        '-'  => @"\-",
        _    => c.ToString()
    };

    private static int WeightFor(string word) => word switch {
        "feat" or "ft" => 10,
        "featuring"    => 9,
        "vs"           => 8,
        "and"          => 7,
        "with"         => 6,
        _              => 5
    };
}
