using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Annotations;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Structural;
using BlackFlag.Engine.Editing.Validation;

namespace BlackFlag.Engine.Editing.Tests.Annotations;

public sealed class PdfAnnotationEditingTests
{
    [Fact]
    public void AddAnnotationCreatesPageScopedStructuralOperation()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var annotation = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Text,
            new PdfAnnotationRectangle(10, 20, 110, 70));

        var operation = session.Document.AddAnnotation(page, annotation);

        Assert.Equal(PdfEditScope.Page, operation.Scope);
        Assert.Equal(PdfStructureKind.Annotation, operation.StructureKind);
        Assert.Equal(PdfAnnotationMutationKind.Add, operation.Kind);
        Assert.Equal(page.Id, operation.PageId);
        Assert.Equal(annotation.Id, operation.AnnotationId);
        Assert.Equal(PdfStructuralValueOrigin.Absent, operation.PreviousState.Origin);
        Assert.Equal(PdfStructuralValueOrigin.Explicit, operation.CurrentState.Origin);
        Assert.Same(annotation, Assert.Single(page.Annotations));
        Assert.Equal(page.Id, annotation.PageId);
    }

    [Fact]
    public void RemoveAnnotationRetainsStableDetachedHandle()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var annotation = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Link,
            new PdfAnnotationRectangle(10, 10, 40, 30));
        _ = session.Document.AddAnnotation(page, annotation);
        var id = annotation.Id;

        var operation = session.Document.RemoveAnnotation(page, annotation);

        Assert.Equal(PdfAnnotationMutationKind.Remove, operation.Kind);
        Assert.Empty(page.Annotations);
        Assert.False(annotation.IsAttached);
        Assert.Null(annotation.PageId);
        Assert.Equal(id, annotation.Id);
    }

    [Fact]
    public void CommonStateEditsPreserveUntargetedAnnotationState()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var originalRectangle = new PdfAnnotationRectangle(10, 10, 40, 30);
        var annotation = session.Document.CreateAnnotation(PdfAnnotationSubtype.Text, originalRectangle);
        _ = session.Document.AddAnnotation(page, annotation);
        _ = session.Document.SetAnnotationContents(page, annotation, "Original");
        _ = session.Document.SetAnnotationFlags(page, annotation, PdfAnnotationOptions.Print);

        var revisedRectangle = new PdfAnnotationRectangle(20, 25, 70, 55);
        var operation = session.Document.SetAnnotationRectangle(page, annotation, revisedRectangle);

        Assert.Equal(PdfAnnotationMutationKind.SetRectangle, operation.Kind);
        Assert.Equal(originalRectangle, operation.PreviousState.Value.Rectangle);
        Assert.Equal(revisedRectangle, operation.CurrentState.Value.Rectangle);
        Assert.Equal("Original", annotation.Contents);
        Assert.Equal(PdfAnnotationOptions.Print, annotation.Flags);
    }

    [Fact]
    public void ExplicitEmptyContentsRemainDistinctFromClearedContents()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var annotation = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.FreeText,
            new PdfAnnotationRectangle(10, 10, 50, 40));
        _ = session.Document.AddAnnotation(page, annotation);

        _ = session.Document.SetAnnotationContents(page, annotation, string.Empty);
        Assert.Equal(string.Empty, annotation.Contents);

        var operation = session.Document.ClearAnnotationContents(page, annotation);

        Assert.Equal(string.Empty, operation.PreviousState.Value.Contents);
        Assert.Null(operation.CurrentState.Value.Contents);
        Assert.Null(annotation.Contents);
    }

    [Fact]
    public void TransactionRollbackRestoresRemovedAnnotationAtOriginalIndex()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var first = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Text,
            new PdfAnnotationRectangle(10, 10, 30, 30));
        var second = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Link,
            new PdfAnnotationRectangle(40, 10, 60, 30));
        _ = session.Document.AddAnnotation(page, first);
        _ = session.Document.AddAnnotation(page, second);

        using var transaction = session.BeginTransaction();
        _ = session.Document.RemoveAnnotation(page, first);
        transaction.Rollback();

        Assert.Equal(2, page.Annotations.Count);
        Assert.Same(first, page.Annotations[0]);
        Assert.Same(second, page.Annotations[1]);
    }

    [Fact]
    public void CommittedAnnotationEditsSupportUndoAndRedo()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var annotation = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Text,
            new PdfAnnotationRectangle(10, 10, 30, 30));
        _ = session.Document.AddAnnotation(page, annotation);

        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetAnnotationContents(page, annotation, "Edited");
            transaction.Commit();
        }

        Assert.Equal("Edited", annotation.Contents);
        _ = session.History.Undo();
        Assert.Null(annotation.Contents);
        _ = session.History.Redo();
        Assert.Equal("Edited", annotation.Contents);
    }

    [Fact]
    public void ValidatorRunsBeforeAnnotationMutation()
    {
        var document = CreateDocumentWithPage();
        var validator = new RejectAnnotationValidator();
        using var session = new PdfEditingSession(
            document,
            new PdfEditingSessionOptions { Validators = [validator] });
        var page = session.Document.Pages[0];
        var annotation = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Text,
            new PdfAnnotationRectangle(10, 10, 30, 30));

        Assert.Throws<PdfEditingValidationException>(
            () => session.Document.AddAnnotation(page, annotation));
        Assert.Empty(page.Annotations);
    }

    [Fact]
    public void AnnotationFromAnotherSessionIsRejected()
    {
        var firstDocument = CreateDocumentWithPage();
        var secondDocument = CreateDocumentWithPage();
        using var firstSession = new PdfEditingSession(firstDocument);
        using var secondSession = new PdfEditingSession(secondDocument);
        var annotation = firstSession.Document.CreateAnnotation(
            PdfAnnotationSubtype.Text,
            new PdfAnnotationRectangle(10, 10, 30, 30));

        Assert.Throws<BlackFlag.Engine.Editing.Ownership.PdfEditingOwnershipException>(
            () => secondSession.Document.AddAnnotation(secondSession.Document.Pages[0], annotation));
    }

    private static PdfDocument CreateDocumentWithPage()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));
        return document;
    }

    private sealed class RejectAnnotationValidator : IPdfEditingValidator
    {
        public void Validate(PdfEditingSession session, IPdfEditOperation operation)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(operation);

            if (operation is IPdfStructuralEditOperation structural &&
                structural.StructureKind == PdfStructureKind.Annotation)
            {
                throw new PdfEditingValidationException("Annotation edit rejected.");
            }
        }
    }
}
