using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;

namespace BlackFlag.Engine.Editing.Tests.Pages;

public sealed class PdfEditablePageCollectionTests
{
    [Fact]
    public void PreservesSourcePageOrderAndOpeningIndexes()
    {
        var document = PdfDocument.CreateEmpty();
        var first = new PdfPage(612, 792);
        var second = new PdfPage(612, 792);
        var third = new PdfPage(612, 792);

        document.Pages.Add(first);
        document.Pages.Add(second);
        document.Pages.Add(third);

        using var session = new PdfEditingSession(document);

        Assert.Equal(first.Id, session.Document.Pages[0].Id);
        Assert.Equal(second.Id, session.Document.Pages[1].Id);
        Assert.Equal(third.Id, session.Document.Pages[2].Id);
        Assert.Equal(0, session.Document.Pages[0].SourceIndex);
        Assert.Equal(1, session.Document.Pages[1].SourceIndex);
        Assert.Equal(2, session.Document.Pages[2].SourceIndex);
    }

    [Fact]
    public void CollectionsDoNotTreatForeignSessionHandlesAsMembers()
    {
        var firstDocument = PdfDocument.CreateEmpty();
        firstDocument.Pages.Add(new PdfPage(612, 792));

        var secondDocument = PdfDocument.CreateEmpty();
        secondDocument.Pages.Add(new PdfPage(612, 792));

        using var firstSession = new PdfEditingSession(firstDocument);
        using var secondSession = new PdfEditingSession(secondDocument);

        var foreignPage = secondSession.Document.Pages[0];

        Assert.False(firstSession.Document.Pages.Contains(foreignPage));
        Assert.Equal(-1, firstSession.Document.Pages.IndexOf(foreignPage));
    }
}
