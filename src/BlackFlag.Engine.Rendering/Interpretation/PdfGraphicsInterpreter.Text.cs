using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Interpretation;

public sealed partial class PdfGraphicsInterpreter
{
    private static byte[] ReadTextBytes(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        return value switch
        {
            // PdfObjectParser stores literal-string bytes through a Latin-1
            // round-trip, so this reconstructs the exact decoded byte values
            // without claiming those bytes are Unicode characters.
            PdfLiteralString literal =>
                Encoding.Latin1.GetBytes(literal.Value),

            PdfHexString hex =>
                hex.Value.ToArray(),

            _ => throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidTextState,
                operation,
                "A text-showing operator requires a literal or hexadecimal string.")
        };
    }

    private static PdfGraphicsStateSnapshot AdvanceTextMatrix(
        PdfGraphicsStateSnapshot state,
        PdfResolvedFont font,
        IEnumerable<PdfTextRunElement> elements,
        PdfGraphicsOperation operation)
    {
        double horizontalScale =
            state.HorizontalScaling / 100.0;

        double translation = 0;

        foreach (PdfTextRunElement element in elements)
        {
            if (element.Kind == PdfTextRunElementKind.Adjustment)
            {
                translation +=
                    -element.Adjustment / 1000.0
                    * state.FontSize
                    * horizontalScale;

                continue;
            }

            translation += CalculateEncodedTextAdvance(
                font,
                element.EncodedBytes.Span,
                state,
                horizontalScale,
                operation);
        }

        if (translation == 0)
        {
            return state;
        }

        PdfMatrix advance =
            new(1, 0, 0, 1, translation, 0);

        return state with
        {
            TextMatrix =
                advance.Multiply(state.TextMatrix)
        };
    }

    private static double CalculateEncodedTextAdvance(
        PdfResolvedFont font,
        ReadOnlySpan<byte> encodedBytes,
        PdfGraphicsStateSnapshot state,
        double horizontalScale,
        PdfGraphicsOperation operation)
    {
        if (encodedBytes.IsEmpty)
        {
            return 0;
        }

        return string.Equals(
            font.Subtype,
            "Type0",
            StringComparison.Ordinal)
                ? CalculateCompositeFontAdvance(
                    font,
                    encodedBytes,
                    state,
                    horizontalScale,
                    operation)
                : CalculateSimpleFontAdvance(
                    font,
                    encodedBytes,
                    state,
                    horizontalScale);
    }

    private static double CalculateSimpleFontAdvance(
        PdfResolvedFont font,
        ReadOnlySpan<byte> encodedBytes,
        PdfGraphicsStateSnapshot state,
        double horizontalScale)
    {
        int firstCharacter =
            TryGetInteger(font.Dictionary, "FirstChar")
            ?? 0;

        PdfArray? widths =
            TryGetArray(font.Dictionary, "Widths");

        double missingWidth =
            TryGetNumber(
                font.Descriptor,
                "MissingWidth")
            ?? 0;

        double total = 0;

        foreach (byte characterCode in encodedBytes)
        {
            int widthIndex =
                characterCode - firstCharacter;

            double glyphWidth =
                widths is not null
                && widthIndex >= 0
                && widthIndex < widths.Count
                && TryReadNumber(widths[widthIndex], out double explicitWidth)
                    ? explicitWidth
                    : missingWidth;

            double spacing =
                state.CharacterSpacing;

            if (characterCode == 0x20)
            {
                spacing +=
                    state.WordSpacing;
            }

            total +=
                (glyphWidth / 1000.0 * state.FontSize + spacing)
                * horizontalScale;
        }

        return total;
    }

    private static double CalculateCompositeFontAdvance(
        PdfResolvedFont font,
        ReadOnlySpan<byte> encodedBytes,
        PdfGraphicsStateSnapshot state,
        double horizontalScale,
        PdfGraphicsOperation operation)
    {
        if (font.Encoding is not PdfName encoding
            || !string.Equals(
                encoding.Value,
                "Identity-H",
                StringComparison.Ordinal))
        {
            // BF-1046 preserves arbitrary CMap resources but does not yet
            // expose a decoded CMap service. Do not invent character-code
            // boundaries for an arbitrary composite-font encoding.
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidTextState,
                operation,
                $"Composite font '{font.FaceName}' uses an encoding that requires decoded CMap support before text advancement can be interpreted.");
        }

        if ((encodedBytes.Length & 1) != 0)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidTextState,
                operation,
                "Identity-H text contains an odd number of bytes.");
        }

        PdfDictionary? descendant =
            GetDirectDescendantFont(font);

        double defaultWidth =
            TryGetNumber(
                descendant,
                "DW")
            ?? 1000;

        PdfArray? widths =
            descendant is null
                ? null
                : TryGetArray(descendant, "W");

        double total = 0;

        for (int index = 0; index < encodedBytes.Length; index += 2)
        {
            int cid =
                (encodedBytes[index] << 8)
                | encodedBytes[index + 1];

            double glyphWidth =
                TryGetCidWidth(
                    widths,
                    cid,
                    out double explicitWidth)
                    ? explicitWidth
                    : defaultWidth;

            total +=
                (glyphWidth / 1000.0 * state.FontSize
                    + state.CharacterSpacing)
                * horizontalScale;
        }

        return total;
    }

    private static PdfDictionary? GetDirectDescendantFont(
        PdfResolvedFont font)
    {
        if (font.DescendantFonts is null
            || font.DescendantFonts.Count == 0)
        {
            return null;
        }

        return font.DescendantFonts[0] as PdfDictionary;
    }

    private static bool TryGetCidWidth(
        PdfArray? widths,
        int cid,
        out double width)
    {
        width = 0;

        if (widths is null)
        {
            return false;
        }

        int index = 0;

        while (index < widths.Count)
        {
            if (!TryReadInteger(
                widths[index],
                out long firstCodeLong))
            {
                return false;
            }

            if (firstCodeLong is < 0 or > int.MaxValue)
            {
                return false;
            }

            int firstCode =
                checked((int)firstCodeLong);

            index++;

            if (index >= widths.Count)
            {
                return false;
            }

            if (widths[index] is PdfArray sequentialWidths)
            {
                int widthIndex =
                    cid - firstCode;

                if (widthIndex >= 0
                    && widthIndex < sequentialWidths.Count
                    && TryReadNumber(
                        sequentialWidths[widthIndex],
                        out width))
                {
                    return true;
                }

                index++;
                continue;
            }

            if (!TryReadInteger(
                    widths[index],
                    out long lastCodeLong)
                || lastCodeLong is < 0 or > int.MaxValue)
            {
                return false;
            }

            int lastCode =
                checked((int)lastCodeLong);

            index++;

            if (index >= widths.Count
                || !TryReadNumber(
                    widths[index],
                    out double rangeWidth))
            {
                return false;
            }

            if (cid >= firstCode
                && cid <= lastCode)
            {
                width = rangeWidth;
                return true;
            }

            index++;
        }

        return false;
    }
}
