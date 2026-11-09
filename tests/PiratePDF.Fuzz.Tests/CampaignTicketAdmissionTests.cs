using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

[Trait(TestPlatform.Key, TestPlatform.Windows)]
public sealed class CampaignTicketAdmissionTests
{
    private const string Run = "admission-test";
    private const string Revision = "f51824dbc031b58497a38a6292ee11568b9ffeda";

    [Fact]
    public async Task RealBoundChildConsumesOnceAndReplayCannotStartWork()
    {
        using var fixture = new ReplayFixture();
        var store = new CampaignTicketStore(fixture.Repository, Run);
        var identity = store.CreateIdentity(Revision, "filter-asciihex", new(1, TimeSpan.FromSeconds(1)));
        await store.IssueAsync(identity);
        (int exit, string output) = await ChildAsync(fixture.Repository, identity.Digest(), "replay",
            child => store.BindChildAsync(identity, CampaignProcessIdentity.Observe(child)));
        Assert.Equal(0, exit);
        Assert.Equal($"admitted:{identity.Digest()}\nreplay-rejected\n", output.Replace("\r", "", StringComparison.Ordinal));
        Assert.False(File.Exists(Path.Combine(DirectoryPath(fixture.Repository), "ticket.json")));
        Assert.Equal(identity.Encode(), File.ReadAllBytes(Path.Combine(DirectoryPath(fixture.Repository), "consumed.json")));
        await Assert.ThrowsAsync<IOException>(() => store.IssueAsync(identity));
    }

    [Theory]
    [InlineData("wrong-digest")] [InlineData("wrong-parent-start")] [InlineData("wrong-parent-pid")]
    [InlineData("wrong-binary")] [InlineData("wrong-target-binary")] [InlineData("wrong-repository")]
    [InlineData("wrong-run")] [InlineData("wrong-config")] [InlineData("wrong-revision")]
    [InlineData("wrong-nonce")] [InlineData("truncated")] [InlineData("oversized")]
    [InlineData("noncanonical")] [InlineData("foreign")]
    public async Task InvalidAdmissionNeverReturnsIdentityAndPreservesArtifacts(string mode)
    {
        using var fixture = new ReplayFixture();
        var store = new CampaignTicketStore(fixture.Repository, Run);
        var identity = store.CreateIdentity(Revision, "filter-asciihex", new(1, TimeSpan.FromSeconds(1)));
        await store.IssueAsync(identity);
        string directory = DirectoryPath(fixture.Repository);
        string ticket = Path.Combine(directory, "ticket.json");
        string digest = mode == "wrong-digest" ? new string('0', 64) : identity.Digest();
        (int exit, string output) = await ChildAsync(fixture.Repository, digest, "consume", async child =>
        {
            // Bind authentic pre-tamper state, then release the fixture child after controlled corruption.
            CampaignTicketBinding binding = CampaignTicketBinding.Create(identity, CampaignProcessIdentity.Observe(child));
            var changed = mode switch
            {
                "wrong-parent-start" => identity with { ParentStartTicks = identity.ParentStartTicks + 1 },
                "wrong-parent-pid" => identity with { ParentId = int.MaxValue },
                "wrong-binary" => identity with { RunnerHash = new string('0', 64) },
                "wrong-target-binary" => identity with { TargetsHash = new string('0', 64) },
                "wrong-repository" => identity with { RepositoryHash = new string('0', 64) },
                "wrong-run" => identity with { Run = "other-run" },
                "wrong-config" => identity with { Cases = 2 },
                "wrong-revision" => identity with { Revision = new string('a', 40) },
                "wrong-nonce" => identity with { Nonce = new string('0', 64) },
                _ => identity
            };
            byte[] bytes = mode switch
            {
                "truncated" => identity.Encode()[..^1], "oversized" => new byte[2049],
                "noncanonical" => Encoding.UTF8.GetBytes(" " + Encoding.UTF8.GetString(identity.Encode())),
                _ => changed.Encode()
            };
            File.WriteAllBytes(ticket, bytes);
            if (mode == "foreign") File.WriteAllText(Path.Combine(directory, "foreign.txt"), "preserve");
            await File.WriteAllBytesAsync(Path.Combine(directory, "child.pending"), binding.Encode());
            File.Move(Path.Combine(directory, "child.pending"), Path.Combine(directory, "child.json"));
        });
        Assert.Equal(94, exit);
        Assert.Equal("admission-rejected", output.Trim());
        Assert.True(File.Exists(ticket) || File.Exists(Path.Combine(directory, "consumed.json")));
        if (mode == "foreign") Assert.Equal("preserve", File.ReadAllText(Path.Combine(directory, "foreign.txt")));
    }

    [Fact]
    public async Task DirectInvocationWithoutIssuedTicketIsRejected()
    {
        using var fixture = new ReplayFixture();
        (int exit, string output) = await ChildAsync(fixture.Repository, new string('0', 64), "direct", _ => Task.CompletedTask);
        Assert.Equal(94, exit);
        Assert.Equal("admission-rejected", output.Trim());
    }

    [Fact]
    public async Task ExactChildBindingCannotBeConsumedByIssuingParent()
    {
        using var fixture = new ReplayFixture();
        var store = new CampaignTicketStore(fixture.Repository, Run);
        var identity = store.CreateIdentity(Revision, "all", new(1, TimeSpan.FromSeconds(1)));
        await store.IssueAsync(identity);
        // Valid metadata for a distinct process instance; the current process cannot impersonate it.
        File.WriteAllBytes(Path.Combine(DirectoryPath(fixture.Repository), "child.json"),
            CampaignTicketBinding.Create(identity, new(int.MaxValue, 1)).Encode());
        await Assert.ThrowsAsync<ArgumentException>(() => store.ConsumeAsync(identity.Digest()));
        Assert.True(File.Exists(Path.Combine(DirectoryPath(fixture.Repository), "consumed.json")));
    }

    [Theory]
    [InlineData("../escape")] [InlineData("x/y")] [InlineData("x:y")] [InlineData("pending")]
    public void RunCannotSelectTicketPath(string run)
    {
        using var fixture = new ReplayFixture();
        Assert.Throws<ArgumentException>(() => new CampaignTicketStore(fixture.Repository, run));
    }

    [Fact]
    public async Task TicketDirectoryLinkIsRejectedAndForeignBytesPreserved()
    {
        using var fixture = new ReplayFixture();
        var store = new CampaignTicketStore(fixture.Repository, Run);
        var identity = store.CreateIdentity(Revision, "all", new(1, TimeSpan.FromSeconds(1)));
        string outside = Path.Combine(fixture.Repository, "outside");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "preserve.txt"), "foreign bytes");
        string directory = DirectoryPath(fixture.Repository);
        Directory.CreateDirectory(Path.GetDirectoryName(directory));
        Directory.CreateSymbolicLink(directory, outside);
        Exception error = await Record.ExceptionAsync(() => store.ConsumeAsync(identity.Digest()));
        Assert.NotNull(error);
        Assert.Equal("PiratePDF.Fuzz.Runner.FindingException", error.GetType().FullName);
        Assert.Equal("foreign bytes", File.ReadAllText(Path.Combine(outside, "preserve.txt")));
        Assert.False(File.Exists(Path.Combine(outside, "consumed.json")));
        await Assert.ThrowsAsync<IOException>(() => store.IssueAsync(identity));
    }

    private static string DirectoryPath(string repository) => Path.Combine(repository, "artifacts", "fuzz", "invocations", Run);

    private static async Task<(int Exit, string Output)> ChildAsync(string repository, string digest, string mode,
        Func<Process, Task> bind)
    {
        var start = new ProcessStartInfo(ReplayFixture.Host)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (string argument in new[] { typeof(SupervisorTestChild).Assembly.Location, "ticket-admission", repository, Run, digest, mode })
            start.ArgumentList.Add(argument);
        using var child = new Process { StartInfo = start };
        Assert.True(child.Start());
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await bind(child);
            Task<string> output = child.StandardOutput.ReadToEndAsync(deadline.Token);
            Task<string> error = child.StandardError.ReadToEndAsync(deadline.Token);
            await child.WaitForExitAsync(deadline.Token);
            Assert.Empty(await error);
            return (child.ExitCode, await output);
        }
        finally
        {
            if (!child.HasExited)
            {
                child.Kill(entireProcessTree: true);
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await child.WaitForExitAsync(cleanup.Token);
            }
        }
    }
}
