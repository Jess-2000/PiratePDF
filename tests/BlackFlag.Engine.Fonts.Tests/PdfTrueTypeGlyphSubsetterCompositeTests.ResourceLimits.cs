using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BlackFlag.Engine.Fonts.Embedding;
using BlackFlag.Engine.Fonts.Sanitization;
using Xunit;

namespace BlackFlag.Engine.Fonts.Tests;

public sealed partial class PdfTrueTypeGlyphSubsetterCompositeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialRootsIncludingGlyphZeroRespectLimit(bool unicode)
    {
        PdfSanitizedExternalFontProgram program = CreateProgram(
            SimpleGlyph(), SimpleGlyph(), SimpleGlyph());

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            Subset(program, "A", unicode, new PdfTrueTypeSubsetOptions
            {
                MaximumRetainedGlyphs = 1
            }));

        Assert.Equal(
            "TrueType subset dependency closure exceeded the configured 1-glyph limit.",
            exception.Message);
    }

    [Theory]
    [InlineData(false, "", 1)]
    [InlineData(true, "", 1)]
    [InlineData(false, "A", 2)]
    [InlineData(true, "A", 2)]
    [InlineData(false, "AAAA", 2)]
    [InlineData(true, "AAAA", 2)]
    [InlineData(false, "ACCA", 2)]
    [InlineData(true, "ACCA", 2)]
    [InlineData(false, "ABCABC", 3)]
    [InlineData(true, "ABCABC", 3)]
    public void DistinctRootsSucceedAtExactLimit(bool unicode, string text, int count)
    {
        PdfSanitizedExternalFontProgram program = CreateProgram(
            SimpleGlyph(), SimpleGlyph(), SimpleGlyph(), SimpleGlyph());
        PdfSanitizedExternalFontProgram result = Subset(program, text, unicode,
            new PdfTrueTypeSubsetOptions { MaximumRetainedGlyphs = count });

        AssertPreservedGlyphs(program, result, Enumerable.Range(0, count).ToArray());
        Assert.Equal(Subset(program, text, unicode, new()).Sha256, result.Sha256);
        if (count > 1)
        {
            AssertGlyphLimit(program, text, unicode, count - 1);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AliasedUnicodeRequestsPreserveMappingsAndWidths(bool unicode)
    {
        PdfSanitizedExternalFontProgram program = CreateProgram(
            SimpleGlyph(), SimpleGlyph(), SimpleGlyph());
        var options = new PdfTrueTypeSubsetOptions { MaximumRetainedGlyphs = 2 };
        if (unicode)
        {
            PdfUnicodeTrueTypeSubsetResult result = new PdfUnicodeTrueTypeGlyphSubsetter()
                .CreateSubset(program, "CAAC", options);
            Assert.Equal((ushort)1, result.CidByUnicode['A']);
            Assert.Equal((ushort)2, result.CidByUnicode['C']);
            Assert.Equal(1, result.GlyphByCid[1]);
            Assert.Equal(1, result.GlyphByCid[2]);
            Assert.Equal((int)'A', result.UnicodeByCid[1]);
            Assert.Equal((int)'C', result.UnicodeByCid[2]);
            Assert.Equal(510, result.WidthByCid[1]);
            Assert.Equal(510, result.WidthByCid[2]);
            Assert.Equal(new byte[] { 0, 2, 0, 1 }, result.EncodeText("CA"));
        }
        else
        {
            PdfTrueTypeSubsetResult result = new PdfTrueTypeGlyphSubsetter()
                .CreateSubset(program, "CAAC", options);
            Assert.Equal(2, result.UnicodeByCode.Count);
            Assert.Equal((int)'A', result.UnicodeByCode[(byte)'A']);
            Assert.Equal((int)'C', result.UnicodeByCode[(byte)'C']);
            PdfExternalTrueTypeMetrics metrics = PdfExternalTrueTypeInspector.Inspect(result.Program);
            Assert.Equal(510, metrics.WinAnsiWidths['A' - 32]);
            Assert.Equal(510, metrics.WinAnsiWidths['C' - 32]);
        }
    }

    [Theory]
    [InlineData(false, "single", "A", 3)]
    [InlineData(true, "single", "A", 3)]
    [InlineData(false, "nested", "A", 4)]
    [InlineData(true, "nested", "A", 4)]
    [InlineData(false, "duplicate", "A", 3)]
    [InlineData(true, "duplicate", "A", 3)]
    [InlineData(false, "shared", "AB", 4)]
    [InlineData(true, "shared", "AB", 4)]
    [InlineData(false, "single", "AB", 3)]
    [InlineData(true, "single", "AB", 3)]
    [InlineData(false, "zero", "A", 2)]
    [InlineData(true, "zero", "A", 2)]
    public void CompositeClosureUsesOneDistinctBudget(bool unicode, string shape, string text, int count)
    {
        byte[][] glyphs = shape switch
        {
            "single" => [SimpleGlyph(), CompositeGlyph((2, 2)), SimpleGlyph(), SimpleGlyph()],
            "nested" => [SimpleGlyph(), CompositeGlyph((2, 2)), CompositeGlyph((2, 3)), SimpleGlyph()],
            "duplicate" => [SimpleGlyph(), CompositeGlyph((2, 2), (2, 2)), SimpleGlyph(), SimpleGlyph()],
            "shared" => [SimpleGlyph(), CompositeGlyph((2, 3)), CompositeGlyph((2, 3)), SimpleGlyph()],
            "zero" => [SimpleGlyph(), CompositeGlyph((2, 0)), SimpleGlyph()],
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        PdfSanitizedExternalFontProgram program = CreateProgram(glyphs);
        PdfSanitizedExternalFontProgram result = Subset(program, text, unicode,
            new PdfTrueTypeSubsetOptions { MaximumRetainedGlyphs = count });

        AssertPreservedGlyphs(program, result, Enumerable.Range(0, count).ToArray());
        AssertGlyphLimit(program, text, unicode, count - 1);
    }

    [Theory]
    [InlineData(false, "cycle", "cycle")]
    [InlineData(true, "cycle", "cycle")]
    [InlineData(false, "depth", "depth")]
    [InlineData(true, "depth", "depth")]
    [InlineData(false, "range", "outside")]
    [InlineData(true, "range", "outside")]
    [InlineData(false, "truncated", "truncated component")]
    [InlineData(true, "truncated", "truncated component")]
    [InlineData(false, "components", "component limit")]
    [InlineData(true, "components", "component limit")]
    public void ExistingCompositeFailuresRemainDistinct(bool unicode, string shape, string message)
    {
        byte[] truncated = new byte[10];
        W16(truncated, 0, ushort.MaxValue);
        byte[][] glyphs = shape switch
        {
            "cycle" => [SimpleGlyph(), CompositeGlyph((2, 2)), CompositeGlyph((2, 1))],
            "depth" => [SimpleGlyph(), CompositeGlyph((2, 2)), CompositeGlyph((2, 3)), SimpleGlyph()],
            "range" => [SimpleGlyph(), CompositeGlyph((2, 99)), SimpleGlyph()],
            "truncated" => [SimpleGlyph(), truncated, SimpleGlyph()],
            "components" => [SimpleGlyph(), CompositeGlyph((2, 2), (2, 2)), SimpleGlyph()],
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        PdfSanitizedExternalFontProgram program = CreateProgram(glyphs);
        var options = new PdfTrueTypeSubsetOptions
        {
            MaximumRetainedGlyphs = 4,
            MaximumCompositeDepth = 1,
            MaximumComponentsPerGlyph = 1
        };
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            Subset(program, "A", unicode, shape == "cycle" ? new() : options));
        Assert.Contains(message, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(false, "A")]
    [InlineData(true, "A")]
    public void CancellationPrecedesRootOrDependencyLimit(bool unicode, string text)
    {
        PdfSanitizedExternalFontProgram program = CreateProgram(
            SimpleGlyph(), CompositeGlyph((2, 2)), SimpleGlyph());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() =>
            Subset(program, text, unicode,
                new PdfTrueTypeSubsetOptions { MaximumRetainedGlyphs = 1 }, cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void GeneratedByteLimitIncludesDirectoryAndPadding(bool unicode, bool composite)
    {
        for (int length = 32; length < 36; length++)
        {
            PdfSanitizedExternalFontProgram program = CreateProgram(
                SimpleGlyph(),
                composite ? CompositeGlyph((2, 2)) : SimpleGlyph(length),
                SimpleGlyph(length));
            PdfSanitizedExternalFontProgram expected = Subset(program, "A", unicode, new());
            int generatedLength = expected.Bytes.Length;
            Assert.Equal(0, generatedLength % 4);
            PdfSanitizedExternalFontProgram exact = Subset(program, "A", unicode,
                new PdfTrueTypeSubsetOptions { MaximumGeneratedBytes = generatedLength });
            Assert.Equal(expected.Sha256, exact.Sha256);
            Assert.Equal(expected.Bytes.ToArray(), exact.Bytes.ToArray());
            AssertPreservedGlyphs(program, exact, composite ? [0, 1, 2] : [0, 1]);

            InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
                Subset(program, "A", unicode,
                    new PdfTrueTypeSubsetOptions { MaximumGeneratedBytes = generatedLength - 1 }));
            string kind = unicode ? "Unicode TrueType" : "TrueType";
            Assert.Equal(
                $"The generated {kind} subset is {generatedLength} bytes, exceeding the configured {generatedLength - 1}-byte limit.",
                exception.Message);
        }
    }

    private static void AssertGlyphLimit(
        PdfSanitizedExternalFontProgram program, string text, bool unicode, int limit)
    {
        byte[] source = program.Bytes.ToArray();
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            Subset(program, text, unicode,
                new PdfTrueTypeSubsetOptions { MaximumRetainedGlyphs = limit }));
        Assert.Equal(
            $"TrueType subset dependency closure exceeded the configured {limit}-glyph limit.",
            exception.Message);
        Assert.Equal(source, program.Bytes.ToArray());
    }

    private static void AssertPreservedGlyphs(
        PdfSanitizedExternalFontProgram source, PdfSanitizedExternalFontProgram result, int[] retained)
    {
        byte[] original = source.Bytes.ToArray();
        byte[] generated = result.Bytes.ToArray();
        byte[] sourceLoca = ReadTable(original, "loca");
        byte[] resultLoca = ReadTable(generated, "loca");
        byte[] sourceGlyf = ReadTable(original, "glyf");
        byte[] resultGlyf = ReadTable(generated, "glyf");
        Assert.Equal(sourceLoca.Length, resultLoca.Length);
        for (int glyph = 0; glyph < sourceLoca.Length / 4 - 1; glyph++)
        {
            int start = checked((int)BinaryPrimitives.ReadUInt32BigEndian(resultLoca.AsSpan(glyph * 4)));
            int end = checked((int)BinaryPrimitives.ReadUInt32BigEndian(resultLoca.AsSpan((glyph + 1) * 4)));
            if (retained.Contains(glyph))
            {
                int oldStart = checked((int)BinaryPrimitives.ReadUInt32BigEndian(sourceLoca.AsSpan(glyph * 4)));
                int oldEnd = checked((int)BinaryPrimitives.ReadUInt32BigEndian(sourceLoca.AsSpan((glyph + 1) * 4)));
                Assert.Equal(sourceGlyf[oldStart..oldEnd], resultGlyf[start..end]);
            }
            else
            {
                Assert.Equal(start, end);
            }
        }

        foreach (string tag in new[] { "maxp", "hhea", "hmtx", "name", "OS/2" })
        {
            Assert.Equal(ReadTable(original, tag), ReadTable(generated, tag));
        }

        Assert.Equal(0xB1B0AFBAu, Checksum(generated, false));
        using var stream = new MemoryStream(generated, writable: false);
        Assert.Equal(result.Sha256, new PdfOpenTypeFontSanitizer().Sanitize(stream).Sha256);
    }

    private static byte[] ReadTable(byte[] sfnt, string tag)
    {
        int count = BinaryPrimitives.ReadUInt16BigEndian(sfnt.AsSpan(4));
        for (int index = 0; index < count; index++)
        {
            int record = 12 + index * 16;
            if (Encoding.ASCII.GetString(sfnt, record, 4) != tag)
            {
                continue;
            }

            int offset = checked((int)BinaryPrimitives.ReadUInt32BigEndian(sfnt.AsSpan(record + 8)));
            int length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(sfnt.AsSpan(record + 12)));
            return sfnt.AsSpan(offset, length).ToArray();
        }

        throw new InvalidDataException($"Missing test table {tag}.");
    }

    private static PdfSanitizedExternalFontProgram Subset(
        PdfSanitizedExternalFontProgram program,
        string text,
        bool unicode,
        PdfTrueTypeSubsetOptions options,
        CancellationToken cancellationToken = default) =>
        unicode
            ? new PdfUnicodeTrueTypeGlyphSubsetter()
                .CreateSubset(program, text, options, cancellationToken).Program
            : new PdfTrueTypeGlyphSubsetter()
                .CreateSubset(program, text, options, cancellationToken).Program;
}
