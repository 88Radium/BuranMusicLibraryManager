using Buran.Types;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Buran.SQLite;

public class FileNameParser {
        public ParsedMetadata Parse(string fileName) {
            // 1. Dateinamen säubern
            string cleanName = CleanFileName(fileName);

            // 2. Bekannte Pattern aus DB probieren
            var patterns = DBConnector.LoadFileNamePatterns();
            foreach(var pattern in patterns.OrderByDescending(p => p.Confidence)) {
                var result = TryPattern(cleanName, pattern.Pattern);
                if(result != null && IsPlausibleResult(result)) {
                    // Pattern war erfolgreich - Confidence erhöhen
                    DBConnector.IncreasePatternConfidence(pattern.Id);
                    return result;
                }
            }

            // 3. Fallback-Heuristiken (hardcoded Standard-Patterns)
            return FallbackParse(cleanName);
        }

        private string CleanFileName(string fileName) {
            // Nur Dateiname ohne Pfad und Endung
            string name = Path.GetFileNameWithoutExtension(fileName);

            // Common replacements
            name = name.Replace('_', ' ')
                       .Replace('[', ' ').Replace(']', ' ')
                       .Replace('(', ' ').Replace(')', ' ')
                       .Replace('{', ' ').Replace('}', ' ')
                       .Trim();

            // Mehrere Leerzeichen zu einem
            name = Regex.Replace(name, @"\s+", " ");

            return name;
        }

        private ParsedMetadata TryPattern(string fileName, string pattern) {
            try {
                string regex = ConvertToRegex(pattern);
                var match = Regex.Match(fileName, regex, RegexOptions.IgnoreCase);

                if (match.Success) {
                    var metadata = new ParsedMetadata {
                        Title = GetGroupValue(match, "title"),
                        Album = GetGroupValue(match, "album"),
                        TrackNumber = GetGroupValue(match, "track"),
                        Year = GetGroupValue(match, "year")
                    };

                    // Sammle alle Artists (sowohl {artist} als auch {artists_N})
                    var allArtists = new List<string>();

                    // Single {artist}
                    var artist = GetGroupValue(match, "artist");
                    if (!string.IsNullOrWhiteSpace(artist)) {
                        allArtists.AddRange(ParseArtistsString(artist));
                    }

                    // Mehrfache {artists_N} - alle durchgehen
                    for (int i = 1; i <= 10; i++) {
                        var artistN = GetGroupValue(match, $"artists_{i}");
                        if (!string.IsNullOrWhiteSpace(artistN)) {
                            allArtists.AddRange(ParseArtistsString(artistN));
                        }
                    }

                    // Duplikate entfernen (case-insensitive)
                    metadata.Artists = allArtists
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // Sammle alle Comments (mehrfache {comment_N})
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
            // Spezialfall: Klammern in Pattern escaped behandeln
            pattern = pattern.Replace("(", @"\(").Replace(")", @"\)");

            // Platzhalter-Zähler für mehrfaches Vorkommen
            var artistCount = 0;
            var commentCount = 0;

            // Mehrfache {artists} → artists_1, artists_2, etc.
            pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"\{artists\}",
                m => $"___ARTISTS_{++artistCount}___");

            // Mehrfache {comment} → comment_1, comment_2, etc.
            pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"\{comment\}",
                m => $"___COMMENT_{++commentCount}___");

            // Einzelne {artist} (kein Zähler, nur einer pro Pattern)
            pattern = pattern.Replace("{artist}", "___ARTIST___")
                             .Replace("{title}", "___TITLE___")
                             .Replace("{album}", "___ALBUM___")
                             .Replace("{track}", "___TRACK___")
                             .Replace("{year}", "___YEAR___");

            // Jetzt escapen (damit Sonderzeichen safe sind)
            string escaped = System.Text.RegularExpressions.Regex.Escape(pattern);

            // Temporäre Markierungen durch Regex-Gruppen ersetzen
            escaped = escaped.Replace("___ARTIST___", "(?<artist>.+?)");

            // Mehrfache {artists_N} → jeweilige Groups
            for (int i = 1; i <= artistCount; i++) {
                escaped = escaped.Replace($"___ARTISTS_{i}___", $"(?<artists_{i}>.+?)");
            }

            escaped = escaped.Replace("___TITLE___", "(?<title>.+?)")
                             .Replace("___ALBUM___", "(?<album>.+?)")
                             .Replace("___TRACK___", "(?<track>.+?)")
                             .Replace("___YEAR___", "(?<year>.+?)");

            // Mehrfache {comment_N} → jeweilige Groups
            for (int i = 1; i <= commentCount; i++) {
                escaped = escaped.Replace($"___COMMENT_{i}___", $"(?<comment_{i}>.+?)");
            }

            // Bindestrich mit optionalen Leerzeichen
            escaped = escaped.Replace(@"\ \- ", @"\s*-\s*");

            if (!escaped.StartsWith("^")) escaped = "^" + escaped;
            if (!escaped.EndsWith("$")) escaped = escaped + "$";

            return escaped;
        }

        private string GetGroupValue(Match match, string groupName) {
            var group = match.Groups[groupName];
            return group.Success ? group.Value.Trim() : null;
        }

        private bool IsPlausibleResult(ParsedMetadata metadata) {
            // Einfache Plausibilitätsprüfung
            if(string.IsNullOrWhiteSpace(metadata.Title))
                return false;

            return true;
        }

        private ParsedMetadata FallbackParse(string fileName) {
            // 1. "Artist(s) - Title" Pattern (mit feat., & usw.)
            var artistTitleMatch = Regex.Match(fileName,
                @"^(?<artist>.+?)\s*[-\u2013\u2014]\s*(?<title>.+)$",
                RegexOptions.IgnoreCase);

            if(artistTitleMatch.Success) {
                return new ParsedMetadata {
                    Artists = ParseArtistsString(artistTitleMatch.Groups["artist"].Value.Trim()),
                    Title = artistTitleMatch.Groups["title"].Value.Trim()
                };
            }

            // 2. "Track - Title" (ohne Artist)
            var trackMatch = Regex.Match(fileName,
                @"^(?<track>\d{1,3})(?:\.\d{1,2})?\s*[-\u2013\u2014\.]\s*(?<title>.+)$",
                RegexOptions.IgnoreCase);

            if(trackMatch.Success) {
                return new ParsedMetadata {
                    TrackNumber = trackMatch.Groups["track"].Value.Trim(),
                    Title = trackMatch.Groups["title"].Value.Trim()
                };
            }

            // 3. Default: Alles als Titel
            return new ParsedMetadata { Title = fileName };
        }

        /// <summary>
        /// Parst einen Artist-String in eine Liste von Künstlernamen
        /// Unterstützt: "Artist1 & Artist2", "Artist1 feat. Artist2", "Artist1, Artist2 and Artist3"
        /// </summary>
        private List<string> ParseArtistsString(string artistsString) {
            if(string.IsNullOrWhiteSpace(artistsString))
                return new List<string>();

            // Normalisiere Trennzeichen
            string normalized = artistsString
                .Replace("feat.", ",")
                .Replace("ft.", ",")
                .Replace("&", ",")
                .Replace(" and ", ",");

            // Splitte und trimme
            return normalized.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(a => a.Trim())
                            .Where(a => !string.IsNullOrWhiteSpace(a))
                            .ToList();
        }

        /// <summary>
        /// Wende geparste Metadaten auf ein MP3FileObject an
        /// </summary>
        public void ApplyToMP3FileObject(Mp3FileObject mp3File, ParsedMetadata metadata) {
            if (mp3File == null || metadata == null)
                return;

            // Titel setzen
            if (!string.IsNullOrEmpty(metadata.Title))
                mp3File.Id3Title = metadata.Title;

            // Künstler setzen (als Liste!)
            if (metadata.Artists != null && metadata.Artists.Any()) {
                // Für jeden Künstler DB-Check machen
                var dbArtistNames = new List<string>();

                foreach (var artist in metadata.Artists) {
                    var artistEvent = DBConnector.CheckForPreferredName(artist);
                    dbArtistNames.Add(artistEvent.PreferredArtistName);

                    // Neuen Künstler in DB eintragen, falls unbekannt
                    if (artistEvent.ArtistNameStatus == ArtistNameStatus.IsNonExistent) {
                        DBConnector.InsertInto_ArtistNames(artistEvent.PreferredArtistName, "");
                    }
                }

                // MP3FileObject aktualisieren
                mp3File.Id3ArtistList = dbArtistNames;
            }

            // Album setzen
            if (!string.IsNullOrEmpty(metadata.Album))
                mp3File.Id3Album = metadata.Album;

            // Comments setzen (mehrfach möglich, mit "; " verbunden)
            if (metadata.Comments != null && metadata.Comments.Any()) {
                mp3File.Id3Comment = string.Join("; ", metadata.Comments);
            }

            // Tracknummer setzen
            if (!string.IsNullOrEmpty(metadata.TrackNumber) &&
                int.TryParse(metadata.TrackNumber, out int trackNum)) {
                // Tracknummer setzen - falls deine MP3FileObject-Klasse das unterstützt
                // mp3File.Track = trackNum; // Wenn du eine Track-Property hast
            }

            // Jahr setzen
            if (!string.IsNullOrEmpty(metadata.Year) &&
                uint.TryParse(metadata.Year, out uint year)) {
                mp3File.Id3ReleaseYear = (int?)year;
            }
        }

        /// <summary>
        /// Lerne neues Pattern aus User-Korrektur
        /// </summary>
        public void LearnNewPattern(string fileName, ParsedMetadata correctMetadata) {
            // Pattern generieren basierend auf den Metadaten
            string pattern = GeneratePatternFromMetadata(fileName, correctMetadata);

            if(!string.IsNullOrEmpty(pattern)) {
                DBConnector.InsertFileNamePattern(pattern, fileName, confidence: 10);
            }
        }

        private string GeneratePatternFromMetadata(string fileName, ParsedMetadata metadata) {
            // Einfache Heuristik: Ersetze bekannte Werte mit Platzhaltern
            string pattern = CleanFileName(fileName);

            // Artist-String erstellen (für Pattern-Matching)
            string artistString = metadata.Artists != null ?
                string.Join(" & ", metadata.Artists) : "";

            if(!string.IsNullOrEmpty(artistString))
                pattern = pattern.Replace(artistString, "{artist}");

            if(!string.IsNullOrEmpty(metadata.Title))
                pattern = pattern.Replace(metadata.Title, "{title}");

            if(!string.IsNullOrEmpty(metadata.Album))
                pattern = pattern.Replace(metadata.Album, "{album}");

            if(!string.IsNullOrEmpty(metadata.TrackNumber))
                pattern = pattern.Replace(metadata.TrackNumber, "{track}");

            // Nur speichern, wenn mindestens 2 Platzhalter ersetzt wurden
            var placeholderCount = Regex.Matches(pattern, @"\{\w+\}").Count;
            return placeholderCount >= 2 ? pattern : null;
        }
    }