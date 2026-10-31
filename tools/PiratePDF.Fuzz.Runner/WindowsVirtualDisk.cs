using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Read-only live VHDX/volume correlation. Opening this provider never attaches, formats or detaches a disk.</summary>
internal sealed class WindowsVirtualDisk : IDisposable
{
    internal sealed record BackingSnapshot(string Identity, Guid DiskId, long Capacity, int Subtype);
    internal static BackingSnapshot InspectBacking(string path)
    {
        NativeLaunchPolicy.ValidatePath(path);
        using SafeFileHandle metadata = WindowsNativeMethods.OpenMetadata(path);
        var type = new StorageType { Device = 3, Vendor = new("ec984aec-a0f9-47e9-901f-71415a66345b") };
        var parameters = new OpenParameters { Version = 1, Depth = 1 };
        Check(OpenVirtualDisk(type, path, 0x80000, 0, parameters, out SafeFileHandle handle));
        using (handle) return new(WindowsNativeMethods.FileIdentity(metadata), ReadIdentifier(handle), ReadSize(handle), ReadSubtype(handle));
    }

    internal static string DiscoverVolume(string root)
    {
        NativeLaunchPolicy.ValidatePath(root);
        if (root.Length != 3 || root[1] != ':' || root[2] != '\\') throw new ArgumentException("A dedicated drive root is required.");
        char[] name = new char[64];
        if (!GetVolumeNameForVolumeMountPointW(root, name, 64)) throw Error();
        return new string(name).TrimEnd('\0');
    }

    internal static void DetachOwned(NativeProvisioningPlan plan)
    {
        // The provisioning caller must first disable the owned account and prove quiescence.
        using (var verified = new WindowsVirtualDisk(plan.BackingPath, plan.BackingIdentity, plan.DiskId,
            plan.MountRoot, plan.VolumeName, plan.Capacity)) { }
        using SafeFileHandle metadata = WindowsNativeMethods.OpenMetadata(plan.BackingPath);
        if (WindowsNativeMethods.FileIdentity(metadata) != plan.BackingIdentity) throw new IOException("Backing file changed before detach.");
        var type = new StorageType { Device = 3, Vendor = new("ec984aec-a0f9-47e9-901f-71415a66345b") };
        var parameters = new OpenParameters { Version = 1, Depth = 1 };
        Check(OpenVirtualDisk(type, plan.BackingPath, 0xc0000, 0, parameters, out SafeFileHandle handle));
        using (handle)
        {
            if (ReadIdentifier(handle) != plan.DiskId || ReadSize(handle) != plan.Capacity || ReadSubtype(handle) != 2)
                throw new IOException("Virtual disk identity changed before detach.");
            Check(DetachVirtualDisk(handle, 0, 0));
        }
    }
    private readonly SafeFileHandle disk;
    private readonly SafeFileHandle backing;
    private readonly SafeFileHandle volume;
    internal Guid Identifier { get; }
    internal long Capacity { get; }
    internal string BackingIdentity { get; }
    internal string VolumeName { get; }

    internal WindowsVirtualDisk(string backingPath, string expectedBackingIdentity, Guid expectedDisk, string mountRoot,
        string expectedVolume, long expectedCapacity)
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess) throw new PlatformNotSupportedException();
        NativeLaunchPolicy.ValidatePath(backingPath);
        NativeLaunchPolicy.ValidatePath(mountRoot);
        if (!backingPath.EndsWith(".vhdx", StringComparison.OrdinalIgnoreCase) || mountRoot.Length != 3 || mountRoot[1] != ':' ||
            mountRoot[2] != '\\' || expectedDisk == Guid.Empty || expectedCapacity is < 16777216 or > 4294967296)
            throw new ArgumentException("Expected a dedicated VHDX with an explicit drive-root mount and capacity.");
        if (!expectedVolume.StartsWith(@"\\?\Volume{", StringComparison.Ordinal) || !expectedVolume.EndsWith("}\\", StringComparison.Ordinal) ||
            !Guid.TryParse(expectedVolume.AsSpan(11, expectedVolume.Length - 13), out _))
            throw new ArgumentException("Invalid expected volume identity.");
        backing = WindowsNativeMethods.OpenMetadata(backingPath);
        try
        {
            BackingIdentity = WindowsNativeMethods.FileIdentity(backing);
            if (BackingIdentity != expectedBackingIdentity) throw new IOException("VHDX backing identity mismatch.");
            var type = new StorageType { Device = 3, Vendor = new("ec984aec-a0f9-47e9-901f-71415a66345b") };
            var parameters = new OpenParameters { Version = 1, Depth = 1 };
            Check(OpenVirtualDisk(type, backingPath, 0x80000, 0, parameters, out disk));
            try
            {
                Identifier = ReadIdentifier(disk);
                Capacity = ReadSize(disk);
                if (Identifier != expectedDisk || Capacity != expectedCapacity || ReadSubtype(disk) != 2)
                    throw new IOException("VHDX must be the approved fixed-capacity disk instance.");
                char[] path = new char[512];
                uint size = 1024;
                Check(GetVirtualDiskPhysicalPath(disk, ref size, path));
                const string prefix = @"\\.\PhysicalDrive";
                string physical = new string(path).TrimEnd('\0');
                if (!physical.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !uint.TryParse(physical.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out uint number))
                    throw new IOException("Unknown physical VHDX attachment.");
                char[] name = new char[64];
                if (!GetVolumeNameForVolumeMountPointW(mountRoot, name, 64)) throw Error();
                VolumeName = new string(name).TrimEnd('\0');
                if (!VolumeName.Equals(expectedVolume, StringComparison.OrdinalIgnoreCase)) throw new IOException("Mount point volume identity mismatch.");
                char[] filesystem = new char[32];
                if (!GetVolumeInformationW(mountRoot, null, 0, out _, out _, out _, filesystem, 32) || new string(filesystem).TrimEnd('\0') != "NTFS")
                    throw new IOException("The execution filesystem must be NTFS.");
                if (!GetDiskFreeSpaceExW(mountRoot, out _, out ulong total, out _) || total == 0 || total > (ulong)expectedCapacity)
                    throw new IOException("Unverified execution volume capacity.");
                volume = OpenVolume(VolumeName.TrimEnd('\\'), 0, 3, nint.Zero, 3, 0, nint.Zero);
                if (volume.IsInvalid) { volume.Dispose(); throw Error(); }
                try { VerifyExtent(volume, number, expectedCapacity); }
                catch { volume.Dispose(); throw; }
            }
            catch { disk.Dispose(); throw; }
        }
        catch { backing.Dispose(); throw; }
    }

    private static void VerifyExtent(SafeFileHandle volume, uint number, long capacity)
    {
        nint buffer = Marshal.AllocHGlobal(32);
        try
        {
            if (!DeviceIoControl(volume, 0x00560000, nint.Zero, 0, buffer, 32, out uint returned, nint.Zero)) throw Error();
            if (returned < 32 || Marshal.ReadInt32(buffer) != 1 || unchecked((uint)Marshal.ReadInt32(buffer, 8)) != number ||
                Marshal.ReadInt64(buffer, 16) < 0 || Marshal.ReadInt64(buffer, 24) <= 0 ||
                Marshal.ReadInt64(buffer, 24) > capacity - Marshal.ReadInt64(buffer, 16))
                throw new IOException("Volume is not wholly on the approved VHDX.");
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static Guid ReadIdentifier(SafeFileHandle disk) => Query(disk, 2, pointer => Marshal.PtrToStructure<Guid>(pointer + 8));
    private static long ReadSize(SafeFileHandle disk) => Query(disk, 1, pointer => Marshal.ReadInt64(pointer, 8));
    private static int ReadSubtype(SafeFileHandle disk) => Query(disk, 7, pointer => Marshal.ReadInt32(pointer, 8));
    private static T Query<T>(SafeFileHandle disk, int version, Func<nint, T> read)
    {
        nint buffer = Marshal.AllocHGlobal(64);
        try
        {
            Marshal.Copy(new byte[64], 0, buffer, 64);
            Marshal.WriteInt32(buffer, version);
            uint size = 64;
            Check(GetVirtualDiskInformation(disk, ref size, buffer, out uint used));
            if (used > 64 || used < 12) throw new IOException("Invalid virtual disk information.");
            return read(buffer);
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static void Check(uint code) { if (code != 0) throw new Win32Exception(unchecked((int)code), "Virtual disk admission failed."); }
    private static Win32Exception Error() => new(Marshal.GetLastPInvokeError());
    public void Dispose() { volume.Dispose(); disk.Dispose(); backing.Dispose(); } // Retain attachment/evidence; no automatic detach.

    [StructLayout(LayoutKind.Sequential)] private struct StorageType { internal uint Device; internal Guid Vendor; }
    [StructLayout(LayoutKind.Sequential)] private struct OpenParameters { internal int Version; internal uint Depth; }
    [DllImport("virtdisk.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint OpenVirtualDisk(in StorageType type, string path, uint access, uint flags, in OpenParameters parameters, out SafeFileHandle disk);
    [DllImport("virtdisk.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint GetVirtualDiskInformation(SafeFileHandle disk, ref uint size, nint information, out uint used);
    [DllImport("virtdisk.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint DetachVirtualDisk(SafeFileHandle disk, uint flags, uint providerFlags);
    [DllImport("virtdisk.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint GetVirtualDiskPhysicalPath(SafeFileHandle disk, ref uint bytes, [Out] char[] path);
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeFileHandle OpenVolume(string path, uint access, uint sharing, nint security, uint creation, uint flags, nint template);
    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetVolumeNameForVolumeMountPointW(string root, [Out] char[] name, uint length);
    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetVolumeInformationW(string root, [Out] char[]? label, uint labelLength,
        out uint serial, out uint maximumComponent, out uint flags, [Out] char[] filesystem, uint length);
    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetDiskFreeSpaceExW(string path, out ulong available, out ulong total, out ulong free);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeviceIoControl(SafeFileHandle handle, uint code, nint input, uint inputSize,
        nint output, uint outputSize, out uint returned, nint overlapped);
}
