namespace ObsidianAI.Application.Services
{
    using ObsidianAI.Domain.Services;
    using ObsidianAI.Domain.Models;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Extracts file operations from response text using regex patterns.
    /// </summary>
    public class RegexFileOperationExtractor : IFileOperationExtractor
    {
        /// <summary>
        /// Attempts to extract a <see cref="FileOperation"/> from the provided response text using predefined regex patterns.
        /// </summary>
        /// <param name="responseText">The raw text returned by the model.</param>
        /// <returns>
        /// A <see cref="FileOperation"/> if a valid operation can be inferred; otherwise, <c>null</c>.
        /// </returns>
        public FileOperation? Extract(string responseText)
        {
            if (string.IsNullOrEmpty(responseText))
                return null;

            // Regex patterns to detect file operations
            var patterns = new[]
            {
                // Pattern for file creation: "created file 'path'" or "created the file 'path'"
                new { Regex = @"(?:created|made|established)\s+(?:the\s+)?(?:file|note)\s+['""]?([^'""\n]+)['""]?", Action = "Created" },
                // Pattern for file modification: "modified file 'path'" or "updated the file 'path'"
                new { Regex = @"(?:modified|updated|edited|changed)\s+(?:the\s+)?(?:file|note)\s+['""]?([^'""\n]+)['""]?", Action = "Modified" },
                // Pattern for file appending: "appended to file 'path'" or "added to the file 'path'"
                new { Regex = @"(?:appended|added)\s+(?:to\s+)?(?:the\s+)?(?:file|note)\s+['""]?([^'""\n]+)['""]?", Action = "Appended" },
                // Pattern for file deletion: "deleted file 'path'" or "removed the file 'path'"
                new { Regex = @"(?:deleted|removed|erased)\s+(?:the\s+)?(?:file|note)\s+['""]?([^'""\n]+)['""]?", Action = "Deleted" },
                // Pattern for file moving: "moved file 'path' to 'path'" or "relocated the file 'path'"
                new { Regex = @"(?:moved|relocated|transferred)\s+(?:the\s+)?(?:file|note)\s+['""]?([^'""\n]+)['""]?", Action = "Moved" }
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(responseText, pattern.Regex, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    var filePath = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        return new FileOperation(pattern.Action, filePath);
                    }
                }
            }

            // If no pattern matched, return null
            return null;
        }
    }
}