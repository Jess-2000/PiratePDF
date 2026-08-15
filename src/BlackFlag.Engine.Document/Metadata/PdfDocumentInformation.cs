namespace BlackFlag.Engine.Document.Metadata;

/// <summary>
/// Represents the authoritative logical values of the standard PDF document
/// information entries supported by the editing model.
/// </summary>
/// <remarks>
/// Absence and an explicitly present empty string are distinct states. This
/// model stores logical text; preservation-aware Writing remains responsible
/// for choosing a PDF string representation when serialization is implemented.
/// </remarks>
public sealed class PdfDocumentInformation
{
    private readonly Dictionary<PdfDocumentInformationField, string>
        values =
            [];

    /// <summary>Gets the number of explicitly present information entries.</summary>
    public int Count =>
        values.Count;

    /// <summary>Gets the document title, or <see langword="null"/> when absent.</summary>
    public string? Title =>
        GetValue(PdfDocumentInformationField.Title);

    /// <summary>Gets the document author, or <see langword="null"/> when absent.</summary>
    public string? Author =>
        GetValue(PdfDocumentInformationField.Author);

    /// <summary>Gets the document subject, or <see langword="null"/> when absent.</summary>
    public string? Subject =>
        GetValue(PdfDocumentInformationField.Subject);

    /// <summary>Gets the document keywords, or <see langword="null"/> when absent.</summary>
    public string? Keywords =>
        GetValue(PdfDocumentInformationField.Keywords);

    /// <summary>Gets the creating application name, or <see langword="null"/> when absent.</summary>
    public string? Creator =>
        GetValue(PdfDocumentInformationField.Creator);

    /// <summary>Gets the producing application name, or <see langword="null"/> when absent.</summary>
    public string? Producer =>
        GetValue(PdfDocumentInformationField.Producer);

    /// <summary>Gets the PDF creation-date text, or <see langword="null"/> when absent.</summary>
    public string? CreationDate =>
        GetValue(PdfDocumentInformationField.CreationDate);

    /// <summary>Gets the PDF modification-date text, or <see langword="null"/> when absent.</summary>
    public string? ModificationDate =>
        GetValue(PdfDocumentInformationField.ModificationDate);

    /// <summary>Gets whether the requested information entry is explicitly present.</summary>
    public bool Contains(
        PdfDocumentInformationField field)
    {
        ValidateField(field);
        return values.ContainsKey(field);
    }

    /// <summary>Gets one information value, or <see langword="null"/> when absent.</summary>
    public string? GetValue(
        PdfDocumentInformationField field)
    {
        ValidateField(field);
        return values.TryGetValue(field, out var value)
            ? value
            : null;
    }

    internal void SetValue(
        PdfDocumentInformationField field,
        string value)
    {
        ValidateField(field);
        ArgumentNullException.ThrowIfNull(value);
        values[field] = value;
    }

    internal bool Remove(
        PdfDocumentInformationField field)
    {
        ValidateField(field);
        return values.Remove(field);
    }

    private static void ValidateField(
        PdfDocumentInformationField field)
    {
        if (!Enum.IsDefined(field))
        {
            throw new ArgumentOutOfRangeException(
                nameof(field),
                field,
                "The document information field is not supported.");
        }
    }
}
