#nullable enable

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Validation;

namespace BlackFlag.Engine.Editing.Tests.Hardening;

public sealed class PdfEditingHardeningTests
{
    [Fact]
    public void SessionOwnsImmutableValidatorAndOptionsSnapshots()
    {
        var document =
            PdfDocument.CreateEmpty();

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        var validator =
            new RecordingValidator();

        var configured =
            new List<IPdfEditingValidator>
            {
                validator
            };

        var options =
            new PdfEditingSessionOptions
            {
                Validators =
                    configured
            };

        using var session =
            new PdfEditingSession(
                document,
                options);

        configured.Clear();

        Assert.Single(
            session.Validators);

        Assert.Single(
            session.Options.Validators);

        Assert.False(
            session.Validators is IPdfEditingValidator[]);

        Assert.False(
            session.Options.Validators is IPdfEditingValidator[]);

        _ = session.Document.SetPageRotation(
            session.Document.Pages[0],
            PdfPageRotation.Clockwise90);

        Assert.Single(
            validator.Operations);
    }

    [Fact]
    public void InvalidStructuralEditIsRejectedBeforeCustomValidator()
    {
        var document =
            PdfDocument.CreateEmpty();

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        var validator =
            new RecordingValidator();

        using var session =
            new PdfEditingSession(
                document,
                new PdfEditingSessionOptions
                {
                    Validators =
                    [
                        validator
                    ]
                });

        var page =
            session.Document.Pages[0];

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                session.Document.InsertPage(
                    2,
                    session.Document.CreatePage(
                        612,
                        792)));

        Assert.Throws<InvalidOperationException>(
            () =>
                session.Document.InsertPage(
                    1,
                    page));

        Assert.Empty(
            validator.Operations);
    }

    [Fact]
    public void InvalidRotationIsRejectedBeforeCustomValidator()
    {
        var document =
            PdfDocument.CreateEmpty();

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        var validator =
            new RecordingValidator();

        using var session =
            new PdfEditingSession(
                document,
                new PdfEditingSessionOptions
                {
                    Validators =
                    [
                        validator
                    ]
                });

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                session.Document.SetPageRotation(
                    session.Document.Pages[0],
                    (PdfPageRotation)45));

        Assert.Empty(
            validator.Operations);

        Assert.Equal(
            PdfPageRotation.None,
            session.Document.Pages[0].Rotation);
    }

    [Fact]
    public void FailedUndoCompensatesAlreadyReplayedChangesAndKeepsHistory()
    {
        var document =
            PdfDocument.CreateEmpty();

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        using var session =
            new PdfEditingSession(document);

        var source =
            session.Document.Pages[0];

        var inserted =
            session.Document.CreatePage(
                612,
                792);

        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.InsertPage(
                1,
                inserted);

            _ = session.Document.SetPageRotation(
                source,
                PdfPageRotation.Clockwise90);

            _ = transaction.Commit();
        }

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                _ = session.History.Undo();
            });

        Assert.Equal(
            PdfPageRotation.Clockwise90,
            source.Rotation);

        Assert.True(
            session.History.CanUndo);

        Assert.False(
            session.History.CanRedo);

        Assert.Equal(
            1,
            session.History.UndoCount);
    }

    [Fact]
    public void FailedRedoCompensatesAlreadyReplayedChangesAndKeepsHistory()
    {
        var document =
            PdfDocument.CreateEmpty();

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        using var session =
            new PdfEditingSession(document);

        var source =
            session.Document.Pages[0];

        var inserted =
            session.Document.CreatePage(
                612,
                792);

        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetPageRotation(
                source,
                PdfPageRotation.Clockwise90);

            _ = session.Document.InsertPage(
                1,
                inserted);

            _ = transaction.Commit();
        }

        _ = session.History.Undo();

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                _ = session.History.Redo();
            });

        Assert.Equal(
            PdfPageRotation.None,
            source.Rotation);

        Assert.False(
            session.History.CanUndo);

        Assert.True(
            session.History.CanRedo);

        Assert.Equal(
            1,
            session.History.RedoCount);
    }

    [Fact]
    public void LargeTransactionMaintainsOrderIdentityAndHistory()
    {
        const int MutationCount =
            1000;

        var document =
            PdfDocument.CreateEmpty();

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        using var session =
            new PdfEditingSession(document);

        var page =
            session.Document.Pages[0];

        using var transaction =
            session.BeginTransaction();

        for (var index = 0; index < MutationCount; index++)
        {
            var rotation =
                index % 2 == 0
                    ? PdfPageRotation.Clockwise90
                    : PdfPageRotation.Clockwise180;

            _ = session.Document.SetPageRotation(
                page,
                rotation);
        }

        var committed =
            transaction.Commit();

        Assert.Equal(
            MutationCount,
            committed.Count);

        Assert.Equal(
            PdfPageRotation.Clockwise180,
            page.Rotation);

        var undone =
            session.History.Undo();

        Assert.Same(
            committed,
            undone);

        Assert.Equal(
            PdfPageRotation.None,
            page.Rotation);

        var redone =
            session.History.Redo();

        Assert.Same(
            committed,
            redone);

        Assert.Equal(
            PdfPageRotation.Clockwise180,
            page.Rotation);
    }

    private sealed class RecordingValidator :
        IPdfEditingValidator
    {
        public List<IPdfEditOperation> Operations { get; } =
            [];

        public void Validate(
            PdfEditingSession session,
            IPdfEditOperation operation)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(operation);

            Operations.Add(
                operation);
        }
    }
}
