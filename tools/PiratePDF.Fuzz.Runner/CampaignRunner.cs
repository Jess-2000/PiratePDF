using System.Diagnostics;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Explicit mutation-only campaign. Every input uses the existing fresh managed worker.</summary>
public sealed class CampaignRunner(WorkerSupervisor supervisor, TimeProvider? timeProvider = null, TimeSpan? outerDuration = null,
    ICampaignWorkerExecutor? workerExecutor = null)
{
    public async Task<CampaignResult> RunAsync(string repository, string run, string targetId, string revision,
        CampaignConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!CampaignCommand.ValidIdentity(repository, run, targetId, revision) || configuration is null)
            return new(CampaignStop.InvalidInvocation);
        if (cancellationToken.IsCancellationRequested) return new(CampaignStop.Cancelled);
        using var outer = new CampaignDeadline(outerDuration ?? CampaignDeadline.MaximumDuration, timeProvider, cancellationToken);
        var results = new List<CampaignTargetResult>();
        MutationWorkspace? workspace = null;
        CampaignEvidence? evidence = null;
        Session? current = null;
        CampaignStop stop = CampaignStop.WorkspaceFailure;
        string? finding = null;
        bool clean = true;
        bool outerExpired = false;
        CampaignDeadline? subsequentOuter = null;
        CampaignDeadline lifetime = outer;
        try
        {
            repository = FindingFiles.Root(repository);
            string incoming = Path.Combine(repository, "artifacts", "fuzz", "incoming", run);
            string findings = Path.Combine(repository, "artifacts", "fuzz", "findings", run);
            if (Path.Exists(incoming) || Path.Exists(findings)) return new(CampaignStop.WorkspaceFailure);
            outer.Token.ThrowIfCancellationRequested();
            string[] targets = targetId == "all" ? ["filter-asciihex", "filter-flate", "font-sanitize"] : [targetId];
            current = new(targets[0], configuration);
            CampaignStop setupStop = current.Budget.AdmitWork(CampaignWork.ManifestUnits, TimeSpan.Zero);
            if (setupStop != CampaignStop.None) return new(setupStop, [current.Snapshot(setupStop)], run);
            ReadOnlyMemory<byte> manifest = await InputFile.ReadAsync(Path.Combine(repository, "tests", "FuzzCorpus", "manifest.json"),
                CorpusValidator.MaximumManifestBytes, outer.Token).ConfigureAwait(false);
            CorpusValidationResult metadata = CorpusValidator.ValidateMetadata(manifest);
            if (!OwnedCorpus(metadata)) return new(CampaignStop.IntegrityFailure);
            string manifestHash = FindingFiles.Digest(manifest);
            long setupWork = CampaignWork.ValidationUnits(metadata.Seeds) + CampaignWork.ManifestUnits;
            setupStop = current.Budget.AdmitWork(setupWork, TimeSpan.Zero);
            if (setupStop != CampaignStop.None) return new(setupStop, [current.Snapshot(setupStop)], run);
            CorpusValidationResult corpus = await CorpusValidator.ValidateAsync(repository, outer.Token).ConfigureAwait(false);
            if (!OwnedCorpus(corpus)) return new(CampaignStop.IntegrityFailure);
            manifest = await InputFile.ReadAsync(Path.Combine(repository, "tests", "FuzzCorpus", "manifest.json"),
                CorpusValidator.MaximumManifestBytes, outer.Token).ConfigureAwait(false);
            if (FindingFiles.Digest(manifest) != manifestHash) return new(CampaignStop.IntegrityFailure);
            string workerHash = supervisor.WorkerIdentity;
            string targetHash = WorkerSupervisor.TargetIdentity;
            evidence = new(repository, run, configuration.TelemetryBytes);
            evidence.Open();
            if (!evidence.Reserve()) throw new IOException("Campaign evidence capacity is unavailable.");
            evidence.AppendReserved(new { schema = "piratepdf-mutation-campaign-start", version = 1, revision,
                run, algorithm = CampaignMutation.Algorithm, seed = CampaignMutation.Seed, configuration,
                manifestHash, workerHash, targetHash, targets = targetId, runtime = Environment.Version.ToString(),
                platform = "Windows-local-file", managedHeapBytes = 268435456, hardTotalMemoryContainment = false,
                caseSeconds = 10, cleanupSeconds = 5, sequentialWorkers = 1,
                outerSeconds = (outerDuration ?? CampaignDeadline.MaximumDuration).TotalSeconds,
                outerIsCooperative = true, setupWorkUnits = setupWork + CampaignWork.ManifestUnits,
                workAccounting = "conservative-payload-byte-pass-v1",
                corpus = corpus.Seeds.Select(seed => new { seed.Target, seed.Id, seed.Sha256, seed.Length }).ToArray() });
            workspace = MutationWorkspace.ForCampaign(repository, run);
            workspace.Open();
            foreach (string target in targets)
            {
                // The first outer clock includes corpus/workspace setup. Later targets get their own finite allowance.
                if (results.Count != 0)
                {
                    subsequentOuter?.Dispose();
                    subsequentOuter = new CampaignDeadline(outerDuration ?? CampaignDeadline.MaximumDuration, timeProvider, cancellationToken);
                    lifetime = subsequentOuter;
                }
                if (results.Count != 0) current = new(target, configuration);
                current.Clock.Restart();
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                deadline.CancelAfter(configuration.Duration);
                try
                {
                    if (workerExecutor is not null) await workerExecutor.BeginAsync(target, deadline.Token).ConfigureAwait(false);
                    stop = await ExecuteTargetAsync(repository, run, revision, corpus.Seeds, manifestHash, current,
                        workspace, evidence, deadline.Token, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (deadline.IsCancellationRequested)
                { stop = lifetime.CancellationStop(cancellationToken); }
                catch (Exception error) when (FindingFiles.IsAdmissionError(error)) { stop = CampaignStop.IntegrityFailure; }
                if (stop == CampaignStop.Deadline && lifetime.Expired) stop = CampaignStop.OuterDeadline;

                if (current.Candidate is not null && stop is not (CampaignStop.CaseLimit or CampaignStop.PoolLimit or
                    CampaignStop.ProcessedByteLimit or CampaignStop.TelemetryLimit or CampaignStop.DuplicateLimit))
                {
                    // The campaign/caller token may already be cancelled. Evidence gets its own finite allowance.
                    using var retention = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    using var retentionDeadline = CancellationTokenSource.CreateLinkedTokenSource(retention.Token, lifetime.ExpirationToken);
                    finding = "case-" + current.Candidate.Identity.Index.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
                    // Caller cancellation must not erase evidence: only the outer clock and retention allowance apply here.
                    bool retained = await RetainAsync(repository, run, revision, finding, stop, current, workspace,
                        retentionDeadline.Token).ConfigureAwait(false);
                    if (!retained)
                    {
                        workspace.PreserveInput();
                        stop = CampaignStop.RetentionFailure;
                        clean = false;
                    }
                    else if (!workspace.ClearCase()) { stop = CampaignStop.CleanupFailure; clean = false; }
                    if (current.Worker?.Status == SupervisionStatus.TerminationFailed) clean = false;
                }
                outerExpired |= lifetime.Expired;
                if (lifetime.Expired && clean && stop != CampaignStop.RetentionFailure) stop = CampaignStop.OuterDeadline;
                results.Add(current.Snapshot(stop));
                if (workerExecutor is not null) await workerExecutor.EndAsync(results[^1], lifetime.Token).ConfigureAwait(false);
                if (stop is not (CampaignStop.CaseLimit or CampaignStop.PoolLimit or CampaignStop.ProcessedByteLimit or CampaignStop.DuplicateLimit)) break;
            }
        }
        catch (OperationCanceledException) when (outer.Token.IsCancellationRequested)
        { stop = outer.CancellationStop(cancellationToken); outerExpired = outer.Expired; }
        catch (Exception error) when (FindingFiles.IsAdmissionError(error))
        {
            stop = CampaignStop.WorkspaceFailure;
            // An admitted case may be the only remaining copy. Never clear it on an unrecorded infrastructure failure.
            if (current?.Candidate is not null) { workspace?.PreserveInput(); clean = false; }
        }
        finally
        {
            workspace?.Dispose();
            if (workspace is not null && !workspace.CleanupSucceeded) clean = false;
        }
        outerExpired |= lifetime.Expired;
        if (outerExpired && clean && stop != CampaignStop.Cancelled) stop = CampaignStop.OuterDeadline;
        if (!clean && stop != CampaignStop.RetentionFailure) stop = CampaignStop.CleanupFailure;
        var result = new CampaignResult(stop, results, run, finding, clean, current?.Worker?.Status, outerExpired);
        if (evidence is not null && !evidence.CanPublish)
        {
            evidence.Dispose();
            if (!evidence.CleanupSucceeded)
                result = new(CampaignStop.CleanupFailure, results, run, finding, false, current?.Worker?.Status, outerExpired);
        }
        if (evidence is { CanPublish: true })
        {
            try
            {
                CampaignResult committed = result.WithPublishedEvidence();
                evidence.Publish(committed, lifetime.ExpirationToken);
                result = committed;
            }
            catch (OperationCanceledException) when (lifetime.Expired)
            {
                result = new(stop is CampaignStop.RetentionFailure or CampaignStop.CleanupFailure or CampaignStop.Cancelled ? stop : CampaignStop.OuterDeadline,
                    results, run, finding, clean, current?.Worker?.Status, true);
            }
            catch (Exception error) when (FindingFiles.IsAdmissionError(error))
            { result = new(CampaignStop.PublicationFailure, results, run, finding, false, current?.Worker?.Status, outerExpired); }
            finally { evidence.Dispose(); }
            if (!evidence.CleanupSucceeded) result = new(CampaignStop.CleanupFailure, results, run, finding, false, current?.Worker?.Status, outerExpired);
        }
        // A committed child snapshot is immutable. Independent parent supervision must classify post-commit expiry
        // in its own invocation receipt, rather than returning a result that contradicts this snapshot.
        subsequentOuter?.Dispose();
        return result;
    }

    private static bool OwnedCorpus(CorpusValidationResult corpus) => corpus.IsValid && corpus.Seeds.Count == 15 &&
        corpus.Seeds.Count(seed => seed.Target == "filter-asciihex") == 6 &&
        corpus.Seeds.Count(seed => seed.Target == "filter-flate") == 6 &&
        corpus.Seeds.Count(seed => seed.Target == "font-sanitize") == 3;

    private async Task<CampaignStop> ExecuteTargetAsync(string root, string run, string revision, IReadOnlyList<CorpusSeed> seeds,
        string manifestHash, Session session, MutationWorkspace workspace, CampaignEvidence evidence,
        CancellationToken deadline, CancellationToken caller)
    {
        FuzzTarget target = TargetRegistry.Get(session.Target);
        foreach (CorpusSeed seed in seeds.Where(seed => seed.Target == session.Target))
        {
            if (session.Pool.Admission(seed.Length) != CampaignStop.None) return CampaignStop.PoolLimit;
            CampaignStop work = session.Budget.AdmitWork(CampaignWork.SeedUnits(seed.Length), session.Clock.Elapsed);
            if (work != CampaignStop.None) return work;
            ReadOnlyMemory<byte> bytes = await ReadSeedAsync(root, seed, target.MaximumInputBytes, deadline).ConfigureAwait(false);
            CampaignStop admitted = session.Pool.Add(seed.Id, bytes);
            if (admitted is not (CampaignStop.None or CampaignStop.Duplicate)) return admitted;
        }
        while (true)
        {
            deadline.ThrowIfCancellationRequested();
            if (session.Budget.Requests == session.Configuration.Cases) return CampaignStop.CaseLimit;
            if (session.AttemptPolicy.BeforeAttempt() != CampaignStop.None) return CampaignStop.DuplicateLimit;
            // Stop before any source copy or generation when even the maximum candidate cannot enter the pool.
            int parentIndex = session.AttemptPolicy.Attempts % session.Pool.Count;
            var description = session.Pool.Describe(parentIndex);
            int candidateCeiling = CampaignWork.CandidateCeiling(description.Length, target.MaximumInputBytes);
            if (session.Pool.Admission(candidateCeiling) != CampaignStop.None) return CampaignStop.PoolLimit;
            CampaignStop admission = session.Budget.AdmitWork(CampaignWork.ManifestUnits, session.Clock.Elapsed);
            if (admission != CampaignStop.None) return admission;
            ReadOnlyMemory<byte> manifest = await InputFile.ReadAsync(Path.Combine(root, "tests", "FuzzCorpus", "manifest.json"),
                CorpusValidator.MaximumManifestBytes, deadline).ConfigureAwait(false);
            if (FindingFiles.Digest(manifest) != manifestHash) return CampaignStop.IntegrityFailure;
            CorpusSeed original = seeds.Single(seed => seed.Target == session.Target && seed.Id == description.RootSeed);
            admission = session.Budget.AdmitWork(CampaignWork.AttemptUnits(description.Length, original.Length,
                target.MaximumInputBytes), session.Clock.Elapsed);
            if (admission != CampaignStop.None) return admission;
            admission = session.Pool.TrySource(parentIndex, session.Budget, session.Clock.Elapsed, out CampaignParent? selected);
            if (admission != CampaignStop.None) return admission;
            CampaignParent parent = selected!;
            _ = await ReadSeedAsync(root, original, target.MaximumInputBytes, deadline).ConfigureAwait(false);
            CampaignCandidate candidate = CampaignMutation.Generate(session.Target, parent.RootSeed, session.AttemptPolicy.Next(), parent.Bytes.Span);
            if (session.Pool.Contains(candidate.Identity.OutputSha256))
            {
                if (session.AttemptPolicy.Observe(true) == CampaignStop.DuplicateLimit) return CampaignStop.DuplicateLimit;
                continue;
            }
            _ = session.AttemptPolicy.Observe(false);
            if (!evidence.Reserve()) return CampaignStop.TelemetryLimit;
            session.Candidate = candidate;
            if (workerExecutor is null) await workspace.PrepareAsync(candidate.Bytes, target.MaximumInputBytes, deadline).ConfigureAwait(false);
            else await workspace.PrepareBrokeredAsync(candidate.Bytes, target.MaximumInputBytes, deadline).ConfigureAwait(false);
            deadline.ThrowIfCancellationRequested();
            session.Budget.Dispatched();
            SupervisionResult worker = workerExecutor is null ?
                await supervisor.RunAsync(session.Target, workspace.InputPath, 1, deadline).ConfigureAwait(false) :
                await workerExecutor.ExecuteAsync(new(session.Target, session.Budget.Requests - 1, candidate.Identity.Index,
                    candidate.Identity.OutputSha256, candidate.Bytes.Length, session.Budget.ProcessedBytes,
                    Math.Max(1, Math.Min(WorkerSupervisor.CaseTimeout.Ticks, (session.Configuration.Duration - session.Clock.Elapsed).Ticks))), deadline).ConfigureAwait(false);
            session.Worker = worker;
            bool completed = worker.Status == SupervisionStatus.Completed && worker.ExitCode is 0 or 1 && worker.WorkerCode == "TargetResult";
            if (completed || worker.Status == SupervisionStatus.WorkerFailure && worker.WorkerCode == "UnexpectedTargetFailure") session.Executed++;
            if (completed) { if (worker.ExitCode == 0) session.Successes++; else session.Rejections++; }
            evidence.AppendCaseReserved(CampaignControlJson.Case(candidate, worker));
            if (!completed) return worker.Status == SupervisionStatus.Cancelled ?
                caller.IsCancellationRequested ? CampaignStop.Cancelled : CampaignStop.Deadline :
                worker.Status == SupervisionStatus.TerminationFailed ? CampaignStop.CleanupFailure : CampaignStop.WorkerFailure;
            if (!workspace.CampaignContentsValid()) return CampaignStop.CleanupFailure;
            if (deadline.IsCancellationRequested) return caller.IsCancellationRequested ? CampaignStop.Cancelled : CampaignStop.Deadline;
            if (!workspace.ClearCase()) return CampaignStop.CleanupFailure;
            _ = session.Pool.Add(parent.RootSeed, candidate.Bytes);
            session.Candidate = null;
            session.Worker = null;
        }
    }

    private static async Task<ReadOnlyMemory<byte>> ReadSeedAsync(string root, CorpusSeed seed, int maximum, CancellationToken token)
    {
        if (seed.Length > maximum) throw new IOException("Owned seed exceeds target ceiling.");
        ReadOnlyMemory<byte> bytes = await InputFile.ReadAsync(Path.Combine(root, seed.Path), seed.Length, token).ConfigureAwait(false);
        if (bytes.Length != seed.Length || FindingFiles.Digest(bytes) != seed.Sha256) throw new IOException("Owned seed identity changed.");
        return bytes;
    }

    private async Task<bool> RetainAsync(string root, string run, string revision, string finding, CampaignStop stop, Session session,
        MutationWorkspace workspace, CancellationToken token)
    {
        try
        {
            CampaignCandidate candidate = session.Candidate!;
            // Prepare may have been cancelled before a complete write. Never publish partial candidate bytes.
            ReadOnlyMemory<byte> bytes = await InputFile.ReadAsync(workspace.InputPath,
                TargetRegistry.Get(session.Target).MaximumInputBytes, token).ConfigureAwait(false);
            if (!bytes.Span.SequenceEqual(candidate.Bytes)) return false;
            FindingClass observed = session.Worker is null ? FindingClass.IntegrityFailure :
                FindingReproduction.Classify(session.Worker.Status, session.Worker.ExitCode, session.Worker.WorkerCode);
            if (observed is FindingClass.Success or FindingClass.ExpectedRejection) observed = stop is CampaignStop.Cancelled or CampaignStop.Deadline ?
                FindingClass.Cancellation : stop == CampaignStop.IntegrityFailure ? FindingClass.IntegrityFailure : FindingClass.CleanupFailure;
            var source = new FindingSource(session.Target, 1, FindingKind.UnexpectedResult, observed,
                FindingOrigin.OwnedSeedMutation, finding, candidate.Identity.ParentSha256, "POST-P2-002", 1049,
                "neutral-net10-v1", revision, WorkerSupervisor.TargetIdentity, supervisor.WorkerIdentity);
            var store = new FindingStore(root, run);
            FindingResult retained = await store.RetainAsync(finding, "incoming/" + run + "/case.bin", source, token).ConfigureAwait(false);
            if (retained.ExitCode != FindingExit.Success) return false;
            FindingMetadata verified = await store.LoadAsync(finding, token).ConfigureAwait(false);
            return verified.Sha256 == candidate.Identity.OutputSha256 && verified.Length == candidate.Bytes.Length;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return false; }
        catch (Exception error) when (FindingFiles.IsAdmissionError(error)) { return false; }
    }

    private sealed class Session(string target, CampaignConfiguration configuration)
    {
        internal string Target { get; } = target;
        internal CampaignConfiguration Configuration { get; } = configuration;
        internal CampaignBudget Budget { get; } = new(configuration);
        internal CampaignPool Pool { get; } = new(configuration.PoolInputs, configuration.PoolBytes);
        internal Stopwatch Clock { get; } = Stopwatch.StartNew();
        internal CampaignAttempts AttemptPolicy { get; } = new();
        internal int Executed, Successes, Rejections;
        internal CampaignCandidate? Candidate;
        internal SupervisionResult? Worker;
        internal CampaignTargetResult Snapshot(CampaignStop stop) => new(Target, stop, AttemptPolicy.Attempts, Budget.Requests,
            Executed, Successes, Rejections, AttemptPolicy.Duplicates, Budget.ProcessedBytes, Pool.Count, Pool.Bytes, Clock.Elapsed.TotalSeconds);
    }
}
