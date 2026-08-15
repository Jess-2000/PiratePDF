using BlackFlag.Engine.Document.Decorations;
using BlackFlag.Engine.Document.Model;

namespace BlackFlag.Engine.Document.Tests.Decorations;

public sealed class PdfPageDecorationTests
{
    [Fact]
    public void NewDocumentHasEmptyDecorationDefinitions()
    {
        var document = PdfDocument.CreateEmpty();

        Assert.Empty(document.PageDecorations.Items);
    }

    [Fact]
    public void PageTargetUsesInclusiveRangeAndPhysicalParity()
    {
        var target = new PdfPageDecorationTarget(
            1,
            5,
            PdfPageSelectionParity.Even);

        Assert.True(target.Includes(1));
        Assert.False(target.Includes(2));
        Assert.True(target.Includes(3));
        Assert.False(target.Includes(0));
        Assert.False(target.Includes(6));
    }

    [Fact]
    public void PageNumberingAdvancesByPhysicalPageOffset()
    {
        var decoration = new PdfPageDecoration(
            PdfPageDecorationKind.PageNumber,
            PdfPageDecorationRegion.Footer,
            PdfPageDecorationAlignment.Center,
            new PdfPageDecorationTarget(
                2,
                8,
                PdfPageSelectionParity.Odd))
        {
            Numbering = new PdfPageNumberingOptions(10)
        };

        Assert.Equal(10, decoration.GetDisplayedPageNumber(2));
        Assert.Equal(12, decoration.GetDisplayedPageNumber(4));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => decoration.GetDisplayedPageNumber(3));
    }

    [Fact]
    public void TextDecorationRejectsDisplayedPageNumberCalculation()
    {
        var decoration = new PdfPageDecoration(
            PdfPageDecorationKind.Text,
            PdfPageDecorationRegion.Header,
            PdfPageDecorationAlignment.Left,
            PdfPageDecorationTarget.AllPages);

        Assert.Throws<InvalidOperationException>(
            () => decoration.GetDisplayedPageNumber(0));
    }
}
