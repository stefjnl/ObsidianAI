using System;
using System.Collections.Generic;
using System.Linq;
using ObsidianAI.Application.DTOs;
using ObsidianAI.Domain.Entities;

namespace ObsidianAI.Application.Mappers;

/// <summary>
/// Helper extensions to convert domain entities to DTOs.
/// </summary>
public static class ConversationMapper
{
    public static ConversationDto ToDto(this Conversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        var provider = conversation.Provider.ToString();
        return new ConversationDto(
            conversation.Id,
            conversation.Title,
            conversation.UpdatedAt,
            conversation.Messages?.Count ?? 0,
            provider,
            conversation.ModelName);
    }

    public static ConversationDetailDto ToDetailDto(this Conversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        var messages = conversation.Messages?
            .OrderBy(m => m.Timestamp)
            .Select(m => m.ToDto())
            .ToList() ?? new List<MessageDto>();

        return new ConversationDetailDto(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.IsArchived,
            conversation.Provider.ToString(),
            conversation.ModelName,
            messages);
    }

    public static MessageDto ToDto(this Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var actionCard = message.ActionCard?.ToDto();
        var plannedActions = message.ActionCard?.PlannedActions
            .OrderBy(pa => pa.SortOrder)
            .Select(pa => pa.ToDto())
            .ToList() ?? new List<PlannedActionDto>();

        return new MessageDto(
            message.Id,
            message.Role.ToString(),
            message.Content,
            message.Timestamp,
            message.IsProcessing,
            message.TokenCount,
            actionCard,
            message.FileOperation?.ToDto(),
            plannedActions);
    }

    private static ActionCardDto? ToDto(this ActionCardRecord? actionCard)
    {
        if (actionCard is null)
        {
            return null;
        }

        return new ActionCardDto(
            actionCard.Id,
            actionCard.Title,
            actionCard.Status.ToString(),
            actionCard.Operation,
            actionCard.StatusMessage,
            actionCard.CreatedAt,
            actionCard.CompletedAt);
    }

    private static PlannedActionDto ToDto(this PlannedActionRecord plannedAction)
    {
        return new PlannedActionDto(
            plannedAction.Id,
            plannedAction.Type.ToString(),
            plannedAction.Source,
            plannedAction.Destination,
            plannedAction.Description,
            plannedAction.Operation,
            plannedAction.Content,
            plannedAction.SortOrder);
    }

    private static FileOperationDto? ToDto(this FileOperationRecord? fileOperation)
    {
        if (fileOperation is null)
        {
            return null;
        }

        return new FileOperationDto(
            fileOperation.Id,
            fileOperation.Action,
            fileOperation.FilePath,
            fileOperation.Timestamp);
    }
}
