using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ObsidianAI.Infrastructure.LLM
{
    public static class UsageMetadataBuilder
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string? TryCreateUsagePayload(object? usageDetails)
        {
            var usage = TryCreateUsageObject(usageDetails);
            if (usage is null)
            {
                return null;
            }

            return JsonSerializer.Serialize(new
            {
                type = "usage",
                usage
            }, SerializerOptions);
        }

        public static Dictionary<string, object?>? TryCreateUsageObject(object? usageDetails)
        {
            if (usageDetails is null)
            {
                return null;
            }

            var usage = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            TryAddNumber(usage, usageDetails, "InputTokens", "inputTokens");
            TryAddNumber(usage, usageDetails, "OutputTokens", "outputTokens");
            TryAddNumber(usage, usageDetails, "TotalTokens", "totalTokens");
            TryAddNumber(usage, usageDetails, "ReasoningTokens", "reasoningTokens");
            TryAddNumber(usage, usageDetails, "TotalTokensIncludingToolCalls", "totalTokensIncludingToolCalls");
            TryAddNumber(usage, usageDetails, "CacheReadTokens", "cacheReadTokens");
            TryAddNumber(usage, usageDetails, "CacheWriteTokens", "cacheWriteTokens");
            TryAddNumber(usage, usageDetails, "InvocationCount", "invocationCount");

            var additionalMetrics = TryGetDictionary(usageDetails, "AdditionalMetrics");
            if (additionalMetrics is { Count: > 0 })
            {
                usage["additionalMetrics"] = additionalMetrics;
            }

            var additionalProperties = TryGetDictionary(usageDetails, "AdditionalProperties");
            if (additionalProperties is { Count: > 0 })
            {
                usage["additionalProperties"] = additionalProperties;
            }

            return usage.Count == 0 ? null : usage;
        }

        private static void TryAddNumber(IDictionary<string, object?> target, object source, string propertyName, string jsonKey)
        {
            var numeric = ConvertToNumeric(GetPropertyValue(source, propertyName));
            if (numeric is not null)
            {
                target[jsonKey] = numeric;
            }
        }

        private static object? ConvertToNumeric(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is int or long or short or byte)
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }

            if (value is double or float or decimal)
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }

            if (value is string stringValue)
            {
                if (long.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longResult))
                {
                    return longResult;
                }

                if (double.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleResult))
                {
                    return doubleResult;
                }
            }

            return null;
        }

        private static Dictionary<string, object?>? TryGetDictionary(object source, string propertyName)
        {
            var value = GetPropertyValue(source, propertyName);
            if (value is null)
            {
                return null;
            }

            if (value is IEnumerable<KeyValuePair<string, object?>> kvps)
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in kvps)
                {
                    dict[kvp.Key] = kvp.Value;
                }

                return dict;
            }

            if (value is IDictionary dictionary)
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is string key)
                    {
                        dict[key] = entry.Value;
                    }
                }

                return dict;
            }

            return null;
        }

        private static object? GetPropertyValue(object source, string propertyName)
        {
            var property = source.GetType().GetProperty(propertyName);
            return property?.GetValue(source);
        }
    }
}
