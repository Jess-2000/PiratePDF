using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Connects committed same-font existing-text edits to full-rewrite and
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
            if (changeSet[index] is not
                PdfExistingTextEditOperation operation)
            {
                throw new NotSupportedException(
                    "Existing-text Writing accepts only existing-text edit operations.");
            }

            session.EnsureOwnership(
                operation);

            if (!document.Objects.TryGet(
                    operation.ContentStreamIdentifier,
                    out PdfObject? value) ||
                value is not PdfStream)
            {
                throw new InvalidOperationException(
                    $"Changed existing-text stream '{operation.ContentStreamIdentifier}' is not present in the current object store.");
            }

            _ = changed.Add(
                operation.ContentStreamIdentifier);
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
