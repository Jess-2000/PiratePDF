using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Bounded, explicit-file candidate storage. Trust assumes an operator-controlled local workspace.</summary>
public sealed class FindingStore
{
    public const int MaximumArtifactBytes = 64 * 1024;
    public const int MaximumMetadataBytes = 8192;
    public const int MaximumEvidenceBytes = 4096;
    public const int MaximumFindings = 16;
    public const int MaximumRunBytes = 256 * 1024;
    public const int RequiredAttempts = 2;
    public const int MaximumReproductions = 4;
    internal string Repository { get; }
    internal string RunPath { get; }
    public string RunId { get; }

    public FindingStore(string repositoryRoot, string runId)
    {
        Repository = FindingFiles.Root(repositoryRoot);
        if (!FindingFiles.Id(runId)) throw new ArgumentException("Invalid run identity.");
        RunId = runId;
        RunPath = Path.Combine(Repository, "artifacts", "fuzz", "findings", runId);
    }

    internal string CandidatePath(string findingId)
    {
        if (!FindingFiles.Id(findingId) || findingId is "pending" or "proposal" or "proposal-work")
            throw new FindingException(FindingExit.InvalidInvocation);
        return Path.Combine(RunPath, findingId);
    }

    internal FileStream Lock()
    {
        FindingFiles.CreateDirectory(RunPath);
        // Persistent lock file is bounded. Never truncate; observed links/devices are refused before opening.
        string path = Path.Combine(RunPath, "active.lock");
        if (Path.Exists(path) && (File.GetAttributes(path) &
            (FileAttributes.ReparsePoint | FileAttributes.Directory | FileAttributes.Device)) != 0)
            throw new FindingException(FindingExit.IntegrityFailure);
        return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    public async Task<FindingResult> RetainAsync(string findingId, string artifactRelativePath, FindingSource source,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            string destination = CandidatePath(findingId);
            FindingValidation.ValidateSource(source);
            string input = IncomingPath(artifactRelativePath);
            FuzzTarget target = TargetRegistry.Get(source.Target);
            ReadOnlyMemory<byte> bytes = await InputFile.ReadAsync(input, target.MaximumInputBytes, cancellationToken).ConfigureAwait(false);
            var metadata = new FindingMetadata("piratepdf-fuzz-finding", 1, RunId, findingId,
                FindingFiles.Digest(bytes), bytes.Length, target.MaximumInputBytes, source,
                "repository-owned-no-external-license-asserted", "unattempted", "not-requested", "not-requested");
            using FileStream lease = Lock();
            IReadOnlyList<FindingMetadata> retained = await InventoryAsync(cancellationToken).ConfigureAwait(false);
            CorpusValidationResult corpus = await CorpusValidator.ValidateAsync(Repository, cancellationToken).ConfigureAwait(false);
            if (!corpus.IsValid) throw new FindingException(FindingExit.IntegrityFailure);
            if (retained.Any(item => item.Sha256 == metadata.Sha256) || corpus.Seeds.Any(seed => seed.Sha256 == metadata.Sha256))
                return new(FindingExit.Duplicate, FindingClass.Duplicate, metadata);
            if (!FindingValidation.CanRetain(retained.Count, retained.Sum(static item => item.Length), bytes.Length))
                throw new FindingException(FindingExit.LimitExceeded);
            if (Path.Exists(destination)) throw new FindingException(FindingExit.Duplicate);
            string pending = Path.Combine(RunPath, "pending");
            if (Path.Exists(pending)) throw new FindingException(FindingExit.InfrastructureFailure);
            FindingFiles.CreateDirectory(pending);
            string payload = Path.Combine(pending, "input.bin");
            string description = Path.Combine(pending, "metadata.json");
            try
            {
                await FindingFiles.WriteNewAsync(payload, bytes, cancellationToken).ConfigureAwait(false);
                await FindingFiles.WriteJsonAsync(description, metadata, cancellationToken).ConfigureAwait(false);
                await VerifyAsync(payload, metadata, cancellationToken).ConfigureAwait(false);
                Directory.Move(pending, destination);
            }
            catch
            {
                if (!FindingFiles.Cleanup(pending, [payload, description], [pending]))
                    throw new FindingException(FindingExit.InfrastructureFailure);
                throw;
            }
            return new(FindingExit.Success, source.Observed, metadata);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return new(FindingExit.Cancelled, FindingClass.Cancellation); }
        catch (Exception error) when (FindingFiles.IsAdmissionError(error)) { return FindingFiles.Failure(error); }
    }

    // At most 18 entries, then at most six files per candidate. No recursive discovery.
    public async Task<IReadOnlyList<FindingMetadata>> InventoryAsync(CancellationToken cancellationToken = default)
    {
        FindingFiles.CheckDirectory(RunPath, false);
        var ids = new List<string>();
        int entries = 0;
        foreach (string path in Directory.EnumerateFileSystemEntries(RunPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (++entries > MaximumFindings + 2) throw new FindingException(FindingExit.LimitExceeded);
            string name = Path.GetFileName(path);
            if (name == "active.lock") continue;
            if (name == "proposal") { FindingFiles.CheckDirectory(path, false); continue; }
            if (!FindingFiles.Id(name) || name is "pending" or "proposal-work") throw new FindingException(FindingExit.IntegrityFailure);
            if (ids.Count == MaximumFindings) throw new FindingException(FindingExit.LimitExceeded);
            ids.Add(name);
        }
        ids.Sort(StringComparer.Ordinal);
        var result = new List<FindingMetadata>();
        int bytes = 0;
        var hashes = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in ids)
        {
            FindingMetadata item = await LoadAsync(id, cancellationToken).ConfigureAwait(false);
            if (!hashes.Add(item.Sha256)) throw new FindingException(FindingExit.Duplicate);
            if (bytes > MaximumRunBytes - item.Length) throw new FindingException(FindingExit.LimitExceeded);
            bytes += item.Length;
            result.Add(item);
        }
        return result.AsReadOnly();
    }

    public async Task<FindingMetadata> LoadAsync(string findingId, CancellationToken cancellationToken = default)
    {
        string directory = CandidatePath(findingId);
        FindingFiles.CheckDirectory(directory, false);
        int count = 0;
        foreach (string path in Directory.EnumerateFileSystemEntries(directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string name = Path.GetFileName(path);
            if (++count > 2 + MaximumReproductions || name is not ("input.bin" or "metadata.json") && !IsEvidenceName(name))
                throw new FindingException(FindingExit.IntegrityFailure);
            int ceiling = name == "input.bin" ? MaximumArtifactBytes : name == "metadata.json" ? MaximumMetadataBytes : MaximumEvidenceBytes;
            _ = await InputFile.ReadAsync(path, ceiling, cancellationToken).ConfigureAwait(false);
        }
        FindingMetadata metadata = FindingJson.Parse<FindingMetadata>(await InputFile.ReadAsync(
            Path.Combine(directory, "metadata.json"), MaximumMetadataBytes, cancellationToken).ConfigureAwait(false));
        FindingValidation.Validate(metadata, RunId, findingId);
        await VerifyAsync(Path.Combine(directory, "input.bin"), metadata, cancellationToken).ConfigureAwait(false);
        return metadata;
    }

    internal static string EvidenceName(int index) => "reproduction-" + index.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + ".json";
    private static bool IsEvidenceName(string name) => name is "reproduction-01.json" or "reproduction-02.json" or
        "reproduction-03.json" or "reproduction-04.json";

    internal async Task<IReadOnlyList<FindingEvidence>> ReadEvidenceAsync(FindingMetadata metadata, CancellationToken token)
    {
        var evidence = new List<FindingEvidence>();
        bool missing = false;
        for (int index = 1; index <= MaximumReproductions; index++)
        {
            string path = Path.Combine(CandidatePath(metadata.FindingId), EvidenceName(index));
            if (!Path.Exists(path)) { missing = true; continue; }
            if (missing) throw new FindingException(FindingExit.IntegrityFailure);
            FindingEvidence item = FindingJson.Parse<FindingEvidence>(await InputFile.ReadAsync(path, MaximumEvidenceBytes, token).ConfigureAwait(false));
            if (!item.IsValidFor(metadata, index)) throw new FindingException(FindingExit.IntegrityFailure);
            evidence.Add(item);
        }
        return evidence.AsReadOnly();
    }

    internal static async Task VerifyAsync(string path, FindingMetadata metadata, CancellationToken token)
    {
        ReadOnlyMemory<byte> bytes = await InputFile.ReadAsync(path, metadata.InputCeiling, token).ConfigureAwait(false);
        if (!FindingValidation.MatchesContent(metadata, bytes))
            throw new FindingException(FindingExit.IntegrityFailure);
    }

    internal string IncomingPath(string relative)
    {
        string[] parts = FindingValidation.IncomingParts(relative);
        return Path.Combine(Repository, "artifacts", "fuzz", Path.Combine(parts));
    }

}
