using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfProjectedFullRewritePlannerTests
{
    [Fact]
    public void CreateRefreshesPlanWithNewProjectedObject()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan basePlan = PdfWritePlanner.Create(document);
        PdfObjectIdentifier identifier =
            basePlan.CreateObjectAllocator().ReserveNext();
        var projections = new PdfObjectWriteProjectionSet(
        [
            new PdfObjectWriteProjection(
                identifier,
                PdfString.FromLatin1("projected"))
        ]);

        PdfWritePlan refreshed =
            PdfProjectedFullRewritePlanner.Create(basePlan, projections);

        Assert.Contains(identifier, refreshed.ExistingObjectIdentifiers);
        Assert.Equal(
            "projected",
            document.Objects.Get<PdfString>(identifier).GetLatin1Text());
        Assert.Equal(identifier.ObjectNumber + 1, refreshed.NextObjectNumber);
    }

    [Fact]
    public void CreatePreservesWriteOptionsAcrossReplanning()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan basePlan = PdfWritePlanner.Create(
            document,
            new PdfWriteOptions
            {
                CrossReferenceMode = PdfCrossReferenceWriteMode.CrossReferenceStream,
                PreserveExistingObjectIdentifiers = false,
                PreserveUntargetedObjects = false,
                NormalizeUntargetedObjects = true
            });
        var identifier = new PdfObjectIdentifier(1);
        var projections = new PdfObjectWriteProjectionSet(
        [
            new PdfObjectWriteProjection(identifier, new PdfDictionary())
        ]);

        PdfWritePlan refreshed =
            PdfProjectedFullRewritePlanner.Create(basePlan, projections);

        Assert.Equal(
            PdfCrossReferenceWriteMode.CrossReferenceStream,
            refreshed.CrossReferenceMode);
        Assert.False(refreshed.Preservation.PreservesExistingObjectIdentifiers);
        Assert.False(refreshed.Preservation.PreservesUntargetedObjects);
        Assert.False(refreshed.Preservation.AvoidsUntargetedNormalization);
    }

    [Fact]
    public void CreateRejectsIncrementalBasePlanWithoutApplyingProjection()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan basePlan = PdfWritePlanner.Create(
            document,
            new PdfWriteOptions { Mode = PdfWriteMode.IncrementalUpdate });
        var identifier = new PdfObjectIdentifier(1);
        PdfObject original = document.Objects.Get(identifier);
        var projections = new PdfObjectWriteProjectionSet(
        [
            new PdfObjectWriteProjection(
                identifier,
                PdfString.FromLatin1("replacement"))
        ]);

        Assert.Throws<InvalidOperationException>(
            () => PdfProjectedFullRewritePlanner.Create(basePlan, projections));

        Assert.Same(original, document.Objects.Get(identifier));
    }

    [Fact]
    public void CreateRejectsDifferentGenerationCollision()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan basePlan = PdfWritePlanner.Create(document);
        var conflicting = new PdfObjectIdentifier(1, 1);
        var projections = new PdfObjectWriteProjectionSet(
        [
            new PdfObjectWriteProjection(conflicting, new PdfDictionary())
        ]);

        Assert.Throws<InvalidOperationException>(
            () => PdfProjectedFullRewritePlanner.Create(basePlan, projections));

        Assert.False(document.Objects.Contains(conflicting));
    }

    [Fact]
    public void RefreshedPlanSerializesNewProjectedObject()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan basePlan = PdfWritePlanner.Create(document);
        PdfObjectIdentifier identifier =
            basePlan.CreateObjectAllocator().ReserveNext();
        var projections = new PdfObjectWriteProjectionSet(
        [
            new PdfObjectWriteProjection(
                identifier,
                PdfString.FromLatin1("new value"))
        ]);

        PdfWritePlan refreshed =
            PdfProjectedFullRewritePlanner.Create(basePlan, projections);
        string text = Encoding.Latin1.GetString(
            PdfFullRewriteWriter.Write(refreshed));

        Assert.Contains(
            $"{identifier.ObjectNumber} 0 obj\n(new value)\nendobj\n",
            text,
            StringComparison.Ordinal);
    }

    private static PdfDocument CreateDocument()
    {
        PdfDocument document = PdfDocument.CreateEmpty(PdfVersion.Pdf17);
        var rootIdentifier = new PdfObjectIdentifier(1);
        document.Objects.Add(rootIdentifier, new PdfDictionary());
        document.Trailer.SetRootObject(rootIdentifier);
        return document;
    }
}
