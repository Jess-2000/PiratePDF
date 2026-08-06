namespace PiratePDF.DocGen.Models;

internal static class DocumentStatuses
{
    // =========================================================================
    // Canonical Status Values
    // =========================================================================

    public const string Proposed = "Proposed";

    public const string Draft = "Draft";

    public const string Review = "Review";

    public const string Approved = "Approved";

    public const string InDevelopment = "In Development";

    public const string Implemented = "Implemented";

    public const string Tested = "Tested";

    public const string Released = "Released";

    public const string Deferred = "Deferred";

    public const string Deprecated = "Deprecated";

    public const string Superseded = "Superseded";

    public const string Rejected = "Rejected";

    private static readonly string[] StatusValues =
    [
        Proposed,
        Draft,
        Review,
        Approved,
        InDevelopment,
        Implemented,
        Tested,
        Released,
        Deferred,
        Deprecated,
        Superseded,
        Rejected
    ];

    private static readonly HashSet<string> StatusLookup =
        new(
            StatusValues,
            StringComparer.Ordinal);

    // =========================================================================
    // Public API
    // =========================================================================

    public static IReadOnlyCollection<string> All =>
        StatusValues;

    public static IEnumerable<string> GetAll()
    {
        return StatusValues;
    }

    public static bool IsValid(
        string? status)
    {
        return status is not null &&
               StatusLookup.Contains(status);
    }
}

