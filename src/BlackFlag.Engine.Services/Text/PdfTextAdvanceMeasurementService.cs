using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Fonts;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Measures existing and replacement text with the same decoded PDF font
/// metrics used by the renderer.
/// </summary>
public static class PdfTextAdvanceMeasurementService
{
    private const double DefaultTolerance = 0.001;

    public static double MeasureOriginal(
        PdfDisplayTextRun run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        return MeasureElements(
            run,
            run.Elements,
            cancellationToken);
    }

    public static double MeasureReplacement(
        PdfDisplayTextRun run,
        IReadOnlyList<ReadOnlyMemory<byte>> replacementTextElements,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(replacementTextElements);

        PdfTextRunElement[] originalTextElements =
        [
            .. run.Elements.Where(
                static element =>
                    element.Kind == PdfTextRunElementKind.Text)
        ];

        if (originalTextElements.Length !=
            replacementTextElements.Count)
        {
            throw new ArgumentException(
                "Replacement text-element count must match the source text-element count.",
                nameof(replacementTextElements));
        }

        int replacementIndex = 0;

        PdfTextRunElement[] elements =
        [
            .. run.Elements.Select(
                element =>
                    element.Kind == PdfTextRunElementKind.Adjustment
                        ? PdfTextRunElement.CreateAdjustment(
                            element.Adjustment)
                        : PdfTextRunElement.CreateText(
                            replacementTextElements[
                                replacementIndex++].Span))
        ];

        return MeasureElements(
            run,
            elements,
            cancellationToken);
    }

    public static PdfTextAdvanceMeasurement Compare(
        PdfDisplayTextRun run,
        IReadOnlyList<ReadOnlyMemory<byte>> replacementTextElements,
        double tolerance = DefaultTolerance,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (!double.IsFinite(tolerance) ||
            tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance));
        }

        double original =
            MeasureOriginal(
                run,
                cancellationToken);

        double replacement =
            MeasureReplacement(
                run,
                replacementTextElements,
                cancellationToken);

        double delta =
            replacement - original;

        PdfTextAdvanceFit fit =
            Math.Abs(delta) <= tolerance
                ? PdfTextAdvanceFit.Exact
                : CanLocallyCompensate(run)
                    ? PdfTextAdvanceFit.LocallyAdjustable
                    : PdfTextAdvanceFit.RequiresReflow;

        return new PdfTextAdvanceMeasurement(
            original,
            replacement,
            fit);
    }

    private static double MeasureElements(
        PdfDisplayTextRun run,
        IReadOnlyList<PdfTextRunElement> elements,
        CancellationToken cancellationToken)
    {
        var temporary =
            new PdfDisplayTextRun(
                run.Font,
                run.FontSize,
                elements,
                run.TextMatrix,
                run.GraphicsState,
                run.SourceSpan,
                run.MarkedContent);

        PdfDecodedTextRun decoded =
            new PdfFontDecoder()
                .Decode(
                    temporary,
                    cancellationToken);

        double advance =
            decoded.Glyphs.Sum(
                static glyph =>
                    glyph.Advance);

        foreach (PdfTextRunElement element in elements)
        {
            if (element.Kind ==
                PdfTextRunElementKind.Adjustment)
            {
                advance +=
                    -element.Adjustment / 1000.0 *
                    run.FontSize;
            }
        }

        return advance *
            (run.GraphicsState.HorizontalScaling / 100.0);
    }

    private static bool CanLocallyCompensate(
        PdfDisplayTextRun run)
    {
        double scale =
            run.GraphicsState.HorizontalScaling / 100.0;

        return run.FontSize != 0 &&
            double.IsFinite(run.FontSize) &&
            scale != 0 &&
            double.IsFinite(scale);
    }
}
