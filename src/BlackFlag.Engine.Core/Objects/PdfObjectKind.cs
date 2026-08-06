namespace BlackFlag.Engine.Core.Objects;

/// <summary>
/// Identifies the logical kind of a PDF object.
/// </summary>
public enum PdfObjectKind
{
    Null = 0,
    Boolean,

    IntegerNumber,
    RealNumber,

    Name,

    StringValue,

    Array,
    Dictionary,
    Stream,
    Reference
}
