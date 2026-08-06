#nullable enable
using System.IO;
using System.IO.Compression;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;

namespace BlackFlag.Engine.Parsing.Tests;

public sealed class PdfCrossReferenceStreamParserTests

{
    [Fact]
    public void ParseDecodesDefaultIndexAndEntryTypes()

    {
        PdfStream stream =

            CreateStream(

                size: 3,

                widths:

                [

                    1,

                    2,

                    1

                ],

                data:

                [

                    0,

                    0,

                    0,

                    255,

                    1,

                    0,

                    42,

                    0,

                    2,

                    0,

                    9,

                    3

                ]);

        PdfCrossReference result =

            CreateParser().Parse(

                offset: 100,

                stream);

        Assert.Equal(

            100,

            result.Offset);

        PdfCrossReferenceStreamSection section =

            Assert.Single(

                result.Sections);

        Assert.Equal(

            0,

            section.FirstObjectNumber);

        Assert.Equal(

            PdfCrossReferenceStreamEntryKind.Free,

            section.Entries[0].Kind);

        Assert.Equal(

            255,

            section.Entries[0].ObjectId.GenerationNumber);

        Assert.Equal(

            PdfCrossReferenceStreamEntryKind.InUse,

            section.Entries[1].Kind);

        Assert.Equal(

            42,

            section.Entries[1].Field2);

        Assert.Equal(

            PdfCrossReferenceStreamEntryKind.Compressed,

            section.Entries[2].Kind);

        Assert.Equal(

            9,

            section.Entries[2].Field2);

        Assert.Equal(

            3,

            section.Entries[2].Field3);
    }

    [Fact]
    public void ParseUsesDefaultTypeWhenFirstWidthIsZero()

    {
        PdfStream stream =

            CreateStream(

                size: 1,

                widths:

                [

                    0,

                    2,

                    1

                ],

                data:

                [

                    0,

                    25,

                    0

                ]);

        PdfCrossReferenceStreamEntry entry =

            Assert.Single(

                Assert.Single(

                    CreateParser()

                        .Parse(

                            offset: 5,

                            stream)

                        .Sections)

                    .Entries);

        Assert.Equal(

            PdfCrossReferenceStreamEntryKind.InUse,

            entry.Kind);

        Assert.Equal(

            25,

            entry.Field2);
    }

    [Fact]
    public void ParseSupportsIndexRanges()

    {
        PdfStream stream =

            CreateStream(

                size: 8,

                widths:

                [

                    1,

                    1,

                    1

                ],

                index:

                [

                    2,

                    2,

                    7,

                    1

                ],

                data:

                [

                    1,

                    10,

                    0,

                    1,

                    20,

                    0,

                    2,

                    5,

                    4

                ]);

        PdfCrossReference result =

            CreateParser().Parse(

                offset: 0,

                stream);

        Assert.Equal(

            2,

            result.Sections.Count);

        Assert.Equal(

            2,

            result.Sections[0].FirstObjectNumber);

        Assert.Equal(

            7,

            result.Sections[1].FirstObjectNumber);
    }

    [Fact]
    public void ParseDecodesFlateCompressedEntries()

    {
        byte[] decoded =

        [

            1,

            0,

            64,

            0

        ];

        PdfStream stream =

            CreateStream(

                size: 1,

                widths:

                [

                    1,

                    2,

                    1

                ],

                data:

                    Compress(decoded),

                filter:

                    new PdfName("FlateDecode"));

        PdfCrossReferenceStreamEntry entry =

            Assert.Single(

                Assert.Single(

                    CreateParser()

                        .Parse(

                            offset: 25,

                            stream)

                        .Sections)

                    .Entries);

        Assert.Equal(

            PdfCrossReferenceStreamEntryKind.InUse,

            entry.Kind);

        Assert.Equal(

            64,

            entry.Field2);
    }

    [Fact]
    public void ParseRejectsMissingType()

    {
        PdfDictionary dictionary =

            CreateDictionary(

                size: 1,

                widths:

                [

                    1,

                    1,

                    1

                ],

                includeType: false);

        var stream =

            new PdfStream(

                dictionary,

                new byte[]

                {

                    1,

                    0,

                    0

                });

        Assert.Throws<FormatException>(

            () => CreateParser().Parse(

                offset: 0,

                stream));
    }

    [Fact]
    public void ParseRejectsInvalidWidths()

    {
        PdfStream stream =

            CreateStream(

                size: 1,

                widths:

                [

                    1,

                    0,

                    1

                ],

                data:

                [

                    1,

                    0

                ]);

        Assert.Throws<FormatException>(

            () => CreateParser().Parse(

                offset: 0,

                stream));
    }

    [Fact]
    public void ParseRejectsOverlappingIndexRanges()

    {
        PdfStream stream =

            CreateStream(

                size: 5,

                widths:

                [

                    1,

                    1,

                    1

                ],

                index:

                [

                    0,

                    3,

                    2,

                    2

                ],

                data:

                    new byte[15]);

        Assert.Throws<FormatException>(

            () => CreateParser().Parse(

                offset: 0,

                stream));
    }

    [Fact]
    public void ParseRejectsDecodedLengthMismatch()

    {
        PdfStream stream =

            CreateStream(

                size: 2,

                widths:

                [

                    1,

                    1,

                    1

                ],

                data:

                [

                    1,

                    0,

                    0

                ]);

        Assert.Throws<FormatException>(

            () => CreateParser().Parse(

                offset: 0,

                stream));
    }

    [Fact]
    public void ParseRejectsUnsupportedEntryType()

    {
        PdfStream stream =

            CreateStream(

                size: 1,

                widths:

                [

                    1,

                    1,

                    1

                ],

                data:

                [

                    3,

                    0,

                    0

                ]);

        Assert.Throws<FormatException>(

            () => CreateParser().Parse(

                offset: 0,

                stream));
    }

    private static PdfCrossReferenceStreamParser CreateParser()

    {
        return new PdfCrossReferenceStreamParser(

            new PdfFilterPipeline(

                PdfFilterRegistrations.CreateDefault()),

            new PdfFilterDecodeLimits(

                maximumDecodedBytes: 1024 * 1024,

                maximumExpansionRatio: 1000,

                maximumFilterCount: 8,

                bufferSize: 256));
    }

    private static PdfStream CreateStream(

        int size,

        int[] widths,

        byte[] data,

        int[]? index = null,

        PdfObject? filter = null)

    {

        return new PdfStream(

            CreateDictionary(

                size,

                widths,

                index,

                filter),

            data);
    }

    private static PdfDictionary CreateDictionary(

        int size,

        int[] widths,

        int[]? index = null,

        PdfObject? filter = null,

        bool includeType = true)

    {

        var entries =

            new List<KeyValuePair<PdfName, PdfObject>>();



        if (includeType)

        {

            entries.Add(

                new KeyValuePair<PdfName, PdfObject>(

                    new PdfName("Type"),

                    new PdfName("XRef")));

        }

        entries.Add(

            new KeyValuePair<PdfName, PdfObject>(

                new PdfName("Size"),

                new PdfInteger(size)));

        entries.Add(

            new KeyValuePair<PdfName, PdfObject>(

                new PdfName("W"),

                new PdfArray(

                    widths.Select(

                        value =>

                            (PdfObject)new PdfInteger(value)))));

        if (index is not null)

        {
            entries.Add(

                new KeyValuePair<PdfName, PdfObject>(

                    new PdfName("Index"),

                    new PdfArray(

                        index.Select(

                            value =>

                                (PdfObject)new PdfInteger(value)))));
        }

        if (filter is not null)

        {
            entries.Add(

                new KeyValuePair<PdfName, PdfObject>(

                    new PdfName("Filter"),

                    filter));
        }

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


