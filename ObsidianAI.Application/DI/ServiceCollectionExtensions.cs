namespace ObsidianAI.Application.DI
{
    using Microsoft.Extensions.DependencyInjection;
    using ObsidianAI.Application.UseCases;

    /// <summary>
    /// Extension methods for registering Application layer services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds ObsidianAI Application services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection with added services.</returns>
        public static IServiceCollection AddObsidianApplication(this IServiceCollection services)
        {
            services.AddScoped<StreamChatUseCase>();
            return services;
        }
    }
}