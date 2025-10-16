using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ObsidianAI.Application.UseCases;
using ObsidianAI.Infrastructure.Data;
using ObsidianAI.Infrastructure.Data.Repositories;
using ObsidianAI.Tests.Support;
using Xunit;

namespace ObsidianAI.Tests.Application;

public sealed class UpdateConversationUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_UpdatesTitleAndArchiveState()
    {
        await using var connection = await SqliteDbContextFactory.CreateOpenConnectionAsync();
        var options = SqliteDbContextFactory.CreateOptions(connection);
        await SqliteDbContextFactory.EnsureCreatedAsync(options);

        var initialUpdatedAt = DateTime.UtcNow.AddMinutes(-30);
        var conversationId = Guid.NewGuid();

        await using (var arrangeContext = new ObsidianAIDbContext(options))
        {
            var repository = new ConversationRepository(arrangeContext);
            var conversation = TestEntityFactory.CreateConversation(
                id: conversationId,
                updatedAt: initialUpdatedAt,
                includeMessages: true,
                includeActionCard: true);

            await repository.CreateAsync(conversation);
        }

        await using (var assertContext = new ObsidianAIDbContext(options))
        {
            var repository = new ConversationRepository(assertContext);
            var useCase = new UpdateConversationUseCase(repository);

            var result = await useCase.ExecuteAsync(conversationId, "  Updated Conversation Title  ", isArchived: true);

            Assert.NotNull(result);
            Assert.Equal("Updated Conversation Title", result!.Title);
            Assert.True(result.IsArchived);

            var persisted = await repository.GetByIdAsync(conversationId, includeMessages: false);
            Assert.NotNull(persisted);
            Assert.Equal("Updated Conversation Title", persisted!.Title);
            Assert.True(persisted.IsArchived);
            Assert.True(persisted.UpdatedAt > initialUpdatedAt);
        }
    }

    [Fact]
    public async Task ExecuteAsync_TruncatesOverlyLongTitles()
    {
        await using var connection = await SqliteDbContextFactory.CreateOpenConnectionAsync();
        var options = SqliteDbContextFactory.CreateOptions(connection);
        await SqliteDbContextFactory.EnsureCreatedAsync(options);

        var conversationId = Guid.NewGuid();
        await using (var arrangeContext = new ObsidianAIDbContext(options))
        {
            var repository = new ConversationRepository(arrangeContext);
            await repository.CreateAsync(TestEntityFactory.CreateConversation(
                id: conversationId,
                title: "Original",
                includeMessages: true));
        }

        await using (var assertContext = new ObsidianAIDbContext(options))
        {
            var repository = new ConversationRepository(assertContext);
            var useCase = new UpdateConversationUseCase(repository);

            var overlongTitle = new string('a', 100);
            var result = await useCase.ExecuteAsync(conversationId, overlongTitle, isArchived: null);

            Assert.NotNull(result);
            Assert.Equal(81, result!.Title.Length); // 80 chars + ellipsis
            Assert.EndsWith("…", result.Title);
        }
    }
}
