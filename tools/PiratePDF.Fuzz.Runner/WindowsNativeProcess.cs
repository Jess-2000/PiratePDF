using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Creates a suspended restricted process inside the configured Job, with only three inherited handles.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsNativeProcess : IDisposable
{
    private readonly SafeProcessHandle process;
    private readonly SafeWaitHandle thread;
    private readonly WindowsCapturePipe output, error;
    private bool resumed;
    internal Stream Output => output.Reader;
    internal Stream Error => error.Reader;
    internal int Id { get; }

    internal WindowsNativeProcess(NativeExecutionIdentity identity, WindowsJob job, TrustedNativeImage image,
        IReadOnlyList<string> arguments, string directory, string windows, string temporary, string desktop)
    {
        NativeLaunchPolicy.ValidatePath(directory);
        if (!desktop.StartsWith("pph2b_", StringComparison.Ordinal) || desktop.Length != 22 ||
            !desktop.EndsWith("\\default", StringComparison.Ordinal) || !desktop.Substring(6, 8).All(Uri.IsHexDigit))
            throw new ArgumentException("A separately admitted dedicated desktop is required.");
        using (System.Diagnostics.Process controller = System.Diagnostics.Process.GetCurrentProcess())
        {
            if (!IsInAnyJob(controller.SafeHandle, nint.Zero, out bool nested)) throw LastError();
            if (nested) throw new IOException("Unapproved nested-Job launch context.");
        }
        output = new();
        try { error = new(); }
        catch { output.Dispose(); throw; }
        var startup = new StartupEx { Startup = new() { Size = Marshal.SizeOf<StartupEx>(), Flags = 0x100, Desktop = desktop } };
        nint attributes = nint.Zero, jobList = nint.Zero, handles = nint.Zero, environment = nint.Zero;
        try
        {
            using SafeFileHandle input = NullInput();
            nuint size = 0;
            InitializeProcThreadAttributeList(nint.Zero, 2, 0, ref size);
            if (size == 0 || size > 65536) throw new IOException("Invalid process attribute size.");
            attributes = Marshal.AllocHGlobal((nint)size);
            if (!InitializeProcThreadAttributeList(attributes, 2, 0, ref size)) throw LastError();
            startup.Attributes = attributes;
            jobList = Marshal.AllocHGlobal(nint.Size);
            Marshal.WriteIntPtr(jobList, job.Handle.DangerousGetHandle());
            handles = Marshal.AllocHGlobal(nint.Size * 3);
            Marshal.WriteIntPtr(handles, input.DangerousGetHandle());
            Marshal.WriteIntPtr(handles, nint.Size, output.Writer.DangerousGetHandle());
            Marshal.WriteIntPtr(handles, nint.Size * 2, error.Writer.DangerousGetHandle());
            if (!UpdateProcThreadAttribute(attributes, 0, 0x2000d, jobList, (nuint)nint.Size, nint.Zero, nint.Zero) ||
                !UpdateProcThreadAttribute(attributes, 0, 0x20002, handles, (nuint)(nint.Size * 3), nint.Zero, nint.Zero)) throw LastError();
            startup.Startup.Input = input.DangerousGetHandle();
            startup.Startup.Output = output.Writer.DangerousGetHandle();
            startup.Startup.Error = error.Writer.DangerousGetHandle();
            string[] values = NativeLaunchPolicy.EnvironmentBlock(windows, temporary);
            environment = Marshal.StringToHGlobalUni(string.Join('\0', values.Order(StringComparer.OrdinalIgnoreCase)) + "\0\0");
            string commandText = string.Join(' ', new[] { image.Path }.Concat(arguments).Select(NativeLaunchPolicy.Quote));
            if (commandText.Length > 32766) throw new ArgumentException("Command exceeds Windows limits.");
            char[] command = (commandText + '\0').ToCharArray();
            if (!CreateProcessAsUserW(identity.Token, image.Path, command, nint.Zero, nint.Zero, true,
                0x80000 | 4 | 0x400 | 0x08000000, environment, directory, ref startup, out ProcessInformation information)) throw LastError();
            process = new SafeProcessHandle(information.Process, true);
            thread = new SafeWaitHandle(information.Thread, true);
            Id = checked((int)information.Id);
            try
            {
                if (!IsProcessInJob(process, job.Handle, out bool member) || !member) throw new IOException("Creation-time Job membership was not established.");
                VerifyIdentity(identity);
                char[] actual = new char[4096];
                uint length = 4096;
                if (!QueryFullProcessImageNameW(process, 0, actual, ref length) || !new string(actual, 0, checked((int)length)).Equals(image.Path, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Suspended process image path mismatch.");
                using SafeFileHandle file = WindowsNativeMethods.OpenMetadata(new string(actual, 0, checked((int)length)));
                if (WindowsNativeMethods.FileIdentity(file) != image.Identity) throw new IOException("Suspended process image instance mismatch.");
            }
            catch
            {
                try { job.Terminate(); }
                finally { thread.Dispose(); process.Dispose(); }
                throw;
            }
        }
        catch { output.Dispose(); error.Dispose(); throw; }
        finally
        {
            if (attributes != nint.Zero) { if (startup.Attributes != nint.Zero) DeleteProcThreadAttributeList(attributes); Marshal.FreeHGlobal(attributes); }
            if (jobList != nint.Zero) Marshal.FreeHGlobal(jobList);
            if (handles != nint.Zero) Marshal.FreeHGlobal(handles);
            if (environment != nint.Zero) Marshal.FreeHGlobal(environment);
        }
    }

    internal void Resume()
    {
        if (resumed) throw new InvalidOperationException("Initial thread was already resumed.");
        if (ResumeThread(thread) != 1) throw LastError();
        resumed = true;
        output.ParentWriteClosed();
        error.ParentWriteClosed();
    }

    private void VerifyIdentity(NativeExecutionIdentity expected)
    {
        if (!OpenProcessToken(process, 8, out SafeAccessTokenHandle token)) throw LastError();
        using (token) expected.Verify(token);
    }

    internal bool HasExited => WaitForSingleObject(process, 0) switch
    { 0 => true, 258 => false, _ => throw LastError() };
    internal int? ExitCode
    {
        get
        {
            if (!HasExited) return null;
            if (!GetExitCodeProcess(process, out uint code)) throw LastError();
            return unchecked((int)code);
        }
    }

    internal void CloseCaptures() { output.Dispose(); error.Dispose(); }
    public void Dispose() { output.Dispose(); error.Dispose(); thread.Dispose(); process.Dispose(); }
    private static Win32Exception LastError() => new(Marshal.GetLastPInvokeError());
    private static SafeFileHandle NullInput()
    {
        var security = new WindowsCapturePipe.SecurityAttributes { Length = Marshal.SizeOf<WindowsCapturePipe.SecurityAttributes>(), Inherit = 1 };
        SafeFileHandle input = OpenNull("NUL", 0x80000000, 3, security, 3, 0, nint.Zero);
        if (input.IsInvalid) { int error = Marshal.GetLastPInvokeError(); input.Dispose(); throw new Win32Exception(error); }
        return input;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Startup
    {
        internal int Size;
        internal nint Reserved;
        [MarshalAs(UnmanagedType.LPWStr)] internal string? Desktop;
        internal nint Title;
        internal uint X, Y, Width, Height, Columns, Rows, Fill, Flags;
        internal ushort Show, ReservedLength;
        internal nint ReservedData, Input, Output, Error;
    }
    [StructLayout(LayoutKind.Sequential)] private struct StartupEx { internal Startup Startup; internal nint Attributes; }
    [StructLayout(LayoutKind.Sequential)] private struct ProcessInformation { internal nint Process, Thread; internal uint Id, ThreadId; }
    [DllImport("advapi32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CreateProcessAsUserW(SafeAccessTokenHandle token, string application,
        [In, Out] char[] command, nint processSecurity, nint threadSecurity, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint flags,
        nint environment, string directory, ref StartupEx startup, out ProcessInformation information);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool InitializeProcThreadAttributeList(nint list, int count, int flags, ref nuint size);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UpdateProcThreadAttribute(nint list, uint flags, nuint attribute,
        nint value, nuint size, nint previous, nint returned);
    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern void DeleteProcThreadAttributeList(nint list);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint ResumeThread(SafeWaitHandle thread);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsProcessInJob(SafeProcessHandle process, SafeJobHandle job, [MarshalAs(UnmanagedType.Bool)] out bool member);
    [DllImport("kernel32.dll", EntryPoint = "IsProcessInJob", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsInAnyJob(SafeProcessHandle process, nint job, [MarshalAs(UnmanagedType.Bool)] out bool member);
    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool OpenProcessToken(SafeProcessHandle process, uint access, out SafeAccessTokenHandle token);
    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool QueryFullProcessImageNameW(SafeProcessHandle process, uint flags, [Out] char[] name, ref uint length);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint WaitForSingleObject(SafeProcessHandle process, uint milliseconds);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetExitCodeProcess(SafeProcessHandle process, out uint code);
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeFileHandle OpenNull(string path, uint access, uint share, in WindowsCapturePipe.SecurityAttributes security,
        uint creation, uint flags, nint template);
}
