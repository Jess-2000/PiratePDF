using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

[Trait(TestPlatform.Key, TestPlatform.Windows)]
public sealed class FindingStorageTests
{
    [Fact]
    public async Task RetentionComputesIdentityAndPreservesDiscovery()
    {
        using var fixture = new FindingFixture();
        FindingResult retained = await fixture.Retain();
        Assert.Equal(FindingExit.Success, retained.ExitCode);
        Assert.Equal(FindingFixture.Hash("F0>"u8.ToArray()), retained.Metadata.Sha256);
        Assert.Equal(3, retained.Metadata.Length);
        Assert.Equal(retained.Metadata, await fixture.Store.LoadAsync("case-one"));
        Assert.Equal("unattempted", retained.Metadata.Reproduction);
    }

    [Theory]
    [InlineData("schema", "wrong")]
    [InlineData("version", "2")]
    [InlineData("extra", "PRIVATE_DATA")]
    [InlineData("target", "unknown-target")]
    [InlineData("configurationVersion", "2")]
    [InlineData("sha256", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("length", "4")]
    [InlineData("inputCeiling", "16385")]
    [InlineData("rights", "external")]
    [InlineData("origin", "Unreviewed")]
    [InlineData("originId", "../private")]
    [InlineData("reproduction", "reproduced")]
    [InlineData("minimization", "minimized")]
    [InlineData("promotion", "promoted")]
    [InlineData("runId", "other-run")]
    [InlineData("findingId", "other-finding")]
    [InlineData("toolchain", "https://untrusted.example")]
    [InlineData("kind", "Unknown")]
    [InlineData("observed", "Unknown")]
    [InlineData("related", "private-content\n")]
    public async Task MetadataContractFailsClosed(string field, string value)
    {
        using var fixture = new FindingFixture();
        await fixture.Retain();
        fixture.Edit("case-one", document =>
        {
            JsonObject container = field is "target" or "configurationVersion" or "origin" or "originId" or "toolchain" or "kind" or "observed" or "related" ?
                document["source"].AsObject() : document;
            container[field] = int.TryParse(value, out int number) ? JsonValue.Create(number) : JsonValue.Create(value);
        });
        FindingResult result = await fixture.Reproduce();
        Assert.Equal(FindingExit.IntegrityFailure, result.ExitCode);
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "finding-executions.log")));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("changed")]
    [InlineData("length")]
    [InlineData("oversized")]
    [InlineData("directory")]
    [InlineData("unknown-file")]
    [InlineData("duplicate-property")]
    [InlineData("missing-property")]
    [InlineData("null-source")]
    [InlineData("long-string")]
    [InlineData("metadata-size")]
    public async Task InvalidStorageNeverLaunches(string fault)
    {
        using var fixture = new FindingFixture();
        await fixture.Retain();
        string input = fixture.FilePath("case-one");
        string metadata = fixture.FilePath("case-one", "metadata.json");
        switch (fault)
        {
            case "missing": File.Delete(input); break;
            case "changed": File.WriteAllBytes(input, "A0>"u8.ToArray()); break;
            case "length": File.WriteAllBytes(input, [1]); break;
            case "oversized": File.WriteAllBytes(input, new byte[16385]); break;
            case "directory": File.Delete(input); Directory.CreateDirectory(input); break;
            case "unknown-file": File.WriteAllText(fixture.FilePath("case-one", "unknown.txt"), "private"); break;
            case "duplicate-property": File.WriteAllText(metadata, File.ReadAllText(metadata).Insert(1, "\"version\":1,")); break;
            case "missing-property": fixture.Edit("case-one", json => json.Remove("length")); break;
            case "null-source": fixture.Edit("case-one", json => json["source"] = null); break;
            case "long-string": fixture.Edit("case-one", json => json["source"]["originId"] = new string('a', 65)); break;
            case "metadata-size": File.WriteAllText(metadata, new string(' ', FindingStore.MaximumMetadataBytes + 1)); break;
        }
        Assert.Equal(FindingExit.IntegrityFailure, (await fixture.Reproduce()).ExitCode);
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "finding-executions.log")));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("incoming/../outside")]
    [InlineData("incoming/file:stream")]
    [InlineData("C:/private.bin")]
    [InlineData("\\\\server\\private")]
    [InlineData("incoming/a/b/c/d")]
    public async Task IncomingTraversalRefused(string input)
    {
        using var fixture = new FindingFixture();
        Assert.Equal(FindingExit.InvalidInvocation, (await fixture.Store.RetainAsync("case-one", input, FindingFixture.Source())).ExitCode);
    }

    [Fact]
    public async Task ExactAndOverTargetCeiling()
    {
        using var fixture = new FindingFixture();
        Assert.Equal(FindingExit.Success, (await fixture.Retain(bytes: new byte[16384])).ExitCode);
        Assert.Equal(FindingExit.IntegrityFailure, (await fixture.Retain("over-limit", new byte[16385])).ExitCode);
    }

    [Fact]
    public async Task DuplicateRetainedAndPermanentHashRefused()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain();
        Assert.Equal(FindingExit.Duplicate, (await fixture.Retain("other-case")).ExitCode);
        Assert.Equal(FindingExit.Duplicate, (await fixture.Retain("base-case", [62])).ExitCode);
    }

    [Fact]
    public async Task CountCeilingAndOrdinalOrder()
    {
        using var fixture = new FindingFixture();
        for (int index = 15; index >= 0; index--)
            Assert.Equal(FindingExit.Success, (await fixture.Retain("case-" + index.ToString("D2", System.Globalization.CultureInfo.InvariantCulture), [(byte)index])).ExitCode);
        IReadOnlyList<FindingMetadata> inventory = await fixture.Store.InventoryAsync();
        Assert.Equal(16, inventory.Count);
        Assert.Equal(inventory.Select(item => item.FindingId).Order(StringComparer.Ordinal), inventory.Select(item => item.FindingId));
        Assert.Equal(FindingExit.LimitExceeded, (await fixture.Retain("overflow", [200])).ExitCode);
    }

    [Fact]
    public async Task AggregateCeilingIsCheckedBeforePublication()
    {
        using var fixture = new FindingFixture();
        FindingSource source = FindingFixture.Source() with { Target = "font-sanitize" };
        for (int index = 0; index < 4; index++)
        {
            byte[] bytes = new byte[65536]; bytes[0] = (byte)index;
            Assert.Equal(FindingExit.Success, (await fixture.Retain("font-" + index, bytes, source)).ExitCode);
        }
        Assert.Equal(FindingExit.LimitExceeded, (await fixture.Retain("overflow", [200])).ExitCode);
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "overflow")));
    }

    [Fact]
    public async Task ArtifactLinkEscapeIsRejected()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain();
        string path = fixture.FilePath("case-one");
        string outside = Path.Combine(fixture.Repository, "outside.bin");
        File.WriteAllBytes(outside, "F0>"u8.ToArray());
        File.Delete(path);
        File.CreateSymbolicLink(path, outside);
        try { Assert.Equal(FindingExit.IntegrityFailure, (await fixture.Reproduce()).ExitCode); }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task UnreviewedProvenanceNotRetained()
    {
        using var fixture = new FindingFixture();
        Assert.Equal(FindingExit.IntegrityFailure, (await fixture.Retain(source: FindingFixture.Source() with { Origin = FindingOrigin.Unreviewed })).ExitCode);
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "case-one")));
    }
}
