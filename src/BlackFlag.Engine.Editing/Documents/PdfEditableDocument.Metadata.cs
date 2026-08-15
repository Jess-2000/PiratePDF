using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Editing.Metadata;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Documents;

public sealed partial class PdfEditableDocument
{
    /// <summary>Gets the exact state of one document-information entry.</summary>
    public PdfStructuralValueState<string> GetDocumentInformationState(
        PdfDocumentInformationField field)
    {
        var value = document.Information.GetValue(field);
        return value is null
            ? PdfStructuralValueState.Absent<string>()
            : PdfStructuralValueState.Explicit(value);
    }

    /// <summary>Sets one document-information entry explicitly.</summary>
    public PdfDocumentInformationEditOperation SetDocumentInformation(
        PdfDocumentInformationField field,
        string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var previousState =
            GetDocumentInformationState(field);
        var currentState =
            PdfStructuralValueState.Explicit(value);
        var operation =
            new PdfDocumentInformationEditOperation(
                owner,
                PdfDocumentInformationMutationKind.Set,
                field,
                previousState,
                currentState);

        session.ValidateOperation(operation);
        document.Information.SetValue(field, value);
        session.RecordChange(
            operation,
            () => RestoreDocumentInformation(field, previousState),
            () => document.Information.SetValue(field, value));

        return operation;
    }

    /// <summary>Clears one explicitly present document-information entry.</summary>
    public PdfDocumentInformationEditOperation ClearDocumentInformation(
        PdfDocumentInformationField field)
    {
        var previousState =
            GetDocumentInformationState(field);

        if (!previousState.HasValue)
        {
            throw new KeyNotFoundException(
                $"Document information field '{field}' is not present.");
        }

        var currentState =
            PdfStructuralValueState.Absent<string>();
        var operation =
            new PdfDocumentInformationEditOperation(
                owner,
                PdfDocumentInformationMutationKind.Clear,
                field,
                previousState,
                currentState);

        session.ValidateOperation(operation);
        _ = document.Information.Remove(field);
        session.RecordChange(
            operation,
            () => RestoreDocumentInformation(field, previousState),
            () => document.Information.Remove(field));

        return operation;
    }

    private void RestoreDocumentInformation(
        PdfDocumentInformationField field,
        PdfStructuralValueState<string> state)
    {
        if (state.HasValue)
        {
            document.Information.SetValue(field, state.Value);
            return;
        }

        _ = document.Information.Remove(field);
    }
}
