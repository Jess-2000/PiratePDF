#nullable enable

using System.IO;
using System.IO.Compression;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.ObjectStreams;

namespace BlackFlag.Engine.Parsing.Tests;

public sealed class PdfObjectStreamParserTests
{
    [Fact]
    public void ParseIndexesAndLazilyReadsObjects()
    {
        PdfStream stream =
            CreateStream(
                objectNumbers:
                [
                    10,
                    20,
                    30
                ],
                objects:
                [
                    "42"u8.ToArray(),
                    "/Example"u8.ToArray(),
                    "[1 2 3]"u8.ToArray()
                ]);

        PdfCompressedObjects result =
            CreateParser().Parse(stream);

        Assert.Equal(
            3,
            result.Entries.Count);

        Assert.Equal(
            10,
            result.Entries[0].ObjectNumber);

        Assert.Equal(
            new PdfInteger(42),
            result.GetByIndex(0));

        Assert.Equal(
            new PdfName("Example"),
            result.GetByObjectNumber(20));

        Assert.Equal(
            new PdfArray(
                [
                    new PdfInteger(1),
                    new PdfInteger(2),
                    new PdfInteger(3)
                ]),
            result.GetByIndex(2));
    }

    [Fact]
    public void ParseDecodesFlateCompressedObjectStream()
    {
        PdfStream uncompressed =
            CreateStream(
                objectNumbers:
                [
                    7
                ],
                objects:
                [
                    "<< /Value 99 >>"u8.ToArray()
                ]);

        byte[] compressed =
            Compress(
                uncompressed.Data.Span);

        PdfDictionary dictionary =
            CopyDictionaryWithFilter(
                uncompressed.Dictionary,
                new PdfName("FlateDecode"));

        var stream =
            new PdfStream(
                dictionary,
                compressed);

        PdfDictionary result =
            Assert.IsType<PdfDictionary>(
                CreateParser()
                    .Parse(stream)
                    .GetByObjectNumber(7));

        Assert.Equal(
            new PdfInteger(99),
            result[
                new PdfName("Value")]);
    }

    [Fact]
    public void GetByIndexReturnsCachedObjectInstance()
    {
        PdfCompressedObjects collection =
            CreateParser().Parse(
                CreateStream(
                    objectNumbers:
                    [
                        5
                    ],
                    objects:
                    [
                        "(cached)"u8.ToArray()
                    ]));

        PdfObject first =
            collection.GetByIndex(0);

        PdfObject second =
            collection.GetByIndex(0);

        Assert.Same(
            first,
            second);
    }

    [Fact]
    public void ParseRejectsMissingObjectStreamType()
    {
        PdfStream valid =
            CreateStream(
                objectNumbers:
                [
                    1
                ],
                objects:
                [
                    "true"u8.ToArray()
                ]);

        var entries =
            valid.Dictionary
                .Where(
                    pair =>
                        pair.Key !=
                        new PdfName("Type"));

        var stream =
            new PdfStream(
                new PdfDictionary(entries),
                valid.Data);

        Assert.Throws<FormatException>(
            () => CreateParser().Parse(stream));
    }

    [Fact]
    public void ParseRejectsDuplicateObjectNumbers()
    {
        Assert.Throws<FormatException>(
            () => CreateParser().Parse(
                CreateStream(
                    objectNumbers:
                    [
                        4,
                        4
                    ],
                    objects:
                    [
                        "1"u8.ToArray(),
                        "2"u8.ToArray()
                    ])));
    }

    [Fact]
    public void ParseRejectsNonIncreasingOffsets()
    {
        byte[] bytes =
            "1 0 2 0 10 20"u8.ToArray();

        PdfDictionary dictionary =
            CreateDictionary(
                count: 2,
                first: 8);

        Assert.Throws<FormatException>(
            () => CreateParser().Parse(
                new PdfStream(
                    dictionary,
                    bytes)));
    }

    [Fact]
    public void ParseRejectsFirstBeyondDecodedLength()
    {
        PdfDictionary dictionary =
            CreateDictionary(
                count: 1,
                first: 100);

        Assert.Throws<FormatException>(
            () => CreateParser().Parse(
                new PdfStream(
                    dictionary,
                    "1 0 42"u8.ToArray())));
    }

    [Fact]
    public void GetByIndexRejectsTrailingSyntax()
    {
        PdfCompressedObjects collection =
            CreateParser().Parse(
                CreateStream(
                    objectNumbers:
                    [
                        3
                    ],
                    objects:
                    [
                        "1 2"u8.ToArray()
                    ]));

        Assert.Throws<FormatException>(
            () => collection.GetByIndex(0));
    }

    [Fact]
    public void ParseRejectsMoreHeaderPairsThanN()
    {
        byte[] bytes =
            "1 0 2 2 42"u8.ToArray();

        PdfDictionary dictionary =
            CreateDictionary(
                count: 1,
                first: 8);

        Assert.Throws<FormatException>(
            () => CreateParser().Parse(
                new PdfStream(
                    dictionary,
                    bytes)));
    }

    private static PdfObjectStreamParser CreateParser()
    {
        return new PdfObjectStreamParser(
            new PdfFilterPipeline(
                PdfFilterRegistrations.CreateDefault()),
            new PdfFilterDecodeLimits(
                maximumDecodedBytes: 1024 * 1024,
                maximumExpansionRatio: 1000,
                maximumFilterCount: 8,
                bufferSize: 256));
    }

    private static PdfStream CreateStream(
        int[] objectNumbers,
        byte[][] objects)
    {
        Assert.Equal(
            objectNumbers.Length,
            objects.Length);

        using var body =
            new MemoryStream();

        var offsets =
            new int[objects.Length];

        for (int index = 0; index < objects.Length; index++)
        {
            offsets[index] =
                checked((int)body.Length);

            body.Write(objects[index]);

            if (index + 1 < objects.Length)
            {
                body.WriteByte(
                    (byte)' ');
            }
        }

        string header =
            string.Join(
                " ",
                objectNumbers.SelectMany(
                    (objectNumber, index) =>
                        new[]
                        {
                            objectNumber,
                            offsets[index]
                        })) +
            " ";

        byte[] headerBytes =
            System.Text.Encoding.ASCII.GetBytes(
                header);

        byte[] decoded =
            new byte[
                checked(
                    headerBytes.Length +
                    body.Length)];

        headerBytes.CopyTo(
            decoded,
            0);

        body.ToArray().CopyTo(
            decoded,
            headerBytes.Length);

        return new PdfStream(
            CreateDictionary(
                objectNumbers.Length,
                headerBytes.Length),
            decoded);
    }

    private static PdfDictionary CreateDictionary(
        int count,
        int first)
    {
        return new PdfDictionary(
            [
                new KeyValuePair<PdfName, PdfObject>(
                    new PdfName("Type"),
                    new PdfName("ObjStm")),
                new KeyValuePair<PdfName, PdfObject>(
                    new PdfName("N"),
                    new PdfInteger(count)),
                new KeyValuePair<PdfName, PdfObject>(
                    new PdfName("First"),
                    new PdfInteger(first))
            ]);
    }

    private static PdfDictionary CopyDictionaryWithFilter(
        PdfDictionary dictionary,
        PdfObject filter)
    {
        var entries =
            dictionary.ToList();

        entries.Add(
            new KeyValuePair<PdfName, PdfObject>(
                new PdfName("Filter"),
                filter));

        return new PdfDictionary(entries);
    }

    private static byte[] Compress(
        ReadOnlySpan<byte> data)
    {
        using var destination =
            new MemoryStream();

        using (
            var stream =
                new ZLibStream(
                    destination,
                    CompressionLevel.SmallestSize,
                    leaveOpen: true))
        {
            stream.Write(data);
        }

        return destination.ToArray();
    }
}

