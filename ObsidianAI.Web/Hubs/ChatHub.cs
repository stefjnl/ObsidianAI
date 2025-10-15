using Microsoft.AspNetCore.SignalR;
using ObsidianAI.Web.Services;
using ObsidianAI.Web.Models;
using System.Net.Http;
using System.Text;
using System.Net.Http.Json;
using System.Text.Json;

namespace ObsidianAI.Web.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger, IHttpClientFactory httpClientFactory)
    {
        _chatService = chatService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task StreamMessage(string message, List<ChatMessage> history)
    {
        _logger.LogInformation("Processing streaming message: {Message}", message);

        try
        {
            var httpClient = _httpClientFactory.CreateClient("ObsidianAI.Api");

            var requestBody = new
            {
                message = message,
                history = history?.Select(h => new { role = h.Sender == MessageSender.User ? "user" : "assistant", content = h.Content }).ToList()
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/chat/stream")
            {
                Content = JsonContent.Create(requestBody)
            };

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var fullResponse = new StringBuilder();
            string? line;
            string? currentEvent = null;
            bool completionSent = false;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                // Log each line for debugging
                _logger.LogInformation("SSE Line: '{Line}'", line);

                // Parse SSE format: lines starting with "event:" or "data:"
                if (line.StartsWith("event: "))
                {
                    currentEvent = line.Substring(7).Trim();
                    _logger.LogInformation("SSE Event Type: '{EventType}'", currentEvent);
                }
                else if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    _logger.LogInformation("SSE Data: '{Data}'", data.Length > 50 ? data.Substring(0, 50) + "..." : data);

                    // Check for completion marker
                    if (data == "[DONE]")
                    {
                        var finalResponse = TextDecoderService.DecodeSurrogatePairs(fullResponse.ToString());
                        await Clients.Caller.SendAsync("MessageComplete", finalResponse);
                        completionSent = true;
                        _logger.LogInformation("Sent MessageComplete event with [DONE] marker");
                        break;
                    }

                    // Handle different event types
                    if (currentEvent == "tool_call")
                    {
                        await Clients.Caller.SendAsync("StatusUpdate", new { type = "tool_call", tool = data });
                        currentEvent = null; // Reset event type
                    }
                    else if (currentEvent == "error")
                    {
                        await Clients.Caller.SendAsync("Error", data);
                        currentEvent = null;
                        completionSent = true;
                        break;
                    }
                    else
                    {
                        // Regular text token
                        var decodedChunk = TextDecoderService.DecodeSurrogatePairs(data);
                        fullResponse.Append(decodedChunk);
                        await Clients.Caller.SendAsync("ReceiveToken", decodedChunk);
                        await Task.Delay(10); // Small delay to prevent UI overload
                    }
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    // Handle lines without 'data:' prefix (LLM fragmentation)
                    _logger.LogInformation("SSE Line without prefix: '{Line}'", line);

                    var decodedChunk = TextDecoderService.DecodeSurrogatePairs(line);
                    fullResponse.Append(decodedChunk);
                    await Clients.Caller.SendAsync("ReceiveToken", decodedChunk);
                    await Task.Delay(10);
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    // Empty line marks end of SSE message
                    currentEvent = null;
                }
            }

            // CRITICAL: If we exit the loop without receiving [DONE], send completion anyway
            if (!completionSent && fullResponse.Length > 0)
            {
                _logger.LogWarning("Stream ended without [DONE] marker. Sending MessageComplete anyway.");
                var finalResponse = TextDecoderService.DecodeSurrogatePairs(fullResponse.ToString());
                await Clients.Caller.SendAsync("MessageComplete", finalResponse);
            }

            _logger.LogInformation("Message streaming complete (completionSent: {CompletionSent}, responseLength: {Length})",
                completionSent, fullResponse.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing streaming message");
            await Clients.Caller.SendAsync("Error", $"Failed to process message: {ex.Message}");
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}