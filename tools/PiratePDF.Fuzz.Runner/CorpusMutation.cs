using System.Security.Cryptography;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Finite corpus-derived mutations, always through a fresh supervised worker.</summary>
public sealed class CorpusMutation
{
    private readonly WorkerSupervisor supervisor;

    public CorpusMutation(WorkerSupervisor supervisor)
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        this.supervisor = supervisor;
    }

    public Task<MutationResult> RunBatchAsync(string repositoryRoot, string algorithm, int perSeed,
        CancellationToken cancellationToken = default) => ExecuteAsync(repositoryRoot, algorithm, perSeed, null, cancellationToken);

    public Task<MutationResult> RunOneAsync(string repositoryRoot, string algorithm, string target, string seed,
        int configurationVersion, int index, CancellationToken cancellationToken = default) =>
        ExecuteAsync(repositoryRoot, algorithm, 1, new Selection(target, seed, configurationVersion, index), cancellationToken);

    private async Task<MutationResult> ExecuteAsync(string root, string algorithm, int perSeed, Selection? selection, CancellationToken token)
    {
        if (algorithm != DeterministicMutation.Algorithm) return new(MutationExitCode.InvalidRequest, MutationFailure.Algorithm);
        if (perSeed is < 1 or > DeterministicMutation.MutationsPerSeed || selection?.Index is < 0 or >= DeterministicMutation.MutationsPerSeed)
            return new(MutationExitCode.InvalidRequest, MutationFailure.Index);
        if (string.IsNullOrWhiteSpace(root) || root.Length > 32767 || root.Contains('\0') || !Path.IsPathFullyQualified(root))
            return new(MutationExitCode.InvalidRequest, MutationFailure.Usage);
        if (selection is not null)
        {
            FuzzTarget target;
            try { target = TargetRegistry.Get(selection.Target); }
            catch (ArgumentException) { return new(MutationExitCode.InvalidRequest, MutationFailure.Target); }
            if (selection.Version != target.ConfigurationVersion) return new(MutationExitCode.InvalidRequest, MutationFailure.Configuration);
            if (string.IsNullOrEmpty(selection.Seed) || selection.Seed.Length > 64) return new(MutationExitCode.InvalidRequest, MutationFailure.Seed);
        }

        CorpusValidationResult corpus;
        try { corpus = await CorpusValidator.ValidateAsync(root, token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return new(MutationExitCode.Cancelled); }
        if (!corpus.IsValid) return new(MutationExitCode.IntegrityFailure, MutationFailure.Corpus, corpusFailure: corpus.Failure);
        IReadOnlyList<CorpusSeed> seeds = corpus.Seeds;
        if (selection is not null)
        {
            CorpusSeed? seed = seeds.FirstOrDefault(seed => seed.Target == selection.Target && seed.Id == selection.Seed);
            if (seed is null) return new(MutationExitCode.InvalidRequest, MutationFailure.Seed, corpus.Seeds.Count);
            seeds = [seed];
        }
        // Validate the whole requested work count before staging or launching anything. Never silently truncate.
        if (seeds.Count > DeterministicMutation.MaximumAttempts / perSeed)
            return new(MutationExitCode.InvalidRequest, MutationFailure.Budget, corpus.Seeds.Count);

        var workspace = new MutationWorkspace(root);
        MutationResult result;
        try { result = await ExecuteCasesAsync(root, corpus.Seeds.Count, seeds, perSeed, selection?.Index, workspace, token).ConfigureAwait(false); }
        finally { workspace.Dispose(); }
        return workspace.CleanupSucceeded ? result : new(MutationExitCode.CleanupFailed, MutationFailure.Cleanup, result.Admitted,
            result.Attempted, result.Observations, result.Identity, result.CorpusFailure, result.SupervisionStatus);
    }

    private async Task<MutationResult> ExecuteCasesAsync(string root, int admitted, IReadOnlyList<CorpusSeed> seeds,
        int perSeed, int? selectedIndex, MutationWorkspace workspace, CancellationToken token)
    {
        int attempted = 0;
        var observations = new List<MutationObservation>();
        MutationIdentity? identity = null;
        SupervisionStatus? supervision = null;
        try
        {
            token.ThrowIfCancellationRequested();
            workspace.Open();
            foreach (CorpusSeed seed in seeds)
            {
                FuzzTarget target = TargetRegistry.Get(seed.Target);
                for (int offset = 0; offset < perSeed; offset++)
                {
                    token.ThrowIfCancellationRequested();
                    int index = selectedIndex ?? offset;
                    identity = new(seed.Target, seed.Id, seed.ConfigurationVersion, seed.Sha256,
                        index, DeterministicMutation.Operator(index).ToString());
                    ReadOnlyMemory<byte> source;
                    try
                    {
                        source = await InputFile.ReadAsync(Path.Combine(root, seed.Path), target.MaximumInputBytes, token).ConfigureAwait(false);
                        if (source.Length != seed.Length || Convert.ToHexString(SHA256.HashData(source.Span)) != seed.Sha256)
                            return new(MutationExitCode.IntegrityFailure, MutationFailure.SeedIdentity, admitted, attempted, observations, identity);
                    }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
                    {
                        return new(MutationExitCode.IntegrityFailure, MutationFailure.SeedIdentity, admitted, attempted, observations, identity);
                    }
                    byte[] bytes = DeterministicMutation.Generate(seed.Target, seed.Id, seed.ConfigurationVersion, index, source.Span);
                    await workspace.PrepareAsync(bytes, target.MaximumInputBytes, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    attempted++;
                    SupervisionResult worker = await supervisor.RunAsync(seed.Target, workspace.InputPath, seed.ConfigurationVersion, token).ConfigureAwait(false);
                    supervision = worker.Status;
                    bool completed = worker.Status == SupervisionStatus.Completed && worker.ExitCode is 0 or 1 && worker.WorkerCode == "TargetResult";
                    if (completed) observations.Add(new(identity, worker.ExitCode == 0 ? "success" : "rejected"));
                    if (!workspace.ClearCase()) return new(MutationExitCode.CleanupFailed, MutationFailure.Cleanup, admitted,
                        attempted, observations, identity, supervisionStatus: supervision);
                    if (!completed)
                    {
                        MutationExitCode exit = worker.Status switch
                        {
                            SupervisionStatus.Cancelled => MutationExitCode.Cancelled,
                            SupervisionStatus.TimedOut => MutationExitCode.TimedOut,
                            SupervisionStatus.TerminationFailed => MutationExitCode.CleanupFailed,
                            _ => MutationExitCode.SupervisionFailure
                        };
                        return new(exit, admitted: admitted, attempted: attempted, observations: observations,
                            identity: identity, supervisionStatus: supervision);
                    }
                }
            }
            token.ThrowIfCancellationRequested();
            return new(MutationExitCode.Completed, admitted: admitted, attempted: attempted, observations: observations,
                identity: selectedIndex.HasValue ? identity : null);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return new(MutationExitCode.Cancelled, admitted: admitted, attempted: attempted, observations: observations, identity: identity);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new(MutationExitCode.TemporaryInputFailure, MutationFailure.TemporaryInput, admitted, attempted, observations, identity,
                supervisionStatus: supervision);
        }
    }

    private sealed record Selection(string Target, string Seed, int Version, int Index);
}
