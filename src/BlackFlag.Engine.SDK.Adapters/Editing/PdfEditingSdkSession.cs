using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Forms;
using BlackFlag.Engine.Editing.Geometry;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Outlines;
using BlackFlag.Engine.Editing.Structural;
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
    private readonly Dictionary<Guid, PdfEditablePage> pagesById = [];
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

        for (var index = 0; index < session.Document.Pages.Count; index++)
        {
            var page = session.Document.Pages[index];
            pagesById.Add(page.Id, page);
        }
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

    public PdfEditingSdkStructureSummary StructureSummary =>
        CreateStructureSummary();

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

        pagesById.Add(page.Id, page);
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

    public string? GetDocumentInformation(
        PdfEditingSdkDocumentInformationField field)
    {
        var state =
            session.Document.GetDocumentInformationState(
                MapDocumentInformationField(field));

        return state.HasValue ? state.Value : null;
    }

    public PdfEditingSdkOperation SetDocumentInformation(
        PdfEditingSdkDocumentInformationField field,
        string value) =>
        MapOperation(
            session.Document.SetDocumentInformation(
                MapDocumentInformationField(field),
                value));

    public PdfEditingSdkOperation ClearDocumentInformation(
        PdfEditingSdkDocumentInformationField field) =>
        MapOperation(
            session.Document.ClearDocumentInformation(
                MapDocumentInformationField(field)));

    public PdfEditingSdkPageBoxState GetPageBox(
        Guid pageId,
        PdfEditingSdkPageBoxKind kind)
    {
        var state = FindPage(pageId).GetBoxState(MapPageBoxKind(kind));
        return new PdfEditingSdkPageBoxState(
            MapOrigin(state.Origin),
            MapPageBox(state.Value));
    }

    public PdfEditingSdkOperation SetPageBox(
        Guid pageId,
        PdfEditingSdkPageBoxKind kind,
        PdfEditingSdkPageBox box) =>
        MapOperation(
            session.Document.SetPageBox(
                FindPage(pageId),
                MapPageBoxKind(kind),
                MapPageBox(box)));

    public PdfEditingSdkOperation CropPage(
        Guid pageId,
        PdfEditingSdkPageBox cropBox) =>
        MapOperation(
            session.Document.CropPage(
                FindPage(pageId),
                MapPageBox(cropBox)));

    public PdfEditingSdkOperation ResizePage(
        Guid pageId,
        double width,
        double height,
        PdfEditingSdkPageResizeAnchor anchor = PdfEditingSdkPageResizeAnchor.LowerLeft) =>
        MapOperation(
            session.Document.ResizePage(
                FindPage(pageId),
                width,
                height,
                MapResizeAnchor(anchor)));

    public void ValidateConsistency() =>
        PdfEditingService.ValidateConsistency(session);

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
        if (pagesById.TryGetValue(pageId, out var page))
        {
            return page;
        }

        throw new KeyNotFoundException(
            $"Editable page '{pageId}' does not belong to this SDK editing session.");
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

            PdfPageGeometryEditOperation geometry =>
                new PdfEditingSdkOperation(
                    geometry.Scope.ToString(),
                    geometry.Kind.ToString(),
                    geometry.PageId),

            PdfDocumentInformationEditOperation information =>
                new PdfEditingSdkOperation(
                    information.Scope.ToString(),
                    information.Kind.ToString()),

            PdfAnnotationEditOperation annotation =>
                new PdfEditingSdkOperation(
                    annotation.Scope.ToString(),
                    annotation.Kind.ToString(),
                    annotation.PageId),

            PdfFormFieldEditOperation formField =>
                new PdfEditingSdkOperation(
                    formField.Scope.ToString(),
                    formField.Kind.ToString()),

            PdfOutlineEditOperation outline =>
                new PdfEditingSdkOperation(
                    outline.Scope.ToString(),
                    outline.Kind.ToString()),

            PdfPageDecorationEditOperation decoration =>
                new PdfEditingSdkOperation(
                    decoration.Scope.ToString(),
                    decoration.Kind.ToString()),

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

    private PdfEditingSdkStructureSummary CreateStructureSummary()
    {
        var annotationCount = 0;
        for (var pageIndex = 0; pageIndex < session.Document.Pages.Count; pageIndex++)
        {
            annotationCount += session.Document.Pages[pageIndex].Annotations.Count;
        }

        var formFieldCount = CountFormFields(session.Document.FormFields);
        var outlineItemCount = CountOutlineItems(session.Document.OutlineItems);
        var informationCount = 0;
        foreach (var field in Enum.GetValues<PdfDocumentInformationField>())
        {
            if (session.Document.GetDocumentInformationState(field).HasValue)
            {
                informationCount++;
            }
        }

        return new PdfEditingSdkStructureSummary(
            session.Document.PageCount,
            annotationCount,
            session.Document.FormFields.Count,
            formFieldCount,
            session.Document.OutlineItems.Count,
            outlineItemCount,
            session.Document.PageDecorations.Count,
            informationCount);
    }

    private static int CountFormFields(PdfEditableFormFieldCollection fields)
    {
        var count = fields.Count;
        for (var index = 0; index < fields.Count; index++)
        {
            count += CountFormFields(fields[index].Children);
        }

        return count;
    }

    private static int CountOutlineItems(PdfEditableOutlineItemCollection items)
    {
        var count = items.Count;
        for (var index = 0; index < items.Count; index++)
        {
            count += CountOutlineItems(items[index].Children);
        }

        return count;
    }

    private static PdfDocumentInformationField MapDocumentInformationField(
        PdfEditingSdkDocumentInformationField field) =>
        field switch
        {
            PdfEditingSdkDocumentInformationField.Title => PdfDocumentInformationField.Title,
            PdfEditingSdkDocumentInformationField.Author => PdfDocumentInformationField.Author,
            PdfEditingSdkDocumentInformationField.Subject => PdfDocumentInformationField.Subject,
            PdfEditingSdkDocumentInformationField.Keywords => PdfDocumentInformationField.Keywords,
            PdfEditingSdkDocumentInformationField.Creator => PdfDocumentInformationField.Creator,
            PdfEditingSdkDocumentInformationField.Producer => PdfDocumentInformationField.Producer,
            PdfEditingSdkDocumentInformationField.CreationDate => PdfDocumentInformationField.CreationDate,
            PdfEditingSdkDocumentInformationField.ModificationDate => PdfDocumentInformationField.ModificationDate,
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "The document-information field is not supported by the SDK adapter.")
        };

    private static PdfPageBoxKind MapPageBoxKind(PdfEditingSdkPageBoxKind kind) =>
        kind switch
        {
            PdfEditingSdkPageBoxKind.MediaBox => PdfPageBoxKind.MediaBox,
            PdfEditingSdkPageBoxKind.CropBox => PdfPageBoxKind.CropBox,
            PdfEditingSdkPageBoxKind.BleedBox => PdfPageBoxKind.BleedBox,
            PdfEditingSdkPageBoxKind.TrimBox => PdfPageBoxKind.TrimBox,
            PdfEditingSdkPageBoxKind.ArtBox => PdfPageBoxKind.ArtBox,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "The page-box kind is not supported by the SDK adapter.")
        };

    private static PdfPageBox MapPageBox(PdfEditingSdkPageBox box) =>
        new(box.Left, box.Bottom, box.Right, box.Top);

    private static PdfEditingSdkPageBox MapPageBox(PdfPageBox box) =>
        new(box.Left, box.Bottom, box.Right, box.Top);

    private static PdfEditingSdkStructuralValueOrigin MapOrigin(PdfStructuralValueOrigin origin) =>
        origin switch
        {
            PdfStructuralValueOrigin.Absent => PdfEditingSdkStructuralValueOrigin.Absent,
            PdfStructuralValueOrigin.Explicit => PdfEditingSdkStructuralValueOrigin.Explicit,
            PdfStructuralValueOrigin.Inherited => PdfEditingSdkStructuralValueOrigin.Inherited,
            PdfStructuralValueOrigin.Defaulted => PdfEditingSdkStructuralValueOrigin.Defaulted,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "The structural value origin is not supported by the SDK adapter.")
        };

    private static PdfPageResizeAnchor MapResizeAnchor(PdfEditingSdkPageResizeAnchor anchor) =>
        anchor switch
        {
            PdfEditingSdkPageResizeAnchor.LowerLeft => PdfPageResizeAnchor.LowerLeft,
            PdfEditingSdkPageResizeAnchor.Center => PdfPageResizeAnchor.Center,
            _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "The page-resize anchor is not supported by the SDK adapter.")
        };

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
