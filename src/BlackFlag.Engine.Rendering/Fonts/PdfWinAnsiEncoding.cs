namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Provides the PDF WinAnsi character-to-Unicode mapping used by simple fonts.</summary>
public static class PdfWinAnsiEncoding
{
    /// <summary>Returns Unicode text for one WinAnsi code.</summary>
    public static string? GetUnicode(byte code)
    {
        if (code is >= 0x20 and <= 0x7E || code >= 0xA0)
        {
            return ((char)code).ToString();
        }

        return code switch
        {
            0x80 => "\u20AC",
            0x82 => "\u201A",
            0x83 => "\u0192",
            0x84 => "\u201E",
            0x85 => "\u2026",
            0x86 => "\u2020",
            0x87 => "\u2021",
            0x88 => "\u02C6",
            0x89 => "\u2030",
            0x8A => "\u0160",
            0x8B => "\u2039",
            0x8C => "\u0152",
            0x8E => "\u017D",
            0x91 => "\u2018",
            0x92 => "\u2019",
            0x93 => "\u201C",
            0x94 => "\u201D",
            0x95 => "\u2022",
            0x96 => "\u2013",
            0x97 => "\u2014",
            0x98 => "\u02DC",
            0x99 => "\u2122",
            0x9A => "\u0161",
            0x9B => "\u203A",
            0x9C => "\u0153",
            0x9E => "\u017E",
            0x9F => "\u0178",
            _ => null
        };
    }
}
