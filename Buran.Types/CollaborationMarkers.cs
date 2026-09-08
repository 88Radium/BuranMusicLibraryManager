using System.Text.RegularExpressions;

namespace Buran.Types;

/// <summary>
/// Collaboration / version tokens used when splitting artist strings and reading filenames.
/// Defaults live here; at runtime the list is loaded from FeatureKeywords and can be edited in the DB editor.
/// Matching is case-insensitive. Word tokens use word boundaries, so "feat" matches "FEAT" / "feat."
/// without splitting "Soft" on "ft". Known catalog names and one-letter fragments ("D &amp; F") stay unsplit.
/// "feat. X" in a title or in parentheses is promoted to an extra artist, not kept as a comment.
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

    public readonly record struct RolePeelResult(
        string Remainder,
        IReadOnlyList<string> FeaturedArtists,
        IReadOnlyList<string> VersionComments);

    private static readonly object Sync = new();
    private static readonly Regex WrapperRegex = new(
        @"[\(\[]([^\)\]]+)[\)\]]",
        RegexOptions.Compiled);

    private static readonly Regex TrailingFeatRegex = new(
        @"\s+\b(?:featuring|feat|ft)\b\.?\s+(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static Regex _splitRegex = MatchNothing();
    private static Regex _collaborationDetectRegex = MatchNothing();
    private static Regex _versionDetectRegex = MatchNothing();
    private static Regex _featuredInnerPrefixRegex = MatchNothing();
    private static HashSet<string> _knownArtists = new(StringComparer.OrdinalIgnoreCase);

    static CollaborationMarkers() => ApplyDefaults();

    public static IEnumerable<Seed> DefaultSeeds() {
        yield return new Seed("&", TypeCollaboration, 10);
        yield return new Seed("+", TypeCollaboration, 8);
        yield return new Seed("/", TypeCollaboration, 8);
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
        var symbols       = new HashSet<char> { ',', ';' };

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

        var split      = BuildSplit(collabWords, symbols);
        var collabDet  = BuildDetect(collabWords.Concat(symbols.Select(c => c.ToString())));
        var versionDet = BuildDetect(versionTokens);
        var innerPref  = BuildFeaturedInnerPrefix(collabWords);

        lock (Sync) {
            _splitRegex               = split;
            _collaborationDetectRegex = collabDet;
            _versionDetectRegex       = versionDet;
            _featuredInnerPrefixRegex = innerPref;
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

    public static void SetKnownArtistNames(IEnumerable<string?> names) {
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names) {
            var normalized = CollapseWhitespace(name);
            if (normalized.Length > 0)
                known.Add(normalized);
        }

        lock (Sync)
            _knownArtists = known;
    }

    /// <summary>
    /// Pull featured artists and version hints out of a filename fragment.
    /// Parenthetical "feat. X" is always an artist. Unwrapped trailing "feat./ft./featuring"
    /// is only applied to title/comment, never to the full "Artist feat. X - Title" line.
    /// </summary>
    public static RolePeelResult PeelRoles(string? text, bool includeTrailingUnwrapped = true) {
        if (string.IsNullOrWhiteSpace(text))
            return new("", [], []);

        Regex innerPrefix;
        lock (Sync)
            innerPrefix = _featuredInnerPrefixRegex;

        var featured = new List<string>();
        var versions = new List<string>();
        var remainder = text.Trim();

        remainder = WrapperRegex.Replace(remainder, match => {
            var inner = match.Groups[1].Value.Trim();
            if (inner.Length == 0)
                return match.Value;

            var featuredMatch = innerPrefix.Match(inner);
            if (featuredMatch.Success) {
                featured.AddRange(SplitArtistNames(featuredMatch.Groups[1].Value));
                return " ";
            }

            if (IndicatesVersion(inner)) {
                versions.Add(CollapseWhitespace(inner));
                return " ";
            }

            return match.Value;
        });

        if (includeTrailingUnwrapped) {
            var trailing = TrailingFeatRegex.Match(remainder);
            if (trailing.Success) {
                featured.AddRange(SplitArtistNames(trailing.Groups[1].Value));
                remainder = remainder[..trailing.Index];
            }
        }

        return new(
            CollapseWhitespace(remainder),
            DistinctPreserveOrder(featured),
            DistinctPreserveOrder(versions));
    }

    /// <summary>
    /// After a pattern match: "feat. X" becomes extra artists, (Live)/[Remix] become comments.
    /// </summary>
    public static void PromoteFeaturedArtistsAndVersions(ParsedMetadata metadata, string? fileNameHint) {
        if (metadata is null)
            return;

        var artists  = new List<string>();
        var comments = new List<string>();

        void addArtists(IEnumerable<string> names) {
            foreach (var name in names) {
                var cleaned = TrimBrackets(name);
                if (cleaned.Length == 0)
                    continue;
                if (artists.Any(a => a.Equals(cleaned, StringComparison.OrdinalIgnoreCase)))
                    continue;
                artists.Add(cleaned);
            }
        }

        void addComments(IEnumerable<string> names) {
            foreach (var name in names) {
                if (name.Length == 0)
                    continue;
                if (artists.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (comments.Any(c => c.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                comments.Add(name);
            }
        }

        foreach (var rawArtist in metadata.Artists) {
            var peeled = PeelRoles(rawArtist, includeTrailingUnwrapped: false);
            addArtists(peeled.FeaturedArtists);
            addComments(peeled.VersionComments);
            addArtists([peeled.Remainder]);
        }

        var wrapped = PeelRoles(fileNameHint, includeTrailingUnwrapped: false);
        addArtists(wrapped.FeaturedArtists);
        addComments(wrapped.VersionComments);

        if (!string.IsNullOrWhiteSpace(metadata.Title)) {
            var titlePeel = PeelRoles(metadata.Title, includeTrailingUnwrapped: true);
            addArtists(titlePeel.FeaturedArtists);
            addComments(titlePeel.VersionComments);
            if (!string.IsNullOrWhiteSpace(titlePeel.Remainder))
                metadata.Title = titlePeel.Remainder;
        }

        foreach (var comment in metadata.Comments) {
            var peeled = PeelRoles(comment, includeTrailingUnwrapped: true);
            addArtists(peeled.FeaturedArtists);
            addComments(peeled.VersionComments);

            var rest = peeled.Remainder;
            if (string.IsNullOrWhiteSpace(rest))
                continue;
            if (artists.Any(a => a.Equals(rest, StringComparison.OrdinalIgnoreCase)))
                continue;
            addComments([rest]);
        }

        metadata.Artists  = artists;
        metadata.Comments = comments;
    }

    public static List<string> SplitArtistNames(string? artistsString) {
        if (string.IsNullOrWhiteSpace(artistsString))
            return [];

        var trimmed = CollapseWhitespace(artistsString);
        if (trimmed.Length == 0)
            return [];
        if (IsKnownArtist(trimmed))
            return [trimmed];

        var regex = _splitRegex;
        var parts = regex.Split(trimmed)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();

        if (parts.Count <= 1)
            return parts.Count == 1 ? parts : [trimmed];

        // "D & F" / "P + J": keep the band name instead of one-letter fragments.
        if (parts.Any(part => part.Length == 1))
            return [trimmed];

        return parts;
    }

    private static bool IsKnownArtist(string name) {
        HashSet<string> known;
        lock (Sync)
            known = _knownArtists;
        return known.Contains(name);
    }

    private static string CollapseWhitespace(string? raw) {
        if (string.IsNullOrWhiteSpace(raw))
            return "";
        return Regex.Replace(raw.Trim(), @"\s+", " ");
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

    private static string TrimBrackets(string? text) {
        var value = CollapseWhitespace(text);
        var changed = true;
        while (changed && value.Length > 0) {
            changed = false;
            if (value[0] is '(' or ')' or '[' or ']' or '{' or '}') {
                value   = value[1..].TrimStart();
                changed = true;
            }

            if (value.Length > 0 && value[^1] is '(' or ')' or '[' or ']' or '{' or '}') {
                value   = value[..^1].TrimEnd();
                changed = true;
            }
        }

        return value;
    }

    private static List<string> DistinctPreserveOrder(IEnumerable<string>? names) {
        var result = new List<string>();
        if (names is null)
            return result;

        foreach (var raw in names) {
            var name = CollapseWhitespace(raw);
            if (name.Length == 0)
                continue;
            if (result.Any(existing => existing.Equals(name, StringComparison.OrdinalIgnoreCase)))
                continue;
            result.Add(name);
        }

        return result;
    }

    private static List<string> DistinctLongestFirst(IEnumerable<string> tokens) =>
        tokens
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(t => t.Length)
            .ThenBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static Regex BuildFeaturedInnerPrefix(IReadOnlyList<string> collabWords) {
        var peel = collabWords
            .Where(word => !word.Equals("and", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (peel.Count == 0)
            return MatchNothing();

        var alternation = string.Join("|", peel.Select(Regex.Escape));
        return new Regex(
            $@"^(?:{alternation})\.?\s+(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

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
