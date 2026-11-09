using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

[Trait(TestPlatform.Key, TestPlatform.Windows)]
public sealed class CampaignGatedWorkerTests
{
    [Fact]
    public async Task RealNeutralWorkerKeepsSingleCaseProtocolAndConfirmsExactExit()
    {
        using var fixture = new ReplayFixture();
        fixture.Add("minimal");
        var store = new CampaignTicketStore(fixture.Repository, "gated-worker-test");
        var identity = store.CreateIdentity(new string('a', 40), "filter-asciihex", new(1, TimeSpan.FromSeconds(1)));
        using var gate = new CampaignWorkerGate(identity);
        SupervisionResult result = await ReplayFixture.Supervisor(real: true).RunCampaignAsync("filter-asciihex", fixture.SeedPath("minimal"), 1, gate, 0);
        Assert.Equal(SupervisionStatus.Completed, result.Status);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("TargetResult", result.WorkerCode);
        Assert.InRange(result.StdoutRetainedBytes, 1, 513);
        Assert.Equal(0, result.StderrRetainedBytes);
        Assert.True(gate.Acknowledged);
        Assert.True(gate.ConfirmExit());
    }

    [Fact]
    public async Task DirectWorkerCannotReadInputWithoutParentGate()
    {
        int exit = await CampaignGatedWorkerCommand.ExecuteAsync(["campaign-worker-internal", "filter-asciihex",
            "NEVER_OPEN_THIS_INPUT", "../invalid-gate", new string('0', 64), "0"]);
        Assert.Equal(2, exit);
    }

    [Fact]
    public async Task FailedLaunchAndCallerCancellationNeverAcknowledgeAWorker()
    {
        using var fixture = new ReplayFixture();
        var store = new CampaignTicketStore(fixture.Repository, "gated-worker-test");
        var identity = store.CreateIdentity(new string('a', 40), "filter-asciihex", new(1, TimeSpan.FromSeconds(1)));
        using var gate = new CampaignWorkerGate(identity);
        var supervisor = new WorkerSupervisor(Path.Combine(fixture.Repository, "missing", "dotnet.exe"), typeof(RunCommand).Assembly.Location);
        SupervisionResult failed = await supervisor.RunCampaignAsync("filter-asciihex", "never-open", 1, gate, 0);
        Assert.Equal(SupervisionStatus.StartFailed, failed.Status);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        SupervisionResult stopped = await ReplayFixture.Supervisor(real: true).RunCampaignAsync("filter-asciihex", "never-open", 1, gate, 0, cancelled.Token);
        Assert.Equal(SupervisionStatus.Cancelled, stopped.Status);
        Assert.False(gate.Acknowledged);
        Assert.False(gate.ConfirmExit());
    }
}
