using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;

namespace BlackFlag.Engine.Editing.Tests.History;

public sealed class PdfEditHistoryTests
{
    [Fact]
    public void CommittedTransactionBecomesUndoable()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        using var transaction = session.BeginTransaction();
        _ = session.Document.SetPageRotation(
            page,
            PdfPageRotation.Clockwise90);

        var committed = transaction.Commit();

        Assert.True(session.History.CanUndo);
        Assert.False(session.History.CanRedo);
        Assert.Equal(1, session.History.UndoCount);
        Assert.Equal(0, session.History.RedoCount);

        var undone = session.History.Undo();

        Assert.Same(committed, undone);
        Assert.Equal(PdfPageRotation.None, page.Rotation);
        Assert.False(session.History.CanUndo);
        Assert.True(session.History.CanRedo);
    }

    [Fact]
    public void RedoReappliesUndoneTransaction()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        using var transaction = session.BeginTransaction();
        _ = session.Document.SetPageRotation(
            page,
            PdfPageRotation.Clockwise270);

        var committed = transaction.Commit();

        _ = session.History.Undo();
        var redone = session.History.Redo();

        Assert.Same(committed, redone);
        Assert.Equal(PdfPageRotation.Clockwise270, page.Rotation);
        Assert.True(session.History.CanUndo);
        Assert.False(session.History.CanRedo);
    }

    [Fact]
    public void UndoAndRedoPreserveStructuralPageIdentity()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var first = session.Document.Pages[0];
        var second = session.Document.Pages[1];
        var inserted = session.Document.CreatePage(792, 612);

        using var transaction = session.BeginTransaction();
        _ = session.Document.MovePage(first, 1);
        _ = session.Document.InsertPage(2, inserted);
        _ = session.Document.RemovePage(second);

        var committed = transaction.Commit();

        _ = session.History.Undo();

        Assert.Equal(2, session.Document.PageCount);
        Assert.Same(first, session.Document.Pages[0]);
        Assert.Same(second, session.Document.Pages[1]);
        Assert.False(session.Document.Pages.Contains(inserted));

        var redone = session.History.Redo();

        Assert.Same(committed, redone);
        Assert.Equal(2, session.Document.PageCount);
        Assert.Same(first, session.Document.Pages[0]);
        Assert.Same(inserted, session.Document.Pages[1]);
        Assert.False(session.Document.Pages.Contains(second));
    }

    [Fact]
    public void NewCommittedEditClearsRedoHistory()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        using (var firstTransaction = session.BeginTransaction())
        {
            _ = session.Document.SetPageRotation(
                page,
                PdfPageRotation.Clockwise90);
            _ = firstTransaction.Commit();
        }

        _ = session.History.Undo();
        Assert.True(session.History.CanRedo);

        using (var secondTransaction = session.BeginTransaction())
        {
            _ = session.Document.SetPageRotation(
                page,
                PdfPageRotation.Clockwise180);
            _ = secondTransaction.Commit();
        }

        Assert.True(session.History.CanUndo);
        Assert.False(session.History.CanRedo);
        Assert.Equal(1, session.History.UndoCount);
        Assert.Equal(PdfPageRotation.Clockwise180, page.Rotation);
    }

    [Fact]
    public void RolledBackAndEmptyTransactionsDoNotEnterHistory()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        using (var rolledBack = session.BeginTransaction())
        {
            _ = session.Document.SetPageRotation(
                page,
                PdfPageRotation.Clockwise90);
            rolledBack.Rollback();
        }

        using (var empty = session.BeginTransaction())
        {
            _ = empty.Commit();
        }

        Assert.False(session.History.CanUndo);
        Assert.False(session.History.CanRedo);
        Assert.Equal(PdfPageRotation.None, page.Rotation);
    }

    [Fact]
    public void UndoAndRedoAreRejectedDuringActiveTransaction()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        using (var committedTransaction = session.BeginTransaction())
        {
            _ = session.Document.SetPageRotation(
                page,
                PdfPageRotation.Clockwise90);
            _ = committedTransaction.Commit();
        }

        using var activeTransaction = session.BeginTransaction();

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                _ = session.History.Undo();
            });

        activeTransaction.Rollback();
    }

    [Fact]
    public void UndoAndRedoWithoutAvailableHistoryAreRejected()
    {
        var document = PdfDocument.CreateEmpty();

        using var session = new PdfEditingSession(document);

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                _ = session.History.Undo();
            });

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                _ = session.History.Redo();
            });
    }

    [Fact]
    public void OperationsRemainOrderedAcrossUndoAndRedo()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var inserted = session.Document.CreatePage(792, 612);

        using var transaction = session.BeginTransaction();
        _ = session.Document.SetPageRotation(
            page,
            PdfPageRotation.Clockwise180);
        _ = session.Document.InsertPage(
            1,
            inserted);

        var committed = transaction.Commit();

        _ = session.History.Undo();
        var redone = session.History.Redo();

        var rotationOperation =
            Assert.IsType<PdfPageEditOperation>(redone[0]);

        var insertOperation =
            Assert.IsType<PdfPageEditOperation>(redone[1]);

        Assert.Equal(
            PdfPageMutationKind.SetRotation,
            rotationOperation.Kind);
        Assert.Equal(
            PdfPageMutationKind.Insert,
            insertOperation.Kind);
        Assert.Same(committed, redone);
    }
}
