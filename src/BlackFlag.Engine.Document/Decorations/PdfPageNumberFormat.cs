namespace BlackFlag.Engine.Document.Decorations;

/// <summary>Identifies the logical formatting requested for generated page numbers.</summary>
public enum PdfPageNumberFormat
{
    /// <summary>Decimal Arabic numerals.</summary>
    Arabic = 0,

    /// <summary>Lowercase Roman numerals.</summary>
    LowerRoman = 1,

    /// <summary>Uppercase Roman numerals.</summary>
    UpperRoman = 2
}
