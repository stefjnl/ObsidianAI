using System;
using System.Threading.Tasks;

namespace ObsidianAI.Web.Services;

/// <summary>
/// Interface for vault operations that bypass LLM processing and return raw data.
/// </summary>
public interface IVaultService
{
    /// <summary>
    /// Reads the raw content of a file from the vault without LLM processing.
    /// </summary>
    /// <param name="path">The path to the file to read.</param>
    /// <returns>The raw file content as a string.</returns>
    Task<string> ReadFileAsync(string path);
}
