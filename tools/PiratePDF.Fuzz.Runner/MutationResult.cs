using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

public enum MutationExitCode
{
    Completed = 0, InvalidRequest = 1, IntegrityFailure = 2, SupervisionFailure = 3,
    TimedOut = 4, Cancelled = 5, CleanupFailed = 6, TemporaryInputFailure = 7
}

public enum MutationFailure { None, Usage, Algorithm, Index, Target, Configuration, Seed, Budget, Corpus, SeedIdentity, TemporaryInput, Cleanup }

public sealed record MutationIdentity(string Target, string Seed, int ConfigurationVersion, string SourceSha256,
    int Index, string Operator);

public sealed record MutationObservation(MutationIdentity Identity, string Outcome);

/// <summary>Safe bounded metadata only. Attempted counts supervisor requests, completed counts valid target outcomes.</summary>
public sealed class MutationResult
{
    public const int MaximumJsonBytes = 1536;

    internal MutationResult(MutationExitCode exitCode, MutationFailure failure = MutationFailure.None, int admitted = 0,
        int attempted = 0, IReadOnlyList<MutationObservation>? observations = null, MutationIdentity? identity = null,
        CorpusFailure? corpusFailure = null, SupervisionStatus? supervisionStatus = null)
    {
        ExitCode = exitCode;
        Failure = failure;
        Admitted = admitted;
        Attempted = attempted;
        Observations = Array.AsReadOnly(observations is null ? [] : observations.ToArray());
        Successes = Observations.Count(static entry => entry.Outcome == "success");
        Rejections = Observations.Count(static entry => entry.Outcome == "rejected");
        Identity = identity;
        CorpusFailure = corpusFailure;
        SupervisionStatus = supervisionStatus;
        Json = JsonSerializer.Serialize(new
        {
            schemaVersion = 1, algorithm = DeterministicMutation.Algorithm, fixedSeed = DeterministicMutation.FixedSeed,
            status = exitCode.ToString(), exitCode = (int)exitCode, failure = failure.ToString(), admitted, attempted,
            completed = Completed, successes = Successes, rejections = Rejections,
            failures = exitCode is MutationExitCode.Completed or MutationExitCode.Cancelled ? 0 : 1,
            cancelled = exitCode == MutationExitCode.Cancelled, identity,
            corpusFailure = corpusFailure?.ToString(), supervisionStatus = supervisionStatus?.ToString()
        });
    }

    public MutationExitCode ExitCode { get; }
    public MutationFailure Failure { get; }
    public int Admitted { get; }
    public int Attempted { get; }
    public int Completed => Observations.Count;
    public int Successes { get; }
    public int Rejections { get; }
    public MutationIdentity? Identity { get; }
    public CorpusFailure? CorpusFailure { get; }
    public SupervisionStatus? SupervisionStatus { get; }
    public IReadOnlyList<MutationObservation> Observations { get; }
    public string Json { get; }
}
