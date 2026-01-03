namespace SecureJournalapp_Paruhang_Khapung.Models
{
    /// <summary>
    /// Represents a journal entry with one entry per day constraint
    /// </summary>
    public class JournalEntry
    {
        public int EntryId { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = ""; // Markdown content stored as plain text
        public DateTime EntryDate { get; set; } // Date of the entry (used for one-per-day constraint)
        public DateTime CreatedAt { get; set; } // System timestamp on creation
        public DateTime UpdatedAt { get; set; } // System timestamp on update
    }
}

