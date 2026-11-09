using System.Security.Cryptography;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class CampaignPolicyTests
{
    [Fact]
    public void ExactConfigurationCeilingsAreAccepted()
    {
        var config = new CampaignConfiguration(10000, TimeSpan.FromSeconds(60));
        Assert.Equal(10000, config.Cases);
        Assert.Equal(TimeSpan.FromSeconds(60), config.Duration);
        Assert.Equal(16 * 1024 * 1024, config.ProcessedBytes);
        Assert.Equal(128, config.PoolInputs);
        Assert.Equal(8 * 1024 * 1024, config.PoolBytes);
        Assert.Equal(16 * 1024 * 1024, config.TelemetryBytes);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(10001, 1)]
    [InlineData(int.MaxValue, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 61)]
    [InlineData(1, int.MaxValue)]
    public void InvalidCountOrDeadlineIsRejected(int cases, int seconds) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignConfiguration(cases, TimeSpan.FromSeconds(seconds)));

    [Theory]
    [InlineData(0, 128, 8388608, 16777216)]
    [InlineData(16777217, 128, 8388608, 16777216)]
    [InlineData(16777216, 0, 8388608, 16777216)]
    [InlineData(16777216, 129, 8388608, 16777216)]
    [InlineData(16777216, 128, 0, 16777216)]
    [InlineData(16777216, 128, 8388609, 16777216)]
    [InlineData(16777216, 128, 8388608, 8191)]
    [InlineData(16777216, 128, 8388608, 16777217)]
    public void ResourceConfigurationCannotDisableOrIncreaseCaps(int processed, int count, int pool, int telemetry) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignConfiguration(1, TimeSpan.FromSeconds(1), processed, count, pool, telemetry));

    [Fact]
    public void AdmissionChecksCountTimeAndProcessedBytesBeforeWork()
    {
        var budget = new CampaignBudget(new CampaignConfiguration(1, TimeSpan.FromSeconds(1), 16384));
        Assert.Equal(CampaignStop.None, budget.Admit(16384, TimeSpan.Zero));
        Assert.Equal(CampaignStop.ProcessedByteLimit, budget.Admit(1, TimeSpan.Zero));
        budget.Dispatched();
        Assert.Equal(CampaignStop.CaseLimit, budget.Admit(0, TimeSpan.Zero));
        var timed = new CampaignBudget(new CampaignConfiguration(2, TimeSpan.FromSeconds(1)));
        Assert.Equal(CampaignStop.Deadline, timed.Admit(0, TimeSpan.FromSeconds(1)));
        Assert.Equal(0, timed.Requests);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65537)]
    [InlineData(int.MaxValue)]
    public void HostileInputLengthIsRejectedWithoutAccounting(int length)
    {
        var budget = new CampaignBudget(new CampaignConfiguration(1, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => budget.Admit(length, TimeSpan.Zero));
        Assert.Equal(0, budget.ProcessedBytes);
    }

    [Fact]
    public void ExactAggregateProcessedLimitAndOneMoreByte()
    {
        var budget = new CampaignBudget(new CampaignConfiguration(10000, TimeSpan.FromSeconds(60)));
        for (int index = 0; index < 256; index++) Assert.Equal(CampaignStop.None, budget.Admit(65536, TimeSpan.Zero));
        Assert.Equal(16777216, budget.ProcessedBytes);
        Assert.Equal(CampaignStop.ProcessedByteLimit, budget.Admit(1, TimeSpan.Zero));
    }

    [Fact]
    public void PoolOwnsBytesAndEnforcesExactCountAndByteLimits()
    {
        var pool = new CampaignPool(128, 8388608);
        for (int index = 0; index < 128; index++)
        {
            byte[] input = new byte[65536];
            input[0] = (byte)index;
            Assert.Equal(CampaignStop.None, pool.Add("owned", input));
            input[0] = 255;
        }
        Assert.Equal(128, pool.Count);
        Assert.Equal(8388608, pool.Bytes);
        Assert.Equal(0, pool.Source(0).Bytes.Span[0]);
        Assert.Equal(CampaignStop.PoolLimit, pool.Add("owned", new byte[] { 255 }));
        var bytes = new CampaignPool(128, 1);
        Assert.Equal(CampaignStop.None, bytes.Add("owned", new byte[] { 1 }));
        Assert.Equal(CampaignStop.PoolLimit, bytes.Add("owned", new byte[] { 2 }));
    }

    [Fact]
    public void DuplicateBytesAreNotNewPoolEntries()
    {
        var pool = new CampaignPool(128, 8388608);
        Assert.Equal(CampaignStop.None, pool.Add("owned", new byte[] { 1 }));
        Assert.Equal(CampaignStop.Duplicate, pool.Add("owned", new byte[] { 1 }));
        Assert.Equal(1, pool.Count);
    }

    [Fact]
    public void TelemetryReservesTerminalSpaceAndBoundsRecordsAndInventory()
    {
        var telemetry = new CampaignTelemetryBudget(16777216);
        for (int index = 0; index < 4094; index++) Assert.True(telemetry.Reserve(4096));
        Assert.False(telemetry.Reserve(1));
        Assert.True(telemetry.ReserveTerminal(8192));
        Assert.False(telemetry.ReserveTerminal(1));
        Assert.False(new CampaignTelemetryBudget(8192).Reserve(4097));
        var records = new CampaignTelemetryBudget(16777216);
        for (int index = 0; index < CampaignTelemetryBudget.MaximumRecords; index++) Assert.True(records.Reserve(1));
        Assert.False(records.Reserve(1));
    }

    [Theory]
    [InlineData("filter-asciihex", 16384)]
    [InlineData("filter-flate", 16384)]
    [InlineData("font-sanitize", 65536)]
    public void V2IsRepeatableAndNeverExceedsRegisteredLength(string target, int maximum)
    {
        byte[] input = new byte[maximum];
        for (int index = 0; index < 16; index++)
        {
            var first = CampaignMutation.Generate(target, "owned", index, input);
            var second = CampaignMutation.Generate(target, "owned", index, input);
            Assert.Equal(first.Identity, second.Identity);
            Assert.Equal(first.Bytes, second.Bytes);
            Assert.InRange(first.Bytes.Length, 0, maximum);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(input)), first.Identity.ParentSha256);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(first.Bytes)), first.Identity.OutputSha256);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignMutation.Generate(target, "owned", 0, new byte[maximum + 1]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(80000)]
    [InlineData(int.MaxValue)]
    public void CaseIndexIsFinite(int index) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignMutation.Generate("filter-asciihex", "owned", index, []));

    [Theory]
    [InlineData("--cases", "0")]
    [InlineData("--cases", "10001")]
    [InlineData("--cases", "01")]
    [InlineData("--cases", "+1")]
    [InlineData("--cases", "2147483648")]
    [InlineData("--seconds", "61")]
    [InlineData("--seconds", "NaN")]
    [InlineData("--target", "unknown")]
    [InlineData("--config", "v2")]
    [InlineData("--algorithm", "deterministic-v1")]
    [InlineData("--seed", "1050")]
    [InlineData("--run", "../escape")]
    [InlineData("--revision", "not-a-commit")]
    public async Task BadOptionsFailBeforeFileAccess(string key, string value)
    {
        string[] args = Arguments(Path.GetFullPath("missing-repository"), "owned-run");
        args[Array.IndexOf(args, key) + 1] = value;
        Assert.Equal(CampaignStop.InvalidInvocation, (await CampaignCommand.ExecuteAsync(args)).Stop);
    }

    [Fact]
    public async Task DuplicateUnknownAndMissingFlagsAreRejected()
    {
        string[] args = Arguments(Path.GetFullPath("missing"), "owned-run");
        Assert.Equal(CampaignStop.InvalidInvocation, (await CampaignCommand.ExecuteAsync([.. args, "--cases", "1"])).Stop);
        Assert.Equal(CampaignStop.InvalidInvocation, (await CampaignCommand.ExecuteAsync([.. args, "--other", "1"])).Stop);
        Assert.Equal(CampaignStop.InvalidInvocation, (await CampaignCommand.ExecuteAsync(args[..^2])).Stop);
        Assert.Equal(CampaignStop.InvalidInvocation, (await CampaignCommand.ExecuteAsync([])).Stop);
    }

    internal static string[] Arguments(string repository, string run) =>
        ["campaign-mutate", "--repository-root", repository, "--run", run, "--target", "filter-asciihex", "--config", "v1",
         "--algorithm", "deterministic-v2", "--seed", "1049", "--cases", "1", "--seconds", "10", "--revision",
         "f51824dbc031b58497a38a6292ee11568b9ffeda"];
}
