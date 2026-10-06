using System.Collections.ObjectModel;
using System.Text;

namespace BlackFlag.Engine.Fonts;

/// <summary>
/// Aggregates platform-specific external-font providers behind deterministic,
/// platform-neutral discovery and matching.
/// </summary>
public sealed class PdfExternalFontCatalog
{
    private readonly ReadOnlyCollection<IPdfExternalFontProvider> providers;

    public PdfExternalFontCatalog(
        IEnumerable<IPdfExternalFontProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        IPdfExternalFontProvider[] snapshot =
            [.. providers];

        if (snapshot.Any(static provider => provider is null))
        {
            throw new ArgumentException(
                "External font providers cannot contain null entries.",
                nameof(providers));
        }

        var providerIds =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (IPdfExternalFontProvider provider in snapshot)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider.Id);

            if (!providerIds.Add(provider.Id))
            {
                throw new ArgumentException(
                    $"Duplicate external font provider id '{provider.Id}'.",
                    nameof(providers));
            }
        }

        this.providers =
            Array.AsReadOnly(snapshot);
    }

    public IReadOnlyList<IPdfExternalFontProvider> Providers =>
        providers;

    public IReadOnlyList<PdfExternalFontFace> GetFaces(
        CancellationToken cancellationToken = default)
    {
        var faces =
            new List<PdfExternalFontFace>();

        var identities =
            new HashSet<(string ProviderId, string FaceId)>();

        foreach (IPdfExternalFontProvider provider in providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<PdfExternalFontFace> providerFaces =
                provider.GetFaces(cancellationToken) ??
                throw new InvalidOperationException(
                    $"External font provider '{provider.Id}' returned a null face collection.");

            for (int index = 0; index < providerFaces.Count; index++)
            {
                PdfExternalFontFace face =
                    providerFaces[index] ??
                    throw new InvalidOperationException(
                        $"External font provider '{provider.Id}' returned a null face.");

                if (!string.Equals(
                        face.ProviderId,
                        provider.Id,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"External font face '{face.FaceId}' belongs to provider '{face.ProviderId}', not '{provider.Id}'.");
                }

                if (!identities.Add(
                        (face.ProviderId, face.FaceId)))
                {
                    throw new InvalidOperationException(
                        $"Duplicate external font face identity '{face.ProviderId}:{face.FaceId}'.");
                }

                faces.Add(face);
            }
        }

        return new ReadOnlyCollection<PdfExternalFontFace>(
        [
            .. faces
                .OrderBy(
                    static face =>
                        face.FamilyName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static face =>
                        face.FaceName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static face =>
                        face.ProviderId,
                    StringComparer.Ordinal)
                .ThenBy(
                    static face =>
                        face.FaceId,
                    StringComparer.Ordinal)
        ]);
    }

    public IReadOnlyList<PdfExternalFontMatch> Match(
        PdfExternalFontMatchRequest request,
        string requiredText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requiredText);

        var providerMap =
            providers.ToDictionary(
                static provider => provider.Id,
                StringComparer.Ordinal);

        List<Rune> requiredRunes =
        [
            .. requiredText.EnumerateRunes().Distinct()
        ];

        var matches =
            new List<PdfExternalFontMatch>();

        foreach (PdfExternalFontFace face in
                GetFaces(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(
                    face.FamilyName,
                    request.FamilyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (request.RequireEmbeddable &&
                !PdfExternalFontEmbeddingPolicy.CanEmbedFace(
                    face))
            {
                continue;
            }

            IPdfExternalFontProvider provider =
                providerMap[face.ProviderId];

            bool supportsAll = true;

            for (int index = 0;
                index < requiredRunes.Count;
                index++)
            {
                if (provider.SupportsRune(
                        face,
                        requiredRunes[index]))
                {
                    continue;
                }

                supportsAll = false;
                break;
            }

            if (!supportsAll)
            {
                continue;
            }

            matches.Add(
                new PdfExternalFontMatch(
                    face,
                    provider));
        }

        return new ReadOnlyCollection<PdfExternalFontMatch>(
        [
            .. matches
                .OrderBy(
                    match =>
                        match.Face.Slant == request.Slant
                            ? 0
                            : 1)
                .ThenBy(
                    match =>
                        Math.Abs(
                            match.Face.Weight -
                            request.Weight))
                .ThenBy(
                    match =>
                        Math.Abs(
                            match.Face.Stretch -
                            request.Stretch))
                .ThenBy(
                    static match =>
                        match.Face.FaceName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static match =>
                        match.Face.ProviderId,
                    StringComparer.Ordinal)
                .ThenBy(
                    static match =>
                        match.Face.FaceId,
                    StringComparer.Ordinal)
        ]);
    }
}
