using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;

namespace BlackFlag.Engine.Document.Tests.Model;

public sealed class PdfDocumentInformationTests
{
    [Fact]
    public void EmptyDocumentInformationContainsNoEntries()
    {
        var document = PdfDocument.CreateEmpty();

        Assert.Equal(0, document.Information.Count);
        Assert.Null(document.Information.Title);
        Assert.False(document.Information.Contains(PdfDocumentInformationField.Title));
    }

    [Fact]
    public void UnsupportedFieldIsRejected()
    {
        var document = PdfDocument.CreateEmpty();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => document.Information.GetValue((PdfDocumentInformationField)99));
    }
}
