using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ObsidianAI.Infrastructure.LLM;

internal static class ToolStreamingFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static string CreatePayload(string toolName, string phase, object? arguments = null, object? result = null)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            toolName = "unknown";
        }

        var payload = new Dictionary<string, object?>
        {
            ["name"] = toolName,
            ["phase"] = phase
        };

        var normalizedArgs = Normalize(arguments);
        if (normalizedArgs is not null)
        {
            payload["arguments"] = normalizedArgs;
        }

        var normalizedResult = Normalize(result);
        if (normalizedResult is not null)
        {
            payload["result"] = normalizedResult;
        }

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private static object? Normalize(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return element;
        }

        if (value is string str)
        {
            if (TryParseJson(str, out var parsed))
            {
                return parsed;
            }

            return str;
        }

        var valueType = value.GetType();
        if (valueType.FullName is "System.BinaryData")
        {
            var text = value.ToString();
            if (!string.IsNullOrWhiteSpace(text) && TryParseJson(text, out var parsedBinary))
            {
                return parsedBinary;
            }

            return text;
        }

        try
        {
            return JsonSerializer.SerializeToElement(value, SerializerOptions);
        }
        catch
        {
            return value.ToString();
        }
    }

    private static bool TryParseJson(string text, out JsonElement element)
    {
        try
        {
            element = JsonSerializer.Deserialize<JsonElement>(text);
            return true;
        }
        catch (JsonException)
        {
            element = default;
            return false;
        }
    }
}
