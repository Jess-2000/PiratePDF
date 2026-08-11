
namespace BlackFlag.Engine.Security.Model;

/// <summary>
/// Represents one format-normalized fact discovered during security
/// inspection. Observations describe what exists; policy decides what it
/// means.
/// </summary>
public sealed record DocumentSecurityObservation
{
    private readonly IReadOnlyDictionary<string, string> properties;

    /// <summary>Initializes a security observation.</summary>
    public DocumentSecurityObservation(
        DocumentSecurityObservationCategory category,
        string kind,
        string description,
        string sourceFormat,
        DocumentSecuritySourceLocation? location = null,
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
            : new Dictionary<string, string>(
                properties,
                StringComparer.Ordinal);
    }

    /// <summary>Gets the normalized observation category.</summary>
    public DocumentSecurityObservationCategory Category { get; }

    /// <summary>
    /// Gets a stable semantic kind supplied by the format analyzer, such as
    /// <c>JavaScript</c>, <c>VbaProject</c>, or <c>EmbeddedOleObject</c>.
    /// </summary>
    public string Kind { get; }

    /// <summary>Gets a human-readable description of what was discovered.</summary>
    public string Description { get; }

    /// <summary>Gets the detected source format identifier.</summary>
    public string SourceFormat { get; }

    /// <summary>Gets the optional source location.</summary>
    public DocumentSecuritySourceLocation? Location { get; }

    /// <summary>Gets analyzer-provided format details.</summary>
    public IReadOnlyDictionary<string, string> Properties => properties;
}
