using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ObsidianAI.Api.Configuration;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.LLM;
using ObsidianAI.Infrastructure.Data;
using ObsidianAI.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddObsidianApiServices(builder.Configuration);

// Validate required configuration on startup
var appSettings = builder.Configuration.Get<ObsidianAI.Infrastructure.Configuration.AppSettings>();
if (appSettings == null)
{
    throw new InvalidOperationException("AppSettings configuration is missing or invalid.");
}

var provider = appSettings.LLM.Provider?.Trim() ?? "LMStudio";
if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
{
    var apiKey = appSettings.LLM.OpenRouter?.ApiKey?.Trim();
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new InvalidOperationException(
            "OpenRouter API key is not configured. " +
            "Set it via user secrets (dotnet user-secrets set \"LLM:OpenRouter:ApiKey\" \"your-key\") " +
            "or environment variable LLM__OpenRouter__ApiKey.");
    }
}
else if (provider.Equals("LMStudio", StringComparison.OrdinalIgnoreCase))
{
    var apiKey = appSettings.LLM.LMStudio?.ApiKey?.Trim();
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new InvalidOperationException(
            "LMStudio API key is not configured. " +
            "Set it via user secrets (dotnet user-secrets set \"LLM:LMStudio:ApiKey\" \"your-key\") " +
            "or environment variable LLM__LMStudio__ApiKey.");
    }
}
else
{
    throw new InvalidOperationException(
        $"Unknown LLM provider '{provider}'. Supported providers: LMStudio, OpenRouter.");
}

var app = builder.Build();

// Register global exception handler FIRST to catch all unhandled exceptions
app.UseGlobalExceptionHandler();

app.MapDefaultEndpoints();
app.MapObsidianEndpoints();
app.MapActionCardEndpoints();
app.MapHealthChecks("/healthz");

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<ObsidianAIDbContext>();
	await dbContext.Database.MigrateAsync();
}

var llmFactory = app.Services.GetRequiredService<ILlmClientFactory>();
var appSettingsOptions = app.Services.GetRequiredService<IOptions<AppSettings>>();
var providerName = appSettingsOptions.Value.LLM.Provider?.Trim() ?? "LMStudio";
var modelName = llmFactory.GetModelName();
app.Logger.LogInformation("Using LLM provider: {Provider}, Model: {Model}", providerName, modelName);

await app.RunAsync();
