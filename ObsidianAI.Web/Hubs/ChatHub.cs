using Microsoft.AspNetCore.SignalR;
using ObsidianAI.Web.Services;
using ObsidianAI.Web.Models;

namespace ObsidianAI.Web.Hubs;

/// <summary>
/// SignalR hub for streaming chat messages from the AI agent.
/// </summary>
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Streams a message response from the AI agent token by token.
    /// </summary>
    /// <param name="message">The user message to process.</param>
    /// <returns>An async enumerable of string tokens.</returns>
    public async IAsyncEnumerable<string> StreamMessage(string message)
    {
        _logger.LogInformation("Processing message: {Message}", message);
        
        // For now, simulate a streaming response
        // In a real implementation, this would call the Agent Framework's RunStreamingAsync method
        var simulatedResponse = "I'm processing your message: \"" + message + "\". " +
            "This is a simulated streaming response. In the actual implementation, " +
            "this would connect to the Agent Framework and stream tokens as they arrive " +
            "from the LM Studio API.";
        
        // Check if this is a move/reorganize request
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
        // Check if this is a search request
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
            // Simulate a delay to mimic streaming behavior
            await Task.Delay(50);
            
            // Yield each word followed by a space
            var token = word + " ";
            fullResponse += token;
            yield return token;
        }
        
        // Signal that the message is complete
        await Clients.Caller.SendAsync("MessageComplete", fullResponse);
    }
    
    // Add error handling through the client connection
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", "Connected to ChatHub");
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error");
        }
        await base.OnDisconnectedAsync(exception);
    }
    
    // These methods have been moved above
}