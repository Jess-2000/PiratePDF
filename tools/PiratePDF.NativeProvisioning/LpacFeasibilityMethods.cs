using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static class LpacFeasibilityMethods
{
    [DllImport("userenv.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetUserProfileDirectoryW(
        SafeAccessTokenHandle token, [Out] char[] path, ref uint size);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static extern uint GetThreadId(SafeWaitHandle thread);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static extern uint GetProcessIdOfThread(SafeWaitHandle thread);
    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static extern nint GetProcessWindowStation();
    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetProcessWindowStation(nint station);
    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static extern nint GetThreadDesktop(uint thread);
    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetUserObjectInformationW(nint handle, int information, [Out] byte[] bytes, uint length, out uint needed);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool QueryInformationJobObject(nint job, int information,
        [Out] byte[] buffer, uint length, out uint returned);
    [StructLayout(LayoutKind.Sequential)] internal struct SidAttributes { internal nint Sid; internal uint Attributes; }
    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool CreateRestrictedToken(SafeAccessTokenHandle token, uint flags,
        uint disabledCount, nint disabled, uint deletedCount, nint deleted, uint restrictedCount, [In] SidAttributes[] restricted,
        out SafeAccessTokenHandle result);
    [StructLayout(LayoutKind.Sequential)] internal struct Capabilities { internal nint Package, Sids; internal uint Count, Reserved; }
    [StructLayout(LayoutKind.Sequential)] internal struct Startup
    {
        internal int Size; internal nint Reserved, Desktop, Title;
        internal uint X, Y, Width, Height, Columns, Rows, Fill, Flags;
        internal ushort Show, ReservedLength; internal nint ReservedBytes, Input, Output, Error;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct StartupEx { internal Startup Startup; internal nint Attributes; }
    [StructLayout(LayoutKind.Sequential)] internal struct ProcessInformation { internal nint Process, Thread; internal uint Id, ThreadId; }
    [DllImport("userenv.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern int DeriveAppContainerSidFromAppContainerName(string moniker, out nint sid);
    [DllImport("userenv.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern int CreateAppContainerProfile(string moniker, string displayName, string description,
        nint capabilities, uint capabilityCount, out nint sid);
    [DllImport("userenv.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern int DeleteAppContainerProfile(string moniker);
    [DllImport("advapi32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static extern nint FreeSid(nint sid);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsProcessInJob(nint process, nint job, [MarshalAs(UnmanagedType.Bool)] out bool member);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool InitializeProcThreadAttributeList(nint list, int count, int flags, ref nuint size);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool UpdateProcThreadAttribute(nint list, uint flags, nuint attribute, nint value, nuint size, nint previous, nint returned);
    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static extern void DeleteProcThreadAttributeList(nint list);
    [DllImport("advapi32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool CreateProcessAsUserW(SafeAccessTokenHandle token, string application,
        [In, Out] char[] command, nint processSecurity, nint threadSecurity, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint flags,
        nint environment, string directory, ref StartupEx startup, out ProcessInformation information);
    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool OpenProcessToken(SafeProcessHandle process, uint access, out SafeAccessTokenHandle token);
    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetTokenInformation(SafeAccessTokenHandle token, int information, nint buffer, int length, out int required);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static extern uint ResumeThread(SafeWaitHandle thread);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static extern uint WaitForSingleObject(SafeProcessHandle process, uint milliseconds);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetExitCodeProcess(SafeProcessHandle process, out uint code);
    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool QueryFullProcessImageNameW(SafeProcessHandle process, uint flags, [Out] char[] path, ref uint length);
}
