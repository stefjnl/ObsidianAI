using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ObsidianAI.Application.UseCases;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Infrastructure.Data;
using ObsidianAI.Infrastructure.Data.Repositories;
using ObsidianAI.Tests.Support;
using Xunit;

namespace ObsidianAI.Tests.Application;

public sealed class UpdateMessageArtifactsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesArtifactsWhenMissing()
    {
        await using var connection = await SqliteDbContextFactory.CreateOpenConnectionAsync();
        var options = SqliteDbContextFactory.CreateOptions(connection);
        await SqliteDbContextFactory.EnsureCreatedAsync(options);

        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddMinutes(-5);
        var completedAt = DateTime.UtcNow;
        var timestamp = DateTime.UtcNow;

        await using (var arrangeContext = new ObsidianAIDbContext(options))
        {
            var conversation = TestEntityFactory.CreateConversation(
                id: conversationId,
                includeMessages: false);

            var message = TestEntityFactory.CreateMessage(conversationId, includeActionCard: false, includeFileOperation: false);
            message.Id = messageId;
            message.Timestamp = createdAt;

            await arrangeContext.Conversations.AddAsync(conversation);
            await arrangeContext.Messages.AddAsync(message);
            await arrangeContext.SaveChangesAsync();
        }

        await using (var assertContext = new ObsidianAIDbContext(options))
        {
            var repository = new MessageRepository(assertContext);
            var useCase = new UpdateMessageArtifactsUseCase(repository);

            var actionCardUpdate = new UpdateMessageArtifactsUseCase.ActionCardUpdate(
                Guid.NewGuid(),
                "Modify note",
                nameof(ActionCardStatus.Completed),
                "modify",
                "Completed successfully",
                createdAt,
                completedAt);

            var plannedActions = new List<UpdateMessageArtifactsUseCase.PlannedActionUpdate>
            {
                new(
                    Guid.NewGuid(),
                    nameof(PlannedActionType.Modify),
                    "notes/source.md",
                    null,
                    "Apply changes",
                    "modify",
                    "Updated content",
                    0)
            };

            var fileOperationUpdate = new UpdateMessageArtifactsUseCase.FileOperationUpdate(
                "modified",
                "notes/source.md",
                timestamp);

            var result = await useCase.ExecuteAsync(messageId, actionCardUpdate, plannedActions, fileOperationUpdate);

            Assert.NotNull(result);
            Assert.Equal(messageId, result!.Id);
            Assert.Equal("modify", result.ActionCard?.Operation);
            Assert.Equal(nameof(ActionCardStatus.Completed), result.ActionCard?.Status);
            Assert.Single(result.PlannedActions);
            Assert.Equal("notes/source.md", result.PlannedActions[0].Source);
            Assert.NotNull(result.FileOperation);
            Assert.Equal("modified", result.FileOperation!.Action);

            var persisted = await repository.GetByIdAsync(messageId);
            Assert.NotNull(persisted);
            Assert.NotNull(persisted!.ActionCard);
            Assert.Equal(ActionCardStatus.Completed, persisted.ActionCard!.Status);
            Assert.Single(persisted.ActionCard.PlannedActions);
            Assert.Equal("notes/source.md", persisted.ActionCard.PlannedActions.First().Source);
            Assert.NotNull(persisted.FileOperation);
            Assert.Equal("modified", persisted.FileOperation!.Action);
        }
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesExistingArtifacts()
    {
        await using var connection = await SqliteDbContextFactory.CreateOpenConnectionAsync();
        var options = SqliteDbContextFactory.CreateOptions(connection);
        await SqliteDbContextFactory.EnsureCreatedAsync(options);

        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        await using (var arrangeContext = new ObsidianAIDbContext(options))
        {
            var conversation = TestEntityFactory.CreateConversation(
                id: conversationId,
                includeMessages: false);

            var message = TestEntityFactory.CreateMessage(conversationId, includeActionCard: true, includeFileOperation: true);
            message.Id = messageId;
            if (message.ActionCard != null)
            {
                message.ActionCard.MessageId = messageId;
                foreach (var planned in message.ActionCard.PlannedActions)
                {
                    planned.ActionCardId = message.ActionCard.Id;
                }
            }

            if (message.FileOperation != null)
            {
                message.FileOperation.MessageId = messageId;
            }

            await arrangeContext.Conversations.AddAsync(conversation);
            await arrangeContext.Messages.AddAsync(message);
            await arrangeContext.SaveChangesAsync();
        }

        await using (var assertContext = new ObsidianAIDbContext(options))
        {
            var repository = new MessageRepository(assertContext);
            var useCase = new UpdateMessageArtifactsUseCase(repository);

            var actionCardUpdate = new UpdateMessageArtifactsUseCase.ActionCardUpdate(
                null,
                "Vault cleanup",
                nameof(ActionCardStatus.Failed),
                "delete",
                "Unable to delete file",
                DateTime.UtcNow.AddMinutes(-2),
                DateTime.UtcNow);

            var plannedActions = new List<UpdateMessageArtifactsUseCase.PlannedActionUpdate>
            {
                new(
                    Guid.NewGuid(),
                    nameof(PlannedActionType.Delete),
                    "notes/obsolete.md",
                    null,
                    "Remove obsolete file",
                    "delete",
                    null,
                    0),
                new(
                    Guid.NewGuid(),
                    nameof(PlannedActionType.Modify),
                    "notes/replacement.md",
                    null,
                    "Adjust replacement file",
                    "modify",
                    "Revised heading",
                    1)
            };

            var fileOperationUpdate = new UpdateMessageArtifactsUseCase.FileOperationUpdate(
                "delete",
                "notes/obsolete.md",
                DateTime.UtcNow);

            var result = await useCase.ExecuteAsync(messageId, actionCardUpdate, plannedActions, fileOperationUpdate);

            Assert.NotNull(result);
            Assert.Equal("Vault cleanup", result!.ActionCard?.Title);
            Assert.Equal(nameof(ActionCardStatus.Failed), result.ActionCard?.Status);
            Assert.Equal(2, result.PlannedActions.Count);
            Assert.Equal("delete", result.FileOperation?.Action);

            var persisted = await repository.GetByIdAsync(messageId);
            Assert.NotNull(persisted);
            Assert.NotNull(persisted!.ActionCard);
            Assert.Equal(ActionCardStatus.Failed, persisted.ActionCard!.Status);
            Assert.Equal("Vault cleanup", persisted.ActionCard.Title);
            Assert.Equal(2, persisted.ActionCard.PlannedActions.Count);
            Assert.Equal("notes/obsolete.md", persisted.ActionCard.PlannedActions.OrderBy(pa => pa.SortOrder).First().Source);
            Assert.Equal("delete", persisted.FileOperation?.Action);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsNullWhenMessageMissing()
    {
        await using var connection = await SqliteDbContextFactory.CreateOpenConnectionAsync();
        var options = SqliteDbContextFactory.CreateOptions(connection);
        await SqliteDbContextFactory.EnsureCreatedAsync(options);

        await using var assertContext = new ObsidianAIDbContext(options);
        var repository = new MessageRepository(assertContext);
        var useCase = new UpdateMessageArtifactsUseCase(repository);

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            null,
            null,
            null);

        Assert.Null(result);
    }
}
