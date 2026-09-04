using BlackFlag.Engine.Editing.Pages;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Defines one displayed-page and authoritative-content-source pair searched
/// by the existing-text find/replace service.
/// </summary>
public sealed class PdfExistingTextSearchScope
{
    /// <summary>Initializes one existing-text search scope.</summary>
    public PdfExistingTextSearchScope(
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source)
    {
        ArgumentNullException.ThrowIfNull(displayedPage);
        ArgumentNullException.ThrowIfNull(source);

        DisplayedPage = displayedPage;
        Source = source;
    }

    /// <summary>Gets the displayed page that owns the editing handle.</summary>
    public PdfEditablePage DisplayedPage { get; }

    /// <summary>Gets the authoritative content source searched in this scope.</summary>
    public PdfExistingTextDiscoverySource Source { get; }
}
