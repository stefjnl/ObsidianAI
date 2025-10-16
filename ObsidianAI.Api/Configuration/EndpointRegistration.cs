using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObsidianAI.Api.Models;
using ObsidianAI.Api.Streaming;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Application.UseCases;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.LLM;

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

        app.MapGet("/conversations", async (
            int? skip,
            int? take,
            ListConversationsUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var conversations = await useCase.ExecuteAsync(
                userId: null,
                includeArchived: false,
                skip: Math.Max(0, skip ?? 0),
                take: Math.Clamp(take ?? 20, 1, 100),
                cancellationToken).ConfigureAwait(false);

            var payload = conversations.Select(c => new
            {
                id = c.Id,
                title = c.Title,
                updatedAt = c.UpdatedAt,
                messageCount = c.MessageCount,
                provider = c.Provider,
                modelName = c.ModelName
            });

            return Results.Ok(payload);
        });

        app.MapGet("/conversations/{id:guid}", async (
            Guid id,
            LoadConversationUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var conversation = await useCase.ExecuteAsync(id, cancellationToken).ConfigureAwait(false);
            if (conversation is null)
            {
                return Results.NotFound();
            }

            var payload = new
            {
                id = conversation.Id,
                title = conversation.Title,
                createdAt = conversation.CreatedAt,
                updatedAt = conversation.UpdatedAt,
                isArchived = conversation.IsArchived,
                provider = conversation.Provider,
                modelName = conversation.ModelName,
                messages = conversation.Messages.Select(message => new
                {
                    id = message.Id,
                    role = message.Role,
                    content = message.Content,
                    timestamp = message.Timestamp,
                    isProcessing = message.IsProcessing,
                    tokenCount = message.TokenCount,
                    actionCard = message.ActionCard == null ? null : new
                    {
                        id = message.ActionCard.Id,
                        title = message.ActionCard.Title,
                        status = message.ActionCard.Status,
                        operation = message.ActionCard.Operation,
                        statusMessage = message.ActionCard.StatusMessage,
                        createdAt = message.ActionCard.CreatedAt,
                        completedAt = message.ActionCard.CompletedAt,
                        plannedActions = message.PlannedActions.Select(action => new
                        {
                            id = action.Id,
                            type = action.Type,
                            source = action.Source,
                            destination = action.Destination,
                            description = action.Description,
                            operation = action.Operation,
                            content = action.Content,
                            sortOrder = action.SortOrder
                        })
                    },
                    fileOperation = message.FileOperation == null ? null : new
                    {
                        id = message.FileOperation.Id,
                        action = message.FileOperation.Action,
                        filePath = message.FileOperation.FilePath,
                        timestamp = message.FileOperation.Timestamp
                    }
                })
            };

            return Results.Ok(payload);
        });

        app.MapPost("/conversations", async (
            CreateConversationRequest request,
            CreateConversationUseCase useCase,
            ILlmClientFactory llmClientFactory,
            IOptions<AppSettings> appSettings,
            CancellationToken cancellationToken) =>
        {
            var provider = ParseProvider(appSettings.Value.LLM.Provider);
            var modelName = llmClientFactory.GetModelName();
            var conversationId = await useCase.ExecuteAsync(
                request.UserId,
                string.IsNullOrWhiteSpace(request.Title) ? "New Conversation" : request.Title,
                provider,
                modelName,
                cancellationToken).ConfigureAwait(false);

            return Results.Created($"/conversations/{conversationId}", new { id = conversationId });
        });

        app.MapDelete("/conversations/{id:guid}", async (
            Guid id,
            IConversationRepository repository,
            CancellationToken cancellationToken) =>
        {
            await repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        });

        app.MapPost("/chat", async (
            ChatRequest request,
            StartChatUseCase useCase,
            ILlmClientFactory llmClientFactory,
            IOptions<AppSettings> appSettings,
            CancellationToken cancellationToken) =>
        {
            var instructions = AgentInstructions.ObsidianAssistant;

            var history = request.History?.Select(h => new ConversationMessage(
                h.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? ParticipantRole.User : ParticipantRole.Assistant,
                h.Content)).ToList();

            var input = new ChatInput(request.Message, history);
            var context = BuildPersistenceContext(request, null, appSettings.Value, llmClientFactory.GetModelName());
            var result = await useCase.ExecuteAsync(input, instructions, context, cancellationToken).ConfigureAwait(false);

            return Results.Ok(new
            {
                conversationId = result.ConversationId,
                userMessageId = result.UserMessageId,
                assistantMessageId = result.AssistantMessageId,
                text = result.Text,
                fileOperationResult = result.FileOperation == null ? null : new FileOperationData(result.FileOperation.Action, result.FileOperation.FilePath)
            });
        });

        app.MapPost("/chat/stream", async (
            ChatRequest request,
            HttpContext context,
            StreamChatUseCase useCase,
            ILlmClientFactory llmClientFactory,
            IOptions<AppSettings> appSettings,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ChatStream");
            logger.LogInformation("Starting SSE stream for message: {Message}", request.Message);

            var instructions = AgentInstructions.ObsidianAssistant;

            var history = request.History?.Select(h => new ConversationMessage(
                h.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? ParticipantRole.User : ParticipantRole.Assistant,
                h.Content)).ToList();

            var input = new ChatInput(request.Message, history);
            var persistenceContext = BuildPersistenceContext(request, null, appSettings.Value, llmClientFactory.GetModelName());
            var stream = useCase.ExecuteAsync(input, instructions, persistenceContext, context.RequestAborted);

            await StreamingEventWriter.WriteAsync(context, stream, logger, context.RequestAborted).ConfigureAwait(false);
        });

        app.MapPost("/vault/search", async (SearchRequest request, SearchVaultUseCase useCase, CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(request.Query, cancellationToken).ConfigureAwait(false);
            var apiResults = result.Results.Select(r => new SearchResult(r.Path, (float)r.Score, r.Preview)).ToList();
            return Results.Ok(new ObsidianAI.Api.Models.SearchResponse(apiResults));
        });

        app.MapPost("/vault/reorganize", (ReorganizeRequest request) => Results.Ok(new ReorganizeResponse("Completed", 10)));

        app.MapPost("/vault/modify", async (ModifyRequest request, ModifyVaultUseCase useCase, CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(request.Operation, request.FilePath, request.Content, cancellationToken).ConfigureAwait(false);
            return Results.Ok(new ModifyResponse(result.Success, result.Message, result.FilePath));
        });
    }

    private static ConversationPersistenceContext BuildPersistenceContext(ChatRequest request, string? userId, AppSettings appSettings, string modelName)
    {
        var provider = ParseProvider(appSettings.LLM.Provider);
        return new ConversationPersistenceContext(request.ConversationId, userId, provider, modelName, request.Message);
    }

    private static ConversationProvider ParseProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return ConversationProvider.Unknown;
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "lmstudio" => ConversationProvider.LmStudio,
            "openrouter" => ConversationProvider.OpenRouter,
            _ => ConversationProvider.Unknown
        };
    }
}
