namespace BlackFlag.Engine.Security.Containers;

/// <summary>Describes a format-independent classification of embedded bytes.</summary>
public sealed record DocumentEmbeddedObjectClassification
{
    public DocumentEmbeddedObjectClassification(
        DocumentEmbeddedObjectKind kind,
        string formatIdentifier,
        bool isContainer,
        bool isExecutable,
        string? mediaType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formatIdentifier);
        Kind = kind;
        FormatIdentifier = formatIdentifier;
        IsContainer = isContainer;
        IsExecutable = isExecutable;
        MediaType = mediaType;
    }

    public DocumentEmbeddedObjectKind Kind { get; }
    public string FormatIdentifier { get; }
    public bool IsContainer { get; }
    public bool IsExecutable { get; }
    public string? MediaType { get; }
}
