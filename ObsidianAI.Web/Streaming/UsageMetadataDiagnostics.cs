using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ObsidianAI.Web.Streaming
{
    internal static class UsageMetadataDiagnostics
    {
        internal readonly record struct UsageMetrics(long? InputTokens, long? OutputTokens, long? TotalTokens);

        public static bool TryExtractUsage(string metadata, out UsageMetrics metrics)
        {
            try
            {
                using var document = JsonDocument.Parse(metadata);
                if (!document.RootElement.TryGetProperty("type", out var typeElement) ||
                    !string.Equals(typeElement.GetString(), "usage", StringComparison.OrdinalIgnoreCase))
                {
                    metrics = default;
                    return false;
                }

                if (!document.RootElement.TryGetProperty("usage", out var usageElement) || usageElement.ValueKind != JsonValueKind.Object)
                {
                    metrics = default;
                    return false;
                }

                metrics = new UsageMetrics(
                    TryGetInt64(usageElement, "inputTokens"),
                    TryGetInt64(usageElement, "outputTokens"),
                    TryGetInt64(usageElement, "totalTokens"));
                return true;
            }
            catch (JsonException)
            {
                metrics = default;
                return false;
            }
        }

        public static bool TryLogUsage(string metadata, ILogger logger)
        {
            if (!TryExtractUsage(metadata, out var metrics))
            {
                return false;
            }

            logger.LogInformation(
                "Token usage → input: {Input}, output: {Output}, total: {Total}",
                metrics.InputTokens?.ToString(CultureInfo.InvariantCulture) ?? "n/a",
                metrics.OutputTokens?.ToString(CultureInfo.InvariantCulture) ?? "n/a",
                metrics.TotalTokens?.ToString(CultureInfo.InvariantCulture) ?? "n/a");

            return true;
        }

        public static string FormatUsageForDisplay(UsageMetrics metrics)
        {
            static string FormatToken(long? value) => value?.ToString("N0", CultureInfo.InvariantCulture) ?? "n/a";

            return $"Tokens — input: {FormatToken(metrics.InputTokens)}, output: {FormatToken(metrics.OutputTokens)}, total: {FormatToken(metrics.TotalTokens)}";
        }

        private static long? TryGetInt64(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            return property.ValueKind switch
            {
                JsonValueKind.Number when property.TryGetInt64(out var value) => value,
                JsonValueKind.String when long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => null
            };
        }
    }
}
