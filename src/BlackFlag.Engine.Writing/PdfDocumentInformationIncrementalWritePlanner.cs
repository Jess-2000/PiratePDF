using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Creates an incremental write plan for explicitly targeted document-information
/// fields, including safe creation of a previously absent information dictionary.
/// </summary>
public static class PdfDocumentInformationIncrementalWritePlanner
{
    /// <summary>
    /// Projects the targeted information fields and returns an incremental plan.
    /// When `/Info` is absent, a new indirect dictionary is allocated only when
    /// at least one targeted logical value is present.
    /// </summary>
    public static PdfIncrementalWritePlan Create(
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision,
        IEnumerable<PdfDocumentInformationField> fields)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);
        ArgumentNullException.ThrowIfNull(fields);

        PdfDocumentInformationField[] targets = [.. fields];

        if (writePlan.Document.Trailer.InformationObject.HasValue)
        {
            PdfObjectWriteProjectionSet existingProjections =
                PdfDocumentInformationWriteProjector.Create(
                    writePlan.Document,
                    targets);

            return PdfProjectedIncrementalWritePlanner.Create(
                writePlan,
                sourceRevision,
                existingProjections);
        }

        EnsureNewInformationDictionaryHasContent(
            writePlan,
            targets);

        PdfObjectIdentifier identifier =
            writePlan.CreateObjectAllocator().ReserveNext();

        PdfObjectWriteProjectionSet projections =
            PdfDocumentInformationWriteProjector.CreateNew(
                writePlan.Document,
                targets,
                identifier);

        writePlan.Document.Trailer.SetInformationObject(identifier);

        try
        {
            return PdfProjectedIncrementalWritePlanner.Create(
                writePlan,
                sourceRevision,
                projections);
        }
        catch
        {
            writePlan.Document.Trailer.SetInformationObject(null);
            throw;
        }
    }

    private static void EnsureNewInformationDictionaryHasContent(
        PdfWritePlan writePlan,
        PdfDocumentInformationField[] targets)
    {
        for (int index = 0; index < targets.Length; index++)
        {
            if (writePlan.Document.Information.GetValue(targets[index]) is not null)
            {
                return;
            }
        }

        throw new InvalidOperationException(
            "A missing document-information dictionary is not created when every targeted field is absent.");
    }
}
