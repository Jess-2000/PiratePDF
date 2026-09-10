using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Applies locally scoped same-font PDF text-state changes to one existing
/// provenance-backed Tj/TJ target.
/// </summary>
public static class PdfExistingTextTypographyService
{
    public static PdfExistingTextEditOperation Apply(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        PdfExistingTextTypographyUpdate update)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(displayedPage);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(update);

        session.EnsureActive();
        session.Document.EnsureOwnership(displayedPage);
        update.Validate();

        if (!update.HasChanges)
        {
            throw new ArgumentException(
                "At least one typography value must change.",
                nameof(update));
        }

        ValidateIdentity(
            session,
            displayedPage,
            source,
            descriptor);

        if (descriptor.Editability is not
            (PdfExistingTextEditability.DirectlyEditable or
            PdfExistingTextEditability.EditableWithPositionPreservation))
        {
            throw new NotSupportedException(
                descriptor.EditabilityReason ??
                "The existing-text target is not eligible for scoped typography editing.");
        }

        if (source.SourceKind != PdfExistingTextSourceKind.PageContent ||
            source.HasSharedSourceImpact)
        {
            throw new NotSupportedException(
                "BF-1054F supports unshared page-owned text only.");
        }

        PdfGraphicsOperation operation =
            GetRequiredOperation(
                source,
                descriptor.Identity);

        if (operation.Operator.Code is not ("Tj" or "TJ" or "'" or "\""))
        {
            throw new NotSupportedException(
                $"Scoped typography editing does not support operator '{operation.Operator.Code}'.");
        }

        PdfDisplayTextRun run =
            GetRequiredRun(
                source,
                descriptor.Identity);

        PdfObjectIdentifier streamIdentifier =
            descriptor.Identity.ContentStreamIdentifier ??
            throw new NotSupportedException(
                "BF-1054F requires an indirect content stream.");

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
                default);

        ValidateSourceText(
            currentData,
            descriptor,
            run);

        byte[] normalizedData =
            PdfCompoundTextOperatorNormalizer.Normalize(
                currentData,
                operation);

        long normalizedEnd =
            checked(
                descriptor.Identity.EndOffset +
                normalizedData.Length -
                currentData.Length);

        byte[] replacementData =
            PdfTextStateTokenEditor.Apply(
                normalizedData,
                descriptor,
                update,
                normalizedEnd);

        byte[] encodedReplacement =
            PdfContentStreamMutationCodec.Encode(
                stream,
                replacementData,
                default);

        return session.Document.ReplaceExistingTextStream(
            descriptor.Identity,
            encodedReplacement);
    }

    private static void ValidateSourceText(
        ReadOnlySpan<byte> currentData,
        PdfExistingTextDescriptor descriptor,
        PdfDisplayTextRun run)
    {
        if (descriptor.Editability ==
            PdfExistingTextEditability.DirectlyEditable)
        {
            ReadOnlyMemory<byte> encodedBytes = descriptor.EncodedBytes;
            _ = PdfTextStringTokenEditor.Replace(
                currentData,
                descriptor.Identity,
                encodedBytes.Span,
                encodedBytes.Span);
            return;
        }

        ReadOnlyMemory<byte>[] segments =
        [
            .. run.Elements
                .Where(
                    static element =>
                        element.Kind ==
                        PdfTextRunElementKind.Text)
                .Select(
                    static element =>
                        element.EncodedBytes)
        ];

        _ = PdfTextArrayTokenEditor.Replace(
            currentData,
            descriptor.Identity.StartOffset,
            descriptor.Identity.EndOffset,
            segments,
            segments);
    }

    private static void ValidateIdentity(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor)
    {
        PdfExistingTextIdentity identity =
            descriptor.Identity;

        if (identity.SessionId != session.Id ||
            identity.PageId != displayedPage.Id ||
            identity.SourceKind != source.SourceKind ||
            identity.ContentOwnerIdentifier != source.ContentOwnerIdentifier)
        {
            throw new InvalidOperationException(
                "The typography target does not belong to the supplied editing session/page/source.");
        }

        if (source.GetContentStreamIdentifier(identity.StreamIndex) !=
            identity.ContentStreamIdentifier)
        {
            throw new InvalidOperationException(
                "The typography target no longer maps to the supplied content stream.");
        }
    }

    private static PdfGraphicsOperation GetRequiredOperation(
        PdfExistingTextDiscoverySource source,
        PdfExistingTextIdentity identity)
    {
        if (identity.OperationIndex < 0 ||
            identity.OperationIndex >= source.Operations.Count)
        {
            throw new InvalidOperationException(
                "The typography target operation index is stale.");
        }

        PdfGraphicsOperation operation =
            source.Operations[identity.OperationIndex];

        if (operation.SourceSpan.StreamIndex != identity.StreamIndex ||
            operation.SourceSpan.StartOffset != identity.StartOffset ||
            operation.SourceSpan.EndOffset != identity.EndOffset)
        {
            throw new InvalidOperationException(
                "The typography target operation provenance is stale.");
        }

        return operation;
    }

    private static PdfDisplayTextRun GetRequiredRun(
        PdfExistingTextDiscoverySource source,
        PdfExistingTextIdentity identity)
    {
        int ordinal = 0;

        foreach (PdfDisplayItem item in source.DisplayList)
        {
            if (item is not PdfDisplayTextRun run ||
                run.SourceSpan.StreamIndex != identity.StreamIndex ||
                run.SourceSpan.StartOffset != identity.StartOffset ||
                run.SourceSpan.EndOffset != identity.EndOffset)
            {
                continue;
            }

            if (ordinal == identity.RunOrdinal)
            {
                return run;
            }

            ordinal++;
        }

        throw new InvalidOperationException(
            "The typography target display-run provenance is stale.");
    }
}
