using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Metadata;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Tests.Metadata;

public sealed class PdfDocumentInformationEditingTests
{
    [Fact]
    public void SetTitleCreatesDocumentScopedMetadataOperation()
    {
        var document = PdfDocument.CreateEmpty();

        using var session = new PdfEditingSession(document);
        var operation = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Quarterly Report");

        Assert.Equal(PdfEditScope.Document, operation.Scope);
        Assert.Equal(PdfStructureKind.Metadata, operation.StructureKind);
        Assert.Equal(PdfDocumentInformationMutationKind.Set, operation.Kind);
        Assert.Equal(PdfDocumentInformationField.Title, operation.Field);
        Assert.Equal(PdfStructuralValueOrigin.Absent, operation.PreviousState.Origin);
        Assert.Equal(PdfStructuralValueOrigin.Explicit, operation.CurrentState.Origin);
        Assert.Equal("Quarterly Report", document.Information.Title);
    }

    [Fact]
    public void ExplicitEmptyStringRemainsDistinctFromAbsent()
    {
        var document = PdfDocument.CreateEmpty();

        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Subject,
            string.Empty);

        var state = session.Document.GetDocumentInformationState(
            PdfDocumentInformationField.Subject);

        Assert.True(state.HasValue);
        Assert.Equal(PdfStructuralValueOrigin.Explicit, state.Origin);
        Assert.Equal(string.Empty, state.Value);
        Assert.True(document.Information.Contains(PdfDocumentInformationField.Subject));
    }

    [Fact]
    public void ClearMetadataCreatesAbsentCurrentState()
    {
        var document = PdfDocument.CreateEmpty();

        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Author,
            "PiratePDF");

        var operation = session.Document.ClearDocumentInformation(
            PdfDocumentInformationField.Author);

        Assert.Equal(PdfDocumentInformationMutationKind.Clear, operation.Kind);
        Assert.Equal("PiratePDF", operation.PreviousState.Value);
        Assert.Equal(PdfStructuralValueOrigin.Absent, operation.CurrentState.Origin);
        Assert.Null(document.Information.Author);
    }

    [Fact]
    public void ClearingAbsentMetadataFailsWithoutCreatingHistory()
    {
        var document = PdfDocument.CreateEmpty();

        using var session = new PdfEditingSession(document);

        Assert.Throws<KeyNotFoundException>(
            () => session.Document.ClearDocumentInformation(
                PdfDocumentInformationField.Keywords));

        Assert.False(session.History.CanUndo);
    }

    [Fact]
    public void TransactionRollbackRestoresAbsentMetadataState()
    {
        var document = PdfDocument.CreateEmpty();

        using var session = new PdfEditingSession(document);
        using var transaction = session.BeginTransaction();

        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Creator,
            "PiratePDF");
        transaction.Rollback();

        var state = session.Document.GetDocumentInformationState(
            PdfDocumentInformationField.Creator);

        Assert.Equal(PdfStructuralValueOrigin.Absent, state.Origin);
        Assert.Null(document.Information.Creator);
    }

    [Fact]
    public void CommittedMetadataEditSupportsUndoAndRedo()
    {
        var document = PdfDocument.CreateEmpty();

        using var session = new PdfEditingSession(document);
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetDocumentInformation(
                PdfDocumentInformationField.Producer,
                "BlackFlag Engine");
            transaction.Commit();
        }

        Assert.Equal("BlackFlag Engine", document.Information.Producer);

        _ = session.History.Undo();
        Assert.Null(document.Information.Producer);

        _ = session.History.Redo();
        Assert.Equal("BlackFlag Engine", document.Information.Producer);
    }

    [Fact]
    public void EditingOneFieldPreservesUntargetedInformationAndTrailerIdentity()
    {
        var document = PdfDocument.CreateEmpty();
        document.Trailer.SetInformationObject(
            new BlackFlag.Engine.Document.Objects.PdfObjectIdentifier(17));

        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Original Title");
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Author,
            "Original Author");

        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Revised Title");

        Assert.Equal("Revised Title", document.Information.Title);
        Assert.Equal("Original Author", document.Information.Author);
        Assert.Equal(17, document.Trailer.InformationObject?.ObjectNumber);
    }
}
