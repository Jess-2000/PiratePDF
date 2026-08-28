using System.Collections.ObjectModel;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Describes one authoritative PDF content source whose interpreted display
/// items can be mapped into existing-text editing identities.
/// </summary>
public sealed class PdfExistingTextDiscoverySource
{
    private readonly ReadOnlyCollection<PdfGraphicsOperation> operations;
    private readonly ReadOnlyCollection<PdfObjectIdentifier?>
        contentStreamIdentifiers;

    /// <summary>Initializes an existing-text discovery source.</summary>
    public PdfExistingTextDiscoverySource(
        PdfObjectIdentifier contentOwnerIdentifier,
        PdfExistingTextSourceKind sourceKind,
        IEnumerable<PdfGraphicsOperation> operations,
        PdfDisplayList displayList,
        IEnumerable<PdfObjectIdentifier?>? contentStreamIdentifiers = null,
        bool hasSharedSourceImpact = false)
    {
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(displayList);

        if (!Enum.IsDefined(sourceKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceKind),
                sourceKind,
                "The existing-text source kind is not supported.");
        }

        PdfGraphicsOperation[] operationSnapshot =
            operations.ToArray();

        if (operationSnapshot.Any(
                static operation =>
                    operation is null))
        {
            throw new ArgumentException(
                "Existing-text discovery operations cannot contain null entries.",
                nameof(operations));
        }

        PdfObjectIdentifier?[] streamSnapshot =
            contentStreamIdentifiers?.ToArray() ??
            [];

        int maximumStreamIndex =
            operationSnapshot.Length == 0
                ? -1
                : operationSnapshot.Max(
                    static operation =>
                        operation.SourceSpan.StreamIndex);

        if (maximumStreamIndex >= streamSnapshot.Length &&
            streamSnapshot.Length != 0)
        {
            throw new ArgumentException(
                "The content-stream identifier map does not cover every operation stream index.",
                nameof(contentStreamIdentifiers));
        }

        ContentOwnerIdentifier =
            contentOwnerIdentifier;
        SourceKind =
            sourceKind;
        this.operations =
            Array.AsReadOnly(
                operationSnapshot);
        DisplayList =
            displayList;
        this.contentStreamIdentifiers =
            Array.AsReadOnly(
                streamSnapshot);
        HasSharedSourceImpact =
            hasSharedSourceImpact;
    }

    /// <summary>Gets the indirect object that owns the source content.</summary>
    public PdfObjectIdentifier ContentOwnerIdentifier { get; }

    /// <summary>Gets whether the source is page content or Form content.</summary>
    public PdfExistingTextSourceKind SourceKind { get; }

    /// <summary>Gets canonical operations in source execution order.</summary>
    public IReadOnlyList<PdfGraphicsOperation> Operations =>
        operations;

    /// <summary>Gets the interpreted display list for this exact source.</summary>
    public PdfDisplayList DisplayList { get; }

    /// <summary>
    /// Gets optional indirect stream identifiers indexed by decoded stream
    /// sequence. An empty collection means stream objects are not identified.
    /// </summary>
    public IReadOnlyList<PdfObjectIdentifier?> ContentStreamIdentifiers =>
        contentStreamIdentifiers;

    /// <summary>
    /// Gets whether mutating this source is known to affect multiple
    /// placements.
    /// </summary>
    public bool HasSharedSourceImpact { get; }

    internal PdfObjectIdentifier? GetContentStreamIdentifier(
        int streamIndex)
    {
        if (contentStreamIdentifiers.Count == 0)
        {
            return null;
        }

        return contentStreamIdentifiers[streamIndex];
    }
}
