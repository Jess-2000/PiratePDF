using System.Globalization;
using System.Text;
using BlackFlag.Engine.Filters;

namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Parses the mapping constructs required from PDF CMap programs.</summary>
public sealed class PdfCMapParser
{
    // A complete two-byte CID map needs fewer than 140,000 generated tokens.
    // Cap token objects separately from decoded bytes and per-range expansion.
    internal const int MaximumTokenCount = 1_000_000;

    /// <summary>Parses a decoded CMap byte sequence.</summary>
    public static PdfCMap Parse(ReadOnlySpan<byte> decodedBytes) =>
        Parse(decodedBytes, (int)PdfFilterDecodeLimits.DefaultMaximumDecodedBytes, MaximumTokenCount);

    internal static PdfCMap Parse(ReadOnlySpan<byte> decodedBytes, int maximumInputBytes, int maximumTokens)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumInputBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumTokens);
        if (decodedBytes.Length > maximumInputBytes)
        {
            throw new InvalidDataException("CMap decoded input exceeds the byte limit.");
        }

        string source = Encoding.ASCII.GetString(decodedBytes);
        // Indexed lookahead needs an eager snapshot; Tokenize bounds progress and token count as it yields.
        string[] tokens = Tokenize(source, maximumTokens).Select(token => token.Value).ToArray();

        var codeSpaces = new List<PdfCodeSpaceRange>();
        var cidMappings = new Dictionary<PdfCharacterCode, int>();
        var unicodeMappings = new Dictionary<PdfCharacterCode, string>();
        int writingMode = 0;

        for (int index = 0; index < tokens.Length; index++)
        {
            string token = tokens[index];

            if (string.Equals(token, "/WMode", StringComparison.Ordinal) &&
                index + 1 < tokens.Length &&
                int.TryParse(tokens[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedMode))
            {
                writingMode = parsedMode;
                continue;
            }

            if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) ||
                index + 1 >= tokens.Length)
            {
                continue;
            }

            string operation = tokens[index + 1];
            index += 1;

            switch (operation)
            {
                case "begincodespacerange":
                    for (int item = 0; item < count; item++)
                    {
                        byte[] lower = ParseHex(tokens[++index]);
                        byte[] upper = ParseHex(tokens[++index]);
                        codeSpaces.Add(new PdfCodeSpaceRange(lower, upper));
                    }
                    break;

                case "begincidchar":
                    for (int item = 0; item < count; item++)
                    {
                        PdfCharacterCode code = new(ParseHex(tokens[++index]));
                        int cid = ParseInteger(tokens[++index]);
                        cidMappings[code] = cid;
                    }
                    break;

                case "begincidrange":
                    for (int item = 0; item < count; item++)
                    {
                        byte[] first = ParseHex(tokens[++index]);
                        byte[] last = ParseHex(tokens[++index]);
                        int firstCid = ParseInteger(tokens[++index]);
                        ExpandNumericRange(first, last, (code, delta) =>
                            cidMappings[new PdfCharacterCode(code)] = checked(firstCid + delta));
                    }
                    break;

                case "beginbfchar":
                    for (int item = 0; item < count; item++)
                    {
                        PdfCharacterCode code = new(ParseHex(tokens[++index]));
                        unicodeMappings[code] = DecodeUnicodeHex(tokens[++index]);
                    }
                    break;

                case "beginbfrange":
                    for (int item = 0; item < count; item++)
                    {
                        byte[] first = ParseHex(tokens[++index]);
                        byte[] last = ParseHex(tokens[++index]);
                        string destination = tokens[++index];

                        if (destination == "[")
                        {
                            ExpandNumericRange(first, last, (code, _) =>
                            {
                                string value = tokens[++index];
                                unicodeMappings[new PdfCharacterCode(code)] = DecodeUnicodeHex(value);
                            });

                            if (index + 1 < tokens.Length && tokens[index + 1] == "]")
                            {
                                index++;
                            }
                        }
                        else
                        {
                            byte[] baseDestination = ParseHex(destination);
                            ExpandNumericRange(first, last, (code, delta) =>
                            {
                                byte[] mapped = AddBigEndian(baseDestination, delta);
                                unicodeMappings[new PdfCharacterCode(code)] = DecodeUtf16Be(mapped);
                            });
                        }
                    }
                    break;
            }
        }

        return new PdfCMap(codeSpaces, cidMappings, unicodeMappings, writingMode);
    }

    internal static IEnumerable<(string Value, int End)> Tokenize(string source, int maximumTokens)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumTokens);
        int index = 0;
        int tokenCount = 0;
        var delimiters = new Stack<char>();
        while (index < source.Length)
        {
            char value = source[index];

            if (value == '\0' || char.IsWhiteSpace(value))
            {
                index++;
                continue;
            }

            if (value == '%')
            {
                while (index < source.Length && source[index] is not '\r' and not '\n')
                {
                    index++;
                }

                continue;
            }

            // Check before creating the next token or growing delimiter state.
            if (tokenCount == maximumTokens)
            {
                throw new InvalidDataException("CMap token count exceeds the safety limit.");
            }

            int start = index;
            switch (value)
            {
                case '<' when index + 1 < source.Length && source[index + 1] == '<':
                    delimiters.Push('<');
                    index += 2;
                    break;
                case '>':
                    if (index + 1 >= source.Length || source[index + 1] != '>' ||
                        !delimiters.TryPop(out char opening) || opening != '<')
                    {
                        throw new InvalidDataException("CMap has an unmatched dictionary closing delimiter.");
                    }
                    index += 2;
                    break;
                case '[':
                    delimiters.Push('[');
                    index++;
                    break;
                case ']':
                    if (!delimiters.TryPop(out char arrayOpening) || arrayOpening != '[')
                    {
                        throw new InvalidDataException("CMap has an unmatched array closing delimiter.");
                    }
                    index++;
                    break;
                case '<':
                    index++;
                    while (index < source.Length && source[index] != '>')
                    {
                        if (source[index] == '<')
                        {
                            throw new InvalidDataException("CMap hexadecimal token contains an opening delimiter.");
                        }
                        index++;
                    }
                    if (index == source.Length)
                    {
                        throw new InvalidDataException("CMap hexadecimal token is unterminated.");
                    }
                    index++;
                    break;
                case '(':
                    // Metadata strings may contain delimiters and escaped parentheses.
                    int depth = 1;
                    index++;
                    while (index < source.Length && depth > 0)
                    {
                        char character = source[index++];
                        if (character == '\\' && index < source.Length)
                        {
                            index++;
                        }
                        else if (character == '(')
                        {
                            depth++;
                        }
                        else if (character == ')')
                        {
                            depth--;
                        }
                    }
                    if (depth != 0)
                    {
                        throw new InvalidDataException("CMap literal string is unterminated.");
                    }
                    break;
                case ')':
                    throw new InvalidDataException("CMap has an unmatched string closing delimiter.");
                default:
                    // Consume a name's slash, then stop at the next lexical delimiter.
                    if (value == '/')
                    {
                        index++;
                    }
                    while (index < source.Length && source[index] != '\0' && !char.IsWhiteSpace(source[index]) &&
                        source[index] is not '[' and not ']' and not '<' and not '>' and
                        not '%' and not '/' and not '(' and not ')')
                    {
                        index++;
                    }
                    break;
            }

            // One yield site: no token can be emitted from an unchanged position.
            if (index <= start)
            {
                throw new InvalidDataException("CMap tokenizer did not advance.");
            }

            tokenCount++;
            yield return (source[start..index], index);
        }

        if (delimiters.Count != 0)
        {
            throw new InvalidDataException("CMap has an unterminated dictionary or array.");
        }
    }

    private static byte[] ParseHex(string token)
    {
        if (token.Length < 2 || token[0] != '<' || token[^1] != '>')
        {
            throw new InvalidDataException($"Expected a CMap hexadecimal token, found '{token}'.");
        }

        string hex = token[1..^1];
        if ((hex.Length & 1) != 0)
        {
            hex += "0";
        }

        return Convert.FromHexString(hex);
    }

    private static int ParseInteger(string token) =>
        int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : throw new InvalidDataException($"Expected a CMap integer, found '{token}'.");

    private static string DecodeUnicodeHex(string token) =>
        DecodeUtf16Be(ParseHex(token));

    private static string DecodeUtf16Be(ReadOnlySpan<byte> bytes)
    {
        if ((bytes.Length & 1) != 0)
        {
            throw new InvalidDataException("ToUnicode destinations must contain complete UTF-16BE code units.");
        }

        return Encoding.BigEndianUnicode.GetString(bytes);
    }

    private static void ExpandNumericRange(
        byte[] first,
        byte[] last,
        Action<byte[], int> action)
    {
        if (first.Length != last.Length || first.Length is < 1 or > 4)
        {
            throw new InvalidDataException("CMap ranges must use equal source-code lengths from one through four bytes.");
        }

        uint firstValue = ToUInt32(first);
        uint lastValue = ToUInt32(last);
        if (lastValue < firstValue)
        {
            throw new InvalidDataException("CMap range end precedes its start.");
        }

        long count = (long)lastValue - firstValue + 1;
        if (count > 1_000_000)
        {
            throw new InvalidDataException("CMap range expansion exceeds the safety limit.");
        }

        for (uint value = firstValue; value <= lastValue; value++)
        {
            int delta = checked((int)(value - firstValue));
            action(FromUInt32(value, first.Length), delta);

            if (value == uint.MaxValue)
            {
                break;
            }
        }
    }

    private static uint ToUInt32(ReadOnlySpan<byte> bytes)
    {
        uint value = 0;
        foreach (byte item in bytes)
        {
            value = (value << 8) | item;
        }

        return value;
    }

    private static byte[] FromUInt32(uint value, int length)
    {
        byte[] bytes = new byte[length];
        for (int index = length - 1; index >= 0; index--)
        {
            bytes[index] = (byte)(value & 0xFF);
            value >>= 8;
        }

        return bytes;
    }

    private static byte[] AddBigEndian(byte[] value, int amount)
    {
        byte[] result = [.. value];
        int carry = amount;

        for (int index = result.Length - 1; index >= 0 && carry > 0; index--)
        {
            int sum = result[index] + (carry & 0xFF);
            result[index] = (byte)sum;
            carry = (carry >> 8) + (sum >> 8);
        }

        if (carry != 0)
        {
            throw new InvalidDataException("CMap Unicode range overflowed its destination width.");
        }

        return result;
    }
}
