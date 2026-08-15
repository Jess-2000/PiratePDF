using BlackFlag.Engine.Document.Decorations;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Decorations;

/// <summary>Represents a stable editing-session handle for one logical page decoration.</summary>
public sealed class PdfEditablePageDecoration
{
    private readonly PdfPageDecoration decoration;

    internal PdfEditablePageDecoration(
        PdfPageDecoration decoration,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(decoration);
        this.decoration = decoration;
        Owner = owner;
    }

    public Guid Id => decoration.Id;
    public PdfPageDecorationKind Kind => decoration.Kind;
    public PdfPageDecorationRegion Region => decoration.Region;
    public PdfPageDecorationAlignment Alignment => decoration.Alignment;
    public PdfPageDecorationTarget Target => decoration.Target;
    public string? Text => decoration.Text;
    public PdfPageNumberingOptions? Numbering => decoration.Numbering;

    internal PdfEditingOwnershipToken Owner { get; }
    internal PdfPageDecoration GetSourceDecoration() => decoration;
}
