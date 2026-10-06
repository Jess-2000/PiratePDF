using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Services.Fonts;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Changes existing page text to a newly embedded external TrueType font using
/// the sanitizer-backed Type0/CID pipeline.
/// </summary>
public static class PdfExternalExistingTextFontService
{
    public static PdfExistingTextResourceChangeOperation ChangeFont(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        PdfExternalFontMatch match,
        IPdfExternalFontSanitizer sanitizer,
        string preferredResourceName = "FExt1",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(displayedPage);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(sanitizer);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredResourceName);

        session.EnsureActive();
        session.Document.EnsureOwnership(
            displayedPage);

        ValidateTarget(
            session,
            displayedPage,
            source,
            descriptor);

        if (match.Face.Technology !=
            PdfExternalFontTechnology.TrueType)
        {
            throw new NotSupportedException(
                "BF-1054Q newly embedded existing-text fonts require TrueType outlines.");
        }

        string requiredText =
            GetRequiredText(
                descriptor,
                source);

        PdfExternalTextShapingGuard
            .EnsureSimpleScalarMapping(
                requiredText);

        PdfSanitizedExternalFontProgram program =
            match.SanitizeFontProgram(
                sanitizer,
                cancellationToken:
                    cancellationToken);

        PdfDocument document =
            session.Document.GetSourceDocument();

        PdfObjectIdentifier pageIdentifier =
            displayedPage.ObjectIdentifier ??
            throw new NotSupportedException(
                "BF-1054Q requires a page bound to an indirect PDF object.");

        PdfWritePlan writePlan =
            PdfWritePlanner.Create(
                document);

        PdfExternalType0TrueTypeResourceProjection projection =
            PdfExternalType0TrueTypeResourceProjector.Create(
                writePlan,
                pageIdentifier,
                preferredResourceName,
                match.Face,
                program,
                requiredText,
                cancellationToken);

        PdfObjectIdentifier streamIdentifier =
            descriptor.Identity.ContentStreamIdentifier ??
            throw new NotSupportedException(
                "BF-1054Q requires an indirect page content stream.");

        PdfStream stream =
            document.Objects.Get<PdfStream>(
                streamIdentifier);

        byte[] decoded =
            PdfContentStreamMutationCodec.Decode(
                stream,
                cancellationToken);

        PdfDisplayTextRun run =
            GetRequiredRun(
                source,
                descriptor.Identity);

        byte[] reencoded =
            ReencodeTarget(
                decoded,
                descriptor,
                run,
                projection,
                cancellationToken);

        string originalResourceName =
            descriptor.FontResourceName ??
            throw new NotSupportedException(
                "BF-1054Q requires a known original font resource name so text state can be restored.");

        long adjustedEnd =
            checked(
                descriptor.Identity.EndOffset +
                reencoded.Length -
                decoded.Length);

        byte[] switched =
            PdfFontSwitchTokenEditor.Wrap(
                reencoded,
                descriptor.Identity.StartOffset,
                adjustedEnd,
                projection.ResourceName,
                originalResourceName,
                descriptor.Typography.FontSize);

        byte[] encoded =
            PdfContentStreamMutationCodec.Encode(
                stream,
                switched,
                cancellationToken);

        Dictionary<PdfObjectIdentifier, PdfObject> objects =
            projection.Projections.Projections.ToDictionary(
                static item => item.Identifier,
                static item => item.Value);

        return session.Document.ApplyExistingTextResourceChange(
            descriptor.Identity,
            encoded,
            objects,
            projection.ResourceName);
    }

    private static byte[] ReencodeTarget(
        ReadOnlySpan<byte> decoded,
        PdfExistingTextDescriptor descriptor,
        PdfDisplayTextRun run,
        PdfExternalType0TrueTypeResourceProjection projection,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (descriptor.Editability ==
            PdfExistingTextEditability.DirectlyEditable)
        {
            string unicode =
                descriptor.UnicodeText ??
                throw new InvalidOperationException(
                    "The directly editable target lost its Unicode mapping.");

            byte[] replacementBytes =
                projection.Subset.EncodeText(
                    unicode);

            return PdfTextStringTokenEditor.Replace(
                decoded,
                descriptor.Identity,
                descriptor.EncodedBytes.Span,
                replacementBytes);
        }

        PdfTextRunElement[] elements =
        [
            .. run.Elements.Where(
                static element =>
                    element.Kind ==
                    PdfTextRunElementKind.Text)
        ];

        var expected =
            new ReadOnlyMemory<byte>[
                elements.Length];

        var replacement =
            new ReadOnlyMemory<byte>[
                elements.Length];

        for (int index = 0;
            index < elements.Length;
            index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PdfTextRunElement element =
                elements[index];

            string unicode =
                element.UnicodeText ??
                throw new InvalidOperationException(
                    $"Positioned text segment {index} lost its Unicode mapping.");

            expected[index] =
                element.EncodedBytes;

            replacement[index] =
                projection.Subset.EncodeText(
                    unicode);
        }

        return PdfTextArrayTokenEditor.Replace(
            decoded,
            descriptor.Identity.StartOffset,
            descriptor.Identity.EndOffset,
            expected,
            replacement);
    }

    private static string GetRequiredText(
        PdfExistingTextDescriptor descriptor,
        PdfExistingTextDiscoverySource source)
    {
        if (descriptor.Editability ==
            PdfExistingTextEditability.DirectlyEditable)
        {
            return descriptor.UnicodeText ??
                throw new InvalidOperationException(
                    "The directly editable target lost its Unicode mapping.");
        }

        PdfDisplayTextRun run =
            GetRequiredRun(
                source,
                descriptor.Identity);

        return string.Concat(
            run.Elements
                .Where(
                    static element =>
                        element.Kind ==
                        PdfTextRunElementKind.Text)
                .Select(
                    static element =>
                        element.UnicodeText ??
                        throw new InvalidOperationException(
                            "A positioned text segment lost its Unicode mapping.")));
    }

    private static void ValidateTarget(
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
            identity.ContentOwnerIdentifier !=
                source.ContentOwnerIdentifier)
        {
            throw new InvalidOperationException(
                "The external-font target does not belong to the supplied editing context.");
        }

        if (source.SourceKind !=
                PdfExistingTextSourceKind.PageContent ||
            source.HasSharedSourceImpact)
        {
            throw new NotSupportedException(
                "BF-1054Q newly embedded font switching is limited to unshared page-owned text.");
        }

        if (descriptor.Editability is not
            (PdfExistingTextEditability.DirectlyEditable or
            PdfExistingTextEditability.EditableWithPositionPreservation))
        {
            throw new NotSupportedException(
                descriptor.EditabilityReason ??
                "The selected text is not eligible for an external font resource change.");
        }

        if (source.GetContentStreamIdentifier(
                identity.StreamIndex) !=
            identity.ContentStreamIdentifier)
        {
            throw new InvalidOperationException(
                "The external-font target no longer maps to the supplied content stream.");
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

            if (ordinal == identity.RunOrdinal)
            {
                return run;
            }

            ordinal++;
        }

        throw new InvalidOperationException(
            "The external-font target display-run provenance is stale.");
    }
}
