using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Transactions;

namespace BlackFlag.Engine.Editing.Tests.Transactions;

public sealed class PdfEditTransactionTests
{
    [Fact]
    public void CommitReturnsOperationsInMutationOrder()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        using var transaction = session.BeginTransaction();
        var source = session.Document.Pages[0];
        var inserted = session.Document.CreatePage(792, 612);

        var rotationOperation =
            session.Document.SetPageRotation(
                source,
                PdfPageRotation.Clockwise90);

        var insertOperation =
            session.Document.InsertPage(
                1,
                inserted);

        var changes = transaction.Commit();

        Assert.Equal(PdfEditTransactionState.Committed, transaction.State);
        Assert.False(session.HasActiveTransaction);
        Assert.Equal(2, changes.Count);
        Assert.Same(rotationOperation, changes[0]);
        Assert.Same(insertOperation, changes[1]);
        Assert.Equal(transaction.Id, changes.TransactionId);
        Assert.Equal(2, document.PageCount);
        Assert.Equal(PdfPageRotation.Clockwise90, source.Rotation);
    }

    [Fact]
    public void RollbackReversesStructuralAndPropertyChanges()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var first = session.Document.Pages[0];
        var second = session.Document.Pages[1];
        var inserted = session.Document.CreatePage(792, 612);

        using var transaction = session.BeginTransaction();

        _ = session.Document.SetPageRotation(
            first,
            PdfPageRotation.Clockwise180);
        _ = session.Document.MovePage(first, 1);
        _ = session.Document.InsertPage(2, inserted);
        _ = session.Document.RemovePage(second);

        transaction.Rollback();

        Assert.Equal(PdfEditTransactionState.RolledBack, transaction.State);
        Assert.False(session.HasActiveTransaction);
        Assert.Equal(2, session.Document.PageCount);
        Assert.Same(first, session.Document.Pages[0]);
        Assert.Same(second, session.Document.Pages[1]);
        Assert.Equal(PdfPageRotation.None, first.Rotation);
        Assert.False(session.Document.Pages.Contains(inserted));
    }

    [Fact]
    public void DisposingActiveTransactionRollsBack()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        using (session.BeginTransaction())
        {
            _ = session.Document.RemovePage(page);
            Assert.Empty(session.Document.Pages);
        }

        Assert.Single(session.Document.Pages);
        Assert.Same(page, session.Document.Pages[0]);
        Assert.False(session.HasActiveTransaction);
    }

    [Fact]
    public void NestedTransactionsAreRejected()
    {
        var document = PdfDocument.CreateEmpty();

        using var session = new PdfEditingSession(document);
        using var transaction = session.BeginTransaction();

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                _ = session.BeginTransaction();
            });

        transaction.Rollback();
    }

    [Fact]
    public void EmptyTransactionCommitsEmptyChangeSet()
    {
        var document = PdfDocument.CreateEmpty();

        using var session = new PdfEditingSession(document);
        using var transaction = session.BeginTransaction();

        var changes = transaction.Commit();

        Assert.True(changes.IsEmpty);
        Assert.Empty(changes);
        Assert.Equal(0, transaction.ChangeCount);
    }

    [Fact]
    public void MutationOutsideTransactionRemainsImmediate()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        var operation =
            session.Document.SetPageRotation(
                page,
                PdfPageRotation.Clockwise270);

        Assert.False(session.HasActiveTransaction);
        Assert.Equal(PdfPageMutationKind.SetRotation, operation.Kind);
        Assert.Equal(PdfPageRotation.Clockwise270, page.Rotation);
    }

    [Fact]
    public void ReplaceRollbackRestoresOriginalHandle()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var first = session.Document.Pages[0];
        var target = session.Document.Pages[1];
        var replacement = session.Document.CreatePage(792, 612);

        using var transaction = session.BeginTransaction();
        _ = session.Document.ReplacePage(target, replacement);

        transaction.Rollback();

        Assert.Same(first, session.Document.Pages[0]);
        Assert.Same(target, session.Document.Pages[1]);
        Assert.False(session.Document.Pages.Contains(replacement));
    }
}
