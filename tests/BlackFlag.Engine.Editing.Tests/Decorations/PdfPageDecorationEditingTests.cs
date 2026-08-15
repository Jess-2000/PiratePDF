using BlackFlag.Engine.Document.Decorations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Decorations;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Structural;
using BlackFlag.Engine.Editing.Validation;

namespace BlackFlag.Engine.Editing.Tests.Decorations;

public sealed class PdfPageDecorationEditingTests
{
    [Fact]
    public void AddTextHeaderCreatesPageDecorationOperation()
    {
        var document = CreateDocument();
        using var session = new PdfEditingSession(document);
        var decoration = session.Document.CreatePageDecoration(
            PdfPageDecorationKind.Text,
            PdfPageDecorationRegion.Header,
            PdfPageDecorationAlignment.Left);

        var operation = session.Document.AddPageDecoration(decoration);

        Assert.Equal(PdfEditScope.Document, operation.Scope);
        Assert.Equal(PdfStructureKind.PageDecoration, operation.StructureKind);
        Assert.Equal(PdfPageDecorationMutationKind.Add, operation.Kind);
        Assert.Equal(decoration.Id, operation.DecorationId);
        Assert.Equal(PdfStructuralValueOrigin.Absent, operation.PreviousState.Origin);
        Assert.Equal(PdfStructuralValueOrigin.Explicit, operation.CurrentState.Origin);
        Assert.Same(decoration, Assert.Single(session.Document.PageDecorations));
    }

    [Fact]
    public void TextMutationPreservesPlacementAndTarget()
    {
        var document = CreateDocument();
        using var session = new PdfEditingSession(document);
        var decoration = session.Document.CreatePageDecoration(
            PdfPageDecorationKind.Text,
            PdfPageDecorationRegion.Footer,
            PdfPageDecorationAlignment.Right);
        _ = session.Document.SetPageDecorationTarget(
            decoration,
            new PdfPageDecorationTarget(1, 3, PdfPageSelectionParity.Odd));
        _ = session.Document.AddPageDecoration(decoration);

        _ = session.Document.SetPageDecorationText(decoration, "Confidential");

        Assert.Equal("Confidential", decoration.Text);
        Assert.Equal(PdfPageDecorationRegion.Footer, decoration.Region);
        Assert.Equal(PdfPageDecorationAlignment.Right, decoration.Alignment);
        Assert.Equal(
            new PdfPageDecorationTarget(1, 3, PdfPageSelectionParity.Odd),
            decoration.Target);
    }

    [Fact]
    public void PageNumberDecorationRejectsLiteralText()
    {
        var document = CreateDocument();
        using var session = new PdfEditingSession(document);
        var decoration = session.Document.CreatePageDecoration(
            PdfPageDecorationKind.PageNumber,
            PdfPageDecorationRegion.Footer);

        Assert.Throws<InvalidOperationException>(
            () => session.Document.SetPageDecorationText(decoration, "Page"));
    }

    [Fact]
    public void TextDecorationRejectsNumberingOptions()
    {
        var document = CreateDocument();
        using var session = new PdfEditingSession(document);
        var decoration = session.Document.CreatePageDecoration(
            PdfPageDecorationKind.Text,
            PdfPageDecorationRegion.Header);

        Assert.Throws<InvalidOperationException>(
            () => session.Document.SetPageNumbering(
                decoration,
                new PdfPageNumberingOptions(3)));
    }

    [Fact]
    public void TransactionRollbackRestoresDecorationState()
    {
        var document = CreateDocument();
        using var session = new PdfEditingSession(document);
        var decoration = session.Document.CreatePageDecoration(
            PdfPageDecorationKind.Text,
            PdfPageDecorationRegion.Header,
            PdfPageDecorationAlignment.Left);
        _ = session.Document.SetPageDecorationText(decoration, "Before");
        _ = session.Document.AddPageDecoration(decoration);

        using var transaction = session.BeginTransaction();
        _ = session.Document.SetPageDecorationText(decoration, "After");
        _ = session.Document.SetPageDecorationPlacement(
            decoration,
            PdfPageDecorationRegion.Footer,
            PdfPageDecorationAlignment.Right);
        transaction.Rollback();

        Assert.Equal("Before", decoration.Text);
        Assert.Equal(PdfPageDecorationRegion.Header, decoration.Region);
        Assert.Equal(PdfPageDecorationAlignment.Left, decoration.Alignment);
    }

    [Fact]
    public void CommittedDecorationRemovalSupportsUndoAndRedo()
    {
        var document = CreateDocument();
        using var session = new PdfEditingSession(document);
        var decoration = session.Document.CreatePageDecoration(
            PdfPageDecorationKind.PageNumber,
            PdfPageDecorationRegion.Footer);
        _ = session.Document.AddPageDecoration(decoration);

        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.RemovePageDecoration(decoration);
            transaction.Commit();
        }

        Assert.Empty(session.Document.PageDecorations);
        _ = session.History.Undo();
        Assert.Same(decoration, Assert.Single(session.Document.PageDecorations));
        _ = session.History.Redo();
        Assert.Empty(session.Document.PageDecorations);
    }

    [Fact]
    public void ValidatorRunsBeforeDecorationMutation()
    {
        var document = CreateDocument();
        var validator = new RejectPageDecorationValidator();
        using var session = new PdfEditingSession(
            document,
            new PdfEditingSessionOptions { Validators = [validator] });
        var decoration = session.Document.CreatePageDecoration(
            PdfPageDecorationKind.Text,
            PdfPageDecorationRegion.Header);

        Assert.Throws<PdfEditingValidationException>(
            () => session.Document.AddPageDecoration(decoration));
        Assert.Empty(session.Document.PageDecorations);
    }

    [Fact]
    public void DecorationFromAnotherSessionIsRejected()
    {
        var firstDocument = CreateDocument();
        var secondDocument = CreateDocument();
        using var firstSession = new PdfEditingSession(firstDocument);
        using var secondSession = new PdfEditingSession(secondDocument);
        var decoration = firstSession.Document.CreatePageDecoration(
            PdfPageDecorationKind.Text,
            PdfPageDecorationRegion.Header);

        Assert.Throws<Editing.Ownership.PdfEditingOwnershipException>(
            () => secondSession.Document.AddPageDecoration(decoration));
    }

    private static PdfDocument CreateDocument()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));
        document.Pages.Add(new PdfPage(612, 792));
        document.Pages.Add(new PdfPage(612, 792));
        document.Pages.Add(new PdfPage(612, 792));
        return document;
    }

    private sealed class RejectPageDecorationValidator : IPdfEditingValidator
    {
        public void Validate(PdfEditingSession session, IPdfEditOperation operation)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(operation);

            if (operation is IPdfStructuralEditOperation structural &&
                structural.StructureKind == PdfStructureKind.PageDecoration)
            {
                throw new PdfEditingValidationException("Page-decoration edit rejected.");
            }
        }
    }
}
