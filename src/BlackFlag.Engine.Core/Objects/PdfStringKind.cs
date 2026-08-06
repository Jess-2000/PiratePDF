namespace BlackFlag.Engine.Core.Objects;

/// <summary>
/// Identifies how a PDF string was represented in the source file.
/// </summary>
public enum PdfStringKind
{
    /// <summary>
    /// A string enclosed in parentheses.
    /// </summary>
    Literal = 0,

    /// <summary>
    /// A hexadecimal string enclosed in angle brackets.
    /// </summary>
    Hexadecimal
}
