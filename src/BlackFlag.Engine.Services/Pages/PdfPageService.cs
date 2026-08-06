using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Services.Pages;

/// <summary>
/// Performs multi-step page operations on PDF documents.
/// </summary>
public static class PdfPageService
{
    public static PdfPage AddBlankPage(
        PdfDocument document,
        double width,
        double height,
        PdfPageRotation rotation = PdfPageRotation.None)
    {
        ArgumentNullException.ThrowIfNull(document);

        var page = new PdfPage(
            width,
            height,
            rotation);

        document.Pages.Add(page);

        return page;
    }

    public static PdfPage InsertBlankPage(
        PdfDocument document,
        int index,
        double width,
        double height,
        PdfPageRotation rotation = PdfPageRotation.None)
    {
        ArgumentNullException.ThrowIfNull(document);

        var page = new PdfPage(
            width,
            height,
            rotation);

        document.Pages.Insert(
            index,
            page);

        return page;
    }

    public static PdfPage RemovePage(
        PdfDocument document,
        int index)
    {
        ArgumentNullException.ThrowIfNull(document);

        return document.Pages.RemoveAt(index);
    }

    public static void MovePage(
        PdfDocument document,
        int sourceIndex,
        int destinationIndex)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Pages.Move(
            sourceIndex,
            destinationIndex);
    }

    public static void RotatePage(
        PdfDocument document,
        int index,
        PdfPageRotation rotation)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Pages[index].SetRotation(rotation);
    }

    public static PdfPage DuplicatePage(
        PdfDocument document,
        int sourceIndex,
        int destinationIndex)
    {
        ArgumentNullException.ThrowIfNull(document);

        var source = document.Pages[sourceIndex];

        var duplicate = new PdfPage(
            source.Width,
            source.Height,
            source.Rotation);

        document.Pages.Insert(
            destinationIndex,
            duplicate);

        return duplicate;
    }
}