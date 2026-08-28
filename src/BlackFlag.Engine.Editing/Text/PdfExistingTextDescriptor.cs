using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Editing.Text;

/// <summary>
/// Describes one immutable, provenance-backed existing-text target discovered
/// for an editing session.
/// </summary>
public sealed class PdfExistingTextDescriptor
{
    private readonly byte[] encodedBytes;

    /// <summary>Initializes an existing-text descriptor.</summary>
    public PdfExistingTextDescriptor(
        PdfExistingTextIdentity identity,
        ReadOnlyMemory<byte> encodedBytes,
        string? unicodeText,
        string? fontResourceName,
        PdfObjectIdentifier? fontObjectIdentifier,
        double fontSize,
        PdfExistingTextEditability editability,
        string? editabilityReason = null)
    {
        if (fontResourceName is not null &&
            string.IsNullOrWhiteSpace(fontResourceName))
        {
            throw new ArgumentException(
                "A font resource name cannot be empty or whitespace.",
                nameof(fontResourceName));
        }

        if (!double.IsFinite(fontSize))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fontSize),
                fontSize,
                "Existing-text font size must be finite.");
        }

        if (!Enum.IsDefined(editability))
        {
            throw new ArgumentOutOfRangeException(
                nameof(editability),
                editability,
                "The existing-text editability state is not supported.");
        }

        if (editability != PdfExistingTextEditability.DirectlyEditable &&
            string.IsNullOrWhiteSpace(editabilityReason))
        {
            throw new ArgumentException(
                "A non-direct existing-text target requires an explicit editability reason.",
                nameof(editabilityReason));
        }

        Identity = identity;
        this.encodedBytes = encodedBytes.ToArray();
        UnicodeText = unicodeText;
        FontResourceName = fontResourceName;
        FontObjectIdentifier = fontObjectIdentifier;
        FontSize = fontSize;
        Editability = editability;
        EditabilityReason = editabilityReason;
    }

    /// <summary>Gets the provenance-backed identity of the text target.</summary>
    public PdfExistingTextIdentity Identity { get; }

    /// <summary>Gets a defensive immutable view of the encoded source text bytes.</summary>
    public ReadOnlyMemory<byte> EncodedBytes => encodedBytes;

    /// <summary>Gets directly derivable Unicode text when available.</summary>
    public string? UnicodeText { get; }

    /// <summary>Gets the source font resource name when known.</summary>
    public string? FontResourceName { get; }

    /// <summary>Gets the indirect source font object when one is known.</summary>
    public PdfObjectIdentifier? FontObjectIdentifier { get; }

    /// <summary>Gets the interpreted font size associated with the target.</summary>
    public double FontSize { get; }

    /// <summary>Gets the current editability classification.</summary>
    public PdfExistingTextEditability Editability { get; }

    /// <summary>Gets the explicit reason for a non-direct editability state.</summary>
    public string? EditabilityReason { get; }

    /// <summary>Gets whether the target is eligible for direct mutation.</summary>
    public bool IsDirectlyEditable =>
        Editability == PdfExistingTextEditability.DirectlyEditable;
}
