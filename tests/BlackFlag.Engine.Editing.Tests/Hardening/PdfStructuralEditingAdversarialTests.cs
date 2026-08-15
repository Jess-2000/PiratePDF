using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Decorations;
using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Outlines;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;

namespace BlackFlag.Engine.Editing.Tests.Hardening;

public sealed class PdfStructuralEditingAdversarialTests
{
    [Fact]
    public void MixedStructuralTransactionPreservesIdentityAcrossRollback()
    {
        var document = CreateDocument(2);
        using var session = new PdfEditingSession(document);
        var firstPage = session.Document.Pages[0];
        var secondPage = session.Document.Pages[1];
        var firstPageId = firstPage.Id;
        var secondPageId = secondPage.Id;
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        var outline = session.Document.CreateOutlineItem("Original");
        var decoration = session.Document.CreatePageDecoration(
            PdfPageDecorationKind.Text,
            PdfPageDecorationRegion.Header);
        var annotation = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Text,
            new PdfAnnotationRectangle(10, 10, 30, 30));

        _ = session.Document.AddFormField(field);
        _ = session.Document.AddOutlineItem(outline);
        _ = session.Document.AddPageDecoration(decoration);
        _ = session.Document.AddAnnotation(firstPage, annotation);

        using var transaction = session.BeginTransaction();
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Temporary");
        _ = session.Document.MovePage(firstPage, 1);
        _ = session.Document.SetFormFieldValue(field, "Temporary");
        _ = session.Document.SetOutlineDestination(outline, secondPage);
        _ = session.Document.SetPageDecorationText(decoration, "Temporary");
        _ = session.Document.SetAnnotationContents(firstPage, annotation, "Temporary");
        transaction.Rollback();

        Assert.Equal(firstPageId, session.Document.Pages[0].Id);
        Assert.Equal(secondPageId, session.Document.Pages[1].Id);
        Assert.Null(document.Information.Title);
        Assert.Null(field.Value);
        Assert.Null(outline.Destination);
        Assert.Null(decoration.Text);
        Assert.Null(annotation.Contents);
        Assert.Equal(firstPageId, annotation.PageId);
        session.ValidateConsistency();
    }

    [Fact]
    public void MixedStructuralCommitRoundTripsThroughUndoAndRedo()
    {
        var document = CreateDocument(2);
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        var outline = session.Document.CreateOutlineItem("Start");
        var decoration = session.Document.CreatePageDecoration(
            PdfPageDecorationKind.Text,
            PdfPageDecorationRegion.Footer);

        _ = session.Document.AddFormField(field);
        _ = session.Document.AddOutlineItem(outline);
        _ = session.Document.AddPageDecoration(decoration);

        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetDocumentInformation(
                PdfDocumentInformationField.Author,
                "PiratePDF");
            _ = session.Document.CropPage(
                page,
                new PdfPageBox(18, 18, 594, 774));
            _ = session.Document.SetFormFieldValue(field, "42");
            _ = session.Document.SetOutlineDestination(outline, page);
            _ = session.Document.SetPageDecorationText(decoration, "Footer");
            _ = transaction.Commit();
        }

        session.ValidateConsistency();
        Assert.Equal("PiratePDF", document.Information.Author);
        Assert.Equal(18, page.GetBoxState(PdfPageBoxKind.CropBox).Value.Left);
        Assert.Equal("42", field.Value);
        Assert.Equal(page.Id, outline.Destination?.PageId);
        Assert.Equal("Footer", decoration.Text);

        _ = session.History.Undo();
        session.ValidateConsistency();
        Assert.Null(document.Information.Author);
        Assert.Equal(0, page.GetBoxState(PdfPageBoxKind.CropBox).Value.Left);
        Assert.Null(field.Value);
        Assert.Null(outline.Destination);
        Assert.Null(decoration.Text);

        _ = session.History.Redo();
        session.ValidateConsistency();
        Assert.Equal("PiratePDF", document.Information.Author);
        Assert.Equal(18, page.GetBoxState(PdfPageBoxKind.CropBox).Value.Left);
        Assert.Equal("42", field.Value);
        Assert.Equal(page.Id, outline.Destination?.PageId);
        Assert.Equal("Footer", decoration.Text);
    }

    [Fact]
    public void DetachedStructuralHandlesRemainReusableAfterRemoveAndUndo()
    {
        var document = CreateDocument(1);
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var annotation = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Text,
            new PdfAnnotationRectangle(10, 10, 30, 30));

        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.AddAnnotation(page, annotation);
            _ = transaction.Commit();
        }

        Assert.True(annotation.IsAttached);
        _ = session.History.Undo();
        Assert.False(annotation.IsAttached);

        _ = session.History.Redo();
        Assert.True(annotation.IsAttached);
        Assert.Equal(page.Id, annotation.PageId);
        session.ValidateConsistency();
    }

    [Fact]
    public void WidgetRelationshipMustBeRemovedBeforeAnnotationCanBeDetached()
    {
        var document = CreateDocument(1);
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var widget = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Widget,
            new PdfAnnotationRectangle(20, 20, 120, 45));
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);

        _ = session.Document.AddAnnotation(page, widget);
        _ = session.Document.AddFormField(field);
        _ = session.Document.AttachWidgetToFormField(field, widget);

        Assert.Throws<InvalidOperationException>(
            () => session.Document.RemoveAnnotation(page, widget));

        _ = session.Document.DetachWidgetFromFormField(field, widget);
        _ = session.Document.RemoveAnnotation(page, widget);

        Assert.False(widget.IsAttached);
        Assert.Equal(0, field.WidgetCount);
        session.ValidateConsistency();
    }

    [Fact]
    public void OutlineDestinationMustBeClearedBeforeTargetPageRemoval()
    {
        var document = CreateDocument(2);
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[1];
        var outline = session.Document.CreateOutlineItem("Target");

        _ = session.Document.AddOutlineItem(outline);
        _ = session.Document.SetOutlineDestination(outline, page);

        Assert.Throws<InvalidOperationException>(
            () => session.Document.RemovePage(page));

        _ = session.Document.ClearOutlineTarget(outline);
        _ = session.Document.RemovePage(page);

        Assert.Single(session.Document.Pages);
        Assert.Null(outline.Destination);
        session.ValidateConsistency();
    }

    private static PdfDocument CreateDocument(int pageCount)
    {
        var document = PdfDocument.CreateEmpty();

        for (var index = 0; index < pageCount; index++)
        {
            document.Pages.Add(new PdfPage(612, 792));
        }

        return document;
    }
}
