using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Creates a full-rewrite plan for explicitly targeted document-information
/// fields, including safe creation of a previously absent information dictionary.
/// </summary>
public static class PdfDocumentInformationFullRewritePlanner
{
    /// <summary>
    /// Projects the targeted information fields and returns a refreshed
    /// full-rewrite plan. When `/Info` is absent, a new indirect dictionary is
    /// allocated only when at least one targeted logical value is present.
    /// </summary>
    public static PdfWritePlan Create(
        PdfWritePlan writePlan,
        IEnumerable<PdfDocumentInformationField> fields)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(fields);

        if (writePlan.Mode != PdfWriteMode.FullRewrite)
        {
            throw new InvalidOperationException(
                "Document-information full-rewrite planning requires a full-rewrite base write plan.");
        }

        PdfDocumentInformationField[] targets = [.. fields];

        if (writePlan.Document.Trailer.InformationObject.HasValue)
        {
            PdfObjectWriteProjectionSet existingProjections =
                PdfDocumentInformationWriteProjector.Create(
                    writePlan.Document,
                    targets);

            return PdfProjectedFullRewritePlanner.Create(
                writePlan,
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
            return PdfProjectedFullRewritePlanner.Create(
                writePlan,
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
