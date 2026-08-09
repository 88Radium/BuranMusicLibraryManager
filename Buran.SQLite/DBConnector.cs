using System.Collections.ObjectModel;
using System.Data;
using System.Runtime.CompilerServices;
using Buran.Types;
using Microsoft.Data.Sqlite;

namespace Buran.SQLite;

public class DBConnector {
        public enum IWantTo { getArtistNames, getAlternativeArtistNames, insertArtistName, insertAlternativeArtistName };

        public static void TestConnection() {

            EventPublisher.ArtistDiscovered += OnArtistDiscovered;

            using(SqliteConnection _connection = new SqliteConnection("Data Source=CerberusMusicManager.db")) {
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


                // Setter "UNIQUE" for "Pattern"-Row is required for the IGNORE statement, otherwise Table will fill up with duplicates
                string SqlCreateFileNamePatterns = @"CREATE TABLE IF NOT EXISTS FileNamePatterns (
                    Id INTEGER PRIMARY KEY,
                    Pattern TEXT NOT NULL UNIQUE,
                    Example TEXT,
                    Confidence INTEGER DEFAULT 1,
                    UserAdded BOOLEAN DEFAULT 1)";
                queryExecutor(SqlCreateFileNamePatterns);


                // Umfangreiche Pattern-Sammlung für MP3-Dateinamen
                // TODO: Für zukünftige Versionen mehrere {artist}-Platzhalter (über 2) unterstützen
                string initPatterns = @"INSERT OR IGNORE INTO FileNamePatterns (Pattern, Example, Confidence, UserAdded) VALUES 
                    -- Basis-Patterns: Artist - Title
                    ('{artists} - {title}', 'Eminem & Rihanna - Love The Way You Lie', 92, 0),
                    ('{artist} - {title}', 'Eminem - Lose Yourself', 90, 0),
                    ('{artist1} & {artist2} - {title}', 'Simon & Garfunkel - The Sound of Silence', 88, 0),
                    
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
                    
                    -- Featured/Collaboration Patterns (im Filename)
                    ('{artist} feat. {featured} - {title}', 'Eminem feat. Rihanna - Love The Way You Lie', 85, 0),
                    ('{artist} ft. {featured} - {title}', 'Eminem ft. Rihanna - Love The Way You Lie', 85, 0),
                    ('{artist} featuring {featured} - {title}', 'Eminem featuring Rihanna - Love The Way You Lie', 83, 0),
                    ('{artist} vs. {featured} - {title}', 'Eminem vs. Rihanna - Love The Way You Lie', 80, 0),
                    
                    -- Title mit Featured/Zusatzinfos in Klammern (Comment-Bereich)
                    ('{artist} - {title} (feat. {comment})', 'Eminem - Love The Way You Lie (feat. Rihanna)', 82, 0),
                    ('{artist} - {title} (ft. {comment})', 'Eminem - Love The Way You Lie (ft. Rihanna)', 82, 0),
                    ('{artist} - {title} (featuring {comment})', 'Eminem - Love The Way You Lie (featuring Rihanna)', 81, 0),
                    ('{artist} - {title} (vs. {comment})', 'Eminem - Love The Way You Lie (vs. Rihanna)', 78, 0),
                    ('{artist} - {title} ({comment})', 'Eminem - Love The Way You Lie (Rihanna)', 75, 0),
                    
                    -- Remix/Version/Live-Patterns
                    ('{artist} - {title} (Live)', 'Queen - Bohemian Rhapsody (Live)', 72, 0),
                    ('{artist} - {title} [Live]', 'Queen - Bohemian Rhapsody [Live]', 72, 0),
                    ('{artist} - {title} (Remix)', 'Eminem - Lose Yourself (Remix)', 70, 0),
                    ('{artist} - {title} [Remix]', 'Eminem - Lose Yourself [Remix]', 70, 0),
                    ('{artist} - {title} ({comment})', 'Pink Floyd - Time (Remaster)', 65, 0),
                    ('{artist} - {title} [{comment}]', 'Eminem - Lose Yourself [Extended]', 65, 0),
                    ('{artist} - {title} - {comment}', 'Pink Floyd - Time - Remaster', 60, 0),
                    
                    -- Alternative Ordnungen: Title first
                    ('{title} by {artist}', 'Lose Yourself by Eminem', 60, 0),
                    ('{title} - {artist}', 'Lose Yourself - Eminem', 50, 0),
                    ('{title} ({artist})', 'Lose Yourself (Eminem)', 55, 0);";
                queryExecutor(initPatterns);

                // Keep the Format like "'content'"! The inner single-quotas are neccessary for the Database-query. It's possible to rewrite the SQLite-Code to the point where this isn't neccessary anymore but that's something for another time!

                //InsertInto_ArtistNames("'Tom MacDonald'", "'Thomas MacDonald'");
                //InsertInto_ArtistNames("'Nova Rockafeller'", "'Nova Leigh Paholek'");
                //InsertInto_ArtistNames("'2Pac'", "'Tupac Amaru Shakur'");
                //InsertInto_ArtistNames("'Eminem'", "'Marshal Matters'");
                //InsertInto_ArtistNames("", "'Moshtekk'");


                //var TableArtistNames = LoadTableContent_ArtistNames();
                //InsertInto_AlternativeArtistNameVariants("'Mushitekk'", (TableArtistNames.Where(x => x.PreferredArtistName == "Moshtekk").First() as DatabaseTable_ArtistNames).ID);


            }
        }

        private static void OnArtistDiscovered(object sender, NewArtistEvent e) {
            Console.WriteLine($"DB-Plugin empfängt: {e.PreferredArtistName}");
            InsertInto_ArtistNames(e.PreferredArtistName, "");
        }

        public void Unsubscribe() {
            EventPublisher.ArtistDiscovered -= OnArtistDiscovered;
        }


        #region Inserts
        public static void InsertInto_ArtistNames(string pArtistName, string pRealName) {
            //TODO: Hier aufgehört zu gucken, ob der neue Typ denn überhaupt den Anforderungen entspricht. Zu besoffen hierfür. Mache morgen weiter!
            NewArtistEvent incommingName = CheckForPreferredName(pArtistName);

            if(incommingName.ArtistNameStatus.Equals(ArtistNameStatus.IsNonExistent)) {
                string ArtistNameString = "'" + incommingName.PreferredArtistName + "'";
                string realNameString = "'" + pRealName + "'";
                queryExecutor($"INSERT INTO ArtistNames (PreferredArtistName, RealName) VALUES ({ArtistNameString}, {realNameString})");
            }
        }

        public static void InsertInto_AlternativeArtistNameVariants(string pAlternativeArtistName, int pRefersToArtistName) {
            queryExecutor($"INSERT INTO AlternativeArtistNameVariants (AlternativeArtistName, RefersToArtistName, IsMissSpelled) VALUES ({pAlternativeArtistName}, {pRefersToArtistName}, {0})");
        }
        #endregion





        public static NewArtistEvent CheckForPreferredName(string pArtistName, [CallerMemberName] string caller = null) {

            ObservableCollection<DatabaseTable_ArtistNames> NameTableEntries = DBConnector.LoadTableContent_ArtistNames();
            ObservableCollection<DatabaseTable_AlternativeArtistNameVariants> alternativeNameTableEntries = DBConnector.LoadTableContent_AlternativeArtistNameVariants();


            DatabaseTable_ArtistNames isAlreadyExisting = NameTableEntries.FirstOrDefault(x => x.PreferredArtistName.ToLower() == pArtistName.ToLower());
            DatabaseTable_AlternativeArtistNameVariants isAlternativeName = alternativeNameTableEntries.FirstOrDefault(x => x.AlternativeArtistName.ToLower() == pArtistName.ToLower());


            if(isAlreadyExisting != null) {
                Console.WriteLine($"Es ist bereits ein Künstler Namens \"{pArtistName}\" in der Datenbank als bevorzugner Name eines Künstlers vorhanden!");
                return new NewArtistEvent(isAlreadyExisting.PreferredArtistName, "", ArtistNameStatus.AlreadyExisting);
            } else if(isAlternativeName != null) {
                isAlreadyExisting = NameTableEntries.FirstOrDefault(x => x.ID == isAlternativeName.RefersToArtistName);

                if(caller == "ApplyArtist") {
                    Console.WriteLine($"Der Künstlername \"{pArtistName}\" ist bereits als alternativer Künstlername in der Datenbank vorhanden! Wir weisen den mp3-Dateien daher den bevorzugten Künstlernamen \"{isAlreadyExisting.PreferredArtistName}\" zu.");
                } else {
                    Console.WriteLine($"Der Künstlername \"{pArtistName}\" ist bereits als alternativer Künstlername in der Datenbank vorhanden!");
                }
                return new NewArtistEvent(isAlreadyExisting.PreferredArtistName, "", ArtistNameStatus.IsAlternativeName);
            } else {
                return new NewArtistEvent(pArtistName, "", ArtistNameStatus.IsNonExistent);
            }


        }



        public static void DeleteFrom_ArtistNames(DatabaseTable_ArtistNames ArtistToDelete) {
            queryExecutor($"DELETE FROM ArtistNames WHERE ID = {ArtistToDelete.ID}");
        }

        public static ObservableCollection<DatabaseTable_ArtistNames> LoadTableContent_ArtistNames() {
            ObservableCollection<DatabaseTable_ArtistNames> tableContent_ArtistNames = new ObservableCollection<DatabaseTable_ArtistNames>();

            using(SqliteConnection _connection = new SqliteConnection("Data Source=CerberusMusicManager.db")) {
                string query = @"SELECT * FROM ArtistNames ORDER BY ID ASC";
                DataTable dataTable = new DataTable();
                SqliteCommand command = new SqliteCommand(query, _connection);

                _connection.Open();

                //queryExecutor(@"INSERT INTO ArtistName ('PreferredArtistName', 'RealName') VALUES ('Klaus Doldinger', 'Klaus Doldinger');");


                SqliteDataReader dataReader = command.ExecuteReader();
                dataTable.Load(dataReader);


                foreach(DataRow r in dataTable.Rows) {
                    var a = new DatabaseTable_ArtistNames(r);
                    tableContent_ArtistNames.Add(a);
                }
                ;
            }
            return tableContent_ArtistNames;
        }

        public static ObservableCollection<DatabaseTable_AlternativeArtistNameVariants> LoadTableContent_AlternativeArtistNameVariants() {
            ObservableCollection<DatabaseTable_AlternativeArtistNameVariants> tableContent_AlternativeArtistNameVariants = new ObservableCollection<DatabaseTable_AlternativeArtistNameVariants>();

            using(SqliteConnection _connection = new SqliteConnection("Data Source=CerberusMusicManager.db")) {
                string query = @"SELECT * FROM AlternativeArtistNameVariants ORDER BY ID ASC";
                DataTable dataTable = new DataTable();
                SqliteCommand command = new SqliteCommand(query, _connection);

                _connection.Open();

                //queryExecutor(@"INSERT INTO ArtistName ('PreferredArtistName', 'RealName') VALUES ('Klaus Doldinger', 'Klaus Doldinger');");


                SqliteDataReader dataReader = command.ExecuteReader();
                dataTable.Load(dataReader);

                foreach(DataRow r in dataTable.Rows) {
                    var a = new DatabaseTable_AlternativeArtistNameVariants(r);
                    tableContent_AlternativeArtistNameVariants.Add(a);
                };
            }
            return tableContent_AlternativeArtistNameVariants;
        }

        public static void FilterTableContent_AlternativeArtistNameVariants() {

        }

        private static void queryExecutor(string pSql) {
            using(SqliteConnection _connection = new SqliteConnection("Data Source=CerberusMusicManager.db")) {
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








        #region FileNamePatterns Methods

        public static ObservableCollection<FileNamePattern> LoadFileNamePatterns() {
            var patterns = new ObservableCollection<FileNamePattern>();

            using(SqliteConnection _connection = new SqliteConnection("Data Source=CerberusMusicManager.db")) {
                string query = @"SELECT * FROM FileNamePatterns ORDER BY Confidence DESC";
                DataTable dataTable = new DataTable();
                SqliteCommand command = new SqliteCommand(query, _connection);

                _connection.Open();
                SqliteDataReader dataReader = command.ExecuteReader();
                dataTable.Load(dataReader);

                foreach(DataRow row in dataTable.Rows) {
                    patterns.Add(new FileNamePattern {
                        Id = Convert.ToInt32(row["Id"]),
                        Pattern = row["Pattern"].ToString(),
                        Example = row["Example"]?.ToString(),
                        Confidence = Convert.ToInt32(row["Confidence"]),
                        UserAdded = Convert.ToBoolean(row["UserAdded"])
                    });
                }
            }

            return patterns;
        }

        public static void InsertFileNamePattern(string pattern, string example = null, int confidence = 1, bool userAdded = true) {
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

        public static void DecreasePatternConfidence(int patternId, int decrement = 1) {
            queryExecutor($"UPDATE FileNamePatterns SET Confidence = Confidence - {decrement} WHERE Id = {patternId}");
        }

        #endregion

        #region Helper Classes

        public class FileNamePattern {
            public int Id { get; set; }
            public string Pattern { get; set; }
            public string Example { get; set; }
            public int Confidence { get; set; }
            public bool UserAdded { get; set; }
        }

        #endregion

    }