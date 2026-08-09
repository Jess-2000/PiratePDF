using System.Globalization;
using System.Text;

namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Parses the mapping constructs required from PDF CMap programs.</summary>
public sealed class PdfCMapParser
{
    /// <summary>Parses a decoded CMap byte sequence.</summary>
    public static PdfCMap Parse(ReadOnlySpan<byte> decodedBytes)
    {
        string source = Encoding.ASCII.GetString(decodedBytes);
        string[] tokens = Tokenize(source).ToArray();

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

    private static IEnumerable<string> Tokenize(string source)
    {
        int index = 0;
        while (index < source.Length)
        {
            char value = source[index];

            if (char.IsWhiteSpace(value))
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

            if (value is '[' or ']')
            {
                yield return value.ToString();
                index++;
                continue;
            }

            if (value == '<')
            {
                int end = source.IndexOf('>', index + 1);
                if (end < 0)
                {
                    throw new InvalidDataException("CMap hexadecimal token is unterminated.");
                }

                yield return source[index..(end + 1)];
                index = end + 1;
                continue;
            }

            int start = index;
            while (index < source.Length &&
                !char.IsWhiteSpace(source[index]) &&
                source[index] is not '[' and not ']' and not '<' and not '>')
            {
                index++;
            }

            yield return source[start..index];
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
        byte[] result = value.ToArray();
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
