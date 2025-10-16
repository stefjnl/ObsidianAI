using System;
using System.Collections.Generic;
using ObsidianAI.Domain.Entities;

namespace ObsidianAI.Tests.Support;

/// <summary>
/// Provides helper factory methods for constructing domain entities in tests.
/// </summary>
internal static class TestEntityFactory
{
    public static Conversation CreateConversation(
        Guid? id = null,
        string? title = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null,
        bool isArchived = false,
        ConversationProvider provider = ConversationProvider.LmStudio,
        string? modelName = null,
        bool includeMessages = true,
        bool includeActionCard = false,
        bool includeFileOperation = false)
    {
        var conversationId = id ?? Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = conversationId,
            Title = title ?? "Test Conversation",
            CreatedAt = createdAt ?? DateTime.UtcNow.AddMinutes(-30),
            UpdatedAt = updatedAt ?? DateTime.UtcNow.AddMinutes(-30),
            IsArchived = isArchived,
            Provider = provider,
            ModelName = modelName ?? "test-model"
        };

        if (includeMessages)
        {
            var message = CreateMessage(conversationId, includeActionCard, includeFileOperation);
            conversation.Messages.Add(message);
        }

        return conversation;
    }

    public static Message CreateMessage(Guid conversationId, bool includeActionCard = false, bool includeFileOperation = false)
    {
        var messageId = Guid.NewGuid();
        var message = new Message
        {
            Id = messageId,
            ConversationId = conversationId,
            Role = MessageRole.User,
            Content = "Hello from the test conversation",
            Timestamp = DateTime.UtcNow.AddMinutes(-20),
            TokenCount = 256,
            IsProcessing = false
        };

        if (includeActionCard)
        {
            var actionCardId = Guid.NewGuid();
            var actionCard = new ActionCardRecord
            {
                Id = actionCardId,
                MessageId = messageId,
                Title = "Planned vault edits",
                Status = ActionCardStatus.Pending,
                Operation = "modify",
                CreatedAt = DateTime.UtcNow.AddMinutes(-19),
                StatusMessage = "Awaiting confirmation",
                PlannedActions = new List<PlannedActionRecord>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ActionCardId = actionCardId,
                        Type = PlannedActionType.Modify,
                        Source = "notes/original.md",
                        Destination = null,
                        Description = "Update the note contents",
                        Operation = "modify",
                        Content = "New content from test",
                        SortOrder = 0
                    }
                }
            };

            message.ActionCard = actionCard;
        }

        if (includeFileOperation)
        {
            message.FileOperation = new FileOperationRecord
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                Action = "modified",
                FilePath = "notes/original.md",
                Timestamp = DateTime.UtcNow.AddMinutes(-18)
            };
        }

        return message;
    }
}
