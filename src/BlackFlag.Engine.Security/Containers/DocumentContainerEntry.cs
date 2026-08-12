namespace BlackFlag.Engine.Security.Containers;

/// <summary>Represents one expanded child produced by a safe container adapter.</summary>
public sealed record DocumentContainerEntry
{
    public DocumentContainerEntry(
        DocumentEmbeddedObject embeddedObject,
        long compressedLength)
    {
        ArgumentNullException.ThrowIfNull(embeddedObject);
        ArgumentOutOfRangeException.ThrowIfNegative(compressedLength);

        EmbeddedObject = embeddedObject;
        CompressedLength = compressedLength;
    }

    public DocumentEmbeddedObject EmbeddedObject { get; }
    public long CompressedLength { get; }
}
