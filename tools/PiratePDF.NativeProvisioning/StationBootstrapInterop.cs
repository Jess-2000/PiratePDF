// Fixed interop shared with the trusted PowerShell parent. Kept compatible with
// Windows PowerShell's compiler: no child CLR is needed to obtain OS identities.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner
{
#if NET
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    public static class StationBootstrapInterop
    {
        public static SafeProcessHandle OpenPeer(int pid, bool duplication)
        {
            SafeProcessHandle handle = OpenProcess(0x101000U | (duplication ? 0x40U : 0U), false, pid);
            if (handle.IsInvalid) { int error = Marshal.GetLastWin32Error(); handle.Dispose(); throw new Win32Exception(error); }
            return handle;
        }
        public static string FileId(SafeFileHandle handle)
        {
            byte[] bytes = new byte[24];
            if (!GetFileInformationByHandleEx(handle, 18, bytes, 24)) throw new Win32Exception(Marshal.GetLastWin32Error());
#if NET
            return Convert.ToHexStringLower(bytes);
#else
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
#endif
        }
        public static string Image(SafeProcessHandle process)
        {
            char[] text = new char[32768]; int length = text.Length;
            if (!QueryFullProcessImageNameW(process, 0, text, ref length)) throw new Win32Exception(Marshal.GetLastWin32Error());
            return new string(text, 0, length);
        }
        public static bool Exited(SafeProcessHandle process) { return WaitForSingleObject(process, 0) == 0; }
        public static uint ExitCode(SafeProcessHandle process)
        {
            uint code;
            if (!GetExitCodeProcess(process, out code)) throw new Win32Exception(Marshal.GetLastWin32Error());
            return code;
        }
        // A bounded exit word preserves pre-pipe failures without inheriting a
        // privileged output handle or introducing another privileged file sink.
        public static int FailureExit(string stage, int error, bool unknown)
        {
            int state;
            switch (stage)
            {
                case "TrustedBootstrapFailure": state = 1; break;
                case "ControllerIdentityMismatch": state = 2; break;
                case "AuthorizationReplayOrClaimFailure": state = 3; break;
                case "EndpointIdentityMismatch": state = 4; break;
                case "StationCollision": state = 5; break;
                case "StationCreationFailure": state = 6; break;
                case "DesktopCreationFailure": state = 7; break;
                case "DescriptorOrLabelMismatch": state = 8; break;
                case "FirstDuplicationFailure": state = 9; break;
                case "SecondDuplicationFailure": state = 10; break;
                case "MissingLateOrRefusedAcknowledgment": state = 11; break;
                case "HelperCleanupFailure": case "UNKNOWN/BLOCKED:CleanupFailure": state = 12; unknown = true; break;
                default: state = 0; unknown = true; break;
            }
            if (error < 0 || error > 65535) { error = 65535; unknown = true; }
            return 0x40000000 | (unknown ? 0x20000000 : 0) | (state << 16) | error;
        }
        public static string[] DecodeFailure(uint code)
        {
            string[] states = { "UnclassifiedFailure", "TrustedBootstrapFailure", "ControllerIdentityMismatch", "AuthorizationReplayOrClaimFailure",
                "EndpointIdentityMismatchOrCollision", "StationCollision", "StationCreationFailure", "DesktopCreationFailure", "DescriptorOrLabelMismatch",
                "FirstDuplicationFailure", "SecondDuplicationFailure", "MissingLateOrRefusedAcknowledgment", "HelperCleanupFailure" };
            uint index = (code >> 16) & 0xff;
            if ((code & 0x40000000) == 0 || index >= states.Length) return new[] { "UnexpectedHelperExit", code.ToString(System.Globalization.CultureInfo.InvariantCulture), "UNKNOWN/BLOCKED" };
            return new[] { states[index], (code & 65535).ToString(System.Globalization.CultureInfo.InvariantCulture), (code & 0x20000000) != 0 ? "UNKNOWN/BLOCKED" : "Refused" };
        }
#if !NET
        // Root protective leases in the resident trusted parent on an unverified exit.
        // Parent crash/exit is not made safe by this root; such a result remains BLOCKED.
        private static IDisposable[] ownedCodeLeases;
        private static System.Collections.Generic.List<IDisposable> ownedLeaseSource;
        private static SafeProcessHandle ownedHelper;
        private static System.Diagnostics.Stopwatch ownedClock;
        private static bool firstOwnedWait;
        private static StationHelperCleanupResult ownedOutcome;
        private static IntPtr stationProbe;
        private static string stationProbeName;
        private static SafeProcessHandle observedController;

        // Used only by the already-trusted permanent PowerShell parent. This
        // launch method is absent from the privileged managed publishing output.
        // Unlike Framework Process.Start, explicitly disables ALL inheritance.
        public static SafeProcessHandle LaunchStationHelper(string id, out int pid, System.Collections.Generic.List<IDisposable> codeLeases)
        {
            if (id == null || !System.Text.RegularExpressions.Regex.IsMatch(id, "\\A[a-f0-9]{32}\\z"))
                throw new SecurityException("Invalid fixed authorization identity.");
            StationHelperCleanup.ValidateResidentArguments(Environment.GetCommandLineArgs());
            using (var selfProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var self = OpenPeer(selfProcess.Id, false))
            {
                string expectedHost = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
                if (IntPtr.Size != 8 || !Image(self).Equals(expectedHost, StringComparison.OrdinalIgnoreCase))
                    throw new SecurityException("Only the independently trusted resident x64 Windows PowerShell host is admitted.");
            }
            if (ownedCodeLeases != null || stationProbe != IntPtr.Zero) throw new SecurityException("UNKNOWN/BLOCKED: protective ownership from an earlier operation remains retained.");
            if (codeLeases == null || codeLeases.Count < 1 || codeLeases.Count > 1024) throw new SecurityException("Invalid bootstrap lease inventory.");
            ownedCodeLeases = codeLeases.ToArray(); ownedLeaseSource = codeLeases; ownedOutcome = null; ownedHelper = null;
            firstOwnedWait = true; ownedClock = System.Diagnostics.Stopwatch.StartNew();
            string code = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "PiratePDF", "NativeProvisioning", "StationV1");
            string runtime = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var environment = new System.Collections.Generic.SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            environment.Add("SystemRoot", windows); environment.Add("WINDIR", windows);
            environment.Add("PATH", System.IO.Path.Combine(windows, "System32")); environment.Add("DOTNET_ROOT", runtime);
            environment.Add("DOTNET_MULTILEVEL_LOOKUP", "0"); environment.Add("DOTNET_EnableDiagnostics", "0");
            environment.Add("DOTNET_EnableDiagnostics_IPC", "0"); environment.Add("DOTNET_EnableDiagnostics_Profiler", "0");
            environment.Add("CORECLR_ENABLE_PROFILING", "0"); environment.Add("COR_ENABLE_PROFILING", "0");
            environment.Add("DOTNET_CLI_TELEMETRY_OPTOUT", "1");
            var block = new System.Text.StringBuilder();
            foreach (var item in environment) block.Append(item.Key).Append('=').Append(item.Value).Append('\0');
            block.Append('\0');
            IntPtr buffer = Marshal.StringToHGlobalUni(block.ToString());
            try
            {
                var startup = new StartupInfo(); startup.Size = Marshal.SizeOf(typeof(StartupInfo));
                ProcessInformation information;
                string image = System.IO.Path.Combine(runtime, "dotnet.exe");
                var command = new System.Text.StringBuilder("\"" + image + "\" \"" + System.IO.Path.Combine(code, "PiratePDF.NativeProvisioning.Station.dll") + "\" StationLeaseOnlyV1 " + id);
                if (!CreateProcessW(image, command, IntPtr.Zero, IntPtr.Zero, false, 0x08000400, buffer, code, ref startup, out information))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                ownedHelper = new SafeProcessHandle(information.Process, true);
                using (var thread = new SafeWaitHandle(information.Thread, true)) { }
                pid = information.Pid;
                return ownedHelper; // Borrowed by script; only CompleteOwnedHelper releases ownership.
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        public static StationHelperCleanupResult CompleteOwnedHelper(bool abort, string failure, System.Collections.Generic.List<IDisposable> codeLeases)
        {
            if (!object.ReferenceEquals(ownedLeaseSource, codeLeases)) return new StationHelperCleanupResult { OriginalFailure = failure };
            if (ownedOutcome != null) return ownedOutcome; // No repeated termination/retry.
            ownedOutcome = ownedHelper == null ? StationHelperCleanup.NoLaunch(failure) :
                StationHelperCleanup.Complete(abort, failure, WaitOwned, TerminateOwned, ExitOwned);
            ownedOutcome.OwnershipEstablished = true;
            if (ownedOutcome.HelperCreated && !ownedOutcome.ExitVerified)
            {
                ownedOutcome.OwnershipRetained = ownedCodeLeases != null;
                return ownedOutcome;
            }
            if (ownedHelper != null) { ownedHelper.Dispose(); ownedHelper = null; }
            if (ownedCodeLeases != null)
            {
                for (int i = ownedCodeLeases.Length - 1; i >= 0; i--)
                {
                    try { ownedCodeLeases[i].Dispose(); }
                    catch (System.IO.IOException) { ownedOutcome.LeaseCloseFailures++; }
                    catch (UnauthorizedAccessException) { ownedOutcome.LeaseCloseFailures++; }
                    catch (InvalidOperationException) { ownedOutcome.LeaseCloseFailures++; }
                }
                if (ownedOutcome.LeaseCloseFailures == 0) { ownedCodeLeases = null; ownedOutcome.CodeLeasesReleased = true; }
                else ownedOutcome.OwnershipRetained = true;
            }
            return ownedOutcome;
        }

        private static long WaitOwned(uint maximum)
        {
            uint timeout = maximum;
            if (firstOwnedWait)
            {
                firstOwnedWait = false;
                timeout = (uint)Math.Max(0, Math.Min(maximum, 10000 - ownedClock.ElapsedMilliseconds));
            }
            uint status = WaitForSingleObject(ownedHelper, timeout);
            int error = status == uint.MaxValue ? Marshal.GetLastWin32Error() : 0;
            return unchecked((long)(((ulong)(uint)error << 32) | status));
        }
        private static long TerminateOwned()
        {
            bool success = TerminateProcess(ownedHelper, 0x600c0000);
            int error = success ? 0 : Marshal.GetLastWin32Error();
            return unchecked((long)(((ulong)(uint)error << 32) | (success ? 1u : 0u)));
        }
        private static long ExitOwned()
        {
            uint code; bool success = GetExitCodeProcess(ownedHelper, out code);
            int error = success ? 0 : Marshal.GetLastWin32Error();
            return unchecked((long)(((ulong)(uint)error << 32) | code));
        }
        public static StationLifetimeResult ObserveStationLifetime(string runId, SafeProcessHandle controller, int session, StationHelperCleanupResult helper)
        {
            NoImpersonation();
            int actualSession;
            using (var observer = System.Diagnostics.Process.GetCurrentProcess())
            using (SafeProcessHandle self = OpenPeer(observer.Id, false))
            {
                string[] facts = Facts(self);
                actualSession = int.Parse(facts[5], System.Globalization.CultureInfo.InvariantCulture);
                if (actualSession <= 0 || actualSession != session || facts[6] != "12288" || facts[7] != "True" || facts[8] != "True")
                    throw new SecurityException("Unapproved administrative observation context.");
            }
            if (stationProbe != IntPtr.Zero) throw new SecurityException("UNKNOWN/BLOCKED: an earlier observation handle remains unclosed.");
            observedController = controller;
            stationProbeName = "pph2b_" + runId;
            try
            {
                StationLifetimeResult result = StationLifetimeEvidence.Observe(runId,
                    helper.HelperCreated && helper.ExitVerified && helper.ExitCode == 0 && !helper.TerminationAttempted && helper.CodeLeasesReleased,
                    WaitController, ExitController, OpenStationProbe, CloseStationProbe);
                result.ObserverSession = actualSession;
                return result;
            }
            finally { observedController = null; }
        }
        private static long WaitController(uint milliseconds)
        {
            uint value = WaitForSingleObject(observedController, milliseconds);
            return unchecked((long)(((ulong)(uint)(value == uint.MaxValue ? Marshal.GetLastWin32Error() : 0) << 32) | value));
        }
        private static long ExitController()
        {
            uint value; bool success = GetExitCodeProcess(observedController, out value);
            return unchecked((long)(((ulong)(uint)(success ? 0 : Marshal.GetLastWin32Error()) << 32) | value));
        }
        private static long OpenStationProbe()
        {
            stationProbe = OpenWindowStationW(stationProbeName, false, 0x20000); // READ_CONTROL only; never creates/associates.
            bool opened = stationProbe != IntPtr.Zero;
            return unchecked((long)(((ulong)(uint)(opened ? 0 : Marshal.GetLastWin32Error()) << 32) | (opened ? 1u : 0u)));
        }
        private static long CloseStationProbe()
        {
            bool closed = CloseWindowStation(stationProbe);
            int error = closed ? 0 : Marshal.GetLastWin32Error();
            if (closed) stationProbe = IntPtr.Zero; // Failed close retains the exact owned reference, never a remote numeric handle.
            return unchecked((long)(((ulong)(uint)error << 32) | (closed ? 1u : 0u)));
        }
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr OpenWindowStationW(string name, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint access);
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseWindowStation(IntPtr station);
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool TerminateProcess(SafeProcessHandle process, uint code);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo
        {
            public int Size; public IntPtr Reserved, Desktop, Title;
            public int X, Y, XSize, YSize, XCount, YCount, Fill, Flags;
            public short Show, ReservedLength; public IntPtr ReservedData, Input, Output, Error;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation { public IntPtr Process, Thread; public int Pid, Tid; }
        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CreateProcessW(string image, System.Text.StringBuilder command,
            IntPtr processSecurity, IntPtr threadSecurity, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint flags, IntPtr environment,
            string directory, ref StartupInfo startup, out ProcessInformation information);
#endif
        public static void NoImpersonation()
        {
            SafeAccessTokenHandle token;
            if (OpenThreadToken(new IntPtr(-2), 8, true, out token))
            { token.Dispose(); throw new SecurityException("Impersonating station caller refused."); }
            int error = Marshal.GetLastWin32Error(); token.Dispose();
            if (error != 1008) throw new Win32Exception(error);
        }
        public static string[] Facts(SafeProcessHandle process)
        {
            long creation, exit, kernel, user;
            if (!GetProcessTimes(process, out creation, out exit, out kernel, out user)) throw new Win32Exception(Marshal.GetLastWin32Error());
            SafeAccessTokenHandle token;
            if (!OpenProcessToken(process, 8, out token)) throw new Win32Exception(Marshal.GetLastWin32Error());
            using (token)
            using (var identity = new WindowsIdentity(token.DangerousGetHandle()))
            {
                string logon;
                int length;
                IntPtr buffer = Query(token, 28, out length);
                try
                {
                    if (length < (IntPtr.Size == 8 ? 24 : 16) || Marshal.ReadInt32(buffer) != 1)
                        throw new SecurityException("Unexpected logon groups.");
                    logon = new SecurityIdentifier(Marshal.ReadIntPtr(buffer, IntPtr.Size == 8 ? 8 : 4)).Value;
                }
                finally { Marshal.FreeHGlobal(buffer); }
                buffer = Query(token, 10, out length);
                string authentication, tokenId; bool primary;
                try
                {
                    if (length != 56) throw new SecurityException("Unexpected token statistics.");
                    tokenId = Marshal.ReadInt64(buffer).ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
                    authentication = Marshal.ReadInt64(buffer, 8).ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
                    primary = Marshal.ReadInt32(buffer, 24) == 1;
                }
                finally { Marshal.FreeHGlobal(buffer); }
                buffer = Query(token, 25, out length); int integrity;
                try
                {
                    if (length < IntPtr.Size + 4) throw new SecurityException("Unexpected integrity information.");
                    string sid = new SecurityIdentifier(Marshal.ReadIntPtr(buffer)).Value;
#if NET
                    integrity = int.Parse(sid.AsSpan(sid.LastIndexOf('-') + 1), System.Globalization.CultureInfo.InvariantCulture);
#else
                    integrity = int.Parse(sid.Substring(sid.LastIndexOf('-') + 1), System.Globalization.CultureInfo.InvariantCulture);
#endif
                }
                finally { Marshal.FreeHGlobal(buffer); }
                var userSid = identity.User;
                if (userSid == null) throw new SecurityException("Missing primary user identity.");
                return new[] { creation.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    userSid.Value, logon, authentication, tokenId,
                    Dword(token, 12).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    integrity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Boolean(token, 20).ToString(), new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator).ToString(),
                    primary.ToString(), Boolean(token, 29).ToString(), Boolean(token, 26).ToString() };
            }
        }
        private static bool Boolean(SafeAccessTokenHandle token, int information)
        {
            int value = Dword(token, information);
            if (value != 0 && value != 1) throw new SecurityException("Invalid DWORD token Boolean.");
            return value == 1;
        }
        private static int Dword(SafeAccessTokenHandle token, int information)
        {
            int length; IntPtr buffer = Query(token, information, out length);
            try { if (length != 4) throw new SecurityException("Unexpected DWORD token response."); return Marshal.ReadInt32(buffer); }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        private static IntPtr Query(SafeAccessTokenHandle token, int information, out int length)
        {
            IntPtr buffer = Marshal.AllocHGlobal(65536);
            if (!GetTokenInformation(token, information, buffer, 65536, out length) || length < 1 || length > 65536)
            { int error = Marshal.GetLastWin32Error(); Marshal.FreeHGlobal(buffer); throw new Win32Exception(error); }
            return buffer;
        }
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetProcessTimes(SafeProcessHandle process, out long creation, out long exit, out long kernel, out long user);
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetFileInformationByHandleEx(SafeFileHandle file, int information, [Out] byte[] bytes, int size);
        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool QueryFullProcessImageNameW(SafeProcessHandle process, uint flags, [Out] char[] image, ref int length);
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static extern uint WaitForSingleObject(SafeProcessHandle process, uint timeout);
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetExitCodeProcess(SafeProcessHandle process, out uint code);
        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool OpenProcessToken(SafeProcessHandle process, uint access, out SafeAccessTokenHandle token);
        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool OpenThreadToken(IntPtr thread, uint access, [MarshalAs(UnmanagedType.Bool)] bool asSelf, out SafeAccessTokenHandle token);
        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetTokenInformation(SafeAccessTokenHandle token, int information, IntPtr buffer, int capacity, out int length);
    }
}
