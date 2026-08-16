using System.Text;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfProjectedIncrementalWritePlannerTests
{
    [Fact]
    public void CreateReplacesExistingObjectAndTargetsOnlyProjection()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan writePlan = CreateWritePlan(document);
        var projected = new PdfString(Encoding.ASCII.GetBytes("changed"));
        var projections = new PdfObjectWriteProjectionSet(
        [
            new PdfObjectWriteProjection(new PdfObjectIdentifier(3), projected)
        ]);

        PdfIncrementalWritePlan plan = PdfProjectedIncrementalWritePlanner.Create(
            writePlan, CreateSource(), projections);

        Assert.Same(projected, document.Objects.Get(new PdfObjectIdentifier(3)));
        Assert.Equal([new PdfObjectIdentifier(3)], plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void CreateAddsNewObjectAtAllocationBoundary()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan writePlan = CreateWritePlan(document);
        var identifier = new PdfObjectIdentifier(writePlan.NextObjectNumber);
        var projections = new PdfObjectWriteProjectionSet(
        [
            new PdfObjectWriteProjection(identifier, new PdfDictionary())
        ]);

        PdfIncrementalWritePlan plan = PdfProjectedIncrementalWritePlanner.Create(
            writePlan, CreateSource(), projections);

        Assert.True(document.Objects.Contains(identifier));
        Assert.Equal([identifier], plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void CreateRejectsNewObjectBelowAllocationBoundary()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan writePlan = CreateWritePlan(document);
        document.Objects.Remove(new PdfObjectIdentifier(3));
        var projections = new PdfObjectWriteProjectionSet(
        [
            new PdfObjectWriteProjection(new PdfObjectIdentifier(3), new PdfDictionary())
        ]);

        Assert.Throws<InvalidOperationException>(
            () => PdfProjectedIncrementalWritePlanner.Create(
                writePlan, CreateSource(), projections));
    }

    [Fact]
    public void ProjectionSetRejectsMultipleGenerationsForObjectNumber()
    {
        Assert.Throws<ArgumentException>(
            () => new PdfObjectWriteProjectionSet(
            [
                new PdfObjectWriteProjection(new PdfObjectIdentifier(4, 0), new PdfDictionary()),
                new PdfObjectWriteProjection(new PdfObjectIdentifier(4, 1), new PdfDictionary())
            ]));
    }

    private static PdfDocument CreateDocument()
    {
        PdfDocument document = PdfDocument.CreateEmpty(PdfVersion.Pdf17);
        document.Objects.Add(new PdfObjectIdentifier(1), new PdfDictionary());
        document.Objects.Add(
            new PdfObjectIdentifier(3),
            new PdfString(Encoding.ASCII.GetBytes("original")));
        document.Trailer.SetRootObject(new PdfObjectIdentifier(1));
        return document;
    }

    private static PdfWritePlan CreateWritePlan(PdfDocument document) =>
        PdfWritePlanner.Create(
            document,
            new PdfWriteOptions
            {
                Mode = PdfWriteMode.IncrementalUpdate,
                CrossReferenceMode = PdfCrossReferenceWriteMode.TraditionalTable
            });

    private static PdfMemorySourceRevision CreateSource() =>
        new(
            Encoding.ASCII.GetBytes("%PDF-1.7\nxref\n%%EOF\n"),
            9,
            PdfCrossReferenceWriteMode.TraditionalTable);
}
