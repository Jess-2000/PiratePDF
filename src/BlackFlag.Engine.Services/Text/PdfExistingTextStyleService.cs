using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Applies scoped script/color/decoration semantics to one provenance-backed
/// existing-text target.
/// </summary>
public static class PdfExistingTextStyleService
{
    public static PdfExistingTextEditOperation Apply(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        PdfExistingTextStyleUpdate update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(displayedPage);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(update);

        session.EnsureActive();
        session.Document.EnsureOwnership(displayedPage);
        update.Validate();

        if (source.SourceKind != PdfExistingTextSourceKind.PageContent ||
            source.HasSharedSourceImpact)
        {
            throw new NotSupportedException(
                "BF-1054S styling is limited to unshared page-owned text.");
        }

        if (descriptor.Editability is not
            (PdfExistingTextEditability.DirectlyEditable or
            PdfExistingTextEditability.EditableWithPositionPreservation))
        {
            throw new NotSupportedException(
                descriptor.EditabilityReason ??
                "The existing-text target is not eligible for scoped style editing.");
        }

        PdfExistingTextIdentity identity =
            descriptor.Identity;

        if (identity.SessionId != session.Id ||
            identity.PageId != displayedPage.Id ||
            identity.SourceKind != source.SourceKind ||
            identity.ContentOwnerIdentifier != source.ContentOwnerIdentifier)
        {
            throw new InvalidOperationException(
                "The style target does not belong to the supplied editing context.");
        }

        if (source.GetContentStreamIdentifier(identity.StreamIndex) !=
            identity.ContentStreamIdentifier)
        {
            throw new InvalidOperationException(
                "The style target no longer maps to the supplied content stream.");
        }

        if (identity.OperationIndex < 0 ||
            identity.OperationIndex >= source.Operations.Count)
        {
            throw new InvalidOperationException(
                "The style target operation index is stale.");
        }

        PdfGraphicsOperation operation =
            source.Operations[identity.OperationIndex];

        if (operation.SourceSpan.StreamIndex != identity.StreamIndex ||
            operation.SourceSpan.StartOffset != identity.StartOffset ||
            operation.SourceSpan.EndOffset != identity.EndOffset)
        {
            throw new InvalidOperationException(
                "The style target operation provenance is stale.");
        }

        PdfDisplayTextRun run =
            GetRequiredRun(
                source,
                identity);

        PdfObjectIdentifier streamIdentifier =
            identity.ContentStreamIdentifier ??
            throw new NotSupportedException(
                "BF-1054S requires an indirect content stream.");

        PdfDocument document =
            session.Document.GetSourceDocument();

        PdfStream stream =
            document.Objects.Get<PdfStream>(
                streamIdentifier);

        byte[] decoded =
            PdfContentStreamMutationCodec.Decode(
                stream,
                cancellationToken);

        ValidateSourceText(
            decoded,
            descriptor,
            run);

        byte[] normalized =
            PdfCompoundTextOperatorNormalizer.Normalize(
                decoded,
                operation);

        long normalizedEnd =
            checked(
                identity.EndOffset +
                normalized.Length -
                decoded.Length);

        PdfExistingTextStyleUpdate resolvedUpdate =
            ResolveMeasuredDecoration(
                update,
                run,
                cancellationToken);

        byte[] styled =
            PdfExistingTextStyleTokenEditor.Apply(
                normalized,
                descriptor,
                run.GraphicsState,
                resolvedUpdate,
                normalizedEnd);

        byte[] encoded =
            PdfContentStreamMutationCodec.Encode(
                stream,
                styled,
                cancellationToken);

        return session.Document.ReplaceExistingTextStream(
            identity,
            encoded);
    }

    private static void ValidateSourceText(
        ReadOnlySpan<byte> data,
        PdfExistingTextDescriptor descriptor,
        PdfDisplayTextRun run)
    {
        if (descriptor.Editability ==
            PdfExistingTextEditability.DirectlyEditable)
        {
            ReadOnlyMemory<byte> encodedBytes = descriptor.EncodedBytes;
            _ = PdfTextStringTokenEditor.Replace(
                data,
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
            data,
            descriptor.Identity.StartOffset,
            descriptor.Identity.EndOffset,
            segments,
            segments);
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
            "The style target display-run provenance is stale.");
    }

    private static PdfExistingTextStyleUpdate ResolveMeasuredDecoration(
        PdfExistingTextStyleUpdate update,
        PdfDisplayTextRun run,
        CancellationToken cancellationToken)
    {
        if (update.Decoration is not
            { RequiresMeasurement: true } decoration)
        {
            return update;
        }

        double advance =
            Math.Abs(
                PdfTextAdvanceMeasurementService.MeasureOriginal(
                    run,
                    cancellationToken));

        if (advance <= 0)
        {
            throw new NotSupportedException(
                "Automatic text decoration requires a positive measurable text advance.");
        }

        return new PdfExistingTextStyleUpdate
        {
            ScriptStyle = update.ScriptStyle,
            SuperscriptScale = update.SuperscriptScale,
            SuperscriptRise = update.SuperscriptRise,
            SubscriptScale = update.SubscriptScale,
            SubscriptDrop = update.SubscriptDrop,
            FillColor = update.FillColor,
            StrokeColor = update.StrokeColor,
            Decoration = decoration.ResolveAdvance(
                advance)
        };
    }

}
