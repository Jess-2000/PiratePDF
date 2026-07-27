using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Document.Model;

/// <summary>
/// Represents high-level information associated with a PDF trailer.
/// </summary>
public sealed class PdfTrailer
{
    public PdfObjectIdentifier? RootObject { get; private set; }

    public PdfObjectIdentifier? InformationObject { get; private set; }

    public PdfObjectIdentifier? EncryptionObject { get; private set; }

    public long? PreviousCrossReferenceOffset { get; private set; }

    public bool IsEncrypted =>
        EncryptionObject.HasValue;

    public void SetRootObject(
        PdfObjectIdentifier identifier)
    {
        RootObject = identifier;
    }

    public void SetInformationObject(
        PdfObjectIdentifier? identifier)
    {
        InformationObject = identifier;
    }

    public void SetEncryptionObject(
        PdfObjectIdentifier? identifier)
    {
        EncryptionObject = identifier;
    }

    public void SetPreviousCrossReferenceOffset(
        long? offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "A previous cross-reference offset cannot be negative.");
        }

        PreviousCrossReferenceOffset = offset;
    }

    internal PdfDictionary CreateDictionarySnapshot(
        int objectStoreSize)
    {
        if (objectStoreSize < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(objectStoreSize),
                objectStoreSize,
                "The trailer size must be at least one.");
        }

        var rootObject =
            RootObject ??
            throw new InvalidOperationException(
                "The trailer root object has not been assigned.");

        var dictionary = new PdfDictionary();

        dictionary.Add(
            PdfNames.Size,
            new PdfInteger(objectStoreSize));

        dictionary.Add(
            PdfNames.Root,
            new PdfIndirectReference(rootObject));

        AddOptionalReference(
            dictionary,
            PdfNames.Info,
            InformationObject);

        AddOptionalReference(
            dictionary,
            PdfNames.Encrypt,
            EncryptionObject);

        if (PreviousCrossReferenceOffset.HasValue)
        {
            dictionary.Add(
                PdfNames.Previous,
                new PdfInteger(
                    PreviousCrossReferenceOffset.Value));
        }

        return dictionary;
    }

    private static void AddOptionalReference(
        PdfDictionary dictionary,
        PdfName key,
        PdfObjectIdentifier? identifier)
    {
        if (!identifier.HasValue)
        {
            return;
        }

        dictionary.Add(
            key,
            new PdfIndirectReference(
                identifier.Value));
    }
}