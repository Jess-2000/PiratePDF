using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using BlackFlag.Engine.Fonts.Sanitization;
using Xunit;

namespace BlackFlag.Engine.Fonts.Tests;

public sealed partial class PdfOpenTypeFontSanitizerNumericTests
{
    [Theory]
    [InlineData("CFF ")]
    [InlineData("CFF2")]
    public void SupportedOpenTypeHeadersRemainByteIdentical(string tag)
    {
        byte[] table = tag == "CFF " ? [1, 0, 4, 4] : [2, 0, 5, 0, 0];
        byte[] font = FontWithTable(tag, table, 0x4F54544F);
        Assert.Equal(PdfExternalFontTechnology.OpenType, Sanitize(font).Technology);
        AssertCanonical(font);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void InputLimitHasExactBoundary(int allowance)
    {
        using MemoryStream fixture = TestFontFactory.CreateMinimalTrueType();
        byte[] bytes = fixture.ToArray();
        using var source = new ShortReadStream(bytes);
        var options = new PdfExternalFontSanitizationOptions { MaximumInputBytes = bytes.Length + allowance };
        if (allowance < 0)
        {
            var failure = Assert.Throws<PdfExternalFontSanitizationException>(() =>
                new PdfOpenTypeFontSanitizer().Sanitize(source, options));
            Assert.Equal(PdfExternalFontSanitizationFailure.InputTooLarge, failure.Failure);
            Assert.Equal(options.MaximumInputBytes + 1, source.Position);
        }
        else
        {
            Assert.Equal(bytes, new PdfOpenTypeFontSanitizer().Sanitize(source, options).Bytes.ToArray());
        }
        Assert.True(source.CanRead);
    }

    [Theory]
    [InlineData("MaximumInputBytes")]
    [InlineData("MaximumTableCount")]
    [InlineData("MaximumSingleTableBytes")]
    [InlineData("MaximumGlyphCount")]
    [InlineData("MaximumCMapSubtableCount")]
    [InlineData("MaximumNameRecordCount")]
    public void InvalidOptionsKeepArgumentContract(string name)
    {
        foreach (int value in new[] { 0, -1 })
        {
            PdfExternalFontSanitizationOptions options = name switch
            {
                "MaximumInputBytes" => new() { MaximumInputBytes = value },
                "MaximumTableCount" => new() { MaximumTableCount = value },
                "MaximumSingleTableBytes" => new() { MaximumSingleTableBytes = value },
                "MaximumGlyphCount" => new() { MaximumGlyphCount = value },
                "MaximumCMapSubtableCount" => new() { MaximumCMapSubtableCount = value },
                "MaximumNameRecordCount" => new() { MaximumNameRecordCount = value },
                _ => throw new ArgumentOutOfRangeException(nameof(name))
            };
            using MemoryStream source = TestFontFactory.CreateMinimalTrueType();
            var failure = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PdfOpenTypeFontSanitizer().Sanitize(source, options));
            Assert.Equal(name, failure.ParamName);
            Assert.Equal(value, failure.ActualValue);
            Assert.Equal(0, source.Position);
        }
    }

    [Fact]
    public void AllMaximumOptionValuesRemainValidForTinyInput()
    {
        using MemoryStream source = TestFontFactory.CreateMinimalTrueType();
        byte[] expected = source.ToArray();
        var options = new PdfExternalFontSanitizationOptions
        {
            MaximumInputBytes = int.MaxValue,
            MaximumTableCount = int.MaxValue,
            MaximumSingleTableBytes = int.MaxValue,
            MaximumGlyphCount = int.MaxValue,
            MaximumCMapSubtableCount = int.MaxValue,
            MaximumNameRecordCount = int.MaxValue
        };
        Assert.Equal(expected, new PdfOpenTypeFontSanitizer().Sanitize(source, options).Bytes.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationBeforeAndDuringBoundedReadKeepsToken(bool duringRead)
    {
        using MemoryStream fixture = TestFontFactory.CreateMinimalTrueType();
        using var cancellation = new CancellationTokenSource();
        using var source = new ShortReadStream(fixture.ToArray(), duringRead ? cancellation : null);
        if (!duringRead) cancellation.Cancel();
        var failure = Assert.Throws<OperationCanceledException>(() =>
            new PdfOpenTypeFontSanitizer().Sanitize(source, cancellationToken: cancellation.Token));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(duringRead ? 7 : 0, source.Position);
        Assert.True(source.CanRead);
    }

    [Theory]
    [InlineData("loca")]
    [InlineData("hhea")]
    [InlineData("hmtx")]
    [InlineData("name")]
    [InlineData("OS/2")]
    [InlineData("cmap")]
    public void OtherMalformedTableNumericBoundsKeepDomainFailure(string tag)
    {
        byte[] table = tag switch
        {
            "loca" => new byte[4],
            "hhea" => new byte[36],
            "hmtx" => new byte[3],
            "name" => new byte[6],
            "OS/2" => new byte[77],
            "cmap" => new byte[12],
            _ => throw new ArgumentOutOfRangeException(nameof(tag))
        };
        if (tag == "loca") U16(table, 2, ushort.MaxValue);
        if (tag == "hhea") U16(table, 34, ushort.MaxValue);
        if (tag == "name") { U16(table, 2, 1); U16(table, 4, ushort.MaxValue); }
        if (tag == "cmap") { U16(table, 2, 1); U32(table, 8, uint.MaxValue); }
        AssertMalformed(Record.Exception(() => Sanitize(FontWithTable(tag, table))));
    }

    [Theory]
    [InlineData(0xFFFFFFFCu, 0u)]
    [InlineData(0u, uint.MaxValue)]
    [InlineData(0xFFFFFFFCu, uint.MaxValue)]
    public void DirectoryRangesRejectUnsignedExtremes(uint offset, uint length)
    {
        using MemoryStream fixture = TestFontFactory.CreateMinimalTrueType();
        byte[] bytes = fixture.ToArray();
        U32(bytes, 20, offset);
        U32(bytes, 24, length);
        var failure = Assert.Throws<PdfExternalFontSanitizationException>(() => Sanitize(bytes));
        Assert.Equal(length > 32 * 1024 * 1024
            ? PdfExternalFontSanitizationFailure.ResourceLimitExceeded
            : PdfExternalFontSanitizationFailure.InvalidTableRange, failure.Failure);
    }

    [Fact]
    public void DirectoryLengthOneBeyondEnclosingInputUsesDomainFailure()
    {
        using MemoryStream fixture = TestFontFactory.CreateMinimalTrueType();
        byte[] bytes = fixture.ToArray();
        uint offset = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20));
        U32(bytes, 24, checked((uint)bytes.Length - offset + 1));
        var failure = Assert.Throws<PdfExternalFontSanitizationException>(() => Sanitize(bytes));
        Assert.Equal(PdfExternalFontSanitizationFailure.InvalidTableRange, failure.Failure);
    }

    private sealed class ShortReadStream(byte[] bytes, CancellationTokenSource cancellation = null)
        : MemoryStream(bytes, writable: false)
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            Assert.InRange(count, 1, buffer.Length);
            int read = base.Read(buffer, offset, Math.Min(7, count));
            cancellation?.Cancel();
            return read;
        }
    }
}
