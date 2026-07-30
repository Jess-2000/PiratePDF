namespace BlackFlag.Engine.Objects;

/// <summary>
/// Identifies the concrete kind of a PDF object.
/// </summary>
public enum PdfObjectType
{
    /// <summary>The PDF null object.</summary>
    Null,

    /// <summary>A PDF Boolean object.</summary>
    Boolean,

    /// <summary>A PDF integer-number object.</summary>
    IntegerNumber,

    /// <summary>A PDF real-number object.</summary>
    RealNumber,

    /// <summary>A PDF name object.</summary>
    Name,

    /// <summary>A PDF literal-string object.</summary>
    LiteralString,

    /// <summary>A PDF hexadecimal-string object.</summary>
    HexString,

    /// <summary>A PDF array object.</summary>
    Array,

    /// <summary>A PDF dictionary object.</summary>
    Dictionary,

    /// <summary>A PDF indirect reference.</summary>
    IndirectReference,

    /// <summary>A PDF stream object.</summary>
    Stream
}
