using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

internal sealed partial class WindowsExecutionDesktop
{
    private WindowsExecutionDesktop(SafeStationHandle ownedStation, SafeDesktopHandle ownedDesktop, string name)
    { station = ownedStation; desktop = ownedDesktop; Name = name; }

    internal static WindowsExecutionDesktop CreateStationLease(string runId)
    {
        NativeStationPolicy.RequireHex(runId, 8);
        string name = "pph2b_" + runId;
        SafeStationHandle? createdStation = null; SafeDesktopHandle? createdDesktop = null;
        string stage = "StationCreationFailure";
        try
        {
            WithDescriptor(false, attributes => createdStation = CreateWindowStationW(name, 1, NativeStationPolicy.StationCreator, attributes));
            if (createdStation is null || createdStation.IsInvalid)
            {
                int error = Marshal.GetLastPInvokeError();
                throw new StationLeaseFailure(error == 183 ? "StationCollision" : stage, error);
            }
            stage = "DescriptorOrLabelMismatch";
            VerifyLeaseObject(createdStation.DangerousGetHandle(), false, name, NativeStationPolicy.StationCreator);
            lock (StationChange)
            {
                nint original = GetProcessWindowStation();
                if (original == 0 || !SetProcessWindowStation(createdStation.DangerousGetHandle())) throw new Win32Exception(Marshal.GetLastPInvokeError());
                bool restored; int desktopError = 0;
                stage = "DesktopCreationFailure";
                try { WithDescriptor(true, attributes => { createdDesktop = CreateDesktopW("default", 0, 0, 0, NativeStationPolicy.DesktopCreator, attributes); desktopError = Marshal.GetLastPInvokeError(); }); }
                finally { restored = SetProcessWindowStation(original); }
                if (!restored) throw new SecurityException("UNKNOWN/BLOCKED: cannot restore helper station association.");
                if (createdDesktop is null || createdDesktop.IsInvalid) throw new Win32Exception(desktopError, "DesktopCreationFailure");
            }
            if (createdDesktop is null || createdDesktop.IsInvalid) throw new Win32Exception(Marshal.GetLastPInvokeError(), "DesktopCreationFailure");
            stage = "DescriptorOrLabelMismatch";
            VerifyLeaseObject(createdDesktop.DangerousGetHandle(), true, "default", NativeStationPolicy.DesktopCreator);
            return new(createdStation, createdDesktop, name + "\\default");
        }
        catch (Exception failure) when (failure is Win32Exception or SecurityException or IOException)
        {
            createdDesktop?.Dispose(); createdStation?.Dispose();
            if (createdDesktop?.CloseSucceeded is false || createdStation?.CloseSucceeded is false)
                throw new StationLeaseFailure("UNKNOWN/BLOCKED:CleanupFailure", 0, failure);
            if (failure is StationLeaseFailure) throw;
            throw new StationLeaseFailure(stage, failure is Win32Exception native ? native.NativeErrorCode : 0, failure);
        }
    }

    private static void WithDescriptor(bool isDesktop, Action<WindowsCapturePipe.SecurityAttributes> create)
    {
        var descriptor = new RawSecurityDescriptor(NativeStationPolicy.Descriptor(isDesktop));
        byte[] bytes = new byte[descriptor.BinaryLength]; descriptor.GetBinaryForm(bytes, 0);
        nint buffer = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            create(new WindowsCapturePipe.SecurityAttributes { Length = Marshal.SizeOf<WindowsCapturePipe.SecurityAttributes>(), Descriptor = buffer });
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    internal long DuplicateStation(SafeProcessHandle target) => DuplicateLease(station, target, NativeStationPolicy.StationLease);
    internal long DuplicateDesktop(SafeProcessHandle target) => DuplicateLease(desktop, target, NativeStationPolicy.DesktopLease);
    private static long DuplicateLease(SafeHandle source, SafeProcessHandle target, uint access)
    {
        bool retained = false;
        try
        {
            source.DangerousAddRef(ref retained);
            if (!DuplicateHandle(new nint(-1), source.DangerousGetHandle(), target, out nint duplicate, access, false, 0))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            return duplicate.ToInt64();
        }
        finally { if (retained) source.DangerousRelease(); }
    }
    internal static void ValidateHandlePair(long stationValue, long desktopValue)
    {
        if (!ValidHandleValue(stationValue) || !ValidHandleValue(desktopValue) || stationValue == desktopValue)
            throw new SecurityException("Invalid or reused station lease handles.");
    }
    private static bool ValidHandleValue(long value) => value > 0 && (IntPtr.Size != 4 || value <= int.MaxValue);
    // Caller must first authenticate the live helper and bind the exact one-use offer.
    // Invalid/unproven numbers are never wrapped or closed as locally owned handles.
    internal static WindowsExecutionDesktop AdoptStationLease(StationMessage offer, StationAuthorization authorization)
    {
        if (offer.Version != 1 || offer.Operation != NativeStationPolicy.Operation || offer.Id != authorization.Id || offer.Kind != "Offer")
            throw new SecurityException("Unproven station offer provenance.");
        bool partial = offer.State == "SecondDuplicationFailure" && offer.Desktop == 0;
        if (partial)
        {
            if (!ValidHandleValue(offer.Station)) throw new SecurityException("Invalid partial station handle.");
        }
        else ValidateHandlePair(offer.Station, offer.Desktop);
        VerifyLeaseObject(new nint(offer.Station), false, "pph2b_" + authorization.RunId, NativeStationPolicy.StationLease);
        var localStation = new SafeStationHandle(new nint(offer.Station));
        try
        {
            if (partial) return new(localStation, new SafeDesktopHandle(), "pph2b_" + authorization.RunId + "\\default");
            VerifyLeaseObject(new nint(offer.Desktop), true, "default", NativeStationPolicy.DesktopLease);
            return new(localStation, new SafeDesktopHandle(new nint(offer.Desktop)), "pph2b_" + authorization.RunId + "\\default");
        }
        catch
        {
            localStation.Dispose();
            if (localStation.CloseSucceeded is not true) throw new SecurityException("UNKNOWN/BLOCKED: partial station adoption cleanup failed.");
            throw;
        }
    }
    internal bool CloseVerified()
    {
        Dispose();
        return (desktop.IsInvalid || desktop.CloseSucceeded is true) && (station.IsInvalid || station.CloseSucceeded is true);
    }
    internal void VerifyStationLease()
    {
        if (station.IsClosed || desktop.IsClosed) throw new SecurityException("Closed station lease cannot be reused.");
        VerifyLeaseObject(station.DangerousGetHandle(), false, Name.Split('\\')[0], NativeStationPolicy.StationLease);
        VerifyLeaseObject(desktop.DangerousGetHandle(), true, "default", NativeStationPolicy.DesktopLease);
    }
    internal void VerifyNoReopen(SafeProcessHandle controller)
    {
        foreach (nint handle in new[] { station.DangerousGetHandle(), desktop.DangerousGetHandle() })
        {
            byte[] bytes = new byte[65536]; uint information = 7;
            if (!GetUserObjectSecurity(handle, ref information, bytes, (uint)bytes.Length, out uint length) || length is < 20 or > 65536)
                throw new SecurityException("Effective-rights descriptor readback failed.");
            VerifyNoReopenDescriptor(bytes, controller);
        }
    }
    // Authz accepts the existing primary token with TOKEN_QUERY; no token
    // duplication or impersonation authority is acquired for an access check.
    internal static void VerifyNoReopenDescriptor(byte[] descriptor, SafeProcessHandle controller)
    {
        if (!OpenProcessToken(controller, 8, out SafeAccessTokenHandle primary)) throw new Win32Exception(Marshal.GetLastPInvokeError());
        using (primary)
        {
            if (!AuthzInitializeResourceManager(1, 0, 0, 0, null, out nint manager)) throw new Win32Exception(Marshal.GetLastPInvokeError());
            nint context = 0; bool contextClosed = true, managerClosed;
            try
            {
                if (!AuthzInitializeContextFromToken(0, primary, manager, 0, 0, 0, out context)) throw new Win32Exception(Marshal.GetLastPInvokeError());
                nint output = Marshal.AllocHGlobal(12);
                try
                {
                    Marshal.WriteInt32(output, -1); Marshal.WriteInt32(output, 4, -1); Marshal.WriteInt32(output, 8, -1);
                    var request = new AccessRequest { Desired = 0x2000000 }; // MAXIMUM_ALLOWED, no generic rights in the fixed DACL.
                    var reply = new AccessReply { Count = 1, Granted = output, Sacl = output + 4, Error = output + 8 };
                    if (!AuthzAccessCheck(0, context, ref request, 0, descriptor, 0, 0, ref reply, 0) ||
                        Marshal.ReadInt32(output) != 0 || Marshal.ReadInt32(output, 8) is not (0 or 5))
                        throw new SecurityException("Controller has named USER-object reopening rights or access check is unverifiable.");
                }
                finally { Marshal.FreeHGlobal(output); }
            }
            finally
            {
                if (context != 0) contextClosed = AuthzFreeContext(context);
                managerClosed = AuthzFreeResourceManager(manager);
            }
            if (!contextClosed || !managerClosed) throw new SecurityException("UNKNOWN/BLOCKED: access-check cleanup failed.");
        }
    }
    private static void VerifyLeaseObject(nint handle, bool isDesktop, string name, uint access)
    {
        if (ObjectName(handle) != name) throw new SecurityException("Station lease name mismatch.");
        byte[] type = new byte[128];
        if (!GetUserObjectInformationW(handle, 3, type, (uint)type.Length, out uint typeLength) || typeLength is < 2 or > 128 || typeLength % 2 != 0 ||
            System.Text.Encoding.Unicode.GetString(type, 0, (int)typeLength).TrimEnd('\0') != (isDesktop ? "Desktop" : "WindowStation"))
            throw new SecurityException("Station lease USER-object type mismatch.");
        byte[] flags = new byte[12];
        if (!GetUserObjectInformationW(handle, 1, flags, 12, out uint flagLength) || flagLength != 12 || BitConverter.ToInt32(flags) != 0)
            throw new SecurityException("Inheritable or unverifiable station lease.");
        byte[] basic = new byte[56];
        int status = NtQueryObject(handle, 0, basic, 56, out uint returned);
        if (status != 0 || returned != 56 || BitConverter.ToUInt32(basic, 4) != access)
            throw new SecurityException("Granted USER-object rights cannot be verified; no wider query rights are requested.");
        byte[] security = new byte[65536]; uint information = 0x17; // owner, group, DACL, mandatory label only, not auditing SACL.
        if (!GetUserObjectSecurity(handle, ref information, security, (uint)security.Length, out uint length) || length is < 20 or > 65536)
            throw new SecurityException("USER-object descriptor/label readback unavailable under fixed rights.");
        VerifyLeaseDescriptor(new RawSecurityDescriptor(security, 0), isDesktop);
    }
    internal static void VerifyLeaseDescriptor(RawSecurityDescriptor actual, bool isDesktop)
    {
        var expected = new RawSecurityDescriptor(NativeStationPolicy.Descriptor(isDesktop));
        if (actual.GetSddlForm(AccessControlSections.All) != expected.GetSddlForm(AccessControlSections.All))
            throw new SecurityException("USER-object owner/DACL/mandatory-label mismatch.");
        // Exact DACL grants only BA creator rights and OWNER_RIGHTS READ_CONTROL.
        // No ordinary controller SID can reopen the name or gain implicit WRITE_DAC.
    }
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DuplicateHandle(nint sourceProcess, nint source, SafeProcessHandle target,
        out nint duplicate, uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint options);
    [DllImport("ntdll.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int NtQueryObject(nint handle, int information, [Out] byte[] bytes, uint length, out uint returned);
    [StructLayout(LayoutKind.Sequential)]
    private struct AccessRequest { internal uint Desired; internal nint PrincipalSelf, ObjectTypes; internal uint Count; internal nint Optional; }
    [StructLayout(LayoutKind.Sequential)]
    private struct AccessReply { internal uint Count; internal nint Granted, Sacl, Error; }
    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool OpenProcessToken(SafeProcessHandle process, uint access, out SafeAccessTokenHandle token);
    [DllImport("authz.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AuthzInitializeResourceManager(uint flags, nint check, nint groups, nint free, string? name, out nint manager);
    [DllImport("authz.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AuthzInitializeContextFromToken(uint flags, SafeAccessTokenHandle token, nint manager, nint expiration, long identifier, nint groups, out nint context);
    [DllImport("authz.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AuthzAccessCheck(uint flags, nint context, ref AccessRequest request, nint audit,
        byte[] descriptor, nint optional, uint count, ref AccessReply reply, nint results);
    [DllImport("authz.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AuthzFreeContext(nint context);
    [DllImport("authz.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AuthzFreeResourceManager(nint manager);
}
