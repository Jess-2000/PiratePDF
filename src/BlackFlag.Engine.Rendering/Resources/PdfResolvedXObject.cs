using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Provides the immutable base representation for resolved PDF XObjects.
/// </summary>
public abstract class PdfResolvedXObject : PdfResolvedResource
{
    /// <summary>Initializes a resolved XObject.</summary>
    protected PdfResolvedXObject(
        PdfResourceIdentity identity,
        PdfStream stream)
        : base(PdfResourceKind.XObject, identity, stream)
    {
        Stream = stream;
    }

    /// <summary>Gets the source XObject stream.</summary>
    public PdfStream Stream { get; }
}
