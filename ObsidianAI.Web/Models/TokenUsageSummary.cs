using System.Globalization;

namespace ObsidianAI.Web.Models
{
    /// <summary>
    /// Represents token usage metrics for a single assistant turn.
    /// </summary>
    public sealed record TokenUsageSummary(long? InputTokens, long? OutputTokens, long? TotalTokens)
    {
        public string FormatForDisplay() =>
            $"Tokens — input: {Format(InputTokens)}, output: {Format(OutputTokens)}, total: {Format(TotalTokens)}";

        private static string Format(long? value) =>
            value?.ToString("N0", CultureInfo.InvariantCulture) ?? "n/a";
    }
}
