using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Projects authoritative geometry state for explicitly targeted bound pages
/// onto their existing indirect page dictionaries.
/// </summary>
public static class PdfPageGeometryWriteProjector
{
    private static readonly PdfName MediaBoxName = PdfName.FromLatin1("MediaBox");
    private static readonly PdfName CropBoxName = PdfName.FromLatin1("CropBox");
    private static readonly PdfName BleedBoxName = PdfName.FromLatin1("BleedBox");
    private static readonly PdfName TrimBoxName = PdfName.FromLatin1("TrimBox");
    private static readonly PdfName ArtBoxName = PdfName.FromLatin1("ArtBox");
    private static readonly PdfName RotateName = PdfName.FromLatin1("Rotate");

    /// <summary>
    /// Creates deterministic projections for the supplied page object identifiers.
    /// Untargeted page-dictionary entries are retained exactly.
    /// </summary>
    public static PdfObjectWriteProjectionSet Create(
        PdfDocument document,
        IEnumerable<PdfObjectIdentifier> pageIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(pageIdentifiers);

        PdfObjectIdentifier[] targets =
        [
            .. pageIdentifiers
                .Distinct()
                .OrderBy(identifier => identifier.ObjectNumber)
                .ThenBy(identifier => identifier.GenerationNumber)
        ];

        if (targets.Length == 0)
        {
            throw new ArgumentException(
                "At least one page object must be targeted.",
                nameof(pageIdentifiers));
        }

        Dictionary<PdfObjectIdentifier, PdfPage> pages =
            CreatePageMap(document);
        var projections = new PdfObjectWriteProjection[targets.Length];

        for (int index = 0; index < targets.Length; index++)
        {
            PdfObjectIdentifier identifier = targets[index];

            if (!pages.TryGetValue(identifier, out PdfPage? page))
            {
                throw new InvalidOperationException(
                    $"Page object '{identifier}' is not bound to a page in the authoritative document page tree.");
            }

            if (!document.Objects.TryGet(identifier, out PdfObject? current) ||
                current is null)
            {
                throw new InvalidOperationException(
                    $"Page object '{identifier}' is not present in the object store.");
            }

            if (current is not PdfDictionary currentDictionary)
            {
                throw new InvalidOperationException(
                    $"Page object '{identifier}' must be a PDF dictionary.");
            }

            PdfDictionary projected = currentDictionary.CreateCopy();
            ProjectBox(projected, MediaBoxName, page.GetBox(PdfPageBoxKind.MediaBox));
            ProjectBox(projected, CropBoxName, page.GetBox(PdfPageBoxKind.CropBox));
            ProjectBox(projected, BleedBoxName, page.GetBox(PdfPageBoxKind.BleedBox));
            ProjectBox(projected, TrimBoxName, page.GetBox(PdfPageBoxKind.TrimBox));
            ProjectBox(projected, ArtBoxName, page.GetBox(PdfPageBoxKind.ArtBox));
            ProjectRotation(projected, page.GetRotationState());

            projections[index] =
                new PdfObjectWriteProjection(identifier, projected);
        }

        return new PdfObjectWriteProjectionSet(projections);
    }

    private static Dictionary<PdfObjectIdentifier, PdfPage> CreatePageMap(
        PdfDocument document)
    {
        var pages = new Dictionary<PdfObjectIdentifier, PdfPage>();

        foreach (PdfPage page in document.Pages)
        {
            if (page.ObjectIdentifier is PdfObjectIdentifier identifier)
            {
                pages.Add(identifier, page);
            }
        }

        return pages;
    }

    private static void ProjectBox(
        PdfDictionary dictionary,
        PdfName name,
        PdfEffectiveValue<PdfPageBox> state)
    {
        if (state.Origin != PdfPageValueOrigin.Explicit)
        {
            _ = dictionary.Remove(name);
            return;
        }

        PdfPageBox box = state.Value;
        dictionary.Set(
            name,
            new PdfArray(
            [
                CreateNumber(box.Left),
                CreateNumber(box.Bottom),
                CreateNumber(box.Right),
                CreateNumber(box.Top)
            ]));
    }

    private static void ProjectRotation(
        PdfDictionary dictionary,
        PdfEffectiveValue<PdfPageRotation> state)
    {
        if (state.Origin == PdfPageValueOrigin.Explicit)
        {
            dictionary.Set(
                RotateName,
                new PdfInteger((int)state.Value));
            return;
        }

        _ = dictionary.Remove(RotateName);
    }

    private static PdfObject CreateNumber(double value) =>
        value == Math.Truncate(value) &&
        value >= long.MinValue &&
        value <= long.MaxValue
            ? new PdfInteger((long)value)
            : new PdfReal(value);
}
