using BlackFlag.Engine.Document.Objects;

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
}