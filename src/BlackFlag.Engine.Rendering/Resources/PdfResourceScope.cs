using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Represents an immutable PDF resource scope with optional ancestor fallback.
/// </summary>
public sealed class PdfResourceScope
{
    private readonly Func<PdfResourceKind, PdfName, PdfResolvedResource?> localResolver;

    internal PdfResourceScope(
        Func<PdfResourceKind, PdfName, PdfResolvedResource?> localResolver,
        PdfResourceScope? parent)
    {
        ArgumentNullException.ThrowIfNull(localResolver);
        this.localResolver = localResolver;
        Parent = parent;
        Depth = parent is null ? 1 : checked(parent.Depth + 1);
    }

    /// <summary>Gets the parent scope used for fallback lookup.</summary>
    public PdfResourceScope? Parent { get; }

    /// <summary>Gets the one-based nesting depth of this scope.</summary>
    public int Depth { get; }

    /// <summary>
    /// Attempts to resolve a named resource in the current scope and then its
    /// ancestors.
    /// </summary>
    public bool TryResolve(
        PdfResourceKind kind,
        PdfName name,
        out PdfResolvedResource? resource)
    {
        ArgumentNullException.ThrowIfNull(name);

        resource = localResolver(kind, name);
        if (resource is not null)
        {
            return true;
        }

        return Parent is not null
            && Parent.TryResolve(kind, name, out resource);
    }

    /// <summary>
    /// Resolves a named resource or throws when no matching resource exists in
    /// the active scope chain.
    /// </summary>
    public PdfResolvedResource Resolve(
        PdfResourceKind kind,
        PdfName name)
    {
        if (TryResolve(kind, name, out PdfResolvedResource? resource))
        {
            return resource!;
        }

        throw new KeyNotFoundException(
            $"The resource '/{name.Value}' was not found in the {kind} category.");
    }

    /// <summary>Resolves a font resource by name.</summary>
    public PdfResolvedFont ResolveFont(PdfName name) =>
        (PdfResolvedFont)Resolve(PdfResourceKind.Font, name);

    /// <summary>Resolves an XObject resource by name.</summary>
    public PdfResolvedXObject ResolveXObject(PdfName name) =>
        (PdfResolvedXObject)Resolve(PdfResourceKind.XObject, name);

    /// <summary>Resolves a color-space resource by name.</summary>
    public PdfResolvedColorSpace ResolveColorSpace(PdfName name) =>
        (PdfResolvedColorSpace)Resolve(PdfResourceKind.ColorSpace, name);

    /// <summary>Resolves an extended graphics-state resource by name.</summary>
    public PdfResolvedExtGState ResolveExtGState(PdfName name) =>
        (PdfResolvedExtGState)Resolve(PdfResourceKind.ExtGState, name);

    /// <summary>Resolves a pattern resource by name.</summary>
    public PdfResolvedPattern ResolvePattern(PdfName name) =>
        (PdfResolvedPattern)Resolve(PdfResourceKind.Pattern, name);

    /// <summary>Resolves a shading resource by name.</summary>
    public PdfResolvedShading ResolveShading(PdfName name) =>
        (PdfResolvedShading)Resolve(PdfResourceKind.Shading, name);

    /// <summary>Resolves a marked-content properties resource by name.</summary>
    public PdfResolvedProperties ResolveProperties(PdfName name) =>
        (PdfResolvedProperties)Resolve(PdfResourceKind.Properties, name);
}
