using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ObsidianAI.Api.Configuration;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.LLM;
using ObsidianAI.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddObsidianApiServices(builder.Configuration);

var app = builder.Build();
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
var appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>();
var providerName = appSettings.Value.LLM.Provider?.Trim() ?? "LMStudio";
var modelName = llmFactory.GetModelName();
app.Logger.LogInformation("Using LLM provider: {Provider}, Model: {Model}", providerName, modelName);

await app.RunAsync();
