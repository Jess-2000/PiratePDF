using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Outlines;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Outlines;
using BlackFlag.Engine.Editing.Structural;
using BlackFlag.Engine.Editing.Validation;

namespace BlackFlag.Engine.Editing.Tests.Outlines;

public sealed class PdfOutlineEditingTests
{
    [Fact]
    public void AddRootItemCreatesDocumentScopedOutlineOperation()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var item = session.Document.CreateOutlineItem("Chapter 1");

        var operation = session.Document.AddOutlineItem(item);

        Assert.Equal(PdfEditScope.Document, operation.Scope);
        Assert.Equal(PdfStructureKind.Outline, operation.StructureKind);
        Assert.Equal(PdfOutlineMutationKind.AddRoot, operation.Kind);
        Assert.Equal(item.Id, operation.ItemId);
        Assert.Equal(PdfStructuralValueOrigin.Absent, operation.PreviousState.Origin);
        Assert.Equal(PdfStructuralValueOrigin.Explicit, operation.CurrentState.Origin);
        Assert.Same(item, Assert.Single(session.Document.OutlineItems));
    }

    [Fact]
    public void ChildHierarchyPreservesStableIdentity()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var parent = session.Document.CreateOutlineItem("Parent");
        var child = session.Document.CreateOutlineItem("Child");
        _ = session.Document.AddOutlineItem(parent);

        _ = session.Document.AddChildOutlineItem(parent, child);

        Assert.Same(child, Assert.Single(parent.Children));
        Assert.Equal(parent.Id, child.ParentItemId);

        _ = session.Document.RemoveChildOutlineItem(parent, child);
        Assert.Empty(parent.Children);
        Assert.Null(child.ParentItemId);
    }

    [Fact]
    public void CommonStateMutationPreservesUntargetedOutlineState()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var item = session.Document.CreateOutlineItem("Before");
        _ = session.Document.AddOutlineItem(item);
        _ = session.Document.SetOutlineOpenState(item, true);
        _ = session.Document.SetOutlineDestination(item, session.Document.Pages[0]);

        _ = session.Document.SetOutlineTitle(item, "After");

        Assert.Equal("After", item.Title);
        Assert.True(item.IsOpen);
        Assert.Equal(session.Document.Pages[0].Id, item.Destination?.PageId);
        Assert.Null(item.Action);
    }

    [Fact]
    public void DestinationAndActionAreMutuallyExclusive()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var item = session.Document.CreateOutlineItem("Link");
        _ = session.Document.AddOutlineItem(item);
        _ = session.Document.SetOutlineDestination(item, session.Document.Pages[0]);

        var operation = session.Document.SetOutlineAction(
            item,
            PdfOutlineNavigationActionType.Uri,
            "https://example.com");

        Assert.Equal(PdfOutlineMutationKind.SetAction, operation.Kind);
        Assert.Null(item.Destination);
        Assert.Equal(PdfOutlineNavigationActionType.Uri, item.Action?.ActionType);
        Assert.Equal("https://example.com", item.Action?.Target);
    }

    [Fact]
    public void PageReferencedByDirectDestinationCannotBeRemoved()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var item = session.Document.CreateOutlineItem("Page link");
        _ = session.Document.AddOutlineItem(item);
        _ = session.Document.SetOutlineDestination(item, page);

        Assert.Throws<InvalidOperationException>(
            () => session.Document.RemovePage(page));

        _ = session.Document.ClearOutlineTarget(item);
        _ = session.Document.RemovePage(page);
        Assert.Empty(session.Document.Pages);
    }

    [Fact]
    public void TransactionRollbackRestoresOutlineTarget()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var item = session.Document.CreateOutlineItem("Target");
        _ = session.Document.AddOutlineItem(item);
        _ = session.Document.SetOutlineDestination(item, session.Document.Pages[0]);

        using var transaction = session.BeginTransaction();
        _ = session.Document.SetOutlineAction(
            item,
            PdfOutlineNavigationActionType.Named,
            "NextPage");
        transaction.Rollback();

        Assert.Equal(session.Document.Pages[0].Id, item.Destination?.PageId);
        Assert.Null(item.Action);
    }

    [Fact]
    public void CommittedHierarchyChangeSupportsUndoAndRedo()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var parent = session.Document.CreateOutlineItem("Parent");
        var child = session.Document.CreateOutlineItem("Child");
        _ = session.Document.AddOutlineItem(parent);

        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.AddChildOutlineItem(parent, child);
            transaction.Commit();
        }

        Assert.Same(child, Assert.Single(parent.Children));
        _ = session.History.Undo();
        Assert.Empty(parent.Children);
        _ = session.History.Redo();
        Assert.Same(child, Assert.Single(parent.Children));
    }

    [Fact]
    public void ValidatorRunsBeforeOutlineMutation()
    {
        var document = CreateDocumentWithPage();
        var validator = new RejectOutlineValidator();
        using var session = new PdfEditingSession(
            document,
            new PdfEditingSessionOptions { Validators = [validator] });
        var item = session.Document.CreateOutlineItem("Rejected");

        Assert.Throws<PdfEditingValidationException>(
            () => session.Document.AddOutlineItem(item));
        Assert.Empty(session.Document.OutlineItems);
    }

    [Fact]
    public void OutlineItemFromAnotherSessionIsRejected()
    {
        var firstDocument = CreateDocumentWithPage();
        var secondDocument = CreateDocumentWithPage();
        using var firstSession = new PdfEditingSession(firstDocument);
        using var secondSession = new PdfEditingSession(secondDocument);
        var item = firstSession.Document.CreateOutlineItem("Foreign");

        Assert.Throws<BlackFlag.Engine.Editing.Ownership.PdfEditingOwnershipException>(
            () => secondSession.Document.AddOutlineItem(item));
    }

    private static PdfDocument CreateDocumentWithPage()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));
        return document;
    }

    private sealed class RejectOutlineValidator : IPdfEditingValidator
    {
        public void Validate(PdfEditingSession session, IPdfEditOperation operation)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(operation);

            if (operation is IPdfStructuralEditOperation structural &&
                structural.StructureKind == PdfStructureKind.Outline)
            {
                throw new PdfEditingValidationException("Outline edit rejected.");
            }
        }
    }
}
