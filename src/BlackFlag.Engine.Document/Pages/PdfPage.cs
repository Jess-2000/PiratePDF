using System.Collections.ObjectModel;

using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Document.Pages;

/// <summary>
/// Represents a high-level page in a PDF document.
/// </summary>
public sealed class PdfPage :
    IPdfDictionaryBackedModel
{
    private readonly PdfObjectBinding binding =
        new();

    private byte[] contentData = [];
    private readonly PdfDictionary resources = [];
    private readonly List<PdfAnnotation> annotations = [];
    private readonly ReadOnlyCollection<PdfAnnotation> readOnlyAnnotations;
    private PdfPageTree? parent;
    private PdfEffectiveValue<PdfPageBox> mediaBox;
    private PdfEffectiveValue<PdfPageBox> cropBox;
    private PdfEffectiveValue<PdfPageBox> bleedBox;
    private PdfEffectiveValue<PdfPageBox> trimBox;
    private PdfEffectiveValue<PdfPageBox> artBox;
    private PdfEffectiveValue<PdfPageRotation> rotationState;

    public PdfPage(
        double width,
        double height,
        PdfPageRotation rotation = PdfPageRotation.None)
    {
        ValidateDimension(
            width,
            nameof(width));

        ValidateDimension(
            height,
            nameof(height));

        ValidateRotation(rotation);

        Id = Guid.NewGuid();
        readOnlyAnnotations = annotations.AsReadOnly();
        mediaBox = new PdfEffectiveValue<PdfPageBox>(
            new PdfPageBox(0, 0, width, height),
            PdfPageValueOrigin.Explicit);
        cropBox = new PdfEffectiveValue<PdfPageBox>(
            mediaBox.Value,
            PdfPageValueOrigin.Defaulted);
        bleedBox = new PdfEffectiveValue<PdfPageBox>(
            cropBox.Value,
            PdfPageValueOrigin.Defaulted);
        trimBox = new PdfEffectiveValue<PdfPageBox>(
            cropBox.Value,
            PdfPageValueOrigin.Defaulted);
        artBox = new PdfEffectiveValue<PdfPageBox>(
            cropBox.Value,
            PdfPageValueOrigin.Defaulted);
        rotationState = new PdfEffectiveValue<PdfPageRotation>(
            rotation,
            rotation == PdfPageRotation.None
                ? PdfPageValueOrigin.Defaulted
                : PdfPageValueOrigin.Explicit);
    }

    public Guid Id { get; }

    public double Width => mediaBox.Value.Width;

    public double Height => mediaBox.Value.Height;

    public PdfPageRotation Rotation => rotationState.Value;

    /// <summary>
    /// Gets whether this page currently owns content-stream data.
    /// </summary>
    public bool HasContent =>
        contentData.Length > 0;

    /// <summary>
    /// Gets whether this page currently owns any direct resource entries.
    /// </summary>
    public bool HasResources =>
        resources.Count > 0;

    /// <summary>Gets the annotations currently attached to this page.</summary>
    public IReadOnlyList<PdfAnnotation> Annotations =>
        readOnlyAnnotations;

    public bool IsLandscape
    {
        get
        {
            var swapsDimensions =
                Rotation is
                    PdfPageRotation.Clockwise90 or
                    PdfPageRotation.Clockwise270;

            return swapsDimensions
                ? Height > Width
                : Width > Height;
        }
    }

    internal PdfObjectIdentifier? ObjectIdentifier =>
        binding.Identifier;

    internal PdfPageTree? Parent =>
        parent;

    public void SetRotation(
        PdfPageRotation rotation)
    {
        ValidateRotation(rotation);
        rotationState = new PdfEffectiveValue<PdfPageRotation>(
            rotation,
            PdfPageValueOrigin.Explicit);
    }

    internal PdfEffectiveValue<PdfPageRotation> GetRotationState() =>
        rotationState;

    internal void RestoreRotationState(
        PdfEffectiveValue<PdfPageRotation> state)
    {
        ValidateRotation(state.Value);
        rotationState = state;
    }

    internal PdfEffectiveValue<PdfPageBox> GetBox(
        PdfPageBoxKind kind) => kind switch
        {
            PdfPageBoxKind.MediaBox => mediaBox,
            PdfPageBoxKind.CropBox => cropBox,
            PdfPageBoxKind.BleedBox => bleedBox,
            PdfPageBoxKind.TrimBox => trimBox,
            PdfPageBoxKind.ArtBox => artBox,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "The page-box kind is not supported.")
        };

    internal void SetBox(
        PdfPageBoxKind kind,
        PdfEffectiveValue<PdfPageBox> state)
    {
        switch (kind)
        {
            case PdfPageBoxKind.MediaBox:
                mediaBox = state;
                RefreshDefaultedGeometry();
                break;
            case PdfPageBoxKind.CropBox:
                cropBox = state;
                RefreshDefaultedProductionBoxes();
                break;
            case PdfPageBoxKind.BleedBox:
                bleedBox = state;
                break;
            case PdfPageBoxKind.TrimBox:
                trimBox = state;
                break;
            case PdfPageBoxKind.ArtBox:
                artBox = state;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "The page-box kind is not supported.");
        }
    }

    private void RefreshDefaultedGeometry()
    {
        if (cropBox.Origin == PdfPageValueOrigin.Defaulted)
        {
            cropBox = new PdfEffectiveValue<PdfPageBox>(
                mediaBox.Value,
                PdfPageValueOrigin.Defaulted);
        }

        RefreshDefaultedProductionBoxes();
    }

    private void RefreshDefaultedProductionBoxes()
    {
        if (bleedBox.Origin == PdfPageValueOrigin.Defaulted)
        {
            bleedBox = new PdfEffectiveValue<PdfPageBox>(cropBox.Value, PdfPageValueOrigin.Defaulted);
        }

        if (trimBox.Origin == PdfPageValueOrigin.Defaulted)
        {
            trimBox = new PdfEffectiveValue<PdfPageBox>(cropBox.Value, PdfPageValueOrigin.Defaulted);
        }

        if (artBox.Origin == PdfPageValueOrigin.Defaulted)
        {
            artBox = new PdfEffectiveValue<PdfPageBox>(cropBox.Value, PdfPageValueOrigin.Defaulted);
        }
    }

    internal void InsertAnnotation(
        int index,
        PdfAnnotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);

        if (index < 0 || index > annotations.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (annotations.Contains(annotation))
        {
            throw new InvalidOperationException(
                "The annotation is already attached to this page.");
        }

        if (annotation.Parent is not null)
        {
            throw new InvalidOperationException(
                "The annotation is already attached to another page.");
        }

        annotations.Insert(index, annotation);
        annotation.AttachToPage(this);
    }

    internal int RemoveAnnotation(
        PdfAnnotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);

        var index = annotations.IndexOf(annotation);

        if (index < 0)
        {
            throw new InvalidOperationException(
                "The annotation is not attached to this page.");
        }

        annotations.RemoveAt(index);
        annotation.DetachFromPage(this);

        return index;
    }

    /// <summary>
    /// Returns a defensive copy of the page content-stream bytes.
    /// </summary>
    public byte[] GetContentBytes() =>
        contentData.ToArray();

    internal void ReplaceContent(
        ReadOnlySpan<byte> replacement)
    {
        contentData = replacement.ToArray();
    }

    internal bool ContainsResource(
        PdfName category,
        PdfName name)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(name);

        return resources.TryGetValue(
                category,
                out var categoryValue) &&
            categoryValue is PdfDictionary categoryDictionary &&
            categoryDictionary.ContainsKey(name);
    }

    internal bool TryGetResource(
        PdfName category,
        PdfName name,
        out PdfObject? resource)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(name);

        if (
            resources.TryGetValue(
                category,
                out var categoryValue) &&
            categoryValue is PdfDictionary categoryDictionary &&
            categoryDictionary.TryGetValue(
                name,
                out var value))
        {
            resource = value;
            return true;
        }

        resource = null;
        return false;
    }

    internal void SetResource(
        PdfName category,
        PdfName name,
        PdfObject resource)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(resource);

        if (!resources.TryGetValue(
                category,
                out var categoryValue))
        {
            categoryValue = new PdfDictionary();
            resources.Add(
                category,
                categoryValue);
        }

        if (categoryValue is not PdfDictionary categoryDictionary)
        {
            throw new InvalidOperationException(
                $"Page resource category '{category}' is not a PDF dictionary.");
        }

        categoryDictionary.Set(
            name,
            resource);
    }

    internal bool RemoveResource(
        PdfName category,
        PdfName name)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(name);

        if (
            !resources.TryGetValue(
                category,
                out var categoryValue) ||
            categoryValue is not PdfDictionary categoryDictionary)
        {
            return false;
        }

        var removed =
            categoryDictionary.Remove(name);

        if (
            removed &&
            categoryDictionary.Count == 0)
        {
            _ = resources.Remove(category);
        }

        return removed;
    }

    internal void BindObjectIdentifier(
        PdfObjectIdentifier identifier)
    {
        binding.Bind(identifier);
    }

    internal void AttachToParent(
        PdfPageTree pageTree)
    {
        ArgumentNullException.ThrowIfNull(pageTree);

        if (
            parent is not null &&
            !ReferenceEquals(parent, pageTree))
        {
            throw new InvalidOperationException(
                "The page already belongs to another page tree.");
        }

        parent = pageTree;
    }

    internal void DetachFromParent(
        PdfPageTree pageTree)
    {
        ArgumentNullException.ThrowIfNull(pageTree);

        if (ReferenceEquals(parent, pageTree))
        {
            parent = null;
        }
    }

    PdfDictionary
        IPdfDictionaryBackedModel.CreateDictionarySnapshot()
    {
        return CreateDictionarySnapshot();
    }

    internal PdfDictionary CreateDictionarySnapshot()
    {
        var dictionary = new PdfDictionary
        {
            {
                PdfNames.Type,
                PdfNames.Page
            },
        };

        AddExplicitBox(dictionary, PdfNames.MediaBox, mediaBox);
        AddExplicitBox(dictionary, PdfNames.CropBox, cropBox);
        AddExplicitBox(dictionary, PdfNames.BleedBox, bleedBox);
        AddExplicitBox(dictionary, PdfNames.TrimBox, trimBox);
        AddExplicitBox(dictionary, PdfNames.ArtBox, artBox);

        if (rotationState.Origin == PdfPageValueOrigin.Explicit)
        {
            dictionary.Add(
                PdfNames.Rotate,
                new PdfInteger((int)rotationState.Value));
        }

        if (contentData.Length > 0)
        {
            dictionary.Add(
                PdfNames.Contents,
                new PdfStream(
                    [],
                    contentData));
        }

        if (resources.Count > 0)
        {
            dictionary.Add(
                PdfNames.Resources,
                CreateResourceSnapshot());
        }

        if (parent is not null)
        {
            var parentIdentifier =
                parent.GetRequiredObjectIdentifier();

            dictionary.Add(
                PdfNames.Parent,
                new PdfIndirectReference(
                    parentIdentifier));
        }

        return dictionary;
    }

    private PdfDictionary CreateResourceSnapshot()
    {
        var snapshot = new PdfDictionary();

        foreach (var category in resources)
        {
            if (category.Value is not PdfDictionary categoryDictionary)
            {
                snapshot.Add(
                    category.Key,
                    category.Value);
                continue;
            }

            snapshot.Add(
                category.Key,
                categoryDictionary.CreateCopy());
        }

        return snapshot;
    }

    private static void AddExplicitBox(
        PdfDictionary dictionary,
        PdfName name,
        PdfEffectiveValue<PdfPageBox> state)
    {
        if (state.Origin != PdfPageValueOrigin.Explicit)
        {
            return;
        }

        var box = state.Value;
        dictionary.Add(
            name,
            new PdfArray(
            [
                CreateNumber(box.Left),
                CreateNumber(box.Bottom),
                CreateNumber(box.Right),
                CreateNumber(box.Top)
            ]));
    }

    private static PdfObject CreateNumber(
        double value)
    {
        return value == Math.Truncate(value) &&
            value >= long.MinValue &&
            value <= long.MaxValue
            ? new PdfInteger((long)value)
            : new PdfReal(value);
    }

    private static void ValidateDimension(
        double value,
        string parameterName)
    {
        if (
            !double.IsFinite(value) ||
            value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "A PDF page dimension must be finite and greater than zero.");
        }
    }

    private static void ValidateRotation(
        PdfPageRotation rotation)
    {
        if (!Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotation),
                rotation,
                "The PDF page rotation is not supported.");
        }
    }
}