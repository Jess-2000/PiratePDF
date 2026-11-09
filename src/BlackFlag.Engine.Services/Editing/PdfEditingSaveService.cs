using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Services.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Editing;

/// <summary>
/// Provides the public preservation-aware save boundary for one committed
/// Editing transaction.
/// </summary>
public static class PdfEditingSaveService
{
    /// <summary>
    /// Plans and serializes one committed Editing transaction to a new byte array.
    /// </summary>
    public static byte[] Write(
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWriteOptions? options = null,
        IPdfOriginalSourceRevision? sourceRevision = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(changeSet);

        using var destination = new MemoryStream();
        Write(
            destination,
            session,
            changeSet,
            options,
            sourceRevision);
        return destination.ToArray();
    }

    /// <summary>
    /// Plans and serializes one committed Editing transaction to a caller-owned
    /// writable stream.
    /// </summary>
    public static void Write(
        Stream destination,
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWriteOptions? options = null,
        IPdfOriginalSourceRevision? sourceRevision = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(changeSet);

        if (!destination.CanWrite)
        {
            throw new ArgumentException(
                "The PDF destination must be writable.",
                nameof(destination));
        }

        if (changeSet.IsEmpty)
        {
            throw new InvalidOperationException(
                "A committed non-empty Editing change set is required for transaction-aware save.");
        }

        PdfWritePlan basePlan =
            PdfWritePlanner.Create(
                session.Document.GetSourceDocument(),
                options);

        SaveDomain domain =
            ResolveDomain(
                session,
                changeSet);

        switch (basePlan.Mode)
        {
            case PdfWriteMode.FullRewrite:
                WriteFullRewrite(
                    destination,
                    session,
                    changeSet,
                    basePlan,
                    domain);
                break;

            case PdfWriteMode.IncrementalUpdate:
                WriteIncremental(
                    destination,
                    session,
                    changeSet,
                    basePlan,
                    domain,
                    sourceRevision);
                break;

            default:
                throw new InvalidOperationException(
                    "The requested PDF write mode is not supported.");
        }
    }

    private static void WriteFullRewrite(
        Stream destination,
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWritePlan basePlan,
        SaveDomain domain)
    {
        PdfWritePlan projected =
            domain switch
            {
                SaveDomain.Pages =>
                    PdfNestedPageTransactionWritingService
                        .CreateFullRewritePlan(
                            session,
                            changeSet,
                            basePlan),

                SaveDomain.Annotations =>
                    PdfAnnotationTransactionWritingService
                        .CreateFullRewritePlan(
                            session,
                            changeSet,
                            basePlan),

                SaveDomain.ExistingText =>
                    PdfExistingTextTransactionWritingService
                        .CreateFullRewritePlan(
                            session,
                            changeSet,
                            basePlan),

                _ => throw new InvalidOperationException(
                    "The Editing save domain is not supported.")
            };

        PdfFullRewriteWriter.Write(
            destination,
            projected);
    }

    private static void WriteIncremental(
        Stream destination,
        PdfEditingSession session,
        PdfEditChangeSet changeSet,
        PdfWritePlan basePlan,
        SaveDomain domain,
        IPdfOriginalSourceRevision? sourceRevision)
    {
        if (sourceRevision is null)
        {
            throw new ArgumentNullException(
                nameof(sourceRevision),
                "Incremental Editing save requires the original source revision.");
        }

        PdfIncrementalWritePlan projected =
            domain switch
            {
                SaveDomain.Pages =>
                    PdfNestedPageTransactionWritingService
                        .CreateIncrementalPlan(
                            session,
                            changeSet,
                            basePlan,
                            sourceRevision),

                SaveDomain.Annotations =>
                    PdfAnnotationTransactionWritingService
                        .CreateIncrementalPlan(
                            session,
                            changeSet,
                            basePlan,
                            sourceRevision),

                SaveDomain.ExistingText =>
                    PdfExistingTextTransactionWritingService
                        .CreateIncrementalPlan(
                            session,
                            changeSet,
                            basePlan,
                            sourceRevision),

                _ => throw new InvalidOperationException(
                    "The Editing save domain is not supported.")
            };

        PdfIncrementalUpdateWriter.Write(
            destination,
            projected);
    }

    private static SaveDomain ResolveDomain(
        PdfEditingSession session,
        PdfEditChangeSet changeSet)
    {
        SaveDomain? domain = null;

        for (int index = 0; index < changeSet.Count; index++)
        {
            IPdfEditOperation operation =
                changeSet[index];

            session.EnsureOwnership(
                operation);

            SaveDomain current =
                operation switch
                {
                    PdfPageEditOperation =>
                        SaveDomain.Pages,
                    PdfPageGeometryEditOperation =>
                        SaveDomain.Pages,
                    PdfAnnotationEditOperation =>
                        SaveDomain.Annotations,
                    PdfExistingTextEditOperation =>
                        SaveDomain.ExistingText,
                    PdfExistingTextResourceChangeOperation =>
                        SaveDomain.ExistingText,
                    _ => throw new NotSupportedException(
                        $"Editing operation '{operation.GetType().Name}' does not have a public save orchestration path.")
                };

            if (domain.HasValue &&
                domain.Value != current)
            {
                throw new NotSupportedException(
                    "A single public save transaction cannot currently mix page and annotation edit domains.");
            }

            domain = current;
        }

        return domain ??
            throw new InvalidOperationException(
                "The committed Editing change set contains no writable operations.");
    }

    private enum SaveDomain
    {
        Pages,
        Annotations,
        ExistingText
    }
}
