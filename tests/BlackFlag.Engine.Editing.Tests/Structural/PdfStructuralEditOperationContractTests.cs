using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Tests.Structural;

public sealed class PdfStructuralEditOperationContractTests
{
    [Fact]
    public void StructuralClassificationIsIndependentFromEditScope()
    {
        var owner =
            new PdfEditingOwnershipToken(
                Guid.NewGuid());

        IPdfStructuralEditOperation operation =
            new TestStructuralOperation(
                owner,
                PdfEditScope.Page,
                PdfStructureKind.Annotation);

        Assert.Equal(
            PdfEditScope.Page,
            operation.Scope);
        Assert.Equal(
            PdfStructureKind.Annotation,
            operation.StructureKind);
    }

    [Fact]
    public void StructuralOperationParticipatesInSessionOwnership()
    {
        var document =
            PdfDocument.CreateEmpty();

        using var session =
            new PdfEditingSession(document);

        IPdfStructuralEditOperation operation =
            new TestStructuralOperation(
                session.CreateOwnershipToken(),
                PdfEditScope.Document,
                PdfStructureKind.Metadata);

        session.EnsureOwnership(operation);
    }

    [Fact]
    public void StructuralOperationFromAnotherSessionIsRejected()
    {
        var firstDocument =
            PdfDocument.CreateEmpty();
        var secondDocument =
            PdfDocument.CreateEmpty();

        using var firstSession =
            new PdfEditingSession(firstDocument);
        using var secondSession =
            new PdfEditingSession(secondDocument);

        IPdfStructuralEditOperation operation =
            new TestStructuralOperation(
                secondSession.CreateOwnershipToken(),
                PdfEditScope.Document,
                PdfStructureKind.Outline);

        Assert.Throws<PdfEditingOwnershipException>(
            () => firstSession.EnsureOwnership(operation));
    }

    private sealed class TestStructuralOperation :
        IPdfStructuralEditOperation
    {
        public TestStructuralOperation(
            PdfEditingOwnershipToken owner,
            PdfEditScope scope,
            PdfStructureKind structureKind)
        {
            Owner = owner;
            Scope = scope;
            StructureKind = structureKind;
        }

        public PdfEditingOwnershipToken Owner { get; }

        public PdfEditScope Scope { get; }

        public PdfStructureKind StructureKind { get; }
    }
}
