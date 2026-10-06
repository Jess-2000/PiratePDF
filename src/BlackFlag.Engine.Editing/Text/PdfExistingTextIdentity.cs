using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Editing.Text;

/// <summary>
/// Identifies one provenance-backed existing-text target within an editing
/// session without relying on viewport coordinates or display-list indices.
/// </summary>
public readonly record struct PdfExistingTextIdentity
{
    /// <summary>Initializes an existing-text identity.</summary>
    public PdfExistingTextIdentity(
        Guid sessionId,
        Guid pageId,
        PdfExistingTextSourceKind sourceKind,
        PdfObjectIdentifier contentOwnerIdentifier,
        PdfObjectIdentifier? contentStreamIdentifier,
        int streamIndex,
        long startOffset,
        long endOffset,
        int operationIndex,
        int runOrdinal)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Existing-text identity requires a non-empty editing-session identity.",
                nameof(sessionId));
        }

        if (pageId == Guid.Empty)
        {
            throw new ArgumentException(
                "Existing-text identity requires a non-empty displayed-page identity.",
                nameof(pageId));
        }

        if (!Enum.IsDefined(sourceKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceKind),
                sourceKind,
                "The existing-text source kind is not supported.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(streamIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(startOffset);
        ArgumentOutOfRangeException.ThrowIfLessThan(endOffset, startOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(operationIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(runOrdinal);

        SessionId = sessionId;
        PageId = pageId;
        SourceKind = sourceKind;
        ContentOwnerIdentifier = contentOwnerIdentifier;
        ContentStreamIdentifier = contentStreamIdentifier;
        StreamIndex = streamIndex;
        StartOffset = startOffset;
        EndOffset = endOffset;
        OperationIndex = operationIndex;
        RunOrdinal = runOrdinal;
    }

    /// <summary>Gets the editing session in which the identity is valid.</summary>
    public Guid SessionId { get; }

    /// <summary>Gets the page on which the text is displayed.</summary>
    public Guid PageId { get; }

    /// <summary>Gets the kind of content source that defines the text.</summary>
    public PdfExistingTextSourceKind SourceKind { get; }

    /// <summary>
    /// Gets the indirect object that owns the authoritative source content.
    /// For page content this is the page owner; for Form text this is the Form.
    /// </summary>
    public PdfObjectIdentifier ContentOwnerIdentifier { get; }

    /// <summary>
    /// Gets the indirect content-stream object when the stream itself has an
    /// identity, or <see langword="null"/> for direct content streams.
    /// </summary>
    public PdfObjectIdentifier? ContentStreamIdentifier { get; }

    /// <summary>Gets the zero-based stream position within the content owner.</summary>
    public int StreamIndex { get; }

    /// <summary>Gets the inclusive decoded source-byte offset.</summary>
    public long StartOffset { get; }

    /// <summary>Gets the exclusive decoded source-byte offset.</summary>
    public long EndOffset { get; }

    /// <summary>Gets the canonical operation ordinal within the decoded stream.</summary>
    public int OperationIndex { get; }

    /// <summary>Gets the run ordinal emitted from the source operation.</summary>
    public int RunOrdinal { get; }

    /// <summary>Gets the number of decoded source bytes covered by the operation.</summary>
    public long SourceLength => EndOffset - StartOffset;
}
