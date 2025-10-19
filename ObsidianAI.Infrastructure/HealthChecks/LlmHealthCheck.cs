using Microsoft.Extensions.Diagnostics.HealthChecks;
using ObsidianAI.Domain.Ports;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies the configured LLM factory can instantiate a chat client.
/// </summary>
public sealed class LlmHealthCheck : IHealthCheck
{
    private readonly IAIAgentFactory _agentFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmHealthCheck"/> class.
    /// </summary>
    /// <param name="agentFactory">The AI agent factory used by the application.</param>
    public LlmHealthCheck(IAIAgentFactory agentFactory)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify the factory can create an agent with minimal instructions
            _ = await _agentFactory.CreateAgentAsync("Health check", tools: null, threadProvider: null, cancellationToken).ConfigureAwait(false);
            var modelName = _agentFactory.GetModelName();
            return HealthCheckResult.Healthy($"LLM client ready for model '{modelName}'");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to initialize LLM client", ex);
        }
    }
}
