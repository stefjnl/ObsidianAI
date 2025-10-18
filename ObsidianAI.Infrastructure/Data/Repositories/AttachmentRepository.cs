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
/// EF Core implementation of <see cref="IAttachmentRepository"/>.
/// </summary>
public sealed class AttachmentRepository : IAttachmentRepository
{
    private readonly ObsidianAIDbContext _dbContext;

    public AttachmentRepository(ObsidianAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(Attachment attachment, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        await _dbContext.Attachments.AddAsync(attachment, ct).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Attachment>> GetByConversationIdAsync(Guid conversationId, CancellationToken ct = default)
    {
        return await _dbContext.Attachments
            .AsNoTracking()
            .Where(a => a.ConversationId == conversationId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<Attachment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Attachments
            .FirstOrDefaultAsync(a => a.Id == id, ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var attachment = await _dbContext.Attachments.FirstOrDefaultAsync(a => a.Id == id, ct).ConfigureAwait(false);
        if (attachment == null)
        {
            return;
        }

        _dbContext.Attachments.Remove(attachment);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}