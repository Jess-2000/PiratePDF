using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Admits only a live direct Windows child of the retained, still-live root instance.</summary>
internal static class ProcessExecution
{
    internal static async Task<Process> AdmitChildAsync(Process root, string path, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Child ancestry is Windows-only.");
        string[] fields = Encoding.ASCII.GetString((await InputFile.ReadAsync(path, 100, token).ConfigureAwait(false)).Span).Split(':');
        if (fields.Length != 2 || !int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out int pid) ||
            pid <= 0 || pid == Environment.ProcessId || pid == root.Id ||
            !long.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks) || ticks <= 0 || root.HasExited)
            throw new IOException("Unverified cooperating child identity.");
        Process candidate = Process.GetProcessById(pid);
        try
        {
            // Force and retain this handle before inspecting identity or assigning cleanup ownership.
            SafeProcessHandle handle = candidate.SafeHandle;
            if (candidate.HasExited || candidate.StartTime.ToUniversalTime().Ticks != ticks ||
                ticks < root.StartTime.ToUniversalTime().Ticks || root.HasExited ||
                NativeMethods.NtQueryInformationProcess(handle, 0, out var information,
                    Marshal.SizeOf<NativeMethods.ProcessBasicInformation>(), out _) != 0 ||
                information.UniqueProcessId != pid || information.ParentProcessId != root.Id || root.HasExited)
                throw new IOException("Child instance or direct parent does not match.");
            return candidate;
        }
        catch
        {
            candidate.Dispose(); // A rejected instance never becomes a termination target.
            throw;
        }
    }

    internal static bool IsProcessError(Exception error) => error is IOException or UnauthorizedAccessException or
        Win32Exception or InvalidOperationException or ArgumentException or NotSupportedException or AggregateException;

    private static class NativeMethods
    {
        // Pointer-width layout is required by PROCESS_BASIC_INFORMATION on both Windows architectures.
        [StructLayout(LayoutKind.Sequential)]
        internal struct ProcessBasicInformation
        {
            internal nint ExitStatus;
            internal nint PebAddress;
            internal nint Affinity;
            internal nint Priority;
            internal nint UniqueProcessId;
            internal nint ParentProcessId;
        }

        [DllImport("ntdll.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern int NtQueryInformationProcess(SafeProcessHandle process, int informationClass,
            out ProcessBasicInformation information, int length, out int returnedLength);
    }
}
