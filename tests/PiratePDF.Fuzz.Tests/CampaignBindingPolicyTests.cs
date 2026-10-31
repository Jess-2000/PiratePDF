using System.Text;
using System.Text.Json;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class CampaignBindingPolicyTests
{
    private static CampaignInvocationIdentity Identity() => new("binding-test", new string('a', 40), "all", 2,
        TimeSpan.FromSeconds(1).Ticks, 16777216, 128, 8388608, 16777216, new string('A', 64), new string('B', 64),
        new string('C', 64), new string('D', 64), 123, 456);

    [Fact]
    public void BindingProofAndExactChildInstanceAreRequired()
    {
        var identity = Identity();
        var child = new CampaignProcessIdentity(789, 1000);
        var binding = CampaignTicketBinding.Create(identity, child);
        Assert.Equal(binding, CampaignTicketBinding.Decode(binding.Encode(), identity, child));
        Assert.DoesNotContain(binding.Proof, binding.ToString(), StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => CampaignTicketBinding.Decode(binding.Encode(), identity, child with { StartTicks = 1001 }));
        Assert.Throws<ArgumentException>(() => CampaignTicketBinding.Decode(binding.Encode(), identity, child with { ProcessId = 790 }));
        Assert.Throws<ArgumentException>(() => CampaignTicketBinding.Decode((binding with { Proof = new string('0', 64) }).Encode(), identity, child));
        var otherIdentity = identity with { Nonce = new string('E', 64) };
        Assert.Throws<ArgumentException>(() => CampaignTicketBinding.Decode(binding.Encode(), otherIdentity, child));
    }

    [Fact]
    public void BindingAndAckRejectAmbiguousOrUnboundedEncoding()
    {
        var identity = Identity();
        var child = new CampaignProcessIdentity(789, 1000);
        byte[] binding = CampaignTicketBinding.Create(identity, child).Encode();
        var registration = CampaignWorkerRegistration.Create(identity, 0, child.ProcessId, child.StartTicks);
        byte[] ack = CampaignWorkerAcknowledgement.Create(identity, registration).Encode();
        foreach (byte[] bytes in new[] { Array.Empty<byte>(), new byte[513], binding[..^1],
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(binding)[..^1] + ",\"extra\":1}"),
            Encoding.UTF8.GetBytes(" " + Encoding.UTF8.GetString(binding)) })
            Rejected(() => CampaignTicketBinding.Decode(bytes, identity, child));
        foreach (byte[] bytes in new[] { Array.Empty<byte>(), new byte[257], ack[..^1],
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(ack)[..^1] + ",\"Proof\":null}"),
            Encoding.UTF8.GetBytes(" " + Encoding.UTF8.GetString(ack)) })
            Rejected(() => CampaignWorkerAcknowledgement.Validate(bytes, identity, registration));
    }

    [Fact]
    public void ParentIdentityRequiresExactLiveStartTime()
    {
        CampaignProcessIdentity current = CampaignProcessIdentity.Current();
        Assert.True(current.IsLive());
        Assert.False((current with { StartTicks = current.StartTicks + 1 }).IsLive());
        Assert.False(new CampaignProcessIdentity(int.MaxValue, 1).IsLive());
        Assert.False(default(CampaignProcessIdentity).IsLive());
    }

    private static void Rejected(Action action)
    {
        Exception error = Record.Exception(action);
        Assert.True(error is ArgumentException or JsonException, $"Unexpected rejection: {error?.GetType().Name ?? "none"}");
    }
}
