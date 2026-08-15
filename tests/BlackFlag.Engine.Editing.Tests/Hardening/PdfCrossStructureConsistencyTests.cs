using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Decorations;
using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;

namespace BlackFlag.Engine.Editing.Tests.Hardening;

public sealed class PdfCrossStructureConsistencyTests
{
    [Fact]
    public void ValidateConsistencyAcceptsIntegratedDocumentState()
    {
        var document = CreateDocument();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var widget = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Widget,
            new PdfAnnotationRectangle(20, 20, 120, 45));
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        var outline = session.Document.CreateOutlineItem("Form");
        var decoration = session.Document.CreatePageDecoration(
            PdfPageDecorationKind.Text,
            PdfPageDecorationRegion.Header);

        _ = session.Document.AddAnnotation(page, widget);
        _ = session.Document.AddFormField(field);
        _ = session.Document.AttachWidgetToFormField(field, widget);
        _ = session.Document.AddOutlineItem(outline);
        _ = session.Document.SetOutlineDestination(outline, page);
        _ = session.Document.SetPageDecorationText(decoration, "PiratePDF");
        _ = session.Document.AddPageDecoration(decoration);

        session.ValidateConsistency();
    }

    [Fact]
    public void CrossStructureRollbackRestoresAllTargetedState()
    {
        var document = CreateDocument();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        var outline = session.Document.CreateOutlineItem("Original");
        var decoration = session.Document.CreatePageDecoration(
            PdfPageDecorationKind.Text,
            PdfPageDecorationRegion.Footer);
        _ = session.Document.AddFormField(field);
        _ = session.Document.AddOutlineItem(outline);
        _ = session.Document.AddPageDecoration(decoration);

        using var transaction = session.BeginTransaction();
        _ = session.Document.SetDocumentInformation(PdfDocumentInformationField.Title, "Edited");
        _ = session.Document.CropPage(page, new PdfPageBox(12, 12, 600, 780));
        _ = session.Document.SetFormFieldValue(field, "Value");
        _ = session.Document.SetOutlineTitle(outline, "Edited outline");
        _ = session.Document.SetPageDecorationText(decoration, "Edited footer");
        transaction.Rollback();

        Assert.Null(document.Information.Title);
        Assert.Equal(0, page.GetBoxState(PdfPageBoxKind.CropBox).Value.Left);
        Assert.Null(field.Value);
        Assert.Equal("Original", outline.Title);
        Assert.Null(decoration.Text);
        session.ValidateConsistency();
    }

    [Fact]
    public void CrossStructureCommitSupportsAtomicUndoAndRedo()
    {
        var document = CreateDocument();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        var outline = session.Document.CreateOutlineItem("Original");
        _ = session.Document.AddFormField(field);
        _ = session.Document.AddOutlineItem(outline);

        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetDocumentInformation(PdfDocumentInformationField.Author, "PiratePDF");
            _ = session.Document.SetFormFieldValue(field, "42");
            _ = session.Document.SetOutlineDestination(outline, page);
            transaction.Commit();
        }

        session.ValidateConsistency();
        Assert.Equal("PiratePDF", document.Information.Author);
        Assert.Equal("42", field.Value);
        Assert.Equal(page.Id, outline.Destination?.PageId);

        _ = session.History.Undo();
        session.ValidateConsistency();
        Assert.Null(document.Information.Author);
        Assert.Null(field.Value);
        Assert.Null(outline.Destination);

        _ = session.History.Redo();
        session.ValidateConsistency();
        Assert.Equal("PiratePDF", document.Information.Author);
        Assert.Equal("42", field.Value);
        Assert.Equal(page.Id, outline.Destination?.PageId);
    }

    [Fact]
    public void LinkedWidgetAndOutlineDestinationPreventDanglingReferences()
    {
        var document = CreateDocument();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var widget = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Widget,
            new PdfAnnotationRectangle(20, 20, 120, 45));
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        var outline = session.Document.CreateOutlineItem("Target");

        _ = session.Document.AddAnnotation(page, widget);
        _ = session.Document.AddFormField(field);
        _ = session.Document.AttachWidgetToFormField(field, widget);
        _ = session.Document.AddOutlineItem(outline);
        _ = session.Document.SetOutlineDestination(outline, page);

        Assert.Throws<InvalidOperationException>(
            () => session.Document.RemoveAnnotation(page, widget));
        Assert.Throws<InvalidOperationException>(
            () => session.Document.RemovePage(page));
        session.ValidateConsistency();
    }

    private static PdfDocument CreateDocument()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));
        return document;
    }
}
