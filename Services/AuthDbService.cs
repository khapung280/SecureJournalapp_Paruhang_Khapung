using Microsoft.Data.Sqlite;
using SecureJournalapp_Paruhang_Khapung.Models;

namespace SecureJournalapp_Paruhang_Khapung.Services
{
    public class AuthDbService
    {
        private readonly string _dbPath;

        public AuthDbService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "securejournal.db");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // Create User table
            var userTableCmd = connection.CreateCommand();
            userTableCmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS User (
                UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                PinHash TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            ";
            userTableCmd.ExecuteNonQuery();

            // Create JournalEntry table
            // Note: One entry per day is enforced at application level in JournalDbService
            var entryTableCmd = connection.CreateCommand();
            entryTableCmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS JournalEntry (
                EntryId INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                EntryDate TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_entry_date ON JournalEntry(EntryDate);
            ";
            entryTableCmd.ExecuteNonQuery();

            // Create Mood table
            var moodTableCmd = connection.CreateCommand();
            moodTableCmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Mood (
                MoodId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Category TEXT NOT NULL
            );
            ";
            moodTableCmd.ExecuteNonQuery();

            // Create EntryMood junction table
            var entryMoodTableCmd = connection.CreateCommand();
            entryMoodTableCmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS EntryMood (
                EntryMoodId INTEGER PRIMARY KEY AUTOINCREMENT,
                EntryId INTEGER NOT NULL,
                MoodId INTEGER NOT NULL,
                IsPrimary INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (EntryId) REFERENCES JournalEntry(EntryId) ON DELETE CASCADE,
                FOREIGN KEY (MoodId) REFERENCES Mood(MoodId) ON DELETE CASCADE
            );
            ";
            entryMoodTableCmd.ExecuteNonQuery();

            // Create Tag table
            var tagTableCmd = connection.CreateCommand();
            tagTableCmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Tag (
                TagId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                IsPreBuilt INTEGER NOT NULL DEFAULT 0
            );
            ";
            tagTableCmd.ExecuteNonQuery();

            // Create EntryTag junction table
            var entryTagTableCmd = connection.CreateCommand();
            entryTagTableCmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS EntryTag (
                EntryTagId INTEGER PRIMARY KEY AUTOINCREMENT,
                EntryId INTEGER NOT NULL,
                TagId INTEGER NOT NULL,
                FOREIGN KEY (EntryId) REFERENCES JournalEntry(EntryId) ON DELETE CASCADE,
                FOREIGN KEY (TagId) REFERENCES Tag(TagId) ON DELETE CASCADE,
                UNIQUE(EntryId, TagId)
            );
            ";
            entryTagTableCmd.ExecuteNonQuery();

            // Seed initial moods if they don't exist
            SeedMoods(connection);

            // Seed initial tags if they don't exist
            SeedTags(connection);
        }

        /// <summary>
        /// Seeds the database with predefined moods if they don't exist.
        /// </summary>
        private void SeedMoods(SqliteConnection connection)
        {
            var moods = new[]
            {
                // Positive moods
                ("Happy", "Positive"),
                ("Excited", "Positive"),
                ("Relaxed", "Positive"),
                ("Grateful", "Positive"),
                ("Confident", "Positive"),
                // Neutral moods
                ("Calm", "Neutral"),
                ("Thoughtful", "Neutral"),
                ("Curious", "Neutral"),
                ("Nostalgic", "Neutral"),
                ("Bored", "Neutral"),
                // Negative moods
                ("Sad", "Negative"),
                ("Angry", "Negative"),
                ("Stressed", "Negative"),
                ("Lonely", "Negative"),
                ("Anxious", "Negative")
            };

            foreach (var (name, category) in moods)
            {
                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM Mood WHERE Name = $name;";
                checkCmd.Parameters.AddWithValue("$name", name);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                if (!exists)
                {
                    var insertCmd = connection.CreateCommand();
                    insertCmd.CommandText = "INSERT INTO Mood (Name, Category) VALUES ($name, $category);";
                    insertCmd.Parameters.AddWithValue("$name", name);
                    insertCmd.Parameters.AddWithValue("$category", category);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Seeds the database with predefined tags if they don't exist.
        /// </summary>
        private void SeedTags(SqliteConnection connection)
        {
            var preBuiltTags = new[]
            {
                "Work", "Health", "Travel", "Fitness", "Family",
                "Friends", "Hobbies", "Learning", "Goals", "Reflection"
            };

            foreach (var tagName in preBuiltTags)
            {
                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM Tag WHERE Name = $name;";
                checkCmd.Parameters.AddWithValue("$name", tagName);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                if (!exists)
                {
                    var insertCmd = connection.CreateCommand();
                    insertCmd.CommandText = "INSERT INTO Tag (Name, IsPreBuilt) VALUES ($name, 1);";
                    insertCmd.Parameters.AddWithValue("$name", tagName);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        // Save user PIN (only once)
        public async Task SaveUserAsync(User user)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            INSERT INTO User (PinHash, CreatedAt)
            VALUES ($pinHash, $createdAt);
            ";

            command.Parameters.AddWithValue("$pinHash", user.PinHash);
            command.Parameters.AddWithValue("$createdAt", user.CreatedAt.ToString("o"));

            await command.ExecuteNonQueryAsync();
        }
        public async Task ResetDatabaseAsync()
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }

            InitializeDatabase();
            await Task.CompletedTask;
        }
        public async Task DeleteAllAsync()
{
    using var connection = new SqliteConnection($"Data Source={_dbPath}");
    await connection.OpenAsync();

    var command = connection.CreateCommand();
    command.CommandText = "DELETE FROM User;";
    await command.ExecuteNonQueryAsync();
}


        // Get stored user
        public async Task<User?> GetUserAsync()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText =
            @"SELECT UserId, PinHash, CreatedAt FROM User LIMIT 1;";

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    PinHash = reader.GetString(1),
                    CreatedAt = DateTime.Parse(reader.GetString(2))
                };
            }

            return null;
        }
    }
}
