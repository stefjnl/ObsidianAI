namespace ObsidianAI.Application.Services
{
    using ObsidianAI.Domain.Services;

    /// <summary>
    /// Basic implementation of vault path normalization.
    /// </summary>
    public class BasicVaultPathNormalizer : IVaultPathNormalizer
    {
        /// <summary>
        /// Normalizes a user-provided filename or path into a canonical vault path.
        /// Trims input, adds .md extension if missing, preserves inner path separators.
        /// </summary>
        /// <param name="userInputFileName">Raw filename or path provided by the user or model.</param>
        /// <returns>A normalized vault path string.</returns>
        public string Normalize(string userInputFileName)
        {
            if (string.IsNullOrWhiteSpace(userInputFileName))
                return string.Empty;

            var trimmed = userInputFileName.Trim();
            if (trimmed.Length == 0)
                return string.Empty;

            var endsWithSlash = trimmed.EndsWith('/') || trimmed.EndsWith("\\");
            var normalized = trimmed.Replace("\\", "/");
            normalized = normalized.TrimEnd('/');

            if (normalized.Length == 0)
            {
                return string.Empty;
            }

            var lastSegment = normalized.Contains('/')
                ? normalized[(normalized.LastIndexOf('/') + 1)..]
                : normalized;

            if (!endsWithSlash && !Path.HasExtension(lastSegment))
            {
                normalized += ".md";
            }

            return normalized;
        }

        /// <summary>
        /// Generates a comparison key used when matching user input to vault paths.
        /// </summary>
        /// <param name="userInputFileName">Raw filename or path provided by the user or model.</param>
        /// <returns>A normalized comparison key string.</returns>
        public string CreateMatchKey(string userInputFileName)
        {
            return PathNormalizer.NormalizePath(userInputFileName ?? string.Empty);
        }
    }
}