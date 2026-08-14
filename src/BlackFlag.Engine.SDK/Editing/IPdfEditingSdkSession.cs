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

    void BeginTransaction();

    PdfEditingSdkChangeSet CommitTransaction();

    void RollbackTransaction();

    PdfEditingSdkChangeSet Undo();

    PdfEditingSdkChangeSet Redo();
}
