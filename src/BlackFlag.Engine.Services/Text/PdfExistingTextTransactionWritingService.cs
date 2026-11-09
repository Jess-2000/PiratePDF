using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Connects committed existing-text and font-resource edits to full-rewrite and
/// incremental preservation-aware Writing.
/// </summary>
public static class PdfExistingTextTransactionWritingService
{
    public static PdfWritePlan CreateFullRewritePlan(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWritePlan writePlan)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(changeSet);
        ArgumentNullException.ThrowIfNull(writePlan);

        if (writePlan.Mode !=
            PdfWriteMode.FullRewrite)
        {
            throw new InvalidOperationException(
                "Existing-text full rewriting requires a full-rewrite base plan.");
        }

        _ = Analyze(
            session,
            changeSet,
            writePlan);

        return writePlan;
    }

    public static PdfIncrementalWritePlan CreateIncrementalPlan(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWritePlan writePlan,
        IPdfOriginalSourceRevision sourceRevision)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(changeSet);
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(sourceRevision);

        if (writePlan.Mode !=
            PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "Existing-text incremental Writing requires an incremental base plan.");
        }

        PdfObjectIdentifier[] changed =
            Analyze(
                session,
                changeSet,
                writePlan);

        return PdfIncrementalWritePlanner.Create(
            writePlan,
            sourceRevision,
            changed);
    }

    private static PdfObjectIdentifier[] Analyze(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWritePlan writePlan)
    {
        PdfDocument document =
            session.Document.GetSourceDocument();

        if (!ReferenceEquals(
                document,
                writePlan.Document))
        {
            throw new InvalidOperationException(
                "The Writing plan does not belong to the editing session document.");
        }

        if (changeSet.IsEmpty)
        {
            throw new InvalidOperationException(
                "Existing-text Writing requires a non-empty committed change set.");
        }

        var changed =
            new HashSet<PdfObjectIdentifier>();

        for (int index = 0;
            index < changeSet.Count;
            index++)
        {
            IPdfEditOperation operation = changeSet[index];
            PdfObjectIdentifier streamIdentifier;
            switch (operation)
            {
                case PdfExistingTextEditOperation text:
                    streamIdentifier = text.ContentStreamIdentifier;
                    break;
                case PdfExistingTextResourceChangeOperation resource:
                    session.EnsureOwnership(resource);
                    if (resource.AffectedObjectIdentifiers.Count != resource.AffectedObjectCount)
                        throw new InvalidOperationException("The font-resource edit has no complete object identifier snapshot.");
                    foreach (PdfObjectIdentifier identifier in resource.AffectedObjectIdentifiers)
                    {
                        if (!document.Objects.Contains(identifier))
                            throw new InvalidOperationException($"Changed font-resource object '{identifier}' is unavailable.");
                        changed.Add(identifier);
                    }
                    streamIdentifier = resource.ContentStreamIdentifier;
                    break;
                default:
                    throw new NotSupportedException(
                        "Existing-text Writing accepts only existing-text edit and font-resource operations.");
            }

            session.EnsureOwnership(
                operation);

            if (!document.Objects.TryGet(
                    streamIdentifier,
                    out PdfObject? value) ||
                value is not PdfStream)
            {
                throw new InvalidOperationException(
                    $"Changed existing-text stream '{streamIdentifier}' is not present in the current object store.");
            }

            _ = changed.Add(
                streamIdentifier);
        }

        return
        [
            .. changed
                .OrderBy(
                    static identifier =>
                        identifier.ObjectNumber)
                .ThenBy(
                    static identifier =>
                        identifier.GenerationNumber)
        ];
    }
}
