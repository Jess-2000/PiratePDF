using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

internal sealed record FindingEvidence(string Schema, int Version, string RunId, string FindingId,
    string Sha256, int Length, string Target, int ConfigurationVersion, string Toolchain,
    string WorkerSha256, string TargetSha256, int Reproduction, bool Agrees, FindingAttempt[] Attempts)
{
    internal bool IsValidFor(FindingMetadata metadata, int index) =>
        Schema == "piratepdf-finding-reproduction" && Version == 1 && RunId == metadata.RunId &&
        FindingId == metadata.FindingId && Target == metadata.Source.Target && ConfigurationVersion == metadata.Source.ConfigurationVersion &&
        Sha256 == metadata.Sha256 && Length == metadata.Length && Toolchain == "neutral-net10-v1" &&
        FindingFiles.Hash(WorkerSha256) && FindingFiles.Hash(TargetSha256) && Reproduction == index &&
        Attempts is { Length: > 0 and <= FindingStore.RequiredAttempts } &&
        Attempts.Select((attempt, position) => attempt is not null && attempt.Attempt == position + 1 &&
            Consistent(attempt)).All(static valid => valid) &&
        Agrees == (Attempts.Length == 2 && Attempts[0].Classification == Attempts[1].Classification && Attempts[0].Code == Attempts[1].Code);

    private static bool Consistent(FindingAttempt attempt) => attempt.Classification switch
    {
        FindingClass.Success or FindingClass.ExpectedRejection => attempt.Code == "TargetResult",
        FindingClass.UnexpectedManagedFailure => attempt.Code == "UnexpectedTargetFailure",
        FindingClass.WorkerProtocolFailure or FindingClass.AbnormalExit or FindingClass.Timeout or
            FindingClass.Cancellation or FindingClass.CleanupFailure => attempt.Code == "NoWorkerRecord",
        FindingClass.Indeterminate => attempt.Code is "NoWorkerRecord" or "Cancelled" or "InputUnavailable" or
            "InputTooLarge" or "UnsupportedInputPlatform" or "UnexpectedRunnerFailure",
        _ => false
    };
}

/// <summary>Two fresh supervised workers, bounded identity checks, and no exception-message oracle.</summary>
public sealed class FindingReproduction(WorkerSupervisor supervisor)
{
    public async Task<FindingResult> RunAsync(FindingStore store, string findingId, CancellationToken cancellationToken = default)
    {
        try
        {
            using FileStream lease = store.Lock();
            return await RunLockedAsync(store, findingId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return new(FindingExit.Cancelled, FindingClass.Cancellation); }
        catch (Exception error) when (FindingFiles.IsAdmissionError(error)) { return FindingFiles.Failure(error); }
    }

    internal async Task<FindingResult> RunLockedAsync(FindingStore store, string id, CancellationToken token)
    {
        FindingMetadata metadata = await store.LoadAsync(id, token).ConfigureAwait(false);
        string directory = store.CandidatePath(id);
        string path = Path.Combine(directory, "input.bin");
        var attempts = new List<FindingAttempt>(FindingStore.RequiredAttempts);
        IReadOnlyList<FindingEvidence> previous = await store.ReadEvidenceAsync(metadata, token).ConfigureAwait(false);
        if (previous.Count == FindingStore.MaximumReproductions) return new(FindingExit.LimitExceeded);
        int reproduction = previous.Count + 1;
        FindingClass? interruption = null;
        try
        {
            for (int index = 0; index < FindingStore.RequiredAttempts; index++)
            {
                token.ThrowIfCancellationRequested();
                // Reload all metadata and bytes; never accept a stale discovery snapshot between workers.
                if (await store.LoadAsync(id, token).ConfigureAwait(false) != metadata)
                    throw new FindingException(FindingExit.IntegrityFailure);
                using var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                await FindingStore.VerifyAsync(path, metadata, token).ConfigureAwait(false);
                SupervisionResult worker = await supervisor.RunAsync(metadata.Source.Target, path,
                    metadata.Source.ConfigurationVersion, token).ConfigureAwait(false);
                FindingClass classification = Classify(worker.Status, worker.ExitCode, worker.WorkerCode);
                attempts.Add(new(index + 1, classification, worker.WorkerCode ?? "NoWorkerRecord"));
                if (classification is FindingClass.CleanupFailure or FindingClass.Cancellation or FindingClass.Indeterminate)
                    break;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { interruption = FindingClass.Cancellation; }
        catch (Exception error) when (FindingFiles.IsAdmissionError(error)) { interruption = FindingClass.IntegrityFailure; }
        bool agrees = attempts.Count == FindingStore.RequiredAttempts &&
            attempts[0].Classification == attempts[1].Classification && attempts[0].Code == attempts[1].Code;
        FindingClass outcome = interruption ?? (agrees ? attempts[0].Classification : attempts.Count == 2 ?
            FindingClass.NonReproducible : attempts[0].Classification);
        FindingExit exit = outcome switch
        {
            FindingClass.Success or FindingClass.ExpectedRejection => FindingExit.Success,
            FindingClass.NonReproducible => FindingExit.NonReproducible,
            FindingClass.Cancellation => FindingExit.Cancelled,
            FindingClass.IntegrityFailure => FindingExit.IntegrityFailure,
            FindingClass.CleanupFailure or FindingClass.Indeterminate => FindingExit.InfrastructureFailure,
            _ => FindingExit.ReproducedFailure
        };
        if (agrees && outcome != metadata.Source.Observed) exit = FindingExit.NonReproducible;
        var result = new FindingResult(exit, outcome, metadata, attempts, agrees);
        // With no child attempt there is no execution evidence to retain. Original bytes/metadata remain intact.
        if (attempts.Count == 0) return result;
        var evidence = new FindingEvidence("piratepdf-finding-reproduction", 1, metadata.RunId, id,
            metadata.Sha256, metadata.Length, metadata.Source.Target, metadata.Source.ConfigurationVersion,
            "neutral-net10-v1", supervisor.WorkerIdentity, WorkerSupervisor.TargetIdentity, reproduction, agrees, attempts.ToArray());
        byte[] evidenceBytes = JsonSerializer.SerializeToUtf8Bytes(evidence, FindingJson.Options);
        if (evidenceBytes.Length > FindingStore.MaximumEvidenceBytes) throw new FindingException(FindingExit.LimitExceeded);
        await FindingFiles.WriteNewAsync(Path.Combine(directory, FindingStore.EvidenceName(reproduction)),
            evidenceBytes, CancellationToken.None).ConfigureAwait(false);
        return result;
    }

    /// <summary>Maps bounded supervisor evidence; a generic managed failure is not proof of an engine defect.</summary>
    public static FindingClass Classify(SupervisionStatus status, int? exit, string? code) => status switch
    {
        SupervisionStatus.Completed when exit == 0 && code == "TargetResult" => FindingClass.Success,
        SupervisionStatus.Completed when exit == 1 && code == "TargetResult" => FindingClass.ExpectedRejection,
        SupervisionStatus.WorkerFailure when exit == 4 && code == "UnexpectedTargetFailure" => FindingClass.UnexpectedManagedFailure,
        SupervisionStatus.InvalidProtocol => FindingClass.WorkerProtocolFailure,
        SupervisionStatus.AbnormalExit => FindingClass.AbnormalExit,
        SupervisionStatus.TimedOut => FindingClass.Timeout,
        SupervisionStatus.Cancelled => FindingClass.Cancellation,
        SupervisionStatus.TerminationFailed => FindingClass.CleanupFailure,
        _ => FindingClass.Indeterminate
    };
}

/// <summary>Reviews one explicitly supplied smaller retained candidate; no search or external minimizer.</summary>
public static class FindingMinimization
{
    public const int MaximumExecutions = 4;
    public const int MaximumProcessedBytes = 4 * FindingStore.MaximumArtifactBytes;

    public static async Task<FindingResult> CompareAsync(FindingStore store, string originalId, string smallerId,
        WorkerSupervisor supervisor, CancellationToken cancellationToken = default)
    {
        try
        {
            using FileStream lease = store.Lock();
            FindingMetadata original = await store.LoadAsync(originalId, cancellationToken).ConfigureAwait(false);
            FindingMetadata smaller = await store.LoadAsync(smallerId, cancellationToken).ConfigureAwait(false);
            if (original.Source.Target != smaller.Source.Target || original.Source.ConfigurationVersion != smaller.Source.ConfigurationVersion ||
                original.Source.Related.Length == 0 || original.Source.Related != smaller.Source.Related ||
                smaller.Length >= original.Length || smaller.Source.ParentSha256 != original.Sha256 ||
                original.Source.Observed != FindingClass.UnexpectedManagedFailure || smaller.Source.Observed != original.Source.Observed)
                return new(FindingExit.MinimizationRefused);
            var reproduction = new FindingReproduction(supervisor);
            FindingResult first = await reproduction.RunLockedAsync(store, originalId, cancellationToken).ConfigureAwait(false);
            if (!first.Agrees || first.Classification != FindingClass.UnexpectedManagedFailure) return Refused(first);
            FindingResult second = await reproduction.RunLockedAsync(store, smallerId, cancellationToken).ConfigureAwait(false);
            if (!second.Agrees || second.Classification != first.Classification || second.Attempts[0].Code != first.Attempts[0].Code)
                return Refused(second);
            // This establishes class preservation only. Same-root-cause characterization remains an explicit human decision.
            return new(FindingExit.Success, second.Classification, smaller, second.Attempts, true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return new(FindingExit.Cancelled, FindingClass.Cancellation); }
        catch (Exception error) when (FindingFiles.IsAdmissionError(error)) { return FindingFiles.Failure(error); }
    }

    private static FindingResult Refused(FindingResult result) => result.ExitCode is FindingExit.Cancelled or FindingExit.InfrastructureFailure ? result :
        new(FindingExit.MinimizationRefused, result.Classification, result.Metadata, result.Attempts, result.Agrees);
}
