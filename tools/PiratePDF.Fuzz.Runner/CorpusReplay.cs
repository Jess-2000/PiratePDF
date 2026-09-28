using System.Security.Cryptography;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Validates the entire corpus, then rechecks and supervises each seed in ordinal order. Fail-fast.</summary>
public sealed class CorpusReplay
{
    private readonly WorkerSupervisor supervisor;

    public CorpusReplay(WorkerSupervisor supervisor)
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        this.supervisor = supervisor;
    }

    public async Task<ReplayResult> RunAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        // Explicit absolute roots avoid a changing current-directory dependency between validation and launch.
        if (string.IsNullOrWhiteSpace(repositoryRoot) || repositoryRoot.Length > 32767 ||
            repositoryRoot.Contains('\0') || !Path.IsPathFullyQualified(repositoryRoot))
            return new(ReplayExitCode.InvalidInvocation);

        int admitted = 0;
        int attempted = 0;
        var observations = new List<ReplayObservation>();
        CorpusSeed? current = null;
        try
        {
            CorpusValidationResult corpus = await CorpusValidator.ValidateAsync(repositoryRoot, cancellationToken).ConfigureAwait(false);
            if (!corpus.IsValid) return new(ReplayExitCode.IntegrityFailure, corpusFailure: corpus.Failure);
            admitted = corpus.Seeds.Count;
            foreach (CorpusSeed seed in corpus.Seeds)
            {
                current = seed;
                cancellationToken.ThrowIfCancellationRequested();
                string path = Path.Combine(repositoryRoot, "tests", "FuzzCorpus", seed.Target, seed.Id + ".bin");
                CorpusFailure identity = await RecheckAsync(path, seed, cancellationToken).ConfigureAwait(false);
                if (identity != CorpusFailure.None)
                    return new(ReplayExitCode.IntegrityFailure, admitted, attempted, observations, identity, failedSeed: seed);

                cancellationToken.ThrowIfCancellationRequested();
                // No intervening discovery or callbacks. The worker still reopens this path; this is not atomic.
                attempted++;
                SupervisionResult worker = await supervisor.RunAsync(seed.Target, path, seed.ConfigurationVersion,
                    cancellationToken).ConfigureAwait(false);
                if (worker.Status == SupervisionStatus.Cancelled)
                    return new(ReplayExitCode.Cancelled, admitted, attempted, observations, supervisionStatus: worker.Status, failedSeed: seed);
                if (worker.Status != SupervisionStatus.Completed || worker.ExitCode is not (0 or 1) ||
                    worker.WorkerCode != "TargetResult")
                    return new(ReplayExitCode.SupervisionFailure, admitted, attempted, observations, supervisionStatus: worker.Status, failedSeed: seed);

                string actual = worker.ExitCode == 0 ? "success" : "rejected";
                observations.Add(new(seed.Target, seed.Id, seed.ConfigurationVersion, seed.ExpectedOutcome, actual));
                if (actual != seed.ExpectedOutcome)
                    return new(ReplayExitCode.OracleMismatch, admitted, attempted, observations, failedSeed: seed);
            }
            cancellationToken.ThrowIfCancellationRequested();
            return new(ReplayExitCode.Success, admitted, attempted, observations);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(ReplayExitCode.Cancelled, admitted, attempted, observations, failedSeed: current);
        }
    }

    private static async Task<CorpusFailure> RecheckAsync(string path, CorpusSeed seed, CancellationToken token)
    {
        try
        {
            ReadOnlyMemory<byte> bytes = await InputFile.ReadAsync(path,
                TargetRegistry.Get(seed.Target).MaximumInputBytes, token).ConfigureAwait(false);
            if (bytes.Length != seed.Length) return CorpusFailure.LengthMismatch;
            return Convert.ToHexString(SHA256.HashData(bytes.Span)) == seed.Sha256 ? CorpusFailure.None : CorpusFailure.HashMismatch;
        }
        catch (InputTooLargeException) { return CorpusFailure.SeedTooLarge; }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException) { return CorpusFailure.MissingSeed; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return CorpusFailure.IoFailure; }
        catch (Exception error) when (error is ArgumentException or NotSupportedException) { return CorpusFailure.UnsafePath; }
    }
}
