namespace ObsidianAI.Domain.Services
{
    /// <summary>
    /// Deterministic policy to normalize user input filenames to vault paths.
    /// Implementations should be pure and side-effect free.
    /// </summary>
    public interface IVaultPathNormalizer
    {
        /// <summary>
        /// Normalizes a user-provided filename or path into a canonical vault path.
        /// </summary>
        /// <param name="userInputFileName">Raw filename or path provided by the user or model.</param>
        /// <returns>A normalized vault path string.</returns>
        string Normalize(string userInputFileName);
    }
}