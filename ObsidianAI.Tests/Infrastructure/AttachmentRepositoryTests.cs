using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Infrastructure.Data;
using ObsidianAI.Infrastructure.Data.Repositories;
using Xunit;

namespace ObsidianAI.Tests.Infrastructure;

public class AttachmentRepositoryTests : IDisposable
{
    private readonly ObsidianAIDbContext _dbContext;
    private readonly AttachmentRepository _repository;

    public AttachmentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ObsidianAIDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ObsidianAIDbContext(options);
        _repository = new AttachmentRepository(_dbContext);
    }

    [Fact]
    public async Task CreateAsync_AddsAttachmentToDatabase()
    {
        // Arrange
        var attachment = new Attachment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test.txt",
            "Hello World",
            ".txt");

        // Act
        await _repository.CreateAsync(attachment, CancellationToken.None);

        // Assert
        var saved = await _dbContext.Attachments.FindAsync(attachment.Id);
        Assert.NotNull(saved);
        Assert.Equal(attachment.Filename, saved.Filename);
        Assert.Equal(attachment.Content, saved.Content);
    }

    [Fact]
    public async Task GetByConversationIdAsync_ReturnsAttachmentsForConversation()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var attachment1 = new Attachment(Guid.NewGuid(), conversationId, "file1.txt", "content1", ".txt");
        var attachment2 = new Attachment(Guid.NewGuid(), conversationId, "file2.md", "content2", ".md");
        var otherConversationId = Guid.NewGuid();
        var attachment3 = new Attachment(Guid.NewGuid(), otherConversationId, "file3.json", "content3", ".json");

        await _dbContext.Attachments.AddRangeAsync(attachment1, attachment2, attachment3);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByConversationIdAsync(conversationId, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Filename == "file1.txt");
        Assert.Contains(result, a => a.Filename == "file2.md");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}