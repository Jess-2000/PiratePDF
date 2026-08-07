using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Represents a resolved marked-content properties resource.
/// </summary>
public sealed class PdfResolvedProperties : PdfResolvedResource
{
    internal PdfResolvedProperties(
        PdfResourceIdentity identity,
        PdfDictionary dictionary)
        : base(PdfResourceKind.Properties, identity, dictionary)
    {
        Dictionary = dictionary;
    }

    /// <summary>Gets the source properties dictionary.</summary>
    public PdfDictionary Dictionary { get; }
}
