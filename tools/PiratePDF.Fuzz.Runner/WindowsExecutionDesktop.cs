using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

/// <summary>A private noninteractive Window Station; never grants the child access to the operator's WinSta0.</summary>
[SupportedOSPlatform("windows")]
internal sealed partial class WindowsExecutionDesktop : IDisposable
{
    private static readonly object StationChange = new();
    private readonly SafeStationHandle station;
    private readonly SafeDesktopHandle desktop;
    internal string Name { get; }

#if !STATION_ONLY
    internal WindowsExecutionDesktop(string runId, string accountSid, string restrictingSid)
        : this(runId, accountSid, restrictingSid, null) { }

    // Only the controlled qualification helper selects the package-specific nonprivileged path.
    internal WindowsExecutionDesktop(string runId, string accountSid, string restrictingSid, string? packageSid)
    {
        if (runId.Length != 8 || !runId.All(Uri.IsHexDigit)) throw new ArgumentException("Invalid desktop owner.");
        Name = "pph2b_" + runId + "\\default";
        using WindowsIdentity controller = WindowsIdentity.GetCurrent();
        string owner = controller.User?.Value ?? throw new IOException("Missing controller SID.");
        var descriptor = new RawSecurityDescriptor(packageSid is null
            ? "O:BAG:BAD:P(A;;GA;;;SY)(A;;GA;;;BA)(A;;0x2037f;;;" + accountSid + ")(A;;0x2037f;;;" + restrictingSid + ")"
            : QualificationDescriptor(owner, restrictingSid, packageSid, false));
        byte[] bytes = new byte[descriptor.BinaryLength];
        descriptor.GetBinaryForm(bytes, 0);
        nint buffer = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            var security = new WindowsCapturePipe.SecurityAttributes { Length = Marshal.SizeOf<WindowsCapturePipe.SecurityAttributes>(), Descriptor = buffer };
            station = CreateWindowStationW(packageSid is null ? "pph2b_" + runId : null, 1,
                packageSid is null ? 0xf037fU : 0x2000aU, security); // Never adopt a foreign existing station.
            if (station.IsInvalid)
            {
                int error = Marshal.GetLastPInvokeError(); station.Dispose();
                if (packageSid is not null && error == 183)
                    throw new Win32Exception(error, "The authentication-context station name already exists; exclusive private-station admission refused.");
                throw new Win32Exception(error);
            }
            try
            {
                string desktopName = packageSid is null ? "default" : "pph2b_" + runId;
                if (packageSid is not null)
                {
                    VerifyDescriptor(station.DangerousGetHandle(), descriptor);
                    string stationName = ObjectName(station.DangerousGetHandle());
                    if (stationName.Equals("WinSta0", StringComparison.OrdinalIgnoreCase)) throw new IOException("Interactive station refused.");
                    Name = stationName + "\\" + desktopName;
                }
                var desktopSecurity = new RawSecurityDescriptor(packageSid is null
                    ? "O:BAG:BAD:P(A;;GA;;;SY)(A;;GA;;;BA)(A;;0x201ff;;;" + accountSid + ")(A;;0x201ff;;;" + restrictingSid + ")"
                    : QualificationDescriptor(owner, restrictingSid, packageSid, true));
                if (desktopSecurity.BinaryLength != bytes.Length) throw new IOException("Desktop security size mismatch.");
                desktopSecurity.GetBinaryForm(bytes, 0);
                Marshal.Copy(bytes, 0, buffer, bytes.Length);
                lock (StationChange)
                {
                    nint original = GetProcessWindowStation();
                    if (original == nint.Zero || !SetProcessWindowStation(station.DangerousGetHandle())) throw new Win32Exception(Marshal.GetLastPInvokeError());
                    bool restored;
                    try { desktop = CreateDesktopW(desktopName, nint.Zero, nint.Zero, 0, packageSid is null ? 0xf01ffU : 0x20083U, security); }
                    finally { restored = SetProcessWindowStation(original); }
                    if (!restored) { desktop.Dispose(); throw new Win32Exception(Marshal.GetLastPInvokeError(), "Cannot restore controller Window Station."); }
                    if (desktop.IsInvalid) { desktop.Dispose(); throw new Win32Exception("Cannot create the private execution desktop."); }
                    if (packageSid is not null)
                    {
                        try
                        {
                            VerifyDescriptor(desktop.DangerousGetHandle(), desktopSecurity);
                            if (ObjectName(desktop.DangerousGetHandle()) != desktopName) throw new IOException("Desktop identity mismatch.");
                        }
                        catch { desktop.Dispose(); throw; }
                    }
                }
            }
            catch { station.Dispose(); throw; }
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    // Read-only diagnosis: the current-process handle is borrowed, never closed or adopted.
    internal static string[] ObserveCurrentStation()
    {
        nint handle = GetProcessWindowStation();
        if (handle == nint.Zero) throw new Win32Exception(Marshal.GetLastPInvokeError());
        byte[] bytes = new byte[65536]; uint information = 7;
        if (!GetUserObjectSecurity(handle, ref information, bytes, (uint)bytes.Length, out _))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        return [ObjectName(handle), new RawSecurityDescriptor(bytes, 0).GetSddlForm(
            AccessControlSections.Owner | AccessControlSections.Group | AccessControlSections.Access)];
    }

    // Permanent admission diagnostics. No existing station is adopted or changed.
    internal static Dictionary<string, object?> InspectCreation(string packageSid, bool compareCreation)
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        using WindowsIdentity? impersonation = WindowsIdentity.GetCurrent(true);
        if (impersonation is not null || new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            throw new SecurityException("Station inspection requires the existing non-elevated, non-impersonating controller.");
        string owner = identity.User?.Value ?? throw new SecurityException("Missing controller identity.");
        string logon = NativeExecutionIdentity.LogonSid(identity.AccessToken);
        var facts = new Dictionary<string, object?> { ["ControllerSid"] = owner, ["LogonSid"] = logon,
            ["ProcessId"] = Environment.ProcessId, ["NativeThreadId"] = GetCurrentThreadId(),
            ["ThreadImpersonating"] = false, ["ComparedCreation"] = compareCreation,
            ["LogonGroupNameHypothesis"] = LogonStationCandidate(logon), ["CandidateIsInference"] = true,
            ["CreationFlags"] = 1, ["AccessMask"] = "0x2000a", ["HandleInheritable"] = false };
        string authenticationName;
        nint statistics = Marshal.AllocHGlobal(64);
        try
        {
            if (!WindowsIdentityMethods.GetTokenInformation(identity.AccessToken, 10, statistics, 64, out int returned))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            if (returned != 56 || Marshal.ReadInt32(statistics, 24) != 1) throw new SecurityException("Unexpected primary token statistics.");
            uint low = unchecked((uint)Marshal.ReadInt32(statistics, 8));
            uint high = unchecked((uint)Marshal.ReadInt32(statistics, 12));
            facts["AuthenticationId"] = new { Low = low, High = high };
            authenticationName = AuthenticationStationName(high, low);
            facts["GeneratedNameCandidate"] = authenticationName;
            facts["NameBasis"] = "Primary token AuthenticationId, not the logon-group SID";
        }
        finally { Marshal.FreeHGlobal(statistics); }
        var results = new List<Dictionary<string, object?>>();
        facts["CreationResults"] = results;
        if (compareCreation)
        {
            var expected = new RawSecurityDescriptor(QualificationDescriptor(owner, owner, packageSid, false));
            byte[] bytes = new byte[expected.BinaryLength]; expected.GetBinaryForm(bytes, 0);
            facts["RequestedSecurity"] = expected.GetSddlForm(AccessControlSections.Owner | AccessControlSections.Group | AccessControlSections.Access);
            nint buffer = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, buffer, bytes.Length);
                var security = new WindowsCapturePipe.SecurityAttributes { Length = Marshal.SizeOf<WindowsCapturePipe.SecurityAttributes>(), Descriptor = buffer };
                facts["SecurityAttributesSize"] = security.Length;
                // First call precedes USER enumeration/getters: inspection must not seed its own collision.
                foreach (string variant in new[] { "managed-null", "managed-empty", "native-null-pointer", "authentication-name", "fresh-explicit-name" })
                {
                    string? requestedName = variant switch { "managed-null" or "native-null-pointer" => null,
                        "managed-empty" => "", "authentication-name" => authenticationName, _ => "pph2b_" + Guid.NewGuid().ToString("N")[..16] };
                    SafeStationHandle Create() => variant == "native-null-pointer"
                        ? CreateStationWithPointer(nint.Zero, 1, 0x2000a, security)
                        : CreateWindowStationW(requestedName, 1, 0x2000a, security);
                    SafeStationHandle handle = Create();
                    int error = Marshal.GetLastPInvokeError();
                    var result = new Dictionary<string, object?> { ["Variant"] = variant, ["Success"] = !handle.IsInvalid,
                        ["RequestedName"] = requestedName, ["Win32Error"] = handle.IsInvalid ? error : 0, ["AdoptedForExecution"] = false };
                    results.Add(result);
                    bool duplicateCloseFailed = false;
                    try
                    {
                        if (!handle.IsInvalid)
                        {
                            result["Name"] = ObjectName(handle.DangerousGetHandle());
                            VerifyDescriptor(handle.DangerousGetHandle(), expected);
                            result["DescriptorVerified"] = true;
                            using SafeStationHandle duplicate = Create();
                            int duplicateError = Marshal.GetLastPInvokeError();
                            result["DuplicateRejected"] = duplicate.IsInvalid && duplicateError == 183;
                            result["DuplicateError"] = duplicate.IsInvalid ? duplicateError : 0;
                            duplicate.Dispose();
                            result["DuplicateCloseSucceeded"] = duplicate.CloseSucceeded;
                            result["DuplicateCloseError"] = duplicate.CloseError;
                            duplicateCloseFailed = duplicate.CloseSucceeded is false;
                        }
                    }
                    catch (Exception observation) when (observation is Win32Exception or IOException)
                    { result["VerificationFailure"] = observation.Message; }
                    finally { handle.Dispose(); }
                    result["CloseSucceeded"] = handle.CloseSucceeded;
                    result["CloseError"] = handle.CloseError;
                    if (duplicateCloseFailed || handle.CloseSucceeded is false) { facts["CleanupVerified"] = false; return facts; }
                }
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        var names = new List<string>();
        bool bounded = false;
        StationCallback callback = (name, _) =>
        {
            if (names.Count >= 128 || name.Length is < 1 or > 256) { bounded = true; return false; }
            names.Add(name); return true;
        };
        bool enumerated = EnumWindowStationsW(callback, nint.Zero);
        int enumerationError = Marshal.GetLastPInvokeError();
        facts["Enumeration"] = new { Success = enumerated, Error = enumerated ? 0 : enumerationError,
            Bounded = bounded, Names = names, AccessFiltered = true };
        try { facts["CurrentStation"] = ObserveCurrentStation(); }
        catch (Win32Exception observation) { facts["CurrentStationError"] = observation.NativeErrorCode; }
        var inspected = new List<object>();
        foreach (string name in new[] { authenticationName, LogonStationCandidate(logon) }.Concat(names).Distinct(StringComparer.OrdinalIgnoreCase).Take(16))
        {
            using SafeStationHandle handle = OpenWindowStationW(name, false, 0x20002);
            int error = Marshal.GetLastPInvokeError();
            string? actualName = null, security = null;
            int? queryError = null;
            if (!handle.IsInvalid)
            {
                try
                {
                    actualName = ObjectName(handle.DangerousGetHandle());
                    byte[] bytes = new byte[65536]; uint information = 7;
                    if (!GetUserObjectSecurity(handle.DangerousGetHandle(), ref information, bytes, (uint)bytes.Length, out _))
                        throw new Win32Exception(Marshal.GetLastPInvokeError());
                    security = new RawSecurityDescriptor(bytes, 0).GetSddlForm(AccessControlSections.Owner | AccessControlSections.Group | AccessControlSections.Access);
                }
                catch (Win32Exception observation) { queryError = observation.NativeErrorCode; }
            }
            bool opened = !handle.IsInvalid;
            handle.Dispose();
            inspected.Add(new { RequestedName = name, Opened = opened, Error = opened ? 0 : error,
                ActualName = actualName, Security = security, QueryError = queryError,
                handle.CloseSucceeded, handle.CloseError, ReadOnly = true });
            if (handle.CloseSucceeded is false) { facts["CleanupVerified"] = false; facts["ReadOnlyStations"] = inspected; return facts; }
        }
        facts["ReadOnlyStations"] = inspected;
        facts["CleanupVerified"] = true;
        return facts;
    }

    internal static string LogonStationCandidate(string sid)
    {
        string[] parts = sid.Split('-');
        if (parts.Length != 6 || !sid.StartsWith("S-1-5-5-", StringComparison.Ordinal) ||
            !uint.TryParse(parts[4], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out uint high) ||
            !uint.TryParse(parts[5], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out uint low))
            throw new SecurityException("Invalid native logon SID.");
        return AuthenticationStationName(high, low);
    }

    internal static string AuthenticationStationName(uint high, uint low) => $"Service-0x{high:x}-{low:x}$";
    private static string QualificationDescriptor(string controller, string restriction, string package, bool isDesktop)
    {
        _ = new SecurityIdentifier(controller);
        _ = new SecurityIdentifier(restriction);
        var sid = new SecurityIdentifier(package);
        if (!sid.Value.StartsWith("S-1-15-2-", StringComparison.Ordinal) || sid.BinaryLength != 40)
            throw new ArgumentException("A specific package SID is required.");
        string controllerRights = isDesktop ? "0x20083" : "0x2000a";
        string workerRights = isDesktop ? "0x81" : "0x2";
        return "O:" + controller + "G:" + controller + "D:P(A;;" + controllerRights + ";;;" + controller +
            ")(A;;" + controllerRights + ";;;" + restriction + ")(A;;" + workerRights + ";;;" + package + ")";
    }

#endif
    public void Dispose() { desktop.Dispose(); station.Dispose(); }

    private static string ObjectName(nint handle)
    {
        byte[] bytes = new byte[1024];
        if (!GetUserObjectInformationW(handle, 2, bytes, (uint)bytes.Length, out uint needed)) throw new Win32Exception(Marshal.GetLastPInvokeError());
        if (needed is < 2 or > 1024 || needed % 2 != 0) throw new IOException("Invalid user-object name length.");
        return System.Text.Encoding.Unicode.GetString(bytes, 0, (int)needed).TrimEnd('\0');
    }

    private static void VerifyDescriptor(nint handle, RawSecurityDescriptor expected)
    {
        byte[] bytes = new byte[65536]; uint information = 7;
        if (!GetUserObjectSecurity(handle, ref information, bytes, (uint)bytes.Length, out _)) throw new Win32Exception(Marshal.GetLastPInvokeError());
        var actual = new RawSecurityDescriptor(bytes, 0);
        const AccessControlSections sections = AccessControlSections.Owner | AccessControlSections.Group | AccessControlSections.Access;
        if (actual.GetSddlForm(sections) != expected.GetSddlForm(sections)) throw new IOException("User-object security readback mismatch.");
    }

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetUserObjectInformationW(nint handle, int information, [Out] byte[] bytes, uint length, out uint needed);
    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetUserObjectSecurity(nint handle, ref uint information, [Out] byte[] bytes, uint length, out uint needed);
    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeStationHandle CreateWindowStationW(string? name, uint flags, uint access, in WindowsCapturePipe.SecurityAttributes security);
    [DllImport("user32.dll", EntryPoint = "CreateWindowStationW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeStationHandle CreateStationWithPointer(nint name, uint flags, uint access, in WindowsCapturePipe.SecurityAttributes security);
    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)] private delegate bool StationCallback([MarshalAs(UnmanagedType.LPWStr)] string name, nint context);
    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool EnumWindowStationsW(StationCallback callback, nint context);
    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeStationHandle OpenWindowStationW(string name, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint access);
    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern nint GetProcessWindowStation();
    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetProcessWindowStation(nint station);
    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeDesktopHandle CreateDesktopW(string name, nint device, nint settings, uint flags, uint access, in WindowsCapturePipe.SecurityAttributes security);
}

internal sealed class SafeStationHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeStationHandle() : base(true) { }
    internal SafeStationHandle(nint owned) : base(true) { SetHandle(owned); }
    internal bool? CloseSucceeded { get; private set; }
    internal int? CloseError { get; private set; }
    protected override bool ReleaseHandle()
    {
        bool closed = CloseWindowStation(handle);
        int error = Marshal.GetLastPInvokeError();
        CloseSucceeded = closed; CloseError = closed ? 0 : error;
        return closed;
    }
    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseWindowStation(nint station);
}
internal sealed class SafeDesktopHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeDesktopHandle() : base(true) { }
    internal SafeDesktopHandle(nint owned) : base(true) { SetHandle(owned); }
    internal bool? CloseSucceeded { get; private set; }
    internal int? CloseError { get; private set; }
    protected override bool ReleaseHandle()
    {
        bool closed = CloseDesktop(handle);
        int error = Marshal.GetLastPInvokeError();
        CloseSucceeded = closed; CloseError = closed ? 0 : error;
        return closed;
    }
    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseDesktop(nint desktop);
}
