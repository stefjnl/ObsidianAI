using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Services;
using ObsidianAI.Infrastructure.Configuration;
using OpenAI;

namespace ObsidianAI.Infrastructure.LLM;

/// <summary>
/// Reflection service that uses OpenRouter LLM to analyze file operations for safety.
/// </summary>
public class OpenRouterReflectionService : IReflectionService
{
    private readonly IChatClient _chatClient;
    private readonly ReflectionPromptBuilder _promptBuilder;
    private readonly ILogger<OpenRouterReflectionService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Initializes a new instance of the OpenRouterReflectionService.
    /// </summary>
    public OpenRouterReflectionService(
        IOptions<AppSettings> appOptions,
        ReflectionPromptBuilder promptBuilder,
        ILogger<OpenRouterReflectionService> logger)
    {
        var settings = appOptions.Value.LLM.OpenRouter;
        var endpoint = settings.Endpoint?.Trim() ?? "https://openrouter.ai/api/v1";
        var apiKey = settings.ApiKey ?? string.Empty;
        var model = settings.Model ?? "google/gemini-2.5-flash-lite-preview-09-2025";

        var openAIClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        _chatClient = openAIClient.GetChatClient(model).AsIChatClient();
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ReflectionResult> ReflectAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolName);
        ArgumentNullException.ThrowIfNull(arguments);

        try
        {
            // Build reflection prompt
            var prompt = _promptBuilder.BuildPrompt(toolName, arguments);

            // Create chat messages for single-shot LLM call
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, "You are a safety validator for file operations. Always respond with valid JSON only. Do not wrap your response in markdown code fences or any other formatting. Return raw JSON directly."),
                new ChatMessage(ChatRole.User, prompt)
            };

            // Call LLM with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10)); // 10 second timeout

            var response = await _chatClient.GetResponseAsync(messages, options: null, cts.Token).ConfigureAwait(false);
            var responseText = response.Text?.Trim();

            if (string.IsNullOrEmpty(responseText))
            {
                _logger.LogWarning("Reflection LLM returned empty response for {ToolName}", toolName);
                return CreateApprovedResult("LLM returned empty response, operation approved with caution");
            }

            // Strip markdown code fences if present (LLM often wraps JSON in ```json ... ```)
            responseText = StripMarkdownCodeFences(responseText);

            // Try to parse JSON response
            var result = JsonSerializer.Deserialize<ReflectionResult>(responseText, _jsonOptions);

            if (result == null)
            {
                _logger.LogWarning("Failed to parse reflection response for {ToolName}: {Response}", toolName, responseText);
                return CreateApprovedResult("Failed to parse LLM response, operation approved with caution");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(result.Reason))
            {
                result.Reason = "Reflection completed but reason not provided";
            }

            _logger.LogInformation(
                "Reflection completed for {ToolName}: Reject={Reject}, Confirm={Confirm}, Reason={Reason}",
                toolName,
                result.ShouldReject,
                result.NeedsUserConfirmation,
                result.Reason);

            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred
            _logger.LogWarning("Reflection LLM call timed out for {ToolName}", toolName);
            return CreateApprovedResult("Reflection timeout, operation approved with caution");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse reflection JSON response for {ToolName}", toolName);
            return CreateApprovedResult("JSON parsing error, operation approved with caution");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reflection service error for {ToolName}", toolName);
            return CreateApprovedResult("Reflection service error, operation approved with caution");
        }
    }

    private static ReflectionResult CreateApprovedResult(string reason)
    {
        return new ReflectionResult
        {
            ShouldReject = false,
            NeedsUserConfirmation = false,
            Reason = reason,
            ActionDescription = "Operation approved due to reflection service limitation",
            SafetyChecks = new List<string> { "Reflection service operational check" },
            Warnings = new List<string> { "Reflection service encountered an issue" }
        };
    }

    /// <summary>
    /// Strips markdown code fences from the response text.
    /// LLMs often wrap JSON in ```json ... ``` or ``` ... ```.
    /// </summary>
    private static string StripMarkdownCodeFences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var trimmed = text.Trim();

        // Check if wrapped in code fences with language identifier (```json ... ```)
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            var startIndex = trimmed.IndexOf('\n') + 1;
            var endIndex = trimmed.LastIndexOf("```");
            if (endIndex > startIndex)
            {
                return trimmed.Substring(startIndex, endIndex - startIndex).Trim();
            }
        }

        // Check if wrapped in plain code fences (``` ... ```)
        if (trimmed.StartsWith("```") && trimmed.EndsWith("```"))
        {
            var startIndex = trimmed.IndexOf('\n') + 1;
            var endIndex = trimmed.LastIndexOf("```");
            if (endIndex > startIndex)
            {
                return trimmed.Substring(startIndex, endIndex - startIndex).Trim();
            }
        }

        return trimmed;
    }
}