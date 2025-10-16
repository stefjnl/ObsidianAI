using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IMessageRepository"/>.
/// </summary>
public sealed class MessageRepository : IMessageRepository
{
    private readonly ObsidianAIDbContext _dbContext;

    public MessageRepository(ObsidianAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Message message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _dbContext.Messages.AddAsync(message, ct).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task AddRangeAsync(IEnumerable<Message> messages, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        await _dbContext.Messages.AddRangeAsync(messages, ct).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Messages
            .Include(m => m.ActionCard)
                .ThenInclude(ac => ac!.PlannedActions)
            .Include(m => m.FileOperation)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Message>> GetByConversationIdAsync(Guid conversationId, CancellationToken ct = default)
    {
        return await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.Timestamp)
            .Include(m => m.ActionCard) // single action card
                .ThenInclude(ac => ac!.PlannedActions)
            .Include(m => m.FileOperation)
            .AsSplitQuery()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Message message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _dbContext.Messages.Update(message);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
