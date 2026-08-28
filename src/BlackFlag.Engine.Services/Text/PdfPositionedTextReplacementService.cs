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
/// Replaces text segments in a positioned TJ array while preserving numeric
/// adjustments and all non-string source bytes.
/// </summary>
public static class PdfPositionedTextReplacementService
{
    /// <summary>
    /// Replaces one Unicode segment per original TJ text element.
    /// </summary>
    public static PdfExistingTextEditOperation Replace(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        IReadOnlyList<string> replacementSegments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(displayedPage);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(replacementSegments);

        session.EnsureActive();
        session.Document.EnsureOwnership(
            displayedPage);

        ValidateIdentity(
            session,
            displayedPage,
            source,
            descriptor);

        if (descriptor.Editability !=
            PdfExistingTextEditability.EditableWithPositionPreservation)
        {
            throw new NotSupportedException(
                descriptor.EditabilityReason ??
                "The selected text does not require the positioned TJ editing strategy.");
        }

        if (source.SourceKind !=
                PdfExistingTextSourceKind.PageContent ||
            source.HasSharedSourceImpact)
        {
            throw new NotSupportedException(
                "BF-1054D supports unshared page-owned positioned text only.");
        }

        PdfObjectIdentifier streamIdentifier =
            descriptor.Identity.ContentStreamIdentifier ??
            throw new NotSupportedException(
                "BF-1054D requires an indirect content stream.");

        if (source.GetContentStreamIdentifier(
                descriptor.Identity.StreamIndex) !=
            streamIdentifier)
        {
            throw new InvalidOperationException(
                "The positioned-text identity no longer maps to the supplied content source.");
        }

        PdfGraphicsOperation operation =
            GetRequiredOperation(
                source,
                descriptor.Identity);

        if (!string.Equals(
                operation.Operator.Code,
                "TJ",
                StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                "BF-1054D position-preserving replacement requires a TJ source operation.");
        }

        PdfDisplayTextRun run =
            GetRequiredRun(
                source,
                descriptor.Identity);

        int textElementCount = 0;

        for (int index = 0;
            index < run.Elements.Count;
            index++)
        {
            if (run.Elements[index].Kind ==
                PdfTextRunElementKind.Text)
            {
                textElementCount++;
            }
        }

        if (textElementCount < 2)
        {
            throw new NotSupportedException(
                "BF-1054D is reserved for composite positioned TJ text.");
        }

        if (replacementSegments.Count !=
            textElementCount)
        {
            throw new ArgumentException(
                $"Positioned text replacement requires exactly {textElementCount} replacement segments.",
                nameof(replacementSegments));
        }

        var expected =
            new ReadOnlyMemory<byte>[
                textElementCount];
        var replacement =
            new ReadOnlyMemory<byte>[
                textElementCount];

        var encoder =
            new PdfSameFontTextEncoder();

        int textIndex = 0;

        for (int elementIndex = 0;
            elementIndex < run.Elements.Count;
            elementIndex++)
        {
            PdfTextRunElement element =
                run.Elements[elementIndex];

            if (element.Kind !=
                PdfTextRunElementKind.Text)
            {
                continue;
            }

            string segment =
                replacementSegments[textIndex] ??
                throw new ArgumentException(
                    $"Replacement segment {textIndex} is null.",
                    nameof(replacementSegments));

            expected[textIndex] =
                element.EncodedBytes;

            if (!encoder.TryEncode(
                    run.Font,
                    segment,
                    out byte[] encoded,
                    out string? failureReason,
                    cancellationToken))
            {
                throw new NotSupportedException(
                    failureReason ??
                    $"Replacement segment {textIndex} cannot be encoded through the existing font.");
            }

            replacement[textIndex] =
                encoded;
            textIndex++;
        }

        PdfDocument document =
            session.Document.GetSourceDocument();

        if (!document.Objects.TryGet(
                streamIdentifier,
                out PdfObject? value) ||
            value is not PdfStream stream)
        {
            throw new InvalidOperationException(
                $"Positioned-text content stream '{streamIdentifier}' is not available in the authoritative object store.");
        }

        if (stream.Dictionary.ContainsKey(
                PdfNames.Filter))
        {
            throw new NotSupportedException(
                "BF-1054D positioned replacement is limited to unfiltered content streams so source offsets remain byte-preserving.");
        }

        byte[] currentData =
            stream.ToByteArray();

        byte[] replacementData =
            PdfTextArrayTokenEditor.Replace(
                currentData,
                descriptor.Identity.StartOffset,
                descriptor.Identity.EndOffset,
                expected,
                replacement);

        return session.Document.ReplaceExistingTextStream(
            descriptor.Identity,
            replacementData);
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
                "The positioned-text identity belongs to a different editing session.");
        }

        if (identity.PageId !=
            displayedPage.Id)
        {
            throw new InvalidOperationException(
                "The positioned-text identity belongs to a different displayed page.");
        }

        if (identity.SourceKind !=
                source.SourceKind ||
            identity.ContentOwnerIdentifier !=
                source.ContentOwnerIdentifier)
        {
            throw new InvalidOperationException(
                "The positioned-text identity does not belong to the supplied authoritative source.");
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
                "The positioned-text operation index is stale.");
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
                "The positioned-text operation provenance is stale.");
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
            "The positioned-text display-run provenance is stale.");
    }
}
