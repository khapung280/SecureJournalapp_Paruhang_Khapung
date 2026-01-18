using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SecureJournalapp_Paruhang_Khapung.Models;
using QuestPDFColors = QuestPDF.Helpers.Colors;

namespace SecureJournalapp_Paruhang_Khapung.Services
{
    /// <summary>
    /// Service for exporting journal entries to PDF format
    /// </summary>
    public class PdfExportService
    {
        private readonly MarkdownService _markdownService;

        public PdfExportService(MarkdownService markdownService)
        {
            _markdownService = markdownService;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// PDF GENERATION LOGIC:
        /// Generates a PDF document containing journal entries within a date range.
        /// 
        /// Algorithm:
        /// 1. Create PDF document structure with title and date range header
        /// 2. For each entry (ordered by date):
        ///    - Add entry date as heading
        ///    - Add entry title (if available)
        ///    - Convert markdown content to plain text and add to PDF
        ///    - Add moods and tags as metadata
        ///    - Add separator between entries
        /// 3. Generate PDF bytes using QuestPDF library
        /// 
        /// Layout:
        /// - Clean, readable format with clear section separators
        /// - Each entry clearly distinguished
        /// - Professional appearance suitable for printing or sharing
        /// </summary>
        public byte[] GeneratePdf(
            List<JournalEntry> entries,
            Dictionary<int, (Mood? Primary, List<Mood> Secondaries)> entryMoods,
            Dictionary<int, List<Tag>> entryTags,
            DateTime startDate,
            DateTime endDate)
        {
            if (entries == null || !entries.Any())
            {
                throw new ArgumentException("No entries provided for PDF generation.");
            }

            // DATE RANGE FILTERING:
            // Filter entries to only include those within the selected date range
            // Entries are already filtered by the calling code, but we ensure ordering here
            var sortedEntries = entries
                .Where(e => e.EntryDate.Date >= startDate.Date && e.EntryDate.Date <= endDate.Date)
                .OrderBy(e => e.EntryDate)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(QuestPDFColors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header()
                        .AlignCenter()
                        .Text("Secure Journal")
                        .FontSize(20)
                        .Bold()
                        .FontColor(QuestPDFColors.Blue.Darken3);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            // Export metadata
                            column.Item()
                                .PaddingBottom((float)0.5, Unit.Centimetre)
                                .Text($"Exported: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}")
                                .FontSize(10)
                                .FontColor(QuestPDFColors.Grey.Medium)
                                .AlignCenter();

                            column.Item()
                                .PaddingBottom(1, Unit.Centimetre)
                                .Text($"Total Entries: {sortedEntries.Count}")
                                .FontSize(10)
                                .FontColor(QuestPDFColors.Grey.Medium)
                                .AlignCenter();

                            column.Item()
                                .PaddingBottom(1, Unit.Centimetre)
                                .LineHorizontal(1)
                                .LineColor(QuestPDFColors.Grey.Lighten1);

                            // JOURNAL ENTRIES:
                            // Iterate through each entry and add to PDF with proper formatting
                            foreach (var entry in sortedEntries)
                            {
                                // Entry Date
                                column.Item()
                                    .PaddingTop((float)0.5, Unit.Centimetre)
                                    .Text(entry.EntryDate.ToString("yyyy-MM-dd"))
                                    .FontSize(14)
                                    .Bold()
                                    .FontColor(QuestPDFColors.Blue.Darken2);

                                // Entry Title
                                if (!string.IsNullOrWhiteSpace(entry.Title))
                                {
                                    column.Item()
                                        .PaddingTop((float)0.2, Unit.Centimetre)
                                        .Text(entry.Title)
                                        .FontSize(12)
                                        .Bold()
                                        .FontColor(QuestPDFColors.Black);
                                }

                                // Entry Content
                                // MARKDOWN TO TEXT CONVERSION:
                                // Convert markdown content to plain text for PDF readability
                                // Remove markdown syntax and preserve basic formatting
                                var plainContent = ConvertMarkdownToPlainText(entry.Content);
                                column.Item()
                                    .PaddingTop((float)0.3, Unit.Centimetre)
                                    .Text(plainContent)
                                    .FontSize(11)
                                    .FontColor(QuestPDFColors.Black)
                                    .LineHeight((float)1.5);

                                // Moods
                                if (entryMoods.TryGetValue(entry.EntryId, out var moods))
                                {
                                    var moodText = new List<string>();
                                    if (moods.Primary != null)
                                    {
                                        moodText.Add($"Primary: {moods.Primary.Name} ({moods.Primary.Category})");
                                    }
                                    if (moods.Secondaries.Any())
                                    {
                                        moodText.Add($"Secondary: {string.Join(", ", moods.Secondaries.Select(m => $"{m.Name} ({m.Category})"))}");
                                    }

                                    if (moodText.Any())
                                    {
                                        column.Item()
                                            .PaddingTop((float)0.2, Unit.Centimetre)
                                            .Text($"Moods: {string.Join(" | ", moodText)}")
                                            .FontSize(9)
                                            .FontColor(QuestPDFColors.Grey.Darken1)
                                            .Italic();
                                    }
                                }

                                // Tags
                                if (entryTags.TryGetValue(entry.EntryId, out var tags) && tags.Any())
                                {
                                    column.Item()
                                        .PaddingTop((float)0.1, Unit.Centimetre)
                                        .Text($"Tags: {string.Join(", ", tags.Select(t => t.Name))}")
                                        .FontSize(9)
                                        .FontColor(QuestPDFColors.Grey.Darken1)
                                        .Italic();
                                }

                                // Entry separator
                                column.Item()
                                    .PaddingVertical((float)0.5, Unit.Centimetre)
                                    .LineHorizontal((float)0.5)
                                    .LineColor(QuestPDFColors.Grey.Lighten2);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .DefaultTextStyle(style => style.FontSize(9f).FontColor(QuestPDFColors.Grey.Medium))
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// MARKDOWN TO PLAIN TEXT CONVERSION:
        /// Converts markdown content to plain text suitable for PDF display.
        /// Removes markdown syntax while preserving basic text structure.
        /// 
        /// Handles:
        /// - Headers (# ## ###) → Convert to plain text
        /// - Bold (**text**) → Remove markdown, keep text
        /// - Italic (*text*) → Remove markdown, keep text
        /// - Lists (- item) → Convert to plain text with line breaks
        /// - Links [text](url) → Show text only
        /// </summary>
        private string ConvertMarkdownToPlainText(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            var text = markdown;

            // Remove markdown headers (# ## ###)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^#{1,6}\s+(.+)$", "$1", System.Text.RegularExpressions.RegexOptions.Multiline);

            // Remove bold (**text** or __text__)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"__(.+?)__", "$1");

            // Remove italic (*text* or _text_)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "$1");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<!_)_(?!_)(.+?)(?<!_)_(?!_)", "$1");

            // Convert links [text](url) to just text
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\[(.+?)\]\(.+?\)", "$1");

            // Convert list items (- item or * item) to plain text with line breaks
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^[\-\*\+]\s+(.+)$", "• $1", System.Text.RegularExpressions.RegexOptions.Multiline);

            // Remove code blocks (```code```)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"```[\s\S]*?```", "");

            // Remove inline code (`code`)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"`(.+?)`", "$1");

            return text.Trim();
        }
    }
}