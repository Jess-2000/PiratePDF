using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>Describes one indirect-object value projected for preservation-aware writing.</summary>
public sealed class PdfObjectWriteProjection
{
    public PdfObjectWriteProjection(PdfObjectIdentifier identifier, PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Identifier = identifier;
        Value = value;
    }

    public PdfObjectIdentifier Identifier { get; }
    public PdfObject Value { get; }
}
