using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Api.Configuration;

/// <summary>
/// Endpoints for ActionCard confirmation and execution.
/// </summary>
public static class ActionCardEndpoints
{
    /// <summary>
    /// Maps ActionCard-related endpoints.
    /// </summary>
    public static void MapActionCardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/actioncards/{reflectionKey}/confirm", ConfirmActionCardAsync)
            .WithName("ConfirmActionCard")
            .WithTags("ActionCards")
            .Produces<ActionCardConfirmationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        app.MapPost("/actioncards/{reflectionKey}/cancel", CancelActionCardAsync)
            .WithName("CancelActionCard")
            .WithTags("ActionCards")
            .Produces<ActionCardCancellationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Confirms and executes a pending ActionCard operation.
    /// </summary>
    private static async Task<IResult> ConfirmActionCardAsync(
        string reflectionKey,
        IAgentStateService stateService,
        Application.Services.IMcpClientProvider mcpClientProvider,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            logger.LogInformation("ActionCard confirmation requested for key: {ReflectionKey}", reflectionKey);

            // Retrieve stored operation context
            var storedContext = stateService.Get<object>(reflectionKey);
            if (storedContext == null)
            {
                logger.LogWarning("ActionCard not found for reflection key: {ReflectionKey}", reflectionKey);
                return Results.NotFound(new ActionCardConfirmationResponse
                {
                    Success = false,
                    Message = "Action card not found or already executed"
                });
            }

            // Extract function name and arguments from stored context
            var contextJson = JsonSerializer.Serialize(storedContext);
            var contextDoc = JsonDocument.Parse(contextJson);
            var root = contextDoc.RootElement;

            if (!root.TryGetProperty("FunctionName", out var funcNameElement) ||
                !root.TryGetProperty("Arguments", out var argsElement))
            {
                logger.LogError("Invalid stored context format for reflection key: {ReflectionKey}", reflectionKey);
                return Results.Problem("Invalid stored operation context");
            }

            var functionName = funcNameElement.GetString() ?? throw new InvalidOperationException("FunctionName is null");
            var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsElement.GetRawText()) 
                ?? new Dictionary<string, object?>();

            logger.LogInformation("Executing confirmed operation: {FunctionName} with arguments: {Arguments}", 
                functionName, JsonSerializer.Serialize(arguments));

            // Get MCP client and execute the tool
            var mcpClient = await mcpClientProvider.GetClientAsync(ct);
            if (mcpClient == null)
            {
                logger.LogError("MCP client not available for ActionCard execution");
                return Results.Problem("MCP client not available");
            }

            // Call the tool through MCP
            var result = await mcpClient.CallToolAsync(functionName, arguments, cancellationToken: ct);

            // Remove the stored context after successful execution
            stateService.Clear(reflectionKey);

            logger.LogInformation("ActionCard operation completed successfully: {FunctionName}", functionName);

            return Results.Ok(new ActionCardConfirmationResponse
            {
                Success = true,
                Message = result?.ToString() ?? "Operation completed successfully",
                FunctionName = functionName
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute ActionCard confirmation for key: {ReflectionKey}", reflectionKey);
            return Results.Problem($"Failed to execute operation: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels a pending ActionCard operation.
    /// </summary>
    private static Task<IResult> CancelActionCardAsync(
        string reflectionKey,
        IAgentStateService stateService,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("ActionCard cancellation requested for key: {ReflectionKey}", reflectionKey);

            // Retrieve stored operation context
            var storedContext = stateService.Get<object>(reflectionKey);
            if (storedContext == null)
            {
                logger.LogWarning("ActionCard not found for reflection key: {ReflectionKey}", reflectionKey);
                return Task.FromResult(Results.NotFound(new ActionCardCancellationResponse
                {
                    Success = false,
                    Message = "Action card not found or already processed"
                }) as IResult);
            }

            // Extract function name from stored context for logging
            var contextJson = JsonSerializer.Serialize(storedContext);
            var contextDoc = JsonDocument.Parse(contextJson);
            var root = contextDoc.RootElement;
            var functionName = root.TryGetProperty("FunctionName", out var funcNameElement) 
                ? funcNameElement.GetString() 
                : "unknown";

            // Remove the stored context
            stateService.Clear(reflectionKey);

            logger.LogInformation("ActionCard operation cancelled: {FunctionName}", functionName);

            return Task.FromResult(Results.Ok(new ActionCardCancellationResponse
            {
                Success = true,
                Message = $"Operation '{functionName}' cancelled successfully",
                FunctionName = functionName
            }) as IResult);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel ActionCard for key: {ReflectionKey}", reflectionKey);
            return Task.FromResult(Results.Problem($"Failed to cancel operation: {ex.Message}") as IResult);
        }
    }
}

/// <summary>
/// Response model for ActionCard confirmation.
/// </summary>
public record ActionCardConfirmationResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? FunctionName { get; init; }
}

/// <summary>
/// Response model for ActionCard cancellation.
/// </summary>
public record ActionCardCancellationResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? FunctionName { get; init; }
}
