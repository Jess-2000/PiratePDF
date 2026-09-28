using System.Collections.ObjectModel;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Enumerates and matches already-resolved font resources available to an
/// existing-text source. This is a document-resource catalog, not a system
/// font provider.
/// </summary>
public static class PdfExistingFontSelectionService
{
    public static IReadOnlyList<PdfExistingFontCandidate> GetCandidates(
        PdfExistingTextDiscoverySource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        PdfExistingFontCandidate[] candidates =
        [
            .. source.AvailableFonts
                .OrderBy(
                    static pair =>
                        pair.Value.FamilyName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static pair =>
                        pair.Value.FaceName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static pair =>
                        pair.Key,
                    StringComparer.Ordinal)
                .Select(
                    static pair =>
                        new PdfExistingFontCandidate(
                            pair.Key,
                            pair.Value))
        ];

        return new ReadOnlyCollection<PdfExistingFontCandidate>(
            candidates);
    }

    public static IReadOnlyList<PdfExistingFontCandidate> Match(
        PdfExistingTextDiscoverySource source,
        string familyName,
        int? weight = null,
        Rendering.Resources.PdfFontSlant? slant = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);

        if (weight is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weight),
                weight,
                "Font weight must be between 1 and 1000.");
        }

        return new ReadOnlyCollection<PdfExistingFontCandidate>(
        [
            .. GetCandidates(source)
                .Where(
                    candidate =>
                        string.Equals(
                            candidate.FamilyName,
                            familyName,
                            StringComparison.OrdinalIgnoreCase) &&
                        (!weight.HasValue ||
                        candidate.Weight == weight.Value) &&
                        (!slant.HasValue ||
                        candidate.Slant == slant.Value))
                .OrderBy(
                    static candidate =>
                        candidate.ResourceName,
                    StringComparer.Ordinal)
        ]);
    }
}
