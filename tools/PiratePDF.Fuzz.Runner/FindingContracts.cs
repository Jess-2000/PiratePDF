using System.Text.Json;
using System.Text.Json.Serialization;

namespace PiratePDF.Fuzz.Runner;

public enum FindingClass
{
    Success, ExpectedRejection, UnexpectedManagedFailure, WorkerProtocolFailure, AbnormalExit,
    Timeout, Cancellation, CleanupFailure, IntegrityFailure, NonReproducible, Duplicate, Indeterminate
}

public enum FindingOrigin { OwnedSeedMutation, OwnedGenerated, ControlledProbe, Unreviewed }
public enum FindingKind { Crash, Timeout, UnexpectedResult, ControlledFixture }
public enum FindingExit
{
    Success = 0, InvalidInvocation = 20, IntegrityFailure = 21, NonReproducible = 22,
    ReproducedFailure = 23, MinimizationRefused = 24, PromotionRefused = 25,
    Cancelled = 26, InfrastructureFailure = 27, Duplicate = 28, LimitExceeded = 29
}

/// <summary>Operator-supplied provenance, never instructions from an artifact's filename or bytes.</summary>
public sealed record FindingSource(string Target, int ConfigurationVersion, FindingKind Kind,
    FindingClass Observed, FindingOrigin Origin, string OriginId, string ParentSha256,
    string Related, int? Seed, string Toolchain, string SourceRevision, string TargetAssemblySha256,
    string WorkerAssemblySha256);

/// <summary>Immutable discovery snapshot. Later evidence does not rewrite the discovery or its oracle.</summary>
public sealed record FindingMetadata(string Schema, int Version, string RunId, string FindingId,
    string Sha256, int Length, int InputCeiling, FindingSource Source, string Rights,
    string Reproduction, string Minimization, string Promotion);

public sealed record FindingAttempt(int Attempt, FindingClass Classification, string Code);

public sealed class FindingResult
{
    internal FindingResult(FindingExit exit, FindingClass classification = FindingClass.Indeterminate,
        FindingMetadata? metadata = null, IEnumerable<FindingAttempt>? attempts = null, bool agrees = false)
    {
        ExitCode = exit;
        Classification = classification;
        Metadata = metadata;
        Attempts = Array.AsReadOnly(attempts?.ToArray() ?? []);
        Agrees = agrees;
    }

    public FindingExit ExitCode { get; }
    public FindingClass Classification { get; }
    public FindingMetadata? Metadata { get; }
    public IReadOnlyList<FindingAttempt> Attempts { get; }
    public bool Agrees { get; }
    public bool MatchesDiscovery => Agrees && Classification == Metadata?.Source.Observed;
    public string Json => JsonSerializer.Serialize(new
    {
        schema = "piratepdf-finding-result", version = 1, exitCode = (int)ExitCode,
        classification = Classification.ToString(), run = Metadata?.RunId, finding = Metadata?.FindingId,
        target = Metadata?.Source.Target, configurationVersion = Metadata?.Source.ConfigurationVersion,
        sha256 = Metadata?.Sha256, length = Metadata?.Length, agrees = Agrees, matchesDiscovery = MatchesDiscovery, attempts = Attempts
    }, FindingJson.Options);
}

internal static class FindingJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        MaxDepth = 6,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    internal static T Parse<T>(ReadOnlyMemory<byte> bytes)
    {
        using JsonDocument document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 6 });
        Check(document.RootElement);
        return JsonSerializer.Deserialize<T>(bytes.Span, Options) ?? throw new JsonException();
    }

    private static void Check(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException();
                Check(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            if (element.GetArrayLength() > 128) throw new JsonException();
            foreach (JsonElement item in element.EnumerateArray()) Check(item);
        }
    }
}

internal sealed class FindingException(FindingExit exit) : Exception
{
    internal FindingExit Exit { get; } = exit;
}
