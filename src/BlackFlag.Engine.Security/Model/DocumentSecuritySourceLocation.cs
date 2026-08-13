
namespace BlackFlag.Engine.Security.Model;

/// <summary>
/// Identifies where a security observation originated without exposing
/// format-specific parser implementation types.
/// </summary>
public sealed record DocumentSecuritySourceLocation
{
    /// <summary>Gets the zero-based page index when the observation belongs to a page.</summary>
    public int? PageIndex { get; init; }

    /// <summary>Gets a format-specific logical path such as an OOXML part name.</summary>
    public string? LogicalPath { get; init; }

    /// <summary>Gets a byte offset when a stable source offset is available.</summary>
    public long? ByteOffset { get; init; }

    /// <summary>Gets an object or relationship identifier suitable for reports.</summary>
    public string? ObjectIdentifier { get; init; }
}
