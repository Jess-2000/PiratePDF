using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Tests.Documents;

public sealed class PdfEditingSessionTests
{
    [Fact]
    public void ConstructorUsesPreservationFirstDefaults()
    {
        using var session =
            new PdfEditingSession(PdfDocument.CreateEmpty());

        Assert.Equal(
            PdfEditingPreservationMode.PreserveUntargetedStructure,
            session.Options.PreservationMode);
        Assert.NotEqual(Guid.Empty, session.Id);
    }

    [Fact]
    public void ConstructorRejectsSecondActiveSessionForSameDocument()
    {
        var document = PdfDocument.CreateEmpty();

        using var first = new PdfEditingSession(document);

        Assert.Throws<PdfEditingOwnershipException>(
            () => new PdfEditingSession(document));
    }

    [Fact]
    public void DisposeReleasesDocumentOwnership()
    {
        var document = PdfDocument.CreateEmpty();
        var first = new PdfEditingSession(document);

        first.Dispose();

        using var second = new PdfEditingSession(document);

        Assert.False(second.IsDisposed);
    }

    [Fact]
    public void CreateOwnershipTokenRejectsDisposedSession()
    {
        var session =
            new PdfEditingSession(PdfDocument.CreateEmpty());

        session.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => session.CreateOwnershipToken());
    }
}
