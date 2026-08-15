namespace BlackFlag.Engine.Document.Forms;

/// <summary>Represents common AcroForm field-option bits.</summary>
[Flags]
public enum PdfFormFieldOptions
{
    /// <summary>No common field options are set.</summary>
    None = 0,
    /// <summary>The field is read-only.</summary>
    ReadOnly = 1 << 0,
    /// <summary>The field is required for submission.</summary>
    Required = 1 << 1,
    /// <summary>The field is excluded from submission.</summary>
    NoExport = 1 << 2
}
