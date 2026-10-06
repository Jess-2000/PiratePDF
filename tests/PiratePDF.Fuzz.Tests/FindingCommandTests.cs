using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class FindingCommandTests
{
    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task RetainAndReproduceRealTargetThroughStrictCommand()
    {
        using var fixture = new FindingFixture();
        string incoming = Path.Combine(fixture.Repository, "artifacts", "fuzz", "incoming");
        File.WriteAllBytes(Path.Combine(incoming, "crash-untrusted-name"), "F0>"u8.ToArray());
        File.WriteAllText(Path.Combine(incoming, "source.json"), JsonSerializer.Serialize(FindingFixture.Source(FindingClass.Success),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } }));
        string[] common = ["--repository-root", fixture.Repository, "--run", "owned-run", "--finding", "case-one"];
        FindingResult retained = await FindingCommand.ExecuteAsync(["retain-finding", .. common, "--input", "incoming/crash-untrusted-name", "--source", "incoming/source.json"]);
        Assert.Equal(FindingExit.Success, retained.ExitCode);
        FindingResult result = await FindingCommand.ExecuteAsync(["reproduce-finding", .. common]);
        Assert.Equal(FindingExit.Success, result.ExitCode);
        Assert.Equal(FindingClass.Success, result.Classification);
        Assert.True(result.MatchesDiscovery);
        Assert.DoesNotContain("crash-untrusted-name", result.Json, StringComparison.Ordinal);
        Assert.InRange(Encoding.UTF8.GetByteCount(result.Json), 1, 1536);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("executable")]
    [InlineData("unknown")]
    [InlineData("relative-root")]
    [InlineData("traversal-id")]
    [InlineData("reserved-id")]
    [InlineData("numeric-decision")]
    [InlineData("wrong-case-decision")]
    public async Task UntrustedOptionsCannotSelectExecutablesOrDestinations(string fault)
    {
        using var fixture = new ReplayFixture();
        string[] args = ["reproduce-finding", "--repository-root", fixture.Repository, "--run", "owned-run", "--finding", "case-one"];
        args = fault switch
        {
            "missing" => args[..5],
            "duplicate" => [.. args, "--run", "owned-run"],
            "executable" => [.. args, "--executable", "cmd.exe"],
            "unknown" => ["unknown-finding", .. args[1..]],
            "relative-root" => [args[0], args[1], ".", .. args[3..]],
            "traversal-id" => [.. args[..6], "../escape"],
            "reserved-id" => [.. args[..6], "proposal"],
            "numeric-decision" => ["propose-promotion", .. args[1..], "--seed", "seed-one", "--decision", "0"],
            _ => ["propose-promotion", .. args[1..], "--seed", "seed-one", "--decision", "ordinaryEngineRegression"]
        };
        Assert.False(FindingCommand.IsValidInvocation(args));
        Assert.Equal(FindingExit.InvalidInvocation, (await FindingCommand.ExecuteAsync(args)).ExitCode);
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "finding-executions.log")));
    }

    [Fact]
    public void ExitMappingsRemainExplicitAndDistinct() => Assert.Equal(
        new[] { 0, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 }, Enum.GetValues<FindingExit>().Select(value => (int)value));
}
