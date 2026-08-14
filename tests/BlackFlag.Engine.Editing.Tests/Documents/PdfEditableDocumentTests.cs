using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Pages;

namespace BlackFlag.Engine.Editing.Tests.Documents;

public sealed class PdfEditableDocumentTests
{
    [Fact]
    public void SessionExposesSourceDocumentThroughControlledEditableFacade()
    {
        var document = PdfDocument.CreateEmpty(PdfVersion.Pdf20);
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);

        Assert.Equal(PdfVersion.Pdf20, session.Document.Version);
        Assert.Equal(1, session.Document.PageCount);
        Assert.Single(session.Document.Pages);
    }

    [Fact]
    public void ExistingPagesReceiveStableSourceHandles()
    {
        var document = PdfDocument.CreateEmpty();
        var sourcePage = new PdfPage(612, 792);
        document.Pages.Add(sourcePage);

        using var session = new PdfEditingSession(document);

        var first = session.Document.Pages[0];
        var second = session.Document.Pages[0];

        Assert.Same(first, second);
        Assert.Equal(sourcePage.Id, first.Id);
        Assert.Equal(PdfEditablePageOrigin.SourceDocument, first.Origin);
        Assert.Equal(0, first.SourceIndex);
        Assert.True(first.IsSourcePage);
    }

    [Fact]
    public void CreatePageProducesDetachedSessionOwnedPage()
    {
        var document = PdfDocument.CreateEmpty();

        using var session = new PdfEditingSession(document);

        var page = session.Document.CreatePage(
            792,
            612,
            PdfPageRotation.Clockwise90);

        Assert.Equal(PdfEditablePageOrigin.SessionCreated, page.Origin);
        Assert.Null(page.SourceIndex);
        Assert.False(page.IsSourcePage);
        Assert.Equal(792, page.Width);
        Assert.Equal(612, page.Height);
        Assert.Equal(PdfPageRotation.Clockwise90, page.Rotation);
        Assert.Equal(0, session.Document.PageCount);
        Assert.False(session.Document.Pages.Contains(page));
    }

    [Fact]
    public void CreatingEditablePageDoesNotMutateSourcePageTree()
    {
        var document = PdfDocument.CreateEmpty();

        using var session = new PdfEditingSession(document);

        _ = session.Document.CreatePage(612, 792);

        Assert.Equal(0, document.PageCount);
    }
}
