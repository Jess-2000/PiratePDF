using System.Collections.Frozen;

namespace PiratePDF.DocGen.Repair.References;

internal static class DocumentationRedirectMap
{
    private static readonly FrozenDictionary<string, string>
        Redirects =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["PP-GOV-001"] = "PP-GOV-002",
                ["PP-ROAD-001"] = "PP-ROAD-000"
            }
            .ToFrozenDictionary(
                StringComparer.OrdinalIgnoreCase);

    public static bool TryResolve(
        string originalDocumentId,
        out string replacementDocumentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            originalDocumentId);

        return Redirects.TryGetValue(
            originalDocumentId,
            out replacementDocumentId!);
    }

    public static IReadOnlyDictionary<string, string> GetAll()
    {
        return Redirects;
    }
}
