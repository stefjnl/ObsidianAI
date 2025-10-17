using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObsidianAI.Application.Services;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Domain.Services;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.Data;
using ObsidianAI.Infrastructure.Data.Repositories;
using ObsidianAI.Infrastructure.Agents;
using ObsidianAI.Infrastructure.LLM;
using ObsidianAI.Infrastructure.Middleware;
using ObsidianAI.Infrastructure.Services;
using ObsidianAI.Infrastructure.Vault;

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
        services.Configure<AppSettings>(configuration);

        var connectionString = configuration.GetConnectionString("ObsidianAI") ?? "Data Source=obsidianai.db";
        services.AddDbContext<ObsidianAIDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        // Register agent state service
        services.AddSingleton<IAgentStateService, AgentStateService>();

        // Register reflection services
        services.AddSingleton<ReflectionPromptBuilder>();
        services.AddSingleton<IReflectionService, OpenRouterReflectionService>();

        // Register reflection middleware
        services.AddSingleton<IFunctionMiddleware, ReflectionFunctionMiddleware>();

        // Register agent factory with middleware injection
        services.AddSingleton<IAIAgentFactory>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AppSettings>>();
            var middlewares = sp.GetServices<IFunctionMiddleware>();
            var logger = sp.GetRequiredService<ILogger<ConfiguredAIAgentFactory>>();
            return new ConfiguredAIAgentFactory(options, middlewares, logger);
        });

        services.AddSingleton<IAgentThreadProvider, InMemoryAgentThreadProvider>();
        services.AddSingleton<IVaultToolExecutor>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<McpVaultToolExecutor>>();
            var provider = sp.GetService<IMcpClientProvider>();
            if (provider == null)
            {
                logger.LogWarning("MCP client provider is not available. Using null vault tool executor - vault operations will be disabled.");
                return new NullVaultToolExecutor();
            }

            return new McpVaultToolExecutor(provider, logger);
        });

        return services;
    }
}