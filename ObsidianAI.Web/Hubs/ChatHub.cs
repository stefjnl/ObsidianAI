using Microsoft.AspNetCore.SignalR;
using ObsidianAI.Web.Services;
using ObsidianAI.Web.Models;

namespace ObsidianAI.Web.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task StreamMessage(string message)
    {
        _logger.LogInformation("Processing message: {Message}", message);

        try
        {
            var simulatedResponse = "I'm processing your message: \"" + message + "\". " +
                "This is a simulated streaming response. In the actual implementation, " +
                "this would connect to the Agent Framework and stream tokens as they arrive " +
                "from the LM Studio API.";

            if (message.Contains("move", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("reorganize", StringComparison.OrdinalIgnoreCase))
            {
                simulatedResponse = "I'll move the files as requested. Please confirm this action.\n\n" +
                    "📄 Move project-retrospective.md → Archive/2024/\n" +
                    "📄 Move sprint-review.md → Archive/2024/\n" +
                    "📄 Move team-meeting.md → Archive/2024/\n" +
                    "📄 Move planning-session.md → Archive/2024/\n" +
                    "📄 Move retrospective-2023.md → Archive/2024/\n" +
                    "📄 Move quarterly-summary.md → Archive/2024/\n" +
                    "📄 Move action-items.md → Archive/2024/\n\n" +
                    "Click Confirm to execute these actions or Cancel to abort.";
            }
            else if (message.Contains("search", StringComparison.OrdinalIgnoreCase))
            {
                simulatedResponse = "I found the following notes in your vault:\n\n" +
                    "Meeting Notes 2024-01-15.md: \"...discussed project timeline and deliverables for Q1...\"\n" +
                    "Project Roadmap.md: \"...key milestones include prototype completion by end of February...\"\n" +
                    "Action Items.md: \"...follow up with design team about UI mockups...\"\n" +
                    "Team Retrospective.md: \"...identified communication gaps between departments...\"\n\n" +
                    "Would you like me to open any of these files?";
            }

            var words = simulatedResponse.Split(' ');
            var fullResponse = string.Empty;

            foreach (var word in words)
            {
                await Task.Delay(50);
                var token = word + " ";
                fullResponse += token;

                // Send each token via SignalR event
                await Clients.Caller.SendAsync("ReceiveToken", token);
            }

            // Send completion event
            await Clients.Caller.SendAsync("MessageComplete", fullResponse);

            _logger.LogInformation("Message processing complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            await Clients.Caller.SendAsync("Error", ex.Message);
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