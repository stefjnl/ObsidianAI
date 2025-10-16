using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Infrastructure.Agents;

/// <summary>
/// In-memory AgentThread provider suitable for single-node deployments or development scenarios.
/// </summary>
public sealed class InMemoryAgentThreadProvider : IAgentThreadProvider
{
    private readonly ConcurrentDictionary<string, AgentThread> _threads = new();

    /// <inheritdoc />
    public Task<string> RegisterThreadAsync(AgentThread thread, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(thread);

        var threadId = Guid.NewGuid().ToString("N");
        _threads[threadId] = thread;
        return Task.FromResult(threadId);
    }

    /// <inheritdoc />
    public Task<AgentThread?> GetThreadAsync(string threadId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(threadId))
        {
            return Task.FromResult<AgentThread?>(null);
        }

        _threads.TryGetValue(threadId, out var thread);
        return Task.FromResult(thread);
    }

    /// <inheritdoc />
    public Task DeleteThreadAsync(string threadId, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(threadId))
        {
            _threads.TryRemove(threadId, out _);
        }

        return Task.CompletedTask;
    }
}
