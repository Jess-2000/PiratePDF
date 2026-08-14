using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Editing.Documents;

namespace BlackFlag.Engine.Editing.Tests.Ownership;

public sealed class PdfEditingOwnershipTests
{
    [Fact]
    public void OwnsReturnsTrueOnlyForTokensCreatedBySession()
    {
        using var first =
            new PdfEditingSession(PdfDocument.CreateEmpty());
        using var second =
            new PdfEditingSession(PdfDocument.CreateEmpty());

        var firstToken = first.CreateOwnershipToken();
        var secondToken = second.CreateOwnershipToken();

        Assert.True(first.Owns(firstToken));
        Assert.False(first.Owns(secondToken));
    }
}
