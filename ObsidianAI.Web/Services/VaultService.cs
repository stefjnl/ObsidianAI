using System.Net.Http.Json;
using ObsidianAI.Web.Models;

namespace ObsidianAI.Web.Services;

/// <summary>
/// Vault service that delegates to REST API endpoints
/// </summary>
public class VaultService : IVaultService
{
    private readonly HttpClient _httpClient;

    public VaultService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<VaultFile>> GetFilesAsync()
    {
        var files = await _httpClient.GetFromJsonAsync<List<VaultFile>>("/vault/files");
        return files ?? new List<VaultFile>();
    }

    public async Task<VaultFile?> GetFileAsync(string path)
    {
        var response = await _httpClient.GetAsync($"/vault/files/{Uri.EscapeDataString(path)}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<VaultFile>();
    }

    public async Task<SearchResult> SearchAsync(string query)
    {
        var response = await _httpClient.PostAsJsonAsync("/vault/search", new { query });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SearchResult>();
        return result ?? new SearchResult(query, new List<VaultFile>());
    }

    public async Task<string> ReadFileAsync(string path)
    {
        var response = await _httpClient.GetAsync($"/vault/read?path={Uri.EscapeDataString(path)}");
        if (!response.IsSuccessStatusCode)
        {
            return string.Empty;
        }
        var result = await response.Content.ReadFromJsonAsync<VaultFileContent>();
        return result?.Content ?? string.Empty;
    }
}
