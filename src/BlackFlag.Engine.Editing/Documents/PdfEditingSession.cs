using System.Runtime.CompilerServices;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Documents;

/// <summary>
/// Owns controlled editing access to one PDF document.
/// </summary>
/// <remarks>
/// A document may have at most one active editing session. The session is the
/// ownership boundary for later mutation, transaction, undo, and redo work.
/// </remarks>
public sealed class PdfEditingSession :
    IDisposable
{
    private static readonly object OwnershipGate =
        new();

    private static readonly ConditionalWeakTable<PdfDocument, object>
        ActiveDocuments =
            new();

    private readonly PdfDocument document;
    private bool disposed;

    public PdfEditingSession(
        PdfDocument document,
        PdfEditingSessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var resolvedOptions =
            options ?? new PdfEditingSessionOptions();

        ValidateOptions(resolvedOptions);

        lock (OwnershipGate)
        {
            if (ActiveDocuments.TryGetValue(
                    document,
                    out _))
            {
                throw new PdfEditingOwnershipException(
                    "The PDF document already has an active editing session.");
            }

            ActiveDocuments.Add(
                document,
                new object());
        }

        this.document = document;
        Options = resolvedOptions;
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Gets the stable identity of this editing session.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the options applied to this editing session.
    /// </summary>
    public PdfEditingSessionOptions Options { get; }

    /// <summary>
    /// Gets whether the editing session has been disposed.
    /// </summary>
    public bool IsDisposed =>
        disposed;

    /// <summary>
    /// Creates an ownership token for state created by this editing session.
    /// </summary>
    public PdfEditingOwnershipToken CreateOwnershipToken()
    {
        EnsureActive();

        return new PdfEditingOwnershipToken(Id);
    }

    /// <summary>
    /// Determines whether an ownership token belongs to this editing session.
    /// </summary>
    public bool Owns(PdfEditingOwnershipToken ownershipToken)
    {
        EnsureActive();

        return ownershipToken.SessionId == Id;
    }

    /// <summary>
    /// Verifies that an editing operation belongs to this editing session.
    /// </summary>
    public void EnsureOwnership(IPdfEditOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        EnsureActive();

        if (!Owns(operation.Owner))
        {
            throw new PdfEditingOwnershipException(
                "The editing operation belongs to another editing session.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        lock (OwnershipGate)
        {
            ActiveDocuments.Remove(document);
        }

        disposed = true;
    }

    internal PdfDocument GetDocument()
    {
        EnsureActive();
        return document;
    }

    internal void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);
    }

    private static void ValidateOptions(
        PdfEditingSessionOptions options)
    {
        if (!Enum.IsDefined(options.PreservationMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.PreservationMode,
                "The PDF editing preservation mode is not supported.");
        }
    }
}
