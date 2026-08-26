using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Projects the authoritative ordered set of bound pages onto the existing
/// indirect page-tree dictionary.
/// </summary>
public static class PdfPageTreeWriteProjector
{
    private static readonly PdfName KidsName = PdfName.FromLatin1("Kids");
    private static readonly PdfName CountName = PdfName.FromLatin1("Count");

    /// <summary>
    /// Creates a preservation-aware projection for the document's bound
    /// page-tree dictionary. All pages currently in the authoritative page
    /// tree must already have indirect-object identities and dictionary
    /// backing objects.
    /// </summary>
    public static PdfObjectWriteProjectionSet Create(
        PdfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return Create(
            document,
            new Dictionary<PdfPage, PdfObjectIdentifier>());
    }

    internal static PdfObjectWriteProjectionSet Create(
        PdfDocument document,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(pendingPageBindings);

        PdfObjectIdentifier pageTreeIdentifier =
            document.Pages.GetRequiredObjectIdentifier();

        if (!document.Objects.TryGet(
                pageTreeIdentifier,
                out PdfObject? current) ||
            current is null)
        {
            throw new InvalidOperationException(
                $"Page-tree object '{pageTreeIdentifier}' is not present in the object store.");
        }

        if (current is not PdfDictionary currentDictionary)
        {
            throw new InvalidOperationException(
                $"Page-tree object '{pageTreeIdentifier}' must be a PDF dictionary.");
        }

        PdfArray kids = CreateKids(
            document,
            pendingPageBindings);
        PdfDictionary projected = currentDictionary.CreateCopy();

        projected.Set(KidsName, kids);
        projected.Set(
            CountName,
            new PdfInteger(document.Pages.Count));

        return new PdfObjectWriteProjectionSet(
        [
            new PdfObjectWriteProjection(
                pageTreeIdentifier,
                projected)
        ]);
    }

    private static PdfArray CreateKids(
        PdfDocument document,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        var kids = new PdfArray();

        foreach (PdfPage page in document.Pages)
        {
            PdfObjectIdentifier identifier =
                ResolvePageIdentifier(
                    page,
                    pendingPageBindings);

            if (document.Objects.TryGet(
                    identifier,
                    out PdfObject? current))
            {
                if (current is not PdfDictionary)
                {
                    throw new InvalidOperationException(
                        $"Page object '{identifier}' must be a PDF dictionary.");
                }
            }
            else if (!pendingPageBindings.TryGetValue(
                        page,
                        out PdfObjectIdentifier pendingIdentifier) ||
                    pendingIdentifier != identifier)
            {
                throw new InvalidOperationException(
                    $"Page object '{identifier}' is not present in the object store.");
            }

            kids.Add(
                new PdfIndirectReference(identifier));
        }

        return kids;
    }

    private static PdfObjectIdentifier ResolvePageIdentifier(
        PdfPage page,
        IReadOnlyDictionary<PdfPage, PdfObjectIdentifier>
            pendingPageBindings)
    {
        if (page.ObjectIdentifier.HasValue)
        {
            return page.ObjectIdentifier.Value;
        }

        if (pendingPageBindings.TryGetValue(
                page,
                out PdfObjectIdentifier pendingIdentifier))
        {
            return pendingIdentifier;
        }

        throw new InvalidOperationException(
            $"Page '{page.Id}' has not been assigned an indirect-object identifier.");
    }
}
