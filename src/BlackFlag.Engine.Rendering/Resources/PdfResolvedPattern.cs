using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Represents a resolved PDF pattern resource.
/// </summary>
public sealed class PdfResolvedPattern : PdfResolvedResource
{
    internal PdfResolvedPattern(
        PdfResourceIdentity identity,
        PdfObject source,
        int patternType)
        : base(PdfResourceKind.Pattern, identity, source)
    {
        PatternType = patternType;
    }

    /// <summary>Gets the PDF PatternType value.</summary>
    public int PatternType { get; }
}
