using System.Text.RegularExpressions;

namespace SecureJournalapp_Paruhang_Khapung.Services
{
    /// <summary>
    /// Simple Markdown to HTML converter for rendering journal entries
    /// Supports: Bold, Italics, Headings, Lists, Links
    /// </summary>
    public class MarkdownService
    {
        /// <summary>
        /// Converts Markdown text to HTML for preview rendering
        /// </summary>
        public string ConvertToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return "<p></p>";

            var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var result = new System.Text.StringBuilder();
            var inUnorderedList = false;
            var inOrderedList = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Headings
                if (trimmed.StartsWith("### "))
                {
                    CloseLists(ref inUnorderedList, ref inOrderedList, result);
                    result.AppendLine($"<h3>{trimmed.Substring(4)}</h3>");
                    continue;
                }
                if (trimmed.StartsWith("## "))
                {
                    CloseLists(ref inUnorderedList, ref inOrderedList, result);
                    result.AppendLine($"<h2>{trimmed.Substring(3)}</h2>");
                    continue;
                }
                if (trimmed.StartsWith("# "))
                {
                    CloseLists(ref inUnorderedList, ref inOrderedList, result);
                    result.AppendLine($"<h1>{trimmed.Substring(2)}</h1>");
                    continue;
                }

                // Unordered list items
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    if (!inUnorderedList)
                    {
                        CloseLists(ref inUnorderedList, ref inOrderedList, result);
                        result.AppendLine("<ul>");
                        inUnorderedList = true;
                    }
                    var content = ProcessInlineMarkdown(trimmed.Substring(2));
                    result.AppendLine($"<li>{content}</li>");
                    continue;
                }

                // Ordered list items
                var orderedMatch = Regex.Match(trimmed, @"^\d+\. (.+)$");
                if (orderedMatch.Success)
                {
                    if (!inOrderedList)
                    {
                        CloseLists(ref inUnorderedList, ref inOrderedList, result);
                        result.AppendLine("<ol>");
                        inOrderedList = true;
                    }
                    var content = ProcessInlineMarkdown(orderedMatch.Groups[1].Value);
                    result.AppendLine($"<li>{content}</li>");
                    continue;
                }

                // Empty line
                if (string.IsNullOrEmpty(trimmed))
                {
                    CloseLists(ref inUnorderedList, ref inOrderedList, result);
                    result.AppendLine();
                    continue;
                }

                // Regular paragraph
                CloseLists(ref inUnorderedList, ref inOrderedList, result);
                var processed = ProcessInlineMarkdown(trimmed);
                result.AppendLine($"<p>{processed}</p>");
            }

            CloseLists(ref inUnorderedList, ref inOrderedList, result);

            return result.ToString();
        }

        private void CloseLists(ref bool inUnorderedList, ref bool inOrderedList, System.Text.StringBuilder result)
        {
            if (inUnorderedList)
            {
                result.AppendLine("</ul>");
                inUnorderedList = false;
            }
            if (inOrderedList)
            {
                result.AppendLine("</ol>");
                inOrderedList = false;
            }
        }

        private string ProcessInlineMarkdown(string text)
        {
            // Links [text](url)
            text = Regex.Replace(text, @"\[([^\]]+)\]\(([^\)]+)\)", "<a href=\"$2\" target=\"_blank\">$1</a>");

            // Bold (**text** or __text__)
            text = Regex.Replace(text, @"\*\*(.*?)\*\*", "<strong>$1</strong>");
            text = Regex.Replace(text, @"__(.*?)__", "<strong>$1</strong>");

            // Italics (*text* or _text_) - but not if it's part of bold
            text = Regex.Replace(text, @"(?<!\*)\*(?!\*)([^*]+?)(?<!\*)\*(?!\*)", "<em>$1</em>");
            text = Regex.Replace(text, @"(?<!_)_(?!_)([^_]+?)(?<!_)_(?!_)", "<em>$1</em>");

            return text;
        }
    }
}

