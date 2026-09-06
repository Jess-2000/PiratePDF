using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Fonts;
using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Performs conservative same-font replacement of provenance-backed existing
/// text in an unfiltered indirect page content stream.
/// </summary>
public static class PdfExistingTextReplacementService
{
    /// <summary>
    /// Replaces or deletes one directly editable existing-text run without
    /// changing font resources or unrelated operation syntax.
    /// </summary>
    public static PdfExistingTextEditOperation Replace(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        string replacementText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(displayedPage);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(replacementText);

        session.EnsureActive();
        session.Document.EnsureOwnership(
            displayedPage);

        ValidateIdentity(
            session,
            displayedPage,
            source,
            descriptor);

        if (descriptor.Editability !=
            PdfExistingTextEditability.DirectlyEditable)
        {
            throw new NotSupportedException(
                descriptor.EditabilityReason ??
                "The selected text is not directly editable.");
        }

        if (source.SourceKind !=
                PdfExistingTextSourceKind.PageContent ||
            source.HasSharedSourceImpact)
        {
            throw new NotSupportedException(
                "BF-1054C supports unshared page-owned text only.");
        }

        PdfObjectIdentifier streamIdentifier =
            descriptor.Identity.ContentStreamIdentifier ??
            throw new NotSupportedException(
                "BF-1054C requires an indirect content stream.");

        if (source.GetContentStreamIdentifier(
                descriptor.Identity.StreamIndex) !=
            streamIdentifier)
        {
            throw new InvalidOperationException(
                "The existing-text identity no longer maps to the supplied content source.");
        }

        PdfGraphicsOperation operation =
            GetRequiredOperation(
                source,
                descriptor.Identity);

        ValidateOperator(
            operation);

        PdfDisplayTextRun run =
            GetRequiredRun(
                source,
                descriptor.Identity);

        if (!run.EncodedBytes.Span.SequenceEqual(
                descriptor.EncodedBytes.Span))
        {
            throw new InvalidOperationException(
                "The existing-text descriptor no longer matches the discovered display run.");
        }

        if (run.Elements.Count != 1 ||
            run.Elements[0].Kind !=
                PdfTextRunElementKind.Text)
        {
            throw new NotSupportedException(
                "BF-1054C requires exactly one source text element; composite TJ editing is deferred.");
        }

        var encoder =
            new PdfSameFontTextEncoder();

        if (!encoder.TryEncode(
                run.Font,
                replacementText,
                out byte[] replacementBytes,
                out string? encodingFailure,
                cancellationToken))
        {
            throw new NotSupportedException(
                encodingFailure ??
                "The replacement text cannot be encoded through the existing font.");
        }

        PdfDocument document =
            session.Document.GetSourceDocument();

        if (!document.Objects.TryGet(
                streamIdentifier,
                out PdfObject? value) ||
            value is not PdfStream stream)
        {
            throw new InvalidOperationException(
                $"Existing-text content stream '{streamIdentifier}' is not available in the authoritative object store.");
        }

        byte[] currentData =
            PdfContentStreamMutationCodec.Decode(
                stream,
                cancellationToken);

        byte[] replacementData =
            PdfTextStringTokenEditor.Replace(
                currentData,
                descriptor.Identity,
                descriptor.EncodedBytes.Span,
                replacementBytes);

        byte[] encodedReplacement =
            PdfContentStreamMutationCodec.Encode(
                stream,
                replacementData,
                cancellationToken);

        return session.Document.ReplaceExistingTextStream(
            descriptor.Identity,
            encodedReplacement);
    }

    private static void ValidateIdentity(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor)
    {
        PdfExistingTextIdentity identity =
            descriptor.Identity;

        if (identity.SessionId !=
            session.Id)
        {
            throw new InvalidOperationException(
                "The existing-text identity belongs to a different editing session.");
        }

        if (identity.PageId !=
            displayedPage.Id)
        {
            throw new InvalidOperationException(
                "The existing-text identity belongs to a different displayed page.");
        }

        if (identity.SourceKind !=
                source.SourceKind ||
            identity.ContentOwnerIdentifier !=
                source.ContentOwnerIdentifier)
        {
            throw new InvalidOperationException(
                "The existing-text identity does not belong to the supplied authoritative content source.");
        }
    }

    private static PdfGraphicsOperation GetRequiredOperation(
        PdfExistingTextDiscoverySource source,
        PdfExistingTextIdentity identity)
    {
        if (identity.OperationIndex < 0 ||
            identity.OperationIndex >=
                source.Operations.Count)
        {
            throw new InvalidOperationException(
                "The existing-text operation index is stale.");
        }

        PdfGraphicsOperation operation =
            source.Operations[
                identity.OperationIndex];

        if (operation.SourceSpan.StreamIndex !=
                identity.StreamIndex ||
            operation.SourceSpan.StartOffset !=
                identity.StartOffset ||
            operation.SourceSpan.EndOffset !=
                identity.EndOffset)
        {
            throw new InvalidOperationException(
                "The existing-text operation provenance is stale.");
        }

        return operation;
    }

    private static void ValidateOperator(
        PdfGraphicsOperation operation)
    {
        switch (operation.Operator.Code)
        {
            case "Tj":
            case "'":
            case "\"":
            case "TJ":
                return;

            default:
                throw new NotSupportedException(
                    $"BF-1054C does not support direct replacement of operator '{operation.Operator.Code}'.");
        }
    }

    private static PdfDisplayTextRun GetRequiredRun(
        PdfExistingTextDiscoverySource source,
        PdfExistingTextIdentity identity)
    {
        int ordinal = 0;

        foreach (PdfDisplayItem item
                in source.DisplayList)
        {
            if (item is not PdfDisplayTextRun run ||
                run.SourceSpan.StreamIndex !=
                    identity.StreamIndex ||
                run.SourceSpan.StartOffset !=
                    identity.StartOffset ||
                run.SourceSpan.EndOffset !=
                    identity.EndOffset)
            {
                continue;
            }

            if (ordinal ==
                identity.RunOrdinal)
            {
                return run;
            }

            ordinal++;
        }

        throw new InvalidOperationException(
            "The existing-text display-run provenance is stale.");
    }
}
