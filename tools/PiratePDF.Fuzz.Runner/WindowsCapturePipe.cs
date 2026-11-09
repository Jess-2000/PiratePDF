using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Controller-only overlapped pipe, with an explicitly inherited write capability for stdout/stderr.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsCapturePipe : IDisposable
{
#if !STATION_ONLY
    internal FileStream Reader { get; }
    internal SafeFileHandle Writer { get; }

    internal WindowsCapturePipe()
    {
        using WindowsIdentity current = WindowsIdentity.GetCurrent();
        string sid = current.User?.Value ?? throw new UnauthorizedAccessException();
        var descriptor = new RawSecurityDescriptor("D:P(A;;GA;;;" + sid + ")");
        byte[] bytes = new byte[descriptor.BinaryLength];
        descriptor.GetBinaryForm(bytes, 0);
        nint security = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, security, bytes.Length);
            var attributes = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>(), Descriptor = security };
            string name = @"\\.\pipe\PiratePDF-H2b-" + Guid.NewGuid().ToString("N");
            SafeFileHandle reader = CreateNamedPipeW(name, 0x40080001, 8, 1, 65536, 65536, 0, attributes);
            if (reader.IsInvalid) { int error = Marshal.GetLastPInvokeError(); reader.Dispose(); throw new Win32Exception(error); }
            try
            {
                attributes.Inherit = 1;
                Writer = OpenWriter(name, 0x40000000, 0, attributes, 3, 0, nint.Zero);
                if (Writer.IsInvalid) { int error = Marshal.GetLastPInvokeError(); Writer.Dispose(); throw new Win32Exception(error); }
                try
                {
                    // The controller already connected the only client. Never wait for a child to connect by name.
                    if (!ConnectNamedPipe(reader, nint.Zero) && Marshal.GetLastPInvokeError() != 535) throw new Win32Exception(Marshal.GetLastPInvokeError());
                    Reader = new FileStream(reader, FileAccess.Read, 4096, isAsync: true);
                }
                catch { Writer.Dispose(); throw; }
            }
            catch { reader.Dispose(); throw; }
        }
        finally { Marshal.FreeHGlobal(security); }
    }

    internal void ParentWriteClosed() => Writer.Dispose();
    public void Dispose() { Writer.Dispose(); Reader.Dispose(); }
#else
    public void Dispose() { }
#endif

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityAttributes { internal int Length; internal nint Descriptor; internal int Inherit; }
#if !STATION_ONLY
    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeFileHandle CreateNamedPipeW(string name, uint mode, uint pipeMode, uint instances, uint output,
        uint input, uint timeout, in SecurityAttributes security);
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeFileHandle OpenWriter(string path, uint access, uint share, in SecurityAttributes security,
        uint creation, uint flags, nint template);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ConnectNamedPipe(SafeFileHandle pipe, nint overlapped);
#endif
}
