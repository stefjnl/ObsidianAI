using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ObsidianAI.Web.Services;

/// <summary>
/// Implementation of vault service that handles direct file operations without LLM processing.
/// </summary>
public class VaultService : IVaultService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VaultService> _logger;

    public VaultService(HttpClient httpClient, ILogger<VaultService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(path));
        }

        try
        {
            _logger.LogInformation("Reading file from vault: {Path}", path);
            
            var encodedPath = Uri.EscapeDataString(path);
            var response = await _httpClient.GetAsync($"/vault/read?path={encodedPath}");
            
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ReadFileResponse>();
            
            if (result == null)
            {
                _logger.LogWarning("Received null response when reading file: {Path}", path);
                return string.Empty;
            }

            _logger.LogInformation("Successfully read {Length} characters from file: {Path}", 
                result.Content?.Length ?? 0, path);
            
            return result.Content ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error reading file from vault: {Path}", path);
            throw new InvalidOperationException($"Failed to read file '{path}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file from vault: {Path}", path);
            throw;
        }
    }
}

/// <summary>
/// Internal response model for file read operations.
/// </summary>
internal record ReadFileResponse
{
    public string Path { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}
