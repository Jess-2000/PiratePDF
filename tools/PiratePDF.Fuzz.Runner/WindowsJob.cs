using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Controller-owned unnamed Job. Creation is nonprivileged; this is not filesystem/network isolation.</summary>
internal sealed class WindowsJob : IDisposable
{
    internal SafeJobHandle Handle { get; }
    private const uint RequiredFlags = 0x2000 | 0x8 | 0x100 | 0x200;

    internal WindowsJob(long memoryBytes, int processes)
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess) throw new PlatformNotSupportedException();
        if (memoryBytes is < 16777216 or > 4294967296 || processes is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(memoryBytes));
        Handle = WindowsNativeMethods.CreateJobObjectW(nint.Zero, null);
        if (Handle.IsInvalid) { Handle.Dispose(); throw new Win32Exception(Marshal.GetLastPInvokeError()); }
        try
        {
            var limits = new ExtendedLimits
            {
                Basic = new BasicLimits { Flags = RequiredFlags, ActiveProcesses = (uint)processes },
                ProcessMemory = (nuint)memoryBytes, JobMemory = (nuint)memoryBytes
            };
            int size = Marshal.SizeOf<ExtendedLimits>();
            nint buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(limits, buffer, false);
                if (!WindowsNativeMethods.SetInformationJobObject(Handle, 9, buffer, (uint)size))
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                if (!WindowsNativeMethods.QueryInformationJobObject(Handle, 9, buffer, (uint)size, out _))
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                var actual = Marshal.PtrToStructure<ExtendedLimits>(buffer);
                if (actual.Basic.Flags != RequiredFlags || actual.Basic.ActiveProcesses != processes ||
                    actual.ProcessMemory != (nuint)memoryBytes || actual.JobMemory != (nuint)memoryBytes)
                    throw new IOException("Job limits were not applied exactly.");
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        catch { Dispose(); throw; }
    }

    internal bool IsEmpty()
    {
        // JOBOBJECT_BASIC_ACCOUNTING_INFORMATION: four LARGE_INTEGERs followed by four DWORDs.
        nint buffer = Marshal.AllocHGlobal(48);
        try
        {
            if (!WindowsNativeMethods.QueryInformationJobObject(Handle, 1, buffer, 48, out _))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            return Marshal.ReadInt32(buffer, 40) == 0;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    internal void Terminate()
    {
        if (!WindowsNativeMethods.TerminateJobObject(Handle, 1)) throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    public void Dispose() => Handle.Dispose();

    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        internal long ProcessTime, JobTime;
        internal uint Flags;
        internal nuint MinimumWorkingSet, MaximumWorkingSet;
        internal uint ActiveProcesses;
        internal nuint Affinity;
        internal uint Priority, Scheduling;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ExtendedLimits
    {
        internal BasicLimits Basic;
        internal ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes;
        internal nuint ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory;
    }
}
