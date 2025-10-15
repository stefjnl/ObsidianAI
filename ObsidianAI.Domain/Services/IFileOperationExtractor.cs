namespace ObsidianAI.Domain.Services
{
    using ObsidianAI.Domain.Models;

    /// <summary>
    /// Extracts structured file operation (action + path) from model text.
    /// Implementations should parse free-form responses and return a <see cref="FileOperation"/> when possible.
    /// </summary>
    public interface IFileOperationExtractor
    {
        /// <summary>
        /// Attempts to extract a <see cref="FileOperation"/> from the provided response text.
        /// </summary>
        /// <param name="responseText">The raw text returned by the model.</param>
        /// <returns>
        /// A <see cref="FileOperation"/> if a valid operation can be inferred; otherwise, <c>null</c>.
        /// </returns>
        FileOperation? Extract(string responseText);
    }
}