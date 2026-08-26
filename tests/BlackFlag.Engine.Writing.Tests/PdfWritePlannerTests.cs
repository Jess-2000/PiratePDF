using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfWritePlannerTests
{
    [Fact]
    public void CreateCapturesSortedExistingObjectIdentityAndNextNumber()
    {
        PdfDocument document = CreateDocument(
            new PdfObjectIdentifier(7),
            new PdfObjectIdentifier(2));

        PdfWritePlan plan = PdfWritePlanner.Create(document);

        Assert.Equal(
            [new PdfObjectIdentifier(2), new PdfObjectIdentifier(7)],
            plan.ExistingObjectIdentifiers.ToArray());
        Assert.Equal(8, plan.NextObjectNumber);
        Assert.Equal(new PdfObjectIdentifier(7), plan.RootObject);
    }

    [Fact]
    public void CreateSnapshotsIdentifiersWithoutTrackingLaterStoreMutation()
    {
        PdfDocument document = CreateDocument(new PdfObjectIdentifier(3));
        PdfWritePlan plan = PdfWritePlanner.Create(document);

        document.Objects.Add(
            new PdfObjectIdentifier(4),
            new PdfDictionary());

        Assert.Single(plan.ExistingObjectIdentifiers);
        Assert.Equal(4, plan.NextObjectNumber);
    }

    [Fact]
    public void CreateCapturesPreservationDefaults()
    {
        PdfWritePlan plan = PdfWritePlanner.Create(
            CreateDocument(new PdfObjectIdentifier(1)));

        Assert.Equal(PdfWriteMode.FullRewrite, plan.Mode);
        Assert.Equal(
            PdfCrossReferenceWriteMode.PreserveSource,
            plan.CrossReferenceMode);
        Assert.True(plan.Preservation.PreservesExistingObjectIdentifiers);
        Assert.True(plan.Preservation.PreservesUntargetedObjects);
        Assert.True(plan.Preservation.AvoidsUntargetedNormalization);
        Assert.False(PdfWritingPreservationContract.GuaranteesByteLevelEquivalence);
        Assert.False(plan.RequiresOriginalSourceRevision);
    }

    [Fact]
    public void IncrementalUpdateDeclaresOriginalRevisionRequirement()
    {
        var options = new PdfWriteOptions
        {
            Mode = PdfWriteMode.IncrementalUpdate
        };

        PdfWritePlan plan = PdfWritePlanner.Create(
            CreateDocument(new PdfObjectIdentifier(1)),
            options);

        Assert.True(plan.RequiresOriginalSourceRevision);
    }

    [Fact]
    public void CreateRejectsMissingTrailerRoot()
    {
        PdfDocument document = PdfDocument.CreateEmpty();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => PdfWritePlanner.Create(document));

        Assert.Contains(
            "root object",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateRejectsTrailerReferenceOutsideObjectStore()
    {
        PdfDocument document = PdfDocument.CreateEmpty();
        document.Trailer.SetRootObject(new PdfObjectIdentifier(9));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => PdfWritePlanner.Create(document));

        Assert.Contains(
            "not present",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateRejectsContradictoryUntargetedObjectPolicy()
    {
        var options = new PdfWriteOptions
        {
            PreserveUntargetedObjects = true,
            NormalizeUntargetedObjects = true
        };

        Assert.Throws<InvalidOperationException>(
            () => PdfWritePlanner.Create(
                CreateDocument(new PdfObjectIdentifier(1)),
                options));
    }

    [Fact]
    public void CreatePreservesOptionalTrailerObjectIdentity()
    {
        PdfDocument document = CreateDocument(
            new PdfObjectIdentifier(4),
            new PdfObjectIdentifier(8),
            new PdfObjectIdentifier(12));
        document.Trailer.SetInformationObject(new PdfObjectIdentifier(8));
        document.Trailer.SetEncryptionObject(new PdfObjectIdentifier(12));

        PdfWritePlan plan = PdfWritePlanner.Create(document);

        Assert.Equal(new PdfObjectIdentifier(8), plan.InformationObject);
        Assert.Equal(new PdfObjectIdentifier(12), plan.EncryptionObject);
        Assert.Equal(13, plan.NextObjectNumber);
    }

    private static PdfDocument CreateDocument(
        params PdfObjectIdentifier[] identifiers)
    {
        PdfDocument document = PdfDocument.CreateEmpty();

        foreach (PdfObjectIdentifier identifier in identifiers)
        {
            document.Objects.Add(identifier, new PdfDictionary());
        }

        document.Trailer.SetRootObject(identifiers[0]);
        return document;
    }
}
