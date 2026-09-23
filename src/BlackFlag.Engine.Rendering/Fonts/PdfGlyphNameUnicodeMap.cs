using System.Text;
using System.Globalization;

namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Maps common PDF/PostScript glyph names to Unicode without backend dependencies.</summary>
public static class PdfGlyphNameUnicodeMap
{
    private static readonly Dictionary<string, string> NamedMappings =
        new(StringComparer.Ordinal)
        {
            ["space"] = " ",
            ["bullet"] = "\u2022",
            ["openbullet"] = "\u25E6",
            ["periodcentered"] = "\u00B7",
            ["endash"] = "\u2013",
            ["emdash"] = "\u2014",
            ["square"] = "\u25A1",
            ["blacksquare"] = "\u25A0",
            ["whitesquare"] = "\u25A1",
            ["blacksmallsquare"] = "\u25AA",
            ["whitesmallsquare"] = "\u25AB",
            ["arrowright"] = "\u2192",
            ["rightArrow"] = "\u2192",
            ["heavyarrowright"] = "\u279C",
            ["blackrightpointingtriangle"] = "\u25B6",
            ["triagrt"] = "\u25B6",
            ["hyphen"] = "-",
            ["minus"] = "\u2212",
            ["period"] = ".",
            ["comma"] = ",",
            ["colon"] = ":",
            ["semicolon"] = ";",
            ["parenleft"] = "(",
            ["parenright"] = ")"
        };

    /// <summary>Attempts to map a glyph name to Unicode text.</summary>
    public static bool TryGetUnicode(string glyphName, out string? unicodeText, StringBuilder result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(glyphName);

        if (NamedMappings.TryGetValue(glyphName, out string? named))
        {
            unicodeText = named;
            return true;
        }

        if (glyphName.Length == 1)
        {
            unicodeText = glyphName;
            return true;
        }

        if (glyphName.Length == 2 && glyphName[0] is >= 'A' and <= 'Z' &&
            glyphName[1] is >= 'a' and <= 'z')
        {
            unicodeText = null;
            return false;
        }

        if (glyphName.StartsWith("uni", StringComparison.Ordinal) &&
            glyphName.Length > 3 &&
            ((glyphName.Length - 3) % 4) == 0)
        {
            for (int index = 3; index < glyphName.Length; index += 4)
            {
                if (!ushort.TryParse(
                    glyphName.AsSpan(index, 4),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out ushort codeUnit))
                {
                    unicodeText = null;
                    return false;
                }

                result.Append((char)codeUnit);
            }

            unicodeText = result.ToString();
            return true;
        }

        if (glyphName.StartsWith('u') &&
            glyphName.Length is >= 5 and <= 7 &&
            int.TryParse(
                glyphName.AsSpan(1),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out int scalar) &&
            Rune.IsValid(scalar))
        {
            unicodeText = new Rune(scalar).ToString();
            return true;
        }

        if (glyphName.Length == 1 || glyphName.All(static c => char.IsLetterOrDigit(c)))
        {
            unicodeText = glyphName switch
            {
                "zero" => "0",
                "one" => "1",
                "two" => "2",
                "three" => "3",
                "four" => "4",
                "five" => "5",
                "six" => "6",
                "seven" => "7",
                "eight" => "8",
                "nine" => "9",
                _ when glyphName.Length == 1 => glyphName,
                _ => null
            };

            return unicodeText is not null;
        }

        unicodeText = null;
        return false;
    }
}
