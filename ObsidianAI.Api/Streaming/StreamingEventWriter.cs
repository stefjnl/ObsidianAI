using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ObsidianAI.Domain.Models;

namespace ObsidianAI.Api.Streaming
{
    /// <summary>
    /// Provides centralized logic for writing Server-Sent Events (SSE) for chat streaming.
    /// </summary>
    public static class StreamingEventWriter
    {
        /// <summary>
        /// Writes chat stream events as Server-Sent Events to the HTTP response.
        /// </summary>
        /// <param name="context">The HTTP context for the response.</param>
        /// <param name="events">The asynchronous enumerable of chat stream events.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task WriteAsync(HttpContext context, IAsyncEnumerable<ChatStreamEvent> events, ILogger logger, CancellationToken ct = default)
        {
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";

            var updateCount = 0;

            try
            {
                await foreach (var update in events)
                {
                    updateCount++;

                    if (update.Kind == ChatStreamEventKind.ToolCall)
                    {
                        logger.LogInformation("Sending tool_call event: {ToolName}", update.ToolName);
                        await context.Response.WriteAsync($"event: tool_call\ndata: {update.ToolName}\n\n", ct);
                        await context.Response.Body.FlushAsync(ct);
                    }
                    else if (update.Kind == ChatStreamEventKind.Text && !string.IsNullOrEmpty(update.Text))
                    {
                        var escapedContent = update.Text.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                        logger.LogInformation("Sending token #{Count}: RAW='{Escaped}' (Length={Length})",
                            updateCount,
                            escapedContent.Length > 100 ? escapedContent.Substring(0, 100) + "..." : escapedContent,
                            update.Text.Length);

                        await context.Response.WriteAsync($"data: {update.Text}\n\n", ct);
                        await context.Response.Body.FlushAsync(ct);
                    }
                }

                logger.LogInformation("Stream complete. Sending [DONE] marker after {Count} updates", updateCount);
                await context.Response.WriteAsync("data: [DONE]\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during streaming after {Count} updates", updateCount);
                await context.Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
            }
        }
    }
}