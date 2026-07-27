using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Document.Model;

/// <summary>
/// Represents the high-level PDF document catalog.
/// </summary>
public sealed class PdfCatalog
{
    public PdfCatalog(PdfPageTree pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        Pages = pages;
    }

    public PdfPageTree Pages { get; }

    public string? Language { get; private set; }

    public void SetLanguage(string? language)
    {
        Language = string.IsNullOrWhiteSpace(language)
            ? null
            : language.Trim();
    }
}