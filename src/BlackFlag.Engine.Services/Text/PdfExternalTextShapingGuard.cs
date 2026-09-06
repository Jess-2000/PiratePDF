using System.Globalization;
using System.Text;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Conservatively blocks text that requires shaping or sequence-level glyph
/// selection that BF-1054Q does not yet implement.
/// </summary>
internal static class PdfExternalTextShapingGuard
{
    public static void EnsureSimpleScalarMapping(
        string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        foreach (Rune rune in text.EnumerateRunes())
        {
            if (RequiresShaping(
                    rune))
            {
                throw new NotSupportedException(
                    $"Unicode scalar U+{rune.Value:X} requires shaping or sequence-aware glyph selection not implemented by BF-1054Q.");
            }
        }
    }

    private static bool RequiresShaping(
        Rune rune)
    {
        UnicodeCategory category =
            Rune.GetUnicodeCategory(
                rune);

        if (category is
            UnicodeCategory.NonSpacingMark or
            UnicodeCategory.SpacingCombiningMark or
            UnicodeCategory.EnclosingMark)
        {
            return true;
        }

        int value =
            rune.Value;

        return value switch
        {
            0x200C or 0x200D => true,
            >= 0xFE00 and <= 0xFE0F => true,
            >= 0xE0100 and <= 0xE01EF => true,

            // Arabic, Syriac, Thaana, NKo and related contextual scripts.
            >= 0x0600 and <= 0x08FF => true,

            // Indic and Southeast Asian scripts requiring shaping.
            >= 0x0900 and <= 0x0DFF => true,
            >= 0x0F00 and <= 0x109F => true,
            >= 0x1780 and <= 0x17FF => true,
            >= 0xA980 and <= 0xA9DF => true,
            >= 0xAA60 and <= 0xAA7F => true,
            >= 0xABC0 and <= 0xABFF => true,

            _ => false
        };
    }
}
