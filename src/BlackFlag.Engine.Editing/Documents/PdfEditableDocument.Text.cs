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
        ReadOnlySpan<byte> replacementStreamData) =>
        ReplaceExistingTextStream(
            identity,
            replacementStreamData,
            allowFormXObjectSource: false);

    internal PdfExistingTextEditOperation ReplaceExistingTextStream(
        PdfExistingTextIdentity identity,
        ReadOnlySpan<byte> replacementStreamData,
        bool allowFormXObjectSource)
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

        if (identity.SourceKind ==
                PdfExistingTextSourceKind.FormXObject &&
            !allowFormXObjectSource)
        {
            throw new NotSupportedException(
                "Form XObject mutation requires an explicit shared-Form editing path.");
        }

        if (identity.SourceKind is not
            (PdfExistingTextSourceKind.PageContent or
            PdfExistingTextSourceKind.FormXObject))
        {
            throw new NotSupportedException(
                "The existing-text source kind is not supported for stream mutation.");
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

    internal PdfExistingTextResourceChangeOperation ApplyExistingTextResourceChange(
        PdfExistingTextIdentity identity,
        ReadOnlySpan<byte> replacementStreamData,
        IReadOnlyDictionary<PdfObjectIdentifier, PdfObject> objectReplacements,
        string resourceName)
    {
        ArgumentNullException.ThrowIfNull(objectReplacements);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        if (identity.SessionId != owner.SessionId)
        {
            throw new PdfEditingOwnershipException(
                "The existing-text identity belongs to another editing session.");
        }

        if (identity.SourceKind != PdfExistingTextSourceKind.PageContent)
        {
            throw new NotSupportedException(
                "BF-1054Q external-font resource changes are page-content only.");
        }

        PdfObjectIdentifier streamIdentifier =
            identity.ContentStreamIdentifier ??
            throw new NotSupportedException(
                "BF-1054Q requires an indirect content stream.");

        if (!document.Objects.TryGet(
                streamIdentifier,
                out PdfObject? streamValue) ||
            streamValue is not PdfStream stream)
        {
            throw new InvalidOperationException(
                $"Existing-text content stream '{streamIdentifier}' is unavailable.");
        }

        if (objectReplacements.ContainsKey(streamIdentifier))
        {
            throw new ArgumentException(
                "The resource projection must not replace the existing-text content stream directly.",
                nameof(objectReplacements));
        }

        var previousObjects =
            new Dictionary<
                PdfObjectIdentifier,
                (bool Existed, PdfObject? Value)>();

        foreach (KeyValuePair<PdfObjectIdentifier, PdfObject> replacement
                in objectReplacements)
        {
            ArgumentNullException.ThrowIfNull(replacement.Value);

            bool existed =
                document.Objects.TryGet(
                    replacement.Key,
                    out PdfObject? previous);

            previousObjects.Add(
                replacement.Key,
                (
                    existed,
                    previous
                ));
        }

        byte[] previousStream =
            stream.ToByteArray();

        byte[] replacementStream =
            replacementStreamData.ToArray();

        var operation =
            new PdfExistingTextResourceChangeOperation(
                owner,
                identity,
                streamIdentifier,
                resourceName,
                objectReplacements.Count,
                previousStream.Length,
                replacementStream.Length);

        session.ValidateOperation(
            operation);

        void ApplyReplacement()
        {
            stream.ReplaceData(
                replacementStream);

            foreach (KeyValuePair<PdfObjectIdentifier, PdfObject> replacement
                    in objectReplacements)
            {
                if (document.Objects.Contains(
                        replacement.Key))
                {
                    document.Objects.Set(
                        replacement.Key,
                        replacement.Value);
                }
                else
                {
                    document.Objects.Add(
                        replacement.Key,
                        replacement.Value);
                }
            }
        }

        void RestorePrevious()
        {
            stream.ReplaceData(
                previousStream);

            foreach (KeyValuePair<
                        PdfObjectIdentifier,
                        (bool Existed, PdfObject? Value)> previous
                    in previousObjects.Reverse())
            {
                if (previous.Value.Existed)
                {
                    document.Objects.Set(
                        previous.Key,
                        previous.Value.Value ??
                        throw new InvalidOperationException(
                            "A captured existing PDF object unexpectedly became null."));
                }
                else
                {
                    _ = document.Objects.Remove(
                        previous.Key);
                }
            }
        }

        try
        {
            ApplyReplacement();
        }
        catch
        {
            RestorePrevious();
            throw;
        }

        session.RecordChange(
            operation,
            RestorePrevious,
            ApplyReplacement);

        return operation;
    }

}
