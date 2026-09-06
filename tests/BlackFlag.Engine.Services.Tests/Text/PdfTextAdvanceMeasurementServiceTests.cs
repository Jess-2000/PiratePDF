using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfTextAdvanceMeasurementServiceTests
{
    [Fact]
    public void SimpleFontAdvanceUsesWidthsSpacingAndHorizontalScale()
    {
        PdfDisplayTextRun run =
            CreateSimpleRun(
                [65, 66],
                horizontalScaling: 80);

        double advance =
            PdfTextAdvanceMeasurementService.MeasureOriginal(
                run);

        // Each glyph includes character spacing before horizontal scaling.
        // ((500 / 1000 * 12 + 1) + (700 / 1000 * 12 + 1)) * 0.8 = 13.12.
        Assert.Equal(
            13.12,
            advance,
            precision: 6);
    }

    [Fact]
    public void ReplacementComparisonReportsLocalAdjustment()
    {
        PdfDisplayTextRun run =
            CreateSimpleRun(
                [65],
                horizontalScaling: 100);

        PdfTextAdvanceMeasurement measurement =
            PdfTextAdvanceMeasurementService.Compare(
                run,
                [new byte[] { 66 }]);

        Assert.Equal(
            PdfTextAdvanceFit.LocallyAdjustable,
            measurement.Fit);

        Assert.True(
            measurement.Delta > 0);
    }

    [Fact]
    public void IdenticalReplacementReportsExactAdvance()
    {
        PdfDisplayTextRun run =
            CreateSimpleRun(
                [65]);

        PdfTextAdvanceMeasurement measurement =
            PdfTextAdvanceMeasurementService.Compare(
                run,
                ["A"u8.ToArray()]);

        Assert.Equal(
            PdfTextAdvanceFit.Exact,
            measurement.Fit);

        Assert.Equal(
            0,
            measurement.Delta,
            precision: 8);
    }

    [Fact]
    public void AutomaticDecorationDefersWidthUntilMeasurement()
    {
        PdfExistingTextDecoration decoration =
            PdfExistingTextDecoration.Automatic(
                PdfExistingTextDecorationKind.Underline);

        Assert.True(
            decoration.RequiresMeasurement);

        PdfExistingTextDecoration resolved =
            decoration.ResolveAdvance(
                42);

        Assert.False(
            resolved.RequiresMeasurement);

        Assert.Equal(
            42,
            resolved.AdvanceWidth);
    }

    private static PdfDisplayTextRun CreateSimpleRun(
        byte[] bytes,
        double horizontalScaling = 100)
    {
        var fontDictionary =
            new PdfDictionary(
                new Dictionary<PdfName, PdfObject>
                {
                    [new PdfName("FirstChar")] =
                        new PdfInteger(65),

                    [new PdfName("Widths")] =
                        new PdfArray(
                        [
                            new PdfInteger(500),
                            new PdfInteger(700)
                        ])
                });

        var descriptor =
            new PdfDictionary(
                new Dictionary<PdfName, PdfObject>
                {
                    [new PdfName("MissingWidth")] =
                        new PdfInteger(500)
                });

        PdfResolvedFont font =
            new(
                PdfResourceIdentity.ForLocal(1),
                fontDictionary,
                "TrueType",
                "Test",
                "Test Regular",
                400,
                5,
                PdfFontSlant.Upright,
                PdfFontTechnology.TrueType,
                PdfFontEmbeddingRights.Unknown,
                false,
                new PdfName("WinAnsiEncoding"),
                null,
                descriptor,
                null,
                null,
                []);

        var state =
            new PdfGraphicsStateSnapshot
            {
                Font = font,
                FontSize = 12,
                CharacterSpacing = 1,
                HorizontalScaling = horizontalScaling
            };

        return new PdfDisplayTextRun(
            font,
            12,
            [PdfTextRunElement.CreateText(bytes)],
            PdfMatrix.Identity,
            state,
            new PdfContentSourceSpan(
                0,
                0,
                1),
            markedContent: null);
    }
}
