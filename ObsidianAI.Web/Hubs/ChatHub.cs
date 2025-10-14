using Microsoft.AspNetCore.SignalR;
using ObsidianAI.Web.Services;
using ObsidianAI.Web.Models;
using System.Net.Http;
using System.Text;
using System.Net.Http.Json;

namespace ObsidianAI.Web.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;
    private readonly HttpClient _httpClient;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger, HttpClient httpClient)
    {
        _chatService = chatService;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task StreamMessage(string message)
    {
        _logger.LogInformation("Processing message: {Message}", message);

        try
        {
            // Call the Agent Framework API streaming endpoint
            var request = new HttpRequestMessage(HttpMethod.Post, "/chat/stream")
            {
                Content = JsonContent.Create(new { message })
            };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Read the streaming response
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var fullResponse = new StringBuilder();
            var buffer = new char[128];  // Larger buffer for batching tokens
            int charsRead;

            // Stream in chunks to reduce SignalR overhead while maintaining streaming feel
            while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var chunk = new string(buffer, 0, charsRead);
                fullResponse.Append(chunk);

                // Send chunk instead of single character
                await Clients.Caller.SendAsync("ReceiveToken", chunk);

                // Small delay to prevent UI overload
                await Task.Delay(10);
            }

            // Send completion event
            await Clients.Caller.SendAsync("MessageComplete", fullResponse.ToString());

            _logger.LogInformation("Message processing complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
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