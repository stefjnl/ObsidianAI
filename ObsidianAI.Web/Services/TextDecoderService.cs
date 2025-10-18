using System.Text.RegularExpressions;
using System.Text;

namespace ObsidianAI.Web.Services;

public static class TextDecoderService
{
    private static readonly Regex UnicodeRegex = new(@"\\u([0-9A-Fa-f]{4})", RegexOptions.Compiled);

    /// <summary>
    /// Unescapes JSON-escaped strings including standard escape sequences and Unicode codes.
    /// </summary>
    public static string UnescapeJson(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder(text.Length);
        var i = 0;

        while (i < text.Length)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                var nextChar = text[i + 1];
                switch (nextChar)
                {
                    case 'n':
                        result.Append('\n');
                        i += 2;
                        break;
                    case 'r':
                        result.Append('\r');
                        i += 2;
                        break;
                    case 't':
                        result.Append('\t');
                        i += 2;
                        break;
                    case '"':
                        result.Append('"');
                        i += 2;
                        break;
                    case '\\':
                        result.Append('\\');
                        i += 2;
                        break;
                    case '/':
                        result.Append('/');
                        i += 2;
                        break;
                    case 'b':
                        result.Append('\b');
                        i += 2;
                        break;
                    case 'f':
                        result.Append('\f');
                        i += 2;
                        break;
                    case 'u' when i + 5 < text.Length:
                        // Handle \uXXXX Unicode escape
                        var hex = text.Substring(i + 2, 4);
                        if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var codePoint))
                        {
                            result.Append((char)codePoint);
                            i += 6;
                        }
                        else
                        {
                            // Invalid Unicode escape, keep original
                            result.Append(text[i]);
                            i++;
                        }
                        break;
                    default:
                        // Unknown escape sequence, keep both characters
                        result.Append(text[i]);
                        i++;
                        break;
                }
            }
            else
            {
                result.Append(text[i]);
                i++;
            }
        }

        return result.ToString();
    }

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