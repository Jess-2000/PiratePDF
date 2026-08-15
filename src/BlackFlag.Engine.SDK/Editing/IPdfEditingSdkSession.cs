namespace BlackFlag.Engine.SDK.Editing;

/// <summary>
/// Provides controlled SDK access to one active PDF editing session.
/// </summary>
public interface IPdfEditingSdkSession :
    IDisposable
{
    Guid Id { get; }

    PdfEditingSdkPreservationMode PreservationMode { get; }

    IReadOnlyList<PdfEditingSdkPage> Pages { get; }

    PdfEditingSdkStructureSummary StructureSummary { get; }

    bool CanUndo { get; }

    bool CanRedo { get; }

    PdfEditingSdkPage CreatePage(
        double width,
        double height,
        PdfEditingSdkPageRotation rotation = PdfEditingSdkPageRotation.None);

    PdfEditingSdkOperation InsertPage(
        int index,
        Guid pageId);

    PdfEditingSdkOperation RemovePage(
        Guid pageId);

    PdfEditingSdkOperation MovePage(
        Guid pageId,
        int destinationIndex);

    PdfEditingSdkOperation ReplacePage(
        Guid targetPageId,
        Guid replacementPageId);

    PdfEditingSdkOperation SetPageRotation(
        Guid pageId,
        PdfEditingSdkPageRotation rotation);

    PdfEditingSdkOperation ReplacePageContent(
        Guid pageId,
        ReadOnlyMemory<byte> replacement);

    string? GetDocumentInformation(
        PdfEditingSdkDocumentInformationField field);

    PdfEditingSdkOperation SetDocumentInformation(
        PdfEditingSdkDocumentInformationField field,
        string value);

    PdfEditingSdkOperation ClearDocumentInformation(
        PdfEditingSdkDocumentInformationField field);

    PdfEditingSdkPageBoxState GetPageBox(
        Guid pageId,
        PdfEditingSdkPageBoxKind kind);

    PdfEditingSdkOperation SetPageBox(
        Guid pageId,
        PdfEditingSdkPageBoxKind kind,
        PdfEditingSdkPageBox box);

    PdfEditingSdkOperation CropPage(
        Guid pageId,
        PdfEditingSdkPageBox cropBox);

    PdfEditingSdkOperation ResizePage(
        Guid pageId,
        double width,
        double height,
        PdfEditingSdkPageResizeAnchor anchor = PdfEditingSdkPageResizeAnchor.LowerLeft);

    void ValidateConsistency();

    void BeginTransaction();

    PdfEditingSdkChangeSet CommitTransaction();

    void RollbackTransaction();

    PdfEditingSdkChangeSet Undo();

    PdfEditingSdkChangeSet Redo();
}
