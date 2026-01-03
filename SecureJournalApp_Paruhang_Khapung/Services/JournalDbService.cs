using Microsoft.Data.Sqlite;
using SecureJournalapp_Paruhang_Khapung.Models;

namespace SecureJournalapp_Paruhang_Khapung.Services
{
    /// <summary>
    /// Service for managing journal entries, moods, and tags with CRUD operations
    /// </summary>
    public class JournalDbService
    {
        private readonly string _dbPath;

        public JournalDbService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "securejournal.db");
        }

        // ========== JOURNAL ENTRY CRUD ==========

        /// <summary>
        /// Creates a new journal entry. Enforces one entry per day constraint.
        /// </summary>
        public async Task<JournalEntry?> CreateEntryAsync(JournalEntry entry)
        {
            // Check if entry already exists for this date
            var existing = await GetEntryByDateAsync(entry.EntryDate.Date);
            if (existing != null)
            {
                throw new InvalidOperationException($"An entry already exists for {entry.EntryDate.Date:yyyy-MM-dd}. Only one entry per day is allowed.");
            }

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO JournalEntry (Title, Content, EntryDate, CreatedAt, UpdatedAt)
                VALUES ($title, $content, $entryDate, $createdAt, $updatedAt);
                SELECT last_insert_rowid();
            ";

            var now = DateTime.UtcNow;
            command.Parameters.AddWithValue("$title", entry.Title);
            command.Parameters.AddWithValue("$content", entry.Content);
            command.Parameters.AddWithValue("$entryDate", entry.EntryDate.Date.ToString("o"));
            command.Parameters.AddWithValue("$createdAt", now.ToString("o"));
            command.Parameters.AddWithValue("$updatedAt", now.ToString("o"));

            var entryId = Convert.ToInt32(await command.ExecuteScalarAsync());
            entry.EntryId = entryId;
            entry.CreatedAt = now;
            entry.UpdatedAt = now;

            return entry;
        }

        /// <summary>
        /// Updates an existing journal entry. Auto-updates UpdatedAt timestamp.
        /// </summary>
        public async Task<bool> UpdateEntryAsync(JournalEntry entry)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            // Check if date changed and if new date already has an entry
            var existing = await GetEntryByIdAsync(entry.EntryId);
            if (existing == null) return false;

            if (existing.EntryDate.Date != entry.EntryDate.Date)
            {
                var conflict = await GetEntryByDateAsync(entry.EntryDate.Date);
                if (conflict != null && conflict.EntryId != entry.EntryId)
                {
                    throw new InvalidOperationException($"An entry already exists for {entry.EntryDate.Date:yyyy-MM-dd}. Only one entry per day is allowed.");
                }
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE JournalEntry
                SET Title = $title, Content = $content, EntryDate = $entryDate, UpdatedAt = $updatedAt
                WHERE EntryId = $entryId;
            ";

            command.Parameters.AddWithValue("$entryId", entry.EntryId);
            command.Parameters.AddWithValue("$title", entry.Title);
            command.Parameters.AddWithValue("$content", entry.Content);
            command.Parameters.AddWithValue("$entryDate", entry.EntryDate.Date.ToString("o"));
            command.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("o"));

            return await command.ExecuteNonQueryAsync() > 0;
        }

        /// <summary>
        /// Deletes a journal entry and all associated moods and tags.
        /// </summary>
        public async Task<bool> DeleteEntryAsync(int entryId)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();
            var transaction = connection.BeginTransaction();

            try
            {
                // Delete associated moods
                var deleteMoodsCmd = connection.CreateCommand();
                deleteMoodsCmd.CommandText = "DELETE FROM EntryMood WHERE EntryId = $entryId;";
                deleteMoodsCmd.Parameters.AddWithValue("$entryId", entryId);
                await deleteMoodsCmd.ExecuteNonQueryAsync();

                // Delete associated tags
                var deleteTagsCmd = connection.CreateCommand();
                deleteTagsCmd.CommandText = "DELETE FROM EntryTag WHERE EntryId = $entryId;";
                deleteTagsCmd.Parameters.AddWithValue("$entryId", entryId);
                await deleteTagsCmd.ExecuteNonQueryAsync();

                // Delete entry
                var deleteEntryCmd = connection.CreateCommand();
                deleteEntryCmd.CommandText = "DELETE FROM JournalEntry WHERE EntryId = $entryId;";
                deleteEntryCmd.Parameters.AddWithValue("$entryId", entryId);
                var result = await deleteEntryCmd.ExecuteNonQueryAsync();

                transaction.Commit();
                return result > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Gets a journal entry by ID.
        /// </summary>
        public async Task<JournalEntry?> GetEntryByIdAsync(int entryId)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT EntryId, Title, Content, EntryDate, CreatedAt, UpdatedAt
                FROM JournalEntry
                WHERE EntryId = $entryId;
            ";

            command.Parameters.AddWithValue("$entryId", entryId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new JournalEntry
                {
                    EntryId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    EntryDate = DateTime.Parse(reader.GetString(3)),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    UpdatedAt = DateTime.Parse(reader.GetString(5))
                };
            }

            return null;
        }

        /// <summary>
        /// Gets a journal entry by date (for one-per-day constraint).
        /// </summary>
        public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT EntryId, Title, Content, EntryDate, CreatedAt, UpdatedAt
                FROM JournalEntry
                WHERE DATE(EntryDate) = DATE($date);
            ";

            command.Parameters.AddWithValue("$date", date.Date.ToString("o"));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new JournalEntry
                {
                    EntryId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    EntryDate = DateTime.Parse(reader.GetString(3)),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    UpdatedAt = DateTime.Parse(reader.GetString(5))
                };
            }

            return null;
        }

        /// <summary>
        /// Gets all journal entries ordered by EntryDate descending.
        /// </summary>
        public async Task<List<JournalEntry>> GetAllEntriesAsync()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT EntryId, Title, Content, EntryDate, CreatedAt, UpdatedAt
                FROM JournalEntry
                ORDER BY EntryDate DESC;
            ";

            var entries = new List<JournalEntry>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                entries.Add(new JournalEntry
                {
                    EntryId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    EntryDate = DateTime.Parse(reader.GetString(3)),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    UpdatedAt = DateTime.Parse(reader.GetString(5))
                });
            }

            return entries;
        }

        // ========== MOOD MANAGEMENT ==========

        /// <summary>
        /// Sets moods for an entry. Enforces 1 primary mood and max 2 secondary moods.
        /// </summary>
        public async Task SetEntryMoodsAsync(int entryId, int primaryMoodId, List<int>? secondaryMoodIds = null)
        {
            if (secondaryMoodIds == null) secondaryMoodIds = new List<int>();
            if (secondaryMoodIds.Count > 2)
            {
                throw new InvalidOperationException("Maximum 2 secondary moods allowed.");
            }

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();
            var transaction = connection.BeginTransaction();

            try
            {
                // Delete existing moods for this entry
                var deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM EntryMood WHERE EntryId = $entryId;";
                deleteCmd.Parameters.AddWithValue("$entryId", entryId);
                await deleteCmd.ExecuteNonQueryAsync();

                // Insert primary mood
                var insertPrimaryCmd = connection.CreateCommand();
                insertPrimaryCmd.CommandText = @"
                    INSERT INTO EntryMood (EntryId, MoodId, IsPrimary)
                    VALUES ($entryId, $moodId, 1);
                ";
                insertPrimaryCmd.Parameters.AddWithValue("$entryId", entryId);
                insertPrimaryCmd.Parameters.AddWithValue("$moodId", primaryMoodId);
                await insertPrimaryCmd.ExecuteNonQueryAsync();

                // Insert secondary moods
                foreach (var moodId in secondaryMoodIds)
                {
                    var insertSecondaryCmd = connection.CreateCommand();
                    insertSecondaryCmd.CommandText = @"
                        INSERT INTO EntryMood (EntryId, MoodId, IsPrimary)
                        VALUES ($entryId, $moodId, 0);
                    ";
                    insertSecondaryCmd.Parameters.AddWithValue("$entryId", entryId);
                    insertSecondaryCmd.Parameters.AddWithValue("$moodId", moodId);
                    await insertSecondaryCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Gets all moods for an entry.
        /// </summary>
        public async Task<(Mood? Primary, List<Mood> Secondaries)> GetEntryMoodsAsync(int entryId)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT m.MoodId, m.Name, m.Category, em.IsPrimary
                FROM EntryMood em
                INNER JOIN Mood m ON em.MoodId = m.MoodId
                WHERE em.EntryId = $entryId;
            ";

            command.Parameters.AddWithValue("$entryId", entryId);

            Mood? primary = null;
            var secondaries = new List<Mood>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var mood = new Mood
                {
                    MoodId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Category = reader.GetString(2)
                };

                if (reader.GetBoolean(3))
                    primary = mood;
                else
                    secondaries.Add(mood);
            }

            return (primary, secondaries);
        }

        /// <summary>
        /// Gets all available moods.
        /// </summary>
        public async Task<List<Mood>> GetAllMoodsAsync()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT MoodId, Name, Category
                FROM Mood
                ORDER BY Category, Name;
            ";

            var moods = new List<Mood>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                moods.Add(new Mood
                {
                    MoodId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Category = reader.GetString(2)
                });
            }

            return moods;
        }

        // ========== TAG MANAGEMENT ==========

        /// <summary>
        /// Sets tags for an entry. Validates no empty or duplicate tags.
        /// </summary>
        public async Task SetEntryTagsAsync(int entryId, List<int> tagIds)
        {
            // Validate: remove duplicates and empty values
            tagIds = tagIds.Where(id => id > 0).Distinct().ToList();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();
            var transaction = connection.BeginTransaction();

            try
            {
                // Delete existing tags
                var deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM EntryTag WHERE EntryId = $entryId;";
                deleteCmd.Parameters.AddWithValue("$entryId", entryId);
                await deleteCmd.ExecuteNonQueryAsync();

                // Insert new tags
                foreach (var tagId in tagIds)
                {
                    var insertCmd = connection.CreateCommand();
                    insertCmd.CommandText = @"
                        INSERT INTO EntryTag (EntryId, TagId)
                        VALUES ($entryId, $tagId);
                    ";
                    insertCmd.Parameters.AddWithValue("$entryId", entryId);
                    insertCmd.Parameters.AddWithValue("$tagId", tagId);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Gets all tags for an entry.
        /// </summary>
        public async Task<List<Tag>> GetEntryTagsAsync(int entryId)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT t.TagId, t.Name, t.IsPreBuilt
                FROM EntryTag et
                INNER JOIN Tag t ON et.TagId = t.TagId
                WHERE et.EntryId = $entryId
                ORDER BY t.Name;
            ";

            command.Parameters.AddWithValue("$entryId", entryId);

            var tags = new List<Tag>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tags.Add(new Tag
                {
                    TagId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    IsPreBuilt = reader.GetBoolean(2)
                });
            }

            return tags;
        }

        /// <summary>
        /// Gets all available tags.
        /// </summary>
        public async Task<List<Tag>> GetAllTagsAsync()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT TagId, Name, IsPreBuilt
                FROM Tag
                ORDER BY IsPreBuilt DESC, Name;
            ";

            var tags = new List<Tag>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tags.Add(new Tag
                {
                    TagId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    IsPreBuilt = reader.GetBoolean(2)
                });
            }

            return tags;
        }

        /// <summary>
        /// Creates a custom tag.
        /// </summary>
        public async Task<Tag> CreateTagAsync(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                throw new ArgumentException("Tag name cannot be empty.");
            }

            // Check for duplicates
            var existing = await GetAllTagsAsync();
            if (existing.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Tag '{tagName}' already exists.");
            }

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Tag (Name, IsPreBuilt)
                VALUES ($name, 0);
                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("$name", tagName.Trim());

            var tagId = Convert.ToInt32(await command.ExecuteScalarAsync());
            return new Tag { TagId = tagId, Name = tagName.Trim(), IsPreBuilt = false };
        }

        // ========== SEARCH & FILTER ==========

        /// <summary>
        /// Searches and filters journal entries by title, content, date range, moods, and tags.
        /// </summary>
        public async Task<List<JournalEntry>> SearchEntriesAsync(
            string? searchText = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            List<int>? moodIds = null,
            List<int>? tagIds = null)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var query = @"
                SELECT DISTINCT e.EntryId, e.Title, e.Content, e.EntryDate, e.CreatedAt, e.UpdatedAt
                FROM JournalEntry e
            ";

            var conditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            // Search text (title or content)
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                conditions.Add("(e.Title LIKE $searchText OR e.Content LIKE $searchText)");
                parameters["$searchText"] = $"%{searchText}%";
            }

            // Date range
            if (startDate.HasValue)
            {
                conditions.Add("DATE(e.EntryDate) >= DATE($startDate)");
                parameters["$startDate"] = startDate.Value.Date.ToString("o");
            }

            if (endDate.HasValue)
            {
                conditions.Add("DATE(e.EntryDate) <= DATE($endDate)");
                parameters["$endDate"] = endDate.Value.Date.ToString("o");
            }

            // Mood filter
            if (moodIds != null && moodIds.Any())
            {
                query += @"
                    INNER JOIN EntryMood em ON e.EntryId = em.EntryId
                ";
                conditions.Add("em.MoodId IN (" + string.Join(",", moodIds) + ")");
            }

            // Tag filter
            if (tagIds != null && tagIds.Any())
            {
                query += @"
                    INNER JOIN EntryTag et ON e.EntryId = et.EntryId
                ";
                conditions.Add("et.TagId IN (" + string.Join(",", tagIds) + ")");
            }

            if (conditions.Any())
            {
                query += " WHERE " + string.Join(" AND ", conditions);
            }

            query += " ORDER BY e.EntryDate DESC;";

            var command = connection.CreateCommand();
            command.CommandText = query;

            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }

            var entries = new List<JournalEntry>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                entries.Add(new JournalEntry
                {
                    EntryId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    EntryDate = DateTime.Parse(reader.GetString(3)),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    UpdatedAt = DateTime.Parse(reader.GetString(5))
                });
            }

            return entries;
        }
    }
}

