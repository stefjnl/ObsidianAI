using System;
using System.Linq;
using System.Threading.Tasks;
using ObsidianAI.Application.UseCases;
using ObsidianAI.Infrastructure.Data;
using ObsidianAI.Infrastructure.Data.Repositories;
using ObsidianAI.Tests.Support;
using Xunit;

namespace ObsidianAI.Tests.Application;

public sealed class ListConversationsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsOrderedSummariesWithMessageCounts()
    {
        await using var connection = await SqliteDbContextFactory.CreateOpenConnectionAsync();
        var options = SqliteDbContextFactory.CreateOptions(connection);
        await SqliteDbContextFactory.EnsureCreatedAsync(options);

        var now = DateTime.UtcNow;
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var archivedId = Guid.NewGuid();

        await using (var arrangeContext = new ObsidianAIDbContext(options))
        {
            var repository = new ConversationRepository(arrangeContext);

            var firstConversation = TestEntityFactory.CreateConversation(
                id: firstId,
                title: "Active Two Messages",
                updatedAt: now.AddMinutes(-5),
                includeMessages: false);
            firstConversation.Messages.Add(TestEntityFactory.CreateMessage(firstId));
            firstConversation.Messages.Add(TestEntityFactory.CreateMessage(firstId));
            await repository.CreateAsync(firstConversation);

            await repository.CreateAsync(TestEntityFactory.CreateConversation(
                id: secondId,
                title: "Newest Active",
                updatedAt: now.AddMinutes(-1),
                includeMessages: true));

            await repository.CreateAsync(TestEntityFactory.CreateConversation(
                id: archivedId,
                title: "Archived",
                updatedAt: now.AddMinutes(-2),
                isArchived: true,
                includeMessages: true));
        }

        await using (var assertContext = new ObsidianAIDbContext(options))
        {
            var repository = new ConversationRepository(assertContext);
            var useCase = new ListConversationsUseCase(repository);

            var summaries = await useCase.ExecuteAsync(userId: null, includeArchived: false, skip: 0, take: 10);
            Assert.Equal(new[] { secondId, firstId }, summaries.Select(s => s.Id));
            Assert.Equal(1, summaries.First(s => s.Id == secondId).MessageCount);
            Assert.Equal(2, summaries.First(s => s.Id == firstId).MessageCount);

            var archivedIncluded = await useCase.ExecuteAsync(userId: null, includeArchived: true, skip: 0, take: 10);
            Assert.Equal(new[] { secondId, archivedId, firstId }, archivedIncluded.Select(s => s.Id));
        }
    }
}
