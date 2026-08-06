using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;

namespace BlackFlag.Engine.Parsing.Tests;

public sealed class PdfCrossReferenceParserTests
{
    [Fact]
    public void ObjectIdRejectsNegativeObjectNumber()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfObjectId(
                objectNumber: -1,
                generationNumber: 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    public void ObjectIdRejectsInvalidGenerationNumber(
        int generationNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfObjectId(
                objectNumber: 1,
                generationNumber));
    }

    [Fact]
    public void ObjectIdSupportsFreeListHead()
    {
        var objectId =
            new PdfObjectId(
                objectNumber: 0,
                generationNumber: 65535);

        Assert.Equal(0, objectId.ObjectNumber);
        Assert.Equal(65535, objectId.GenerationNumber);
        Assert.Equal("0 65535", objectId.ToString());
    }

    [Fact]
    public void EntryRejectsNegativeOffset()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfCrossReferenceEntry(
                new PdfObjectId(1, 0),
                offset: -1,
                isInUse: true));
    }

    [Fact]
    public void SectionRejectsNonSequentialEntryNumbers()
    {
        PdfCrossReferenceEntry[] entries =
        [
            CreateEntry(
                objectNumber: 5,
                generationNumber: 0,
                offset: 20,
                isInUse: true),
            CreateEntry(
                objectNumber: 7,
                generationNumber: 0,
                offset: 30,
                isInUse: true)
        ];

        Assert.Throws<ArgumentException>(
            () => new PdfCrossReferenceSection(
                firstObjectNumber: 5,
                entries));
    }

    [Fact]
    public void SectionCopiesEntries()
    {
        PdfCrossReferenceEntry[] entries =
        [
            CreateEntry(
                objectNumber: 2,
                generationNumber: 0,
                offset: 25,
                isInUse: true)
        ];

        var section =
            new PdfCrossReferenceSection(
                firstObjectNumber: 2,
                entries);

        entries[0] =
            CreateEntry(
                objectNumber: 2,
                generationNumber: 0,
                offset: 99,
                isInUse: true);

        Assert.Equal(
            25,
            section.Entries[0].Offset);
    }

    [Fact]
    public void TableRejectsDuplicateObjectIdentifiers()
    {
        PdfCrossReferenceEntry entry =
            CreateEntry(
                objectNumber: 1,
                generationNumber: 0,
                offset: 20,
                isInUse: true);

        var first =
            new PdfCrossReferenceSection(
                firstObjectNumber: 1,
                [entry]);

        var second =
            new PdfCrossReferenceSection(
                firstObjectNumber: 1,
                [entry]);

        Assert.Throws<ArgumentException>(
            () => new PdfCrossReferenceTable(
                offset: 10,
                [first, second],
                PdfDictionary.Empty));
    }

    [Fact]
    public void ChainCopiesTablesAndEntries()
    {
        PdfCrossReferenceEntry entry =
            CreateEntry(
                objectNumber: 1,
                generationNumber: 0,
                offset: 20,
                isInUse: true);

        PdfCrossReferenceTable table =
            CreateTable(
                offset: 10,
                entry);

        var tables =
            new List<PdfCrossReferenceTable>
            {
                table
            };

        var entries =
            new Dictionary<PdfObjectId, PdfCrossReferenceEntry>
            {
                [entry.ObjectId] = entry
            };

        var chain =
            new PdfCrossReferenceChain(
                tables,
                entries);

        tables.Clear();
        entries.Clear();

        Assert.Single(chain.Tables);
        Assert.Single(chain.Entries);
    }

    [Fact]
    public void ParseReadsSingleTraditionalTable()
    {
        const string xref =
            """
            xref
            0 3
            0000000000 65535 f
            0000000017 00000 n
            0000000081 00002 n
            trailer
            << /Size 3 /Root 1 0 R >>

            """;

        using TestRandomAccessSource source =
            CreatePdf(
                prefix: "%PDF-1.7\n",
                xref);

        var parser =
            new PdfCrossReferenceParser(source);

        PdfCrossReferenceChain chain =
            parser.Parse();

        PdfCrossReferenceTable table =
            Assert.Single(chain.Tables);

        PdfCrossReferenceSection section =
            Assert.Single(table.Sections);

        Assert.Equal(0, section.FirstObjectNumber);
        Assert.Equal(3, section.Entries.Count);

        PdfCrossReferenceEntry freeEntry =
            chain.Entries[
                new PdfObjectId(0, 65535)];

        Assert.False(freeEntry.IsInUse);
        Assert.Equal(0, freeEntry.Offset);

        PdfCrossReferenceEntry firstObject =
            chain.Entries[
                new PdfObjectId(1, 0)];

        Assert.True(firstObject.IsInUse);
        Assert.Equal(17, firstObject.Offset);

        PdfCrossReferenceEntry secondObject =
            chain.Entries[
                new PdfObjectId(2, 2)];

        Assert.Equal(81, secondObject.Offset);

        Assert.Equal(
            3,
            Assert.IsType<PdfInteger>(
                table.Trailer[
                    new PdfName("Size")])
                .Value);

        PdfIndirectReference root =
            Assert.IsType<PdfIndirectReference>(
                table.Trailer[
                    new PdfName("Root")]);

        Assert.Equal(1, root.ObjectNumber);
        Assert.Equal(0, root.GenerationNumber);
    }

    [Fact]
    public void ParseReadsMultipleSubsectionsAndCrLfLineEndings()
    {
        const string prefix =
            "%PDF-1.7\r\n";

        const string xref =
            "xref\r\n"
            + "0 1\r\n"
            + "0000000000 65535 f\r\n"
            + "5 2\r\n"
            + "0000000100 00000 n\r\n"
            + "0000000200 00001 n\r\n"
            + "trailer\r\n"
            + "<< /Size 7 >>\r\n";

        using TestRandomAccessSource source =
            CreatePdf(
                prefix,
                xref,
                lineEnding: "\r\n");

        var parser =
            new PdfCrossReferenceParser(source);

        PdfCrossReferenceChain chain =
            parser.Parse();

        PdfCrossReferenceTable table =
            Assert.Single(chain.Tables);

        Assert.Equal(2, table.Sections.Count);
        Assert.Equal(0, table.Sections[0].FirstObjectNumber);
        Assert.Equal(5, table.Sections[1].FirstObjectNumber);
        Assert.Equal(
            200,
            chain.Entries[
                new PdfObjectId(6, 1)]
                .Offset);
    }

    [Fact]
    public void ParseAllowsZeroEntrySubsection()
    {
        const string xref =
            """
            xref
            0 0
            trailer
            << /Size 0 >>

            """;

        using TestRandomAccessSource source =
            CreatePdf(
                prefix: "%PDF-1.7\n",
                xref);

        var parser =
            new PdfCrossReferenceParser(source);

        PdfCrossReferenceChain chain =
            parser.Parse();

        Assert.Empty(
            Assert.Single(
                Assert.Single(chain.Tables).Sections)
                .Entries);

        Assert.Empty(chain.Entries);
    }

    [Fact]
    public void ParseUsesLastStartXRefSection()
    {
        var content =
            new StringBuilder();

        content.Append(
            "%PDF-1.7\n"
            + "startxref\n"
            + "0\n"
            + "%%EOF\n");

        long finalXrefOffset =
            GetByteCount(content);

        content.Append(
            "xref\n"
            + "0 1\n"
            + "0000000000 65535 f\n"
            + "trailer\n"
            + "<< /Size 1 >>\n"
            + "startxref\n");

        content.Append(finalXrefOffset);
        content.Append(
            "\n"
            + "%%EOF\n");

        using var source =
            new TestRandomAccessSource(
                Encoding.ASCII.GetBytes(
                    content.ToString()));

        var parser =
            new PdfCrossReferenceParser(source);

        PdfCrossReferenceChain chain =
            parser.Parse();

        Assert.Equal(
            finalXrefOffset,
            Assert.Single(chain.Tables).Offset);
    }

    [Fact]
    public void ParseFollowsPrevAndBuildsOldestToNewestChain()
    {
        var content =
            new StringBuilder();

        content.Append("%PDF-1.7\n");

        long oldestOffset =
            GetByteCount(content);

        content.Append(
            "xref\n"
            + "0 2\n"
            + "0000000000 65535 f\n"
            + "0000000010 00000 n\n"
            + "trailer\n"
            + "<< /Size 2 >>\n");

        long newestOffset =
            GetByteCount(content);

        content.Append(
            "xref\n"
            + "1 1\n"
            + "0000000999 00000 n\n"
            + "trailer\n"
            + "<< /Size 2 /Prev ");

        content.Append(oldestOffset);
        content.Append(
            " >>\n"
            + "startxref\n");

        content.Append(newestOffset);
        content.Append(
            "\n"
            + "%%EOF\n");

        using var source =
            new TestRandomAccessSource(
                Encoding.ASCII.GetBytes(
                    content.ToString()));

        var parser =
            new PdfCrossReferenceParser(source);

        PdfCrossReferenceChain chain =
            parser.Parse();

        Assert.Equal(2, chain.Tables.Count);
        Assert.Equal(oldestOffset, chain.Tables[0].Offset);
        Assert.Equal(newestOffset, chain.Tables[1].Offset);

        Assert.Equal(
            999,
            chain.Entries[
                new PdfObjectId(1, 0)]
                .Offset);
    }

    [Fact]
    public void ParseRetainsDifferentGenerationsInEffectiveLookup()
    {
        var content =
            new StringBuilder();

        content.Append("%PDF-1.7\n");

        long oldestOffset =
            GetByteCount(content);

        content.Append(
            "xref\n"
            + "1 1\n"
            + "0000000010 00000 n\n"
            + "trailer\n"
            + "<< /Size 2 >>\n");

        long newestOffset =
            GetByteCount(content);

        content.Append(
            "xref\n"
            + "1 1\n"
            + "0000000099 00001 n\n"
            + "trailer\n"
            + "<< /Size 2 /Prev ");

        content.Append(oldestOffset);
        content.Append(
            " >>\n"
            + "startxref\n");

        content.Append(newestOffset);
        content.Append(
            "\n"
            + "%%EOF\n");

        using var source =
            new TestRandomAccessSource(
                Encoding.ASCII.GetBytes(
                    content.ToString()));

        PdfCrossReferenceChain chain =
            new PdfCrossReferenceParser(source)
                .Parse();

        Assert.Equal(2, chain.Entries.Count);
        Assert.Equal(
            10,
            chain.Entries[
                new PdfObjectId(1, 0)]
                .Offset);
        Assert.Equal(
            99,
            chain.Entries[
                new PdfObjectId(1, 1)]
                .Offset);
    }

    [Fact]
    public void ParseRejectsPrevCycle()
    {
        var content =
            new StringBuilder();

        content.Append("%PDF-1.7\n");

        long xrefOffset =
            GetByteCount(content);

        content.Append(
            "xref\n"
            + "0 1\n"
            + "0000000000 65535 f\n"
            + "trailer\n"
            + "<< /Size 1 /Prev ");

        content.Append(xrefOffset);
        content.Append(
            " >>\n"
            + "startxref\n");

        content.Append(xrefOffset);
        content.Append(
            "\n"
            + "%%EOF\n");

        using var source =
            new TestRandomAccessSource(
                Encoding.ASCII.GetBytes(
                    content.ToString()));

        var parser =
            new PdfCrossReferenceParser(source);

        Assert.Throws<FormatException>(
            parser.Parse);
    }

    [Theory]
    [InlineData("")]
    [InlineData("%PDF-1.7\nstartxref\n0\n")]
    [InlineData("%PDF-1.7\n%%EOF\n")]
    [InlineData("%PDF-1.7\nstartxref\n9999\n%%EOF\n")]
    public void ParseRejectsInvalidStartXRef(
        string content)
    {
        using var source =
            new TestRandomAccessSource(
                Encoding.ASCII.GetBytes(content));

        var parser =
            new PdfCrossReferenceParser(source);

        Assert.Throws<FormatException>(
            parser.Parse);
    }

    [Theory]
    [InlineData(
        "xref\n"
        + "0\n"
        + "trailer\n"
        + "<< /Size 0 >>\n")]
    [InlineData(
        "xref\n"
        + "0 one\n"
        + "trailer\n"
        + "<< /Size 0 >>\n")]
    [InlineData(
        "xref\n"
        + "-1 1\n"
        + "0000000000 65535 f\n"
        + "trailer\n"
        + "<< /Size 1 >>\n")]
    [InlineData(
        "xref\n"
        + "0 -1\n"
        + "trailer\n"
        + "<< /Size 0 >>\n")]
    public void ParseRejectsInvalidSubsectionHeader(
        string xref)
    {
        using TestRandomAccessSource source =
            CreatePdf(
                prefix: "%PDF-1.7\n",
                xref);

        var parser =
            new PdfCrossReferenceParser(source);

        Assert.ThrowsAny<Exception>(
            parser.Parse);
    }

    [Theory]
    [InlineData("000000000 00000 n")]
    [InlineData("00000000000 00000 n")]
    [InlineData("0000000000 0000 n")]
    [InlineData("0000000000 000000 n")]
    [InlineData("0000000000 00000 x")]
    [InlineData("abcdefghij 00000 n")]
    [InlineData("0000000000 abcde n")]
    public void ParseRejectsMalformedEntry(
        string entry)
    {
        string xref =
            "xref\n"
            + "0 1\n"
            + entry
            + "\n"
            + "trailer\n"
            + "<< /Size 1 >>\n";

        using TestRandomAccessSource source =
            CreatePdf(
                prefix: "%PDF-1.7\n",
                xref);

        var parser =
            new PdfCrossReferenceParser(source);

        Assert.Throws<FormatException>(
            parser.Parse);
    }

    [Fact]
    public void ParseRejectsMissingTrailer()
    {
        const string xref =
            """
            xref
            0 1
            0000000000 65535 f

            """;

        using TestRandomAccessSource source =
            CreatePdf(
                prefix: "%PDF-1.7\n",
                xref);

        var parser =
            new PdfCrossReferenceParser(source);

        Assert.ThrowsAny<Exception>(
            parser.Parse);
    }

    [Fact]
    public void ParseRejectsNonDictionaryTrailer()
    {
        const string xref =
            """
            xref
            0 1
            0000000000 65535 f
            trailer
            42

            """;

        using TestRandomAccessSource source =
            CreatePdf(
                prefix: "%PDF-1.7\n",
                xref);

        var parser =
            new PdfCrossReferenceParser(source);

        Assert.Throws<FormatException>(
            parser.Parse);
    }

    [Fact]
    public void ParseRejectsNonIntegerPrev()
    {
        const string xref =
            """
            xref
            0 1
            0000000000 65535 f
            trailer
            << /Size 1 /Prev /Bad >>

            """;

        using TestRandomAccessSource source =
            CreatePdf(
                prefix: "%PDF-1.7\n",
                xref);

        var parser =
            new PdfCrossReferenceParser(source);

        Assert.Throws<FormatException>(
            parser.Parse);
    }

    [Fact]
    public void ParseRejectsOutOfRangePrev()
    {
        const string xref =
            """
            xref
            0 1
            0000000000 65535 f
            trailer
            << /Size 1 /Prev 999999 >>

            """;

        using TestRandomAccessSource source =
            CreatePdf(
                prefix: "%PDF-1.7\n",
                xref);

        var parser =
            new PdfCrossReferenceParser(source);

        Assert.Throws<FormatException>(
            parser.Parse);
    }

    private static PdfCrossReferenceEntry CreateEntry(
        int objectNumber,
        int generationNumber,
        long offset,
        bool isInUse)
    {
        return new PdfCrossReferenceEntry(
            new PdfObjectId(
                objectNumber,
                generationNumber),
            offset,
            isInUse);
    }

    private static PdfCrossReferenceTable CreateTable(
        long offset,
        params PdfCrossReferenceEntry[] entries)
    {
        int firstObjectNumber =
            entries[0].ObjectId.ObjectNumber;

        return new PdfCrossReferenceTable(
            offset,
            [
                new PdfCrossReferenceSection(
                    firstObjectNumber,
                    entries)
            ],
            PdfDictionary.Empty);
    }

    private static TestRandomAccessSource CreatePdf(
        string prefix,
        string xref,
        string lineEnding = "\n")
    {
        long xrefOffset =
            Encoding.ASCII.GetByteCount(prefix);

        string content =
            prefix
            + xref
            + "startxref"
            + lineEnding
            + xrefOffset
            + lineEnding
            + "%%EOF"
            + lineEnding;

        return new TestRandomAccessSource(
            Encoding.ASCII.GetBytes(content));
    }

    private static long GetByteCount(
        StringBuilder builder)
    {
        return Encoding.ASCII.GetByteCount(
            builder.ToString());
    }
}

