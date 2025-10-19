namespace ObsidianAI.Application;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Application.Services;
using ObsidianAI.Domain.Ports;

public static class DependencyInjection
{
    public static IServiceCollection AddAIProviderApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ========================================================================
        // Configuration (Application Layer - Orchestration Settings)
        // ========================================================================
        
        services.Configure<AIProviderOptions>(configuration.GetSection(AIProviderOptions.SectionName));

        // ========================================================================
        // Application Services
        // ========================================================================
        
        services.AddScoped<IProviderSelectionStrategy, HealthBasedProviderSelection>();
        services.AddScoped<AIProvider>();

        return services;
    }
}