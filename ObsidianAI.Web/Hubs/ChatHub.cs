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
            int lineCount = 0;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineCount++;

                // Log with escaped characters to see actual structure
                var escapedLine = line.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                _logger.LogInformation("Line #{LineNum}: '{EscapedLine}'", lineCount,
                    escapedLine.Length > 100 ? escapedLine.Substring(0, 100) + "..." : escapedLine);

                if (line.StartsWith("event: "))
                {
                    currentEvent = line.Substring(7).Trim();
                }
                else if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);

                    if (data == "[DONE]")
                    {
                        var finalResponse = TextDecoderService.DecodeSurrogatePairs(fullResponse.ToString());

                        // Log final response with escaped characters
                        var escapedFinal = finalResponse.Replace("\n", "\\n").Replace("\r", "\\r");
                        _logger.LogWarning("Final accumulated response (Length={Length}): '{EscapedResponse}'",
                            finalResponse.Length,
                            escapedFinal.Length > 500 ? escapedFinal.Substring(0, 500) + "..." : escapedFinal);

                        await Clients.Caller.SendAsync("MessageComplete", finalResponse);
                        completionSent = true;
                        break;
                    }

                    if (currentEvent == "tool_call")
                    {
                        await Clients.Caller.SendAsync("StatusUpdate", new { type = "tool_call", tool = data });
                        currentEvent = null;
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
                        var decodedChunk = TextDecoderService.DecodeSurrogatePairs(data);
                        var fullResponseStr = fullResponse.ToString();

                        // Add newline before list items if previous content exists and doesn't end with newline
                        if (fullResponse.Length > 0 &&
                            !fullResponseStr.EndsWith("\n") &&
                            decodedChunk.TrimStart().StartsWith("*"))
                        {
                            fullResponse.Append("\n\n");
                            await Clients.Caller.SendAsync("ReceiveToken", "\n\n");
                        }
                        // Add newline after list when transitioning to regular text
                        // Only trigger if we already have a newline (list item ended) and new content isn't a list item
                        else if (fullResponse.Length > 0 &&
                                 fullResponseStr.EndsWith("\n") &&
                                 !fullResponseStr.EndsWith("\n\n") &&
                                 !decodedChunk.TrimStart().StartsWith("*"))
                        {
                            var lastLine = fullResponseStr.Split('\n').Reverse().Skip(1).FirstOrDefault()?.TrimStart() ?? "";
                            if (lastLine.StartsWith("*"))
                            {
                                fullResponse.Append("\n");
                                await Clients.Caller.SendAsync("ReceiveToken", "\n");
                            }
                        }

                        fullResponse.Append(decodedChunk);
                        await Clients.Caller.SendAsync("ReceiveToken", decodedChunk);
                        await Task.Delay(10);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    var decodedChunk = TextDecoderService.DecodeSurrogatePairs(line);
                    var fullResponseStr = fullResponse.ToString();

                    // Add newline before list items if previous content exists
                    if (fullResponse.Length > 0 &&
                        !fullResponseStr.EndsWith("\n") &&
                        decodedChunk.TrimStart().StartsWith("*"))
                    {
                        fullResponse.Append("\n\n");
                        await Clients.Caller.SendAsync("ReceiveToken", "\n\n");
                    }
                    // Add newline after list when transitioning to regular text
                    // Only trigger if we already have a newline (list item ended) and new content isn't a list item
                    else if (fullResponse.Length > 0 &&
                             fullResponseStr.EndsWith("\n") &&
                             !fullResponseStr.EndsWith("\n\n") &&
                             !decodedChunk.TrimStart().StartsWith("*"))
                    {
                        var lastLine = fullResponseStr.Split('\n').Reverse().Skip(1).FirstOrDefault()?.TrimStart() ?? "";
                        if (lastLine.StartsWith("*"))
                        {
                            fullResponse.Append("\n");
                            await Clients.Caller.SendAsync("ReceiveToken", "\n");
                        }
                    }

                    fullResponse.Append(decodedChunk);
                    // Don't append extra newline - it breaks multiline content
                    // fullResponse.Append("\n");

                    await Clients.Caller.SendAsync("ReceiveToken", decodedChunk);
                    await Task.Delay(10);
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    currentEvent = null;
                }
            }

            if (!completionSent && fullResponse.Length > 0)
            {
                var finalResponse = TextDecoderService.DecodeSurrogatePairs(fullResponse.ToString());

                // Log final response with escaped characters
                var escapedFinal = finalResponse.Replace("\n", "\\n").Replace("\r", "\\r");
                _logger.LogWarning("Final accumulated response (Length={Length}): '{EscapedResponse}'",
                    finalResponse.Length,
                    escapedFinal.Length > 500 ? escapedFinal.Substring(0, 500) + "..." : escapedFinal);

                await Clients.Caller.SendAsync("MessageComplete", finalResponse);
            }

            _logger.LogInformation("Message streaming complete (Total lines: {LineCount})", lineCount);
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