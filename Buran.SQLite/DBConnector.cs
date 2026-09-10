using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Buran.Types;
using Microsoft.Data.Sqlite;

namespace Buran.SQLite;

public partial class DBConnector {
        public static void TestConnection() {
            using(SqliteConnection _connection = new SqliteConnection(ConnectionString)) {
                _connection.Open();

                string SqlCreateArtistName = @"CREATE TABLE IF NOT EXISTS 'ArtistNames' (
	                'ID' INTEGER NOT NULL UNIQUE,
                    'PreferredArtistName' TEXT NOT NULL,
	                'RealName'	TEXT,
	                PRIMARY KEY('ID' AUTOINCREMENT)
                    );";
                queryExecutor(SqlCreateArtistName);

                string SqlCreateAlternativeArtistNameVariants = @"CREATE TABLE IF NOT EXISTS AlternativeArtistNameVariants(
                    'ID' INTEGER NOT NULL UNIQUE,
                    'AlternativeArtistName' TEXT NOT NULL,
                    'IsMissSpelled' BOOL NOT NULL,
                    'RefersToArtistName' INT NOT NULL,
                    PRIMARY KEY('ID' AUTOINCREMENT)
                    )";
                queryExecutor(SqlCreateAlternativeArtistNameVariants);

                string SqlGenreNames = @"CREATE TABLE IF NOT EXISTS GenreNames(
                    'ID' INTEGER NOT NULL UNIQUE,
                    'GenreName' TEXT NOT NULL,
                    PRIMARY KEY('ID' AUTOINCREMENT)
                    )";
                queryExecutor(SqlGenreNames);


                string SqlCreateAlternativeGenreNameVariants = @"CREATE TABLE IF NOT EXISTS AlternativeGenreNameVariants(
                    'ID' INTEGER NOT NULL UNIQUE,
                    'GenreNameVariant' TEXT NOT NULL,
                    'IsMissSpelled' bool NOT NULL,
                    'RefersToGenreName' INT NOT NULL,
                    PRIMARY KEY('ID' AUTOINCREMENT)
                    )";
                queryExecutor(SqlCreateAlternativeGenreNameVariants);

                queryExecutor(@"CREATE TABLE IF NOT EXISTS MoodNames(
                    ID INTEGER NOT NULL UNIQUE,
                    MoodName TEXT NOT NULL,
                    PRIMARY KEY(ID AUTOINCREMENT)
                    )");

                queryExecutor(@"CREATE TABLE IF NOT EXISTS AlternativeMoodNameVariants(
                    ID INTEGER NOT NULL UNIQUE,
                    MoodNameVariant TEXT NOT NULL,
                    IsMissSpelled BOOL NOT NULL,
                    RefersToMoodName INT NOT NULL,
                    PRIMARY KEY(ID AUTOINCREMENT)
                    )");

                queryExecutor(@"CREATE TABLE IF NOT EXISTS ArtistGenres(
                    ArtistId INTEGER NOT NULL,
                    GenreId INTEGER NOT NULL,
                    PRIMARY KEY(ArtistId, GenreId)
                    )");

                queryExecutor(@"CREATE TABLE IF NOT EXISTS ArtistMoods(
                    ArtistId INTEGER NOT NULL,
                    MoodId INTEGER NOT NULL,
                    PRIMARY KEY(ArtistId, MoodId)
                    )");

                queryExecutor(@"CREATE TABLE IF NOT EXISTS ArtistMemberships(
                    GroupArtistId INTEGER NOT NULL,
                    MemberArtistId INTEGER NOT NULL,
                    PRIMARY KEY(GroupArtistId, MemberArtistId),
                    CHECK(GroupArtistId != MemberArtistId)
                    )");

                // Setter "UNIQUE" for "Pattern"-Row is required for the IGNORE statement, otherwise Table will fill up with duplicates
                string SqlCreateFileNamePatterns = @"CREATE TABLE IF NOT EXISTS FileNamePatterns (
                    Id INTEGER PRIMARY KEY,
                    Pattern TEXT NOT NULL UNIQUE,
                    Example TEXT,
                    Confidence INTEGER DEFAULT 1,
                    UserAdded BOOLEAN DEFAULT 1)";
                queryExecutor(SqlCreateFileNamePatterns);

                // Phase 2 (planned): album↔artist mapping for metadata disambiguation. Unused until the resolver is wired.
                string SqlCreateArtistAlbums = @"CREATE TABLE IF NOT EXISTS ArtistAlbums (
                    Id INTEGER PRIMARY KEY,
                    ArtistId INTEGER NOT NULL,
                    AlbumName TEXT NOT NULL,
                    FOREIGN KEY(ArtistId) REFERENCES ArtistNames(ID),
                    UNIQUE(ArtistId, AlbumName))";
                queryExecutor(SqlCreateArtistAlbums);

                // Phase 2 (planned): store user decisions on ambiguous tokens. Unused until the resolver is wired.
                string SqlCreateAmbiguousMetadata = @"CREATE TABLE IF NOT EXISTS AmbiguousMetadata (
                    Id INTEGER PRIMARY KEY,
                    RawValue TEXT NOT NULL,
                    PatternContext TEXT,
                    ResolvedAsType TEXT,
                    Confidence INTEGER DEFAULT 0,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(RawValue, ResolvedAsType))";
                queryExecutor(SqlCreateAmbiguousMetadata);

                string SqlCreateFeatureKeywords = @"CREATE TABLE IF NOT EXISTS FeatureKeywords (
                    Id INTEGER PRIMARY KEY,
                    Keyword TEXT NOT NULL UNIQUE,
                    Type TEXT NOT NULL,
                    Weight INTEGER DEFAULT 1)";
                queryExecutor(SqlCreateFeatureKeywords);

                EnsureTrackIndex();

                queryExecutor(@"CREATE TABLE IF NOT EXISTS BlockedCatalogValues (
                    Id INTEGER PRIMARY KEY,
                    Kind TEXT NOT NULL,
                    Value TEXT NOT NULL COLLATE NOCASE,
                    UNIQUE(Kind, Value))");

                var collaborationValues = string.Join(", ",
                    CollaborationMarkers.DefaultSeeds()
                        .Select(s => $"('{s.Keyword.Replace("'", "''")}', '{s.Type}', {s.Weight})"));
                queryExecutor(
                    $"INSERT OR IGNORE INTO FeatureKeywords (Keyword, Type, Weight) VALUES {collaborationValues}");


                // Pattern-Sammlung mit vereinheitlichten Platzhaltern
                // {artists} - Mehrere Künstler möglich (feat., &, vs., etc.)
                // {comment} - Zusätzliche Info wie (Live), [Remix], etc.
                string initPatterns = @"INSERT OR IGNORE INTO FileNamePatterns (Pattern, Example, Confidence, UserAdded) VALUES 
                    -- Basis-Patterns: Artist(s) - Title
                    ('{artist} - {title}', 'Eminem - Lose Yourself', 90, 0),
                    ('{artists} & {artists} - {title}', 'Simon & Garfunkel - The Sound of Silence', 88, 0),
                    
                    -- Track-basierte Patterns
                    ('{track} - {title}', '01 - Lose Yourself', 85, 0),
                    ('{track} - {artist} - {title}', '01 - Eminem - Lose Yourself', 87, 0),
                    ('{track}. {artist} - {title}', '01. Eminem - Lose Yourself', 86, 0),
                    ('{track} {artist} - {title}', '01 Eminem - Lose Yourself', 84, 0),
                    
                    -- Patterns mit Zusatzinfos (Album, Jahr)
                    ('{artist} - {album} - {title}', 'Pink Floyd - Dark Side - Time', 75, 0),
                    ('({year}) {artist} - {title}', '(2002) Eminem - Lose Yourself', 70, 0),
                    ('{year} - {artist} - {title}', '2002 - Eminem - Lose Yourself', 68, 0),
                    ('{track}. {artist} - {album} - {title}', '01. Pink Floyd - Dark Side - Time', 70, 0),
                    
                    -- Featured/Collaboration Patterns (Künstler im Filename)
                    ('{artist} feat. {artists} - {title}', 'Eminem feat. Rihanna - Love The Way You Lie', 93, 0),
                    ('{artist} feat {artists} - {title}', 'Eminem feat Rihanna - Love The Way You Lie', 93, 0),
                    ('{artist} ft. {artists} - {title}', 'Eminem ft. Rihanna - Love The Way You Lie', 93, 0),
                    ('{artist} ft {artists} - {title}', 'Eminem ft Rihanna - Love The Way You Lie', 93, 0),
                    ('{artist} featuring {artists} - {title}', 'Eminem featuring Rihanna - Love The Way You Lie', 91, 0),
                    ('{artist} vs. {artists} - {title}', 'Eminem vs. Rihanna - Love The Way You Lie', 90, 0),
                    ('{artists} & {artists} feat. {artists} - {title}', 'Simon & Garfunkel feat. Disturbed - Sound Of Silence', 92, 0),
                    
                    -- feat. after the title is another artist, not a comment
                    ('{artist} - {title} (feat. {artists})', 'Eminem - Stan (feat. Dido)', 94, 0),
                    ('{artist} - {title} (ft. {artists})', 'Eminem - Stan (ft. Dido)', 94, 0),
                    ('{artist} - {title} (featuring {artists})', 'Eminem - Stan (featuring Dido)', 93, 0),
                    ('{artist} - {title} (vs. {artists})', 'Eminem - Stan (vs. Dido)', 90, 0),
                    ('{artist} - {title} ({comment})', 'Eminem - Lose Yourself (Demo)', 70, 0),
                    
                    -- Remix/Version/Live-Patterns
                    ('{artist} - {title} (Live)', 'Queen - Bohemian Rhapsody (Live)', 72, 0),
                    ('{artist} - {title} [Live]', 'Queen - Bohemian Rhapsody [Live]', 72, 0),
                    ('{artist} - {title} (Remix)', 'Eminem - Lose Yourself (Remix)', 70, 0),
                    ('{artist} - {title} [Remix]', 'Eminem - Lose Yourself [Remix]', 70, 0),
                    ('{artist} - {title} [{comment}] ({comment})', 'Queen - Bohemian Rhapsody [Extended Remix] (Live)', 68, 0),
                    ('{artist} - {title} ({comment}) [{comment}]', 'Queen - Bohemian Rhapsody (Live) [Remix]', 68, 0),
                    ('{artist} - {title} - {comment}', 'Pink Floyd - Time - Remaster', 60, 0),
                    
                    -- Alternative Ordnungen: Title first
                    ('{title} by {artist}', 'Lose Yourself by Eminem', 60, 0),
                    ('{title} - {artist}', 'Lose Yourself - Eminem', 50, 0),
                    ('{title} ({artist})', 'Lose Yourself (Eminem)', 55, 0);";
                queryExecutor(initPatterns);
                MigrateBuiltInFileNamePatterns();

            }

            ReloadCollaborationMarkers();
        }

        private static void MigrateBuiltInFileNamePatterns() {
            ReplaceBuiltInPattern(
                "{artist} - {title} (feat. {comment})",
                "{artist} - {title} (feat. {artists})",
                "Eminem - Stan (feat. Dido)",
                94);
            ReplaceBuiltInPattern(
                "{artist} - {title} (ft. {comment})",
                "{artist} - {title} (ft. {artists})",
                "Eminem - Stan (ft. Dido)",
                94);
            ReplaceBuiltInPattern(
                "{artist} - {title} (vs. {comment})",
                "{artist} - {title} (vs. {artists})",
                "Eminem - Love The Way You Lie (vs. Rihanna)",
                90);
            queryExecutor(@"INSERT OR IGNORE INTO FileNamePatterns (Pattern, Example, Confidence, UserAdded) VALUES
                ('{artist} - {title} (feat. {artists})', 'Eminem - Stan (feat. Dido)', 94, 0),
                ('{artist} - {title} (ft. {artists})', 'Eminem - Stan (ft. Dido)', 94, 0),
                ('{artist} - {title} (featuring {artists})', 'Eminem - Stan (featuring Dido)', 93, 0)");
            queryExecutor(@"UPDATE FileNamePatterns SET Confidence = CASE WHEN Confidence < 93 THEN 93 ELSE Confidence END
                WHERE UserAdded = 0 AND Pattern IN (
                    '{artist} feat. {artists} - {title}',
                    '{artist} feat {artists} - {title}',
                    '{artist} ft. {artists} - {title}',
                    '{artist} ft {artists} - {title}')");
        }

        private static void ReplaceBuiltInPattern(string from, string to, string example, int minConfidence) {
            var fromSql = from.Replace("'", "''");
            var toSql   = to.Replace("'", "''");
            var exSql   = example.Replace("'", "''");
            queryExecutor($@"DELETE FROM FileNamePatterns
                WHERE Pattern = '{fromSql}'
                  AND EXISTS (SELECT 1 FROM FileNamePatterns WHERE Pattern = '{toSql}')");
            queryExecutor($@"UPDATE FileNamePatterns
                SET Pattern = '{toSql}',
                    Example = '{exSql}',
                    Confidence = CASE WHEN Confidence < {minConfidence} THEN {minConfidence} ELSE Confidence END
                WHERE Pattern = '{fromSql}'");
        }





        public static NewArtistEvent CheckForPreferredName(string pArtistName, [CallerMemberName] string? caller = null) {
            var resolved = ResolveArtistName(pArtistName);
            if (resolved.Status == ArtistNameStatus.AlreadyExisting)
                Debug.WriteLine($"\"{pArtistName}\" is already stored as a preferred artist name.");
            else if (resolved.Status == ArtistNameStatus.IsAlternativeName)
                Debug.WriteLine(caller == "ApplyArtist"
                    ? $"\"{pArtistName}\" is already an alternative artist name; assigning preferred name \"{resolved.PreferredName}\"."
                    : $"\"{pArtistName}\" is already stored as an alternative artist name.");

            return new NewArtistEvent(resolved.PreferredName, "", resolved.Status);
        }



        public static ObservableCollection<DatabaseTable_ArtistNames> LoadTableContent_ArtistNames() {
            var tableContent_ArtistNames = new ObservableCollection<DatabaseTable_ArtistNames>();
            foreach (DataRow r in QueryTable("SELECT * FROM ArtistNames ORDER BY ID ASC").Rows)
                tableContent_ArtistNames.Add(new DatabaseTable_ArtistNames(r));
            return tableContent_ArtistNames;
        }

        public static ObservableCollection<DatabaseTable_AlternativeArtistNameVariants> LoadTableContent_AlternativeArtistNameVariants() {
            var tableContent_AlternativeArtistNameVariants = new ObservableCollection<DatabaseTable_AlternativeArtistNameVariants>();
            foreach (DataRow r in QueryTable("SELECT * FROM AlternativeArtistNameVariants ORDER BY ID ASC").Rows)
                tableContent_AlternativeArtistNameVariants.Add(new DatabaseTable_AlternativeArtistNameVariants(r));
            return tableContent_AlternativeArtistNameVariants;
        }

        private static void queryExecutor(string pSql) {
            lock (DbSync) {
                using(SqliteConnection _connection = new SqliteConnection(ConnectionString)) {
                    _connection.Open();
                    SqliteCommand cmd = new SqliteCommand(pSql, _connection);

                    try {
                        cmd.ExecuteNonQuery();
                    } catch(Exception e) {
                        var a = e.Message;
                        BuranMessageBox.Show(a + " " + e.StackTrace);
                    }
                }
            }
        }








        #region FileNamePatterns Methods

        public static ObservableCollection<FileNamePattern> LoadFileNamePatterns() {
            var patterns = new ObservableCollection<FileNamePattern>();
            foreach (DataRow row in QueryTable("SELECT * FROM FileNamePatterns ORDER BY Confidence DESC").Rows) {
                patterns.Add(new FileNamePattern {
                    Id = Convert.ToInt32(row["Id"]),
                    Pattern = row["Pattern"].ToString() ?? "",
                    Example = row["Example"]?.ToString() ?? "",
                    Confidence = Convert.ToInt32(row["Confidence"]),
                    UserAdded = Convert.ToBoolean(row["UserAdded"])
                });
            }
            return patterns;
        }

        public static void InsertFileNamePattern(string pattern, string? example = null, int confidence = 1, bool userAdded = true) {
            // Pattern schon vorhanden?
            var existing = LoadFileNamePatterns().FirstOrDefault(p => p.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase));

            if(existing != null) {
                // Confidence erhöhen, wenn Pattern erfolgreich war
                IncreasePatternConfidence(existing.Id);
                return;
            }

            string patternStr = $"'{pattern.Replace("'", "''")}'";
            string exampleStr = example != null ? $"'{example.Replace("'", "''")}'" : "NULL";

            queryExecutor($"INSERT INTO FileNamePatterns (Pattern, Example, Confidence, UserAdded) " + $"VALUES ({patternStr}, {exampleStr}, {confidence}, {(userAdded ? 1 : 0)})");
        }

        public static void IncreasePatternConfidence(int patternId, int increment = 5) {
            queryExecutor($"UPDATE FileNamePatterns SET Confidence = Confidence + {increment} WHERE Id = {patternId}");
        }

        #endregion

        #region Helper Classes

        public class FileNamePattern {
            public int Id { get; set; }
            public string Pattern { get; set; } = "";
            public string Example { get; set; } = "";
            public int Confidence { get; set; }
            public bool UserAdded { get; set; }
        }

        #endregion

    }