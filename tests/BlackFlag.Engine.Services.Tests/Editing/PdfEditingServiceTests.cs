using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Services.Editing;

namespace BlackFlag.Engine.Services.Tests.Editing;

public sealed class PdfEditingServiceTests
{
    [Fact]
    public void OpenSessionCreatesControlledEditingBoundary()
    {
        var document =
            PdfDocument.CreateEmpty();

        var service =
            new PdfEditingService();

        using var session =
            PdfEditingService.OpenSession(document);

        Assert.Equal(
            document.Version,
            session.Document.Version);
        Assert.Equal(
            document.PageCount,
            session.Document.PageCount);
    }
}
