using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class CampaignWorkerGateTests
{
    private static CampaignInvocationIdentity Identity()
    {
        CampaignProcessIdentity parent = CampaignProcessIdentity.Current();
        return new("gate-test", new string('a', 40), "filter-asciihex", 2, TimeSpan.FromSeconds(1).Ticks,
            16777216, 128, 8388608, 16777216, new string('A', 64), new string('B', 64), new string('C', 64),
            new string('D', 64), parent.ProcessId, parent.StartTicks);
    }

    [Fact]
    public async Task ExactRegistrationAndAckPrecedeExecutionAndExitIsIndependentlyConfirmed()
    {
        var identity = Identity();
        using var gate = new CampaignWorkerGate(identity);
        using Process worker = Start("gate-worker", gate.PipeName, identity.Digest(), "normal");
        try
        {
            await gate.RegisterAsync(worker, 0);
            Assert.True(gate.Acknowledged);
            await WaitAsync(worker);
            Assert.Equal("executed", (await worker.StandardOutput.ReadToEndAsync()).Trim());
            Assert.True(gate.ConfirmExit());
            await Assert.ThrowsAsync<InvalidOperationException>(() => gate.RegisterAsync(worker, 0));
        }
        finally { await ReapAsync(worker); }
    }

    [Fact]
    public async Task MissingAckAndLateParentNeverExecuteInput()
    {
        var identity = Identity();
        using var gate = new CampaignWorkerGate(identity);
        using Process worker = Start("gate-worker", gate.PipeName, identity.Digest(), "short");
        try
        {
            await WaitAsync(worker);
            Assert.Equal(94, worker.ExitCode);
            Assert.Empty(await worker.StandardOutput.ReadToEndAsync());
            Assert.False(gate.Acknowledged);
            Assert.False(gate.ConfirmExit()); // No registered ownership is fabricated from root exit.
            await Assert.ThrowsAsync<InvalidOperationException>(() => gate.RegisterAsync(worker, 0));
        }
        finally { await ReapAsync(worker); }
    }

    [Fact]
    public async Task OwnedWorkerIsReapedAfterCancelledHandshake()
    {
        var identity = Identity();
        using var gate = new CampaignWorkerGate(identity);
        using Process worker = Start("gate-worker", gate.PipeName, identity.Digest(), "hold");
        try
        {
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.RegisterAsync(worker, 0, cancelled.Token));
            Assert.False(gate.Acknowledged);
            Assert.True(await gate.CleanupAsync());
            Assert.True(gate.ConfirmExit());
        }
        finally { await ReapAsync(worker); }
    }

    [Fact]
    public async Task AuthenticatedWrongCaseRegistrationIsRefusedAndOwnedWorkerReaped()
    {
        var identity = Identity();
        using var gate = new CampaignWorkerGate(identity);
        using Process worker = Start("gate-worker", gate.PipeName, identity.Digest(), "wrong-index");
        try
        {
            await Assert.ThrowsAsync<IOException>(() => gate.RegisterAsync(worker, 0));
            Assert.False(gate.Acknowledged);
            Assert.True(await gate.CleanupAsync());
            Assert.True(gate.ConfirmExit());
            Assert.Empty(await worker.StandardOutput.ReadToEndAsync());
        }
        finally { await ReapAsync(worker); }
    }

    [Fact]
    public async Task CampaignRootExitDoesNotReleaseExactLiveWorkerOwnership()
    {
        var identity = Identity();
        using var gate = new CampaignWorkerGate(identity);
        using Process root = Start("gate-root", gate.PipeName, identity.Digest(), "hold");
        Process worker = null;
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            string line = await root.StandardOutput.ReadLineAsync(deadline.Token);
            string[] parts = line.Split(':');
            worker = Process.GetProcessById(int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(long.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), CampaignProcessIdentity.Observe(worker).StartTicks);
            await root.WaitForExitAsync(deadline.Token);
            Assert.False(worker.HasExited);
            await gate.RegisterAsync(worker, 0, deadline.Token);
            Assert.True(gate.Acknowledged);
            Assert.False(gate.ConfirmExit());
            Assert.True(await gate.CleanupAsync());
            Assert.True(worker.HasExited);
            Assert.True(gate.ExitConfirmed);
        }
        finally
        {
            if (worker is not null) { await ReapAsync(worker); worker.Dispose(); }
            await ReapAsync(root);
        }
    }

    [Theory]
    [InlineData("forged")] [InlineData("oversized")] [InlineData("truncated")] [InlineData("duplicate")]
    public async Task InvalidRealAckPreventsExecution(string mode)
    {
        var identity = Identity();
        string name = "pp-campaign-" + Guid.NewGuid().ToString("N");
        using var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, 4096, 4096);
        using Process worker = Start("gate-worker", name, identity.Digest(), "normal");
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await server.WaitForConnectionAsync(deadline.Token);
            await WriteFrameAsync(server, identity.Encode(), deadline.Token);
            byte[] prefix = new byte[4];
            await server.ReadExactlyAsync(prefix, deadline.Token);
            int length = BinaryPrimitives.ReadInt32LittleEndian(prefix);
            Assert.InRange(length, 1, 512);
            byte[] bytes = new byte[length];
            await server.ReadExactlyAsync(bytes, deadline.Token);
            var registration = CampaignWorkerRegistration.Decode(bytes, identity);
            if (mode == "forged")
                await WriteFrameAsync(server, (CampaignWorkerAcknowledgement.Create(identity, registration) with { Proof = new string('0', 64) }).Encode(), deadline.Token);
            else if (mode == "duplicate")
            {
                byte[] acknowledgement = CampaignWorkerAcknowledgement.Create(identity, registration).Encode();
                byte[] frames = new byte[2 * (4 + acknowledgement.Length)];
                BinaryPrimitives.WriteInt32LittleEndian(frames, acknowledgement.Length);
                acknowledgement.CopyTo(frames.AsSpan(4));
                BinaryPrimitives.WriteInt32LittleEndian(frames.AsSpan(4 + acknowledgement.Length), acknowledgement.Length);
                acknowledgement.CopyTo(frames.AsSpan(8 + acknowledgement.Length));
                await server.WriteAsync(frames, deadline.Token);
                await server.FlushAsync(deadline.Token);
            }
            else
            {
                BinaryPrimitives.WriteInt32LittleEndian(prefix, mode == "oversized" ? 257 : 1);
                await server.WriteAsync(prefix, deadline.Token);
                await server.FlushAsync(deadline.Token);
                server.Disconnect();
            }
            await WaitAsync(worker);
            Assert.Equal(94, worker.ExitCode);
            Assert.Empty(await worker.StandardOutput.ReadToEndAsync());
        }
        finally { await ReapAsync(worker); }
    }

    [Fact]
    public void AckCannotAuthorizeAnotherWorkerOrReplayChangedInstance()
    {
        var identity = Identity();
        var registration = CampaignWorkerRegistration.Create(identity, 0, int.MaxValue, 1);
        byte[] ack = CampaignWorkerAcknowledgement.Create(identity, registration).Encode();
        CampaignWorkerAcknowledgement.Validate(ack, identity, registration);
        var other = CampaignWorkerRegistration.Create(identity, 0, int.MaxValue, 2);
        Assert.Throws<ArgumentException>(() => CampaignWorkerAcknowledgement.Validate(ack, identity, other));
    }

    [Fact]
    public async Task IssuingParentCannotBecomeItsOwnCleanupTarget()
    {
        using var gate = new CampaignWorkerGate(Identity());
        using Process parent = Process.GetCurrentProcess();
        await Assert.ThrowsAsync<ArgumentException>(() => gate.RegisterAsync(parent, 0));
        Assert.False(await gate.CleanupAsync());
        Assert.False(gate.ConfirmExit());
        Assert.False(parent.HasExited);
    }

    private static Process Start(string mode, string name, string digest, string behavior)
    {
        var start = new ProcessStartInfo(ReplayFixture.Host)
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in new[] { typeof(SupervisorTestChild).Assembly.Location, mode, name, digest, "0", behavior })
            start.ArgumentList.Add(argument);
        return Process.Start(start) ?? throw new InvalidOperationException("Controlled start failed.");
    }

    private static async Task WriteFrameAsync(Stream stream, byte[] bytes, CancellationToken token)
    {
        byte[] prefix = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(prefix, bytes.Length);
        await stream.WriteAsync(prefix, token);
        await stream.WriteAsync(bytes, token);
        await stream.FlushAsync(token);
    }

    private static async Task WaitAsync(Process process)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await process.WaitForExitAsync(deadline.Token);
        Assert.Empty(await process.StandardError.ReadToEndAsync(deadline.Token));
    }

    private static async Task ReapAsync(Process process)
    {
        if (process.HasExited) return;
        process.Kill(entireProcessTree: true);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await process.WaitForExitAsync(deadline.Token);
    }
}
