using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IConversationRepository"/>.
/// </summary>
public sealed class ConversationRepository : IConversationRepository
{
    private readonly ObsidianAIDbContext _dbContext;

    public ConversationRepository(ObsidianAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(Conversation conversation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        await _dbContext.Conversations.AddAsync(conversation, ct).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, bool includeMessages = false, CancellationToken ct = default)
    {
        IQueryable<Conversation> query = _dbContext.Conversations;

        if (includeMessages)
        {
            query = query
                .Include(c => c.Messages)
                    .ThenInclude(m => m.ActionCard)
                        .ThenInclude(ac => ac!.PlannedActions)
                .Include(c => c.Messages)
                    .ThenInclude(m => m.FileOperation)
                .AsSplitQuery();
        }

        return await query.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Conversation>> GetAllAsync(string? userId, bool includeArchived, int skip, int take, CancellationToken ct = default)
    {
        IQueryable<Conversation> query = _dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Messages);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(c => c.UserId == userId);
        }

        if (!includeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        query = query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip(skip)
            .Take(take);

        return await query.ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Conversation conversation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        _dbContext.Conversations.Update(conversation);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var conversation = await _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        if (conversation == null)
        {
            return;
        }

        conversation.IsArchived = true;
        conversation.Touch();
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var conversation = await _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        if (conversation == null)
        {
            return;
        }

        _dbContext.Conversations.Remove(conversation);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
