using ObsidianAI.Web.Models;

namespace ObsidianAI.Web.Services;

/// <summary>
/// Service interface for vault operations
/// </summary>
public interface IVaultService
{
    Task<IEnumerable<VaultFile>> GetFilesAsync();
    Task<VaultFile?> GetFileAsync(string path);
    Task<SearchResult> SearchAsync(string query);
    Task<string> ReadFileAsync(string path);
}
