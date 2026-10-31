using System.ComponentModel;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

[SupportedOSPlatform("windows")]
internal static class NativeStationTransaction
{
    private static async Task<(StationAuthorization Authorization, FileStream Lease)> ReadAuthorizationAsync(string id, CancellationToken token)
    {
        string path = NativeStationPolicy.AuthorizationPath(id);
        NativeStationTrust.ProtectAncestors(NativeStationPolicy.StoreRoot);
        while (true)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                NativeStationTrust.ProtectAncestors(path);
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    if (stream.Length is < 2 or > 4096) throw new SecurityException("Authorization record budget exceeded.");
                    byte[] bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes);
                    var authorization = NativeStationPolicy.Decode<StationAuthorization>(bytes);
                    NativeStationPolicy.ValidateAuthorization(authorization, id, DateTimeOffset.UtcNow);
                    return (authorization, stream);
                }
                catch { stream.Dispose(); throw; }
            }
            catch (FileNotFoundException) { await Task.Delay(20, token).ConfigureAwait(false); }
            catch (IOException failure) when ((failure.HResult & 0xffff) is 32 or 33)
            { await Task.Delay(20, token).ConfigureAwait(false); }
        }
    }
    private static void Claim(StationAuthorization authorization)
    {
        string marker = Path.Combine(NativeStationPolicy.StoreRoot, authorization.Id + ".claimed");
        using var claim = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        byte[] bytes = Encoding.ASCII.GetBytes(authorization.Id + "\n" + authorization.CodeDigest + "\n" + authorization.Helper.Pid + "\n" + authorization.Helper.CreationTime);
        claim.Write(bytes); claim.Flush(true);
        NativeStationTrust.ProtectAncestors(marker);
        // Never remove this marker, including on failure or a crash. A new operation
        // needs independent operator approval and a new authorization identity.
    }
    internal static async Task<int> RunHelperAsync(string id)
    {
        var state = new StationTransferState(); WindowsExecutionDesktop? owned = null;
        string stage = "TrustedBootstrapFailure";
        try
        {
            StationBootstrapInterop.NoImpersonation();
            using var self = StationBootstrapInterop.OpenPeer(Environment.ProcessId, false);
            NativeStationPolicy.ValidatePeer(NativeStationTrust.Peer(Environment.ProcessId, self), true);
            var expectedEnvironment = NativeStationPolicy.EnvironmentFor(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            var actualEnvironment = Environment.GetEnvironmentVariables();
            if (actualEnvironment.Count != expectedEnvironment.Count || expectedEnvironment.Any(pair => (string?)actualEnvironment[pair.Key] != pair.Value))
                throw new SecurityException("Bootstrap environment contract mismatch; managed checks do not replace pre-CLR trust.");
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var record = await ReadAuthorizationAsync(id, deadline.Token).ConfigureAwait(false);
            using var authorizationLease = record.Lease;
            StationAuthorization authorization = record.Authorization;
            NativeStationPolicy.MatchPeer(authorization.Helper, NativeStationTrust.Peer(Environment.ProcessId, self));
            using var trust = new NativeStationTrust(authorization.CodeDigest);
            trust.VerifyImage(self, true);
            stage = "ControllerIdentityMismatch";
            using var controller = StationBootstrapInterop.OpenPeer(authorization.Controller.Pid, false);
            NativeStationPolicy.MatchPeer(authorization.Controller, NativeStationTrust.Peer(authorization.Controller.Pid, controller));
            trust.VerifyImage(controller, false);
            stage = "AuthorizationReplayOrClaimFailure";
            Claim(authorization);
            stage = "EndpointIdentityMismatch";
            using var pipe = await NativeStationTransport.ServerAsync(authorization, deadline.Token).ConfigureAwait(false);
            if (pipe.PeerPid(true) != authorization.Controller.Pid) throw new SecurityException("Unauthorized independent pipe peer.");
            NativeStationPolicy.MatchPeer(authorization.Controller, NativeStationTrust.Peer(authorization.Controller.Pid, controller));
            await pipe.ReceiveAsync(authorization, "Request", deadline.Token).ConfigureAwait(false);
            NativeStationPolicy.ValidateAuthorization(authorization, id, DateTimeOffset.UtcNow);
            long station = 0, desktop = 0; string offerState = "Pair"; int offerError = 0;
            try
            {
                stage = "StationCreationFailure"; deadline.Token.ThrowIfCancellationRequested();
                owned = WindowsExecutionDesktop.CreateStationLease(authorization.RunId);
                stage = "DescriptorOrLabelMismatch"; owned.VerifyNoReopen(controller);
                // Open duplication rights only after the exact endpoint/controller
                // and independently authorized fixed resource policy are admitted.
                stage = "FirstDuplicationFailure";
                using var target = StationBootstrapInterop.OpenPeer(authorization.Controller.Pid, true);
                NativeStationPolicy.MatchPeer(authorization.Controller, NativeStationTrust.Peer(authorization.Controller.Pid, target));
                NativeStationPolicy.ValidateAuthorization(authorization, id, DateTimeOffset.UtcNow);
                deadline.Token.ThrowIfCancellationRequested();
                station = owned.DuplicateStation(target); state.Duplicated();
                stage = "SecondDuplicationFailure";
                NativeStationPolicy.ValidateAuthorization(authorization, id, DateTimeOffset.UtcNow);
                deadline.Token.ThrowIfCancellationRequested();
                desktop = owned.DuplicateDesktop(target); state.Duplicated();
            }
            catch (StationLeaseFailure failure) { offerState = failure.State; offerError = failure.ErrorCode; }
            catch (Win32Exception failure) when (stage is "FirstDuplicationFailure" or "SecondDuplicationFailure")
            { offerState = stage; offerError = failure.NativeErrorCode; }
            catch (SecurityException) when (stage == "DescriptorOrLabelMismatch") { offerState = stage; }
            await pipe.SendAsync(new(1, NativeStationPolicy.Operation, id, "Offer", station, desktop, offerState, offerError), deadline.Token).ConfigureAwait(false);
            stage = "MissingLateOrRefusedAcknowledgment";
            StationMessage ack = await pipe.ReceiveAsync(authorization, "Ack", deadline.Token).ConfigureAwait(false);
            NativeStationPolicy.ValidateAuthorization(authorization, id, DateTimeOffset.UtcNow);
            if (ack.State != "Verified" || offerState != "Pair") { stage = offerState == "Pair" ? stage : offerState; throw new SecurityException("Receiver did not verify the complete station lease."); }
            if (pipe.PeerPid(true) != authorization.Controller.Pid) throw new SecurityException("Endpoint changed before acknowledgment.");
            NativeStationPolicy.MatchPeer(authorization.Controller, NativeStationTrust.Peer(authorization.Controller.Pid, controller));
            state.Acknowledge(); stage = "HelperCleanupFailure";
            if (owned is null || !owned.CloseVerified()) throw new SecurityException("UNKNOWN/BLOCKED: helper USER-object close failed.");
            return 0;
        }
        catch (Exception failure) when (failure is SecurityException or IOException or Win32Exception or OperationCanceledException or JsonException or UnauthorizedAccessException)
        {
            bool helperClosed = owned is null || owned.CloseVerified();
            if (!helperClosed || failure is StationLeaseFailure { State: "UNKNOWN/BLOCKED:CleanupFailure" }) stage = "UNKNOWN/BLOCKED:CleanupFailure";
            state.Fail(stage);
            Console.Error.WriteLine(JsonSerializer.Serialize(new { Operation = NativeStationPolicy.Operation, state.State,
                Error = failure.GetType().Name, Win32Error = failure is Win32Exception native ? native.NativeErrorCode : failure is StationLeaseFailure lease ? lease.ErrorCode : 0,
                FinalLifetime = "UNKNOWN", ProductionNativeExecution = "Disabled" }));
            int error = failure is Win32Exception nativeError ? nativeError.NativeErrorCode : failure is StationLeaseFailure resourceError ? resourceError.ErrorCode : 0;
            return StationBootstrapInterop.FailureExit(stage, error, !helperClosed || state.Transferred > 0 || stage.StartsWith("UNKNOWN/BLOCKED", StringComparison.Ordinal));
        }
        finally
        {
            if (owned is not null && !owned.CloseVerified())
                Console.Error.WriteLine("UNKNOWN/BLOCKED: owned helper handles did not close; no retry or worker admission.");
        }
    }

#if !STATION_ONLY
    internal static async Task<int> RunReceiverAsync(string id, CancellationToken cancellation)
    {
        WindowsExecutionDesktop? owned = null; bool closed = false; string stage = "AuthorizationFailure";
        var state = new StationTransferState();
        Microsoft.Win32.SafeHandles.SafeProcessHandle? helper = null;
        try
        {
            StationBootstrapInterop.NoImpersonation();
            using var self = StationBootstrapInterop.OpenPeer(Environment.ProcessId, false);
            NativeStationPolicy.ValidatePeer(NativeStationTrust.Peer(Environment.ProcessId, self), false);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var record = await ReadAuthorizationAsync(id, deadline.Token).ConfigureAwait(false);
            using var authorizationLease = record.Lease;
            StationAuthorization authorization = record.Authorization;
            NativeStationPolicy.MatchPeer(authorization.Controller, NativeStationTrust.Peer(Environment.ProcessId, self));
            using var trust = new NativeStationTrust(authorization.CodeDigest); trust.VerifyImage(self, false);
            stage = "HelperIdentityMismatch";
            helper = StationBootstrapInterop.OpenPeer(authorization.Helper.Pid, false);
            NativeStationPolicy.MatchPeer(authorization.Helper, NativeStationTrust.Peer(authorization.Helper.Pid, helper)); trust.VerifyImage(helper, true);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            using var pipe = await NativeStationTransport.ClientAsync(authorization, deadline.Token).ConfigureAwait(false);
            if (pipe.PeerPid(false) != authorization.Helper.Pid) throw new SecurityException("Unauthorized station server.");
            NativeStationPolicy.MatchPeer(authorization.Helper, NativeStationTrust.Peer(authorization.Helper.Pid, helper));
            await pipe.SendAsync(new(1, NativeStationPolicy.Operation, id, "Request"), deadline.Token).ConfigureAwait(false);
            stage = "UNKNOWN/BLOCKED:PartialTransferOrReceiverRefusal";
            StationMessage offer = await pipe.ReceiveAsync(authorization, "Offer", deadline.Token).ConfigureAwait(false);
            if (offer.Station != 0) state.Duplicated();
            if (offer.Desktop != 0) state.Duplicated();
            NativeStationPolicy.ValidateAuthorization(authorization, id, DateTimeOffset.UtcNow);
            if (offer.Station == 0 && offer.Desktop == 0 && offer.State != "Pair")
            {
                stage = offer.State + ":Win32=" + offer.ErrorCode;
                await pipe.SendAsync(new(1, NativeStationPolicy.Operation, id, "Ack", State: "RefusedClosed"), deadline.Token).ConfigureAwait(false);
                throw new SecurityException("Helper refused before transferring authority.");
            }
            owned = WindowsExecutionDesktop.AdoptStationLease(offer, authorization);
            if (offer.State != "Pair")
            {
                closed = owned.CloseVerified();
                await pipe.SendAsync(new(1, NativeStationPolicy.Operation, id, "Ack", State: closed ? "RefusedClosed" : "UNKNOWN/BLOCKED"), deadline.Token).ConfigureAwait(false);
                throw new SecurityException("Incomplete handle pair; no retry or worker admission.");
            }
            owned.VerifyStationLease();
            owned.VerifyNoReopen(self);
            NativeStationPolicy.ValidateAuthorization(authorization, id, DateTimeOffset.UtcNow);
            state.Acknowledge();
            await pipe.SendAsync(new(1, NativeStationPolicy.Operation, id, "Ack", State: "Verified"), deadline.Token).ConfigureAwait(false);
            stage = "UNKNOWN/BLOCKED:HelperTerminationOrCompletionTimeout";
            while (!StationBootstrapInterop.Exited(helper)) await Task.Delay(20, deadline.Token).ConfigureAwait(false);
            uint helperExit = StationBootstrapInterop.ExitCode(helper);
            if (helperExit != 0) { stage = "UNKNOWN/BLOCKED:HelperExit=" + helperExit; throw new SecurityException("Helper did not confirm its own cleanup."); }
            owned.VerifyStationLease(); closed = owned.CloseVerified();
            state.Closed(closed, true);
            if (!closed) throw new SecurityException("Receiver cleanup failed.");
            Console.WriteLine(JsonSerializer.Serialize(new { Operation = NativeStationPolicy.Operation, Authorization = id,
                State = "LocalHandlesClosed", HelperExited = true, CleanupVerified = true,
                ControllerOwnedHandlesClosed = closed, HelperCloseReceipt = "AdmittedHelperZeroExit",
                NamespaceObserved = false, DestructionProved = false,
                FinalObjectLifetime = "UNKNOWN: live Windows qualification still required", ProductionNativeExecution = "Disabled" }));
            return 0;
        }
        catch (Exception failure) when (failure is SecurityException or IOException or Win32Exception or OperationCanceledException or JsonException or UnauthorizedAccessException)
        {
            if (owned is not null) closed = owned.CloseVerified();
            if (helper is not null && StationBootstrapInterop.Exited(helper) && StationBootstrapInterop.ExitCode(helper) is uint code && code != 0)
            {
                string[] outcome = StationBootstrapInterop.DecodeFailure(code);
                stage = outcome[2] + ":" + outcome[0] + ":Win32=" + outcome[1];
            }
            Console.WriteLine(JsonSerializer.Serialize(new { Operation = NativeStationPolicy.Operation, State = stage,
                Error = failure.GetType().Name, LocallyOwnedHandlesClosed = closed, FinalObjectLifetime = "UNKNOWN/BLOCKED", ProductionNativeExecution = "Disabled" }));
            return 2;
        }
        finally { owned?.Dispose(); helper?.Dispose(); }
    }
#endif
}
