using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

internal static class WindowsIdentityMethods
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SidAttributes { internal nint Sid; internal uint Attributes; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Privilege { internal uint Low; internal int High; internal uint Attributes; }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool LogonUserW(string user, string domain, nint password, int type, int provider,
        out SafeAccessTokenHandle token);

    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateRestrictedToken(SafeAccessTokenHandle existing, uint flags, uint disabledCount,
        nint disabled, uint deletedCount, [In] Privilege[] deleted, uint restrictedCount,
        [In] SidAttributes[] restricted, out SafeAccessTokenHandle token);

    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetTokenInformation(SafeAccessTokenHandle token, int informationClass, nint buffer,
        int length, out int required);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool LookupPrivilegeValueW(string? system, string name, out long luid);

    [StructLayout(LayoutKind.Sequential)]
    internal struct LsaAttributes
    {
        internal uint Length;
        internal nint Root, Name;
        internal uint Attributes;
        internal nint Security, Quality;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct LsaString { internal ushort Length, MaximumLength; internal nint Buffer; }
    [DllImport("advapi32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint LsaOpenPolicy(nint system, in LsaAttributes attributes, uint access, out nint policy);
    [DllImport("advapi32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint LsaEnumerateAccountRights(nint policy, nint sid, out nint rights, out uint count);
    [DllImport("advapi32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint LsaFreeMemory(nint memory);
    [DllImport("advapi32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint LsaClose(nint policy);
    [DllImport("advapi32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint LsaNtStatusToWinError(uint status);
}
