using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

/// <summary>One controlled launch experiment, never a production execution backend.</summary>
[SupportedOSPlatform("windows")]
internal static class LpacFeasibility
{
    private const BindingFlags Members = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

    // Permanent station admission inspection through the existing provisioning executable.
    // This command creates no process, profile, executable, logon or alternative supervisor.
    internal static async Task<int> InspectStationsAsync(string evidence, string runner, string expectedHash, bool compareCreation)
    {
        string resultPath = Path.Combine(evidence, "station-inspection.json");
        if (File.Exists(resultPath)) throw new IOException("Station evidence must not be overwritten.");
        var facts = new Dictionary<string, object?> { ["Status"] = "BLOCKED", ["InspectionOnly"] = true,
            ["ProductionEnabled"] = false, ["ChildCreated"] = false, ["ObservationUtc"] = DateTimeOffset.UtcNow };
        nint package = nint.Zero;
        try
        {
            using var lease = new FileStream(runner, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (lease.Length > 16777216 || !Convert.ToHexString(SHA256.HashData(lease)).Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new System.Security.SecurityException("Station manager identity mismatch.");
            string moniker = "PiratePDF.Station.Inspect." + Guid.NewGuid().ToString("N");
            int derived = LpacFeasibilityMethods.DeriveAppContainerSidFromAppContainerName(moniker, out package);
            Marshal.ThrowExceptionForHR(derived);
            if (derived != 0) throw new IOException("Unexpected package SID derivation status.");
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(runner));
            Type manager = assembly.GetType("PiratePDF.Fuzz.Runner.WindowsExecutionDesktop", true)!;
            var observation = (Dictionary<string, object?>)manager.GetMethod("InspectCreation", Members)!
                .Invoke(null, [new SecurityIdentifier(package).Value, compareCreation])!;
            facts["StationContext"] = observation;
            facts["Status"] = observation.GetValueOrDefault("CleanupVerified") is true ? "INSPECTED" : "BLOCKED";
        }
        catch (Exception failure)
        {
            Exception actual = failure is TargetInvocationException { InnerException: not null } invocation ? invocation.InnerException! : failure;
            facts["ErrorType"] = actual.GetType().Name;
            facts["Reason"] = actual.Message;
            if (actual is Win32Exception native) facts["Win32Error"] = native.NativeErrorCode;
        }
        finally { if (package != nint.Zero) _ = LpacFeasibilityMethods.FreeSid(package); }
        await using (var result = new FileStream(resultPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            await JsonSerializer.SerializeAsync(result, facts).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(facts));
        return facts["Status"]?.ToString() == "INSPECTED" ? 0 : 3;
    }

    internal static async Task<int> RunAsync(string fixture, string expectedHash, string evidence, string runner,
        bool jobOnly = false, bool admissionOnly = false, bool createProfile = false)
    {
        var facts = new Dictionary<string, object?> { ["Status"] = "BLOCKED", ["ProductionEnabled"] = false };
        facts["JobOnly"] = jobOnly;
        facts["AdmissionOnly"] = admissionOnly;
        facts["ChildResumed"] = false;
        string resultPath = Path.Combine(evidence, "launch-result.json");
        if (File.Exists(resultPath)) throw new IOException("Qualification evidence must not be overwritten.");
        try
        {
            if (createProfile && (!admissionOnly || jobOnly)) throw new ArgumentException("Profile diagnosis requires suspended-only LPAC admission.");
            if (!Environment.Is64BitProcess || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) throw new PlatformNotSupportedException();
            using WindowsIdentity current = WindowsIdentity.GetCurrent();
            facts["ControllerUserSid"] = current.User?.Value;
            char[] profileDirectory = new char[1024]; uint profileSize = (uint)profileDirectory.Length;
            bool profileKnown = LpacFeasibilityMethods.GetUserProfileDirectoryW(current.AccessToken, profileDirectory, ref profileSize);
            int profileError = Marshal.GetLastPInvokeError();
            facts["ControllerProfileDirectory"] = new { Success = profileKnown, Error = profileKnown ? 0 : profileError,
                Path = profileKnown ? new string(profileDirectory).TrimEnd('\0') : null };
            facts["EnvironmentUserProfile"] = Environment.GetEnvironmentVariable("USERPROFILE");
            try
            {
                using var hive = Microsoft.Win32.Registry.Users.OpenSubKey(current.User!.Value);
                facts["ControllerUserHiveLoaded"] = hive is not null;
            }
            catch (Exception observation) when (observation is UnauthorizedAccessException or System.Security.SecurityException or IOException)
            { facts["ControllerUserHiveInspectionError"] = observation.GetType().Name; }
            if (new WindowsPrincipal(current).IsInRole(WindowsBuiltInRole.Administrator))
                throw new UnauthorizedAccessException("This experiment requires a non-elevated controller.");
            if (!LpacFeasibilityMethods.IsProcessInJob(new nint(-1), nint.Zero, out bool nested)) throw Error();
            facts["ControllerInJob"] = nested;
            if (nested)
            {
                // Test-only admission: retain inherited Jobs and add the existing owned Job.
                // Silent breakaway would defeat that assumption even without a creation flag.
                byte[] limits = new byte[144];
                if (!LpacFeasibilityMethods.QueryInformationJobObject(nint.Zero, 9, limits, (uint)limits.Length, out _)) throw Error();
                uint flags = BitConverter.ToUInt32(limits, 16);
                facts["ControllerJobLimitFlags"] = flags;
                if ((flags & 0x1000) != 0) throw new InvalidOperationException("Inherited silent-breakaway Job is not supported by this experiment.");
                byte[] ui = new byte[4];
                if (!LpacFeasibilityMethods.QueryInformationJobObject(nint.Zero, 4, ui, 4, out _)) throw Error();
                facts["ControllerJobUiRestrictions"] = BitConverter.ToUInt32(ui);
                if (BitConverter.ToUInt32(ui) != 0) throw new InvalidOperationException("Inherited Job UI restrictions prevent this nested experiment.");
                facts["NestedJobAttemptAuthorized"] = true;
                if (!jobOnly && !admissionOnly) throw new InvalidOperationException("Nested Job ownership and cleanup must be demonstrated in Job-only mode before LPAC continuation.");
            }
            using var imageLease = new FileStream(fixture, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (!Convert.ToHexString(SHA256.HashData(imageLease)).Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Controlled fixture hash mismatch.");
            facts["FixtureSha256"] = expectedHash;
            string moniker = "PiratePDF.Lpac.Feasibility." + Guid.NewGuid().ToString("N");
            facts["ProfileMoniker"] = moniker;
            facts["Stage"] = "DerivePackageSid";
            nint package;
            int hr = LpacFeasibilityMethods.DeriveAppContainerSidFromAppContainerName(moniker, out package);
            facts["ProfileOperationHResult"] = "0x" + hr.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            Marshal.ThrowExceptionForHR(hr);
            if (hr != 0) throw new InvalidOperationException("Unexpected AppContainer profile/SID success status.");
            try
            {
                var sid = new SecurityIdentifier(package);
                facts["PackageSid"] = sid.Value;
                facts["ProfileCreated"] = false;
                // Only the newly compiled fixture is granted access; no ancestor/system ACL is changed.
                var file = new FileInfo(fixture);
                FileSecurity original = file.GetAccessControl();
                facts["FixtureOwner"] = original.GetOwner(typeof(SecurityIdentifier))?.Value ?? throw new IOException("Missing fixture owner.");
                facts["OriginalFixtureSecurity"] = Convert.ToBase64String(original.GetSecurityDescriptorBinaryForm());
                try
                {
                    facts["Stage"] = "GrantFixturePackageReadExecute";
                    FileSecurity admitted = file.GetAccessControl();
                    admitted.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.ReadAndExecute, AccessControlType.Allow));
                    file.SetAccessControl(admitted);
                    await LaunchAsync(fixture, runner, package, current.AccessToken, facts, jobOnly, admissionOnly, createProfile).ConfigureAwait(false);
                }
                finally
                {
                    var restored = new FileSecurity();
                    restored.SetSecurityDescriptorBinaryForm(original.GetSecurityDescriptorBinaryForm(), AccessControlSections.Access);
                    file.SetAccessControl(restored);
                    facts["FixtureAclRestored"] = file.GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access) ==
                        original.GetSecurityDescriptorSddlForm(AccessControlSections.Access);
                }
                if (!(bool)facts["FixtureAclRestored"]!) throw new IOException("Fixture security restoration could not be verified.");
            }
            finally
            {
                _ = LpacFeasibilityMethods.FreeSid(package);
                // Only a profile created successfully by this invocation is owned here.
                // Never adopt or delete an existing profile after ERROR_ALREADY_EXISTS.
                if (facts.TryGetValue("ProfileCreated", out object? ownedProfile) && ownedProfile is true)
                {
                    int removed = LpacFeasibilityMethods.DeleteAppContainerProfile(moniker);
                    facts["ProfileDeletionHResult"] = "0x" + removed.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
                    facts["ProfileDeleted"] = removed == 0;
                    Marshal.ThrowExceptionForHR(removed);
                }
            }
        }
        catch (Exception failure)
        {
            facts["Status"] = facts.ContainsKey("ProcessCreated") ? "UNKNOWN" : "BLOCKED";
            Exception actual = failure is TargetInvocationException { InnerException: not null } invocation ? invocation.InnerException! : failure;
            facts["ErrorType"] = actual.GetType().Name;
            facts["Reason"] = actual.Message;
            if (actual is Win32Exception native) facts["Win32Error"] = native.NativeErrorCode;
        }
        await using (var resultFile = new FileStream(resultPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            await JsonSerializer.SerializeAsync(resultFile, facts).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(facts));
        return facts["Status"]?.ToString() == "PASS" ? 0 : 3;
    }

    private static async Task LaunchAsync(string fixture, string runner, nint package, SafeAccessTokenHandle caller,
        Dictionary<string, object?> facts, bool jobOnly, bool admissionOnly, bool createProfile)
    {
        Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(runner));
        Type Type(string name) => assembly.GetType("PiratePDF.Fuzz.Runner." + name, true)!;
        object New(string name, params object[] arguments) => Activator.CreateInstance(Type(name), Members, null, arguments, null)!;
        object? Get(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value);
        object? Call(object value, string name) => value.GetType().GetMethod(name, Members)!.Invoke(value, null);
        nint groups = Marshal.AllocHGlobal(65536);
        SafeAccessTokenHandle restricted;
        string user, restriction;
        try
        {
            using var controller = new WindowsIdentity(caller.DangerousGetHandle());
            user = controller.User?.Value ?? throw new InvalidOperationException("Missing controller identity.");
            if (!LpacFeasibilityMethods.GetTokenInformation(caller, 11, groups, 65536, out _)) throw Error();
            int count = Marshal.ReadInt32(groups);
            if (count is < 0 or > 128) throw new InvalidOperationException("Invalid original restricting list.");
            var sid = count == 0 ? new SecurityIdentifier(user) : new SecurityIdentifier(Marshal.ReadIntPtr(groups, nint.Size));
            restriction = sid.Value;
            byte[] bytes = new byte[sid.BinaryLength]; sid.GetBinaryForm(bytes, 0); Marshal.Copy(bytes, 0, groups, bytes.Length);
            if (!LpacFeasibilityMethods.CreateRestrictedToken(caller, 1, 0, nint.Zero, 0, nint.Zero, 1,
                [new() { Sid = groups }], out restricted)) throw Error();
        }
        finally { Marshal.FreeHGlobal(groups); }
        using SafeAccessTokenHandle ownedRestricted = restricted;
        using var identity = (IDisposable)New("NativeExecutionIdentity", restricted, user, restriction);
        // Preserve the existing verifier rather than silently relaxing its privilege/restriction contract.
        facts["Stage"] = "VerifyRestrictedControllerIdentity";
        foreach (int information in new[] { 1, 12, 8, 15, 21, 24, 26, 25, 11, 3 })
        {
            bool queried = LpacFeasibilityMethods.GetTokenInformation(restricted, information, nint.Zero, 0, out int required);
            facts["TokenQuery" + information] = new { Success = queried, Required = required, Error = Marshal.GetLastPInvokeError() };
        }
        _ = identity.GetType().GetMethod("Verify", Members)!.Invoke(identity, [restricted]);
        facts["BaseIdentityVerified"] = true;
        facts["DedicatedAccountQualified"] = false;
        facts["RestrictingSid"] = restriction;
        facts["Stage"] = "InspectExecutableAccess";
        bool Allows(string path) => (bool)Type("NativeFilesystemAdmission").GetMethod("Allows", Members)!
            .Invoke(null, [restricted, path, 0x1200a9U])!;
        facts["RestrictedImageReadExecuteBeforeCorrection"] = Allows(fixture);
        if (!(bool)facts["RestrictedImageReadExecuteBeforeCorrection"]!)
        {
            var controlled = new FileInfo(fixture);
            FileSecurity security = controlled.GetAccessControl();
            if (security.GetOwner(typeof(SecurityIdentifier))?.Value != user ||
                !restriction.StartsWith("S-1-5-21-", StringComparison.Ordinal))
                throw new IOException("Fixture ownership or specific restricting identity is not admitted for ACL correction.");
            // Only this controller-owned fixture; the outer lease restores the original DACL.
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(restriction),
                FileSystemRights.ReadAndExecute, AccessControlType.Allow));
            controlled.SetAccessControl(security);
            facts["NarrowFixtureReadExecuteGrant"] = new { Sid = restriction, AccessMask = "0x1200a9", Object = fixture };
        }
        var fileSecurity = new FileInfo(fixture).GetAccessControl();
        var descriptor = new RawSecurityDescriptor(fileSecurity.GetSecurityDescriptorBinaryForm(), 0);
        var auMasks = new List<int>();
        if (descriptor.DiscretionaryAcl is null) throw new IOException("Fixture DACL is absent.");
        for (int index = descriptor.DiscretionaryAcl.Count - 1; index >= 0; index--)
            if (descriptor.DiscretionaryAcl[index] is CommonAce ace && ace.SecurityIdentifier.Value == "S-1-5-11")
            { auMasks.Add(ace.AccessMask); descriptor.DiscretionaryAcl.RemoveAce(index); }
        byte[] withoutAu = new byte[descriptor.BinaryLength]; descriptor.GetBinaryForm(withoutAu, 0);
        facts["FixtureSecurityBeforeLaunch"] = fileSecurity.GetSecurityDescriptorSddlForm(AccessControlSections.All & ~AccessControlSections.Audit);
        facts["AuthenticatedUsersAceMasks"] = auMasks;
        facts["RestrictedImageReadExecute"] = Allows(fixture);
        facts["RestrictedImageReadExecuteWithoutAuthenticatedUsers"] = Type("NativeFilesystemAdmission")
            .GetMethod("AllowsDescriptor", Members)!.Invoke(null, [restricted, withoutAu, 0x1200a9U]);
        facts["RestrictedWorkingDirectoryReadExecute"] = Allows(Path.GetDirectoryName(fixture)!);
        facts["RestrictedImageWriteData"] = Type("NativeFilesystemAdmission").GetMethod("Allows", Members)!
            .Invoke(null, [restricted, fixture, 2U]);
        facts["RestrictedImageWriteDac"] = Type("NativeFilesystemAdmission").GetMethod("Allows", Members)!
            .Invoke(null, [restricted, fixture, 0x40000U]);
        if (!(bool)facts["RestrictedImageReadExecute"]!) throw new IOException("Restricted controller cannot read/execute the admitted image.");
        if ((bool)facts["RestrictedImageWriteData"]! || (bool)facts["RestrictedImageWriteDac"]!)
        {
            string logonSid = (string)Type("NativeExecutionIdentity").GetMethod("LogonSid", Members)!.Invoke(null, [caller])!;
            byte[] proposed = (byte[])Type("NativeFilesystemAdmission").GetMethod("RestrictFixtureOwner", Members)!
                .Invoke(null, [fileSecurity.GetSecurityDescriptorBinaryForm(), user, restriction, logonSid, new SecurityIdentifier(package).Value])!;
            bool Check(SafeAccessTokenHandle token, uint desired) => (bool)Type("NativeFilesystemAdmission")
                .GetMethod("AllowsDescriptor", Members)!.Invoke(null, [token, proposed, desired])!;
            facts["OwnerRightsProposal"] = new { ControllerLogonSid = logonSid, ControllerCanRestore = Check(caller, 0x40000),
                RestrictedCanReadExecute = Check(restricted, 0x1200a9), RestrictedWriteData = Check(restricted, 2),
                RestrictedWriteDac = Check(restricted, 0x40000), RestrictedWriteOwner = Check(restricted, 0x80000),
                Descriptor = new RawSecurityDescriptor(proposed, 0).GetSddlForm(AccessControlSections.All & ~AccessControlSections.Audit) };
            if (!Check(caller, 0x40000) || !Check(restricted, 0x1200a9) || Check(restricted, 2) ||
                Check(restricted, 0x40000) || Check(restricted, 0x80000))
                throw new IOException("Proposed fixture security cannot preserve controller cleanup and restricted read-only admission.");
            var corrected = new FileInfo(fixture).GetAccessControl();
            corrected.SetSecurityDescriptorBinaryForm(proposed, AccessControlSections.Access);
            new FileInfo(fixture).SetAccessControl(corrected);
            foreach (uint desired in new[] { 2U, 0x40000U, 0x80000U })
                if ((bool)Type("NativeFilesystemAdmission").GetMethod("Allows", Members)!.Invoke(null, [restricted, fixture, desired])!)
                    throw new IOException("Restricted identity retains image mutation/security rights after correction.");
            if (!Allows(fixture)) throw new IOException("Corrected fixture lost read/execute access.");
            facts["OwnerRightsCorrectionVerified"] = true;
        }
        facts["Stage"] = "CreatePrivateDesktopBeforeProfile";
        IDisposable desktopOwner;
        try { desktopOwner = (IDisposable)New("WindowsExecutionDesktop", Guid.NewGuid().ToString("N")[..8], user, restriction,
            new SecurityIdentifier(package).Value); }
        catch (TargetInvocationException failure) when (failure.InnerException is Win32Exception { NativeErrorCode: 183 })
        {
            try
            {
                var context = (Dictionary<string, object?>)Type("WindowsExecutionDesktop").GetMethod("InspectCreation", Members)!
                    .Invoke(null, [new SecurityIdentifier(package).Value, false])!;
                facts["StationCreationContext"] = context;
                if (context.GetValueOrDefault("CurrentStation") is string[] observation)
                    facts["ExistingControllerStation"] = new { Name = observation[0], Security = observation[1], Adopted = false };
            }
            catch (Exception observation)
            {
                // Optional evidence must not replace or bypass the original CREATE_ONLY refusal.
                Exception actual = observation is TargetInvocationException { InnerException: not null } nested ? nested.InnerException! : observation;
                facts["ExistingStationObservationUnavailable"] = new { Type = actual.GetType().Name,
                    Win32Error = (actual as Win32Exception)?.NativeErrorCode, Reason = actual.Message };
            }
            throw;
        }
        using var desktop = desktopOwner;
        facts["QualificationDesktop"] = Get(desktop, "Name");
        if (createProfile)
        {
            facts["Stage"] = "CreateDisposableProfileAfterDesktop";
            int result = LpacFeasibilityMethods.CreateAppContainerProfile((string)facts["ProfileMoniker"]!,
                "PiratePDF controlled LPAC probe", "Suspended-only launch context qualification", nint.Zero, 0, out nint createdSid);
            facts["ProfileCreationHResult"] = "0x" + result.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            if (result != 0) { Marshal.ThrowExceptionForHR(result); throw new IOException("Unexpected profile creation success code."); }
            facts["ProfileCreated"] = true;
            try
            {
                if (new SecurityIdentifier(createdSid).Value != new SecurityIdentifier(package).Value)
                    throw new IOException("Created profile SID differs from admitted package identity.");
            }
            finally { _ = LpacFeasibilityMethods.FreeSid(createdSid); }
        }
        facts["Stage"] = "RetainTrustedImage";
        using var image = (IDisposable)New("TrustedNativeImage", fixture, (string)facts["FixtureSha256"]!, 8388608L);
        facts["Stage"] = "CreateOwnedJob";
        using var job = (IDisposable)New("WindowsJob", 67108864L, 2);
        facts["Stage"] = "CreateCapturePipes";
        using var output = (IDisposable)New("WindowsCapturePipe");
        using var error = (IDisposable)New("WindowsCapturePipe");
        facts["Stage"] = "OpenNullInput";
        using var input = (SafeFileHandle)Type("WindowsNativeProcess").GetMethod("NullInput", Members)!.Invoke(null, null)!;
        object capture = New("BoundedCapture"), errorCapture = New("BoundedCapture");
        var startup = new LpacFeasibilityMethods.StartupEx { Startup = new() { Size = Marshal.SizeOf<LpacFeasibilityMethods.StartupEx>(), Flags = 0x100,
            Input = input.DangerousGetHandle(), Output = ((SafeHandle)Get(output, "Writer")!).DangerousGetHandle(), Error = ((SafeHandle)Get(error, "Writer")!).DangerousGetHandle() } };
        nint attributes = nint.Zero;
        var allocations = new List<nint>();
        SafeProcessHandle? process = null;
        SafeWaitHandle? thread = null;
        Task drains = Task.CompletedTask;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            facts["Stage"] = "ConfigureCreationAttributes";
            nuint size = 0;
            int attributeCount = jobOnly ? 2 : 4;
            _ = LpacFeasibilityMethods.InitializeProcThreadAttributeList(nint.Zero, attributeCount, 0, ref size);
            if (size is 0 or > 65536) throw new InvalidOperationException("Invalid attribute work budget.");
            attributes = Marshal.AllocHGlobal((nint)size);
            if (!LpacFeasibilityMethods.InitializeProcThreadAttributeList(attributes, attributeCount, 0, ref size)) throw Error();
            startup.Attributes = attributes;
            void Attribute(nuint key, nint data, nuint bytes)
            { if (!LpacFeasibilityMethods.UpdateProcThreadAttribute(attributes, 0, key, data, bytes, nint.Zero, nint.Zero)) throw Error(); }
            nint Allocate(int bytes) { nint pointer = Marshal.AllocHGlobal(bytes); allocations.Add(pointer); return pointer; }
            startup.Startup.Desktop = Marshal.StringToHGlobalUni((string)Get(desktop, "Name")!);
            allocations.Add(startup.Startup.Desktop);
            nint jobs = Allocate(nint.Size);
            Marshal.WriteIntPtr(jobs, ((SafeHandle)Get(job, "Handle")!).DangerousGetHandle());
            Attribute(0x2000d, jobs, (nuint)nint.Size);
            nint handles = Allocate(nint.Size * 3);
            Marshal.WriteIntPtr(handles, startup.Startup.Input); Marshal.WriteIntPtr(handles, nint.Size, startup.Startup.Output); Marshal.WriteIntPtr(handles, nint.Size * 2, startup.Startup.Error);
            Attribute(0x20002, handles, (nuint)(nint.Size * 3));
            if (!jobOnly)
            {
                nint capabilities = Allocate(Marshal.SizeOf<LpacFeasibilityMethods.Capabilities>());
                Marshal.StructureToPtr(new LpacFeasibilityMethods.Capabilities { Package = package }, capabilities, false);
                Attribute(0x20009, capabilities, (nuint)Marshal.SizeOf<LpacFeasibilityMethods.Capabilities>());
                nint policy = Allocate(4); Marshal.WriteInt32(policy, 1); Attribute(0x2000f, policy, 4);
            }
            string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string commandLine = "\"" + fixture + "\"";
            string workingDirectory = Path.GetDirectoryName(fixture)!;
            facts["LaunchConfiguration"] = new { Executable = fixture, WorkingDirectory = workingDirectory, CommandLine = commandLine,
                SystemRoot = systemRoot, Desktop = Get(desktop, "Name"), CreationFlags = "0x08080404", AttributeCount = attributeCount,
                JobList = true, ExplicitInheritedHandles = new[] { "NUL stdin", "private stdout writer", "private stderr writer" },
                AppContainerCapabilities = !jobOnly, AllApplicationPackagesOptOut = !jobOnly, CapabilityCount = 0 };
            nint environment = Marshal.StringToHGlobalUni("SystemRoot=" + systemRoot + "\0\0");
            allocations.Add(environment);
            facts["Stage"] = "CreateSuspendedProcess";
            bool launched = LpacFeasibilityMethods.CreateProcessAsUserW(restricted, fixture, (commandLine + "\0").ToCharArray(), nint.Zero, nint.Zero,
                true, 0x80000 | 4 | 0x400 | 0x08000000, environment, workingDirectory, ref startup, out var created);
            int launchError = Marshal.GetLastPInvokeError();
            facts["CreateProcessWin32Error"] = launched ? 0 : launchError;
            if (!launched) throw new Win32Exception(launchError, "CreateProcessAsUserW failed during suspended admission.");
            process = new(created.Process, true); thread = new(created.Thread, true);
            facts["ProcessCreated"] = true;
            facts["Stage"] = "VerifySuspendedProcess";
            if (!LpacFeasibilityMethods.IsProcessInJob(process.DangerousGetHandle(), ((SafeHandle)Get(job, "Handle")!).DangerousGetHandle(), out bool member) || !member)
                throw new InvalidOperationException("Creation-time Job membership failed.");
            facts["JobMembership"] = true;
            if (jobOnly)
            {
                // Diagnosis only: never replace the original pre-resume check with this observation.
                facts["CreatedProcessId"] = created.Id;
                facts["CreatedThreadId"] = created.ThreadId;
                uint observedThread = LpacFeasibilityMethods.GetThreadId(thread);
                int threadError = Marshal.GetLastPInvokeError();
                uint observedProcess = LpacFeasibilityMethods.GetProcessIdOfThread(thread);
                int processError = Marshal.GetLastPInvokeError();
                facts["ThreadHandleIdentity"] = new { Thread = observedThread, Process = observedProcess, threadError, processError };
                if (observedThread != created.ThreadId || observedProcess != created.Id) throw new IOException("Diagnostic thread identity mismatch.");
                nint originalStation = LpacFeasibilityMethods.GetProcessWindowStation();
                var targetStation = (SafeHandle)desktop.GetType().GetField("station", Members)!.GetValue(desktop)!;
                if (originalStation == nint.Zero) throw Error();
                bool switched = LpacFeasibilityMethods.SetProcessWindowStation(targetStation.DangerousGetHandle());
                int switchError = Marshal.GetLastPInvokeError();
                facts["DiagnosticObserverStationSwitch"] = new { Success = switched, Error = switched ? 0 : switchError };
                if (switched)
                {
                    bool restored;
                    int restoreError;
                    try
                    {
                        nint sameStationDesktop = LpacFeasibilityMethods.GetThreadDesktop(created.ThreadId);
                        int observationError = Marshal.GetLastPInvokeError();
                        facts["SameStationSuspendedObservation"] = new { HandleAvailable = sameStationDesktop != nint.Zero, Error = observationError };
                    }
                    finally
                    {
                        restored = LpacFeasibilityMethods.SetProcessWindowStation(originalStation);
                        restoreError = Marshal.GetLastPInvokeError();
                        facts["DiagnosticObserverStationRestored"] = restored;
                    }
                    if (!restored) throw new Win32Exception(restoreError, "Diagnostic observer station restoration failed.");
                }
            }
            // A suspended-only probe measures admission without granting initialization permission.
            // It cannot reach ResumeThread; the normal launch retains its association requirement.
            if (!admissionOnly)
            {
            nint childDesktop = LpacFeasibilityMethods.GetThreadDesktop(created.ThreadId);
            int desktopError = Marshal.GetLastPInvokeError();
            facts["ChildDesktopHandleAvailableBeforeResume"] = childDesktop != nint.Zero;
            facts["ChildDesktopObservationError"] = desktopError;
            if (childDesktop == nint.Zero) throw new InvalidOperationException("Suspended child desktop association unavailable; execution refused.");
            byte[] desktopName = new byte[1024];
            if (!LpacFeasibilityMethods.GetUserObjectInformationW(childDesktop, 2, desktopName, (uint)desktopName.Length, out uint nameBytes)) throw Error();
            if (nameBytes is < 2 or > 1024 || nameBytes % 2 != 0) throw new IOException("Invalid suspended desktop name.");
            string observedDesktop = System.Text.Encoding.Unicode.GetString(desktopName, 0, (int)nameBytes).TrimEnd('\0');
            facts["ChildDesktopBeforeResume"] = observedDesktop;
            if (!((string)Get(desktop, "Name")!).EndsWith("\\" + observedDesktop, StringComparison.Ordinal))
                throw new IOException("Suspended child desktop association mismatch.");
            }
            char[] actualImage = new char[4096]; uint actualLength = 4096;
            if (!LpacFeasibilityMethods.QueryFullProcessImageNameW(process, 0, actualImage, ref actualLength) ||
                !new string(actualImage, 0, (int)actualLength).Equals(fixture, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Suspended image path mismatch.");
            using (var actualFile = (SafeFileHandle)Type("WindowsNativeMethods").GetMethod("OpenMetadata", Members)!.Invoke(null, [fixture])!)
                if (!Equals(Get(image, "Identity"), Type("WindowsNativeMethods").GetMethod("FileIdentity", Members)!.Invoke(null, [actualFile])))
                    throw new InvalidOperationException("Suspended image identity mismatch.");
            facts["ImageVerifiedBeforeResume"] = true;
            if (!LpacFeasibilityMethods.OpenProcessToken(process, 8, out SafeAccessTokenHandle token)) throw Error();
            using (token)
            {
                _ = identity.GetType().GetMethod("Verify", Members)!.Invoke(identity, [token]);
                if (!jobOnly)
                {
                nint packageInformation = Allocate(64);
                if (!LpacFeasibilityMethods.GetTokenInformation(token, 31, packageInformation, 64, out _)) throw Error();
                string actualPackage = new SecurityIdentifier(Marshal.ReadIntPtr(packageInformation)).Value;
                facts["ActualPackageSid"] = actualPackage;
                if (actualPackage != new SecurityIdentifier(package).Value) throw new InvalidOperationException("Suspended package identity mismatch.");
                int Read(int information)
                {
                    nint value = Allocate(64);
                    bool success = LpacFeasibilityMethods.GetTokenInformation(token, information, value, 64, out int returned);
                    int nativeError = Marshal.GetLastPInvokeError();
                    facts["ChildTokenQuery" + information] = new { Success = success, Returned = returned, Error = success ? 0 : nativeError };
                    if (!success) throw new Win32Exception(nativeError, "Suspended child token query " + information + " failed.");
                    // Capabilities is TOKEN_GROUPS; the flags are DWORDs. Never read a truncated response.
                    if (information == 30 ? returned < nint.Size || returned > 64 : returned != 4)
                        throw new IOException("Unexpected suspended token-information length.");
                    return Marshal.ReadInt32(value);
                }
                facts["TokenIsAppContainer"] = Read(29);
                facts["TokenIsLpac"] = Read(46);
                facts["CapabilityCount"] = Read(30);
                if (Read(29) != 1 || Read(46) != 1 || Read(30) != 0) throw new InvalidOperationException("LPAC identity or zero-capability readback failed.");
                }
            }
            if (admissionOnly)
            {
                facts["SuspendedAdmissionVerified"] = true;
                throw new InvalidOperationException("Suspended-only admission probe complete; initialization remains blocked pending dependency, object-access, filesystem and network/IPC qualification.");
            }
            Task Drain(object target, object pipe) => (Task)target.GetType().GetMethod("DrainAsync", Members)!.Invoke(target, [Get(pipe, "Reader"), deadline.Token])!;
            drains = Task.WhenAll(Drain(capture, output), Drain(errorCapture, error));
            if (LpacFeasibilityMethods.ResumeThread(thread) != 1) throw Error();
            facts["ChildResumed"] = true;
            _ = Call(output, "ParentWriteClosed"); _ = Call(error, "ParentWriteClosed");
            while (LpacFeasibilityMethods.WaitForSingleObject(process, 0) == 258) await Task.Delay(10, deadline.Token).ConfigureAwait(false);
            await drains.WaitAsync(deadline.Token).ConfigureAwait(false);
            if (!LpacFeasibilityMethods.GetExitCodeProcess(process, out uint exit)) throw Error();
            facts["ExitCode"] = exit;
            facts["Output"] = System.Text.Encoding.UTF8.GetString(((ReadOnlyMemory<byte>)Get(capture, "Bytes")!).Span);
            facts["JobEmpty"] = Call(job, "IsEmpty");
            facts["Status"] = exit == 0 && (bool)Call(job, "IsEmpty")! &&
                ((string)facts["Output"]!).Trim() == "lpac-controlled-fixture-started" ? "PASS" : "UNKNOWN";
        }
        finally
        {
            try
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                if (!(bool)Call(job, "IsEmpty")!) _ = Call(job, "Terminate");
                while (!(bool)Call(job, "IsEmpty")! || process is not null && LpacFeasibilityMethods.WaitForSingleObject(process, 0) == 258)
                    await Task.Delay(10, cleanup.Token).ConfigureAwait(false);
                // Close controller writer copies even if creation or resume failed.
                _ = Call(output, "ParentWriteClosed"); _ = Call(error, "ParentWriteClosed");
                await drains.WaitAsync(cleanup.Token).ConfigureAwait(false);
                facts["CleanupVerified"] = true;
            }
            finally
            {
                await deadline.CancelAsync().ConfigureAwait(false);
                _ = drains.ContinueWith(task => _ = task.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
                thread?.Dispose(); process?.Dispose();
                if (startup.Attributes != nint.Zero) LpacFeasibilityMethods.DeleteProcThreadAttributeList(attributes);
                if (attributes != nint.Zero) Marshal.FreeHGlobal(attributes);
                foreach (nint pointer in allocations) Marshal.FreeHGlobal(pointer);
            }
        }
    }

    private static Win32Exception Error() => new(Marshal.GetLastPInvokeError());
}
