using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;

namespace BlackFlag.Engine.Services.Text;

internal static partial class PdfExistingTextStyleTokenEditor
{
    internal static PdfGraphicsStateSnapshot GetStyledState(
        PdfGraphicsStateSnapshot state,
        PdfExistingTextStyleUpdate update) => update.ScriptStyle switch
        {
            PdfExistingTextScriptStyle.Superscript => state with
            {
                FontSize = state.FontSize * update.SuperscriptScale,
                TextRise = state.TextRise + state.FontSize * update.SuperscriptRise
            },
            PdfExistingTextScriptStyle.Subscript => state with
            {
                FontSize = state.FontSize * update.SubscriptScale,
                TextRise = state.TextRise - state.FontSize * update.SubscriptDrop
            },
            _ => state
        };

    private static void WriteDecoration(
        Stream output,
        PdfExistingTextDescriptor descriptor,
        PdfGraphicsStateSnapshot state,
        PdfExistingTextStyleUpdate update,
        double styledAdvance)
    {
        PdfExistingTextDecoration decoration = update.Decoration!;
        PdfGraphicsStateSnapshot styled = GetStyledState(state, update);
        double offset = styled.TextRise + (decoration.Offset ??
            (decoration.Kind == PdfExistingTextDecorationKind.Underline
                ? -0.12 * styled.FontSize : 0.30 * styled.FontSize));
        double thickness = decoration.Thickness ?? Math.Max(0.5, 0.05 * Math.Abs(styled.FontSize));
        // Measurement already includes Tz, spacing and TJ adjustments. Retain
        // its direction for reversed horizontal text; explicit widths stay literal.
        double width = decoration.RequiresMeasurement ? styledAdvance : decoration.AdvanceWidth;
        double continuation = GetLineAdvance(state) + styledAdvance;
        RequireFinite(offset, thickness, continuation, -continuation * 1000);
        PdfMatrix placement = state.TextMatrix.Multiply(state.Transform);
        PdfPoint start = placement.Transform(new PdfPoint(0, offset));
        PdfPoint end = placement.Transform(new PdfPoint(width, offset));
        RequireFinite(start.X, start.Y, end.X, end.Y,
            thickness * placement.A, thickness * placement.B, thickness * placement.C, thickness * placement.D);

        // Paths are not legal inside BT/ET. Keep the insertion at the target's
        // marked-content scope; q/Q isolates CTM, color and stroke properties.
        WriteAscii(output, "ET\nq\n");
        WriteMatrix(output, state.TextMatrix);
        WriteAscii(output, " cm\n");
        if (update.FillColor is { } color)
        {
            WriteColor(output, color, stroking: true);
        }
        else
        {
            WriteDisplayColor(output, state.FillColor, stroking: true);
        }
        WriteNumber(output, thickness);
        WriteAscii(output, " w\n0 ");
        WriteNumber(output, offset);
        WriteAscii(output, " m\n");
        WriteNumber(output, width);
        WriteAscii(output, " ");
        WriteNumber(output, offset);
        WriteAscii(output, " l\nS\nQ");
        WriteContinuation(output, descriptor.FontResourceName, state, styledAdvance);
    }

    internal static void WriteContinuation(Stream output, string? fontName, PdfGraphicsStateSnapshot state, double advance)
    {
        double continuation = GetLineAdvance(state) + advance;
        RequireFinite(continuation, -continuation * 1000);
        WriteAscii(output, "\nBT\n");

        // BT resets BOTH matrices. Tm restores the line origin; an adjustment-
        // only TJ restores the current position without moving the line origin.
        // Unit font size/scaling avoids division by a zero or tiny source size.
        WriteMatrix(output, state.TextLineMatrix);
        WriteAscii(output, " Tm\n");
        if (continuation != 0)
        {
            string name = fontName ?? throw new NotSupportedException(
                "Decoration continuation requires a known font resource name.");
            WriteName(output, name);
            WriteAscii(output, " 1 Tf\n100 Tz\n[");
            WriteNumber(output, -continuation * 1000);
            WriteAscii(output, "] TJ\n");
            WriteName(output, name);
            WriteAscii(output, " ");
            WriteNumber(output, state.FontSize);
            WriteAscii(output, " Tf\n");
            WriteNumber(output, state.HorizontalScaling);
            WriteAscii(output, " Tz\n");
        }
    }

    private static double GetLineAdvance(PdfGraphicsStateSnapshot state)
    {
        PdfMatrix text = state.TextMatrix;
        PdfMatrix line = state.TextLineMatrix;
        // Horizontal writing changes only the translation along the line's x
        // basis. No inverse is needed, including singular affine transforms.
        double advance = Math.Abs(line.A) >= Math.Abs(line.B)
            ? line.A == 0 ? 0 : (text.E - line.E) / line.A
            : (text.F - line.F) / line.B;
        PdfMatrix expected = new PdfMatrix(1, 0, 0, 1, advance, 0).Multiply(line);
        if (text.A != line.A || text.B != line.B || text.C != line.C || text.D != line.D ||
            !Close(expected.E, text.E) || !Close(expected.F, text.F))
        {
            throw new NotSupportedException("Decoration requires restorable horizontal text and line matrices.");
        }
        return advance;
    }

    private static bool Close(double left, double right) =>
        double.IsFinite(left) && double.IsFinite(right) && Math.Abs(left - right) <= 0.000001;

    private static void WriteMatrix(Stream output, PdfMatrix matrix)
    {
        double[] values = [matrix.A, matrix.B, matrix.C, matrix.D, matrix.E, matrix.F];
        RequireFinite(values);
        for (int index = 0; index < values.Length; index++)
        {
            if (index != 0) WriteAscii(output, " ");
            WriteNumber(output, values[index]);
        }
    }

    private static void RequireFinite(params double[] values)
    {
        if (values.Any(static value => !double.IsFinite(value)))
        {
            throw new NotSupportedException("Decoration geometry must remain finite.");
        }
    }
}
