using Microsoft.Extensions.Options;
using ObsidianAI.Api.Configuration;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.LLM;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddObsidianApiServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapObsidianEndpoints();
app.MapHealthChecks("/healthz");

var llmFactory = app.Services.GetRequiredService<ILlmClientFactory>();
var appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>();
var providerName = appSettings.Value.LLM.Provider?.Trim() ?? "LMStudio";
var modelName = llmFactory.GetModelName();
app.Logger.LogInformation("Using LLM provider: {Provider}, Model: {Model}", providerName, modelName);

app.Run();
