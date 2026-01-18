namespace SecureJournalapp_Paruhang_Khapung.Models
{
    /// <summary>
    /// Represents a tag (pre-built or custom user-created)
    /// </summary>
    public class Tag
    {
        public int TagId { get; set; }
        public string Name { get; set; } = "";
        public bool IsPreBuilt { get; set; } // true for pre-built tags, false for custom tags
    }

    /// <summary>
    /// Junction table for Entry-Tag many-to-many relationship
    /// </summary>
    public class EntryTag
    {
        public int EntryTagId { get; set; }
        public int EntryId { get; set; }
        public int TagId { get; set; }
    }
}

