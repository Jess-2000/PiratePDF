using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Services.Pages;

namespace BlackFlag.Engine.Services.Tests.Pages;

public sealed class PdfPageServiceTests
{
    [Fact]
    public void AddBlankPage_AddsPageToDocument()
    {
        var document = PdfDocument.CreateEmpty();

        var page = PdfPageService.AddBlankPage(
            document,
            612,
            792);

        Assert.Equal(
            1,
            document.PageCount);

        Assert.Same(
            page,
            document.Pages[0]);
    }

    [Fact]
    public void InsertBlankPage_InsertsAtRequestedIndex()
    {
        var document = PdfDocument.CreateEmpty();

        var first = PdfPageService.AddBlankPage(
            document,
            612,
            792);

        var inserted = PdfPageService.InsertBlankPage(
            document,
            0,
            595,
            842);

        Assert.Equal(
            2,
            document.PageCount);

        Assert.Same(
            inserted,
            document.Pages[0]);

        Assert.Same(
            first,
            document.Pages[1]);
    }

    [Fact]
    public void MovePage_ChangesPageOrder()
    {
        var document = PdfDocument.CreateEmpty();

        var first = PdfPageService.AddBlankPage(
            document,
            612,
            792);

        var second = PdfPageService.AddBlankPage(
            document,
            595,
            842);

        PdfPageService.MovePage(
            document,
            1,
            0);

        Assert.Same(
            second,
            document.Pages[0]);

        Assert.Same(
            first,
            document.Pages[1]);
    }

    [Fact]
    public void DuplicatePage_CopiesPageDimensionsAndRotation()
    {
        var document = PdfDocument.CreateEmpty();

        var source = PdfPageService.AddBlankPage(
            document,
            612,
            792,
            PdfPageRotation.Clockwise90);

        var duplicate = PdfPageService.DuplicatePage(
            document,
            0,
            1);

        Assert.NotSame(
            source,
            duplicate);

        Assert.Equal(
            source.Width,
            duplicate.Width);

        Assert.Equal(
            source.Height,
            duplicate.Height);

        Assert.Equal(
            source.Rotation,
            duplicate.Rotation);
    }

    [Fact]
    public void RemovePage_ReturnsRemovedPage()
    {
        var document = PdfDocument.CreateEmpty();

        var page = PdfPageService.AddBlankPage(
            document,
            612,
            792);

        var removed = PdfPageService.RemovePage(
            document,
            0);

        Assert.Same(
            page,
            removed);

        Assert.Equal(
            0,
            document.PageCount);
    }
}