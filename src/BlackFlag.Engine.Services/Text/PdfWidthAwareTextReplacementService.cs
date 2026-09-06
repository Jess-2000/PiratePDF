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
/// Performs same-font replacement while measuring and, for a primitive Tj
/// target, optionally preserving the original local text advance.
/// </summary>
public static class PdfWidthAwareTextReplacementService
{
    public static (
        PdfExistingTextEditOperation Operation,
        PdfTextAdvanceMeasurement Measurement)
        Replace(
            PdfEditingSession session,
            PdfEditablePage displayedPage,
            PdfExistingTextDiscoverySource source,
            PdfExistingTextDescriptor descriptor,
            string replacementText,
            bool preserveAdvance = true,
            double tolerance = 0.001,
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

        if (descriptor.Editability !=
            PdfExistingTextEditability.DirectlyEditable)
        {
            throw new NotSupportedException(
                descriptor.EditabilityReason ??
                "Width-aware replacement requires directly editable text.");
        }

        if (source.SourceKind !=
                PdfExistingTextSourceKind.PageContent ||
            source.HasSharedSourceImpact)
        {
            throw new NotSupportedException(
                "BF-1054T width-aware replacement supports unshared page-owned text only.");
        }

        PdfExistingTextIdentity identity =
            descriptor.Identity;

        if (identity.SessionId != session.Id ||
            identity.PageId != displayedPage.Id ||
            identity.SourceKind != source.SourceKind ||
            identity.ContentOwnerIdentifier !=
                source.ContentOwnerIdentifier)
        {
            throw new InvalidOperationException(
                "The width-aware target does not belong to the supplied editing context.");
        }

        PdfGraphicsOperation operation =
            GetRequiredOperation(
                source,
                identity);

        PdfDisplayTextRun run =
            GetRequiredRun(
                source,
                identity);

        if (run.Elements.Count != 1 ||
            run.Elements[0].Kind !=
                PdfTextRunElementKind.Text)
        {
            throw new NotSupportedException(
                "BF-1054T local compensation currently requires one source text element.");
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

        PdfTextAdvanceMeasurement measurement =
            PdfTextAdvanceMeasurementService.Compare(
                run,
                [replacementBytes],
                tolerance,
                cancellationToken);

        if (preserveAdvance &&
            measurement.Fit ==
                PdfTextAdvanceFit.RequiresReflow)
        {
            throw new NotSupportedException(
                "The replacement changes text advance and cannot be compensated locally; layout/reflow is required.");
        }

        PdfObjectIdentifier streamIdentifier =
            identity.ContentStreamIdentifier ??
            throw new NotSupportedException(
                "BF-1054T requires an indirect content stream.");

        PdfDocument document =
            session.Document.GetSourceDocument();

        PdfStream stream =
            document.Objects.Get<PdfStream>(
                streamIdentifier);

        byte[] decoded =
            PdfContentStreamMutationCodec.Decode(
                stream,
                cancellationToken);

        byte[] replaced =
            PdfTextStringTokenEditor.Replace(
                decoded,
                identity,
                descriptor.EncodedBytes.Span,
                replacementBytes);

        if (preserveAdvance &&
            measurement.Fit ==
                PdfTextAdvanceFit.LocallyAdjustable)
        {
            long adjustedEnd =
                checked(
                    identity.EndOffset +
                    replaced.Length -
                    decoded.Length);

            var adjustedIdentity =
                new PdfExistingTextIdentity(
                    identity.SessionId,
                    identity.PageId,
                    identity.SourceKind,
                    identity.ContentOwnerIdentifier,
                    identity.ContentStreamIdentifier,
                    identity.StreamIndex,
                    identity.StartOffset,
                    adjustedEnd,
                    identity.OperationIndex,
                    identity.RunOrdinal);

            replaced =
                PdfTextAdvanceCompensationTokenEditor.Apply(
                    replaced,
                    adjustedIdentity,
                    operation,
                    measurement.Delta,
                    run.FontSize,
                    run.GraphicsState.HorizontalScaling);
        }

        byte[] encoded =
            PdfContentStreamMutationCodec.Encode(
                stream,
                replaced,
                cancellationToken);

        PdfExistingTextEditOperation edit =
            session.Document.ReplaceExistingTextStream(
                identity,
                encoded);

        return (
            edit,
            measurement);
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
                "The width-aware target operation index is stale.");
        }

        PdfGraphicsOperation operation =
            source.Operations[
                identity.OperationIndex];

        if (operation.SourceSpan.StreamIndex != identity.StreamIndex ||
            operation.SourceSpan.StartOffset != identity.StartOffset ||
            operation.SourceSpan.EndOffset != identity.EndOffset)
        {
            throw new InvalidOperationException(
                "The width-aware target operation provenance is stale.");
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
            "The width-aware target display-run provenance is stale.");
    }
}
