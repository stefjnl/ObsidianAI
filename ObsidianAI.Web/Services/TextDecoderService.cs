using System.Text.RegularExpressions;

namespace ObsidianAI.Web.Services;

public static class TextDecoderService
{
    private static readonly Regex UnicodeRegex = new(@"\\u([0-9A-Fa-f]{4})", RegexOptions.Compiled);

    public static string DecodeUnicode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return UnicodeRegex.Replace(text, match =>
        {
            var hex = match.Groups[1].Value;
            var codePoint = Convert.ToInt32(hex, 16);
            return char.ConvertFromUtf32(codePoint);
        });
    }

    public static string DecodeSurrogatePairs(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Handle surrogate pairs like \ud83d\udca1
        var result = UnicodeRegex.Replace(text, match =>
        {
            var hex = match.Groups[1].Value;
            var value = Convert.ToInt32(hex, 16);
            return char.ConvertFromUtf32(value);
        });

        // Combine surrogate pairs
        var chars = result.ToCharArray();
        var builder = new System.Text.StringBuilder();
        
        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsHighSurrogate(chars[i]) && i + 1 < chars.Length && char.IsLowSurrogate(chars[i + 1]))
            {
                var codePoint = char.ConvertToUtf32(chars[i], chars[i + 1]);
                builder.Append(char.ConvertFromUtf32(codePoint));
                i++; // Skip the low surrogate
            }
            else
            {
                builder.Append(chars[i]);
            }
        }

        return builder.ToString();
    }
}