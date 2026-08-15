using System;
using System.Collections.Generic;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.SDK.Adapters.Editing;
using BlackFlag.Engine.SDK.Editing;

namespace BlackFlag.Engine.SDK.Adapters.Tests.Editing;

public sealed class PdfEditingSdkHardeningTests
{
    [Fact]
    public void RemovedPageCanBeReinsertedByStableSdkIdentity()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using IPdfEditingSdkSession sdk = new PdfEditingSdkSession(document);
        var original = Assert.Single(sdk.Pages);

        _ = sdk.RemovePage(original.Id);
        Assert.Empty(sdk.Pages);

        _ = sdk.InsertPage(0, original.Id);

        Assert.Equal(original.Id, Assert.Single(sdk.Pages).Id);
        sdk.ValidateConsistency();
    }

    [Fact]
    public void SdkTransactionKeepsStructuralSummaryCoherentAcrossHistory()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using IPdfEditingSdkSession sdk = new PdfEditingSdkSession(document);
        var page = Assert.Single(sdk.Pages);

        sdk.BeginTransaction();
        _ = sdk.SetDocumentInformation(
            PdfEditingSdkDocumentInformationField.Title,
            "PiratePDF");
        _ = sdk.CropPage(
            page.Id,
            new PdfEditingSdkPageBox(18, 18, 594, 774));
        var committed = sdk.CommitTransaction();

        Assert.Equal(2, committed.Operations.Count);
        Assert.Equal(1, sdk.StructureSummary.ExplicitDocumentInformationCount);
        Assert.Equal(
            PdfEditingSdkStructuralValueOrigin.Explicit,
            sdk.GetPageBox(page.Id, PdfEditingSdkPageBoxKind.CropBox).Origin);
        sdk.ValidateConsistency();

        _ = sdk.Undo();

        Assert.Equal(0, sdk.StructureSummary.ExplicitDocumentInformationCount);
        Assert.Equal(
            PdfEditingSdkStructuralValueOrigin.Defaulted,
            sdk.GetPageBox(page.Id, PdfEditingSdkPageBoxKind.CropBox).Origin);
        sdk.ValidateConsistency();

        _ = sdk.Redo();

        Assert.Equal(1, sdk.StructureSummary.ExplicitDocumentInformationCount);
        Assert.Equal(
            PdfEditingSdkStructuralValueOrigin.Explicit,
            sdk.GetPageBox(page.Id, PdfEditingSdkPageBoxKind.CropBox).Origin);
        sdk.ValidateConsistency();
    }

    [Fact]
    public void UnknownPageIdentityIsRejectedWithoutChangingDocument()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using IPdfEditingSdkSession sdk = new PdfEditingSdkSession(document);
        var original = Assert.Single(sdk.Pages);

        Assert.Throws<KeyNotFoundException>(
            () => sdk.SetPageRotation(
                Guid.NewGuid(),
                PdfEditingSdkPageRotation.Clockwise90));

        Assert.Equal(original.Id, Assert.Single(sdk.Pages).Id);
        Assert.Equal(PdfEditingSdkPageRotation.None, Assert.Single(sdk.Pages).Rotation);
        sdk.ValidateConsistency();
    }
}
