using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ObsidianAI.Application.Services;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case for reading raw file content from the vault without LLM processing.
/// </summary>
public sealed class ReadFileUseCase
{
    private readonly IMcpClientProvider _mcpClientProvider;
    private readonly ILogger<ReadFileUseCase> _logger;

    public ReadFileUseCase(
        IMcpClientProvider mcpClientProvider,
        ILogger<ReadFileUseCase> logger)
    {
        _mcpClientProvider = mcpClientProvider ?? throw new ArgumentNullException(nameof(mcpClientProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reads the raw content of a file from the vault.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw file content as a string.</returns>
    public async Task<string> ExecuteAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        _logger.LogInformation("Reading file from vault: {FilePath}", filePath);

        try
        {
            var mcpClient = await _mcpClientProvider.GetClientAsync("obsidian", ct).ConfigureAwait(false);
            if (mcpClient == null)
            {
                _logger.LogWarning("MCP client is not available. Cannot read file.");
                throw new InvalidOperationException("MCP server is not available. Vault operations are disabled.");
            }

            // Call the MCP tool to read file content
            var args = new Dictionary<string, object?>
            {
                ["filepath"] = filePath
            };

            var result = await mcpClient.CallToolAsync("obsidian_get_file_contents", args, cancellationToken: ct).ConfigureAwait(false);

            if (result.IsError ?? false)
            {
                var errorMessage = ExtractErrorMessage(result.Content);
                _logger.LogWarning("MCP returned error when reading file {FilePath}: {Error}", filePath, errorMessage);
                throw new InvalidOperationException($"Failed to read file: {errorMessage}");
            }

            // Extract text from the response
            var content = ExtractTextContent(result.Content);
            
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("MCP returned empty content for file: {FilePath}", filePath);
                return string.Empty;
            }

            _logger.LogInformation("Successfully read {Length} characters from file: {FilePath}", content.Length, filePath);
            return content;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("File read operation was cancelled for: {FilePath}", filePath);
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error reading file from vault: {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to read file '{filePath}': {ex.Message}", ex);
        }
    }

    private static string ExtractTextContent(IEnumerable<ContentBlock>? content)
    {
        if (content == null)
        {
            return string.Empty;
        }

        var textBlock = content.OfType<TextContentBlock>().FirstOrDefault();
        return textBlock?.Text?.Trim() ?? string.Empty;
    }

    private static string ExtractErrorMessage(IEnumerable<ContentBlock>? content)
    {
        if (content == null)
        {
            return "Unknown error occurred";
        }

        var textBlock = content.OfType<TextContentBlock>().FirstOrDefault();
        return textBlock?.Text?.Trim() ?? "Unknown error occurred";
    }
}
