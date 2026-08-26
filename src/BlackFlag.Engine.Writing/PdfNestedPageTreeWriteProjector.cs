using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Projects the validated existing nested page-tree hierarchy without
/// flattening intermediate `/Pages` nodes.
/// </summary>
public static class PdfNestedPageTreeWriteProjector
{
    private static readonly PdfName CountName =
        PdfName.FromLatin1("Count");
    private static readonly PdfName ParentName =
        PdfName.FromLatin1("Parent");

    /// <summary>
    /// Creates preservation-aware projections for every existing `/Pages`
    /// node and `/Page` leaf required to normalize hierarchy counts and
    /// parent references.
    /// </summary>
    public static PdfObjectWriteProjectionSet Create(
        PdfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        PdfPageTreeHierarchySnapshot hierarchy =
            PdfPageTreeHierarchySnapshot.Create(
                document);
        var projections =
            new List<PdfObjectWriteProjection>(
                hierarchy.Nodes.Count +
                hierarchy.Leaves.Count);

        foreach (PdfPageTreeHierarchySnapshot.Node node
                in hierarchy.Nodes)
        {
            PdfDictionary projected =
                GetDictionary(
                    document,
                    node.Identifier)
                .CreateCopy();

            projected.Set(
                CountName,
                new PdfInteger(
                    node.DescendantPageCount));

            if (node.ParentIdentifier.HasValue)
            {
                projected.Set(
                    ParentName,
                    new PdfIndirectReference(
                        node.ParentIdentifier.Value));
            }
            else
            {
                _ = projected.Remove(
                    ParentName);
            }

            projections.Add(
                new PdfObjectWriteProjection(
                    node.Identifier,
                    projected));
        }

        foreach (PdfPageTreeHierarchySnapshot.Leaf leaf
                in hierarchy.Leaves)
        {
            PdfDictionary projected =
                GetDictionary(
                    document,
                    leaf.Identifier)
                .CreateCopy();

            projected.Set(
                ParentName,
                new PdfIndirectReference(
                    leaf.ParentIdentifier));

            projections.Add(
                new PdfObjectWriteProjection(
                    leaf.Identifier,
                    projected));
        }

        return new PdfObjectWriteProjectionSet(
            projections);
    }

    private static PdfDictionary GetDictionary(
        PdfDocument document,
        PdfObjectIdentifier identifier)
    {
        if (!document.Objects.TryGet(
                identifier,
                out PdfObject? value) ||
            value is null)
        {
            throw new InvalidOperationException(
                $"Page-tree object '{identifier}' is not present in the object store.");
        }

        return value as PdfDictionary ??
            throw new InvalidOperationException(
                $"Page-tree object '{identifier}' must be a PDF dictionary.");
    }
}
