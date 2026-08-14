using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Tests.Pages;

public sealed class PdfPageMutationTests
{
    [Fact]
    public void InsertPageAttachesSessionCreatedPageAtRequestedIndex()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var inserted = session.Document.CreatePage(792, 612);

        var operation =
            session.Document.InsertPage(
                0,
                inserted);

        Assert.Equal(2, document.PageCount);
        Assert.Equal(2, session.Document.PageCount);
        Assert.Same(inserted, session.Document.Pages[0]);
        Assert.Equal(inserted.Id, document.Pages[0].Id);
        Assert.Equal(PdfPageMutationKind.Insert, operation.Kind);
        Assert.Equal(PdfEditScope.Page, operation.Scope);
        Assert.Equal(0, operation.DestinationIndex);
        Assert.True(session.Owns(operation.Owner));
    }

    [Fact]
    public void RemovePageDetachesPageWithoutDiscardingItsHandle()
    {
        var document = PdfDocument.CreateEmpty();
        var source = new PdfPage(612, 792);
        document.Pages.Add(source);

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        var operation =
            session.Document.RemovePage(page);

        Assert.Equal(0, document.PageCount);
        Assert.Equal(0, session.Document.PageCount);
        Assert.False(session.Document.Pages.Contains(page));
        Assert.Equal(source.Id, page.Id);
        Assert.Equal(0, page.SourceIndex);
        Assert.Equal(PdfPageMutationKind.Remove, operation.Kind);
        Assert.Equal(0, operation.SourceIndex);
    }

    [Fact]
    public void RemovedPageCanBeReinsertedByItsOwningSession()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        _ = session.Document.RemovePage(page);
        _ = session.Document.InsertPage(0, page);

        Assert.Single(session.Document.Pages);
        Assert.Same(page, session.Document.Pages[0]);
        Assert.True(page.IsSourcePage);
        Assert.Equal(0, page.SourceIndex);
    }

    [Fact]
    public void MovePageChangesCurrentOrderWithoutChangingOpeningProvenance()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));
        document.Pages.Add(new PdfPage(612, 792));
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var first = session.Document.Pages[0];
        var second = session.Document.Pages[1];
        var third = session.Document.Pages[2];

        var operation =
            session.Document.MovePage(
                first,
                2);

        Assert.Same(second, session.Document.Pages[0]);
        Assert.Same(third, session.Document.Pages[1]);
        Assert.Same(first, session.Document.Pages[2]);
        Assert.Equal(first.Id, document.Pages[2].Id);
        Assert.Equal(0, first.SourceIndex);
        Assert.Equal(1, second.SourceIndex);
        Assert.Equal(2, third.SourceIndex);
        Assert.Equal(0, operation.SourceIndex);
        Assert.Equal(2, operation.DestinationIndex);
    }

    [Fact]
    public void ReplacePagePreservesUntargetedPageHandlesAndOrder()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));
        document.Pages.Add(new PdfPage(612, 792));
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var first = session.Document.Pages[0];
        var target = session.Document.Pages[1];
        var third = session.Document.Pages[2];
        var replacement = session.Document.CreatePage(792, 612);

        var operation =
            session.Document.ReplacePage(
                target,
                replacement);

        Assert.Same(first, session.Document.Pages[0]);
        Assert.Same(replacement, session.Document.Pages[1]);
        Assert.Same(third, session.Document.Pages[2]);
        Assert.False(session.Document.Pages.Contains(target));
        Assert.Equal(replacement.Id, document.Pages[1].Id);
        Assert.Equal(PdfPageMutationKind.Replace, operation.Kind);
        Assert.Equal(target.Id, operation.PageId);
        Assert.Equal(replacement.Id, operation.RelatedPageId);
        Assert.Equal(1, operation.SourceIndex);
    }

    [Fact]
    public void SetPageRotationChangesOnlyTargetPage()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var target = session.Document.Pages[0];
        var untouched = session.Document.Pages[1];

        var operation =
            session.Document.SetPageRotation(
                target,
                PdfPageRotation.Clockwise90);

        Assert.Equal(PdfPageRotation.Clockwise90, target.Rotation);
        Assert.Equal(PdfPageRotation.None, untouched.Rotation);
        Assert.Equal(PdfPageRotation.Clockwise90, operation.Rotation);
        Assert.Equal(PdfPageMutationKind.SetRotation, operation.Kind);
    }

    [Fact]
    public void MutationRejectsForeignSessionPage()
    {
        var firstDocument = PdfDocument.CreateEmpty();
        var secondDocument = PdfDocument.CreateEmpty();

        using var firstSession = new PdfEditingSession(firstDocument);
        using var secondSession = new PdfEditingSession(secondDocument);

        var foreign =
            secondSession.Document.CreatePage(612, 792);

        Assert.Throws<PdfEditingOwnershipException>(
            () => firstSession.Document.InsertPage(
                0,
                foreign));

        Assert.Empty(firstSession.Document.Pages);
        Assert.Empty(secondSession.Document.Pages);
    }

    [Fact]
    public void InsertRejectsPageAlreadyAttachedToDocument()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var attached = session.Document.Pages[0];

        Assert.Throws<InvalidOperationException>(
            () => session.Document.InsertPage(
                1,
                attached));

        Assert.Single(session.Document.Pages);
        Assert.Single(document.Pages);
    }

    [Fact]
    public void MutationDetectsExternalPageTreeChanges()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var detached =
            session.Document.CreatePage(612, 792);

        document.Pages.Add(new PdfPage(612, 792));

        Assert.Throws<InvalidOperationException>(
            () => session.Document.InsertPage(
                0,
                detached));

        Assert.Equal(2, document.PageCount);
        Assert.Single(session.Document.Pages);
    }
}
