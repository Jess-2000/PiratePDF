using System;
using System.Buffers.Binary;
using System.IO;
using BlackFlag.Engine.Fonts.Sanitization;
using Xunit;

namespace BlackFlag.Engine.Fonts.Tests;

public sealed class PdfOpenTypeFontSanitizerTests
{
    [Fact]
    public void MinimalTrueTypeIsCanonicalizedIntoImmutableTrustedBytes()
    {
        using MemoryStream source =
            TestFontFactory.CreateMinimalTrueType();

        byte[] original =
            source.ToArray();

        PdfSanitizedExternalFontProgram result =
            new PdfOpenTypeFontSanitizer().Sanitize(source);

        Assert.Equal(
            PdfExternalFontTechnology.TrueType,
            result.Technology);

        Assert.Contains("head", result.TableTags);
        Assert.Contains("cmap", result.TableTags);
        Assert.NotEmpty(result.Sha256);
        Assert.NotSame(original, result.Bytes.ToArray());

        byte first = result.Bytes.Span[0];
        byte[] exposed = result.Bytes.ToArray();
        exposed[0] ^= 0xFF;

        Assert.Equal(first, result.Bytes.Span[0]);
    }

    [Theory]
    [InlineData(0x74746366u)]
    [InlineData(0x774F4646u)]
    [InlineData(0x774F4632u)]
    public void CollectionAndWebFontContainersAreRejected(
        uint scalar)
    {
        byte[] bytes = new byte[12];
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(0, 4),
            scalar);

        using var source =
            new MemoryStream(bytes, writable: false);

        PdfExternalFontSanitizationException exception =
            Assert.Throws<PdfExternalFontSanitizationException>(
                () =>
                {
                    new PdfOpenTypeFontSanitizer().Sanitize(source);
                });

        Assert.Equal(
            PdfExternalFontSanitizationFailure.UnsupportedContainer,
            exception.Failure);
    }

    [Fact]
    public void SizeLimitRejectsBeforeUnboundedRead()
    {
        using MemoryStream source =
            TestFontFactory.CreateMinimalTrueType();

        PdfExternalFontSanitizationException exception =
            Assert.Throws<PdfExternalFontSanitizationException>(
                () =>
                {
                    new PdfOpenTypeFontSanitizer().Sanitize(
                        source,
                        new PdfExternalFontSanitizationOptions
                        {
                            MaximumInputBytes = 16
                        });
                });

        Assert.Equal(
            PdfExternalFontSanitizationFailure.InputTooLarge,
            exception.Failure);
    }

    [Fact]
    public void CorruptTableChecksumIsRejected()
    {
        using MemoryStream valid =
            TestFontFactory.CreateMinimalTrueType();

        byte[] bytes = valid.ToArray();
        int cmapRecord =
            FindDirectoryRecord(
                bytes,
                "cmap");
        int cmapOffset =
            checked(
                (int)BinaryPrimitives.ReadUInt32BigEndian(
                    bytes.AsSpan(cmapRecord + 8, 4)));

        bytes[cmapOffset] ^= 0x01;

        using var source =
            new MemoryStream(bytes, writable: false);

        PdfExternalFontSanitizationException exception =
            Assert.Throws<PdfExternalFontSanitizationException>(
                () => new PdfOpenTypeFontSanitizer().Sanitize(source));

        Assert.Equal(
            PdfExternalFontSanitizationFailure.InvalidChecksum,
            exception.Failure);
    }

    [Fact]
    public void DuplicateTableTagIsRejected()
    {
        using MemoryStream valid =
            TestFontFactory.CreateMinimalTrueType();

        byte[] bytes = valid.ToArray();

        // Copy the first table tag over the second directory tag.
        bytes.AsSpan(12, 4)
            .CopyTo(bytes.AsSpan(28, 4));

        using var source =
            new MemoryStream(bytes, writable: false);

        PdfExternalFontSanitizationException exception =
            Assert.Throws<PdfExternalFontSanitizationException>(
                () => new PdfOpenTypeFontSanitizer().Sanitize(source));

        Assert.Equal(
            PdfExternalFontSanitizationFailure.DuplicateTable,
            exception.Failure);
    }

    [Fact]
    public void OutOfRangeTableIsRejected()
    {
        using MemoryStream valid =
            TestFontFactory.CreateMinimalTrueType();

        byte[] bytes = valid.ToArray();

        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(12 + 8, 4),
            checked((uint)(bytes.Length + 4)));

        using var source =
            new MemoryStream(bytes, writable: false);

        PdfExternalFontSanitizationException exception =
            Assert.Throws<PdfExternalFontSanitizationException>(
                () => new PdfOpenTypeFontSanitizer().Sanitize(source));

        Assert.Equal(
            PdfExternalFontSanitizationFailure.InvalidTableRange,
            exception.Failure);
    }

    [Fact]
    public void MalformedCmapSubtableIsRejected()
    {
        using MemoryStream valid =
            TestFontFactory.CreateMinimalTrueType();

        byte[] bytes = valid.ToArray();

        // Locate cmap in the directory and corrupt its declared subtable length.
        int tableCount =
            BinaryPrimitives.ReadUInt16BigEndian(
                bytes.AsSpan(4, 2));

        int cmapOffset = -1;

        for (int index = 0; index < tableCount; index++)
        {
            int record = 12 + (index * 16);
            if (System.Text.Encoding.ASCII.GetString(
                    bytes,
                    record,
                    4) == "cmap")
            {
                cmapOffset =
                    checked((int)BinaryPrimitives.ReadUInt32BigEndian(
                        bytes.AsSpan(record + 8, 4)));
                break;
            }
        }

        Assert.True(cmapOffset >= 0);

        // format-0 length is at cmap + subtable offset(12) + 2.
        BinaryPrimitives.WriteUInt16BigEndian(
            bytes.AsSpan(cmapOffset + 14, 2),
            ushort.MaxValue);

        // Repair the cmap table checksum so rejection reaches structural validation.
        RepairDirectoryChecksum(bytes, "cmap");

        using var source =
            new MemoryStream(bytes, writable: false);

        PdfExternalFontSanitizationException exception =
            Assert.Throws<PdfExternalFontSanitizationException>(
                () => new PdfOpenTypeFontSanitizer().Sanitize(source));

        Assert.Equal(
            PdfExternalFontSanitizationFailure.MalformedRequiredTable,
            exception.Failure);
    }

    private static void RepairDirectoryChecksum(
        byte[] bytes,
        string tag)
    {
        int record =
            FindDirectoryRecord(
                bytes,
                tag);
        int offset =
            checked((int)BinaryPrimitives.ReadUInt32BigEndian(
                bytes.AsSpan(record + 8, 4)));
        int length =
            checked((int)BinaryPrimitives.ReadUInt32BigEndian(
                bytes.AsSpan(record + 12, 4)));

        uint checksum = 0;
        int padded = (length + 3) & ~3;
        Span<byte> word = stackalloc byte[4];

        for (int wordOffset = 0; wordOffset < padded; wordOffset += 4)
        {
            word.Clear();
            int available = Math.Min(4, length - wordOffset);

            if (available > 0)
            {
                bytes.AsSpan(offset + wordOffset, available)
                    .CopyTo(word);
            }

            checksum =
                unchecked(
                    checksum +
                    BinaryPrimitives.ReadUInt32BigEndian(word));
        }

        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(record + 4, 4),
            checksum);
    }

    private static int FindDirectoryRecord(
        byte[] bytes,
        string tag)
    {
        int count =
            BinaryPrimitives.ReadUInt16BigEndian(
                bytes.AsSpan(4, 2));

        for (int index = 0; index < count; index++)
        {
            int record = 12 + (index * 16);

            if (System.Text.Encoding.ASCII.GetString(
                    bytes,
                    record,
                    4) == tag)
            {
                return record;
            }
        }

        throw new InvalidOperationException(
            $"Table '{tag}' was not found.");
    }
}
