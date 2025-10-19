namespace ObsidianAI.Web.Models;

/// <summary>
/// Represents a quick action button displayed in the chat input area
/// </summary>
public record QuickAction(string Label, string Prompt)
{
    /// <summary>
    /// Display text for the quick action button
    /// </summary>
    public string Label { get; init; } = Label;

    /// <summary>
    /// The prompt that will be sent when the action is clicked
    /// </summary>
    public string Prompt { get; init; } = Prompt;

    /// <summary>
    /// Optional prefix text prepended to the prompt
    /// </summary>
    public string Prefix { get; init; } = string.Empty;
}
