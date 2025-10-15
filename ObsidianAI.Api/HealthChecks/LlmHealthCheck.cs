using Microsoft.Extensions.Diagnostics.HealthChecks;
using ObsidianAI.Infrastructure.LLM;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Api.HealthChecks;

/// <summary>
/// Health check that verifies the configured LLM factory can instantiate a chat client.
/// </summary>
public sealed class LlmHealthCheck : IHealthCheck
{
    private readonly ILlmClientFactory _llmClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmHealthCheck"/> class.
    /// </summary>
    /// <param name="llmClientFactory">The LLM client factory used by the application.</param>
    public LlmHealthCheck(ILlmClientFactory llmClientFactory)
    {
        _llmClientFactory = llmClientFactory ?? throw new ArgumentNullException(nameof(llmClientFactory));
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = _llmClientFactory.CreateChatClient();
            var modelName = _llmClientFactory.GetModelName();
            return Task.FromResult(HealthCheckResult.Healthy($"LLM client ready for model '{modelName}'"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to initialize LLM client", ex));
        }
    }
}
