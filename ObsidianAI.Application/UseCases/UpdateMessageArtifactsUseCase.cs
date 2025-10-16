using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Application.DTOs;
using ObsidianAI.Application.Mappers;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Updates persisted message artifacts such as action cards and file operations.
/// </summary>
public sealed class UpdateMessageArtifactsUseCase
{
    private readonly IMessageRepository _messageRepository;

    public UpdateMessageArtifactsUseCase(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    /// <summary>
    /// Applies the provided artifacts to the specified message and returns the updated DTO.
    /// </summary>
    /// <param name="messageId">Identifier of the message to update.</param>
    /// <param name="actionCard">Optional action card update details.</param>
    /// <param name="plannedActions">Optional planned action collection for the action card.</param>
    /// <param name="fileOperation">Optional file operation to associate with the message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated message DTO or <c>null</c> when the message does not exist.</returns>
    public async Task<MessageDto?> ExecuteAsync(
        Guid messageId,
        ActionCardUpdate? actionCard,
        IReadOnlyList<PlannedActionUpdate>? plannedActions,
        FileOperationUpdate? fileOperation,
        CancellationToken ct = default)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message identifier cannot be empty.", nameof(messageId));
        }

        var message = await _messageRepository.GetByIdAsync(messageId, ct).ConfigureAwait(false);
        if (message is null)
        {
            return null;
        }

        if (actionCard != null)
        {
            ApplyActionCardUpdate(message, actionCard, plannedActions);
        }

        if (fileOperation != null)
        {
            ApplyFileOperationUpdate(message, fileOperation);
        }

        await _messageRepository.UpdateAsync(message, ct).ConfigureAwait(false);
        return message.ToDto();
    }

    private static void ApplyActionCardUpdate(Message message, ActionCardUpdate actionCard, IReadOnlyList<PlannedActionUpdate>? plannedActions)
    {
        var status = ParseEnum(actionCard.Status, ActionCardStatus.Pending);
        var createdAt = actionCard.CreatedAt ?? message.Timestamp;
        var completedAt = actionCard.CompletedAt;
        var actionCardId = actionCard.Id ?? message.ActionCard?.Id ?? Guid.NewGuid();

        if (message.ActionCard == null)
        {
            message.ActionCard = new ActionCardRecord
            {
                Id = actionCardId,
                MessageId = message.Id,
                Title = actionCard.Title ?? string.Empty,
                Operation = actionCard.Operation ?? string.Empty,
                Status = status,
                StatusMessage = actionCard.StatusMessage,
                CreatedAt = createdAt,
                CompletedAt = completedAt,
                PlannedActions = new List<PlannedActionRecord>()
            };
        }
        else
        {
            message.ActionCard.Id = actionCardId;
            message.ActionCard.MessageId = message.Id;
            message.ActionCard.Title = actionCard.Title ?? message.ActionCard.Title;
            message.ActionCard.Operation = actionCard.Operation ?? message.ActionCard.Operation;
            message.ActionCard.Status = status;
            message.ActionCard.StatusMessage = actionCard.StatusMessage;
            message.ActionCard.CreatedAt = createdAt;
            message.ActionCard.CompletedAt = completedAt;
        }

        if (plannedActions != null)
        {
            message.ActionCard!.PlannedActions = plannedActions
                .Select((action, index) => new PlannedActionRecord
                {
                    Id = action.Id ?? Guid.NewGuid(),
                    ActionCardId = actionCardId,
                    Type = ParseEnum(action.Type, PlannedActionType.Other),
                    Source = action.Source ?? string.Empty,
                    Destination = action.Destination,
                    Description = action.Description ?? string.Empty,
                    Operation = action.Operation ?? string.Empty,
                    Content = action.Content,
                    SortOrder = action.SortOrder ?? index
                })
                .ToList();
        }
    }

    private static void ApplyFileOperationUpdate(Message message, FileOperationUpdate fileOperation)
    {
        if (string.IsNullOrWhiteSpace(fileOperation.Action) || string.IsNullOrWhiteSpace(fileOperation.FilePath))
        {
            return;
        }

        var timestamp = fileOperation.Timestamp ?? DateTime.UtcNow;

        if (message.FileOperation == null)
        {
            message.FileOperation = new FileOperationRecord
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                Action = fileOperation.Action,
                FilePath = fileOperation.FilePath,
                Timestamp = timestamp
            };
        }
        else
        {
            message.FileOperation.Action = fileOperation.Action;
            message.FileOperation.FilePath = fileOperation.FilePath;
            message.FileOperation.Timestamp = timestamp;
        }
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
    }

    /// <summary>
    /// Action card update payload.
    /// </summary>
    public sealed record ActionCardUpdate(
        Guid? Id,
        string? Title,
        string? Status,
        string? Operation,
        string? StatusMessage,
        DateTime? CreatedAt,
        DateTime? CompletedAt);

    /// <summary>
    /// Planned action update payload.
    /// </summary>
    public sealed record PlannedActionUpdate(
        Guid? Id,
        string? Type,
        string? Source,
        string? Destination,
        string? Description,
        string? Operation,
        string? Content,
        int? SortOrder);

    /// <summary>
    /// File operation update payload.
    /// </summary>
    public sealed record FileOperationUpdate(
        string Action,
        string FilePath,
        DateTime? Timestamp);
}
