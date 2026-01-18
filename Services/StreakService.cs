using SecureJournalapp_Paruhang_Khapung.Models;

namespace SecureJournalapp_Paruhang_Khapung.Services
{
    /// <summary>
    /// Service for calculating journal entry streaks and statistics
    /// </summary>
    public class StreakService
    {
        /// <summary>
        /// STREAK CALCULATION LOGIC: Calculate Current Streak
        /// 
        /// Current streak is the number of consecutive days (up to today) where a journal entry exists.
        /// The streak counts backwards from today until a day is found without an entry.
        /// 
        /// Algorithm:
        /// 1. Start from today and count backwards day by day
        /// 2. For each day, check if an entry exists
        /// 3. If entry exists, increment streak count and continue
        /// 4. If entry doesn't exist, stop counting (streak is broken)
        /// 5. Return the count of consecutive days
        /// 
        /// Example:
        /// - Today: Jan 17, entries exist on Jan 17, 16, 15, 14 → Current Streak = 4 days
        /// - If Jan 13 has no entry → Current Streak = 4 days (stops at Jan 14)
        /// </summary>
        public int CalculateCurrentStreak(List<JournalEntry> allEntries)
        {
            if (allEntries == null || !allEntries.Any())
                return 0;

            // Get today's date (ignore time component)
            var today = DateTime.Today;
            var entryDates = allEntries.Select(e => e.EntryDate.Date).ToHashSet();
            
            int streak = 0;
            var currentDate = today;

            // Count backwards from today until we find a day without an entry
            while (entryDates.Contains(currentDate))
            {
                streak++;
                currentDate = currentDate.AddDays(-1);
                
                // Safety check: don't go beyond a reasonable past date
                if (currentDate < today.AddYears(-10))
                    break;
            }

            return streak;
        }

        /// <summary>
        /// STREAK CALCULATION LOGIC: Calculate Longest Streak
        /// 
        /// Longest streak is the maximum number of consecutive days the user has ever journaled.
        /// This is calculated by finding the longest sequence of consecutive dates with entries.
        /// 
        /// Algorithm:
        /// 1. Sort all entry dates in ascending order
        /// 2. Iterate through sorted dates and find consecutive sequences
        /// 3. Track the length of each consecutive sequence
        /// 4. Return the maximum length found
        /// 
        /// Example:
        /// - Entries on: Jan 1, 2, 3, 5, 6, 7, 10, 11, 12, 13
        /// - Consecutive sequences: [1,2,3] = 3 days, [5,6,7] = 3 days, [10,11,12,13] = 4 days
        /// - Longest Streak = 4 days
        /// </summary>
        public int CalculateLongestStreak(List<JournalEntry> allEntries)
        {
            if (allEntries == null || !allEntries.Any())
                return 0;

            // Get unique entry dates and sort in ascending order
            var entryDates = allEntries
                .Select(e => e.EntryDate.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (!entryDates.Any())
                return 0;

            int longestStreak = 1; // At least 1 day if there's an entry
            int currentStreak = 1;

            // Iterate through sorted dates to find consecutive sequences
            for (int i = 1; i < entryDates.Count; i++)
            {
                var previousDate = entryDates[i - 1];
                var currentDate = entryDates[i];
                
                // Check if dates are consecutive (difference of exactly 1 day)
                if ((currentDate - previousDate).TotalDays == 1)
                {
                    // Dates are consecutive - increment current streak
                    currentStreak++;
                }
                else
                {
                    // Gap found - reset current streak and update longest if needed
                    if (currentStreak > longestStreak)
                    {
                        longestStreak = currentStreak;
                    }
                    currentStreak = 1; // Reset for new sequence
                }
            }

            // Check final streak (in case longest streak ends at the last entry)
            if (currentStreak > longestStreak)
            {
                longestStreak = currentStreak;
            }

            return longestStreak;
        }

        /// <summary>
        /// STREAK CALCULATION LOGIC: Calculate Missed Days
        /// 
        /// Missed days are dates between the first journal entry and today where no entry exists.
        /// Only counts past and present dates (ignores future dates).
        /// 
        /// Algorithm:
        /// 1. Find the first entry date (earliest date with an entry)
        /// 2. Create a date range from first entry date to today
        /// 3. For each date in the range, check if an entry exists
        /// 4. Count dates without entries (missed days)
        /// 
        /// Example:
        /// - First entry: Jan 1, 2026
        /// - Today: Jan 17, 2026
        /// - Entries exist on: Jan 1, 2, 3, 5, 7
        /// - Missed days: Jan 4, 6, 8-17 = 11 missed days
        /// </summary>
        public int CalculateMissedDays(List<JournalEntry> allEntries)
        {
            if (allEntries == null || !allEntries.Any())
                return 0;

            var today = DateTime.Today;
            var entryDates = allEntries.Select(e => e.EntryDate.Date).ToHashSet();

            // Find first entry date (earliest entry)
            var firstEntryDate = allEntries.Min(e => e.EntryDate.Date);

            // Only count missed days from first entry to today (ignore future dates)
            var endDate = today < firstEntryDate ? firstEntryDate : today;

            int missedDays = 0;
            var currentDate = firstEntryDate;

            // Count all dates between first entry and today that don't have entries
            while (currentDate <= endDate)
            {
                if (!entryDates.Contains(currentDate))
                {
                    missedDays++;
                }
                currentDate = currentDate.AddDays(1);
            }

            return missedDays;
        }

        /// <summary>
        /// Get all missed dates (list of dates without entries)
        /// Useful for displaying specific missed days to the user
        /// </summary>
        public List<DateTime> GetMissedDates(List<JournalEntry> allEntries)
        {
            if (allEntries == null || !allEntries.Any())
                return new List<DateTime>();

            var today = DateTime.Today;
            var entryDates = allEntries.Select(e => e.EntryDate.Date).ToHashSet();
            var firstEntryDate = allEntries.Min(e => e.EntryDate.Date);
            var endDate = today < firstEntryDate ? firstEntryDate : today;

            var missedDates = new List<DateTime>();
            var currentDate = firstEntryDate;

            while (currentDate <= endDate)
            {
                if (!entryDates.Contains(currentDate))
                {
                    missedDates.Add(currentDate);
                }
                currentDate = currentDate.AddDays(1);
            }

            return missedDates;
        }
    }
}
