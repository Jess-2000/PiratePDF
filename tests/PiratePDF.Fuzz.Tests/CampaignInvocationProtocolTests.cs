using System.Text;
using System.Text.Json;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class CampaignInvocationProtocolTests
{
    private static CampaignInvocationIdentity Identity() => new("protocol-test", new string('a', 40),
        "filter-asciihex", 2, TimeSpan.FromSeconds(1).Ticks, 16777216, 128, 8388608, 16777216,
        new string('A', 64), new string('B', 64), new string('C', 64), new string('D', 64), 123, 456);

    [Fact]
    public void CanonicalIdentityAndRegistrationRoundTripWithoutPrintingSecret()
    {
        var identity = Identity();
        Assert.Equal(identity, CampaignInvocationIdentity.Decode(identity.Encode()));
        var registration = CampaignWorkerRegistration.Create(identity, 1, 789, 1000);
        Assert.True(registration.Encode().Length <= CampaignWorkerRegistration.MaximumBytes);
        Assert.Equal(registration, CampaignWorkerRegistration.Decode(registration.Encode(), identity));
        Assert.DoesNotContain(identity.Nonce, identity.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(registration.Proof, registration.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    [InlineData(10)] [InlineData(11)] [InlineData(12)] [InlineData(13)] [InlineData(14)]
    public void RegistrationBindsEveryInvocationField(int field)
    {
        var identity = Identity();
        byte[] bytes = CampaignWorkerRegistration.Create(identity, 0, 789, 1000).Encode();
        var changed = field switch
        {
            0 => identity with { Run = "other" }, 1 => identity with { Revision = new string('b', 40) },
            2 => identity with { Target = "all" }, 3 => identity with { Cases = 3 },
            4 => identity with { DurationTicks = 2 }, 5 => identity with { ProcessedBytes = 1 },
            6 => identity with { PoolInputs = 1 }, 7 => identity with { PoolBytes = 1 },
            8 => identity with { TelemetryBytes = 16384 }, 9 => identity with { RepositoryHash = new string('E', 64) },
            10 => identity with { RunnerHash = new string('E', 64) }, 11 => identity with { TargetsHash = new string('E', 64) },
            12 => identity with { Nonce = new string('E', 64) }, 13 => identity with { ParentId = 124 },
            _ => identity with { ParentStartTicks = 457 }
        };
        Assert.Throws<ArgumentException>(() => CampaignWorkerRegistration.Decode(bytes, changed));
    }

    [Theory]
    [InlineData(-1, 789, 1000)] [InlineData(2, 789, 1000)] [InlineData(0, 0, 1000)]
    [InlineData(0, 123, 1000)] [InlineData(0, 789, 0)] [InlineData(0, 789, long.MaxValue)]
    public void InvalidWorkerFieldsAreRejected(int index, int pid, long ticks) =>
        Assert.Throws<ArgumentException>(() => CampaignWorkerRegistration.Create(Identity(), index, pid, ticks));

    [Fact]
    public void ForgedExactInstanceAndProofAreRejected()
    {
        var identity = Identity();
        var registration = CampaignWorkerRegistration.Create(identity, 0, 789, 1000);
        foreach (var forged in new[] { registration with { ProcessId = 790 }, registration with { StartTicks = 1001 },
            registration with { CaseIndex = 1 }, registration with { Proof = new string('0', 64) } })
            Assert.Throws<ArgumentException>(() => CampaignWorkerRegistration.Decode(forged.Encode(), identity));
    }

    [Theory]
    [InlineData("Run")] [InlineData("Version")] [InlineData("Algorithm")] [InlineData("Seed")]
    public void DuplicateOrUnknownIdentityPropertiesAreRejected(string property)
    {
        string json = Encoding.UTF8.GetString(Identity().Encode());
        byte[] bytes = Encoding.UTF8.GetBytes(json[..^1] + $",\"{property}\":null}}");
        AssertRejected(() => CampaignInvocationIdentity.Decode(bytes));
    }

    [Fact]
    public void BoundedMalformedAndNoncanonicalRecordsAreRejected()
    {
        var identity = Identity();
        byte[] registration = CampaignWorkerRegistration.Create(identity, 0, 789, 1000).Encode();
        foreach (byte[] bytes in new[] { Array.Empty<byte>(), new byte[2049], identity.Encode()[..^1],
            Encoding.UTF8.GetBytes(" " + Encoding.UTF8.GetString(identity.Encode())) })
            AssertRejected(() => CampaignInvocationIdentity.Decode(bytes));
        foreach (byte[] bytes in new[] { Array.Empty<byte>(), new byte[513], registration[..^1],
            Encoding.UTF8.GetBytes(" " + Encoding.UTF8.GetString(registration)) })
            AssertRejected(() => CampaignWorkerRegistration.Decode(bytes, identity));
    }

    [Fact]
    public void InvalidInvocationCannotBeEncoded()
    {
        foreach (var identity in new[] { Identity() with { Run = "../unsafe" }, Identity() with { Run = "pending" },
            Identity() with { Target = "foreign" }, Identity() with { Cases = 10001 },
            Identity() with { Nonce = "bad" }, Identity() with { ParentStartTicks = long.MaxValue },
            Identity() with { Version = 2 }, Identity() with { Algorithm = "foreign" }, Identity() with { Seed = 1 } })
            Assert.ThrowsAny<ArgumentException>(() => identity.Encode());
    }

    private static void AssertRejected(Action decode)
    {
        Exception error = Record.Exception(decode);
        Assert.True(error is ArgumentException or JsonException, $"Unexpected rejection: {error?.GetType().Name ?? "none"}");
    }
}
