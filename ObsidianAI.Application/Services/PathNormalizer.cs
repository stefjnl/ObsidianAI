using System.Text.RegularExpressions;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Provides helpers for emoji-aware vault path normalization that can be reused across layers.
/// </summary>
public static class PathNormalizer
{
    // Matches a wide range of emoji glyphs so comparisons can ignore them reliably.
    private static readonly Regex EmojiPattern = new Regex(
        @"[\u2600-\u27BF]|[\uD83C][\uDC00-\uDFFF]|[\uD83D][\uDC00-\uDFFF]|[\uD83E][\uDD00-\uDDFF]|[\u2700-\u27BF]|[\uD83C-\uDBFF][\uDC00-\uDFFF]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Removes emoji glyphs from the provided string and trims the result.
    /// </summary>
    public static string RemoveEmojis(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return EmojiPattern.Replace(input, string.Empty).Trim();
    }

    /// <summary>
    /// Generates a normalized key that can be used for comparing user input against vault paths.
    /// Emojis are removed, the string is lower-cased, trimmed, and internal spaces are collapsed.
    /// </summary>
    public static string NormalizePath(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return RemoveEmojis(input)
            .ToLowerInvariant()
            .Trim()
            .Replace(" ", string.Empty);
    }
}
