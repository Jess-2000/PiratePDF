using System.Text.Json;
using System.Text.Json.Nodes;

namespace PiratePDF.Fuzz.Runner;

public enum PromotionDecision { OrdinaryEngineRegression, OrdinaryToolingRegression, CorpusFixedSuccess, CorpusFixedRejection }

/// <summary>Publishes a review proposal only. Never writes the repository's permanent corpus or its oracle.</summary>
public static class FindingPromotion
{
    public static async Task<FindingResult> ProposeAsync(FindingStore store, string findingId, string seedId,
        PromotionDecision decision, WorkerSupervisor supervisor, CancellationToken cancellationToken = default)
    {
        var files = new List<string>();
        var directories = new List<string>();
        string? staging = null;
        FindingResult result;
        try
        {
            if (!FindingFiles.Id(seedId) || !Enum.IsDefined(decision)) return new(FindingExit.InvalidInvocation);
            using FileStream lease = store.Lock();
            FindingMetadata metadata = await store.LoadAsync(findingId, cancellationToken).ConfigureAwait(false);
            // Preserve refusal before reading any ledger for an uncharacterized finding.
            if (metadata.Source.Related.Length == 0) return new(FindingExit.PromotionRefused);
            // A persisted prior reproduction is required; metadata alone cannot assert reproducibility.
            IReadOnlyList<FindingEvidence> ledger = await store.ReadEvidenceAsync(metadata, cancellationToken).ConfigureAwait(false);
            FindingEvidence? prior = ledger.LastOrDefault(static item => item.Agrees && item.Attempts[0].Classification is not
                (FindingClass.Success or FindingClass.ExpectedRejection or FindingClass.Cancellation or FindingClass.Indeterminate or FindingClass.CleanupFailure));
            if (prior is null || !IsEligible(metadata, decision, prior.Attempts[0].Classification)) return new(FindingExit.PromotionRefused);
            bool corpusProposal = decision is PromotionDecision.CorpusFixedSuccess or PromotionDecision.CorpusFixedRejection;
            CorpusValidationResult corpus = await CorpusValidator.ValidateAsync(store.Repository, cancellationToken).ConfigureAwait(false);
            if (!corpus.IsValid) return new(FindingExit.IntegrityFailure, FindingClass.IntegrityFailure);
            if (corpus.Seeds.Any(seed => seed.Sha256 == metadata.Sha256 || seed.Id == seedId))
                return new(FindingExit.Duplicate, FindingClass.Duplicate, metadata);
            if (corpusProposal && corpus.Seeds.Count == CorpusValidator.MaximumEntries) return new(FindingExit.LimitExceeded);
            if (Path.Exists(Path.Combine(store.RunPath, "proposal"))) return new(FindingExit.PromotionRefused);
            FindingResult current = await new FindingReproduction(supervisor).RunLockedAsync(store, findingId, cancellationToken).ConfigureAwait(false);
            if (!current.Agrees) return current.ExitCode is FindingExit.Cancelled or FindingExit.InfrastructureFailure ? current : new(FindingExit.PromotionRefused);
            FindingClass expected = decision switch
            {
                PromotionDecision.CorpusFixedSuccess => FindingClass.Success,
                PromotionDecision.CorpusFixedRejection => FindingClass.ExpectedRejection,
                _ => prior.Attempts[0].Classification
            };
            if (current.Classification != expected) return new(FindingExit.PromotionRefused, current.Classification);

            staging = Path.Combine(store.RunPath, "proposal-work");
            if (Path.Exists(staging)) { staging = null; return new(FindingExit.InfrastructureFailure); }
            FindingFiles.CreateDirectory(staging);
            directories.Add(staging);
            if (corpusProposal)
                await StageCorpusAsync(store, staging, metadata, seedId, expected, corpus, files, directories, cancellationToken).ConfigureAwait(false);
            string proposalFile = Path.Combine(staging, "proposal.json");
            files.Add(proposalFile);
            await FindingFiles.WriteJsonAsync(proposalFile, new
            {
                schema = "piratepdf-finding-promotion-proposal", version = 1, decision,
                run = store.RunId, finding = findingId, metadata.Sha256, metadata.Length,
                related = metadata.Source.Related, origin = metadata.Source.Origin, metadata.Rights,
                seed = seedId, expected, reviewRequired = true, permanentWritePerformed = false,
                priorReproduction = prior, currentAttempts = current.Attempts
            }, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            FindingFiles.CheckDirectory(store.RunPath, false);
            Directory.Move(staging, Path.Combine(store.RunPath, "proposal"));
            staging = null;
            result = new(FindingExit.Success, current.Classification, metadata, current.Attempts, true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { result = new(FindingExit.Cancelled, FindingClass.Cancellation); }
        catch (Exception error) when (FindingFiles.IsAdmissionError(error)) { result = FindingFiles.Failure(error); }
        if (staging is not null && !FindingFiles.Cleanup(staging, files, directories))
            return new(FindingExit.InfrastructureFailure, FindingClass.CleanupFailure);
        return result;
    }

    /// <summary>Necessary metadata policy only; callers must still validate the ledger, bytes and fresh workers.</summary>
    public static bool IsEligible(FindingMetadata metadata, PromotionDecision decision, FindingClass? priorFailure)
    {
        if (!Enum.IsDefined(decision) || metadata.Source.Related.Length == 0 || priorFailure is null ||
            priorFailure is FindingClass.Success or FindingClass.ExpectedRejection or FindingClass.Cancellation or
                FindingClass.Indeterminate or FindingClass.CleanupFailure) return false;
        bool corpus = decision is PromotionDecision.CorpusFixedSuccess or PromotionDecision.CorpusFixedRejection;
        if (corpus && (metadata.Source.Origin is FindingOrigin.ControlledProbe or FindingOrigin.Unreviewed ||
            metadata.Source.Kind == FindingKind.ControlledFixture || priorFailure != FindingClass.UnexpectedManagedFailure ||
            metadata.Source.Observed != FindingClass.UnexpectedManagedFailure)) return false;
        return decision != PromotionDecision.OrdinaryEngineRegression ||
            metadata.Source.Origin != FindingOrigin.ControlledProbe && priorFailure == FindingClass.UnexpectedManagedFailure;
    }
    private static async Task StageCorpusAsync(FindingStore store, string staging, FindingMetadata metadata, string seedId,
        FindingClass expected, CorpusValidationResult corpus, List<string> files, List<string> directories, CancellationToken token)
    {
        string tests = Path.Combine(staging, "tests");
        string corpusRoot = Path.Combine(tests, "FuzzCorpus");
        foreach (string directory in new[] { tests, corpusRoot })
        {
            FindingFiles.CreateDirectory(directory);
            directories.Add(directory);
        }
        var seeds = new JsonArray();
        foreach (CorpusSeed seed in corpus.Seeds)
        {
            token.ThrowIfCancellationRequested();
            ReadOnlyMemory<byte> bytes = await InputFile.ReadAsync(Path.Combine(store.Repository, "tests", "FuzzCorpus", seed.Target, seed.Id + ".bin"),
                FindingStore.MaximumArtifactBytes, token).ConfigureAwait(false);
            if (bytes.Length != seed.Length || FindingFiles.Digest(bytes) != seed.Sha256) throw new FindingException(FindingExit.IntegrityFailure);
            await CopyAsync(seed.Target, seed.Id, bytes).ConfigureAwait(false);
            seeds.Add(JsonSerializer.SerializeToNode(seed, FindingJson.Options));
        }
        string input = Path.Combine(store.CandidatePath(metadata.FindingId), "input.bin");
        await FindingStore.VerifyAsync(input, metadata, token).ConfigureAwait(false);
        ReadOnlyMemory<byte> candidate = await InputFile.ReadAsync(input, metadata.InputCeiling, token).ConfigureAwait(false);
        if (FindingFiles.Digest(candidate) != metadata.Sha256 || candidate.Length != metadata.Length)
            throw new FindingException(FindingExit.IntegrityFailure);
        await CopyAsync(metadata.Source.Target, seedId, candidate).ConfigureAwait(false);
        seeds.Add(JsonSerializer.SerializeToNode(new CorpusSeed(seedId, metadata.Source.Target, metadata.Source.ConfigurationVersion,
            $"tests/FuzzCorpus/{metadata.Source.Target}/{seedId}.bin", metadata.Length, metadata.Sha256,
            "tests/FuzzCorpus/manifest.json", "repository-owned", "Repository-owned generated fixture; no external license is asserted.",
            "historical-regression", "Explicit reviewed finding " + metadata.Source.Related + "; see paired promotion proposal.",
            expected == FindingClass.Success ? "success" : "rejected", Array.AsReadOnly(new[] { metadata.Source.Related })), FindingJson.Options));
        // Bounded corpus count has already been checked; publish the canonical ordinal order only.
        var ordered = new JsonArray(seeds.OrderBy(static seed => seed!["path"]!.GetValue<string>(), StringComparer.Ordinal)
            .Select(static seed => seed!.DeepClone()).ToArray());
        byte[] manifest = JsonSerializer.SerializeToUtf8Bytes(new JsonObject
        {
            ["schema"] = "piratepdf-fuzz-corpus", ["version"] = 1, ["seeds"] = ordered
        });
        if (manifest.Length > CorpusValidator.MaximumManifestBytes) throw new FindingException(FindingExit.LimitExceeded);
        string path = Path.Combine(corpusRoot, "manifest.json");
        files.Add(path);
        await FindingFiles.WriteNewAsync(path, manifest, token).ConfigureAwait(false);
        CorpusValidationResult proposed = await CorpusValidator.ValidateAsync(staging, token).ConfigureAwait(false);
        if (!proposed.IsValid) throw new FindingException(FindingExit.PromotionRefused);

        async Task CopyAsync(string target, string id, ReadOnlyMemory<byte> bytes)
        {
            string directory = Path.Combine(corpusRoot, target);
            if (!Directory.Exists(directory)) { FindingFiles.CreateDirectory(directory); directories.Add(directory); }
            string output = Path.Combine(directory, id + ".bin");
            files.Add(output);
            await FindingFiles.WriteNewAsync(output, bytes, token).ConfigureAwait(false);
        }
    }
}
