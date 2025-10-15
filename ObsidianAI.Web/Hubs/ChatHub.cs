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
            var request = new HttpRequestMessage(HttpMethod.Post, "/chat/stream")
            {
                Content = JsonContent.Create(new { message })
            };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var fullResponse = new StringBuilder();
            var buffer = new char[128];
            int charsRead;

            while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var chunk = new string(buffer, 0, charsRead);
                
                // DECODE UNICODE BEFORE SENDING
                var decodedChunk = TextDecoderService.DecodeSurrogatePairs(chunk);
                fullResponse.Append(decodedChunk);

                await Clients.Caller.SendAsync("ReceiveToken", decodedChunk);
                await Task.Delay(10);
            }

            var finalResponse = TextDecoderService.DecodeSurrogatePairs(fullResponse.ToString());
            await Clients.Caller.SendAsync("MessageComplete", finalResponse);

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