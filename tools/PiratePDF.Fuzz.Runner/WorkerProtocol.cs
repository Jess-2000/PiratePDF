using System.Text.Json;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

internal static class WorkerProtocol
{
    internal static string? Validate(ReadOnlyMemory<byte> bytes, int exitCode, FuzzTarget target)
    {
        // Accept the worker's optional LF/CRLF terminator, not arbitrary surrounding records/whitespace.
        if (bytes.Length > 0 && bytes.Span[^1] == '\n')
        {
            bytes = bytes[..^1];
            if (bytes.Length > 0 && bytes.Span[^1] == '\r') bytes = bytes[..^1];
        }

        if (bytes.Length is 0 or > RunResult.MaximumJsonBytes || bytes.Span[0] != '{' ||
            bytes.Span[^1] != '}' || bytes.Span.ContainsAny((byte)'\r', (byte)'\n')) return null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 4 });
            JsonElement root = document.RootElement;
            string[] names = ["schemaVersion", "target", "configurationVersion", "outcome", "code", "observation", "exitCode"];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!names.Contains(property.Name, StringComparer.Ordinal) || !seen.Add(property.Name)) return null;
            }

            if (seen.Count != names.Length || !Number(root, "schemaVersion", 1) ||
                !Number(root, "configurationVersion", target.ConfigurationVersion) || !Number(root, "exitCode", exitCode) ||
                Text(root, "target") != target.Id) return null;

            string? outcome = Text(root, "outcome");
            string? code = Text(root, "code");
            JsonElement observation = root.GetProperty("observation");
            bool valid = exitCode switch
            {
                0 => outcome == "success" && code == "TargetResult" && IsDigest(observation),
                1 => outcome == "rejected" && code == "TargetResult" && IsRejection(observation),
                2 => outcome == "invalid" && observation.ValueKind == JsonValueKind.Null &&
                    code is "InputUnavailable" or "InputTooLarge" or "UnsupportedInputPlatform",
                3 => outcome == "cancelled" && code == "Cancelled" && observation.ValueKind == JsonValueKind.Null,
                4 => outcome == "failure" && observation.ValueKind == JsonValueKind.Null &&
                    code is "UnexpectedTargetFailure" or "UnexpectedRunnerFailure",
                _ => false
            };
            return valid ? code : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null; // Includes invalid UTF-8/string token access; no exception text is retained.
        }
    }

    private static bool Number(JsonElement root, string name, int expected) =>
        root.GetProperty(name).ValueKind == JsonValueKind.Number &&
        root.GetProperty(name).TryGetInt32(out int actual) && actual == expected;

    private static string? Text(JsonElement root, string name) =>
        root.GetProperty(name).ValueKind == JsonValueKind.String ? root.GetProperty(name).GetString() : null;

    private static bool IsDigest(JsonElement observation)
    {
        string? value = observation.ValueKind == JsonValueKind.String ? observation.GetString() : null;
        return value is { Length: 64 } && value.All(static ch => ch is >= '0' and <= '9' or >= 'A' and <= 'F');
    }

    private static bool IsRejection(JsonElement observation)
    {
        if (observation.ValueKind != JsonValueKind.String) return false;
        try
        {
            _ = FuzzResult.Rejected(observation.GetString()!);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
