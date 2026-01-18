namespace SecureJournalapp_Paruhang_Khapung.Models
{
    /// <summary>
    /// Represents a mood category (Positive, Neutral, Negative)
    /// </summary>
    public class Mood
    {
        public int MoodId { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = ""; // Positive, Neutral, Negative
    }

    /// <summary>
    /// Junction table for Entry-Mood relationship
    /// Each entry has 1 Primary Mood (IsPrimary = true) and up to 2 Secondary Moods (IsPrimary = false)
    /// </summary>
    public class EntryMood
    {
        public int EntryMoodId { get; set; }
        public int EntryId { get; set; }
        public int MoodId { get; set; }
        public bool IsPrimary { get; set; } // true for primary mood, false for secondary
    }
}

