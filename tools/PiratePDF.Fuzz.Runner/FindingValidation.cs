using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Portable discovery validation; no filesystem admission or execution authority is granted.</summary>
public static class FindingValidation
{
    public static FindingResult Inspect(ReadOnlyMemory<byte> json, ReadOnlyMemory<byte> input, string runId, string findingId)
    {
        try
        {
            if (json.Length > FindingStore.MaximumMetadataBytes || input.Length > FindingStore.MaximumArtifactBytes)
                throw new FindingException(FindingExit.IntegrityFailure);
            FindingMetadata metadata = FindingJson.Parse<FindingMetadata>(json);
            Validate(metadata, runId, findingId);
            if (!MatchesContent(metadata, input)) throw new FindingException(FindingExit.IntegrityFailure);
            return new(FindingExit.Success, metadata.Source.Observed, metadata);
        }
        catch (Exception error) when (FindingFiles.IsAdmissionError(error)) { return FindingFiles.Failure(error); }
    }

    public static bool CanRetain(int count, int retainedBytes, int inputBytes) =>
        count is >= 0 and < FindingStore.MaximumFindings && retainedBytes is >= 0 and <= FindingStore.MaximumRunBytes &&
        inputBytes is >= 0 and <= FindingStore.MaximumArtifactBytes && retainedBytes <= FindingStore.MaximumRunBytes - inputBytes;

    public static bool IsIncomingPath(string relative)
    {
        try { _ = IncomingParts(relative); return true; }
        catch (FindingException) { return false; }
    }

    internal static bool MatchesContent(FindingMetadata metadata, ReadOnlyMemory<byte> bytes) =>
        bytes.Length == metadata.Length && FindingFiles.Digest(bytes) == metadata.Sha256;

    internal static string[] IncomingParts(string relative)
    {
        if (string.IsNullOrEmpty(relative) || relative.Length > 256) throw new FindingException(FindingExit.InvalidInvocation);
        string[] parts = relative.Split('/');
        if (parts.Length is < 2 or > 4 || parts[0] != "incoming" || parts.Any(static part => part.Length is 0 or > 96 ||
            part[0] == '.' || part.Contains("..", StringComparison.Ordinal) ||
            !part.All(static c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_' or '.')))
            throw new FindingException(FindingExit.InvalidInvocation);
        return parts;
    }

    internal static void Validate(FindingMetadata metadata, string runId, string id)
    {
        if (!FindingFiles.Id(runId) || !FindingFiles.Id(id) || id is "pending" or "proposal" or "proposal-work" ||
            metadata.Schema != "piratepdf-fuzz-finding" || metadata.Version != 1 || metadata.RunId != runId ||
            metadata.FindingId != id || !FindingFiles.Hash(metadata.Sha256) || metadata.Length < 0 ||
            metadata.Rights != "repository-owned-no-external-license-asserted" || metadata.Reproduction != "unattempted" ||
            metadata.Minimization != "not-requested" || metadata.Promotion != "not-requested")
            throw new FindingException(FindingExit.IntegrityFailure);
        ValidateSource(metadata.Source);
        int ceiling = TargetRegistry.Get(metadata.Source.Target).MaximumInputBytes;
        if (metadata.InputCeiling != ceiling || metadata.Length > ceiling) throw new FindingException(FindingExit.IntegrityFailure);
    }

    internal static void ValidateSource(FindingSource source)
    {
        if (source is null || !FindingFiles.Id(source.Target) || !FindingFiles.Id(source.OriginId) ||
            !FindingFiles.Hash(source.ParentSha256) || !FindingFiles.Hash(source.TargetAssemblySha256) ||
            !FindingFiles.Hash(source.WorkerAssemblySha256) || source.SourceRevision is not { Length: 40 } ||
            !source.SourceRevision.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f') ||
            source.Toolchain is not ("neutral-net10-v1" or "pp-aud-036") ||
            !Enum.IsDefined(source.Kind) || !Enum.IsDefined(source.Observed) ||
            source.Origin is not (FindingOrigin.OwnedGenerated or FindingOrigin.OwnedSeedMutation or FindingOrigin.ControlledProbe) ||
            source.Seed is < 0 || source.Related is null || source.Related.Length > 64 ||
            !source.Related.All(static c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '-'))
            throw new FindingException(FindingExit.IntegrityFailure);
        FuzzTarget target = TargetRegistry.Get(source.Target);
        if (target.ConfigurationVersion != source.ConfigurationVersion || target.MaximumInputBytes > FindingStore.MaximumArtifactBytes)
            throw new FindingException(FindingExit.IntegrityFailure);
    }
}
