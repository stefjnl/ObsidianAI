using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Shared helpers for parsing MCP tool responses related to vault listings.
/// </summary>
public static class VaultToolResponseParser
{
    /// <summary>
    /// Extracts raw vault paths from the supplied MCP content blocks.
    /// </summary>
    /// <param name="content">Content blocks returned by the MCP tool invocation.</param>
    /// <returns>A list of raw vault paths preserving emojis and trailing separators.</returns>
    public static IReadOnlyList<string> ExtractPaths(IEnumerable<ContentBlock>? content)
    {
        if (content is null)
        {
            return Array.Empty<string>();
        }

        var textBlock = content.OfType<TextContentBlock>().FirstOrDefault();
        if (textBlock is null || string.IsNullOrWhiteSpace(textBlock.Text))
        {
            return Array.Empty<string>();
        }

        return ParsePathsFromText(textBlock.Text);
    }

    /// <summary>
    /// Parses raw vault paths from a text payload returned by MCP tooling.
    /// </summary>
    /// <param name="text">Raw text response.</param>
    /// <returns>List of vault paths.</returns>
    public static IReadOnlyList<string> ParsePathsFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return Array.Empty<string>();
        }

        if (TryParseJsonArray(trimmed, out var parsed))
        {
            return parsed;
        }

        var segments = trimmed
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Trim())
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return segments;
    }

    private static bool TryParseJsonArray(string text, out IReadOnlyList<string> paths)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var values = doc.RootElement
                    .EnumerateArray()
                    .Select(element => element.ValueKind == JsonValueKind.String ? element.GetString() : null)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                paths = values;
                return values.Length > 0;
            }
        }
        catch (JsonException)
        {
            // Ignore malformed JSON and fall back to newline parsing.
        }

        paths = Array.Empty<string>();
        return false;
    }
}
