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

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS User (
                UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                PinHash TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            ";
            command.ExecuteNonQuery();
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
