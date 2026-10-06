using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

[Trait(TestPlatform.Key, TestPlatform.Windows)]
public sealed class FindingPromotionTests
{
    [Theory]
    [InlineData(PromotionDecision.CorpusFixedSuccess, false)]
    [InlineData(PromotionDecision.CorpusFixedRejection, true)]
    public async Task ReviewedPostFixOracleProducesCompleteStagedCorpusOnly(PromotionDecision decision, bool reject)
    {
        using var fixture = new FindingFixture();
        await fixture.Retain();
        Assert.Equal(FindingExit.ReproducedFailure, (await fixture.Reproduce()).ExitCode);
        byte[] originalManifest = File.ReadAllBytes(Path.Combine(fixture.Repository, "tests", "FuzzCorpus", "manifest.json"));
        byte[] prior = File.ReadAllBytes(fixture.FilePath("case-one", "reproduction-01.json"));
        fixture.Fixed();
        if (reject) File.WriteAllText(Path.Combine(fixture.Repository, "reject.flag"), "reject");
        FindingResult result = await Propose(fixture, decision);
        Assert.Equal(FindingExit.Success, result.ExitCode);
        string staged = Path.Combine(fixture.RunPath, "proposal");
        CorpusValidationResult corpus = await CorpusValidator.ValidateAsync(staged);
        Assert.True(corpus.IsValid);
        Assert.Equal(2, corpus.Seeds.Count);
        Assert.Equal(reject ? "rejected" : "success", corpus.Seeds.Single(seed => seed.Id == "regression-one").ExpectedOutcome);
        Assert.Equal(originalManifest, File.ReadAllBytes(Path.Combine(fixture.Repository, "tests", "FuzzCorpus", "manifest.json")));
        Assert.Equal(prior, File.ReadAllBytes(fixture.FilePath("case-one", "reproduction-01.json")));
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "tests", "FuzzCorpus", "filter-asciihex", "regression-one.bin")));
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "proposal-work")));
        Assert.Equal(FindingExit.PromotionRefused, (await Propose(fixture, decision)).ExitCode);
        Assert.True(File.Exists(Path.Combine(staged, "proposal.json")));
    }

    [Theory]
    [InlineData("unreproduced")]
    [InlineData("expected-rejection")]
    [InlineData("not-fixed")]
    [InlineData("tooling")]
    [InlineData("unknown-rights")]
    [InlineData("wrong-oracle")]
    [InlineData("unknown-finding")]
    [InlineData("tampered-evidence")]
    [InlineData("contradictory-evidence")]
    [InlineData("duplicate-id")]
    [InlineData("duplicate-hash")]
    [InlineData("invalid-corpus")]
    public async Task IneligibleProposalCannotChangePermanentState(string fault)
    {
        using var fixture = new FindingFixture();
        FindingSource source = FindingFixture.Source();
        byte[] bytes = "F0>"u8.ToArray();
        if (fault == "tooling") source = source with { Origin = FindingOrigin.ControlledProbe };
        if (fault == "unknown-finding") source = source with { Related = "" };
        if (fault == "expected-rejection") { bytes = "R"u8.ToArray(); source = source with { Observed = FindingClass.ExpectedRejection }; }
        await fixture.Retain(bytes: bytes, source: source);
        if (fault != "unreproduced") await fixture.Reproduce();
        if (fault != "not-fixed") fixture.Fixed();
        if (fault == "unknown-rights") fixture.Edit("case-one", json => json["rights"] = "unknown");
        if (fault is "tampered-evidence" or "contradictory-evidence")
        {
            string evidence = fixture.FilePath("case-one", "reproduction-01.json");
            var json = JsonNode.Parse(File.ReadAllText(evidence));
            if (fault == "tampered-evidence") json["sha256"] = new string('E', 64);
            else { json["attempts"][0]["code"] = "TargetResult"; json["attempts"][1]["code"] = "TargetResult"; }
            File.WriteAllText(evidence, json.ToJsonString());
        }
        string manifestPath = Path.Combine(fixture.Repository, "tests", "FuzzCorpus", "manifest.json");
        if (fault == "invalid-corpus") File.WriteAllText(manifestPath, "{}");
        if (fault == "duplicate-hash")
        {
            string seed = Path.Combine(fixture.Repository, "tests", "FuzzCorpus", "filter-asciihex", "base-seed.bin");
            File.WriteAllBytes(seed, bytes);
            var json = JsonNode.Parse(File.ReadAllText(manifestPath));
            json["seeds"][0]["length"] = bytes.Length;
            json["seeds"][0]["sha256"] = FindingFixture.Hash(bytes);
            File.WriteAllText(manifestPath, json.ToJsonString());
        }
        byte[] manifest = File.ReadAllBytes(manifestPath);
        PromotionDecision decision = fault == "wrong-oracle" ? PromotionDecision.CorpusFixedRejection : PromotionDecision.CorpusFixedSuccess;
        FindingResult result = await FindingPromotion.ProposeAsync(fixture.Store, "case-one",
            fault == "duplicate-id" ? "base-seed" : "regression-one", decision, FindingFixture.Supervisor());
        Assert.NotEqual(FindingExit.Success, result.ExitCode);
        Assert.Equal(manifest, File.ReadAllBytes(manifestPath));
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "proposal")));
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "proposal-work")));
    }

    [Fact]
    public async Task ToolingDefectCanRecommendOrdinaryTestWithoutCorpus()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain(source: FindingFixture.Source() with { Origin = FindingOrigin.ControlledProbe });
        await fixture.Reproduce();
        Assert.Equal(FindingExit.Success, (await Propose(fixture, PromotionDecision.OrdinaryToolingRegression)).ExitCode);
        Assert.True(File.Exists(Path.Combine(fixture.RunPath, "proposal", "proposal.json")));
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "proposal", "tests")));
    }

    [Fact]
    public async Task ExistingForeignStagingIsPreserved()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain();
        await fixture.Reproduce();
        fixture.Fixed();
        string staging = Path.Combine(fixture.RunPath, "proposal-work");
        Directory.CreateDirectory(staging);
        File.WriteAllText(Path.Combine(staging, "foreign.txt"), "preserve");
        Assert.Equal(FindingExit.InfrastructureFailure, (await Propose(fixture)).ExitCode);
        Assert.Equal("preserve", File.ReadAllText(Path.Combine(staging, "foreign.txt")));
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "proposal")));
    }

    [Fact]
    public async Task CancellationNeverPublishesPartialProposal()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain();
        await fixture.Reproduce();
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        FindingResult result = await FindingPromotion.ProposeAsync(fixture.Store, "case-one", "regression-one",
            PromotionDecision.OrdinaryEngineRegression, FindingFixture.Supervisor(), cancellation.Token);
        Assert.Equal(FindingExit.Cancelled, result.ExitCode);
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "proposal")));
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "proposal-work")));
    }

    private static Task<FindingResult> Propose(FindingFixture fixture, PromotionDecision decision = PromotionDecision.CorpusFixedSuccess) =>
        FindingPromotion.ProposeAsync(fixture.Store, "case-one", "regression-one", decision, FindingFixture.Supervisor());

    [Fact]
    public async Task ChangedCorpusDuringReproductionCleansPartiallyStagedProposal()
    {
        using var fixture = new FindingFixture();
        string manifestPath = Path.Combine(fixture.Repository, "tests", "FuzzCorpus", "manifest.json");
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath));
        JsonNode late = manifest["seeds"][0].DeepClone();
        late["id"] = "z-late"; late["path"] = "tests/FuzzCorpus/filter-asciihex/z-late.bin";
        late["sha256"] = FindingFixture.Hash([1]);
        manifest["seeds"].AsArray().Add(late);
        File.WriteAllBytes(Path.Combine(fixture.Repository, "tests", "FuzzCorpus", "filter-asciihex", "z-late.bin"), [1]);
        File.WriteAllText(manifestPath, manifest.ToJsonString());
        await fixture.Retain(); await fixture.Reproduce(); fixture.Fixed();
        File.WriteAllText(Path.Combine(fixture.Repository, "change-corpus.flag"), "controlled");
        byte[] originalManifest = File.ReadAllBytes(manifestPath);
        Assert.Equal(FindingExit.IntegrityFailure, (await Propose(fixture)).ExitCode);
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "proposal-work")));
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "proposal")));
        Assert.Equal(originalManifest, File.ReadAllBytes(manifestPath));
    }
}
