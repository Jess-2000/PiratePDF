# Post-BF-1054 Batch B9 - Process Supervision Completion Report

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-032 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1054 |
| **Last Updated** | 2026-09-24 |
| **Next Review** | 2026-12-24 |

## Disposition and starting state

**The neutral single-case supervision slice is complete within the limitations below. Overall B9 is not complete; POST-P2-002 remains open.** This report supplements [PP-AUD-029](Post-BF-1054-Batch-B9-Bounded-Fuzz-and-Corpus-Completion-Report.md), [PP-AUD-030](Post-BF-1054-Batch-B9-Neutral-Targets-Completion-Report.md) and [PP-AUD-031](Post-BF-1054-Batch-B9-Neutral-Runner-Completion-Report.md). Their earlier checkpoint statements remain historical; those reports and the current audit were not edited.

Starting and ending branch: `maintenance/post-BF-1054-repository-review`. Starting and ending HEAD: **93865f639acf96f0b9598c97b04d49cea38e8260**. Root AGENTS.md, PROJECT-CHARTER.md, all three reports, target isolation/registry contracts, current run/result protocol, ChildProcess and the existing test project were inspected before implementation.

The starting tree contained **35 changed/untracked files: six modified tracked files and 29 untracked files**. The exact starting short status was:

```text
 M .gitignore
 M PiratePDF.slnx
 M docs/Documentation-Health.md
 M docs/Documentation-Index.md
 M docs/ValidationReport.md
 M docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md
?? docs/reviews/Post-BF-1054-Batch-B9-Bounded-Fuzz-and-Corpus-Completion-Report.md
?? docs/reviews/Post-BF-1054-Batch-B9-Neutral-Runner-Completion-Report.md
?? docs/reviews/Post-BF-1054-Batch-B9-Neutral-Targets-Completion-Report.md
?? dotnet-tools.json
?? tests/PiratePDF.Fuzz.Tests/
?? tools/PiratePDF.Fuzz.Runner/
?? tools/PiratePDF.Fuzz.SharpFuzz/
?? tools/PiratePDF.Fuzz.Targets/
```

All existing work was preserved. No reset, clean, revert, recreation, commit, push, merge, tag or branch switch occurred. SHA-256 comparison confirmed that all **34 pre-existing changed/untracked files outside the one modified test project file** remained identical; none disappeared.

## Architecture and invocation

`WorkerSupervisor` is a Runner-library API for one target/input/configuration request. It takes a trusted absolute installed dotnet-host path and trusted absolute worker assembly path. These are operator/tool configuration, never document input. It validates the request against the real TargetRegistry before launch, then starts the existing runner with separate arguments:

```text
<dotnet-host> <neutral-runner.dll> run --target <registered-id> --input <path> --config v1
```

For example, an authorized caller constructs `new WorkerSupervisor(dotnetHostPath, typeof(RunCommand).Assembly.Location)` and awaits `RunAsync("filter-asciihex", inputPath, 1, cancellationToken)`. This slice adds no new CLI dispatch, replay or mutation command.

ProcessStartInfo uses **UseShellExecute=false**, **ArgumentList**, **CreateNoWindow=true**, and separate redirected stdout/stderr pipes. No shell is launched and no input bytes are placed in arguments. The constructor rejects shell executable names and relative launch paths. Absolute paths are not a signature/integrity check: the caller must select trusted installed host and repository artifact paths.

Every invocation launches a fresh child; there is no in-process shortcut or worker pool. The existing isolation enum remains registry metadata, and all three existing targets are covered by this fresh-process policy. Target implementations and registration are not duplicated or changed.

The old ChildProcess/ChildResult scaffold was reviewed, not assumed complete. Its smoke-tool path conflates caller cancellation with timeout, retains characters rather than an explicit byte count, exposes raw output and handles a native-driver-specific lease. That path and the existing smoke command remain untouched and unexecuted. The neutral supervisor has its own bounded capture/protocol lifecycle; this slice makes no claim that the legacy smoke path now satisfies these contracts.

## Deadlines, cancellation and cleanup

The default case deadline is **10 seconds**, starting before launch and covering worker exit plus EOF on both output streams. Callers may shorten it for controlled tests but cannot disable or extend it. Timeout does not depend on target cooperation or its cancellation token. The managed process-start/kill OS calls themselves are synchronous; this is not a real-time scheduling guarantee against an unresponsive OS.

On deadline or explicit cancellation, a still-running worker receives `Kill(entireProcessTree: true)`. Cleanup then waits for root termination and pipe completion with an independent, shared **5-second cleanup budget**. It attempts the root wait even if tree termination reports an error. A failed kill/wait or failure to finish draining becomes **TerminationFailed**, overriding a timeout/cancellation classification. Output streams are cancelled/closed and task faults observed after the finite cleanup wait. Cleanup never relies on the already cancelled caller token.

Explicit caller cancellation is **Cancelled**, distinct from **TimedOut**. Pre-cancellation launches no child. If caller cancellation and the case deadline are both observed, explicit cancellation takes classification precedence, subject to termination failure. A worker emitting a cancellation record without a supervisor cancellation request is **WorkerFailure**, not normal rejection.

The .NET tree-kill API is not a containment sandbox. [Microsoft documents](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill?view=net-10.0) that waiting on the root does not prove all descendants have exited, and inaccessible descendants can be skipped. Tests independently verified exit of controlled root/descendant identities after timeout and cancellation. An exited parent leaving an inherited pipe open produces TerminationFailed after bounded cleanup; its controlled test descendant is independently reaped by the test. The supervisor cannot discover arbitrary already-detached descendants through an exited parent using this API, and pipe EOF cannot prove no detached process exists. A real TerminationFailed result requires caller/operator attention; no such process was left by this validation run. Stronger OS containment or general orphan discovery is not implemented or claimed.

## Capture and protocol

Each stream retains at most **65,536 bytes**, with a fixed 4,096-byte read scratch buffer. Capture counts saturate at the retained limit and use a boolean truncation flag; there is no unbounded total counter, string accumulation or line buffering. Both streams continue draining beyond the cap until EOF or cleanup. Multibyte UTF-8 tests verify byte bounds rather than character bounds.

Captured bytes stay internal. The supervision result exposes only fixed status, validated target/configuration metadata, numeric exit code, a validated fixed worker code, retained byte counts and truncation flags. It does not expose observations or raw child output. Its deterministic JSON contains no paths, document bytes, arbitrary exception text, environment values, PIDs or timing measurements. Tests bound serialized metadata to 512 UTF-8 bytes.

Acceptance requires exactly one valid worker record on **stdout**, at most **512 UTF-8 bytes**, with an optional single LF/CRLF terminator. Stderr must be empty. A truncated stream, oversized record, extra record, duplicate/unknown/missing property, invalid UTF-8/JSON, excessive JSON depth, wrong property type, wrong target/configuration/schema, inconsistent outcome/code/observation or exit-code mismatch fails validation. JSON parsing is depth-bounded. Success requires a 64-character uppercase hexadecimal digest; rejection uses the existing bounded FuzzResult factory contract. This validates protocol structure without extending target exception allowlists. Child observations are validated but not copied to normal supervision results.

| Status | Meaning |
|---|---|
| Completed | Valid single worker protocol; inspect ExitCode: 0 success, 1 expected rejection, 2 worker input validation failure. Completed alone does not mean target success. |
| WorkerFailure | Valid worker failure record (exit 4), or unrequested worker cancellation (exit 3). |
| AbnormalExit | Exit outside the worker protocol's 0-4 range; never ordinary target rejection. |
| InvalidProtocol | Missing, malformed, duplicated, oversized, inconsistent or stderr-contaminated protocol. |
| TimedOut / Cancelled | Deadline / explicit caller cancellation with bounded cleanup completed. |
| StartFailed | Child launch failed; exception details/path are not returned. |
| TerminationFailed | Tree termination, root reaping or pipe cleanup could not be confirmed within the cleanup contract. |
| CaptureFailed | Pipe/process observation failed without being accepted as target rejection. |
| InvalidRequest | Unknown target, mismatched configuration or invalid request arguments; no child launched. |

## Memory and platform scope

The neutral launch preserves the scaffold's authorized `DOTNET_GCHeapHardLimit=0x10000000` (256 MiB) and `DOTNET_DbgEnableMiniDump=0`. The GC setting bounds managed heap configuration, **not native allocations, stacks, mapped memory or total RSS**. No hard whole-process memory limit, sampled RSS limiter, Job Object P/Invoke, Unix rlimit, container or new package was introduced. Dump configuration is not a guarantee against every OS crash-reporting mechanism.

Validation ran on **Windows x64, SDK 10.0.302 / runtime 10.0.10**. Supervisor mechanics use BCL process APIs, but Linux/macOS mechanics were not executed. Ordinary worker input admission remains Windows-local-file-only and fails closed elsewhere exactly as PP-AUD-031 records. No Linux/macOS replay readiness is claimed. The portability branch in the real-worker test expects UnsupportedInputPlatform elsewhere; it does not count that as successful target decoding.

## Files changed

Relative to the starting dirty tree: **8 files: seven added and one modified**.

| File | Change |
|---|---|
| tools/PiratePDF.Fuzz.Runner/WorkerSupervisor.cs | New request/launch, deadline, cleanup and protocol orchestration. |
| tools/PiratePDF.Fuzz.Runner/BoundedCapture.cs | New internal byte-bounded streaming capture. |
| tools/PiratePDF.Fuzz.Runner/WorkerProtocol.cs | New bounded, strict worker protocol validation. |
| tools/PiratePDF.Fuzz.Runner/SupervisionResult.cs | New safe metadata/status result contract. |
| tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj | Select executable test-child entry point without generating an empty SDK Main; retain normal xUnit discovery. |
| tests/PiratePDF.Fuzz.Tests/SupervisorTestChild.cs | New controlled child/grandchild behaviors in the existing test artifact. |
| tests/PiratePDF.Fuzz.Tests/WorkerSupervisorTests.cs | New 40 supervisor contract cases. |
| docs/reviews/Post-BF-1054-Batch-B9-Process-Supervision-Completion-Report.md | This report. |

No production implementation/API, target code, friend declaration, package, project reference, solution, CI, corpus, SharpFuzz or existing report/audit changed. No new test project was necessary.

## Validation and corrections

| Check | Actual final result |
|---|---|
| Smallest focused supervisor suite | **40 passed, 0 failed, 0 skipped**, Release. |
| Complete PiratePDF.Fuzz.Tests suite | **118 passed, 0 failed, 0 skipped**, Release: previous 78 plus 40 additions. |
| Affected Runner Release build | Passed, zero warnings and zero errors. |
| git diff --check | Passed; all eight slice files additionally checked as untracked/new content. |
| Full release gate | Not run, per the slice instructions; no production/repository-wide defect exposed. |

Exact commands:

```powershell
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~WorkerSupervisorTests --verbosity quiet
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --verbosity quiet
dotnet build tools/PiratePDF.Fuzz.Runner/PiratePDF.Fuzz.Runner.csproj -c Release --no-restore --verbosity quiet
git diff --check
```

Existing restored assets sufficed. No installation, restore of new dependencies or external fuzz executable was needed. Tests launch only the installed dotnet host with repository-owned runner/test assemblies. Temporary owned test files and PID/start-time handshakes are not a corpus; fixtures clean them up.

The 40 new cases cover real worker success/rejection and repeat output; 17 malformed/inconsistent/missing protocol scenarios; abnormal exit; worker failure/spontaneous cancellation; exact/over stdout/stderr retention including multibyte UTF-8; simultaneous draining beyond both caps; exact/over protocol record size; continuous-output timeout; timeout tree termination; explicit and pre-cancellation; exited-parent pipe cleanup failure; deterministic start failure; invalid registry/configuration admission; shell-free argument passing with spaces/metacharacters; and timeout/host guards. Controlled descendants have finite fallback lifetimes, and termination tests verify owned PID/start-time identities independently.

During implementation, tests exposed the SDK-generated empty Main shadowing the intended child entry point (CS8892). Selecting the explicit child entry point fixed the cause without warning suppression or loss of test discovery. A Unicode output fixture initially used the Windows console code page and emitted 32,768 bytes; changing that fixture to explicit UTF-8 made its intended 98,304-byte output deterministic. Final validation passed after both fixture corrections. Lifecycle review also ensured faults from abandoned composite waits are observed and root reaping is attempted even after a tree-kill error. No production defect or target exception-allowlist expansion occurred.

No SharpFuzz, instrumentation, libfuzzer-dotnet, campaign, corpus creation, replay, mutation or CI integration was performed. No later P2 item, canonical font editing or measurement-only P1 work started. Stop after this slice; **POST-P2-002 remains open**.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-24 | Record bounded neutral process supervision, strict protocol/capture contracts, 40 additional tests and explicit containment/platform limitations. |
