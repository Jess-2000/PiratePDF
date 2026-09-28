using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

public enum ReplayExitCode
{
    Success = 0,
    IntegrityFailure = 1,
    OracleMismatch = 2,
    SupervisionFailure = 3,
    Cancelled = 4,
    InvalidInvocation = 5
}

/// <summary>One completed, protocol-validated oracle comparison; no child observations or bytes.</summary>
public sealed record ReplayObservation(string Target, string Seed, int ConfigurationVersion,
    string Expected, string Actual);

/// <summary>Bounded summary plus at most 128 ordered comparisons from validated manifest metadata.</summary>
public sealed class ReplayResult
{
    public const int MaximumJsonBytes = 768;

    internal ReplayResult(ReplayExitCode exitCode, int admitted = 0, int attempted = 0,
        IReadOnlyList<ReplayObservation>? observations = null, CorpusFailure? corpusFailure = null,
        SupervisionStatus? supervisionStatus = null, CorpusSeed? failedSeed = null)
    {
        ExitCode = exitCode;
        Admitted = admitted;
        Attempted = attempted;
        Observations = Array.AsReadOnly(observations is null ? [] : observations.ToArray());
        Matched = Observations.Count(static entry => entry.Expected == entry.Actual);
        CorpusFailure = corpusFailure;
        SupervisionStatus = supervisionStatus;
        Json = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            manifestSchema = "piratepdf-fuzz-corpus",
            manifestVersion = 1,
            status = exitCode.ToString(),
            exitCode = (int)exitCode,
            admitted,
            attempted,
            executed = Executed,
            matched = Matched,
            mismatched = Mismatched,
            corpusFailure = corpusFailure?.ToString(),
            supervisionStatus = supervisionStatus?.ToString(),
            target = failedSeed?.Target,
            seed = failedSeed?.Id
        });
    }

    public ReplayExitCode ExitCode { get; }
    public int Admitted { get; }
    public int Attempted { get; }
    /// <summary>Completed target success/rejection outcomes; infrastructure attempts are counted separately.</summary>
    public int Executed => Observations.Count;
    public int Matched { get; }
    public int Mismatched => Executed - Matched;
    public CorpusFailure? CorpusFailure { get; }
    public SupervisionStatus? SupervisionStatus { get; }
    public IReadOnlyList<ReplayObservation> Observations { get; }
    public string Json { get; }
}
