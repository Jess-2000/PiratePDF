using System.Text.Json;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

internal static class CorpusManifestParser
{
    internal static IReadOnlyList<CorpusSeed> Parse(ReadOnlyMemory<byte> bytes)
    {
        using JsonDocument document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 4 });
        JsonElement root = document.RootElement;
        Fields(root, ["schema", "version", "seeds"]);
        if (Text(root, "schema", 64) != "piratepdf-fuzz-corpus" || Integer(root, "version") != 1)
            Fail(CorpusFailure.UnsupportedSchema);
        JsonElement entries = root.GetProperty("seeds");
        if (entries.ValueKind != JsonValueKind.Array) Fail(CorpusFailure.InvalidFields);
        int count = entries.GetArrayLength();
        if (count > CorpusValidator.MaximumEntries) Fail(CorpusFailure.TooManyEntries);
        if (count == 0) Fail(CorpusFailure.InvalidFields);

        var seeds = new List<CorpusSeed>(count);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        string? previousPath = null;
        foreach (JsonElement entry in entries.EnumerateArray())
        {
            Fields(entry, ["id", "target", "configurationVersion", "path", "length", "sha256", "sourceRecipe",
                "origin", "rights", "category", "purpose", "expectedOutcome", "related"]);
            string id = Text(entry, "id", 64);
            if (!StableId(id)) Fail(CorpusFailure.InvalidId);
            string targetId = Text(entry, "target", 64);
            FuzzTarget target;
            try { target = TargetRegistry.Get(targetId); }
            catch (ArgumentException) { throw new CorpusValidationException(CorpusFailure.UnknownTarget); }
            int configuration = Integer(entry, "configurationVersion");
            if (configuration != target.ConfigurationVersion) Fail(CorpusFailure.ConfigurationMismatch);
            string path = Text(entry, "path", 200);
            if (!ids.Add(id)) Fail(CorpusFailure.DuplicateId);
            if (!paths.Add(path)) Fail(CorpusFailure.DuplicatePath);
            // A single canonical form excludes roots, separators, traversal, device/ADS and case aliases.
            if (path != $"tests/FuzzCorpus/{target.Id}/{id}.bin") Fail(CorpusFailure.InvalidPath);
            if (previousPath is not null && StringComparer.Ordinal.Compare(previousPath, path) >= 0)
                Fail(CorpusFailure.InvalidOrdering);
            previousPath = path;
            int length = Integer(entry, "length");
            if (length < 0) Fail(CorpusFailure.InvalidMetadata);
            if (length > target.MaximumInputBytes) Fail(CorpusFailure.SeedTooLarge);
            string hash = Text(entry, "sha256", 64);
            if (hash.Length != 64 || !hash.All(static ch => ch is >= '0' and <= '9' or >= 'A' and <= 'F'))
                Fail(CorpusFailure.InvalidMetadata);
            string source = Text(entry, "sourceRecipe", 256);
            if (!source.StartsWith("tests/", StringComparison.Ordinal) || source.Contains("..", StringComparison.Ordinal) ||
                source.Contains(':') || source.Contains('\\')) Fail(CorpusFailure.InvalidMetadata);
            string origin = Text(entry, "origin", 32);
            string category = Text(entry, "category", 32);
            string expected = Text(entry, "expectedOutcome", 32);
            if (origin != "repository-owned" || expected is not ("success" or "rejected") ||
                category is not ("valid-minimal" or "malformed-structural" or "truncated" or "numeric-boundary" or
                    "resource-boundary" or "historical-regression")) Fail(CorpusFailure.InvalidVocabulary);
            JsonElement related = entry.GetProperty("related");
            if (related.ValueKind != JsonValueKind.Array || related.GetArrayLength() > 4) Fail(CorpusFailure.InvalidMetadata);
            var references = new List<string>(4);
            foreach (JsonElement reference in related.EnumerateArray())
            {
                string value = BoundedText(reference, 64);
                if (!value.All(static ch => ch is >= 'A' and <= 'Z' or >= '0' and <= '9' or '-'))
                    Fail(CorpusFailure.InvalidMetadata);
                references.Add(value);
            }
            seeds.Add(new CorpusSeed(id, targetId, configuration, path, length, hash, source, origin,
                Text(entry, "rights", 160), category, Text(entry, "purpose", 256), expected, references.AsReadOnly()));
        }
        return seeds.AsReadOnly();
    }

    private static void Fields(JsonElement element, string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object) Fail(CorpusFailure.InvalidFields);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!names.Contains(property.Name, StringComparer.Ordinal) || !seen.Add(property.Name))
                Fail(CorpusFailure.InvalidFields);
        }
        if (seen.Count != names.Length) Fail(CorpusFailure.InvalidFields);
    }

    private static int Integer(JsonElement element, string name)
    {
        JsonElement value = element.GetProperty(name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int result))
            throw new CorpusValidationException(CorpusFailure.InvalidFields);
        return result;
    }

    private static string Text(JsonElement element, string name, int maximum) => BoundedText(element.GetProperty(name), maximum);

    private static string BoundedText(JsonElement value, int maximum)
    {
        if (value.ValueKind != JsonValueKind.String) Fail(CorpusFailure.InvalidFields);
        string text = value.GetString()!;
        if (string.IsNullOrWhiteSpace(text) || text.Length > maximum || text.Any(char.IsControl))
            Fail(CorpusFailure.InvalidMetadata);
        return text;
    }

    private static bool StableId(string id) => id[0] is >= 'a' and <= 'z' &&
        id.All(static ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    private static void Fail(CorpusFailure failure) => throw new CorpusValidationException(failure);
}
