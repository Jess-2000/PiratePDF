using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Represents immutable marked-content context attached to display items.</summary>
public sealed class PdfMarkedContentContext
{
    /// <summary>Initializes marked-content context.</summary>
    public PdfMarkedContentContext(
        PdfName tag,
        PdfObject? properties,
        PdfResolvedProperties? resolvedProperties,
        PdfMarkedContentContext? parent)
    {
        ArgumentNullException.ThrowIfNull(tag);
        Tag = tag;
        Properties = properties;
        ResolvedProperties = resolvedProperties;
        Parent = parent;
        Depth = parent is null ? 1 : checked(parent.Depth + 1);
    }

    /// <summary>Gets the marked-content tag.</summary>
    public PdfName Tag { get; }

    /// <summary>Gets inline or source property data when present.</summary>
    public PdfObject? Properties { get; }

    /// <summary>Gets resolved named properties when present.</summary>
    public PdfResolvedProperties? ResolvedProperties { get; }

    /// <summary>Gets the containing marked-content context.</summary>
    public PdfMarkedContentContext? Parent { get; }

    /// <summary>Gets the one-based nesting depth.</summary>
    public int Depth { get; }
}
