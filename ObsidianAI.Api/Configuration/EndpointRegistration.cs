using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
                        plannedActions = message.ActionCard == null
                            ? Enumerable.Empty<object>()
                            : message.PlannedActions.Select(action => (object)new
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
            DeleteConversationUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            await useCase.ExecuteAsync(id, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        });

        app.MapPut("/conversations/{id:guid}", async (
            Guid id,
            UpdateConversationRequest request,
            UpdateConversationUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var updated = await useCase.ExecuteAsync(id, request.Title, request.IsArchived, cancellationToken).ConfigureAwait(false);
            if (updated is null)
            {
                return Results.NotFound();
            }

            var payload = new
            {
                id = updated.Id,
                title = updated.Title,
                createdAt = updated.CreatedAt,
                updatedAt = updated.UpdatedAt,
                isArchived = updated.IsArchived,
                provider = updated.Provider,
                modelName = updated.ModelName,
                messageCount = updated.Messages.Count
            };

            return Results.Ok(payload);
        });

        app.MapGet("/conversations/{id:guid}/export", async (
            Guid id,
            LoadConversationUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var conversation = await useCase.ExecuteAsync(id, cancellationToken).ConfigureAwait(false);
            if (conversation is null)
            {
                return Results.NotFound();
            }

            var exportPayload = new
            {
                conversation = new
                {
                    conversation.Id,
                    conversation.Title,
                    conversation.CreatedAt,
                    conversation.UpdatedAt,
                    conversation.IsArchived,
                    conversation.Provider,
                    conversation.ModelName,
                    messages = conversation.Messages.Select(message => new
                    {
                        message.Id,
                        message.Role,
                        message.Content,
                        message.Timestamp,
                        message.IsProcessing,
                        message.TokenCount,
                        actionCard = message.ActionCard == null ? null : new
                        {
                            message.ActionCard.Id,
                            message.ActionCard.Title,
                            message.ActionCard.Status,
                            message.ActionCard.Operation,
                            message.ActionCard.StatusMessage,
                            message.ActionCard.CreatedAt,
                            message.ActionCard.CompletedAt,
                            plannedActions = message.PlannedActions.Select(action => new
                            {
                                action.Id,
                                action.Type,
                                action.Source,
                                action.Destination,
                                action.Description,
                                action.Operation,
                                action.Content,
                                action.SortOrder
                            })
                        },
                        fileOperation = message.FileOperation == null ? null : new
                        {
                            message.FileOperation.Id,
                            message.FileOperation.Action,
                            message.FileOperation.FilePath,
                            message.FileOperation.Timestamp
                        }
                    })
                }
            };

            var json = JsonSerializer.Serialize(exportPayload, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var fileName = $"{SanitizeFileName(conversation.Title)}-{conversation.Id}.json";
            return Results.File(Encoding.UTF8.GetBytes(json), "application/json", fileName);
        });

        app.MapPost("/conversations/{id:guid}/archive", async (
            Guid id,
            ArchiveConversationUseCase archiveUseCase,
            CancellationToken cancellationToken) =>
        {
            var archived = await archiveUseCase.ExecuteAsync(id, cancellationToken).ConfigureAwait(false);
            if (archived is null)
            {
                return Results.NotFound();
            }

            var payload = new
            {
                id = archived.Id,
                title = archived.Title,
                createdAt = archived.CreatedAt,
                updatedAt = archived.UpdatedAt,
                isArchived = archived.IsArchived,
                provider = archived.Provider,
                modelName = archived.ModelName,
                messageCount = archived.Messages.Count
            };

            return Results.Ok(payload);
        });

        app.MapPost("/messages/{id:guid}/artifacts", async (
            Guid id,
            UpdateMessageArtifactsRequest request,
            UpdateMessageArtifactsUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var actionCardUpdate = request.ActionCard == null
                ? null
                : new UpdateMessageArtifactsUseCase.ActionCardUpdate(
                    TryParseGuid(request.ActionCard.Id),
                    request.ActionCard.Title,
                    request.ActionCard.Status,
                    request.ActionCard.Operation,
                    request.ActionCard.StatusMessage,
                    request.ActionCard.CreatedAt,
                    request.ActionCard.CompletedAt);

            var plannedActions = request.ActionCard?.PlannedActions?.Select(action => new UpdateMessageArtifactsUseCase.PlannedActionUpdate(
                TryParseGuid(action.Id),
                action.Type,
                action.Source,
                action.Destination,
                action.Description,
                action.Operation,
                action.Content,
                action.SortOrder)).ToList();

            var fileOperationUpdate = request.FileOperation == null
                ? null
                : new UpdateMessageArtifactsUseCase.FileOperationUpdate(
                    request.FileOperation.Action,
                    request.FileOperation.FilePath,
                    request.FileOperation.Timestamp);

            var updated = await useCase.ExecuteAsync(
                id,
                actionCardUpdate,
                plannedActions,
                fileOperationUpdate,
                cancellationToken).ConfigureAwait(false);

            if (updated is null)
            {
                return Results.NotFound();
            }

            var payload = new
            {
                id = updated.Id,
                role = updated.Role,
                content = updated.Content,
                timestamp = updated.Timestamp,
                isProcessing = updated.IsProcessing,
                tokenCount = updated.TokenCount,
                actionCard = updated.ActionCard == null ? null : new
                {
                    id = updated.ActionCard.Id,
                    title = updated.ActionCard.Title,
                    status = updated.ActionCard.Status,
                    operation = updated.ActionCard.Operation,
                    statusMessage = updated.ActionCard.StatusMessage,
                    createdAt = updated.ActionCard.CreatedAt,
                    completedAt = updated.ActionCard.CompletedAt,
                    plannedActions = updated.PlannedActions.Select(action => new
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
                fileOperation = updated.FileOperation == null ? null : new
                {
                    id = updated.FileOperation.Id,
                    action = updated.FileOperation.Action,
                    filePath = updated.FileOperation.FilePath,
                    timestamp = updated.FileOperation.Timestamp
                }
            };

            return Results.Ok(payload);
        });

        app.MapPost("/chat", async (
            ChatRequest request,
            StartChatUseCase useCase,
            ILlmClientFactory llmClientFactory,
            IConversationRepository conversationRepository,
            IOptions<AppSettings> appSettings,
            CancellationToken cancellationToken) =>
        {
            var instructions = AgentInstructions.ObsidianAssistant;

            string? threadId = null;
            if (request.ConversationId.HasValue)
            {
                var conversation = await conversationRepository.GetByIdAsync(request.ConversationId.Value, includeMessages: false, cancellationToken).ConfigureAwait(false);
                threadId = conversation?.ThreadId;
            }

            var input = new ChatInput(request.Message);
            var context = BuildPersistenceContext(request, null, appSettings.Value, llmClientFactory.GetModelName(), threadId);
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
            IConversationRepository conversationRepository,
            IOptions<AppSettings> appSettings,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ChatStream");
            logger.LogInformation("Starting SSE stream for message: {Message}", request.Message);

            var instructions = AgentInstructions.ObsidianAssistant;

            string? threadId = null;
            if (request.ConversationId.HasValue)
            {
                var conversation = await conversationRepository.GetByIdAsync(request.ConversationId.Value, includeMessages: false, context.RequestAborted).ConfigureAwait(false);
                threadId = conversation?.ThreadId;
            }

            var input = new ChatInput(request.Message);
            var persistenceContext = BuildPersistenceContext(request, null, appSettings.Value, llmClientFactory.GetModelName(), threadId);
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

    private static ConversationPersistenceContext BuildPersistenceContext(ChatRequest request, string? userId, AppSettings appSettings, string modelName, string? threadId)
    {
        var provider = ParseProvider(appSettings.LLM.Provider);
        return new ConversationPersistenceContext(request.ConversationId, userId, provider, modelName, request.Message, threadId);
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

    private static string SanitizeFileName(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "conversation";
        }

        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = new string(title.Select(c => invalidChars.Contains(c) ? '-' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "conversation" : sanitized;
    }

    private static Guid? TryParseGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var result) ? result : null;
    }
}
