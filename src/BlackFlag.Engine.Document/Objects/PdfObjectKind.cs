namespace BlackFlag.Engine.Document.Objects;

/// <summary>
/// Identifies the semantic kind of a PDF object.
/// </summary>
public enum PdfObjectKind
{
    NullValue,
    LogicalValue,
    WholeNumber,
    RealNumber,
    NameValue,
    ByteString,
    ArrayValue,
    DictionaryValue,
    StreamValue,
    IndirectReference
}
