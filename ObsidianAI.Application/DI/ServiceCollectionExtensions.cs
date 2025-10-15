namespace ObsidianAI.Application.DI
{
    using Microsoft.Extensions.DependencyInjection;
    using ObsidianAI.Application.Services;
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
            services.AddSingleton<ObsidianAI.Domain.Services.IFileOperationExtractor, RegexFileOperationExtractor>();
            services.AddSingleton<ObsidianAI.Domain.Services.IVaultPathNormalizer, BasicVaultPathNormalizer>();
            services.AddSingleton<StartChatUseCase>();
            services.AddSingleton<StreamChatUseCase>();
            services.AddSingleton<ModifyVaultUseCase>();
            services.AddSingleton<SearchVaultUseCase>();
            return services;
        }
    }
}