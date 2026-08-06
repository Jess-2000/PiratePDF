#nullable enable
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;

namespace BlackFlag.Engine.Parsing.Tests.Loading;

public sealed class PdfObjectLocationTableTests
{
    [Fact]
    public void NewestRevisionReplacesOlderGeneration()
    {
        var older = PdfObjectLocation.CreateDirect(
            new PdfObjectId(7, 0),
            offset: 10);
        var newer = PdfObjectLocation.CreateDirect(
            new PdfObjectId(7, 1),
            offset: 90);

        var table = new PdfObjectLocationTable(
            [
                [older],
                [newer]
            ]);

        Assert.False(
            table.TryGetLocation(
                older.ObjectId,
                out _));
        Assert.True(
            table.TryGetLocation(
                newer.ObjectId,
                out PdfObjectLocation? result));
        Assert.Same(newer, result);
    }

    [Fact]
    public void ConvertsCompressedCrossReferenceEntry()
    {
        PdfCrossReferenceStreamEntry entry =
            PdfCrossReferenceStreamEntry.CreateCompressed(
                objectNumber: 12,
                objectStreamNumber: 4,
                objectStreamIndex: 2);

        PdfObjectLocation location = PdfObjectLocation.From(entry);

        Assert.Equal(PdfObjectLocationKind.Compressed, location.Kind);
        Assert.Equal(new PdfObjectId(12, 0), location.ObjectId);
        Assert.Equal(4, location.ObjectStreamNumber);
        Assert.Equal(2, location.ObjectStreamIndex);
    }
}
