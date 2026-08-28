using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Text;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>
/// Describes one provenance-backed replacement of existing text within an
/// indirect page content stream.
/// </summary>
public sealed class PdfExistingTextEditOperation :
    IPdfEditOperation
{
    internal PdfExistingTextEditOperation(
        PdfEditingOwnershipToken owner,
        PdfExistingTextIdentity identity,
        PdfObjectIdentifier contentStreamIdentifier,
        int previousLength,
        int replacementLength)
    {
        Owner = owner;
        Identity = identity;
        ContentStreamIdentifier =
            contentStreamIdentifier;
        PreviousLength =
            previousLength;
        ReplacementLength =
            replacementLength;
    }

    /// <inheritdoc />
    public PdfEditingOwnershipToken Owner { get; }

    /// <inheritdoc />
    public PdfEditScope Scope =>
        PdfEditScope.Content;

    /// <summary>Gets the existing-text identity targeted by this edit.</summary>
    public PdfExistingTextIdentity Identity { get; }

    /// <summary>Gets the indirect content-stream object mutated by this edit.</summary>
    public PdfObjectIdentifier ContentStreamIdentifier { get; }

    /// <summary>Gets the stream-data length before replacement.</summary>
    public int PreviousLength { get; }

    /// <summary>Gets the stream-data length after replacement.</summary>
    public int ReplacementLength { get; }
}
