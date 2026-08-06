using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;

namespace BlackFlag.Engine.Parsing.Documents;

/// <summary>Loads a validated page tree and resolves effective page attributes.</summary>
public sealed class PdfEffectivePageTreeLoader
{
    private static readonly PdfName TypeKey = new("Type");
    private static readonly PdfName PagesName = new("Pages");
    private static readonly PdfName PageName = new("Page");
    private static readonly PdfName KidsKey = new("Kids");
    private static readonly PdfName MediaBoxKey = new("MediaBox");
    private static readonly PdfName CropBoxKey = new("CropBox");
    private static readonly PdfName BleedBoxKey = new("BleedBox");
    private static readonly PdfName TrimBoxKey = new("TrimBox");
    private static readonly PdfName ArtBoxKey = new("ArtBox");
    private static readonly PdfName RotateKey = new("Rotate");
    private static readonly PdfName ResourcesKey = new("Resources");
    private static readonly PdfName UserUnitKey = new("UserUnit");

    private readonly IPdfIndirectObjectResolver resolver;
    private readonly PdfPageTreeLoadOptions options;

    public PdfEffectivePageTreeLoader(
        IPdfIndirectObjectResolver resolver,
        PdfPageTreeLoadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        this.resolver = resolver;
        this.options = options ?? new PdfPageTreeLoadOptions();
    }

    public PdfEffectivePageCollection Load(PdfObjectId rootPagesId)
    {
        var pages = new List<PdfEffectivePage>();
        var active = new HashSet<PdfObjectId>();
        var visitedPages = new HashSet<PdfObjectId>();
        Traverse(rootPagesId, PageInheritance.Empty, 0, pages, active, visitedPages);
        return new PdfEffectivePageCollection(pages);
    }

    private void Traverse(
        PdfObjectId objectId,
        PageInheritance inherited,
        int depth,
        List<PdfEffectivePage> pages,
        HashSet<PdfObjectId> active,
        HashSet<PdfObjectId> visitedPages)
    {
        if (depth > options.MaximumDepth)
        {
            throw new PdfPageTreeLoadException(
                $"The page tree exceeds the maximum depth of {options.MaximumDepth}.");
        }

        if (!active.Add(objectId))
        {
            throw new PdfPageTreeLoadException(
                $"The page tree contains a cycle at object {objectId}.");
        }

        try
        {
            PdfDictionary dictionary = ResolveDictionary(objectId);
            PdfName type = GetRequiredName(dictionary, TypeKey, objectId);

            if (type == PagesName)
            {
                PageInheritance next = inherited.Apply(dictionary, resolver);
                PdfArray kids = GetRequiredArray(dictionary, KidsKey, objectId);
                foreach (PdfObject child in kids)
                {
                    PdfObjectId childId = GetReferenceId(child, objectId);
                    Traverse(childId, next, depth + 1, pages, active, visitedPages);
                }
                return;
            }

            if (type != PageName)
            {
                throw new PdfPageTreeLoadException(
                    $"Object {objectId} has unsupported page-tree type /{type.Value}.");
            }

            if (!visitedPages.Add(objectId))
            {
                throw new PdfPageTreeLoadException(
                    $"Page object {objectId} occurs more than once in the page tree.");
            }

            if (pages.Count >= options.MaximumPages)
            {
                throw new PdfPageTreeLoadException(
                    $"The page tree exceeds the maximum page count of {options.MaximumPages}.");
            }

            PageInheritance effective = inherited.Apply(dictionary, resolver);
            pages.Add(CreatePage(objectId, dictionary, effective));
        }
        finally
        {
            active.Remove(objectId);
        }
    }

    private static PdfEffectivePage CreatePage(
        PdfObjectId objectId,
        PdfDictionary dictionary,
        PageInheritance values)
    {
        PdfEffectiveValue<PdfPageBox> media = values.MediaBox
            ?? throw new PdfPageTreeLoadException(
                $"Page {objectId} has no effective /MediaBox.");
        PdfEffectiveValue<PdfPageBox> crop = values.CropBox
            ?? new PdfEffectiveValue<PdfPageBox>(media.Value, PdfPageValueOrigin.Defaulted);
        PdfEffectiveValue<PdfPageBox> bleed = ReadBox(dictionary, BleedBoxKey)
            ?? new PdfEffectiveValue<PdfPageBox>(crop.Value, PdfPageValueOrigin.Defaulted);
        PdfEffectiveValue<PdfPageBox> trim = ReadBox(dictionary, TrimBoxKey)
            ?? new PdfEffectiveValue<PdfPageBox>(crop.Value, PdfPageValueOrigin.Defaulted);
        PdfEffectiveValue<PdfPageBox> art = ReadBox(dictionary, ArtBoxKey)
            ?? new PdfEffectiveValue<PdfPageBox>(crop.Value, PdfPageValueOrigin.Defaulted);
        PdfEffectiveValue<int> rotation = values.Rotation
            ?? new PdfEffectiveValue<int>(0, PdfPageValueOrigin.Defaulted);
        PdfEffectiveValue<double> userUnit = ReadNumber(dictionary, UserUnitKey)
            is double unit
                ? new PdfEffectiveValue<double>(unit, PdfPageValueOrigin.Explicit)
                : new PdfEffectiveValue<double>(1, PdfPageValueOrigin.Defaulted);
        PdfEffectiveValue<PdfDictionary> resources = values.Resources
            ?? new PdfEffectiveValue<PdfDictionary>(
                PdfDictionary.Empty,
                PdfPageValueOrigin.Defaulted);

        return new PdfEffectivePage(
            objectId.ObjectNumber,
            objectId.GenerationNumber,
            dictionary,
            new PdfPageGeometry(media, crop, bleed, trim, art, rotation, userUnit),
            resources);
    }

    private PdfDictionary ResolveDictionary(PdfObjectId id) =>
        resolver.Resolve(id) as PdfDictionary
        ?? throw new PdfPageTreeLoadException(
            $"Page-tree object {id} must resolve to a dictionary.");

    private static PdfObjectId GetReferenceId(PdfObject value, PdfObjectId owner) =>
        value is PdfIndirectReference reference
            ? new PdfObjectId(reference.ObjectNumber, reference.GenerationNumber)
            : throw new PdfPageTreeLoadException(
                $"The /Kids array on object {owner} must contain indirect references.");

    private static PdfName GetRequiredName(
        PdfDictionary dictionary,
        PdfName key,
        PdfObjectId owner) =>
        dictionary.TryGetValue(key, out PdfObject value) && value is PdfName name
            ? name
            : throw new PdfPageTreeLoadException(
                $"Object {owner} requires the name /{key.Value}.");

    private static PdfArray GetRequiredArray(
        PdfDictionary dictionary,
        PdfName key,
        PdfObjectId owner) =>
        dictionary.TryGetValue(key, out PdfObject value) && value is PdfArray array
            ? array
            : throw new PdfPageTreeLoadException(
                $"Object {owner} requires the array /{key.Value}.");

    private static PdfEffectiveValue<PdfPageBox>? ReadBox(
        PdfDictionary dictionary,
        PdfName key)
    {
        if (!dictionary.TryGetValue(key, out PdfObject value))
        {
            return null;
        }

        if (value is not PdfArray array || array.Count != 4)
        {
            throw new PdfPageTreeLoadException(
                $"/{key.Value} must be an array of four numbers.");
        }

        return new PdfEffectiveValue<PdfPageBox>(
            new PdfPageBox(
                GetNumber(array[0], key),
                GetNumber(array[1], key),
                GetNumber(array[2], key),
                GetNumber(array[3], key)),
            PdfPageValueOrigin.Explicit);
    }

    private static double? ReadNumber(PdfDictionary dictionary, PdfName key)
    {
        if (!dictionary.TryGetValue(key, out PdfObject value))
        {
            return null;
        }

        return GetNumber(value, key);
    }

    private static double GetNumber(PdfObject value, PdfName key) => value switch
    {
        PdfInteger integer => integer.Value,
        PdfReal real => real.Value,
        _ => throw new PdfPageTreeLoadException(
            $"/{key.Value} must contain PDF numbers.")
    };

    private sealed record PageInheritance(
        PdfEffectiveValue<PdfPageBox>? MediaBox,
        PdfEffectiveValue<PdfPageBox>? CropBox,
        PdfEffectiveValue<int>? Rotation,
        PdfEffectiveValue<PdfDictionary>? Resources)
    {
        public static PageInheritance Empty { get; } = new(null, null, null, null);

        public PageInheritance Apply(
            PdfDictionary dictionary,
            IPdfIndirectObjectResolver objectResolver)
        {
            PdfEffectiveValue<PdfPageBox>? media = ReadBox(dictionary, MediaBoxKey)
                ?? Inherit(MediaBox);
            PdfEffectiveValue<PdfPageBox>? crop = ReadBox(dictionary, CropBoxKey)
                ?? Inherit(CropBox);
            PdfEffectiveValue<int>? rotation = ReadNumber(dictionary, RotateKey)
                is double angle
                    ? new PdfEffectiveValue<int>(checked((int)angle), PdfPageValueOrigin.Explicit)
                    : Inherit(Rotation);
            PdfEffectiveValue<PdfDictionary>? resources =
                ReadResources(dictionary, objectResolver)
                ?? Inherit(Resources);
            return new PageInheritance(media, crop, rotation, resources);
        }

        private static PdfEffectiveValue<T>? Inherit<T>(PdfEffectiveValue<T>? value) =>
            value is PdfEffectiveValue<T> present
                ? new PdfEffectiveValue<T>(present.Value, PdfPageValueOrigin.Inherited)
                : null;

        private static PdfEffectiveValue<PdfDictionary>? ReadResources(
            PdfDictionary dictionary,
            IPdfIndirectObjectResolver objectResolver)
        {
            if (!dictionary.TryGetValue(ResourcesKey, out PdfObject value))
            {
                return null;
            }

            PdfDictionary resources = value switch
            {
                PdfDictionary direct => direct,
                PdfIndirectReference reference =>
                    objectResolver.Resolve(new PdfObjectId(
                        reference.ObjectNumber,
                        reference.GenerationNumber)) as PdfDictionary
                    ?? throw new PdfPageTreeLoadException(
                        "An indirect /Resources value must resolve to a dictionary."),
                _ => throw new PdfPageTreeLoadException(
                    "/Resources must be a dictionary or an indirect reference.")
            };

            return new PdfEffectiveValue<PdfDictionary>(
                resources,
                PdfPageValueOrigin.Explicit);
        }
    }
}
