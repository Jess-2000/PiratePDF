using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Describes the existing indirect-object topology of a validated PDF page
/// tree without replacing the authoritative flat page collection.
/// </summary>
public sealed class PdfPageTreeHierarchySnapshot
{
    private static readonly PdfName TypeName =
        PdfName.FromLatin1("Type");
    private static readonly PdfName PagesName =
        PdfName.FromLatin1("Pages");
    private static readonly PdfName PageName =
        PdfName.FromLatin1("Page");
    private static readonly PdfName KidsName =
        PdfName.FromLatin1("Kids");

    private readonly IReadOnlyList<Node> nodes;
    private readonly IReadOnlyList<Leaf> leaves;

    private PdfPageTreeHierarchySnapshot(
        PdfObjectIdentifier rootIdentifier,
        IReadOnlyList<Node> nodes,
        IReadOnlyList<Leaf> leaves)
    {
        RootIdentifier = rootIdentifier;
        this.nodes = nodes;
        this.leaves = leaves;
    }

    /// <summary>
    /// Gets the indirect object identifier of the root `/Pages` dictionary.
    /// </summary>
    public PdfObjectIdentifier RootIdentifier { get; }

    /// <summary>
    /// Gets the validated `/Pages` nodes in traversal order.
    /// </summary>
    public IReadOnlyList<Node> Nodes =>
        nodes;

    /// <summary>
    /// Gets the validated leaf `/Page` objects in document order.
    /// </summary>
    public IReadOnlyList<Leaf> Leaves =>
        leaves;

    /// <summary>
    /// Reads and validates the existing indirect page-tree topology.
    /// </summary>
    public static PdfPageTreeHierarchySnapshot Create(
        PdfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        PdfObjectIdentifier rootIdentifier =
            document.Pages.GetRequiredObjectIdentifier();

        var nodes =
            new List<Node>();
        var leaves =
            new List<Leaf>();
        var active =
            new HashSet<PdfObjectIdentifier>();
        var visited =
            new HashSet<PdfObjectIdentifier>();

        Traverse(
            document,
            rootIdentifier,
            parentIdentifier: null,
            nodes,
            leaves,
            active,
            visited);

        ValidateAuthoritativePageOrder(
            document,
            leaves);

        return new PdfPageTreeHierarchySnapshot(
            rootIdentifier,
            nodes.AsReadOnly(),
            leaves.AsReadOnly());
    }

    private static int Traverse(
        PdfDocument document,
        PdfObjectIdentifier identifier,
        PdfObjectIdentifier? parentIdentifier,
        List<Node> nodes,
        List<Leaf> leaves,
        HashSet<PdfObjectIdentifier> active,
        HashSet<PdfObjectIdentifier> visited)
    {
        if (!active.Add(identifier))
        {
            throw new InvalidOperationException(
                $"The page tree contains a cycle at object '{identifier}'.");
        }

        if (!visited.Add(identifier))
        {
            throw new InvalidOperationException(
                $"Page-tree object '{identifier}' occurs more than once.");
        }

        try
        {
            PdfDictionary dictionary =
                GetDictionary(
                    document,
                    identifier);
            PdfName type =
                dictionary.Get<PdfName>(
                    TypeName);

            if (type == PageName)
            {
                if (!parentIdentifier.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Root page-tree object '{identifier}' cannot be a /Page.");
                }

                leaves.Add(
                    new Leaf(
                        identifier,
                        parentIdentifier.Value));

                return 1;
            }

            if (type != PagesName)
            {
                throw new InvalidOperationException(
                    $"Page-tree object '{identifier}' has unsupported type '/{type.GetLatin1Text()}'.");
            }

            PdfArray kids =
                dictionary.Get<PdfArray>(
                    KidsName);
            var childIdentifiers =
                new PdfObjectIdentifier[kids.Count];

            int descendantPageCount = 0;

            for (int index = 0; index < kids.Count; index++)
            {
                PdfIndirectReference child =
                    kids.Get<PdfIndirectReference>(
                        index);

                childIdentifiers[index] =
                    child.Identifier;

                descendantPageCount +=
                    Traverse(
                        document,
                        child.Identifier,
                        identifier,
                        nodes,
                        leaves,
                        active,
                        visited);
            }

            nodes.Add(
                new Node(
                    identifier,
                    parentIdentifier,
                    childIdentifiers,
                    descendantPageCount));

            return descendantPageCount;
        }
        finally
        {
            _ = active.Remove(identifier);
        }
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

    private static void ValidateAuthoritativePageOrder(
        PdfDocument document,
        List<Leaf> leaves)
    {
        if (leaves.Count != document.Pages.Count)
        {
            throw new InvalidOperationException(
                "The indirect page-tree leaf count does not match the authoritative document page count.");
        }

        for (int index = 0; index < leaves.Count; index++)
        {
            PdfObjectIdentifier identifier =
                document.Pages[index].ObjectIdentifier ??
                throw new InvalidOperationException(
                    $"Authoritative page '{document.Pages[index].Id}' is not bound to an indirect object.");

            if (identifier != leaves[index].Identifier)
            {
                throw new InvalidOperationException(
                    $"Indirect page-tree leaf '{leaves[index].Identifier}' does not match authoritative page '{identifier}' at index {index}.");
            }
        }
    }

    /// <summary>
    /// Describes one indirect `/Pages` node.
    /// </summary>
    public sealed record Node(
        PdfObjectIdentifier Identifier,
        PdfObjectIdentifier? ParentIdentifier,
        IReadOnlyList<PdfObjectIdentifier> ChildIdentifiers,
        int DescendantPageCount);

    /// <summary>
    /// Describes one indirect `/Page` leaf and its actual hierarchy parent.
    /// </summary>
    public sealed record Leaf(
        PdfObjectIdentifier Identifier,
        PdfObjectIdentifier ParentIdentifier);
}
