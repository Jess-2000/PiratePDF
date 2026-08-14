using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;

namespace BlackFlag.Engine.Editing.Validation;

/// <summary>
/// Validates an editing operation before it is allowed to mutate document
/// state.
/// </summary>
public interface IPdfEditingValidator
{
    void Validate(
        PdfEditingSession session,
        IPdfEditOperation operation);
}
