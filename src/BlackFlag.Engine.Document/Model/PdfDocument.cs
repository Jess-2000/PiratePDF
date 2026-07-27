using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Document.Model;

/// <summary>
/// Represents the authoritative in-memory model of a PDF document.
/// </summary>
public sealed class PdfDocument
{
    public PdfDocument(
        PdfVersion version,
        PdfCatalog catalog,
        PdfTrailer trailer,
        PdfObjectStore objects)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(trailer);
        ArgumentNullException.ThrowIfNull(objects);

        Version = version;
        Catalog = catalog;
        Trailer = trailer;
        Objects = objects;
    }

    public PdfVersion Version { get; private set; }

    public PdfCatalog Catalog { get; }

    public PdfTrailer Trailer { get; }

    public PdfObjectStore Objects { get; }

    public PdfPageTree Pages =>
        Catalog.Pages;

    public int PageCount =>
        Pages.Count;

    public static PdfDocument CreateEmpty(
        PdfVersion? version = null)
    {
        var pages = new PdfPageTree();

        return new PdfDocument(
            version ?? PdfVersion.Pdf17,
            new PdfCatalog(pages),
            new PdfTrailer(),
            new PdfObjectStore());
    }

    internal void BindCoreStructure(
        PdfObjectIdentifier catalogIdentifier,
        PdfObjectIdentifier pageTreeIdentifier)
    {
        Catalog.BindObjectIdentifier(
            catalogIdentifier);

        Pages.BindObjectIdentifier(
            pageTreeIdentifier);

        Trailer.SetRootObject(
            catalogIdentifier);
    }

    public void SetVersion(PdfVersion version)
    {
        Version = version;
    }
}