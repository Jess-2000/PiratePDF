using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using PiratePDF.DocGen.Library;
using PiratePDF.DocGen.Models;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Infrastructure;

// Deliberately limited to the five approved B9 registrations, not a provenance framework.
internal static class HistoricalMetadataSupplements
{
    internal const string FileName = "B9-Historical-Metadata.json";
    internal const string PolicyMarker = "B9-HISTORICAL-METADATA-V1";
    private static readonly JsonSerializerOptions Options = new()
    { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 8 };
    internal static readonly string[] Originals =
    [
        "reviews/Post-BF-1054-B9-Coverage-Guided-Campaign-Readiness.md",
        "reviews/Post-BF-1054-B9-Phase-A-Completion-2-Final-Integration.md",
        "reviews/Post-BF-1054-B9-Phase-A-Completion-2-Remaining-Independent-Supervision-and-Handoff.md",
        "reviews/Post-BF-1054-B9-Phase-A-Completion-2-Stabilization.md",
        "reviews/Post-BF-1054-B9-Phase-A-Completion-2B-Independent-Parent-and-Worker-Ownership.md"
    ];

    internal static IReadOnlyList<DocumentationDocument> Apply(string root, DocumentationDocument[] documents)
    {
        string path = Path.Combine(root, "reviews", FileName);
        if (!File.Exists(path)) return documents;
        byte[] bytes = ReadBounded(path);
        using JsonDocument json = JsonDocument.Parse(bytes, new() { MaxDepth = 8 });
        RejectDuplicateProperties(json.RootElement);
        Registration[] records = JsonSerializer.Deserialize<Registration[]>(bytes, Options)
            ?? throw new InvalidDataException("Missing historical registrations.");
        if (records.Length is < 1 or > 5) throw new InvalidDataException("Invalid historical registration count.");
        string policyPath = Path.Combine(root, "05-Governance", "Documentation-Standards.md");
        byte[] policyBytes = ReadBounded(policyPath);
        DocumentationDocument policy = Parsing.MarkdownDocumentParser.Parse(policyPath);
        if (!policy.RawMarkdown.Contains(PolicyMarker, StringComparison.Ordinal) ||
            DocumentationValidator.Validate(new[] { policy.Metadata }).HasErrors ||
            !policy.HasSection(DocumentationContract.RevisionHistoryHeading) ||
            MarkdownStructureValidator.Validate(policy).Any(issue => issue.Severity == ValidationSeverity.Error))
            throw new InvalidDataException("Historical supplementation policy is absent or invalid.");
        string policyHash = Convert.ToHexString(SHA256.HashData(policyBytes));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Registration record in records)
        {
            if (record is null) throw new InvalidDataException("Missing historical registration.");
            int index = Array.IndexOf(Originals, record.OriginalPath?.StartsWith("docs/", StringComparison.Ordinal) == true
                ? record.OriginalPath[5..] : "");
            // Report the rejected invariant without weakening historical provenance enforcement.
            string? invalidField =
                index < 0 ? "OriginalPath" :
                !seen.Add(record.OriginalPath!) ? "DuplicateOriginalPath" :
                record.Version != 1 ? "Version" :
                record.DocumentId != $"PP-HIST-B9-{index + 1:000}" ? "DocumentId" :
                record.Registered != "2026-10-24" ? "Registered" :
                record.NextReview != "2027-01-24" ? "NextReview" :
                record.HistoricalMetadata != "Unknown; not reconstructed." ? "HistoricalMetadata" :
                record.History != "2026-10-24: Initial corrective registration; original report unchanged." ? "History" :
                record.PolicySha256 != policyHash ? $"PolicySha256 (registered={record.PolicySha256 ?? "<null>"}, computed={policyHash})" : null;
            if (invalidField is not null)
                throw new InvalidDataException(
                    $"Unsupported, conflicting or unsubstantiated historical registration at entry {Array.IndexOf(records, record) + 1}: {invalidField}.");
            string originalPath = Path.GetFullPath(Path.Combine(root, Originals[index]));
            int position = Array.FindIndex(documents, document => string.Equals(document.FilePath, originalPath, StringComparison.OrdinalIgnoreCase));
            if (position < 0)
                throw new InvalidDataException($"Historical report entry {index + 1}: approved original is missing from parsed documents.");
            string originalHash = Convert.ToHexString(SHA256.HashData(ReadBounded(originalPath)));
            if (record.OriginalSha256 != originalHash)
                throw new InvalidDataException(
                    $"Historical report entry {index + 1}: OriginalSha256 mismatch (registered={record.OriginalSha256 ?? "<null>"}, computed={originalHash}).");
            DocumentationDocument original = documents[position];
            // Supplement only missing canonical fields; never override an existing header.
            if (original.Metadata != new DocumentMetadata(original.FilePath, original.Title, "", "", "", "", "", "", "", ""))
                throw new InvalidDataException("Historical registration conflicts with canonical metadata.");
            var metadata = new DocumentMetadata(original.FilePath,
                original.Title + " (historical; metadata registered " + record.Registered + ")",
                record.DocumentId, "Historical Metadata Registration", "1.0", "Review", "PiratePDF Project", "BF-1054",
                record.Registered, record.NextReview);
            if (DocumentationValidator.Validate(new[] { metadata }).HasErrors)
                throw new InvalidDataException("Invalid supplemental metadata.");
            documents[position] = new DocumentationDocument(metadata, original.RawMarkdown, original.Sections,
                original.RelatedDocumentIds, original.RelatedWorkItems.Append("BF-1054"), original.RevisionHistory)
            { MetadataSupplementPath = path };
        }
        return documents;
    }

    internal static bool IsHistoricalPath(string path) => Originals.Any(relative =>
        path.Replace('\\', '/').EndsWith("/" + relative, StringComparison.Ordinal));

    private static byte[] ReadBounded(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is < 1 or > 65536) throw new InvalidDataException("Historical registration resource exceeds its budget.");
        byte[] bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate supplemental property.");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (JsonElement item in element.EnumerateArray()) RejectDuplicateProperties(item);
    }

    internal sealed record Registration(int Version, string OriginalPath, string OriginalSha256, string DocumentId,
        string Registered, string NextReview, string HistoricalMetadata, string History, string PolicySha256);
}
