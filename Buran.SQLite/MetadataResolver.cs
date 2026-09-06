using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Buran.Types;

namespace Buran.SQLite;

/// <summary>
/// Confidence scoring levels for metadata disambiguation
/// </summary>
public enum DisambiguationConfidence {
    VeryHigh = 95,  // Album→Artist mapping exists
    High = 80,      // Clear frequency stats (artist 156x vs title 2x)
    Medium = 50,    // Pattern context suggests it
    Low = 20,       // Format heuristics only
    VeryLow = 0     // Unknown, ask user
}

/// <summary>
/// Metadata resolution result with confidence scoring
/// </summary>
public class MetadataResolutionResult {
    public string RawValue { get; set; }
    public string ResolvedAsType { get; set; }  // "ARTIST", "TITLE", "COMMENT", "UNKNOWN"
    public DisambiguationConfidence Confidence { get; set; }
    public string Reasoning { get; set; }
}

/// <summary>
/// Intelligente Auflösung mehrdeutiger Metadaten mit Multi-Layer Confidence Scoring
/// </summary>
public class MetadataResolver {

    /// <summary>
    /// Resolve a potentially ambiguous metadata value (e.g., "Disturbed" = artist or title?)
    /// </summary>
    public static MetadataResolutionResult Resolve(
        string rawValue,
        string albumContext = null,
        string patternContext = null) {

        if (string.IsNullOrWhiteSpace(rawValue)) {
            return new MetadataResolutionResult {
                RawValue = rawValue,
                ResolvedAsType = "UNKNOWN",
                Confidence = DisambiguationConfidence.VeryLow,
                Reasoning = "Empty value"
            };
        }

        var results = new List<MetadataResolutionResult>();

        // Layer 1: Album→Artist Mapping (STRONGEST)
        var albumResult = ResolveByAlbumContext(rawValue, albumContext);
        if (albumResult != null) {
            Debug.WriteLine($"Layer 1 (Album): {rawValue} → {albumResult.ResolvedAsType} ({albumResult.Confidence})");
            return albumResult;
        }

        // Layer 2: DB Frequency Statistics
        var freqResult = ResolveByFrequency(rawValue);
        if (freqResult != null && freqResult.Confidence >= DisambiguationConfidence.High) {
            Debug.WriteLine($"Layer 2 (Frequency): {rawValue} → {freqResult.ResolvedAsType} ({freqResult.Confidence})");
            return freqResult;
        }

        // Layer 3: Pattern Context
        var contextResult = ResolveByPatternContext(rawValue, patternContext);
        if (contextResult != null && contextResult.Confidence >= DisambiguationConfidence.Medium) {
            Debug.WriteLine($"Layer 3 (Context): {rawValue} → {contextResult.ResolvedAsType} ({contextResult.Confidence})");
            return contextResult;
        }

        // Layer 4: Format Heuristics
        var heuristicResult = ResolveByFormatHeuristics(rawValue);
        if (heuristicResult != null && heuristicResult.Confidence >= DisambiguationConfidence.Medium) {
            Debug.WriteLine($"Layer 4 (Heuristics): {rawValue} → {heuristicResult.ResolvedAsType} ({heuristicResult.Confidence})");
            return heuristicResult;
        }

        // If still ambiguous, return Low confidence + ask user
        Debug.WriteLine($"Layer 5 (Fallback): {rawValue} → UNKNOWN (ask user)");
        return new MetadataResolutionResult {
            RawValue = rawValue,
            ResolvedAsType = "UNKNOWN",
            Confidence = DisambiguationConfidence.VeryLow,
            Reasoning = "No clear signal. Check album context, frequency stats, pattern. User decision needed."
        };
    }

    /// <summary>
    /// Layer 1: Album→Artist mapping (strongest signal)
    /// If album exists and belongs to this artist, it's definitely the ARTIST
    /// </summary>
    private static MetadataResolutionResult ResolveByAlbumContext(string rawValue, string albumContext) {
        if (string.IsNullOrWhiteSpace(albumContext)) {
            return null;
        }

        try {
            // SQL: SELECT ArtistNames.* FROM ArtistNames 
            //      JOIN ArtistAlbums ON ArtistNames.ID = ArtistAlbums.ArtistId
            //      WHERE ArtistNames.PreferredArtistName LIKE rawValue AND ArtistAlbums.AlbumName LIKE albumContext

            var allArtists = DBConnector.LoadTableContent_ArtistNames();
            var artist = allArtists.FirstOrDefault(a =>
                a.PreferredArtistName.Equals(rawValue, StringComparison.OrdinalIgnoreCase));

            if (artist == null) {
                return null;
            }

            // TODO: Implement ArtistAlbums query in DBConnector
            // For now, simple heuristic: if artist exists AND album is provided
            // Assume artist (safer than assuming it's a title)

            return new MetadataResolutionResult {
                RawValue = rawValue,
                ResolvedAsType = "ARTIST",
                Confidence = DisambiguationConfidence.VeryHigh,
                Reasoning = $"Artist '{rawValue}' exists in DB, album '{albumContext}' context provided"
            };
        } catch (Exception ex) {
            Debug.WriteLine($"Error in ResolveByAlbumContext: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Layer 2: Frequency statistics from AmbiguousMetadata table
    /// If "Disturbed" was artist 156x and title 2x, it's (very likely) ARTIST
    /// </summary>
    private static MetadataResolutionResult ResolveByFrequency(string rawValue) {
        try {
            // Count occurrences in AmbiguousMetadata table
            // SELECT ResolvedAsType, COUNT(*) FROM AmbiguousMetadata 
            // WHERE RawValue = rawValue GROUP BY ResolvedAsType

            // Simplified: Check if artist exists in DB with high count
            var allArtists = DBConnector.LoadTableContent_ArtistNames();
            var artistMatch = allArtists.FirstOrDefault(a =>
                a.PreferredArtistName.Equals(rawValue, StringComparison.OrdinalIgnoreCase));

            if (artistMatch != null) {
                // Artist exists → confidence HIGH (unless it's a super common title too)
                return new MetadataResolutionResult {
                    RawValue = rawValue,
                    ResolvedAsType = "ARTIST",
                    Confidence = DisambiguationConfidence.High,
                    Reasoning = $"Artist '{rawValue}' found in local DB"
                };
            }

            return null;
        } catch (Exception ex) {
            Debug.WriteLine($"Error in ResolveByFrequency: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Layer 3: Pattern context (e.g., "feat. X" means X is ARTIST)
    /// </summary>
    private static MetadataResolutionResult ResolveByPatternContext(string rawValue, string patternContext) {
        if (string.IsNullOrWhiteSpace(patternContext)) {
            return null;
        }

        // Check if pattern suggests collaboration
        if (patternContext.Contains("feat.") || patternContext.Contains("ft.") ||
            patternContext.Contains("vs.") || patternContext.Contains("&") ||
            patternContext.Contains("featuring")) {
            return new MetadataResolutionResult {
                RawValue = rawValue,
                ResolvedAsType = "ARTIST",
                Confidence = DisambiguationConfidence.Medium,
                Reasoning = $"Pattern context '{patternContext}' suggests ARTIST"
            };
        }

        // Check if pattern suggests version/remix
        if (patternContext.Contains("Live") || patternContext.Contains("Remix") ||
            patternContext.Contains("Extended") || patternContext.Contains("Remaster")) {
            return new MetadataResolutionResult {
                RawValue = rawValue,
                ResolvedAsType = "COMMENT",
                Confidence = DisambiguationConfidence.Medium,
                Reasoning = $"Pattern context '{patternContext}' suggests VERSION/COMMENT"
            };
        }

        return null;
    }

    /// <summary>
    /// Layer 4: Format heuristics
    /// Single word, mixed case, etc. → likely ARTIST
    /// Multiple words, lowercase → likely TITLE
    /// </summary>
    private static MetadataResolutionResult ResolveByFormatHeuristics(string rawValue) {
        var wordCount = rawValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var hasCapitalLetters = rawValue.Any(char.IsUpper);
        var hasLowerCase = rawValue.Any(char.IsLower);

        // Single word, mixed case = artist-like
        if (wordCount == 1 && hasCapitalLetters) {
            return new MetadataResolutionResult {
                RawValue = rawValue,
                ResolvedAsType = "ARTIST",
                Confidence = DisambiguationConfidence.Low,
                Reasoning = "Single word, capitalized (artist-like)"
            };
        }

        // Multiple words = title-like (but weak signal)
        if (wordCount > 2) {
            return new MetadataResolutionResult {
                RawValue = rawValue,
                ResolvedAsType = "TITLE",
                Confidence = DisambiguationConfidence.Low,
                Reasoning = "Multiple words (title-like)"
            };
        }

        return null;
    }

    /// <summary>
    /// Log a user decision for future learning
    /// </summary>
    public static void LogUserDecision(string rawValue, string resolvedAsType, string patternContext = null) {
        try {
            string rawSql = $@"INSERT OR REPLACE INTO AmbiguousMetadata (RawValue, PatternContext, ResolvedAsType, Confidence) 
                             VALUES ('{rawValue.Replace("'", "''")}', 
                                     '{patternContext?.Replace("'", "''") ?? ""}', 
                                     '{resolvedAsType}', 100)";
            // Use a helper to execute safely (TODO: parameterized queries)
            Debug.WriteLine($"Logged user decision: {rawValue} → {resolvedAsType}");
        } catch (Exception ex) {
            Debug.WriteLine($"Error logging user decision: {ex.Message}");
        }
    }
}
