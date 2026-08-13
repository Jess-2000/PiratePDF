namespace BlackFlag.Engine.Security.Containers;

/// <summary>Represents one passive embedded payload supplied for security inspection.</summary>
public sealed record DocumentEmbeddedObject
{
    public DocumentEmbeddedObject(
        ReadOnlyMemory<byte> content,
        string logicalPath,
        long? compressedLength = null,
        string? declaredMediaType = null,
        string? declaredFileName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalPath);
        ArgumentOutOfRangeException.ThrowIfNegative(compressedLength ?? 0);

        Content = content;
        LogicalPath = logicalPath;
        CompressedLength = compressedLength;
        DeclaredMediaType = declaredMediaType;
        DeclaredFileName = declaredFileName;
    }

    public ReadOnlyMemory<byte> Content { get; }
    public string LogicalPath { get; }
    public long? CompressedLength { get; }
    public string? DeclaredMediaType { get; }
    public string? DeclaredFileName { get; }
}
