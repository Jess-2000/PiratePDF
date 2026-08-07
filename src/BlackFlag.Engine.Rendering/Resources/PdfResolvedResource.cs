using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Provides the immutable base representation shared by resolved PDF
/// resources.
/// </summary>
public abstract class PdfResolvedResource
{
    /// <summary>
    /// Initializes a resolved PDF resource.
    /// </summary>
    protected PdfResolvedResource(
        PdfResourceKind kind,
        PdfResourceIdentity identity,
        PdfObject source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Kind = kind;
        Identity = identity;
        Source = source;
    }

    /// <summary>Gets the canonical resource category.</summary>
    public PdfResourceKind Kind { get; }

    /// <summary>Gets the canonical resource identity.</summary>
    public PdfResourceIdentity Identity { get; }

    /// <summary>Gets the immutable PDF object from which the resource was resolved.</summary>
    public PdfObject Source { get; }
}
