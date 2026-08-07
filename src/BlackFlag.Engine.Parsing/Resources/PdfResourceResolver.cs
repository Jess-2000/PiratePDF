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
public sealed class PdfResourceResolver
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

    private PdfResolvedFont CreateFont(
        PdfResourceIdentity identity,
        PdfObject source)
    {
        PdfDictionary dictionary = RequireDictionary(source, PdfResourceKind.Font);
        string subtype = RequireName(dictionary, "Subtype", PdfResourceKind.Font);
        string baseFont = GetOptionalName(dictionary, "BaseFont") ?? subtype;
        string faceName = RemoveSubsetPrefix(baseFont, out bool isSubset);

        PdfDictionary? descriptor = ResolveOptionalDictionary(dictionary, "FontDescriptor");
        string familyName = GetOptionalString(descriptor, "FontFamily") ?? faceName;
        int weight = GetFontWeight(descriptor, faceName);
        int stretch = GetFontStretch(descriptor);
        PdfFontSlant slant = GetFontSlant(descriptor, faceName);
        PdfFontTechnology technology = GetFontTechnology(subtype, descriptor);
        PdfObject? encoding = GetOptionalRaw(dictionary, "Encoding");
        PdfStream? toUnicode = ResolveOptionalStream(dictionary, "ToUnicode");
        PdfArray? descendantFonts = ResolveOptionalArray(dictionary, "DescendantFonts");

        if (descendantFonts is not null && descendantFonts.Count > options.MaximumFontDescendantCount)
        {
            throw CreateException(
                PdfResourceResolutionFailure.LimitExceeded,
                PdfResourceKind.Font,
                resourceName: null,
                $"Font descendant count {descendantFonts.Count} exceeds the configured maximum of "
                + $"{options.MaximumFontDescendantCount}.");
        }

        PdfStream? embeddedFont = ResolveEmbeddedFontProgram(descriptor);
        if (embeddedFont is not null && embeddedFont.Data.Length > options.MaximumEmbeddedFontBytes)
        {
            throw CreateException(
                PdfResourceResolutionFailure.LimitExceeded,
                PdfResourceKind.Font,
                resourceName: null,
                $"Embedded font stream size {embeddedFont.Data.Length} exceeds the configured maximum of "
                + $"{options.MaximumEmbeddedFontBytes} bytes.");
        }

        return new PdfResolvedFont(
            identity,
            dictionary,
            subtype,
            familyName,
            faceName,
            weight,
            stretch,
            slant,
            technology,
            PdfFontEmbeddingRights.Unknown,
            isSubset,
            encoding,
            toUnicode,
            descriptor,
            descendantFonts,
            embeddedFont,
            Array.Empty<PdfFontVariationAxis>());
    }

    private PdfResolvedXObject CreateXObject(
        PdfResourceIdentity identity,
        PdfObject source,
        PdfResourceScope? parent)
    {
        if (source is not PdfStream stream)
        {
            throw CreateException(
                PdfResourceResolutionFailure.InvalidXObject,
                PdfResourceKind.XObject,
                resourceName: null,
                "An XObject resource must resolve to a PDF stream.");
        }

        string subtype = RequireName(stream.Dictionary, "Subtype", PdfResourceKind.XObject);
        return subtype switch
        {
            "Image" => CreateImageXObject(identity, stream),
            "Form" => CreateFormXObject(identity, stream, parent),
            _ => throw CreateException(
                PdfResourceResolutionFailure.UnsupportedResource,
                PdfResourceKind.XObject,
                resourceName: null,
                $"XObject subtype /{subtype} is not supported by BF-1046.")
        };
    }

    private PdfResolvedImageXObject CreateImageXObject(
        PdfResourceIdentity identity,
        PdfStream stream)
    {
        if (stream.Data.Length > options.MaximumImageEncodedBytes)
        {
            throw CreateException(
                PdfResourceResolutionFailure.LimitExceeded,
                PdfResourceKind.XObject,
                resourceName: null,
                $"Image stream size {stream.Data.Length} exceeds the configured maximum of "
                + $"{options.MaximumImageEncodedBytes} bytes.");
        }

        int width = RequirePositiveInteger(stream.Dictionary, "Width", PdfResourceKind.XObject);
        int height = RequirePositiveInteger(stream.Dictionary, "Height", PdfResourceKind.XObject);
        bool imageMask = GetOptionalBoolean(stream.Dictionary, "ImageMask") ?? false;
        int? bitsPerComponent = GetOptionalInteger(stream.Dictionary, "BitsPerComponent");
        if (imageMask && bitsPerComponent is null)
        {
            bitsPerComponent = 1;
        }

        if (!imageMask && bitsPerComponent is null)
        {
            throw CreateException(
                PdfResourceResolutionFailure.InvalidXObject,
                PdfResourceKind.XObject,
                resourceName: null,
                "An image XObject must define /BitsPerComponent unless /ImageMask is true.");
        }

        return new PdfResolvedImageXObject(
            identity,
            stream,
            width,
            height,
            bitsPerComponent,
            imageMask,
            GetOptionalBoolean(stream.Dictionary, "Interpolate") ?? false,
            GetOptionalRaw(stream.Dictionary, "ColorSpace"),
            GetOptionalRaw(stream.Dictionary, "Decode"),
            GetOptionalRaw(stream.Dictionary, "Mask"),
            GetOptionalRaw(stream.Dictionary, "SMask"),
            GetOptionalRaw(stream.Dictionary, "Filter"));
    }

    private PdfResolvedFormXObject CreateFormXObject(
        PdfResourceIdentity identity,
        PdfStream stream,
        PdfResourceScope? parent)
    {
        PdfArray boundingBox = ResolveRequiredArray(stream.Dictionary, "BBox", PdfResourceKind.XObject);
        PdfArray? matrix = ResolveOptionalArray(stream.Dictionary, "Matrix");
        PdfDictionary? group = ResolveOptionalDictionary(stream.Dictionary, "Group");
        PdfObject? resourcesObject = GetOptionalRaw(stream.Dictionary, "Resources");

        var resourceScope = new Lazy<PdfResourceScope?>(
            () =>
            {
                if (resourcesObject is null)
                {
                    return parent;
                }

                PdfObject resolved = Dereference(resourcesObject);
                if (resolved is not PdfDictionary formResources)
                {
                    throw CreateException(
                        PdfResourceResolutionFailure.WrongResourceType,
                        PdfResourceKind.XObject,
                        resourceName: null,
                        "A Form XObject /Resources entry must resolve to a dictionary.");
                }

                return CreateScope(formResources, parent);
            },
            LazyThreadSafetyMode.ExecutionAndPublication);

        return new PdfResolvedFormXObject(
            identity,
            stream,
            boundingBox,
            matrix,
            resourceScope,
            group);
    }

    private PdfResolvedColorSpace CreateColorSpace(
        PdfResourceIdentity identity,
        PdfObject source)
    {
        PdfObject resolved = Dereference(source);

        if (resolved is PdfName name)
        {
            return CreateNamedColorSpace(identity, resolved, name.Value);
        }

        if (resolved is not PdfArray array || array.Count == 0 || array[0] is not PdfName family)
        {
            throw CreateException(
                PdfResourceResolutionFailure.InvalidColorSpace,
                PdfResourceKind.ColorSpace,
                resourceName: null,
                "A color-space resource must be a name or a non-empty array beginning with a name.");
        }

        return family.Value switch
        {
            "CalGray" => CreateSimpleColorSpace(identity, resolved, PdfColorSpaceKind.CalGray, 1),
            "CalRGB" => CreateSimpleColorSpace(identity, resolved, PdfColorSpaceKind.CalRgb, 3),
            "Lab" => CreateSimpleColorSpace(identity, resolved, PdfColorSpaceKind.Lab, 3),
            "Indexed" => CreateSimpleColorSpace(identity, resolved, PdfColorSpaceKind.Indexed, 1),
            "Pattern" => CreateSimpleColorSpace(identity, resolved, PdfColorSpaceKind.Pattern, 0),
            "ICCBased" => CreateIccBasedColorSpace(identity, resolved, array),
            "Separation" => CreateSeparationColorSpace(identity, resolved, array),
            "DeviceN" => CreateDeviceNColorSpace(identity, resolved, array),
            _ => throw CreateException(
                PdfResourceResolutionFailure.UnsupportedResource,
                PdfResourceKind.ColorSpace,
                resourceName: null,
                $"Color-space family /{family.Value} is not supported by BF-1046.")
        };
    }

    private static PdfResolvedColorSpace CreateNamedColorSpace(
        PdfResourceIdentity identity,
        PdfObject source,
        string name) =>
        name switch
        {
            "DeviceGray" or "G" => CreateSimpleColorSpace(identity, source, PdfColorSpaceKind.DeviceGray, 1),
            "DeviceRGB" or "RGB" => CreateSimpleColorSpace(identity, source, PdfColorSpaceKind.DeviceRgb, 3),
            "DeviceCMYK" or "CMYK" => CreateSimpleColorSpace(identity, source, PdfColorSpaceKind.DeviceCmyk, 4),
            "Pattern" => CreateSimpleColorSpace(identity, source, PdfColorSpaceKind.Pattern, 0),
            _ => new PdfResolvedColorSpace(
                identity,
                source,
                PdfColorSpaceKind.Unknown,
                componentCount: 0,
                Array.Empty<string>(),
                iccProfile: null,
                alternateSpace: null,
                tintTransform: null)
        };

    private static PdfResolvedColorSpace CreateSimpleColorSpace(
        PdfResourceIdentity identity,
        PdfObject source,
        PdfColorSpaceKind kind,
        int components) =>
        new(
            identity,
            source,
            kind,
            components,
            Array.Empty<string>(),
            iccProfile: null,
            alternateSpace: null,
            tintTransform: null);

    private PdfResolvedColorSpace CreateIccBasedColorSpace(
        PdfResourceIdentity identity,
        PdfObject source,
        PdfArray array)
    {
        if (array.Count < 2)
        {
            throw InvalidColorSpace("An ICCBased color space requires an ICC profile stream.");
        }

        PdfObject profileObject = Dereference(array[1]);
        if (profileObject is not PdfStream profile)
        {
            throw InvalidColorSpace("An ICCBased profile must resolve to a stream.");
        }

        if (profile.Data.Length > options.MaximumIccProfileBytes)
        {
            throw CreateException(
                PdfResourceResolutionFailure.LimitExceeded,
                PdfResourceKind.ColorSpace,
                resourceName: null,
                $"ICC profile size {profile.Data.Length} exceeds the configured maximum of "
                + $"{options.MaximumIccProfileBytes} bytes.");
        }

        int components = RequirePositiveInteger(profile.Dictionary, "N", PdfResourceKind.ColorSpace);
        PdfObject? alternate = GetOptionalRaw(profile.Dictionary, "Alternate");
        return new PdfResolvedColorSpace(
            identity,
            source,
            PdfColorSpaceKind.IccBased,
            components,
            Array.Empty<string>(),
            profile,
            alternate,
            tintTransform: null);
    }

    private static PdfResolvedColorSpace CreateSeparationColorSpace(
        PdfResourceIdentity identity,
        PdfObject source,
        PdfArray array)
    {
        if (array.Count < 4 || array[1] is not PdfName colorant)
        {
            throw InvalidColorSpace("A Separation color space requires a colorant name, alternate space, and tint transform.");
        }

        return new PdfResolvedColorSpace(
            identity,
            source,
            PdfColorSpaceKind.Separation,
            componentCount: 1,
            new[] { colorant.Value },
            iccProfile: null,
            array[2],
            array[3]);
    }

    private static PdfResolvedColorSpace CreateDeviceNColorSpace(
        PdfResourceIdentity identity,
        PdfObject source,
        PdfArray array)
    {
        if (array.Count < 4 || array[1] is not PdfArray names)
        {
            throw InvalidColorSpace("A DeviceN color space requires a colorant-name array, alternate space, and tint transform.");
        }

        var colorants = new string[names.Count];
        for (int index = 0; index < names.Count; index++)
        {
            if (names[index] is not PdfName colorant)
            {
                throw InvalidColorSpace("Every DeviceN colorant must be a PDF name.");
            }

            colorants[index] = colorant.Value;
        }

        return new PdfResolvedColorSpace(
            identity,
            source,
            PdfColorSpaceKind.DeviceN,
            names.Count,
            colorants,
            iccProfile: null,
            array[2],
            array[3]);
    }

    private static PdfResolvedExtGState CreateExtGState(
        PdfResourceIdentity identity,
        PdfObject source)
    {
        PdfDictionary dictionary = RequireDictionary(source, PdfResourceKind.ExtGState);
        return new PdfResolvedExtGState(
            identity,
            dictionary,
            GetOptionalNumber(dictionary, "CA"),
            GetOptionalNumber(dictionary, "ca"),
            GetOptionalBoolean(dictionary, "OP"),
            GetOptionalBoolean(dictionary, "op"),
            GetOptionalInteger(dictionary, "OPM"),
            GetOptionalRaw(dictionary, "BM"),
            GetOptionalRaw(dictionary, "SMask"));
    }

    private static PdfResolvedPattern CreatePattern(
        PdfResourceIdentity identity,
        PdfObject source)
    {
        PdfDictionary dictionary = GetDictionaryFromDictionaryOrStream(source, PdfResourceKind.Pattern);
        int patternType = RequirePositiveInteger(dictionary, "PatternType", PdfResourceKind.Pattern);
        if (patternType is not 1 and not 2)
        {
            throw CreateException(
                PdfResourceResolutionFailure.UnsupportedResource,
                PdfResourceKind.Pattern,
                resourceName: null,
                $"PatternType {patternType} is not supported.");
        }

        return new PdfResolvedPattern(identity, source, patternType);
    }

    private static PdfResolvedShading CreateShading(
        PdfResourceIdentity identity,
        PdfObject source)
    {
        PdfDictionary dictionary = GetDictionaryFromDictionaryOrStream(source, PdfResourceKind.Shading);
        int shadingType = RequirePositiveInteger(dictionary, "ShadingType", PdfResourceKind.Shading);
        if (shadingType is < 1 or > 7)
        {
            throw CreateException(
                PdfResourceResolutionFailure.UnsupportedResource,
                PdfResourceKind.Shading,
                resourceName: null,
                $"ShadingType {shadingType} is outside the supported range from 1 through 7.");
        }

        return new PdfResolvedShading(
            identity,
            source,
            shadingType,
            GetOptionalRaw(dictionary, "ColorSpace"),
            GetOptionalRaw(dictionary, "Function"));
    }

    private static PdfResolvedProperties CreateProperties(
        PdfResourceIdentity identity,
        PdfObject source) =>
        new(identity, RequireDictionary(source, PdfResourceKind.Properties));

    private PdfObject Dereference(PdfObject value)
    {
        if (value is not PdfIndirectReference reference)
        {
            return value;
        }

        return objectResolver.Resolve(
            new PdfObjectId(reference.ObjectNumber, reference.GenerationNumber));
    }

    private PdfDictionary? ResolveOptionalDictionary(PdfDictionary dictionary, string key)
    {
        PdfObject? value = GetOptionalRaw(dictionary, key);
        if (value is null)
        {
            return null;
        }

        return Dereference(value) as PdfDictionary
            ?? throw new InvalidOperationException($"/{key} must resolve to a dictionary.");
    }

    private PdfStream? ResolveOptionalStream(PdfDictionary dictionary, string key)
    {
        PdfObject? value = GetOptionalRaw(dictionary, key);
        if (value is null)
        {
            return null;
        }

        return Dereference(value) as PdfStream
            ?? throw new InvalidOperationException($"/{key} must resolve to a stream.");
    }

    private PdfArray? ResolveOptionalArray(PdfDictionary dictionary, string key)
    {
        PdfObject? value = GetOptionalRaw(dictionary, key);
        if (value is null)
        {
            return null;
        }

        return Dereference(value) as PdfArray
            ?? throw new InvalidOperationException($"/{key} must resolve to an array.");
    }

    private PdfArray ResolveRequiredArray(
        PdfDictionary dictionary,
        string key,
        PdfResourceKind kind) =>
        ResolveOptionalArray(dictionary, key)
        ?? throw CreateException(
            PdfResourceResolutionFailure.MalformedResource,
            kind,
            resourceName: null,
            $"Resource dictionary is missing required /{key} array.");

    private PdfStream? ResolveEmbeddedFontProgram(PdfDictionary? descriptor)
    {
        if (descriptor is null)
        {
            return null;
        }

        return ResolveOptionalStream(descriptor, "FontFile")
            ?? ResolveOptionalStream(descriptor, "FontFile2")
            ?? ResolveOptionalStream(descriptor, "FontFile3");
    }

    private static PdfDictionary RequireDictionary(PdfObject source, PdfResourceKind kind) =>
        source as PdfDictionary
        ?? throw CreateException(
            PdfResourceResolutionFailure.WrongResourceType,
            kind,
            resourceName: null,
            $"A {kind} resource must resolve to a PDF dictionary.");

    private static PdfDictionary GetDictionaryFromDictionaryOrStream(PdfObject source, PdfResourceKind kind) =>
        source switch
        {
            PdfDictionary dictionary => dictionary,
            PdfStream stream => stream.Dictionary,
            _ => throw CreateException(
                PdfResourceResolutionFailure.WrongResourceType,
                kind,
                resourceName: null,
                $"A {kind} resource must resolve to a dictionary or stream.")
        };

    private static string RequireName(PdfDictionary dictionary, string key, PdfResourceKind kind) =>
        GetOptionalName(dictionary, key)
        ?? throw CreateException(
            PdfResourceResolutionFailure.MalformedResource,
            kind,
            resourceName: null,
            $"Resource dictionary is missing required /{key} name.");

    private static string? GetOptionalName(PdfDictionary? dictionary, string key)
    {
        if (dictionary is null || !dictionary.TryGetValue(new PdfName(key), out PdfObject value))
        {
            return null;
        }

        return value is PdfName name
            ? name.Value
            : throw new InvalidOperationException($"/{key} must be a PDF name.");
    }

    private static string? GetOptionalString(PdfDictionary? dictionary, string key)
    {
        if (dictionary is null || !dictionary.TryGetValue(new PdfName(key), out PdfObject value))
        {
            return null;
        }

        return value is PdfLiteralString text
            ? text.Value
            : null;
    }

    private static PdfObject? GetOptionalRaw(PdfDictionary dictionary, string key) =>
        dictionary.TryGetValue(new PdfName(key), out PdfObject value)
            ? value
            : null;

    private static int RequirePositiveInteger(PdfDictionary dictionary, string key, PdfResourceKind kind)
    {
        int? value = GetOptionalInteger(dictionary, key);
        if (value is null || value <= 0)
        {
            throw CreateException(
                PdfResourceResolutionFailure.MalformedResource,
                kind,
                resourceName: null,
                $"/{key} must be a positive integer.");
        }

        return value.Value;
    }

    private static int? GetOptionalInteger(PdfDictionary dictionary, string key)
    {
        if (!dictionary.TryGetValue(new PdfName(key), out PdfObject value))
        {
            return null;
        }

        if (value is not PdfInteger integer || integer.Value is < int.MinValue or > int.MaxValue)
        {
            throw new InvalidOperationException($"/{key} must be a 32-bit PDF integer.");
        }

        return checked((int)integer.Value);
    }

    private static double? GetOptionalNumber(PdfDictionary dictionary, string key)
    {
        if (!dictionary.TryGetValue(new PdfName(key), out PdfObject value))
        {
            return null;
        }

        return value switch
        {
            PdfInteger integer => integer.Value,
            PdfReal real => real.Value,
            _ => throw new InvalidOperationException($"/{key} must be numeric.")
        };
    }

    private static bool? GetOptionalBoolean(PdfDictionary dictionary, string key)
    {
        if (!dictionary.TryGetValue(new PdfName(key), out PdfObject value))
        {
            return null;
        }

        return value is PdfBoolean boolean
            ? boolean.Value
            : throw new InvalidOperationException($"/{key} must be a PDF boolean.");
    }

    private static string RemoveSubsetPrefix(string value, out bool isSubset)
    {
        isSubset = value.Length > 7
            && value[6] == '+'
            && value.AsSpan(0, 6).ToString().All(character => character is >= 'A' and <= 'Z');

        return isSubset ? value[7..] : value;
    }

    private static int GetFontWeight(PdfDictionary? descriptor, string faceName)
    {
        double? explicitWeight = descriptor is null ? null : GetOptionalNumber(descriptor, "FontWeight");
        if (explicitWeight.HasValue)
        {
            return Math.Clamp((int)Math.Round(explicitWeight.Value, MidpointRounding.AwayFromZero), 1, 1000);
        }

        if (faceName.Contains("Black", StringComparison.OrdinalIgnoreCase))
        {
            return 900;
        }

        if (faceName.Contains("Bold", StringComparison.OrdinalIgnoreCase))
        {
            return 700;
        }

        if (faceName.Contains("Light", StringComparison.OrdinalIgnoreCase))
        {
            return 300;
        }

        return 400;
    }

    private static int GetFontStretch(PdfDictionary? descriptor)
    {
        string? stretch = GetOptionalName(descriptor, "FontStretch");
        return stretch switch
        {
            "UltraCondensed" => 1,
            "ExtraCondensed" => 2,
            "Condensed" => 3,
            "SemiCondensed" => 4,
            "Normal" or null => 5,
            "SemiExpanded" => 6,
            "Expanded" => 7,
            "ExtraExpanded" => 8,
            "UltraExpanded" => 9,
            _ => 5
        };
    }

    private static PdfFontSlant GetFontSlant(PdfDictionary? descriptor, string faceName)
    {
        double italicAngle = descriptor is null ? 0 : GetOptionalNumber(descriptor, "ItalicAngle") ?? 0;
        if (faceName.Contains("Oblique", StringComparison.OrdinalIgnoreCase))
        {
            return PdfFontSlant.Oblique;
        }

        return italicAngle != 0 || faceName.Contains("Italic", StringComparison.OrdinalIgnoreCase)
            ? PdfFontSlant.Italic
            : PdfFontSlant.Upright;
    }

    private static PdfFontTechnology GetFontTechnology(string subtype, PdfDictionary? descriptor)
    {
        if (descriptor is not null)
        {
            PdfObject? fontFile3 = GetOptionalRaw(descriptor, "FontFile3");
            if (fontFile3 is PdfStream fontFile3Stream)
            {
                string? embeddedSubtype = GetOptionalName(fontFile3Stream.Dictionary, "Subtype");
                if (string.Equals(embeddedSubtype, "OpenType", StringComparison.Ordinal))
                {
                    return PdfFontTechnology.OpenType;
                }

                if (embeddedSubtype is "Type1C" or "CIDFontType0C")
                {
                    return PdfFontTechnology.Cff;
                }
            }
        }

        return subtype switch
        {
            "Type1" or "MMType1" => PdfFontTechnology.Type1,
            "TrueType" or "CIDFontType2" => PdfFontTechnology.TrueType,
            "Type0" => PdfFontTechnology.Composite,
            "Type3" => PdfFontTechnology.Type3,
            "CIDFontType0" => PdfFontTechnology.Cff,
            _ => PdfFontTechnology.Unknown
        };
    }

    private static PdfName GetCategoryKey(PdfResourceKind kind) =>
        kind switch
        {
            PdfResourceKind.Font => FontKey,
            PdfResourceKind.XObject => XObjectKey,
            PdfResourceKind.ColorSpace => ColorSpaceKey,
            PdfResourceKind.ExtGState => ExtGStateKey,
            PdfResourceKind.Pattern => PatternKey,
            PdfResourceKind.Shading => ShadingKey,
            PdfResourceKind.Properties => PropertiesKey,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported resource kind.")
        };

    private static PdfResourceResolutionException InvalidColorSpace(string message) =>
        CreateException(
            PdfResourceResolutionFailure.InvalidColorSpace,
            PdfResourceKind.ColorSpace,
            resourceName: null,
            message);

    private static PdfResourceResolutionException CreateException(
        PdfResourceResolutionFailure failure,
        PdfResourceKind kind,
        string? resourceName,
        string message,
        Exception? innerException = null) =>
        new(failure, kind, resourceName, message, innerException);

    private readonly record struct ResourceCacheKey(
        PdfResourceKind Kind,
        PdfObjectId ObjectId);

    private readonly record struct LocalResourceKey(
        PdfResourceKind Kind,
        string Name);
}


