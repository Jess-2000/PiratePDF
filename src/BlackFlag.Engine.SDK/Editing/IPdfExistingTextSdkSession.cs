namespace BlackFlag.Engine.SDK.Editing;

/// <summary>
/// Provides SDK-owned access to provenance-backed existing PDF text registered
/// by the host for one active editing session.
/// </summary>
public interface IPdfExistingTextSdkSession
{
    IReadOnlyList<PdfExistingTextSdkTarget> DiscoverExistingText(Guid pageId);

    IReadOnlyList<PdfExistingTextSdkSearchMatch> FindExistingText(
        string searchText,
        bool matchCase = true);

    PdfExistingTextSdkFindReplaceResult ReplaceAllExistingText(
        string searchText,
        string replacementText,
        bool matchCase = true,
        CancellationToken cancellationToken = default);

    PdfExistingTextSdkReplaceResult ReplaceExistingText(
        Guid targetId,
        string replacementText,
        bool preserveAdvance = true,
        double tolerance = 0.001,
        CancellationToken cancellationToken = default);

    PdfEditingSdkOperation ReplacePositionedExistingText(
        Guid targetId,
        IReadOnlyList<string> replacementSegments,
        CancellationToken cancellationToken = default);

    PdfEditingSdkOperation ApplyExistingTextTypography(
        Guid targetId,
        PdfExistingTextSdkTypographyUpdate update);

    PdfEditingSdkOperation ApplyExistingTextStyle(
        Guid targetId,
        PdfExistingTextSdkStyleUpdate update);

    IReadOnlyList<PdfExistingTextSdkFontCandidate> GetExistingTextFontCandidates(
        Guid targetId);

    PdfEditingSdkOperation ChangeExistingTextFont(
        Guid targetId,
        string resourceName,
        CancellationToken cancellationToken = default);
}
