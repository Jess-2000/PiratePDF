using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Represents a resolved Form XObject with lazily constructed nested resource
/// scope.
/// </summary>
public sealed class PdfResolvedFormXObject : PdfResolvedXObject
{
    private readonly Lazy<PdfResourceScope?> resourceScope;

    internal PdfResolvedFormXObject(
        PdfResourceIdentity identity,
        PdfStream stream,
        PdfArray boundingBox,
        PdfArray? matrix,
        Lazy<PdfResourceScope?> resourceScope,
        PdfDictionary? group)
        : base(identity, stream)
    {
        BoundingBox = boundingBox;
        Matrix = matrix;
        this.resourceScope = resourceScope;
        Group = group;
    }

    /// <summary>Gets the Form XObject bounding box.</summary>
    public PdfArray BoundingBox { get; }

    /// <summary>Gets the optional Form XObject transformation matrix.</summary>
    public PdfArray? Matrix { get; }

    /// <summary>
    /// Resolves and returns the nested resource scope when the form defines
    /// resources. The scope is created at most once.
    /// </summary>
    public PdfResourceScope? GetResourceScope() => resourceScope.Value;

    /// <summary>Gets the optional transparency group dictionary.</summary>
    public PdfDictionary? Group { get; }
}
