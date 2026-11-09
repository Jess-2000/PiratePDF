// Shared with the precompiled Windows PowerShell bootstrap; C# 5 / Framework API only.
using System;
using System.Security;

namespace PiratePDF.Fuzz.Runner
{
    public sealed class StationHelperCleanupResult
    {
        public StationHelperCleanupResult() { OriginalFailure = "None"; }
        public string OriginalFailure { get; internal set; }
        public bool HelperCreated { get; internal set; }
        public bool OwnershipEstablished { get; internal set; }
        public uint InitialWait { get; internal set; }
        public int InitialWaitError { get; internal set; }
        public bool TerminationAttempted { get; internal set; }
        public int TerminationError { get; internal set; }
        public uint FinalWait { get; internal set; }
        public int FinalWaitError { get; internal set; }
        public bool ExitVerified { get; internal set; }
        public uint ExitCode { get; internal set; }
        public int ExitError { get; internal set; }
        public bool CodeLeasesReleased { get; internal set; }
        public bool OwnershipRetained { get; internal set; }
        public int LeaseCloseFailures { get; internal set; }
    }

    // Only the fixed bootstrap supplies native callbacks over its own retained handle.
    // Packing preserves both the exact native result (low DWORD) and last error (high DWORD).
    internal static class StationHelperCleanup
    {
        internal static StationHelperCleanupResult Complete(bool abort, string failure, Func<uint, long> wait,
            Func<long> terminate, Func<long> exit)
        {
            var result = new StationHelperCleanupResult { OriginalFailure = failure, HelperCreated = true };
            long observed = wait(abort ? 0u : 10000u);
            result.InitialWait = unchecked((uint)observed); result.InitialWaitError = (int)(observed >> 32);
            if (!abort && result.InitialWait == 258 && failure == "None") result.OriginalFailure = "Timeout";
            if (result.InitialWait != 0)
            {
                result.TerminationAttempted = true;
                long terminated = terminate();
                result.TerminationError = (int)(terminated >> 32);
                if (unchecked((uint)terminated) == 0 && result.TerminationError == 0) result.TerminationError = -1;
                observed = wait(5000);
            }
            result.FinalWait = unchecked((uint)observed); result.FinalWaitError = (int)(observed >> 32);
            if (result.FinalWait == 0 && result.FinalWaitError == 0)
            {
                long code = exit(); result.ExitError = (int)(code >> 32);
                result.ExitCode = unchecked((uint)code); result.ExitVerified = result.ExitError == 0;
            }
            return result;
        }

        internal static StationHelperCleanupResult NoLaunch(string failure)
        { return new StationHelperCleanupResult { OriginalFailure = failure }; }

        internal static void ValidateResidentArguments(string[] arguments)
        {
            bool noProfile = false;
            for (int i = 1; i < arguments.Length; i++)
            {
                string value = arguments[i];
                if (string.Equals(value, "-NoProfile", StringComparison.OrdinalIgnoreCase)) noProfile = true;
                else if (!string.Equals(value, "-NoLogo", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value, "-NoExit", StringComparison.OrdinalIgnoreCase))
                    throw new SecurityException("Require an independently trusted interactive PowerShell -NoProfile parent; no disposable -File/-Command host.");
            }
            if (!noProfile) throw new SecurityException("A resident trusted -NoProfile parent is required.");
        }
    }
}
