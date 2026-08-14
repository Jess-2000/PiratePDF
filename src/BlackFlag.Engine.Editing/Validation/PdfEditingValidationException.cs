namespace BlackFlag.Engine.Editing.Validation;

/// <summary>
/// Represents an editing request that violates a document-editing invariant.
/// </summary>
public sealed class PdfEditingValidationException :
    InvalidOperationException
{
    public PdfEditingValidationException(string message)
        : base(message)
    {
    }
}
