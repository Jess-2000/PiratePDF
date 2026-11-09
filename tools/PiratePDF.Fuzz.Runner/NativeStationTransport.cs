using System.Buffers.Binary;
using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.AccessControl;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

// Exactly one local request/offer/acknowledgment. No worker or privileged output path.
[SupportedOSPlatform("windows")]
internal sealed class NativeStationTransport : IDisposable
{
    private readonly PipeStream stream;
    private int remaining = NativeStationPolicy.TransactionLimit;
    private NativeStationTransport(PipeStream owned) { stream = owned; }
    internal static async Task<NativeStationTransport> ServerAsync(StationAuthorization authorization, CancellationToken token)
    {
        var descriptor = new RawSecurityDescriptor("O:BAG:BAD:P(A;;0x12019f;;;BA)(A;;0x120083;;;" + authorization.Controller.User + ")");
        byte[] bytes = new byte[descriptor.BinaryLength]; descriptor.GetBinaryForm(bytes, 0);
        nint buffer = Marshal.AllocHGlobal(bytes.Length);
        SafePipeHandle handle;
        try
        {
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            var attributes = new WindowsCapturePipe.SecurityAttributes { Length = Marshal.SizeOf<WindowsCapturePipe.SecurityAttributes>(), Descriptor = buffer };
            handle = CreateNamedPipeW(@"\\.\pipe\" + NativeStationPolicy.Endpoint(authorization.Id), 0x40080003, 8, 1, 4096, 4096, 0, attributes);
        }
        finally { Marshal.FreeHGlobal(buffer); }
        if (handle.IsInvalid) { int error = Marshal.GetLastPInvokeError(); handle.Dispose(); throw new Win32Exception(error, "EndpointCollisionOrCreationFailure"); }
        NamedPipeServerStream? pipe = null;
        try
        {
            pipe = new NamedPipeServerStream(PipeDirection.InOut, true, false, handle);
            await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
            return new(pipe);
        }
        catch { if (pipe is null) handle.Dispose(); else pipe.Dispose(); throw; }
    }
    internal static async Task<NativeStationTransport> ClientAsync(StationAuthorization authorization, CancellationToken token)
    {
        SafePipeHandle? handle = null;
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                // SQOS_PRESENT | SECURITY_IDENTIFICATION; never impersonation/delegation.
                handle = CreateFileW(@"\\.\pipe\" + NativeStationPolicy.Endpoint(authorization.Id), 0x120083, 0, 0, 3, 0x40110000, 0);
                if (!handle.IsInvalid) break;
                int error = Marshal.GetLastPInvokeError(); handle.Dispose(); handle = null;
                if (error is not (2 or 231)) throw new Win32Exception(error);
                await Task.Delay(20, token).ConfigureAwait(false);
            }
            var pipe = new NamedPipeClientStream(PipeDirection.InOut, true, true, handle);
            return new(pipe);
        }
        catch { handle?.Dispose(); throw; }
    }
    internal int PeerPid(bool server)
    {
        uint pid;
        bool success = server ? GetNamedPipeClientProcessId(stream.SafePipeHandle, out pid) : GetNamedPipeServerProcessId(stream.SafePipeHandle, out pid);
        if (!success || pid > int.MaxValue || pid == 0) throw new SecurityException("Endpoint peer process identity unavailable.");
        return (int)pid;
    }
    internal static void ValidateFrameLength(int length)
    {
        if (length is < 2 or > NativeStationPolicy.FrameLimit) throw new SecurityException("Invalid station frame length.");
    }
    private void Charge(int length)
    {
        ValidateFrameLength(length);
        remaining -= length + 4;
        if (remaining < 0) throw new SecurityException("Station transaction budget exceeded.");
    }
    internal async Task SendAsync(StationMessage message, CancellationToken token)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(message, NativeStationPolicy.Json); Charge(bytes.Length);
        byte[] prefix = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(prefix, bytes.Length);
        await stream.WriteAsync(prefix, token).ConfigureAwait(false);
        await stream.WriteAsync(bytes, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
    }
    internal async Task<StationMessage> ReceiveAsync(StationAuthorization authorization, string kind, CancellationToken token)
    {
        byte[] prefix = new byte[4]; await stream.ReadExactlyAsync(prefix, token).ConfigureAwait(false);
        int length = BinaryPrimitives.ReadInt32LittleEndian(prefix); Charge(length);
        byte[] bytes = new byte[length]; await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        StationMessage message = NativeStationPolicy.Decode<StationMessage>(bytes);
        ValidateMessage(message, authorization.Id, kind);
        return message;
    }
    internal static void ValidateMessage(StationMessage message, string id, string kind)
    {
        if (message.Version != 1 || message.Operation != NativeStationPolicy.Operation || message.Id != id || message.Kind != kind ||
            kind is not ("Request" or "Offer" or "Ack") ||
            message.ErrorCode < 0 || (kind != "Offer" && (message.Station != 0 || message.Desktop != 0 || message.ErrorCode != 0)) ||
            (kind == "Request" && message.State != "") ||
            (kind == "Ack" && message.State is not ("Verified" or "RefusedClosed" or "UNKNOWN/BLOCKED")) ||
            (kind == "Offer" && message.State is not ("Pair" or "FirstDuplicationFailure" or "SecondDuplicationFailure" or "StationCollision" or
                "StationCreationFailure" or "DesktopCreationFailure" or "DescriptorOrLabelMismatch" or "UNKNOWN/BLOCKED:CleanupFailure")) ||
            (kind == "Offer" && message.State == "Pair" && message.ErrorCode != 0))
            throw new SecurityException("Station protocol correlation/state mismatch.");
    }
    public void Dispose() => stream.Dispose();
    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafePipeHandle CreateNamedPipeW(string name, uint mode, uint pipeMode, uint instances, uint output,
        uint input, uint timeout, in WindowsCapturePipe.SecurityAttributes attributes);
    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafePipeHandle CreateFileW(string name, uint access, uint share, nint security, uint creation, uint flags, nint template);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint pid);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint pid);
}
