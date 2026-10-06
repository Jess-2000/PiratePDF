using System.IO;
using System.Text;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfIncrementalWritePlannerTests
{
    [Fact]
    public void CreateCapturesSortedDistinctChangedObjectsAndPreservesSourceMode()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan writePlan = CreateIncrementalPlan(document);
        var source = new PdfMemorySourceRevision(
            Encoding.ASCII.GetBytes("%PDF-1.7\nxref\n%%EOF\n"),
            9,
            PdfCrossReferenceWriteMode.TraditionalTable);

        PdfIncrementalWritePlan plan = PdfIncrementalWritePlanner.Create(
            writePlan,
            source,
            [new PdfObjectIdentifier(3), new PdfObjectIdentifier(1), new PdfObjectIdentifier(3)]);

        Assert.Same(writePlan, plan.WritePlan);
        Assert.Same(source, plan.SourceRevision);
        Assert.Equal(
            [new PdfObjectIdentifier(1), new PdfObjectIdentifier(3)],
            plan.ChangedObjectIdentifiers.ToArray());
        Assert.Equal(
            PdfCrossReferenceWriteMode.TraditionalTable,
            plan.CrossReferenceMode);
    }

    [Fact]
    public void CreateHonorsExplicitCrossReferenceMode()
    {
        PdfDocument document = CreateDocument();
        var options = new PdfWriteOptions
        {
            Mode = PdfWriteMode.IncrementalUpdate,
            CrossReferenceMode = PdfCrossReferenceWriteMode.CrossReferenceStream
        };
        PdfWritePlan writePlan = PdfWritePlanner.Create(document, options);
        var source = CreateSource(PdfCrossReferenceWriteMode.TraditionalTable);

        PdfIncrementalWritePlan plan = PdfIncrementalWritePlanner.Create(
            writePlan, source, [new PdfObjectIdentifier(1)]);

        Assert.Equal(PdfCrossReferenceWriteMode.CrossReferenceStream, plan.CrossReferenceMode);
    }

    [Fact]
    public void CreateRejectsFullRewritePlan()
    {
        PdfWritePlan writePlan = PdfWritePlanner.Create(CreateDocument());

        Assert.Throws<InvalidOperationException>(
            () => PdfIncrementalWritePlanner.Create(
                writePlan, CreateSource(), [new PdfObjectIdentifier(1)]));
    }

    [Fact]
    public void CreateRejectsEmptyChangeSet()
    {
        PdfWritePlan writePlan = CreateIncrementalPlan(CreateDocument());

        Assert.Throws<ArgumentException>(
            () => PdfIncrementalWritePlanner.Create(
                writePlan, CreateSource(), []));
    }

    [Fact]
    public void CreateRejectsChangedObjectOutsideCurrentStore()
    {
        PdfWritePlan writePlan = CreateIncrementalPlan(CreateDocument());

        Assert.Throws<InvalidOperationException>(
            () => PdfIncrementalWritePlanner.Create(
                writePlan, CreateSource(), [new PdfObjectIdentifier(99)]));
    }

    [Fact]
    public void MemorySourceRevisionCopiesStableSnapshot()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("%PDF-1.7\nxref\n%%EOF\n");
        var source = new PdfMemorySourceRevision(
            bytes, 9, PdfCrossReferenceWriteMode.TraditionalTable);
        bytes[0] = (byte)'!';

        using var destination = new MemoryStream();
        source.CopyTo(destination);

        Assert.Equal((byte)'%', destination.ToArray()[0]);
        Assert.Equal(9, source.CrossReferenceOffset);
        Assert.Equal(20, source.Length);
    }

    [Fact]
    public void MemorySourceRevisionRejectsPreserveSourceAsPhysicalRepresentation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfMemorySourceRevision(
                [1, 2, 3], 1, PdfCrossReferenceWriteMode.PreserveSource));
    }

    private static PdfWritePlan CreateIncrementalPlan(PdfDocument document)
    {
        return PdfWritePlanner.Create(
            document,
            new PdfWriteOptions { Mode = PdfWriteMode.IncrementalUpdate });
    }

    private static PdfMemorySourceRevision CreateSource(
        PdfCrossReferenceWriteMode mode = PdfCrossReferenceWriteMode.TraditionalTable)
    {
        return new PdfMemorySourceRevision(
            Encoding.ASCII.GetBytes("%PDF-1.7\nxref\n%%EOF\n"), 9, mode);
    }

    private static PdfDocument CreateDocument()
    {
        PdfDocument document = PdfDocument.CreateEmpty(PdfVersion.Pdf17);
        document.Objects.Add(new PdfObjectIdentifier(1), new PdfDictionary());
        document.Objects.Add(new PdfObjectIdentifier(3), new PdfDictionary());
        document.Trailer.SetRootObject(new PdfObjectIdentifier(1));
        return document;
    }
}
