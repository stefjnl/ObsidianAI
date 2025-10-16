using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ObsidianAI.Infrastructure.Data;
using ObsidianAI.Infrastructure.Data.Repositories;
using ObsidianAI.Tests.Support;
using Xunit;

namespace ObsidianAI.Tests.Infrastructure;

public sealed class ConversationRepositoryTests
{
    [Fact]
    public async Task CreateAsync_PersistsConversationGraph()
    {
        await using var connection = await SqliteDbContextFactory.CreateOpenConnectionAsync();
        var options = SqliteDbContextFactory.CreateOptions(connection);
        await SqliteDbContextFactory.EnsureCreatedAsync(options);

        var conversationId = Guid.NewGuid();

        await using (var arrangeContext = new ObsidianAIDbContext(options))
        {
            var repository = new ConversationRepository(arrangeContext);
            var conversation = TestEntityFactory.CreateConversation(
                id: conversationId,
                includeMessages: true,
                includeActionCard: true,
                includeFileOperation: true);

            await repository.CreateAsync(conversation);
        }

        await using (var assertContext = new ObsidianAIDbContext(options))
        {
            var persisted = await assertContext.Conversations
                .Include(c => c.Messages)
                    .ThenInclude(m => m.ActionCard)
                        .ThenInclude(ac => ac!.PlannedActions)
                .Include(c => c.Messages)
                    .ThenInclude(m => m.FileOperation)
                .SingleOrDefaultAsync(c => c.Id == conversationId);

            Assert.NotNull(persisted);
            Assert.Single(persisted!.Messages);
            var message = persisted.Messages.Single();
            Assert.NotNull(message.ActionCard);
            Assert.Single(message.ActionCard!.PlannedActions);
            Assert.NotNull(message.FileOperation);
        }
    }

    [Fact]
    public async Task GetAllAsync_RespectsArchiveFilterAndOrdering()
    {
        await using var connection = await SqliteDbContextFactory.CreateOpenConnectionAsync();
        var options = SqliteDbContextFactory.CreateOptions(connection);
        await SqliteDbContextFactory.EnsureCreatedAsync(options);

        var now = DateTime.UtcNow;
        var newestActiveId = Guid.NewGuid();
        var olderActiveId = Guid.NewGuid();
        var archivedId = Guid.NewGuid();

        await using (var arrangeContext = new ObsidianAIDbContext(options))
        {
            var repository = new ConversationRepository(arrangeContext);

            await repository.CreateAsync(TestEntityFactory.CreateConversation(
                id: newestActiveId,
                title: "Newest Active",
                updatedAt: now.AddMinutes(-1),
                includeMessages: true));

            await repository.CreateAsync(TestEntityFactory.CreateConversation(
                id: olderActiveId,
                title: "Older Active",
                updatedAt: now.AddMinutes(-5),
                includeMessages: true));

            await repository.CreateAsync(TestEntityFactory.CreateConversation(
                id: archivedId,
                title: "Archived",
                updatedAt: now.AddMinutes(-10),
                isArchived: true,
                includeMessages: true));

            arrangeContext.ChangeTracker.Clear();

            var activeOnly = await repository.GetAllAsync(userId: null, includeArchived: false, skip: 0, take: 10);
            Assert.Equal(2, activeOnly.Count);
            Assert.All(activeOnly, c => Assert.False(c.IsArchived));
            Assert.Equal(new[] { newestActiveId, olderActiveId }, activeOnly.Select(c => c.Id));

            var allConversations = await repository.GetAllAsync(userId: null, includeArchived: true, skip: 0, take: 10);
            Assert.Equal(3, allConversations.Count);
            Assert.Equal(new[] { newestActiveId, olderActiveId, archivedId }, allConversations.Select(c => c.Id));

            var paged = await repository.GetAllAsync(userId: null, includeArchived: true, skip: 1, take: 1);
            Assert.Single(paged);
            Assert.Equal(olderActiveId, paged[0].Id);
        }
    }
}
