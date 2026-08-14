namespace BlackFlag.Engine.Document.Forms;

/// <summary>Identifies the logical type of an AcroForm field.</summary>
public enum PdfFormFieldType
{
    /// <summary>Text field.</summary>
    Text = 0,
    /// <summary>Button field, including push buttons, check boxes, and radio buttons.</summary>
    Button = 1,
    /// <summary>Choice field, including list boxes and combo boxes.</summary>
    Choice = 2,
    /// <summary>Signature field.</summary>
    Signature = 3
}
