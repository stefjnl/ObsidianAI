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
        // Match pairs of \uXXXX sequences
        var pairRegex = new Regex(@"\\u([dD][89aAbB][0-9A-Fa-f]{2})\\u([dD][c-fC-F][0-9A-Fa-f]{2})", RegexOptions.Compiled);

        // First, decode surrogate pairs
        text = pairRegex.Replace(text, match =>
        {
            try
            {
                var high = Convert.ToInt32(match.Groups[1].Value, 16);
                var low = Convert.ToInt32(match.Groups[2].Value, 16);

                // Validate surrogate range
                if (high >= 0xD800 && high <= 0xDBFF && low >= 0xDC00 && low <= 0xDFFF)
                {
                    var codePoint = 0x10000 + ((high - 0xD800) * 0x400) + (low - 0xDC00);
                    return char.ConvertFromUtf32(codePoint);
                }

                // If not valid surrogate pair, return original
                return match.Value;
            }
            catch
            {
                // If conversion fails, return original text
                return match.Value;
            }
        });

        // Then decode remaining single \uXXXX sequences (non-surrogate characters)
        text = UnicodeRegex.Replace(text, match =>
        {
            try
            {
                var hex = match.Groups[1].Value;
                var value = Convert.ToInt32(hex, 16);

                // Skip surrogate values (they should have been handled above)
                if (value >= 0xD800 && value <= 0xDFFF)
                {
                    return match.Value; // Keep original if it's an unpaired surrogate
                }

                return char.ConvertFromUtf32(value);
            }
            catch
            {
                return match.Value;
            }
        });

        return text;
    }
}