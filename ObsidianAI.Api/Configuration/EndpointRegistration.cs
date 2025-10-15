using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObsidianAI.Api.Models;
using ObsidianAI.Api.Streaming;
using ObsidianAI.Application.UseCases;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using System;
using System.Linq;
using System.Threading;

namespace ObsidianAI.Api.Configuration;

/// <summary>
/// Endpoint registration helpers for the ObsidianAI API application.
/// </summary>
public static class EndpointRegistration
{
    /// <summary>
    /// Maps all API endpoints exposed by the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapObsidianEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/llm/provider", (IOptions<AppSettings> appSettings) =>
        {
            var provider = appSettings.Value.LLM.Provider?.Trim() ?? "LMStudio";
            return Results.Ok(new { provider });
        });

        app.MapPost("/chat", async (ChatRequest request, StartChatUseCase useCase, CancellationToken cancellationToken) =>
        {
            var instructions = AgentInstructions.ObsidianAssistant;

            var history = request.History?.Select(h => new ConversationMessage(
                h.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? ParticipantRole.User : ParticipantRole.Assistant,
                h.Content)).ToList();

            var input = new ChatInput(request.Message, history);
            var result = await useCase.ExecuteAsync(input, instructions, cancellationToken).ConfigureAwait(false);

            return Results.Ok(new
            {
                text = result.Text,
                fileOperationResult = result.FileOperation == null ? null : new FileOperationData(result.FileOperation.Action, result.FileOperation.FilePath)
            });
        });

        app.MapPost("/chat/stream", async (ChatRequest request, HttpContext context, StreamChatUseCase useCase, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ChatStream");
            logger.LogInformation("Starting SSE stream for message: {Message}", request.Message);

            var instructions = AgentInstructions.ObsidianAssistant;

            var history = request.History?.Select(h => new ConversationMessage(
                h.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? ParticipantRole.User : ParticipantRole.Assistant,
                h.Content)).ToList();

            var input = new ChatInput(request.Message, history);
            var stream = useCase.ExecuteAsync(input, instructions, context.RequestAborted);

            await StreamingEventWriter.WriteAsync(context, stream, logger, context.RequestAborted).ConfigureAwait(false);
        });

        app.MapPost("/vault/search", async (SearchRequest request, SearchVaultUseCase useCase, CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(request.Query, cancellationToken).ConfigureAwait(false);
            var apiResults = result.Results.Select(r => new SearchResult(r.Path, (float)r.Score, r.Preview)).ToList();
            return Results.Ok(new SearchResponse(apiResults));
        });

        app.MapPost("/vault/reorganize", (ReorganizeRequest request) => Results.Ok(new ReorganizeResponse("Completed", 10)));

        app.MapPost("/vault/modify", async (ModifyRequest request, ModifyVaultUseCase useCase, CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(request.Operation, request.FilePath, request.Content, cancellationToken).ConfigureAwait(false);
            return Results.Ok(new ModifyResponse(result.Success, result.Message, result.FilePath));
        });
    }
}
