#nullable enable
using System.Threading;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplacementReadsEachTextElementOnceBeforeMeasurement(bool cancelled)
    {
        PdfDisplayTextRun run = CreateMixedRun();
        var replacements = new RecordingReplacementElements();
        using var cancellation = new CancellationTokenSource();
        if (cancelled)
        {
            cancellation.Cancel();
        }

        if (cancelled)
        {
            OperationCanceledException exception = Assert.Throws<OperationCanceledException>(
                () => PdfTextAdvanceMeasurementService.MeasureReplacement(run, replacements, cancellation.Token));
            Assert.Equal(cancellation.Token, exception.CancellationToken);
        }
        else
        {
            // ((700 / 1000 * 12 + 1) + (500 / 1000 * 12 + 1) + 1.44 - 0.24) * 0.8.
            Assert.Equal(14.08, PdfTextAdvanceMeasurementService.MeasureReplacement(run, replacements), precision: 6);
        }

        Assert.Equal([0, 1], replacements.ReadIndices);
        Assert.Equal([65, 65], run.EncodedBytes.ToArray());
        Assert.Equal(-120, run.Elements[1].Adjustment);
        Assert.Equal(20, run.Elements[3].Adjustment);
    }

    [Fact]
    public void ReplacementReadFailurePrecedesMeasurementCancellation()
    {
        var failure = new InvalidOperationException("Fixture replacement read failure.");
        var replacements = new RecordingReplacementElements(failure);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(
            () => PdfTextAdvanceMeasurementService.MeasureReplacement(
                CreateMixedRun(), replacements, cancellation.Token)));
        Assert.Equal([0, 1], replacements.ReadIndices);
    }

    private static PdfDisplayTextRun CreateMixedRun()
    {
        PdfDisplayTextRun template = CreateSimpleRun([65], horizontalScaling: 80);
        return new PdfDisplayTextRun(
            template.Font,
            template.FontSize,
            [
                PdfTextRunElement.CreateText([65]),
                PdfTextRunElement.CreateAdjustment(-120),
                PdfTextRunElement.CreateText([65]),
                PdfTextRunElement.CreateAdjustment(20)
            ],
            template.TextMatrix,
            template.GraphicsState,
            template.SourceSpan,
            template.MarkedContent);
    }

    private sealed class RecordingReplacementElements(Exception? failure = null)
        : IReadOnlyList<ReadOnlyMemory<byte>>
    {
        private readonly ReadOnlyMemory<byte>[] values = [new byte[] { 66 }, new byte[] { 65 }];

        public List<int> ReadIndices { get; } = [];
        public int Count => values.Length;
        public ReadOnlyMemory<byte> this[int index]
        {
            get
            {
                ReadIndices.Add(index);
                if (index == 1 && failure is not null)
                {
                    throw failure;
                }
                return values[index];
            }
        }

        public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator() =>
            ((IEnumerable<ReadOnlyMemory<byte>>)values).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
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
