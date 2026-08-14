using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.SDK.Editing;
using BlackFlag.Engine.Services.Editing;

namespace BlackFlag.Engine.SDK.Adapters.Editing;

/// <summary>
/// Adapts the isolated SDK editing contract to the internal editing subsystem.
/// </summary>
public sealed class PdfEditingSdkSession :
    IPdfEditingSdkSession
{
    private readonly PdfEditingSession session;
    private PdfEditTransaction? transaction;

    public PdfEditingSdkSession(
        PdfDocument document,
        PdfEditingSdkPreservationMode preservationMode =
            PdfEditingSdkPreservationMode.PreserveUntargetedStructure)
    {
        ArgumentNullException.ThrowIfNull(document);

        PreservationMode =
            ValidatePreservationMode(preservationMode);

        session =
            PdfEditingService.OpenSession(
                document,
                new PdfEditingSessionOptions
                {
                    PreservationMode =
                        PdfEditingPreservationMode.PreserveUntargetedStructure
                });
    }

    public Guid Id =>
        session.Id;

    public PdfEditingSdkPreservationMode PreservationMode { get; }

    public IReadOnlyList<PdfEditingSdkPage> Pages
    {
        get
        {
            var pages =
                new PdfEditingSdkPage[session.Document.PageCount];

            for (var index = 0; index < pages.Length; index++)
            {
                pages[index] =
                    MapPage(
                        session.Document.Pages[index]);
            }

            return pages;
        }
    }

    public bool CanUndo =>
        session.History.CanUndo;

    public bool CanRedo =>
        session.History.CanRedo;

    public PdfEditingSdkPage CreatePage(
        double width,
        double height,
        PdfEditingSdkPageRotation rotation = PdfEditingSdkPageRotation.None)
    {
        var page =
            session.Document.CreatePage(
                width,
                height,
                MapRotation(rotation));

        return MapPage(page);
    }

    public PdfEditingSdkOperation InsertPage(
        int index,
        Guid pageId) =>
        MapOperation(
            session.Document.InsertPage(
                index,
                FindPage(pageId)));

    public PdfEditingSdkOperation RemovePage(
        Guid pageId) =>
        MapOperation(
            session.Document.RemovePage(
                FindPage(pageId)));

    public PdfEditingSdkOperation MovePage(
        Guid pageId,
        int destinationIndex) =>
        MapOperation(
            session.Document.MovePage(
                FindPage(pageId),
                destinationIndex));

    public PdfEditingSdkOperation ReplacePage(
        Guid targetPageId,
        Guid replacementPageId) =>
        MapOperation(
            session.Document.ReplacePage(
                FindPage(targetPageId),
                FindPage(replacementPageId)));

    public PdfEditingSdkOperation SetPageRotation(
        Guid pageId,
        PdfEditingSdkPageRotation rotation) =>
        MapOperation(
            session.Document.SetPageRotation(
                FindPage(pageId),
                MapRotation(rotation)));

    public PdfEditingSdkOperation ReplacePageContent(
        Guid pageId,
        ReadOnlyMemory<byte> replacement) =>
        MapOperation(
            session.Document.ReplacePageContent(
                FindPage(pageId),
                replacement.Span));

    public void BeginTransaction()
    {
        if (transaction is not null)
        {
            throw new InvalidOperationException(
                "The SDK editing session already has an active transaction.");
        }

        transaction =
            session.BeginTransaction();
    }

    public PdfEditingSdkChangeSet CommitTransaction()
    {
        var active =
            GetActiveTransaction();

        var changeSet =
            active.Commit();

        transaction = null;

        return MapChangeSet(changeSet);
    }

    public void RollbackTransaction()
    {
        var active =
            GetActiveTransaction();

        active.Rollback();
        transaction = null;
    }

    public PdfEditingSdkChangeSet Undo() =>
        MapChangeSet(
            session.History.Undo());

    public PdfEditingSdkChangeSet Redo() =>
        MapChangeSet(
            session.History.Redo());

    public void Dispose()
    {
        transaction?.Dispose();
        transaction = null;
        session.Dispose();
    }

    private PdfEditTransaction GetActiveTransaction() =>
        transaction ??
        throw new InvalidOperationException(
            "The SDK editing session does not have an active transaction.");

    private PdfEditablePage FindPage(
        Guid pageId)
    {
        for (var index = 0; index < session.Document.Pages.Count; index++)
        {
            var page =
                session.Document.Pages[index];

            if (page.Id == pageId)
            {
                return page;
            }
        }

        throw new KeyNotFoundException(
            $"Editable page '{pageId}' is not attached to the document.");
    }

    private static PdfEditingSdkPage MapPage(
        PdfEditablePage page) =>
        new(
            page.Id,
            page.Width,
            page.Height,
            MapRotation(page.Rotation),
            page.IsSourcePage,
            page.SourceIndex,
            page.HasContent,
            page.HasResources);

    private static PdfEditingSdkOperation MapOperation(
        IPdfEditOperation operation) =>
        operation switch
        {
            PdfPageEditOperation page =>
                new PdfEditingSdkOperation(
                    page.Scope.ToString(),
                    page.Kind.ToString(),
                    page.PageId,
                    page.RelatedPageId),

            PdfContentEditOperation content =>
                new PdfEditingSdkOperation(
                    content.Scope.ToString(),
                    content.Kind.ToString(),
                    content.PageId),

            PdfResourceEditOperation resource =>
                new PdfEditingSdkOperation(
                    resource.Scope.ToString(),
                    resource.Kind.ToString(),
                    resource.PageId),

            _ =>
                new PdfEditingSdkOperation(
                    operation.Scope.ToString(),
                    operation.GetType().Name)
        };

    private static PdfEditingSdkChangeSet MapChangeSet(
        PdfEditChangeSet changeSet)
    {
        var operations =
            new PdfEditingSdkOperation[changeSet.Count];

        for (var index = 0; index < operations.Length; index++)
        {
            operations[index] =
                MapOperation(
                    changeSet[index]);
        }

        return new PdfEditingSdkChangeSet(
            changeSet.TransactionId,
            operations);
    }

    private static PdfEditingSdkPreservationMode ValidatePreservationMode(
        PdfEditingSdkPreservationMode preservationMode) =>
        preservationMode switch
        {
            PdfEditingSdkPreservationMode.PreserveUntargetedStructure =>
                preservationMode,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(preservationMode),
                    preservationMode,
                    "The SDK editing preservation mode is not supported.")
        };

    private static PdfPageRotation MapRotation(
        PdfEditingSdkPageRotation rotation) =>
        rotation switch
        {
            PdfEditingSdkPageRotation.None =>
                PdfPageRotation.None,

            PdfEditingSdkPageRotation.Clockwise90 =>
                PdfPageRotation.Clockwise90,

            PdfEditingSdkPageRotation.Clockwise180 =>
                PdfPageRotation.Clockwise180,

            PdfEditingSdkPageRotation.Clockwise270 =>
                PdfPageRotation.Clockwise270,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(rotation),
                    rotation,
                    "The SDK page rotation is not supported.")
        };

    private static PdfEditingSdkPageRotation MapRotation(
        PdfPageRotation rotation) =>
        rotation switch
        {
            PdfPageRotation.None =>
                PdfEditingSdkPageRotation.None,

            PdfPageRotation.Clockwise90 =>
                PdfEditingSdkPageRotation.Clockwise90,

            PdfPageRotation.Clockwise180 =>
                PdfEditingSdkPageRotation.Clockwise180,

            PdfPageRotation.Clockwise270 =>
                PdfEditingSdkPageRotation.Clockwise270,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(rotation),
                    rotation,
                    "The page rotation is not supported by the SDK.")
        };
}
