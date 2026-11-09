using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

[Trait(TestPlatform.Key, TestPlatform.Windows)]
public sealed class CampaignEvidenceTests
{
    private static string Pending(CampaignFixture fixture, string run = "owned-run") =>
        Path.Combine(fixture.Repository, "artifacts", "fuzz", "campaigns", "." + run + "-pending");

    [Fact]
    public async Task CommittedDirectoryContainsMatchingTerminalAndSummaryWithoutLiveLease()
    {
        using var fixture = new CampaignFixture();
        CampaignResult result = await fixture.Run();
        Assert.True(result.EvidencePublished);
        string final = fixture.Evidence("owned-run");
        Assert.Equal(new[] { "ledger.jsonl", "summary.json" }, Directory.GetFiles(final).Select(Path.GetFileName).Order().ToArray());
        Assert.False(Directory.Exists(Pending(fixture)));
        Assert.Equal(result.Json, File.ReadAllText(Path.Combine(final, "summary.json")));
        Assert.Equal(result.Json, File.ReadAllLines(Path.Combine(final, "ledger.jsonl")).Last());
    }

    [Theory]
    [InlineData("publication-summary")]
    [InlineData("publication-rename")]
    [InlineData("failure-publication-rename")]
    public async Task FailedWriteOrRenameCannotPublishAConflictingTerminal(string mode)
    {
        using var fixture = new CampaignFixture(mode);
        CampaignResult result = await fixture.Run(config: new(1, TimeSpan.FromSeconds(10)));
        Assert.Equal(CampaignStop.PublicationFailure, result.Stop);
        Assert.Equal(8, result.ExitCode);
        Assert.False(result.EvidencePublished);
        Assert.True(Directory.Exists(Pending(fixture)));
        Assert.False(File.Exists(Path.Combine(fixture.Evidence("owned-run"), "ledger.jsonl")));
        Assert.False(File.Exists(Path.Combine(fixture.Evidence("owned-run"), "summary.json")));
        if (mode == "publication-summary")
            Assert.Equal("owned fixture", File.ReadAllText(Path.Combine(Pending(fixture), "summary.json")));
        else
            Assert.Equal("owned fixture", File.ReadAllText(Path.Combine(fixture.Evidence("owned-run"), "foreign.txt")));
        if (mode == "failure-publication-rename")
        {
            string payload = Path.Combine(fixture.Finding("owned-run"), "input.bin");
            Assert.Equal(File.ReadAllBytes(Path.Combine(fixture.Repository, "last-candidate.bin")), File.ReadAllBytes(payload));
            Assert.False(File.Exists(fixture.Incoming("owned-run")));
        }
    }

    [Fact]
    public async Task ExistingPendingDirectoryIsNeverReusedOrModified()
    {
        using var fixture = new CampaignFixture();
        Directory.CreateDirectory(Pending(fixture));
        string foreign = Path.Combine(Pending(fixture), "foreign.txt");
        File.WriteAllText(foreign, "preserve me");
        CampaignResult result = await fixture.Run();
        Assert.Equal(CampaignStop.WorkspaceFailure, result.Stop);
        Assert.False(result.EvidencePublished);
        Assert.Equal("preserve me", File.ReadAllText(foreign));
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "campaign-executions.log")));
    }

    [Fact]
    public async Task EightDuplicatesStopReproduciblyWithoutWorkerRequestsOrCaseWrites()
    {
        using var fixture = new CampaignFixture();
        ConfigureEmptyDuplicateRecipe(fixture);
        Assert.True((await CorpusValidator.ValidateAsync(fixture.Repository)).IsValid);
        for (int index = 0; index < 8; index++)
            Assert.Empty(CampaignMutation.Generate("filter-asciihex", "duplicate-006086", index, []).Bytes);
        CampaignResult first = await fixture.Run(config: new(10, TimeSpan.FromSeconds(10)));
        CampaignResult second = await fixture.Run("repeated-run", new(10, TimeSpan.FromSeconds(10)));
        foreach (CampaignResult result in new[] { first, second })
        {
            Assert.Equal(CampaignStop.DuplicateLimit, result.Stop);
            var target = Assert.Single(result.Targets);
            Assert.Equal(8, target.Attempts);
            Assert.Equal(8, target.Duplicates);
            Assert.Equal(0, target.WorkerRequests);
            Assert.Equal(0, target.Executed);
            Assert.Equal(1, target.PoolInputs);
            Assert.Equal(0, target.PoolBytes);
            Assert.True(result.EvidencePublished);
            string[] ledger = File.ReadAllLines(Path.Combine(fixture.Evidence(result.Run), "ledger.jsonl"));
            Assert.Equal(2, ledger.Length);
            Assert.Equal("piratepdf-mutation-campaign-start", JsonDocument.Parse(ledger[0]).RootElement.GetProperty("schema").GetString());
        }
        Assert.Equal(first.Targets.Single().ProcessedBytes, second.Targets.Single().ProcessedBytes);
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "campaign-executions.log")));
        Assert.True((await CorpusValidator.ValidateAsync(fixture.Repository)).IsValid);
    }

    private static void ConfigureEmptyDuplicateRecipe(CampaignFixture fixture)
    {
        JsonObject manifest = JsonNode.Parse(File.ReadAllText(fixture.Manifest)).AsObject();
        JsonArray seeds = manifest["seeds"].AsArray();
        int index = 0;
        foreach (JsonNode seed in seeds.Where(seed => seed["target"].GetValue<string>() == "filter-asciihex"))
        {
            string old = Path.GetFullPath(Path.Combine(fixture.Repository, seed["path"].GetValue<string>()));
            string id = index++ == 0 ? "duplicate-006086" : "z-duplicate-" + index;
            string relative = "tests/FuzzCorpus/filter-asciihex/" + id + ".bin";
            string destination = Path.GetFullPath(Path.Combine(fixture.Repository, relative));
            string ownedPrefix = fixture.Repository + Path.DirectorySeparatorChar;
            if (!old.StartsWith(ownedPrefix, StringComparison.Ordinal) || !destination.StartsWith(ownedPrefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Fixture path escaped its owned temporary repository.");
            File.Move(old, destination);
            File.WriteAllBytes(destination, []);
            seed["id"] = id;
            seed["path"] = relative;
            seed["length"] = 0;
            seed["sha256"] = Convert.ToHexString(SHA256.HashData([]));
            seed["expectedOutcome"] = "rejected";
            seed["sourceRecipe"] = "tests/PiratePDF.Fuzz.Tests/CampaignEvidenceTests.cs";
        }
        manifest["seeds"] = new JsonArray(seeds.OrderBy(seed => seed["path"].GetValue<string>(), StringComparer.Ordinal)
            .Select(seed => seed.DeepClone()).ToArray());
        File.WriteAllText(fixture.Manifest, manifest.ToJsonString());
    }
}
