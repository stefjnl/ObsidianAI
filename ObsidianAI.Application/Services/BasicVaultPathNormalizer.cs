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

            // If file name lacks extension, add ".md"
            if (!Path.HasExtension(trimmed))
            {
                trimmed += ".md";
            }

            return trimmed;
        }
    }
}