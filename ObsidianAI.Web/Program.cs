using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ObsidianAI.Application;
using ObsidianAI.Application.DI;
using ObsidianAI.Application.Services;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Agents;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.Data;
using ObsidianAI.Infrastructure.DI;
using ObsidianAI.Infrastructure.LLM;
using ObsidianAI.Infrastructure.Middleware;
using ObsidianAI.Infrastructure.Services;
using ObsidianAI.Web.Components;
using ObsidianAI.Web.Endpoints;
using ObsidianAI.Web.HealthChecks;
using ObsidianAI.Web.Hubs;
using ObsidianAI.Web.Services;
using McpClientService = ObsidianAI.Web.Services.McpClientService;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (from Aspire)
builder.AddServiceDefaults();

// Configure settings
builder.Services.Configure<AppSettings>(builder.Configuration);

// Add Razor Components and SignalR
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();

// Add Application layer
builder.Services.AddObsidianApplication();

// Add Infrastructure layer with LLM and MCP
builder.Services.AddObsidianAI(builder.Configuration);

// Add MCP services
builder.Services.AddSingleton<McpClientService>();
builder.Services.AddHostedService<McpClientService>(sp => sp.GetRequiredService<McpClientService>());
builder.Services.AddSingleton<IMcpClientProvider>(sp => sp.GetRequiredService<McpClientService>());
builder.Services.AddSingleton<IMicrosoftLearnMcpClientProvider, MicrosoftLearnMcpClient>();

// Add HTTP context accessor for endpoint access
builder.Services.AddHttpContextAccessor();

// Add OpenAPI for REST endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add antiforgery
builder.Services.AddAntiforgery();

// Add in-memory cache for AIProvider
builder.Services.AddMemoryCache();

// Register AI Provider infrastructure and application layers
builder.Services.AddAIProviderInfrastructure(builder.Configuration);
builder.Services.AddAIProviderApplication(builder.Configuration);

// Register health checks with all providers
builder.Services.AddHealthChecks()
    .AddCheck<ObsidianAI.Infrastructure.HealthChecks.McpHealthCheck>("mcp")
    .AddCheck<ObsidianAI.Infrastructure.HealthChecks.LlmHealthCheck>("llm")
    .AddCheck<MicrosoftLearnHealthCheck>("microsoft-learn");

// Register VaultResizeService for UI resize functionality
builder.Services.AddScoped<IVaultResizeService, VaultResizeService>();

// Register HTTP client-based services for Blazor components
builder.Services.AddHttpClient<IChatService, ChatService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5244");
});
builder.Services.AddHttpClient<IVaultService, VaultService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5244");
});
builder.Services.AddHttpClient("ObsidianAI.Api", client =>
{
    client.BaseAddress = new Uri("http://localhost:5244");
});

// Validate required configuration on startup
var appSettings = builder.Configuration.Get<AppSettings>();
if (appSettings == null)
{
    throw new InvalidOperationException("AppSettings configuration is missing or invalid.");
}

var provider = appSettings.LLM.Provider?.Trim() ?? "LMStudio";
if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
{
    var apiKey = builder.Configuration["OpenRouter:ApiKey"];
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new InvalidOperationException(
            "OpenRouter API key is not configured. " +
            "Set it via user secrets (dotnet user-secrets set \"OpenRouter:ApiKey\" \"your-key\") " +
            "or environment variable OpenRouter__ApiKey.");
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
else if (provider.Equals("NanoGPT", StringComparison.OrdinalIgnoreCase))
{
    // NanoGPT validation - API key is optional for local deployments
    var endpoint = appSettings.LLM.NanoGPT?.Endpoint?.Trim();
    if (string.IsNullOrEmpty(endpoint))
    {
        throw new InvalidOperationException(
            "NanoGPT endpoint is not configured. " +
            "Set it via appsettings.json or environment variable LLM__NanoGPT__Endpoint.");
    }
    var apiKey = builder.Configuration["NanoGpt:ApiKey"];
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new InvalidOperationException(
            "NanoGPT API key is not configured. " +
            "Set it via user secrets (dotnet user-secrets set \"NanoGpt:ApiKey\" \"your-key\") " +
            "or environment variable NanoGpt__ApiKey.");
    }
}
else
{
    throw new InvalidOperationException(
        $"Unknown LLM provider '{provider}'. Supported providers: LMStudio, OpenRouter, NanoGPT.");
}

var app = builder.Build();

// Register global exception handler FIRST to catch all unhandled exceptions
app.UseGlobalExceptionHandler();

// Configure the HTTP request pipeline
app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapStaticAssets();

// Map Blazor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub
app.MapHub<ChatHub>("/chathub");

// Map REST API endpoints (for future external consumers)
app.MapObsidianEndpoints();
app.MapActionCardEndpoints();
// Map AI endpoints
app.MapAIEndpoints();
app.MapHealthChecks("/healthz");

// Map OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Run database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ObsidianAIDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Log LLM provider configuration
var agentFactory = app.Services.GetRequiredService<IAIAgentFactory>();
var appSettingsOptions = app.Services.GetRequiredService<IOptions<AppSettings>>();
var providerName = agentFactory.ProviderName;
var modelName = agentFactory.GetModelName();
app.Logger.LogInformation("Using LLM provider: {Provider}, Model: {Model}", providerName, modelName);

await app.RunAsync();