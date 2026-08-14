using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>
/// Describes one controlled direct page resource mutation.
/// </summary>
public sealed class PdfResourceEditOperation :
    IPdfEditOperation
{
    internal PdfResourceEditOperation(
        PdfEditingOwnershipToken owner,
        PdfResourceMutationKind kind,
        Guid pageId,
        PdfName category,
        PdfName name,
        bool replacedExisting)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(name);

        Owner = owner;
        Kind = kind;
        PageId = pageId;
        Category = category;
        Name = name;
        ReplacedExisting = replacedExisting;
    }

    public PdfEditingOwnershipToken Owner { get; }

    public PdfEditScope Scope =>
        PdfEditScope.Resource;

    public PdfResourceMutationKind Kind { get; }

    public Guid PageId { get; }

    public PdfName Category { get; }

    public PdfName Name { get; }

    public bool ReplacedExisting { get; }
}
