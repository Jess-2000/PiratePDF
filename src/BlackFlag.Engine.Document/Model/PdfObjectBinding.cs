using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Document.Model;

/// <summary>
/// Tracks the optional indirect-object identity assigned to a high-level
/// document-model object.
/// </summary>
internal sealed class PdfObjectBinding
{
    public PdfObjectIdentifier? Identifier { get; private set; }

    public bool IsBound =>
        Identifier.HasValue;

    public void Bind(PdfObjectIdentifier identifier)
    {
        if (
            Identifier.HasValue &&
            Identifier.Value != identifier)
        {
            throw new InvalidOperationException(
                $"The model is already bound to object '{Identifier.Value}'.");
        }

        Identifier = identifier;
    }

    public PdfObjectIdentifier GetRequiredIdentifier(
        string modelName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);

        return Identifier ??
            throw new InvalidOperationException(
                $"{modelName} has not been assigned an indirect-object identifier.");
    }
}