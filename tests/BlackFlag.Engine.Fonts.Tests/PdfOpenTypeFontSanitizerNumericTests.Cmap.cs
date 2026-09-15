using System;
using System.IO;
using BlackFlag.Engine.Fonts.Sanitization;
using Xunit;

namespace BlackFlag.Engine.Fonts.Tests;

public sealed partial class PdfOpenTypeFontSanitizerNumericTests
{
    [Theory]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public void DeclaredLengthsAreCheckedAgainstHeaderAndEnclosingTable(ushort format)
    {
        int header = format == 14 ? 10 : 16;
        byte[] subtable = Subtable(format, header);
        AssertCanonical(FontWithSubtable(subtable));
        foreach (uint length in new uint[] { 0, 1, (uint)header - 1, (uint)header + 1,
            int.MaxValue, (uint)int.MaxValue + 1, uint.MaxValue })
        {
            U32(subtable, format == 14 ? 2 : 4, length);
            AssertMalformed(Record.Exception(() => Sanitize(FontWithSubtable(subtable))));
        }

        // Every proper header prefix, with a self-consistent shortened length
        // where the length field itself is present, must still be rejected.
        subtable = Subtable(format, header);
        for (int size = 0; size < header; size++)
        {
            byte[] prefix = subtable[..size];
            if (size >= (format == 14 ? 6 : 8))
            {
                U32(prefix, format == 14 ? 2 : 4, (uint)size);
            }
            AssertMalformed(Record.Exception(() => Sanitize(FontWithSubtable(prefix))));
        }
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    public void OtherUnsignedCmapLengthFieldsAlsoRejectBeforeNarrowing(ushort format)
    {
        byte[] subtable = Subtable(format, 8);
        U32(subtable, 4, uint.MaxValue);
        AssertMalformed(Record.Exception(() => Sanitize(FontWithSubtable(subtable))));
    }

    [Theory]
    [InlineData(12)]
    [InlineData(13)]
    public void GroupArraysUseDeclaredLengthAndOverflowSafeCounts(ushort format)
    {
        byte[] subtable = Subtable(format, 28);
        U32(subtable, 12, 1);
        U32(subtable, 16, 65);
        U32(subtable, 20, 65);
        AssertCanonical(FontWithSubtable(subtable));

        foreach (uint count in new uint[] { 2, 178956970, 357913942, int.MaxValue, uint.MaxValue })
        {
            U32(subtable, 12, count);
            AssertMalformed(Record.Exception(() => Sanitize(FontWithSubtable(subtable))));
        }

        U32(subtable, 12, 1);
        U32(subtable, 4, 27);
        AssertMalformed(Record.Exception(() => Sanitize(FontWithSubtable(subtable))));
        U32(subtable, 4, 16);
        AssertMalformed(Record.Exception(() => Sanitize(FontWithSubtable(subtable))));
        U32(subtable, 12, 0);
        AssertCanonical(FontWithSubtable(subtable)); // Enclosing trailing bytes remain allowed.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VariationOffsetsAndNestedCountsStayInsideDeclaredSubtable(bool nonDefault)
    {
        int stride = nonDefault ? 5 : 4;
        int field = nonDefault ? 17 : 13;
        byte[] subtable = Subtable(14, 25 + stride);
        U32(subtable, 6, 1);
        subtable[10] = 0x0E;
        subtable[12] = 1;
        U32(subtable, field, 21);
        U32(subtable, 21, 1);
        subtable[27] = 65;
        AssertCanonical(FontWithSubtable(subtable));

        foreach (uint offset in new uint[] { (uint)subtable.Length, (uint)subtable.Length - 3,
            int.MaxValue, (uint)int.MaxValue + 1, uint.MaxValue })
        {
            U32(subtable, field, offset);
            AssertMalformed(Record.Exception(() => Sanitize(FontWithSubtable(subtable))));
        }

        U32(subtable, field, 21);
        foreach (uint count in new uint[] { 2, 858993460, 1073741824, uint.MaxValue })
        {
            U32(subtable, 21, count);
            AssertMalformed(Record.Exception(() => Sanitize(FontWithSubtable(subtable))));
        }

        U32(subtable, 21, 1);
        U32(subtable, 2, (uint)subtable.Length - 1);
        AssertMalformed(Record.Exception(() => Sanitize(FontWithSubtable(subtable))));
        U32(subtable, 21, 0);
        U32(subtable, 2, 25);
        AssertCanonical(FontWithSubtable(subtable)); // Four-byte nested header exactly fits.
        U32(subtable, field, 0);
        U32(subtable, 2, 21);
        AssertCanonical(FontWithSubtable(subtable)); // Both optional offsets may be absent.
    }

    [Fact]
    public void VariationSelectorArrayCountIsBoundedBeforeTraversal()
    {
        byte[] subtable = Subtable(14, 21);
        U32(subtable, 6, 1);
        subtable[10] = 0x0E;
        subtable[12] = 1;
        AssertCanonical(FontWithSubtable(subtable));
        foreach (uint count in new uint[] { 2, 195225787, 390451573, uint.MaxValue })
        {
            U32(subtable, 6, count);
            AssertMalformed(Record.Exception(() => Sanitize(FontWithSubtable(subtable))));
        }
        U32(subtable, 6, 1);
        U32(subtable, 2, 20);
        AssertMalformed(Record.Exception(() => Sanitize(FontWithSubtable(subtable))));
    }

    private static void AssertCanonical(byte[] bytes)
    {
        PdfSanitizedExternalFontProgram result = Sanitize(bytes);
        Assert.Equal(bytes, result.Bytes.ToArray());
        Assert.Equal(result.Sha256, Sanitize(result.Bytes.ToArray()).Sha256);
    }
}
