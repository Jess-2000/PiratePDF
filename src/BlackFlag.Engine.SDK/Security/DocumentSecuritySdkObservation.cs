namespace BlackFlag.Engine.SDK.Security;

/// <summary>Represents one immutable SDK-facing security observation.</summary>
public sealed record DocumentSecuritySdkObservation
{
    private readonly IReadOnlyDictionary<string, string> properties;

    public DocumentSecuritySdkObservation(
        DocumentSecuritySdkObservationCategory category,
        string kind,
        string description,
        string sourceFormat,
        DocumentSecuritySdkSourceLocation? location = null,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFormat);
        Category = category;
        Kind = kind;
        Description = description;
        SourceFormat = sourceFormat;
        Location = location;
        this.properties = properties is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(properties, StringComparer.Ordinal);
    }

    public DocumentSecuritySdkObservationCategory Category { get; }
    public string Kind { get; }
    public string Description { get; }
    public string SourceFormat { get; }
    public DocumentSecuritySdkSourceLocation? Location { get; }
    public IReadOnlyDictionary<string, string> Properties => properties;
}
