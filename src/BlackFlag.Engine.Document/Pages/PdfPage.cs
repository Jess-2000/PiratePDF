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
    private PdfPageTree? parent;

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
        Width = width;
        Height = height;
        Rotation = rotation;
    }

    public Guid Id { get; }

    public double Width { get; }

    public double Height { get; }

    public PdfPageRotation Rotation { get; private set; }

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
        Rotation = rotation;
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
            {
                PdfNames.MediaBox,
                CreateMediaBox()
            }
        };

        if (Rotation != PdfPageRotation.None)
        {
            dictionary.Add(
                PdfNames.Rotate,
                new PdfInteger((int)Rotation));
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

    private PdfArray CreateMediaBox()
    {
        return new PdfArray(
        [
            new PdfInteger(0),
            new PdfInteger(0),
            CreateNumber(Width),
            CreateNumber(Height)
        ]);
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