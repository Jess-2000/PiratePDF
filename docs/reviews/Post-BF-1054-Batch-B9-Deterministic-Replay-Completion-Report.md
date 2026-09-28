# Post-BF-1054 Batch B9 - Deterministic Supervised Replay Report

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-034 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1054 |
| **Last Updated** | 2026-09-24 |
| **Next Review** | 2026-12-24 |

## Disposition

**The deterministic supervised corpus replay slice is complete.** All **38 new replay tests passed** in agent-executed validation. The user subsequently reported **all 209 tests passed** in the requested permitted Windows context, including the unchanged symbolic-link fixture. Direct Release CLI replay passed twice with identical output: **15 admitted, 15 executed, 15 oracle matches, zero mismatches**. The earlier agent-executed 208/209 result and fixture-creation privilege failure are preserved below as historical evidence.

**Overall B9 is not complete; POST-P2-002 remains open.** This report supplements PP-AUD-029 through PP-AUD-033; those historical reports and the repository-wide audit were not rewritten. The prior user-reported 171/171 result and PP-AUD-033 closure remain valid historical evidence; the new user-reported 209/209 result validates the expanded suite.

## Starting inventory

Branch: `maintenance/post-BF-1054-repository-review`. HEAD: **93865f639acf96f0b9598c97b04d49cea38e8260**. The starting tree contained **66 modified/untracked files: seven modified tracked files and 59 untracked files**. The modified tracked files were .gitignore, AGENTS.md, PiratePDF.slnx, docs/Documentation-Health.md, docs/Documentation-Index.md, docs/ValidationReport.md and docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md. Existing untracked work comprised the five B9 reports, dotnet-tools.json, neutral tooling/tests, SharpFuzz scaffolding and the owned corpus.

Root AGENTS.md, PROJECT-CHARTER.md, PP-AUD-029/030/031/032/033, the actual registry/adapters, runner/protocol/supervisor, manifest parser/validator and existing tests were read. Their instructions and implementation remain authoritative. No reset, clean, revert, recreation, commit, push, merge, tag or branch switch occurred.

| Registered target | Configuration | Input ceiling | Isolation metadata | Persisted/admitted seeds |
|---|---:|---:|---|---:|
| filter-asciihex | 1 | 16 KiB | SupervisedManagedWorker | 6 |
| filter-flate | 1 | 16 KiB | SupervisedManagedWorker | 6 |
| font-sanitize | 1 | 64 KiB | SupervisedManagedWorker | 3 |

Manifest schema is **piratepdf-fuzz-corpus**, version **1**, with **15 repository-owned seeds**. These are the existing intended committed corpus files, still uncommitted in this working tree; no seed was added, regenerated, modified or imported. The reconciled baseline was **53 corpus/manifest tests and 171 complete Fuzz.Tests tests**, all passed in the user's permitted Windows context per PP-AUD-033 v1.0.

## Replay architecture and trust boundary

`CorpusReplay` accepts an explicit absolute repository root and a trusted configured `WorkerSupervisor`. It starts with the existing `CorpusValidator.ValidateAsync`. It does not accept caller-supplied seed lists or bypass validation. If schema, inventory, target/configuration, path, ceiling, length or hash validation fails, **no supervisor request is issued and no seed executes**. Only seeds from the fully successful read-only snapshot are eligible.

The validator's existing bounds remain unchanged: 128 KiB manifest, depth four, at most 128 seeds, at most 256 inspected filesystem entries, bounded metadata and exact ordinal paths. The existing closed vocabularies, provenance rules, duplicate/unlisted/missing rejection and target-specific byte ceilings remain in force. No recursive corpus search, arbitrary-file batch execution, in-process target invocation or duplicated parser/target implementation was added.

For each seed, in the validated manifest's ordinal order, replay builds its path from validated target and seed IDs. Immediately before supervision it calls the existing bounded local `InputFile` reader, applies the actual registry ceiling, recomputes byte length and SHA-256, and compares both to the published manifest metadata. Missing, oversized, changed or inaccessible input fails closed before that seed's launch. A mismatch never updates the manifest, its hash or its oracle. Tests have the first controlled worker change the next temporary fixture after whole-corpus validation; hash, length, oversize and removal changes all stop replay before the second child.

No discovery or test callback intervenes between the recheck and supervisor invocation. The temporary recheck buffer is not exposed in results. The existing supervisor launches a fresh child for every admitted case, with separate ArgumentList arguments, UseShellExecute=false and the unchanged worker protocol. There is no worker pool or in-process replay shortcut. Tests can configure the same supervisor with the repository-owned controlled test assembly; the CLI always selects the neutral runner assembly and installed dotnet host, with no corpus-controlled executable override.

Successful replay writes no corpus files or artifacts. Controlled test-file changes and identity/launch logs belong only to isolated temporary test fixtures; they are not a deterministic mutation engine or corpus campaign.

## Remaining filesystem and process limitations

**The worker still reopens the path after the recheck closes its read handle. Byte identity across verification and worker consumption is not atomic.** The recheck detects changes present at that point; it cannot prove the worker consumes the same bytes if another actor changes them afterward. No stronger guarantee is inferred from successful replay. Manifest edits after admission do not refresh the in-memory oracle snapshot.

BCL reparse checks remain conservative snapshots, **not an atomic filesystem sandbox**. They do not prove protection against concurrent parent substitution or general physical-link escape, and do not distinguish hard links. The previously validated real link test establishes the tested rejection behavior only. No native API, filesystem sandbox or new security policy was introduced. Operator-controlled local fixture directories remain required for reproducible replay.

File admission remains **Windows-local-file-only**. On non-Windows, corpus validation returns UnsupportedPlatform and replay returns IntegrityFailure with zero attempts. Tests assert this fail-closed contract where applicable; no Linux/macOS replay readiness or execution evidence is claimed.

The unchanged supervisor enforces a **10-second case deadline**, including exit and pipe EOF, and an independent **5-second cleanup budget**. Tests may shorten, never extend, the deadline. Both pipes retain at most **64 KiB** while continuing to drain excess output; acceptance requires a single strict worker record of at most **512 UTF-8 bytes** and empty stderr. Captured bytes and worker observations never enter replay output.

On timeout/cancellation the supervisor attempts process-tree termination and root reaping; cleanup failure remains TerminationFailed, including when cancellation was requested. Root exit/pipe EOF cannot prove all descendants exited, and BCL tree kill cannot guarantee discovery of inaccessible or already-detached descendants. Controlled timeout/cancellation tests independently check process identities; the deliberately orphaned-pipe fixture is independently killed/reaped by its test after confirming failed supervisor cleanup. A real TerminationFailed requires operator attention.

The child retains **DOTNET_GCHeapHardLimit=0x10000000** (256 MiB managed heap) and **DOTNET_DbgEnableMiniDump=0**. These are not a hard total-memory/RSS cap or a guarantee against every OS crash-report mechanism. Native allocations, stacks, mappings, OS process operations and detached descendants retain the limitations in PP-AUD-032. No Job Object, rlimit, container, native API or dependency was added.

## Oracle and failure policy

The policy is **sequential, ordinal and fail-fast**. Only `SupervisionStatus.Completed`, worker code TargetResult, and worker exit **0 or 1** can satisfy an oracle:

| Validated worker outcome | Manifest oracle | Replay decision |
|---|---|---|
| Completed / 0 / TargetResult | success | Match |
| Completed / 1 / TargetResult | rejected | Match |
| Completed / 0 / TargetResult | rejected | OracleMismatch; stop |
| Completed / 1 / TargetResult | success | OracleMismatch; stop |
| Completed / 2 (worker input validation) | Either | SupervisionFailure; stop |
| Any other supervisor status | Either | Failure or explicit cancellation; never an oracle match |

Timeout, abnormal exit, malformed protocol, worker failure, start/capture/termination failure and invalid supervisor request remain failures. Closed supervisor status is preserved in the result. A spontaneous worker cancellation remains WorkerFailure. Explicit caller cancellation stops new launches, waits for the supervisor's cleanup and returns Cancelled unless the supervisor reports a cleanup failure. Pre-cancellation launches nothing. Cancellation between cases or during the identity read is distinct as well. No target exception allowlist was broadened and no crash/resource failure was converted into expected malformed-input rejection.

## Result schema and exit codes

`ReplayResult.Json` is one deterministic JSON object, at most **768 UTF-8 bytes**, excluding the CLI newline. Schema version **1** contains these ordered fields:

`schemaVersion`, `manifestSchema`, `manifestVersion`, `status`, `exitCode`, `admitted`, `attempted`, `executed`, `matched`, `mismatched`, `corpusFailure`, `supervisionStatus`, `target`, `seed`.

The manifest schema/version fields identify the supported contract, including when validation fails. `admitted` is zero until whole-corpus validation succeeds. `attempted` counts supervisor requests, including a failed start. `executed` counts completed protocol-validated success/rejection outcomes; an infrastructure attempt may have started a child without completing a target outcome, so it is counted only in attempted. `matched + mismatched == executed`. Failure target/seed IDs come only from admitted bounded metadata. Corpus/supervision diagnostics are closed enum codes. Success has null failure metadata.

The library additionally exposes a read-only owned sequence of at most 128 `ReplayObservation` records, in execution order, containing validated target/seed/configuration and expected/actual outcome only. The compact CLI summary does not emit this ledger. Neither representation includes input bytes, child output, unrestricted exception text, environment values, absolute paths, PIDs, timestamps or durations. Runtime/process observations do not contribute to deterministic result identity.

| Replay exit | Status | Meaning |
|---:|---|---|
| 0 | Success | All admitted seeds completed and matched. |
| 1 | IntegrityFailure | Initial corpus validation or pre-execution identity/admission failed. |
| 2 | OracleMismatch | Valid completed outcome disagreed with the manifest. |
| 3 | SupervisionFailure | Worker/protocol/deadline/start/capture/cleanup failure; includes completed worker input failure. |
| 4 | Cancelled | Explicit cancellation with no failed supervisor cleanup. |
| 5 | InvalidInvocation | Incorrect replay options or missing/nonabsolute/invalid root argument. |

Child exit values are never forwarded as replay's public exit code. Expected target rejection therefore contributes to a successful replay, not replay exit 1. Existing `run` exit codes and dispatch are unchanged.

## CLI and direct evidence

The CLI uses the existing validator's repository-root convention. Exactly one explicit absolute root is required; its fixed `tests/FuzzCorpus` child is the only corpus selected. The current working directory is not searched for a repository or corpus.

```powershell
dotnet tools/PiratePDF.Fuzz.Runner/bin/Release/net10.0/PiratePDF.Fuzz.Runner.dll replay --repository-root C:\Development\PiratePDF
```

The installed host is derived from the current runtime installation, not PATH or a corpus/command option, and the worker is this runner assembly. This assumes the installed runtime and built repository artifact are trusted; path construction is not signature verification. Ctrl+C uses the existing cancellation handler. No shell is invoked to start workers.

The direct Release command ran **twice**, both with **exit 0**, identical **255-byte JSON** (excluding newline), and no raw paths, bytes or child output:

```json
{"schemaVersion":1,"manifestSchema":"piratepdf-fuzz-corpus","manifestVersion":1,"status":"Success","exitCode":0,"admitted":15,"attempted":15,"executed":15,"matched":15,"mismatched":0,"corpusFailure":null,"supervisionStatus":null,"target":null,"seed":null}
```

All **6 ASCIIHex, 6 Flate and 3 font-sanitization seeds** matched their independently authored manifest expectations. No production discrepancy or oracle correction was exposed.

## Files changed

Relative to the starting dirty tree: **9 files, seven new and two modified**.

| Path | Change |
|---|---|
| tools/PiratePDF.Fuzz.Runner/CorpusReplay.cs | New validation, bounded identity recheck, sequential supervision and fail-fast oracle comparison. |
| tools/PiratePDF.Fuzz.Runner/ReplayResult.cs | New bounded immutable result and explicit replay exit codes. |
| tools/PiratePDF.Fuzz.Runner/ReplayCommand.cs | New finite explicit replay invocation. |
| tools/PiratePDF.Fuzz.Runner/Program.cs | Add replay dispatch within the existing cancellation handler. |
| tests/PiratePDF.Fuzz.Tests/CorpusReplayTests.cs | New 38 replay contract cases. |
| tests/PiratePDF.Fuzz.Tests/ReplayFixture.cs | New isolated temporary manifest/seed and trusted supervisor fixture. |
| tests/PiratePDF.Fuzz.Tests/ReplayTestChild.cs | New controlled child oracle/identity-change scenarios, reusing existing failure fixtures. |
| tests/PiratePDF.Fuzz.Tests/SupervisorTestChild.cs | Dispatch explicit .bin replay fixtures to the controlled child helper. |
| docs/reviews/Post-BF-1054-Batch-B9-Deterministic-Replay-Completion-Report.md | This report. |

No production code/API, target implementation/registration, corpus/manifest, existing validator/admission/supervisor/protocol, project reference, dependency, friend declaration, CI, SharpFuzz tool or historical report/audit was changed. Hash comparison confirmed the **64 other pre-existing modified/untracked files** remained unchanged, and none disappeared.

## Tests and validation

Environment: Windows x64, .NET SDK **10.0.302**, runtime **10.0.10**. Existing restored assets sufficed. The 38 new cases cover real 15-seed repeat/order/count checks; four oracle combinations and no rewriting; nine initial integrity failures with zero launches; four post-validation identity changes; five abnormal/protocol/worker/input failures; start failure; timeout/cancellation cleanup; termination failure with independent fixture cleanup; pre-cancellation; real command replay; literal spaced/metacharacter paths and no shell; seven invalid invocations; stable exit-code mapping and bounded/no-sensitive output.

CaptureFailed and InvalidRequest are covered by the same fail-closed non-Completed classification branch; no artificial unsafe OS capture failure or injected supervisor bypass was added merely to manufacture those statuses. The existing supervisor suite continues to cover protocol/capture limits and request validation. Tests do not invoke external fuzz engines, networks, system fonts or private documents.

| Validation | Actual result |
|---|---|
| Focused CorpusReplayTests Release | **38 passed, 0 failed, 0 skipped**. |
| Complete Fuzz.Tests Release | Final user-reported permitted-context result: **209 passed, 0 failed, 0 skipped (209 total)**; 171 prior cases preserved plus 38 new cases. Earlier agent-executed result: **208 passed, 1 failed, 0 skipped**, due to fixture privilege. |
| Existing symbolic-link validation | Passed within the user's complete 209-test rerun. Earlier focused agent diagnostic: **1 failed** at File.CreateSymbolicLink, before corpus validator invocation. |
| Runner Release build | Passed, **0 warnings/errors**. |
| Targets Release build | Passed, **0 warnings/errors**. |
| Fuzz.Tests Release build | Passed, **0 warnings/errors**. |
| Direct replay CLI | Two successful identical results, 15/15 oracle matches each, exit 0. |
| git diff --check | Passed; new/untracked slice text additionally checked. |
| Full release gate | Not run, per scope; no production/repository-wide discrepancy exposed. |

Commands used:

```powershell
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CorpusReplayTests --verbosity quiet
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --verbosity quiet
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~CorpusManifestTests.SeedLinkEscapeIsRejected --verbosity normal
dotnet build tools/PiratePDF.Fuzz.Runner/PiratePDF.Fuzz.Runner.csproj -c Release --no-restore --verbosity quiet
dotnet build tools/PiratePDF.Fuzz.Targets/PiratePDF.Fuzz.Targets.csproj -c Release --no-restore --verbosity quiet
dotnet build tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --verbosity quiet
git diff --check
```

The first build flagged CA1822 on the stateless replay command; making it static resolved the analyzer finding without suppression. All subsequent affected builds passed. No production correction or accepted-exception expansion occurred.

The only failure in the earlier agent-executed complete suite was the unchanged `CorpusManifestTests.SeedLinkEscapeIsRejected` at line 300: Windows reported **"A required privilege is not held by the client"** during symbolic-link fixture creation, before validator invocation. No test was skipped, weakened or removed. Root AGENTS.md requires a manual permitted-context rerun for this actual privilege failure. The user was asked to run the following in **Administrator PowerShell**, from **C:\Development\PiratePDF**, and report both final results:

```powershell
Set-Location C:\Development\PiratePDF
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CorpusManifestTests.SeedLinkEscapeIsRejected --verbosity normal
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --verbosity normal
```

On **2026-09-24**, the user replied **"All 209 tests passed"**. This is the final user-reported complete-suite validation, including the symbolic-link test, and closes the fixture privilege blocker for this slice. No separate numerical result for the standalone focused manual command was supplied or inferred. The passing real symbolic-link test confirms its intended escape rejection for that fixture, without broadening the filesystem-containment claims above. The earlier PP-AUD-033 user validation is preserved, not substituted for this fresh expanded-suite result. No security/OS policy change or dependency installation was requested. Only report wording changed after the successful manual suite; final whitespace and scope checks were refreshed.

## Stop boundary

No deterministic mutation engine was implemented or run. SharpFuzz and libfuzzer-dotnet were not executed; no assembly instrumentation, guided campaign or CI integration began. No target family, external/private corpus, later P2 item, canonical font editing or measurement-only P1 work was added. No full release gate, commit, push, merge, tag or branch switch occurred. **POST-P2-002 remains open.**

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-09-24 | Record deterministic supervised replay, 38 passing new tests and two identical 15-seed CLI passes; retain the expanded-suite Windows symbolic-link fixture privilege blocker pending manual validation. |
| 1.0 | 2026-09-24 | Close the fixture privilege blocker using the user's 209/209 complete-suite pass; complete the replay slice while preserving historical failure evidence, containment limitations and open overall B9 status. |
