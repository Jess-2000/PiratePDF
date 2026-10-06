using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Resources;

/// <summary>
/// Adds a resource to a page's effective resource scope without silently
/// changing inherited or shared indirect resource owners.
/// </summary>
public static class PdfPageResourceMutationProjector
{
    private static readonly PdfName ResourcesName =
        PdfName.FromLatin1("Resources");

    private static readonly PdfName ParentName =
        PdfName.FromLatin1("Parent");

    public static PdfPageResourceMutationProjection Create(
        PdfWritePlan writePlan,
        PdfObjectIdentifier pageIdentifier,
        PdfName category,
        string requestedResourceName,
        PdfObject resource)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedResourceName);
        ArgumentNullException.ThrowIfNull(resource);

        PdfDocument document = writePlan.Document;

        PdfDictionary page =
            GetDictionary(
                document,
                pageIdentifier,
                "page");

        PdfDictionary projectedPage =
            page.CreateCopy();

        (
            PdfDictionary resources,
            PdfResourceMutationOwnership ownership) =
            ResolveEffectiveResources(
                document,
                page);

        (
            PdfDictionary categoryDictionary,
            bool copiedIndirectCategory) =
            ResolveCategory(
                document,
                resources,
                category);

        string resourceName =
            AllocateResourceName(
                categoryDictionary,
                requestedResourceName);

        categoryDictionary.Add(
            PdfName.FromLatin1(resourceName),
            resource);

        resources.Set(
            category,
            categoryDictionary);

        projectedPage.Set(
            ResourcesName,
            resources);

        return new PdfPageResourceMutationProjection(
            resourceName,
            ownership,
            copiedIndirectCategory,
            new PdfObjectWriteProjection(
                pageIdentifier,
                projectedPage));
    }

    private static (
        PdfDictionary Resources,
        PdfResourceMutationOwnership Ownership)
        ResolveEffectiveResources(
            PdfDocument document,
            PdfDictionary page)
    {
        if (page.TryGetValue(
                ResourcesName,
                out PdfObject? local))
        {
            return local switch
            {
                PdfDictionary dictionary =>
                    (
                        dictionary.CreateCopy(),
                        PdfResourceMutationOwnership.DirectPageResources
                    ),

                PdfIndirectReference reference =>
                    (
                        ResolveDictionaryReference(
                            document,
                            reference,
                            "page /Resources"),
                        PdfResourceMutationOwnership
                            .IndirectPageResourcesCopyOnWrite
                    ),

                _ =>
                    throw new InvalidOperationException(
                        "Page /Resources must be a dictionary or indirect dictionary reference.")
            };
        }

        PdfDictionary? inherited =
            FindInheritedResources(
                document,
                page);

        return inherited is null
            ? (
                new PdfDictionary(),
                PdfResourceMutationOwnership.NewPageResources
            )
            : (
                inherited,
                PdfResourceMutationOwnership.InheritedResourcesCopyOnWrite
            );
    }

    private static PdfDictionary? FindInheritedResources(
        PdfDocument document,
        PdfDictionary page)
    {
        if (!page.TryGetValue(
                ParentName,
                out PdfObject? parentValue))
        {
            return null;
        }

        if (parentValue is not PdfIndirectReference parentReference)
        {
            throw new InvalidOperationException(
                "Page /Parent must be an indirect reference.");
        }

        var visited =
            new HashSet<PdfObjectIdentifier>();

        PdfObjectIdentifier current =
            parentReference.Identifier;

        while (true)
        {
            if (!visited.Add(current))
            {
                throw new InvalidOperationException(
                    $"The page resource inheritance chain contains a cycle at object '{current}'.");
            }

            PdfDictionary parent =
                GetDictionary(
                    document,
                    current,
                    "/Pages ancestor");

            if (parent.TryGetValue(
                    ResourcesName,
                    out PdfObject? inherited))
            {
                return inherited switch
                {
                    PdfDictionary dictionary =>
                        dictionary.CreateCopy(),

                    PdfIndirectReference reference =>
                        ResolveDictionaryReference(
                            document,
                            reference,
                            "inherited /Resources"),

                    _ =>
                        throw new InvalidOperationException(
                            "Inherited /Resources must be a dictionary or indirect dictionary reference.")
                };
            }

            if (!parent.TryGetValue(
                    ParentName,
                    out PdfObject? next))
            {
                return null;
            }

            if (next is not PdfIndirectReference nextReference)
            {
                throw new InvalidOperationException(
                    "/Pages /Parent must be an indirect reference.");
            }

            current =
                nextReference.Identifier;
        }
    }

    private static (
        PdfDictionary Category,
        bool CopiedFromIndirectObject)
        ResolveCategory(
            PdfDocument document,
            PdfDictionary resources,
            PdfName category)
    {
        if (!resources.TryGetValue(
                category,
                out PdfObject? value))
        {
            return (
                new PdfDictionary(),
                false);
        }

        return value switch
        {
            PdfDictionary dictionary =>
                (
                    dictionary.CreateCopy(),
                    false
                ),

            PdfIndirectReference reference =>
                (
                    ResolveDictionaryReference(
                        document,
                        reference,
                        $"resource category '{category}'"),
                    true
                ),

            _ =>
                throw new InvalidOperationException(
                    $"Resource category '{category}' must be a dictionary or indirect dictionary reference.")
        };
    }

    private static PdfDictionary ResolveDictionaryReference(
        PdfDocument document,
        PdfIndirectReference reference,
        string description)
    {
        if (!document.Objects.TryGet(
                reference.Identifier,
                out PdfObject? value) ||
            value is not PdfDictionary dictionary)
        {
            throw new InvalidOperationException(
                $"{description} reference '{reference.Identifier}' does not resolve to a dictionary.");
        }

        return dictionary.CreateCopy();
    }

    private static PdfDictionary GetDictionary(
        PdfDocument document,
        PdfObjectIdentifier identifier,
        string description)
    {
        if (!document.Objects.TryGet(
                identifier,
                out PdfObject? value) ||
            value is not PdfDictionary dictionary)
        {
            throw new InvalidOperationException(
                $"{description} object '{identifier}' is not an indirect dictionary.");
        }

        return dictionary;
    }

    private static string AllocateResourceName(
        PdfDictionary category,
        string requested)
    {
        if (!category.ContainsKey(
                PdfName.FromLatin1(requested)))
        {
            return requested;
        }

        (string stem, int nextNumber) =
            SplitTrailingNumber(requested);

        while (true)
        {
            string candidate =
                string.Concat(
                    stem,
                    nextNumber.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));

            if (!category.ContainsKey(
                    PdfName.FromLatin1(candidate)))
            {
                return candidate;
            }

            if (nextNumber == int.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Resource name sequence based on '{requested}' is exhausted.");
            }

            nextNumber++;
        }
    }

    private static (
        string Stem,
        int NextNumber)
        SplitTrailingNumber(string value)
    {
        int digitStart = value.Length;

        while (digitStart > 0 &&
            char.IsAsciiDigit(
                value[digitStart - 1]))
        {
            digitStart--;
        }

        if (digitStart == value.Length)
        {
            return (value, 1);
        }

        string digits = value[digitStart..];

        if (!int.TryParse(
                digits,
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out int current) ||
            current == int.MaxValue)
        {
            return (value, 1);
        }

        return (
            value[..digitStart],
            current + 1);
    }
}
