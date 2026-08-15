using System;
using System.Linq;
using System.Text;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.SDK.Adapters.Editing;
using BlackFlag.Engine.SDK.Editing;

namespace BlackFlag.Engine.SDK.Adapters.Tests.Editing;

public sealed class PdfEditingSdkSessionTests
{
    [Fact]
    public void AdapterMutatesThroughEditingSession()
    {
        var document =
            PdfDocument.CreateEmpty();

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        using IPdfEditingSdkSession sdk =
            new PdfEditingSdkSession(document);

        var page =
            Assert.Single(sdk.Pages);

        var operation =
            sdk.SetPageRotation(
                page.Id,
                PdfEditingSdkPageRotation.Clockwise90);

        Assert.Equal(
            "Page",
            operation.Scope);

        Assert.Equal(
            "SetRotation",
            operation.Kind);

        Assert.Equal(
            PdfPageRotation.Clockwise90,
            document.Pages[0].Rotation);
    }

    [Fact]
    public void AdapterTransactionMapsChangeSetAndHistory()
    {
        var document =
            PdfDocument.CreateEmpty();

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        using IPdfEditingSdkSession sdk =
            new PdfEditingSdkSession(document);

        var page =
            Assert.Single(sdk.Pages);

        sdk.BeginTransaction();

        _ = sdk.ReplacePageContent(
            page.Id,
            Encoding.ASCII.GetBytes("q Q"));

        var committed =
            sdk.CommitTransaction();

        Assert.Single(
            committed.Operations);

        Assert.True(
            sdk.CanUndo);

        var undone =
            sdk.Undo();

        Assert.Equal(
            committed.TransactionId,
            undone.TransactionId);

        Assert.True(
            sdk.CanRedo);

        Assert.False(
            Assert.Single(sdk.Pages).HasContent);

        _ = sdk.Redo();

        Assert.True(
            Assert.Single(sdk.Pages).HasContent);
    }

    [Fact]
    public void AdapterDoesNotExposeMutableDocumentModelThroughSdkContract()
    {
        var returnTypes =
            typeof(IPdfEditingSdkSession)
                .GetMethods()
                .Select(
                    static method =>
                        method.ReturnType)
                .ToArray();

        Assert.DoesNotContain(
            returnTypes,
            static type =>
                type.Assembly.GetName().Name is string name &&
                name.StartsWith(
                    "BlackFlag.Engine.",
                    StringComparison.Ordinal) &&
                name != "BlackFlag.Engine.SDK");
    }

    [Fact]
    public void DetachedPageCanBeCreatedAndInsertedThroughSdkIdentity()
    {
        var document = PdfDocument.CreateEmpty();

        using IPdfEditingSdkSession sdk =
            new PdfEditingSdkSession(document);

        var created = sdk.CreatePage(612, 792);
        _ = sdk.InsertPage(0, created.Id);

        Assert.Equal(created.Id, Assert.Single(sdk.Pages).Id);
    }

    [Fact]
    public void AdapterExposesMetadataGeometrySummaryAndConsistencyWithoutModelLeakage()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using IPdfEditingSdkSession sdk =
            new PdfEditingSdkSession(document);

        var page = Assert.Single(sdk.Pages);
        _ = sdk.SetDocumentInformation(
            PdfEditingSdkDocumentInformationField.Title,
            "PiratePDF");
        _ = sdk.SetPageBox(
            page.Id,
            PdfEditingSdkPageBoxKind.CropBox,
            new PdfEditingSdkPageBox(18, 18, 594, 774));

        var state = sdk.GetPageBox(page.Id, PdfEditingSdkPageBoxKind.CropBox);
        var summary = sdk.StructureSummary;

        Assert.Equal("PiratePDF", sdk.GetDocumentInformation(PdfEditingSdkDocumentInformationField.Title));
        Assert.Equal(PdfEditingSdkStructuralValueOrigin.Explicit, state.Origin);
        Assert.Equal(18, state.Value.Left);
        Assert.Equal(1, summary.ExplicitDocumentInformationCount);
        Assert.Equal(1, summary.PageCount);

        sdk.ValidateConsistency();
    }
}
