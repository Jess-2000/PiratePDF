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
        var dictionary = new PdfDictionary();

        dictionary.Add(
            PdfNames.Type,
            PdfNames.Page);

        dictionary.Add(
            PdfNames.MediaBox,
            CreateMediaBox());

        if (Rotation != PdfPageRotation.None)
        {
            dictionary.Add(
                PdfNames.Rotate,
                new PdfInteger((int)Rotation));
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