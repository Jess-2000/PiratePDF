using System.Diagnostics;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Parent-owned independent deterministic admission state; child counters never enlarge policy.</summary>
internal sealed class CampaignBrokerSchedule(string repository, CampaignInvocationIdentity identity)
{
    private readonly CampaignConfiguration configuration = new(identity.Cases, TimeSpan.FromTicks(identity.DurationTicks), identity.ProcessedBytes,
        identity.PoolInputs, identity.PoolBytes, identity.TelemetryBytes);
    private readonly string[] targets = identity.Target == "all" ? ["filter-asciihex", "filter-flate", "font-sanitize"] : [identity.Target];
    private IReadOnlyList<CorpusSeed>? seeds;
    internal string? ManifestHash { get; private set; }
    private Session? session;
    private int position;
    internal int Requests { get; private set; }
    internal int Executed { get; private set; }
    internal int Successes { get; private set; }
    internal int Rejections { get; private set; }
    internal List<CampaignTargetResult> Ended { get; } = [];
    internal bool TerminalWorkerFailure { get; private set; }

    internal async Task BeginAsync(string target, CancellationToken token)
    {
        if (session is not null || position >= targets.Length || target != targets[position] || TerminalWorkerFailure)
            throw new IOException("Out-of-order target request.");
        var next = new Session(target, configuration);
        if (seeds is null)
        {
            Reserve(next, CampaignWork.ManifestUnits);
            ReadOnlyMemory<byte> manifest = await InputFile.ReadAsync(Path.Combine(repository, "tests", "FuzzCorpus", "manifest.json"),
                CorpusValidator.MaximumManifestBytes, token).ConfigureAwait(false);
            var metadata = CorpusValidator.ValidateMetadata(manifest);
            if (!Owned(metadata)) throw new IOException("Invalid parent corpus.");
            ManifestHash = FindingFiles.Digest(manifest);
            Reserve(next, CampaignWork.ValidationUnits(metadata.Seeds) + CampaignWork.ManifestUnits);
            var corpus = await CorpusValidator.ValidateAsync(repository, token).ConfigureAwait(false);
            if (!Owned(corpus)) throw new IOException("Invalid parent corpus.");
            seeds = corpus.Seeds;
            await CheckManifestAsync(token).ConfigureAwait(false);
        }
        session = next;
        foreach (var seed in seeds.Where(seed => seed.Target == target))
        {
            if (next.Pool.Admission(seed.Length) != CampaignStop.None) break;
            Reserve(next, CampaignWork.SeedUnits(seed.Length));
            var bytes = await ReadSeedAsync(seed, token).ConfigureAwait(false);
            _ = next.Pool.Add(seed.Id, bytes);
        }
    }

    internal async Task<CampaignCandidate> AdmitAsync(CampaignCaseRequest request, CancellationToken token)
    {
        Session current = session ?? throw new IOException("No active target.");
        if (request.Target != current.Target || request.Case != current.Budget.Requests ||
            request.RemainingTicks <= 0 || request.RemainingTicks > WorkerSupervisor.CaseTimeout.Ticks || TerminalWorkerFailure ||
            current.Clock.Elapsed >= configuration.Duration) throw new IOException("Invalid case request.");
        CampaignCandidate candidate = await NextAsync(current, token).ConfigureAwait(false) ?? throw new IOException("Parent policy refuses case.");
        if (candidate.Identity.Index != request.Attempt || candidate.Identity.OutputSha256 != request.InputHash ||
            candidate.Bytes.Length != request.Length || request.Work != current.Budget.ProcessedBytes)
            throw new IOException("Deterministic case or work mismatch.");
        current.Budget.Dispatched();
        Requests++;
        return candidate;
    }

    internal TimeSpan Remaining(CampaignCaseRequest request) => TimeSpan.FromTicks(Math.Max(1,
        Math.Min(request.RemainingTicks, (configuration.Duration - session!.Clock.Elapsed).Ticks)));

    internal void Observe(CampaignCandidate candidate, SupervisionResult worker)
    {
        Session current = session!;
        bool completed = worker.Status == SupervisionStatus.Completed && worker.ExitCode is 0 or 1 && worker.WorkerCode == "TargetResult";
        if (completed || worker.Status == SupervisionStatus.WorkerFailure && worker.WorkerCode == "UnexpectedTargetFailure") { Executed++; current.Executed++; }
        if (completed)
        {
            if (worker.ExitCode == 0) { Successes++; current.Successes++; } else { Rejections++; current.Rejections++; }
            _ = current.Pool.Add(candidate.Identity.RootSeed, candidate.Bytes);
        }
        else TerminalWorkerFailure = true;
        current.Pending = null;
    }

    internal async Task EndAsync(CampaignTargetResult result, CancellationToken token)
    {
        Session current = session ?? throw new IOException("Missing target state.");
        if (result.Target != current.Target || result.WorkerRequests != current.Budget.Requests || result.Executed != current.Executed ||
            result.Successes != current.Successes || result.Rejections != current.Rejections || result.Attempts < current.Attempts.Attempts ||
            result.Attempts > CampaignConfiguration.MaximumCases * CampaignConfiguration.DuplicateRetries || !Enum.IsDefined(result.Stop) ||
            !double.IsFinite(result.ElapsedSeconds) || result.ElapsedSeconds < 0 || result.PoolInputs < 0 || result.PoolInputs > configuration.PoolInputs ||
            result.PoolBytes < 0 || result.PoolBytes > configuration.PoolBytes) throw new IOException("Invalid target accounting.");
        // Reproduce trailing duplicates/admission stops without trusting a claimed attempt/work allowance.
        while (current.Attempts.Attempts < result.Attempts)
        {
            var candidate = await NextAsync(current, token).ConfigureAwait(false);
            if (candidate is not null && current.Attempts.Attempts != result.Attempts) throw new IOException("Unreported executable case.");
            if (candidate is null && current.Attempts.Attempts < result.Attempts) throw new IOException("Attempt limit mismatch.");
        }
        if (result.Stop is CampaignStop.ProcessedByteLimit or CampaignStop.PoolLimit or CampaignStop.DuplicateLimit)
        {
            if (await NextAsync(current, token).ConfigureAwait(false) is not null || current.Stop != result.Stop)
                throw new IOException("Terminal policy mismatch.");
        }
        if (result.ProcessedBytes < current.Budget.ProcessedBytes || result.ProcessedBytes > configuration.ProcessedBytes ||
            result.Duplicates != current.Attempts.Duplicates || result.Stop == CampaignStop.CaseLimit && current.Budget.Requests != configuration.Cases)
            throw new IOException("Terminal budget mismatch.");
        Ended.Add(result);
        position++;
        session = null;
    }

    private async Task<CampaignCandidate?> NextAsync(Session current, CancellationToken token)
    {
        if (current.Pending is not null) return current.Pending;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            if (current.Budget.Requests == configuration.Cases) { current.Stop = CampaignStop.CaseLimit; return null; }
            if (current.Attempts.BeforeAttempt() != CampaignStop.None || current.Stop == CampaignStop.DuplicateLimit)
            { current.Stop = CampaignStop.DuplicateLimit; return null; }
            if (current.Pool.Count == 0) { current.Stop = CampaignStop.PoolLimit; return null; }
            int index = current.Attempts.Attempts % current.Pool.Count;
            var source = current.Pool.Describe(index);
            int maximum = TargetRegistry.Get(current.Target).MaximumInputBytes;
            if (current.Pool.Admission(CampaignWork.CandidateCeiling(source.Length, maximum)) != CampaignStop.None)
            { current.Stop = CampaignStop.PoolLimit; return null; }
            if (!TryReserve(current, CampaignWork.ManifestUnits)) return null;
            await CheckManifestAsync(token).ConfigureAwait(false);
            var root = seeds!.Single(seed => seed.Target == current.Target && seed.Id == source.RootSeed);
            if (!TryReserve(current, CampaignWork.AttemptUnits(source.Length, root.Length, maximum))) return null;
            if (current.Pool.TrySource(index, current.Budget, TimeSpan.Zero, out CampaignParent? parent) != CampaignStop.None)
            { current.Stop = CampaignStop.ProcessedByteLimit; return null; }
            _ = await ReadSeedAsync(root, token).ConfigureAwait(false);
            var candidate = CampaignMutation.Generate(current.Target, parent!.RootSeed, current.Attempts.Next(), parent.Bytes.Span);
            if (current.Pool.Contains(candidate.Identity.OutputSha256))
            {
                current.Stop = current.Attempts.Observe(true);
                if (current.Stop == CampaignStop.DuplicateLimit) return null;
                continue;
            }
            _ = current.Attempts.Observe(false);
            current.Stop = CampaignStop.None;
            current.Pending = candidate;
            return candidate;
        }
    }

    private async Task CheckManifestAsync(CancellationToken token)
    {
        if (FindingFiles.Digest(await InputFile.ReadAsync(Path.Combine(repository, "tests", "FuzzCorpus", "manifest.json"),
            CorpusValidator.MaximumManifestBytes, token).ConfigureAwait(false)) != ManifestHash) throw new IOException("Parent corpus changed.");
    }
    private async Task<ReadOnlyMemory<byte>> ReadSeedAsync(CorpusSeed seed, CancellationToken token)
    {
        var bytes = await InputFile.ReadAsync(Path.Combine(repository, seed.Path), seed.Length, token).ConfigureAwait(false);
        if (bytes.Length != seed.Length || FindingFiles.Digest(bytes) != seed.Sha256) throw new IOException("Parent seed changed.");
        return bytes;
    }
    private static bool Owned(CorpusValidationResult result) => result.IsValid && result.Seeds.Count == 15 &&
        result.Seeds.Count(seed => seed.Target == "filter-asciihex") == 6 && result.Seeds.Count(seed => seed.Target == "filter-flate") == 6 &&
        result.Seeds.Count(seed => seed.Target == "font-sanitize") == 3;
    private static bool TryReserve(Session current, long units)
    {
        current.Stop = current.Budget.AdmitWork(units, TimeSpan.Zero);
        return current.Stop == CampaignStop.None;
    }
    private static void Reserve(Session current, long units) { if (!TryReserve(current, units)) throw new IOException("Parent work budget exhausted."); }
    private sealed class Session(string target, CampaignConfiguration configuration)
    {
        internal string Target { get; } = target;
        internal CampaignBudget Budget { get; } = new(configuration);
        internal CampaignPool Pool { get; } = new(configuration.PoolInputs, configuration.PoolBytes);
        internal CampaignAttempts Attempts { get; } = new();
        internal Stopwatch Clock { get; } = Stopwatch.StartNew();
        internal CampaignStop Stop;
        internal CampaignCandidate? Pending;
        internal int Executed, Successes, Rejections;
    }
}
