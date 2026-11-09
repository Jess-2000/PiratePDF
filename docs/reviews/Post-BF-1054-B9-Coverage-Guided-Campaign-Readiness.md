# B9 Coverage-Guided Campaign Readiness

Status: PARTIAL — toolchain verified and campaign plans prepared; not approved for substantive execution.

Branch: maintenance/post-BF-1054-repository-review; HEAD: f51824dbc031b58497a38a6292ee11568b9ffeda.

Evidence: artifacts/fuzz/readiness-20261010-01. Entry status, tracked diff, exact untracked inventory and relevant hashes saved before validation. Stabilized 269-test Administrator evidence is preserved, not campaign evidence.

## Preservation and scope

Entry: 9 tracked modifications, 42 untracked files. Exact paths and tracked patch are in entry-status.txt, entry-untracked.txt and entry-diff.patch. All 104 captured source/configuration/report/tool/corpus/stabilization-evidence hashes still match after validation (preservation-check.json). The stabilized Administrator TRX and its 269-test source remain preserved. No pre-existing source, tests, reports, package, smoke harness, permanent corpus, CI, release gate or dormant integration changed. Public dispatch is unchanged. The only new repository document is this report; generated plans, seed copies, assemblies and validation evidence are ignored artifacts. No Git history operations occurred.

Root instructions, charter, accepted maintenance handoff, PP-AUD-036/037/038/039, stabilization report, corpus manifest, actual harness/registry/session/transport/launcher sources and historical Stage.ps1 were inspected. Historical smoke and 269-test results are distinguished from the fresh checks below. No substantive campaign or release gate ran. Completion-2 remains PARTIAL; B9 / POST-P2-002 remains OPEN.

## Actual installed toolchain

| Component | Fresh observation |
|---|---|
| Windows / .NET | Windows x64, SDK 10.0.302, runtime 10.0.10; dotnet-info.log, exit 0. Host C:\Program Files\dotnet\dotnet.exe. |
| SharpFuzz CLI | Repository-local sharpfuzz.commandline 2.3.0; local-tools.log, exit 0. Root dotnet-tools.json is authoritative. Nested .config/dotnet-tools.json also exists in the optional project. |
| SharpFuzz runtime | Project pin and lock: SharpFuzz 2.3.0, SharpFuzz.Common 2.2.0, dnlib 4.4.0; fresh optional project --no-restore build succeeds. |
| LLVM | C:\Program Files\LLVM\bin\clang.exe, 23.1.2, commit 85ac560262434c9ccfc0c183ec22d4138ed647fb; clang-version.log, exit 0. No replacement driver compiled. |
| Native driver | tools/PiratePDF.Fuzz.SharpFuzz/tools/libfuzzer-dotnet-windows.exe, 1,512,448 bytes; SHA-256 17AF5B3F6FF4D2C57B44B9A35C13051B570EB66F0557D00015DF3832709050BF. Accepted provenance identifies v2025.05.02.0904; live hash matches. |
| Driver usage | Initial -help=1 without target returns 1 (missing --target_path), retained in driver-help.log. Corrected help with trusted target_path/target_arg returns 0; driver-help-with-target.log verifies runs, seed, max_total_time, timeout, max_len, rss_limit_mb, artifact_prefix and print_final_stats. |

The driver still emits the three accepted missing sanitizer-helper warnings: __sanitizer_acquire_crash_state, __sanitizer_print_stack_trace, __sanitizer_set_death_callback. They are preserved, not suppressed; no comprehensive sanitizer compatibility is inferred.

Actual Windows transport uses one --target_path containing the quoted dotnet executable and one --target_arg containing the quoted glue DLL followed by the target identifier. Repeated target_arg is incorrect for this pinned driver. WindowsLibFuzzerArguments validates only the fixed smoke allowlist, so it is not modified or described as a general campaign API. The glue itself accepts one target ID and resolves the full registry inside the initialized LibFuzzer callback.

No campaign dictionary was found under tools/tests. No macOS driver is provisioned or exercised here. Windows .exe/CreateProcess quoting, lease/file admission and current smoke launcher are not macOS launch instructions. macOS tooling, path admission, native launch and cleanup remain unverified; do not invent a Mac command. Linux policy tests are likewise not native campaign evidence.

## Fresh bounded validation

All runs below occurred on 2026-10-10 local time (2026-10-11 UTC). Logs/commands/TRX/results remain create-new evidence.

| Check | Outcome |
|---|---|
| Optional SharpFuzz Release build, --no-restore | Exit 0, zero warnings/errors; sharpfuzz-build.log and sharpfuzz-build-result.json; build-reported 9.40 seconds. |
| Staging/instrumentation | Existing Stage.ps1 copied its fixed 23-file set twice into fresh readiness-20261010-instrumented and readiness-20261010-control areas. Instrumentation changed only the Targets copy; original outputs and other staged files stayed equal, recorded in before.json/after.json. |
| Initialized managed controls | Both registry-runtime controls exit 0, no timeout/truncation, cleanup true. Instrumented callback observes initialized counter memory and asserts ASCIIHex success/rejection plus all three registry IDs. These controls do not execute native Flate/font campaigns. |
| Fresh staged IL inspection | inspect-il exits 0 with bounded capture/cleanup; injected Trace::SharedMem, Trace::PrevLocation and ldind.u1 counter access are present; inspect-il.log and staged inspect-il.json. |
| Native ASCIIHex compatibility | Existing unchanged SmokeCommand, isolated smoke-root containing exact copied corpus/driver/stage: exit 0; 128 runs, <=3s native total, 1s input timeout, 10s outer plus 5s cleanup; 64KiB retained per pipe. Cleanup true, no truncation. Exact arguments and bounded result are retained under smoke-root. |
| Focused readiness tests | 52 total/executed/passed, zero failed/skipped, exit 0; readiness-focused.trx/log; runner time 5.1028s. Selection: SharpFuzzTargetSessionTests, TargetRegistryTests, WindowsLibFuzzerArgumentsTests, ManagedTargetTests, SmokeChildProcessTests. |
| Seed identity | All 15 owned seed lengths/hashes match immutable manifest; manifest and files unchanged. |
| Whitespace/preservation | git diff --check exit 0 (existing CRLF notice); 104/104 entry hashes unchanged. New report explicitly checked separately. |

The isolated smoke root avoids overwriting historical fixed smoke paths. Its lease markers record runtime-call, entered, success and rejected. A separate exact PID/start-time check finds the observed managed child absent (smoke-child-check.json). Native evidence reports 65,536 extra counters, INITED ft:40, two REDUCE events and stat::new_units_added:2; 128 executed units. The working seed copy has generated reductions; original seeds remain immutable.

This ties managed extra-counter feedback and corpus evolution to the staged instrumented PiratePDF.Fuzz.Targets.dll, supported by initialized controls and callback markers, rather than native process exit alone. Native cov:2 is not two managed branches or PDF semantic coverage. Engine/framework/dependency assemblies were not instrumented. Useful depth inside uninstrumented decoder/sanitizer implementation is not established. No coverage percentage, parity or campaign result is claimed.

Fresh Targets hashes: original/control FCAFCF6A99E7F97CDCDA2C3416E26B9C5CFE97B07597DCB46C123DB5050E5B91; instrumented copy 040493430DCF1CAA509F96F7C77DFF275C0803AFDFF6D8D42548ADB429014CEC. trusted-binary-hashes.json retains all staged assembly identities and installed host/clang hashes. validation-summary.json retains observed exits; asciihex-working-corpus-after.json retains generated working-copy identities. No original corpus/output was instrumented.

Additional staged controls are in artifacts/fuzz/initialization-20260925/readiness-20261010-instrumented and readiness-20261010-control. Their staging/control commands and logs are retained in the readiness directory; these fresh areas are not reused. Fresh real native Flate/font compatibility checks and new native failure probes were not run: the unchanged supervised smoke interface does not admit those targets. Their adapter contracts are covered by the 52-test selection; that does not substitute for native launch/feedback checks.

## Proposed campaign contract (requires approval)

| Target | Owned starting corpus | -max_len | Proposed per-target bound |
|---|---|---|---|
| filter-asciihex | 6 seeds / 39 bytes | 16384 | 10000 executions OR 60 native seconds, whichever stops first |
| filter-flate | 6 seeds / 8873 bytes | 16384 | Same |
| font-sanitize | 3 seeds / 579 bytes | 65536 | Same |

All use seed 1049, configuration v1, one driver/managed target pair at a time, no parallel jobs, -timeout=10, -rss_limit_mb=512 and -print_final_stats=1. Proposed independent launcher allowance is 75 seconds per target including startup/exit, with separate <=5-second exact-instance cleanup. These outer allowances are not currently connected to a substantive launcher and are not OS-hard deadlines. Interrupted or timeout runs remain partial, never completed campaigns.

The installed driver documents -runs as number of individual runs and -max_total_time as maximum fuzzing time. Startup/cleanup may fall outside native fuzzing time; neither guarantees exactly 10000 mutations, useful target exploration, clean descendant exit or independent wall-clock containment. Reaching one limit does not satisfy coverage/finding acceptance by itself. Per-input timeout is native driver behavior, distinct from existing fresh-worker supervision. Runtime DOTNET_GCHeapHardLimit=0x10000000 and DOTNET_DbgEnableMiniDump=0 should be explicitly recorded for future execution. GC heap limit excludes native allocations/stacks/mappings. Driver RSS accounting is not proven aggregate containment of the separate managed process.

FuzzTarget.Run independently rejects inputs over the registered ceiling before adapter allocation; driver max_len is an additional generator bound. Filters have 32KiB decoded-output and ratio-32 limits, fixed single filter/no predictor, pipeline four-filter ceiling and 1KiB buffer. Font sanitizer has 64KiB input/table, 32 tables, 256 glyphs, 16 CMap subtables and 64 name records. No bound is enlarged.

Proposed evidence controls: retain at most 64KiB stdout and 64KiB stderr while continuously draining; proposed mutable corpus ceiling 128 files / 8MiB; stop on any raw failure artifact and preserve it, at registered input ceiling (<=64KiB). PP-AUD-037 retention remains 16 findings / 256KiB originals per run; overflow is an explicit stop, not deletion. Native flags alone do not enforce corpus inventory/byte or retained log ceilings. Those monitor/cleanup gaps block substantive execution readiness. No 16MiB first-party mutation work accounting is claimed for libFuzzer.

## Prepared command matrix and provenance

campaign-matrix.json records exact trusted paths, target, ceilings and seed counts. campaign-plan/<target>/arguments.json contains observed native argument syntax with the proposed substantive budgets. Each target has its own fresh writable corpus and empty raw directory; seed-manifest.json records original/copy path, length/hash. No permanent corpus is passed to the driver. Shared glue path is the freshly verified instrumented staging copy, not ordinary outputs.

These directories are PLANNED-NOT-EXECUTED. Refuse stale runs, foreign files, reparse/device paths or changed binaries/seeds; do not overwrite/recycle them. A future invocation needs preflight identity comparison and an exclusively claimed run directory. Current plans are not a one-use admission protocol and must not be presented as such.

Safe plan inspection now, regular PowerShell (does not launch the driver):

```powershell
Set-Location C:\Development\PiratePDF
Get-Content artifacts/fuzz/readiness-20261010-01/campaign-matrix.json
Get-Content artifacts/fuzz/readiness-20261010-01/campaign-plan/filter-asciihex/arguments.json
Get-Content artifacts/fuzz/readiness-20261010-01/campaign-plan/filter-flate/arguments.json
Get-Content artifacts/fuzz/readiness-20261010-01/campaign-plan/font-sanitize/arguments.json
```

Native command bodies below are for future approved orchestration in an independent regular PowerShell terminal, not instructions to run now. Administrator is unnecessary for ordinary copied-file/native execution; only separately selected real symlink-fixture tests may require it. Unsupervised execution below lacks bounded capture and independent cleanup, so do not run these standalone until those gaps are resolved:

```powershell
$driver = 'C:\Development\PiratePDF\tools\PiratePDF.Fuzz.SharpFuzz\tools\libfuzzer-dotnet-windows.exe'
$asciiHexArguments = Get-Content 'C:\Development\PiratePDF\artifacts\fuzz\readiness-20261010-01\campaign-plan\filter-asciihex\arguments.json' -Raw | ConvertFrom-Json
$flateArguments = Get-Content 'C:\Development\PiratePDF\artifacts\fuzz\readiness-20261010-01\campaign-plan\filter-flate\arguments.json' -Raw | ConvertFrom-Json
$fontArguments = Get-Content 'C:\Development\PiratePDF\artifacts\fuzz\readiness-20261010-01\campaign-plan\font-sanitize\arguments.json' -Raw | ConvertFrom-Json
# Proposed native command matrix, not executed:
# & $driver @asciiHexArguments
# & $driver @flateArguments
# & $driver @fontArguments
```

Native usage flags were checked against the installed executable; actual quoted argument transport is exercised by the ASCIIHex smoke. Merely generating Flate/font argument arrays does not prove their native callbacks or cleanup.

## Evidence, replay and acceptance

For each future target retain: exact argument array, trusted executable/staged and uninstrumented binary hashes, instrumentation before/after allowlist, runtime/tool versions, revision plus dirty-source hashes, explicit relevant environment settings, immutable seed manifest, before/after working corpus inventory/hashes, bounded native logs with truncation flags, start/end/elapsed/exit, root/managed exact identities and independent exit checks. Preserve emitted INITED/NEW/REDUCE/DONE/features/extra-counter metrics only when present. Treat missing/truncated required evidence as inconclusive. Fixed seed and corpus order improve repeatability but do not promise identical schedules across timings, runtime/builds or platforms.

Repeatable compatibility proof proposed for each native target: fresh identical staged assembly/seed copies; seed 1049; <=128 runs OR 3 native seconds; 1s per-input; independent 10s outer and 5s cleanup; 64KiB/pipe. Require entered/outcome markers for the selected target, known seed outcomes, emitted extra-counter/feature evidence and actual working-corpus evolution where observed. Compare an uninstrumented control, and retain a no-novelty outcome honestly. Do not invent a feature increase or equate exit 0 with feedback. This experiment was freshly executed for ASCIIHex only.

Stop progression on native crash/hang/OOM, nonzero launch/driver exit, corruption, failed cleanup, changed corpus identities, log/disk limits or ambiguous result. Preserve raw bytes and associated output without trusting artifact filenames as commands, target IDs or destinations. Treat interrupted shells/startup failures/cleanup failures as infrastructure or partial-run evidence. Never infer descendant exit from driver EOF. Hash-deduplicate retained findings while preserving per-run provenance. No automatic corpus promotion or minimization runs.

Use existing PP-AUD-037 retention/reproduction after operator classification: copy one bounded artifact to a newly owned incoming slot, write finite validated FindingSource metadata with actual target/configuration and trusted uninstrumented neutral-worker identity, then retain-finding and reproduce-finding. Reproduction uses two supervised neutral workers (10s case/5s cleanup), immutable originals and bounded evidence. Native failure and neutral success/rejection may differ; record nonreproducibility/harness attribution, do not relabel it an engine bug. Native driver exits are not neutral worker protocol exits. Only human-reviewed proposals can become permanent regression seeds.

Existing reproduction command shape, after a separately retained finding with chosen allowlisted IDs:

```powershell
dotnet tools/PiratePDF.Fuzz.Runner/bin/Release/net10.0/PiratePDF.Fuzz.Runner.dll reproduce-finding --repository-root C:\Development\PiratePDF --run <owned-run-id> --finding <retained-finding-id>
```

The placeholders are provenance identifiers, not current executable actions; no finding exists from this assignment. Preserve different native/neutral configurations and binary hashes instead of asserting equivalent failures.

## Readiness verdict and next boundary

Toolchain/version/hash, current build, managed instrumentation initialization, native ASCIIHex feedback/corpus evolution, ordinary three-target contracts, target caps, immutable seed copies, proposed budgets and exact native plans are verified/prepared. No controller was added: existing documented commands suffice for plans, but existing smoke-only supervision cannot safely be widened in this scope.

BLOCKERS for substantive execution: native Flate/font compatibility and feedback evidence; a bounded three-target launcher/monitor with 64KiB pipe capture, finite corpus/disk policy, independently finite launch/runtime/cleanup and verified cancellation/root/managed-child ownership; successful corresponding controlled failure tests. No safe current command proves all of these. Mac readiness is explicitly absent. The prepared matrix must remain unexecuted.

Next approval boundary: review proposed budgets and authorize a small tooling-only campaign-launch readiness slice (at most one PowerShell orchestrator with plan mode and focused tests), using existing driver/glue and exact-instance cleanup, followed only by <=128/3-second compatibility checks for all three targets and controlled interruption/failure proof. Do not change smoke bounds or resume first-party Completion-2. If process ownership cannot be verified without broader architecture, stop rather than claim cleanup. Substantive 10000/60-second campaigns require a subsequent explicit execution authorization after readiness passes.

B9 closure still requires substantive bounded coverage-guided execution on all three existing targets, honest instrumentation reach/feedback and campaign evidence, retained unexpected failures, deterministic supervised replay/triage, reviewed regressions where warranted, and separately required acceptance validation. This planning checkpoint, 128-run smoke and 269-test stabilization do not close B9, establish whole-engine semantic coverage or complete first-party Completion-2.
