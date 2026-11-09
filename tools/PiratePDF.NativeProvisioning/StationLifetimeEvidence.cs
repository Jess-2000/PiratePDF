// The administrative parent performs one exact-name observation, never an object-destruction claim.
// C# 5 / Framework compatible, also linked into the runner for deterministic policy tests.
using System;
using System.Security;

namespace PiratePDF.Fuzz.Runner
{
    public sealed class StationLifetimeResult
    {
        public StationLifetimeResult()
        {
            StationName = ""; State = "NotObserved";
            EvidenceScope = "Source-bound owner exits and one READ_CONTROL named-station probe; desktop destruction unproved";
            FinalObjectLifetime = "UNKNOWN/BLOCKED";
        }
        public string StationName { get; internal set; }
        public string State { get; internal set; }
        public uint ControllerWait { get; internal set; }
        public int ControllerWaitError { get; internal set; }
        public uint ControllerExit { get; internal set; }
        public int ControllerExitError { get; internal set; }
        public bool KnownTransactionOwnersClosed { get; internal set; }
        public int ProbeAttempts { get; internal set; }
        public int OpenError { get; internal set; }
        public bool ProbeClosed { get; internal set; }
        public int ProbeCloseError { get; internal set; }
        public int ObserverPid { get; internal set; }
        public int ObserverSession { get; internal set; }
        public string EvidenceScope { get; private set; }
        public bool DestructionProved { get; private set; }
        public string FinalObjectLifetime { get; private set; }
    }

    internal static class StationLifetimeEvidence
    {
        internal static StationLifetimeResult Observe(string runId, bool helperConfirmed, Func<uint, long> waitController,
            Func<long> controllerExit, Func<long> openStation, Func<long> closeProbe)
        {
            if (runId == null || !System.Text.RegularExpressions.Regex.IsMatch(runId, "\\A[a-f0-9]{8}\\z"))
                throw new SecurityException("Invalid fixed station observation identity.");
            var result = new StationLifetimeResult { StationName = "pph2b_" + runId };
            using (var observer = System.Diagnostics.Process.GetCurrentProcess()) result.ObserverPid = observer.Id;
            if (!helperConfirmed) { result.State = "HelperUnverified"; return result; }
            long wait = waitController(5000);
            result.ControllerWait = unchecked((uint)wait); result.ControllerWaitError = (int)(wait >> 32);
            if (result.ControllerWait != 0 || result.ControllerWaitError != 0)
            {
                result.State = result.ControllerWait == 258 ? "ControllerTimeout" : "ControllerWaitFailure";
                return result;
            }
            long exited = controllerExit(); result.ControllerExit = unchecked((uint)exited); result.ControllerExitError = (int)(exited >> 32);
            if (result.ControllerExit != 0 || result.ControllerExitError != 0) { result.State = "ControllerFailure"; return result; }
            // Zero exits are receipts from the exact admitted helper/receiver code,
            // not a claim that Windows has destroyed every possible object reference.
            result.KnownTransactionOwnersClosed = true;
            result.ProbeAttempts = 1;
            long opened = openStation(); result.OpenError = (int)(opened >> 32);
            if (unchecked((uint)opened) == 1 && result.OpenError == 0)
            {
                long closed = closeProbe(); result.ProbeCloseError = (int)(closed >> 32);
                result.ProbeClosed = unchecked((uint)closed) == 1 && result.ProbeCloseError == 0;
                result.State = result.ProbeClosed ? "NamedObjectPresent" : "ProbeCloseFailure";
            }
            else result.State = result.OpenError == 2 ? "NamespaceAbsentAtProbe" :
                result.OpenError == 5 ? "AccessDeniedNotAbsence" : result.OpenError == 0 ? "AmbiguousOpenFailure" : "OpenFailure";
            return result; // Even error 2 establishes only exact-name absence at this instant.
        }
    }
}
