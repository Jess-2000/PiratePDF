using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

internal static class WindowsNativeMethods
{
    internal static SafeFileHandle OpenMetadata(string path)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        SafeFileHandle handle = CreateFileW(path, 0x80, 3, nint.Zero, 3, 0x00200000, nint.Zero);
        if (handle.IsInvalid) { int error = Marshal.GetLastPInvokeError(); handle.Dispose(); throw new Win32Exception(error); }
        if ((File.GetAttributes(handle) & (FileAttributes.ReparsePoint | FileAttributes.Directory | FileAttributes.Device)) != 0)
        { handle.Dispose(); throw new IOException("Metadata handle is not an ordinary file."); }
        return handle;
    }
    internal static SafeFileHandle OpenImage(string path)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        SafeFileHandle handle = CreateFileW(path, 0x81, 1, nint.Zero, 3, 0x00200000, nint.Zero);
        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            throw new Win32Exception(error, "Cannot retain image admission lease.");
        }
        if ((File.GetAttributes(handle) & (FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.Device)) != 0)
        { handle.Dispose(); throw new IOException("Image handle is not an ordinary file."); }
        return handle;
    }

    internal static string FileIdentity(SafeFileHandle handle)
    {
        nint buffer = Marshal.AllocHGlobal(24);
        try
        {
            if (!GetFileInformationByHandleEx(handle, 18, buffer, 24)) throw new Win32Exception(Marshal.GetLastPInvokeError());
            byte[] identity = new byte[24];
            Marshal.Copy(buffer, identity, 0, identity.Length);
            return Convert.ToHexString(identity);
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    internal static SafeFileHandle OpenDirectory(string path)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        SafeFileHandle handle = CreateFileW(path, 0x80, 3, nint.Zero, 3, 0x02200000, nint.Zero);
        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            throw new Win32Exception(error, "Cannot retain directory admission lease: " + path);
        }
        if ((File.GetAttributes(handle) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != FileAttributes.Directory)
        { handle.Dispose(); throw new IOException("Directory identity changed during admission."); }
        return handle;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint sharing, nint security,
        uint creation, uint flags, nint template);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle handle, int informationClass, nint information, uint length);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern SafeJobHandle CreateJobObjectW(nint security, string? name);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetInformationJobObject(SafeJobHandle job, int informationClass, nint information, uint length);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool QueryInformationJobObject(SafeJobHandle job, int informationClass, nint information, uint length, out uint returned);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool TerminateJobObject(SafeJobHandle job, uint exitCode);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(nint handle);
}

internal sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeJobHandle() : base(true) { }
    protected override bool ReleaseHandle() => WindowsNativeMethods.CloseHandle(handle);
}
