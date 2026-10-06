using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Text;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Provides one Services-layer façade over the established BF-1054 existing-text
/// discovery, replacement, typography, style, font-selection, and search workflows.
/// </summary>
public static class PdfExistingTextEditingService
{
    public static IReadOnlyList<PdfExistingTextDescriptor> Discover(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source) =>
        PdfExistingTextDiscoveryService.Discover(session, displayedPage, source);

    public static IReadOnlyList<PdfExistingTextSearchMatch> Find(
        PdfEditingSession session,
        IReadOnlyList<PdfExistingTextSearchScope> scopes,
        string searchText,
        bool matchCase = true) =>
        PdfExistingTextFindReplaceService.Find(session, scopes, searchText, matchCase);

    public static PdfExistingTextFindReplaceResult ReplaceAll(
        PdfEditingSession session,
        IReadOnlyList<PdfExistingTextSearchScope> scopes,
        string searchText,
        string replacementText,
        bool matchCase = true,
        CancellationToken cancellationToken = default) =>
        PdfExistingTextFindReplaceService.ReplaceAll(
            session, scopes, searchText, replacementText, matchCase, cancellationToken);

    public static (PdfExistingTextEditOperation Operation, PdfTextAdvanceMeasurement Measurement) Replace(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        string replacementText,
        bool preserveAdvance = true,
        double tolerance = 0.001,
        CancellationToken cancellationToken = default) =>
        PdfWidthAwareTextReplacementService.Replace(
            session, displayedPage, source, descriptor, replacementText,
            preserveAdvance, tolerance, cancellationToken);

    public static PdfExistingTextEditOperation ReplacePositioned(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        IReadOnlyList<string> replacementSegments,
        CancellationToken cancellationToken = default) =>
        PdfPositionedTextReplacementService.Replace(
            session, displayedPage, source, descriptor, replacementSegments, cancellationToken);

    public static PdfExistingTextEditOperation ApplyTypography(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        PdfExistingTextTypographyUpdate update) =>
        PdfExistingTextTypographyService.Apply(session, displayedPage, source, descriptor, update);

    public static PdfExistingTextEditOperation ApplyStyle(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        PdfExistingTextStyleUpdate update) =>
        PdfExistingTextStyleService.Apply(session, displayedPage, source, descriptor, update);

    public static IReadOnlyList<PdfExistingFontCandidate> GetFontCandidates(
        PdfExistingTextDiscoverySource source) =>
        PdfExistingFontSelectionService.GetCandidates(source);

    public static PdfExistingTextEditOperation ChangeFont(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        string resourceName,
        CancellationToken cancellationToken = default) =>
        PdfExistingTextFontService.ChangeFont(
            session, displayedPage, source, descriptor, resourceName, cancellationToken);
}
