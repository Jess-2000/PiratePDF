using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Immutable metadata snapshot, not a lease on file contents. Execution must revalidate bytes.</summary>
public sealed record CorpusSeed(string Id, string Target, int ConfigurationVersion, string Path,
    int Length, string Sha256, string SourceRecipe, string Origin, string Rights, string Category,
    string Purpose, string ExpectedOutcome, IReadOnlyList<string> Related);

public enum CorpusFailure
{
    None, UnsupportedPlatform, MissingManifest, ManifestTooLarge, InvalidManifest, UnsupportedSchema,
    InvalidFields, TooManyEntries, InvalidId, UnknownTarget, ConfigurationMismatch, InvalidPath,
    DuplicateId, DuplicatePath, InvalidOrdering, InvalidMetadata, InvalidVocabulary, SeedTooLarge,
    MissingSeed, LengthMismatch, HashMismatch, UnlistedFile, UnsafePath, TooManyFiles, IoFailure
}

public sealed class CorpusValidationResult
{
    internal CorpusValidationResult(CorpusFailure failure, IReadOnlyList<CorpusSeed>? seeds = null)
    {
        Failure = failure;
        Seeds = seeds ?? Array.Empty<CorpusSeed>();
    }

    public CorpusFailure Failure { get; }
    public IReadOnlyList<CorpusSeed> Seeds { get; }
    public bool IsValid => Failure == CorpusFailure.None;
    public string ToJson() => JsonSerializer.Serialize(new { status = Failure.ToString(), seedCount = Seeds.Count });
}

internal sealed class CorpusValidationException(CorpusFailure failure) : Exception
{
    internal CorpusFailure Failure { get; } = failure;
}
