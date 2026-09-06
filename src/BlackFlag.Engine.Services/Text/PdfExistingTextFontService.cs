using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Fonts;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Changes existing text to another already-resolved font resource in the same
/// active source resource scope without importing or embedding a new font.
/// </summary>
public static class PdfExistingTextFontService
{
    public static PdfExistingTextEditOperation ChangeFont(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        string selectedResourceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(displayedPage);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedResourceName);

        session.EnsureActive();
        session.Document.EnsureOwnership(
            displayedPage);

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
                "The existing-text target is not eligible for font switching.");
        }

        if (source.SourceKind !=
                PdfExistingTextSourceKind.PageContent ||
            source.HasSharedSourceImpact)
        {
            throw new NotSupportedException(
                "BF-1054G supports unshared page-owned text only.");
        }

        string originalResourceName =
            descriptor.FontResourceName ??
            throw new NotSupportedException(
                "Font switching requires a known original font resource name.");

        if (string.Equals(
                originalResourceName,
                selectedResourceName,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The selected font resource is already active for this text.",
                nameof(selectedResourceName));
        }

        PdfResolvedFont selectedFont =
            source.GetRequiredFont(
                selectedResourceName);

        PdfGraphicsOperation operation =
            GetRequiredOperation(
                source,
                descriptor.Identity);

        if (operation.Operator.Code is not ("Tj" or "TJ"))
        {
            throw new NotSupportedException(
                "BF-1054G font switching supports Tj and TJ targets only.");
        }

        PdfDisplayTextRun run =
            GetRequiredRun(
                source,
                descriptor.Identity);

        PdfObjectIdentifier streamIdentifier =
            descriptor.Identity.ContentStreamIdentifier ??
            throw new NotSupportedException(
                "BF-1054G requires an indirect content stream.");

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

        byte[] reencoded =
            ReencodeText(
                currentData,
                descriptor,
                run,
                selectedFont,
                cancellationToken);

        long adjustedEnd =
            checked(
                descriptor.Identity.EndOffset +
                (reencoded.Length - currentData.Length));

        byte[] replacementData =
            PdfFontSwitchTokenEditor.Wrap(
                reencoded,
                descriptor.Identity.StartOffset,
                adjustedEnd,
                selectedResourceName,
                originalResourceName,
                descriptor.Typography.FontSize);

        byte[] encodedReplacement =
            PdfContentStreamMutationCodec.Encode(
                stream,
                replacementData,
                cancellationToken);

        return session.Document.ReplaceExistingTextStream(
            descriptor.Identity,
            encodedReplacement);
    }

    private static byte[] ReencodeText(
        ReadOnlySpan<byte> currentData,
        PdfExistingTextDescriptor descriptor,
        PdfDisplayTextRun run,
        PdfResolvedFont selectedFont,
        CancellationToken cancellationToken)
    {
        var encoder =
            new PdfSameFontTextEncoder();

        if (descriptor.Editability ==
            PdfExistingTextEditability.DirectlyEditable)
        {
            string unicode =
                descriptor.UnicodeText ??
                throw new InvalidOperationException(
                    "A directly editable font-switch target lost its Unicode text.");

            if (!encoder.TryEncode(
                    selectedFont,
                    unicode,
                    out byte[] replacement,
                    out string? reason,
                    cancellationToken))
            {
                throw new NotSupportedException(
                    reason ??
                    "The selected font cannot represent the existing text.");
            }

            return PdfTextStringTokenEditor.Replace(
                currentData,
                descriptor.Identity,
                descriptor.EncodedBytes.Span,
                replacement);
        }

        PdfTextRunElement[] textElements =
        [
            .. run.Elements.Where(
                static element =>
                    element.Kind ==
                    PdfTextRunElementKind.Text)
        ];

        var expected =
            new ReadOnlyMemory<byte>[
                textElements.Length];
        var replacementSegments =
            new ReadOnlyMemory<byte>[
                textElements.Length];

        for (int index = 0;
            index < textElements.Length;
            index++)
        {
            PdfTextRunElement element =
                textElements[index];

            string unicode =
                element.UnicodeText ??
                throw new InvalidOperationException(
                    $"Positioned text segment {index} lost its Unicode mapping.");

            expected[index] =
                element.EncodedBytes;

            if (!encoder.TryEncode(
                    selectedFont,
                    unicode,
                    out byte[] replacement,
                    out string? reason,
                    cancellationToken))
            {
                throw new NotSupportedException(
                    reason ??
                    $"The selected font cannot represent positioned text segment {index}.");
            }

            replacementSegments[index] =
                replacement;
        }

        return PdfTextArrayTokenEditor.Replace(
            currentData,
            descriptor.Identity.StartOffset,
            descriptor.Identity.EndOffset,
            expected,
            replacementSegments);
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
                "The font-switch target does not belong to the supplied editing session/page/source.");
        }

        if (source.GetContentStreamIdentifier(
                identity.StreamIndex) !=
            identity.ContentStreamIdentifier)
        {
            throw new InvalidOperationException(
                "The font-switch target no longer maps to the supplied content stream.");
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
                "The font-switch target operation index is stale.");
        }

        PdfGraphicsOperation operation =
            source.Operations[
                identity.OperationIndex];

        if (operation.SourceSpan.StreamIndex != identity.StreamIndex ||
            operation.SourceSpan.StartOffset != identity.StartOffset ||
            operation.SourceSpan.EndOffset != identity.EndOffset)
        {
            throw new InvalidOperationException(
                "The font-switch target operation provenance is stale.");
        }

        return operation;
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
            "The font-switch target display-run provenance is stale.");
    }
}
