using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>
/// Describes one controlled page content-stream mutation.
/// </summary>
public sealed class PdfContentEditOperation :
    IPdfEditOperation
{
    internal PdfContentEditOperation(
        PdfEditingOwnershipToken owner,
        PdfContentMutationKind kind,
        Guid pageId,
        int previousLength,
        int replacementLength)
    {
        Owner = owner;
        Kind = kind;
        PageId = pageId;
        PreviousLength = previousLength;
        ReplacementLength = replacementLength;
    }

    public PdfEditingOwnershipToken Owner { get; }

    public PdfEditScope Scope =>
        PdfEditScope.Content;

    public PdfContentMutationKind Kind { get; }

    public Guid PageId { get; }

    public int PreviousLength { get; }

    public int ReplacementLength { get; }
}
