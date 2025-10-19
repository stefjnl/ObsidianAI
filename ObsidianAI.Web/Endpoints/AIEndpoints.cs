namespace ObsidianAI.Web.Endpoints;

using ObsidianAI.Application.DTOs;
using ObsidianAI.Application.Services;
using Microsoft.AspNetCore.Mvc;

public static class AIEndpoints
{
    public static IEndpointRouteBuilder MapAIEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai")
            .WithTags("AI");

        group.MapPost("/generate", GenerateContent)
            .WithName("GenerateContent")
            .WithOpenApi();

        group.MapGet("/providers", GetProviders)
            .WithName("GetProviders")
            .WithOpenApi();

        group.MapGet("/providers/{name}/health", CheckProviderHealth)
            .WithName("CheckProviderHealth")
            .WithOpenApi();

        group.MapGet("/providers/{name}/models", GetProviderModels)
            .WithName("GetProviderModels")
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GenerateContent(
        [FromBody] GenerateContentRequest request,
        [FromServices] AIProvider aiProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await aiProvider.GenerateContentAsync(
                request.Prompt,
                request.Context,
                request.Provider,
                request.Model,
                cancellationToken);

            return Results.Ok(new GenerateContentResponse(
                content,
                request.Provider ?? "auto-selected",
                request.Model ?? "default",
                0)); // Token count would need to be tracked from AIResponse
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Failed to generate content");
        }
    }

    private static async Task<IResult> GetProviders(
        [FromServices] AIProvider aiProvider,
        CancellationToken cancellationToken)
    {
        var providers = await aiProvider.GetAvailableProvidersAsync(cancellationToken);
        return Results.Ok(providers);
    }

    private static async Task<IResult> CheckProviderHealth(
        string name,
        [FromServices] AIProvider aiProvider,
        CancellationToken cancellationToken)
    {
        var isHealthy = await aiProvider.IsProviderAvailableAsync(name, cancellationToken);
        return Results.Ok(new ProviderHealthResponse(name, isHealthy));
    }

    private static async Task<IResult> GetProviderModels(
        string name,
        [FromServices] AIProvider aiProvider,
        CancellationToken cancellationToken)
    {
        var models = await aiProvider.GetModelsForProviderAsync(name, cancellationToken);
        return Results.Ok(models);
    }
}