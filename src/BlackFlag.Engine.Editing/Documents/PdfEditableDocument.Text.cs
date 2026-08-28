using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Text;

namespace BlackFlag.Engine.Editing.Documents;

public sealed partial class PdfEditableDocument
{
    /// <summary>
    /// Replaces the bytes of the indirect content stream identified by one
    /// validated existing-text identity.
    /// </summary>
    internal PdfExistingTextEditOperation ReplaceExistingTextStream(
        PdfExistingTextIdentity identity,
        ReadOnlySpan<byte> replacementStreamData)
    {
        if (identity.SessionId !=
            owner.SessionId)
        {
            throw new PdfEditingOwnershipException(
                "The existing-text identity belongs to another editing session.");
        }

        bool pageExists = false;

        for (int index = 0;
            index < Pages.Count;
            index++)
        {
            if (Pages[index].Id ==
                identity.PageId)
            {
                pageExists = true;
                break;
            }
        }

        if (!pageExists)
        {
            throw new InvalidOperationException(
                "The existing-text identity refers to a page that is no longer attached to this editing document.");
        }

        if (identity.SourceKind !=
            PdfExistingTextSourceKind.PageContent)
        {
            throw new NotSupportedException(
                "BF-1054C mutates page-owned text only; Form XObject mutation is deferred.");
        }

        PdfObjectIdentifier streamIdentifier =
            identity.ContentStreamIdentifier ??
            throw new NotSupportedException(
                "BF-1054C requires an indirect content-stream identity.");

        if (!document.Objects.TryGet(
                streamIdentifier,
                out PdfObject? value) ||
            value is not PdfStream stream)
        {
            throw new InvalidOperationException(
                $"Existing-text content stream '{streamIdentifier}' is not present as a stream in the authoritative object store.");
        }

        byte[] previous =
            stream.ToByteArray();
        byte[] replacement =
            replacementStreamData.ToArray();

        var operation =
            new PdfExistingTextEditOperation(
                owner,
                identity,
                streamIdentifier,
                previous.Length,
                replacement.Length);

        session.ValidateOperation(
            operation);

        stream.ReplaceData(
            replacement);

        session.RecordChange(
            operation,
            () =>
                stream.ReplaceData(
                    previous),
            () =>
                stream.ReplaceData(
                    replacement));

        return operation;
    }
}
