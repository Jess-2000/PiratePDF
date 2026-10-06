using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BlackFlag.Engine.Fonts.Sanitization;
using Xunit;
using Xunit.Abstractions;

namespace BlackFlag.Engine.Fonts.Tests;

public sealed partial class PdfOpenTypeFontSanitizerNumericTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public void UnsignedCmapLengthUsesDomainFailure(ushort format)
    {
        byte[] subtable = Subtable(format, format == 14 ? 10 : 16);
        U32(subtable, format == 14 ? 2 : 4, uint.MaxValue);
        Exception failure = Record.Exception(() => Sanitize(FontWithSubtable(subtable)));
        LogFailure(failure);
        AssertMalformed(failure);
    }

    [Fact]
    public void MaximumIntInputBudgetAcceptsTinyValidFont()
    {
        using MemoryStream source = TestFontFactory.CreateMinimalTrueType();
        Exception failure = Record.Exception(() => new PdfOpenTypeFontSanitizer().Sanitize(
            source, new PdfExternalFontSanitizationOptions { MaximumInputBytes = int.MaxValue }));
        LogFailure(failure);
        Assert.Null(failure);
    }

    [Fact]
    public void UnrepresentableSearchFieldsUseDomainFailure()
    {
        byte[] bytes = new byte[12 + 4096 * 16];
        U32(bytes, 0, 0x00010000);
        U16(bytes, 4, 4096);
        using var source = new MemoryStream(bytes);
        Exception failure = Record.Exception(() => new PdfOpenTypeFontSanitizer().Sanitize(
            source, new PdfExternalFontSanitizationOptions { MaximumTableCount = 4096 }));
        LogFailure(failure);
        var domain = Assert.IsType<PdfExternalFontSanitizationException>(failure);
        Assert.Equal(PdfExternalFontSanitizationFailure.MalformedHeader, domain.Failure);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public void ImpossibleRecordCountIsRejectedBeforePublication(ushort format)
    {
        byte[] subtable = Subtable(format, format == 14 ? 10 : 16);
        U32(subtable, format == 14 ? 6 : 12, uint.MaxValue);
        Exception failure = Record.Exception(() => Sanitize(FontWithSubtable(subtable)));
        LogFailure(failure);
        AssertMalformed(failure);
    }

    private void LogFailure(Exception failure) => output.WriteLine(
        $"Type={failure?.GetType().FullName ?? "none"}; Message={failure?.Message}; Inner={failure?.InnerException?.GetType().FullName ?? "none"}");

    private static void AssertMalformed(Exception failure)
    {
        var domain = Assert.IsType<PdfExternalFontSanitizationException>(failure);
        Assert.Equal(PdfExternalFontSanitizationFailure.MalformedRequiredTable, domain.Failure);
        Assert.Null(domain.InnerException);
    }

    private static PdfSanitizedExternalFontProgram Sanitize(byte[] bytes)
    {
        using var source = new MemoryStream(bytes, writable: false);
        return new PdfOpenTypeFontSanitizer().Sanitize(source);
    }

    private static byte[] Subtable(ushort format, int length)
    {
        byte[] result = new byte[length];
        U16(result, 0, format);
        U32(result, format == 14 ? 2 : 4, checked((uint)length));
        return result;
    }

    private static byte[] FontWithSubtable(byte[] subtable)
    {
        byte[] cmap = new byte[12 + subtable.Length];
        int format = subtable.Length >= 2 ? BinaryPrimitives.ReadUInt16BigEndian(subtable) : -1;
        U16(cmap, 2, 1);
        U16(cmap, 4, format is 13 or 14 ? (ushort)0 : (ushort)3);
        U16(cmap, 6, format == 14 ? (ushort)5 : format == 13 ? (ushort)6 : (ushort)10);
        U32(cmap, 8, 12);
        subtable.CopyTo(cmap, 12);
        return FontWithTable("cmap", cmap);
    }

    private static byte[] FontWithTable(string tag, byte[] value, uint scalar = 0x00010000)
    {
        using MemoryStream source = TestFontFactory.CreateMinimalTrueType();
        byte[] bytes = source.ToArray();
        var tables = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        int count = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(4));
        for (int index = 0; index < count; index++)
        {
            int record = 12 + index * 16;
            string tableTag = Encoding.ASCII.GetString(bytes, record, 4);
            int offset = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(record + 8)));
            int length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(record + 12)));
            tables.Add(tableTag, bytes.AsSpan(offset, length).ToArray());
        }

        tables[tag] = value;
        return TestFontFactory.BuildSfnt(scalar, tables);
    }

    private static void U16(Span<byte> bytes, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(bytes[offset..], value);

    private static void U32(Span<byte> bytes, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(bytes[offset..], value);
}
