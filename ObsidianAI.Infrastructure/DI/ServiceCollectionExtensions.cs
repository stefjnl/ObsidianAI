using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.LLM;
using ObsidianAI.Infrastructure.Vault;
using Microsoft.Extensions.Logging;

namespace ObsidianAI.Infrastructure.DI;

/// <summary>
/// Extension methods for configuring ObsidianAI services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ObsidianAI services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddObsidianAI(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(options =>
        {
            configuration.Bind(options);
        });
        services.AddSingleton<IAIAgentFactory, ConfiguredAIAgentFactory>();
        services.AddSingleton<IVaultToolExecutor>(sp =>
        {
            var mcpClient = sp.GetService<ModelContextProtocol.Client.McpClient>();
            if (mcpClient == null)
            {
                // Log that we're using null executor
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("ServiceCollectionExtensions");
                logger.LogWarning("MCP client is not available. Using null vault tool executor - vault operations will be disabled.");
                return new NullVaultToolExecutor();
            }
            return new McpVaultToolExecutor(mcpClient);
        });

        return services;
    }
}