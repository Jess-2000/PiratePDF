using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Fonts;

/// <summary>
/// Connects external TrueType font-resource construction to the existing
/// preservation-aware full-rewrite and incremental projection planners.
/// </summary>
public static class PdfExternalFontEmbeddingWritingService
{
    public static PdfWritePlan CreateFullRewritePlan(
        PdfWritePlan writePlan,
        PdfObjectIdentifier pageIdentifier,
        string resourceName,
        PdfExternalFontFace face,
        PdfSanitizedExternalFontProgram program)
    {
        ArgumentNullException.ThrowIfNull(writePlan);

        if (writePlan.Mode != PdfWriteMode.FullRewrite)
        {
            throw new InvalidOperationException(
                "External-font full embedding requires a full-rewrite base plan.");
        }

        PdfExternalFontResourceProjection projection =
            PdfExternalTrueTypeResourceProjector.Create(
                writePlan,
                pageIdentifier,
                resourceName,
                face,
                program);

        return PdfProjectedFullRewritePlanner.Create(
            writePlan,
            projection.Projections);
    }

    public static PdfIncrementalWritePlan CreateIncrementalPlan(
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision,
        PdfObjectIdentifier pageIdentifier,
        string resourceName,
        PdfExternalFontFace face,
        PdfSanitizedExternalFontProgram program)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);

        if (writePlan.Mode != PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "External-font incremental embedding requires an incremental base plan.");
        }

        PdfExternalFontResourceProjection projection =
            PdfExternalTrueTypeResourceProjector.Create(
                writePlan,
                pageIdentifier,
                resourceName,
                face,
                program);

        return PdfProjectedIncrementalWritePlanner.Create(
            writePlan,
            sourceRevision,
            projection.Projections);
    }

    public static PdfWritePlan CreateSubsetFullRewritePlan(
        PdfWritePlan writePlan,
        PdfObjectIdentifier pageIdentifier,
        string resourceName,
        PdfExternalFontFace face,
        PdfSanitizedExternalFontProgram program,
        string requiredText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writePlan);

        if (writePlan.Mode != PdfWriteMode.FullRewrite)
        {
            throw new InvalidOperationException(
                "External-font subset embedding requires a full-rewrite base plan.");
        }

        PdfExternalFontResourceProjection projection =
            PdfExternalTrueTypeSubsetResourceProjector.Create(
                writePlan,
                pageIdentifier,
                resourceName,
                face,
                program,
                requiredText,
                cancellationToken);

        return PdfProjectedFullRewritePlanner.Create(
            writePlan,
            projection.Projections);
    }

    public static PdfIncrementalWritePlan CreateSubsetIncrementalPlan(
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision,
        PdfObjectIdentifier pageIdentifier,
        string resourceName,
        PdfExternalFontFace face,
        PdfSanitizedExternalFontProgram program,
        string requiredText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);

        if (writePlan.Mode != PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "External-font subset embedding requires an incremental base plan.");
        }

        PdfExternalFontResourceProjection projection =
            PdfExternalTrueTypeSubsetResourceProjector.Create(
                writePlan,
                pageIdentifier,
                resourceName,
                face,
                program,
                requiredText,
                cancellationToken);

        return PdfProjectedIncrementalWritePlanner.Create(
            writePlan,
            sourceRevision,
            projection.Projections);
    }


    public static PdfWritePlan CreateUnicodeSubsetFullRewritePlan(
        PdfWritePlan writePlan,
        PdfObjectIdentifier pageIdentifier,
        string resourceName,
        PdfExternalFontFace face,
        PdfSanitizedExternalFontProgram program,
        string requiredText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writePlan);

        if (writePlan.Mode != PdfWriteMode.FullRewrite)
        {
            throw new InvalidOperationException(
                "Unicode external-font subset embedding requires a full-rewrite base plan.");
        }

        PdfExternalType0TrueTypeResourceProjection projection =
            PdfExternalType0TrueTypeResourceProjector.Create(
                writePlan,
                pageIdentifier,
                resourceName,
                face,
                program,
                requiredText,
                cancellationToken);

        return PdfProjectedFullRewritePlanner.Create(
            writePlan,
            projection.Projections);
    }

    public static PdfIncrementalWritePlan CreateUnicodeSubsetIncrementalPlan(
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision,
        PdfObjectIdentifier pageIdentifier,
        string resourceName,
        PdfExternalFontFace face,
        PdfSanitizedExternalFontProgram program,
        string requiredText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);

        if (writePlan.Mode != PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "Unicode external-font subset embedding requires an incremental base plan.");
        }

        PdfExternalType0TrueTypeResourceProjection projection =
            PdfExternalType0TrueTypeResourceProjector.Create(
                writePlan,
                pageIdentifier,
                resourceName,
                face,
                program,
                requiredText,
                cancellationToken);

        return PdfProjectedIncrementalWritePlanner.Create(
            writePlan,
            sourceRevision,
            projection.Projections);
    }

}
