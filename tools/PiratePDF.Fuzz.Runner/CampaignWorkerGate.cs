using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Single-worker gate and exact-instance ownership seam. Not yet connected to campaign dispatch.</summary>
public sealed class CampaignWorkerGate : IDisposable
{
    public static readonly TimeSpan MaximumHandshake = TimeSpan.FromSeconds(5);
    private readonly CampaignInvocationIdentity identity;
    private readonly NamedPipeServerStream pipe;
    private Process? ownedWorker;
    private CampaignProcessIdentity expectedWorker;
    private bool used;
    private bool disposed;
    public string PipeName { get; } = "pp-campaign-" + Guid.NewGuid().ToString("N");
    public bool Acknowledged { get; private set; }
    public bool ExitConfirmed { get; private set; }
    public string InvocationHash => identity.Digest();

    public CampaignWorkerGate(CampaignInvocationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        _ = identity.Encode();
        if (new CampaignProcessIdentity(identity.ParentId, identity.ParentStartTicks) != CampaignProcessIdentity.Current())
            throw new ArgumentException("Gate belongs to the exact issuing parent.");
        this.identity = identity;
        pipe = new(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, 4096, 4096);
    }

    public async Task RegisterAsync(Process worker, int caseIndex, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worker);
        ObjectDisposedException.ThrowIf(disposed, this);
        if (used || caseIndex < 0 || caseIndex >= identity.Cases) throw new InvalidOperationException("Gate is one-use.");
        used = true;
        expectedWorker = CampaignProcessIdentity.Observe(worker);
        if (expectedWorker.ProcessId == identity.ParentId) throw new ArgumentException("Issuing parent cannot be registered as its worker.");
        // Acquire/retain our own handle before ACK; caller disposal and campaign-root exit cannot release ownership.
        Process candidate = Process.GetProcessById(expectedWorker.ProcessId);
        try
        {
            _ = candidate.SafeHandle;
            if (CampaignProcessIdentity.Observe(candidate) != expectedWorker) throw new IOException("Worker instance changed.");
            ownedWorker = candidate;
        }
        finally
        {
            // A PID reused during attachment must never become a cleanup target.
            if (ownedWorker != candidate) candidate.Dispose();
        }
        using var allowance = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        allowance.CancelAfter(MaximumHandshake);
        await pipe.WaitForConnectionAsync(allowance.Token).ConfigureAwait(false);
        await CampaignGateTransport.WriteAsync(pipe, identity.Encode(), CampaignInvocationIdentity.MaximumBytes, allowance.Token).ConfigureAwait(false);
        var registration = CampaignWorkerRegistration.Decode(await CampaignGateTransport.ReadAsync(pipe,
            CampaignWorkerRegistration.MaximumBytes, allowance.Token).ConfigureAwait(false), identity);
        if (registration.CaseIndex != caseIndex || registration.ProcessId != expectedWorker.ProcessId ||
            registration.StartTicks != expectedWorker.StartTicks || CampaignProcessIdentity.Observe(ownedWorker) != expectedWorker)
            throw new IOException("Registration does not identify the owned worker.");
        byte[] acknowledgement = CampaignWorkerAcknowledgement.Create(identity, registration).Encode();
        await CampaignGateTransport.WriteAsync(pipe, acknowledgement,
            CampaignWorkerAcknowledgement.MaximumBytes, allowance.Token).ConfigureAwait(false);
        byte[] confirmation = await CampaignGateTransport.ReadAsync(pipe, 32, allowance.Token).ConfigureAwait(false);
        if (!CryptographicOperations.FixedTimeEquals(confirmation, SHA256.HashData(acknowledgement)))
            throw new IOException("Invalid gate confirmation.");
        Acknowledged = true;
        pipe.Dispose(); // EOF commits the one-ACK handshake; the worker rejects any trailing frame.
    }

    public bool ConfirmExit()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (ownedWorker is null) return false;
        try { ExitConfirmed = ownedWorker.HasExited; return ExitConfirmed; }
        catch (Exception error) when (error is InvalidOperationException or Win32Exception) { return false; }
    }

    public async Task<bool> CleanupAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (ownedWorker is null) return false;
        using var allowance = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        allowance.CancelAfter(WorkerSupervisor.CleanupTimeout);
        try
        {
            if (!ownedWorker.HasExited) ownedWorker.Kill(entireProcessTree: true);
            await ownedWorker.WaitForExitAsync(allowance.Token).ConfigureAwait(false);
            return ConfirmExit();
        }
        catch (Exception error) when (error is InvalidOperationException or Win32Exception or OperationCanceledException or
            NotSupportedException or AggregateException) { return false; }
    }

    /// <summary>The worker must call this before opening any input or invoking a target.</summary>
    public static async Task<bool> AwaitAcknowledgementAsync(string pipeName, string expectedHash, int caseIndex,
        CancellationToken cancellationToken = default)
    {
        if (pipeName is not { Length: 44 } || !pipeName.StartsWith("pp-campaign-", StringComparison.Ordinal) ||
            !pipeName.AsSpan(12).ToString().All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f') ||
            !FindingFiles.Hash(expectedHash)) return false;
        using var allowance = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        allowance.CancelAfter(MaximumHandshake);
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await client.ConnectAsync(allowance.Token).ConfigureAwait(false);
            var identity = CampaignInvocationIdentity.Decode(await CampaignGateTransport.ReadAsync(client,
                CampaignInvocationIdentity.MaximumBytes, allowance.Token).ConfigureAwait(false));
            if (identity.Digest() != expectedHash || !new CampaignProcessIdentity(identity.ParentId, identity.ParentStartTicks).IsLive()) return false;
            CampaignProcessIdentity worker = CampaignProcessIdentity.Current();
            var registration = CampaignWorkerRegistration.Create(identity, caseIndex, worker.ProcessId, worker.StartTicks);
            await CampaignGateTransport.WriteAsync(client, registration.Encode(), CampaignWorkerRegistration.MaximumBytes, allowance.Token).ConfigureAwait(false);
            byte[] acknowledgement = await CampaignGateTransport.ReadAsync(client,
                CampaignWorkerAcknowledgement.MaximumBytes, allowance.Token).ConfigureAwait(false);
            CampaignWorkerAcknowledgement.Validate(acknowledgement, identity, registration);
            await CampaignGateTransport.WriteAsync(client, SHA256.HashData(acknowledgement), 32, allowance.Token).ConfigureAwait(false);
            byte[] probe = new byte[1];
            return await client.ReadAsync(probe, allowance.Token).ConfigureAwait(false) == 0 &&
                new CampaignProcessIdentity(identity.ParentId, identity.ParentStartTicks).IsLive();
        }
        catch (Exception error) when (error is IOException or ArgumentException or OperationCanceledException or
            UnauthorizedAccessException or System.Text.Json.JsonException or InvalidOperationException or Win32Exception)
        { return false; }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        pipe.Dispose();
        ownedWorker?.Dispose();
        // Disposal closes handles; it deliberately does not certify cleanup. ConfirmExit/CleanupAsync are required before receipt success.
    }
}
