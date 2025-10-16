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
            services.AddSingleton<IVaultPathResolver, VaultPathResolver>();

            services.AddScoped<CreateConversationUseCase>();
            services.AddScoped<LoadConversationUseCase>();
            services.AddScoped<ListConversationsUseCase>();
            services.AddScoped<ArchiveConversationUseCase>();
            services.AddScoped<UpdateConversationUseCase>();
            services.AddScoped<DeleteConversationUseCase>();

            services.AddScoped<StartChatUseCase>();
            services.AddScoped<StreamChatUseCase>();
            services.AddScoped<ModifyVaultUseCase>();
            services.AddScoped<SearchVaultUseCase>();
            services.AddScoped<UpdateMessageArtifactsUseCase>();
            return services;
        }
    }
}