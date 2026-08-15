using BlackFlag.Engine.Document.Decorations;
using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Outlines;
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
        Information = new PdfDocumentInformation();
        PageDecorations = new PdfPageDecorations();
        AcroForm = new PdfAcroForm();
        Outline = new PdfOutline();
    }

    public PdfVersion Version { get; private set; }

    public PdfCatalog Catalog { get; }

    public PdfTrailer Trailer { get; }

    public PdfObjectStore Objects { get; }

    /// <summary>Gets the authoritative logical document-information metadata.</summary>
    public PdfDocumentInformation Information { get; }

    /// <summary>Gets the authoritative logical reusable page-decoration definitions.</summary>
    public PdfPageDecorations PageDecorations { get; }

    /// <summary>Gets the authoritative logical AcroForm state.</summary>
    public PdfAcroForm AcroForm { get; }

    /// <summary>Gets the authoritative logical document-outline state.</summary>
    public PdfOutline Outline { get; }

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