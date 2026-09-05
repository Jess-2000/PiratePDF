using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Text;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>
/// Describes one existing-text mutation that also installs or replaces PDF
/// resource objects.
/// </summary>
public sealed class PdfExistingTextResourceChangeOperation :
    IPdfEditOperation
{
    internal PdfExistingTextResourceChangeOperation(
        PdfEditingOwnershipToken owner,
        PdfExistingTextIdentity identity,
        PdfObjectIdentifier contentStreamIdentifier,
        string resourceName,
        int affectedObjectCount,
        int previousLength,
        int replacementLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentOutOfRangeException.ThrowIfNegative(affectedObjectCount);
        ArgumentOutOfRangeException.ThrowIfNegative(previousLength);
        ArgumentOutOfRangeException.ThrowIfNegative(replacementLength);

        Owner = owner;
        Identity = identity;
        ContentStreamIdentifier = contentStreamIdentifier;
        ResourceName = resourceName;
        AffectedObjectCount = affectedObjectCount;
        PreviousLength = previousLength;
        ReplacementLength = replacementLength;
    }

    public PdfEditingOwnershipToken Owner { get; }

    public PdfEditScope Scope =>
        PdfEditScope.Resource;

    public PdfExistingTextIdentity Identity { get; }

    public PdfObjectIdentifier ContentStreamIdentifier { get; }

    /// <summary>Gets the newly assigned PDF font resource name.</summary>
    public string ResourceName { get; }

    /// <summary>Gets the supported editability strategy represented by this operation.</summary>
    public static PdfExistingTextEditability Editability =>
        PdfExistingTextEditability.EditableWithResourceChange;

    public int AffectedObjectCount { get; }
    public int PreviousLength { get; }
    public int ReplacementLength { get; }
}
