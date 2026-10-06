using System.Collections.Concurrent;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Parsing.Resources;

/// <summary>
/// Resolves named PDF resources into immutable canonical resource models with
/// scope fallback, indirect-object identity caching, and cycle protection.
/// </summary>
public sealed partial class PdfResourceResolver
{
    private static readonly PdfName FontKey = new("Font");
    private static readonly PdfName XObjectKey = new("XObject");
    private static readonly PdfName ColorSpaceKey = new("ColorSpace");
    private static readonly PdfName ExtGStateKey = new("ExtGState");
    private static readonly PdfName PatternKey = new("Pattern");
    private static readonly PdfName ShadingKey = new("Shading");
    private static readonly PdfName PropertiesKey = new("Properties");

    private readonly IPdfIndirectObjectResolver objectResolver;
    private readonly PdfResourceResolutionOptions options;
    private readonly ConcurrentDictionary<ResourceCacheKey, Lazy<PdfResolvedResource>> objectCache = new();
    private readonly ConcurrentDictionary<PdfObject, long> localIdentities =
        new(ReferenceEqualityComparer.Instance);
    private readonly AsyncLocal<HashSet<ResourceCacheKey>?> activeResolutions = new();
    private long nextLocalIdentity;

    /// <summary>
    /// Initializes a PDF resource resolver.
    /// </summary>
    public PdfResourceResolver(
        IPdfIndirectObjectResolver objectResolver,
        PdfResourceResolutionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(objectResolver);

        this.objectResolver = objectResolver;
        this.options = options ?? new PdfResourceResolutionOptions();
        this.options.Validate();
    }

    /// <summary>
    /// Creates a resource scope for an effective page or Form XObject resource
    /// dictionary.
    /// </summary>
    public PdfResourceScope CreateScope(
        PdfDictionary resources,
        PdfResourceScope? parent = null)
    {
        ArgumentNullException.ThrowIfNull(resources);

        int depth = parent is null ? 1 : checked(parent.Depth + 1);
        if (depth > options.MaximumResourceDepth)
        {
            throw CreateException(
                PdfResourceResolutionFailure.LimitExceeded,
                PdfResourceKind.Properties,
                resourceName: null,
                $"Resource scope depth {depth} exceeds the configured maximum of "
                + $"{options.MaximumResourceDepth}.");
        }

        var localCache = new ConcurrentDictionary<LocalResourceKey, Lazy<PdfResolvedResource?>>();

        return new PdfResourceScope(
            (kind, name) =>
            {
                var key = new LocalResourceKey(kind, name.Value);
                Lazy<PdfResolvedResource?> lazy = localCache.GetOrAdd(
                    key,
                    _ => new Lazy<PdfResolvedResource?>(
                        () => ResolveNamedResource(resources, kind, name, parent),
                        LazyThreadSafetyMode.ExecutionAndPublication));

                try
                {
                    return lazy.Value;
                }
                catch
                {
                    localCache.TryRemove(key, out _);
                    throw;
                }
            },
            parent);
    }

    private PdfResolvedResource? ResolveNamedResource(
        PdfDictionary resources,
        PdfResourceKind kind,
        PdfName name,
        PdfResourceScope? parent)
    {
        PdfName categoryKey = GetCategoryKey(kind);
        if (!resources.TryGetValue(categoryKey, out PdfObject categoryObject))
        {
            return null;
        }

        PdfObject resolvedCategory = Dereference(categoryObject);
        if (resolvedCategory is not PdfDictionary categoryDictionary)
        {
            throw CreateException(
                PdfResourceResolutionFailure.WrongResourceType,
                kind,
                name.Value,
                $"The /{categoryKey.Value} resource category must resolve to a dictionary.");
        }

        if (!categoryDictionary.TryGetValue(name, out PdfObject resourceObject))
        {
            return null;
        }

        return ResolveResource(kind, name, resourceObject, parent);
    }

    private PdfResolvedResource ResolveResource(
        PdfResourceKind kind,
        PdfName name,
        PdfObject resourceObject,
        PdfResourceScope? parent)
    {
        if (resourceObject is PdfIndirectReference reference)
        {
            var objectId = new PdfObjectId(
                reference.ObjectNumber,
                reference.GenerationNumber);
            var cacheKey = new ResourceCacheKey(kind, objectId);
            Lazy<PdfResolvedResource> lazy = objectCache.GetOrAdd(
                cacheKey,
                key => new Lazy<PdfResolvedResource>(
                    () => ResolveIndirectResource(key, name, parent),
                    LazyThreadSafetyMode.ExecutionAndPublication));

            try
            {
                return lazy.Value;
            }
            catch
            {
                objectCache.TryRemove(cacheKey, out _);
                throw;
            }
        }

        long localId = localIdentities.GetOrAdd(
            resourceObject,
            _ => Interlocked.Increment(ref nextLocalIdentity));
        PdfResourceIdentity identity = PdfResourceIdentity.ForLocal(localId);
        return CreateResolvedResource(kind, name, resourceObject, identity, parent);
    }

    private PdfResolvedResource ResolveIndirectResource(
        ResourceCacheKey key,
        PdfName name,
        PdfResourceScope? parent)
    {
        HashSet<ResourceCacheKey> active = activeResolutions.Value ??= [];
        if (!active.Add(key))
        {
            throw CreateException(
                PdfResourceResolutionFailure.CircularReference,
                key.Kind,
                name.Value,
                $"Circular resource resolution detected for object {key.ObjectId}.");
        }

        try
        {
            PdfObject resolved = objectResolver.Resolve(key.ObjectId);
            PdfResourceIdentity identity = PdfResourceIdentity.ForIndirect(
                key.ObjectId.ObjectNumber,
                key.ObjectId.GenerationNumber);
            return CreateResolvedResource(key.Kind, name, resolved, identity, parent);
        }
        finally
        {
            active.Remove(key);
            if (active.Count == 0)
            {
                activeResolutions.Value = null;
            }
        }
    }

    private PdfResolvedResource CreateResolvedResource(
        PdfResourceKind kind,
        PdfName name,
        PdfObject source,
        PdfResourceIdentity identity,
        PdfResourceScope? parent)
    {
        try
        {
            return kind switch
            {
                PdfResourceKind.Font => CreateFont(identity, source),
                PdfResourceKind.XObject => CreateXObject(identity, source, parent),
                PdfResourceKind.ColorSpace => CreateColorSpace(identity, source),
                PdfResourceKind.ExtGState => CreateExtGState(identity, source),
                PdfResourceKind.Pattern => CreatePattern(identity, source),
                PdfResourceKind.Shading => CreateShading(identity, source),
                PdfResourceKind.Properties => CreateProperties(identity, source),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported resource kind.")
            };
        }
        catch (PdfResourceResolutionException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            throw CreateException(
                PdfResourceResolutionFailure.MalformedResource,
                kind,
                name.Value,
                $"Resource '/{name.Value}' in category {kind} is malformed.",
                exception);
        }
    }

}
