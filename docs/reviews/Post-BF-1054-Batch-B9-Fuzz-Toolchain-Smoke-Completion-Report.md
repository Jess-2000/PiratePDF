# Post-BF-1054 Batch B9 - Fuzz Toolchain Smoke Completion Report

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-036 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1054 |
| **Last Updated** | 2026-09-26 |
| **Next Review** | 2026-12-26 |

## Current disposition (v1.0)

**Approved-toolchain compatibility smoke is complete. Instrumented startup, actual callback/coverage and all four controlled probes passed.** After the requested Administrator PowerShell rerun, the user reported **"All 297 tests passed"**, establishing final complete-suite evidence of **297 passed, 0 failed, 0 skipped**, including the unchanged `CorpusManifestTests.SeedLinkEscapeIsRejected` test. This is user-reported permitted-context validation, not an agent-executed privileged run.

The earlier agent-run result remains **296 passed, 1 failed, 0 skipped (297 total)**. Its sole failure occurred during symbolic-link fixture creation because Windows denied the privilege **before the validator ran**. No test was skipped or weakened. The successful complete suite now confirms the intended real symbolic-link escape rejection behavior for that fixture; it does not broaden the existing filesystem-containment claims.

The root cause was a **zero SharpFuzz coverage-memory pointer read by injected IL before Fuzzer.LibFuzzer.Run initialized coverage**, not a null registry/default descriptor or broken collection expression. Registry resolution now occurs in the initialized callback, using the unchanged registry and target APIs. Both initialized instrumented/uninstrumented controls pass with identical descriptors and outcomes. No dependency or production code changed.

The actual instrumented smoke completed **128 runs**, reported **65,536 extra counters / 40 features**, and exercised success and expected rejection. Controlled exception and abnormal exit returned driver **77**, the hanging callback returned driver timeout **70**, and explicit outer cancellation was independently recorded. All observed leased children exited and every cleanup succeeded. Detailed v0.3 evidence follows below. **This remains bounded compatibility validation; the substantive coverage-guided campaign has NOT begun. B9 / POST-P2-002 remain OPEN.**

## Historical v0.2 disposition

**The argument-transport correction passed 19 focused tests, but compatibility smoke remains incomplete.** One corrected, bounded retry launched the managed glue past its unchanged one-argument guard. The instrumented `TargetRegistry.Get` then threw `NullReferenceException` before `Fuzzer.LibFuzzer.Run` and the callback. No coverage was reported. The ten-second outer deadline terminated the stalled driver; cleanup succeeded and the leased managed child was confirmed absent afterward. The user's explicit stop conditions for a corrected transport that still does not reach the callback / establish coverage were applied. No further harness alteration or retry occurred.

Details of the correction, fresh evidence directory, and second failure follow in **Corrective continuation evidence (v0.2)** below. Fresh replay/mutation/full-suite validation remains pending; there is no v1.0 acceptance. No manual/Administrator action is currently required. Overall B9 / POST-P2-002 remain open. No substantive campaign or CI integration began.

## Historical v0.1 disposition

The following original attempt record is preserved as historical evidence, including its then-current next-step recommendation. The v0.2 continuation supersedes its current-status statements without rewriting the failed attempt.

**Compatibility smoke is blocked, not complete.** SharpFuzz instrumentation of the one allowlisted assembly copy returned zero and changed its hash; the original outputs and all other staged files remained unchanged. The native driver launched the managed glue, but the glue rejected its arguments before entering `Fuzzer.LibFuzzer.Run` or the target callback. The driver then stalled during startup. The independent ten-second outer deadline terminated/reaped it, cleanup reported success, and a subsequent identity check found the leased managed child had exited.

**No useful coverage feedback was obtained. No instrumented target invocation, normal/rejected target outcome, or coverage-guided compatibility is established.** This is a harness invocation failure, not evidence of a PiratePDF production defect or an incompatible .NET/SharpFuzz version. The user's explicit stop condition, **"the driver cannot execute the instrumented target,"** was applied after this first attempt. There was no second driver run or dependency substitution.

The substantive coverage-guided campaign has **NOT begun**. B9 and **POST-P2-002 remain OPEN**. PP-AUD-029 through PP-AUD-035 and the repository-wide audit were not rewritten.

## Starting state and exact changes

Branch: `maintenance/post-BF-1054-repository-review`. Starting HEAD: `93865f639acf96f0b9598c97b04d49cea38e8260`.

The initial working tree contained **82 modified/untracked files: seven tracked modifications and 75 untracked files**. The tracked modifications were `.gitignore`, `AGENTS.md`, `PiratePDF.slnx`, `docs/Documentation-Health.md`, `docs/Documentation-Index.md`, `docs/ValidationReport.md`, and `docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md`. Existing reports, local manifests, tooling, tests, and corpus were preserved. Their starting hashes are retained in the ignored evidence directory as `worktree-before.json`.

Root AGENTS.md, PROJECT-CHARTER.md, PP-AUD-029 through PP-AUD-035, the current registry, worker, corpus/mutation infrastructure, SharpFuzz references/lock/assets, local tool manifests, ignore rules, and prior artifacts were inspected. The accepted prior evidence remains **265/265 tests** and **15/15 replay matches**, as recorded in PP-AUD-035; those are historical results, not fresh regression validation for these changes.

Exactly **five source/document files** changed in this slice:

| File | Change |
|---|---|
| `tools/PiratePDF.Fuzz.Runner/ChildProcess.cs` | Smoke-only child launcher now reuses byte-bounded capture, distinguishes cancellation from timeout, and shares an independent five-second cleanup budget across root, leased child, and pipe draining. Lease reads use existing bounded regular-file admission and PID/start-time checks. Cleanup/truncation are explicit results. |
| `tools/PiratePDF.Fuzz.Runner/Program.cs` | Replaces the legacy inline smoke path with SmokeCommand and adds smoke Ctrl+C handling; neutral run/replay/mutate dispatch is retained. |
| `tools/PiratePDF.Fuzz.Runner/SmokeCommand.cs` | New fixed experiment entry point: pinned driver verification, existing corpus admission and per-copy identity recheck, fresh ignored mode directory, fixed bounds and absolute host/glue paths, bounded evidence retention and metadata-only console output. **Its inherited repeated-target-argument assumption is the blocker documented below.** |
| `tools/PiratePDF.Fuzz.SharpFuzz/Program.cs` | Exclusive child lease creation; fixed one-byte callback/outcome markers; opt-in controlled abnormal-exit, harness-failure and hanging probes. None of those callbacks was reached. |
| `docs/reviews/Post-BF-1054-Batch-B9-Fuzz-Toolchain-Smoke-Completion-Report.md` | This incomplete-slice evidence report. |

These implementation changes are build-validated, **not accepted as fully regression-tested**. No tests, production implementation/API, dependencies, target registry, corpus/oracles, CI, or historical reports changed. No reset, clean, revert, commit, push, merge, tag, or branch switch occurred.

## Verified provisioned toolchain

| Component | Current evidence |
|---|---|
| OS/architecture | Windows x64 / win-x64 |
| .NET SDK | `dotnet --version`: **10.0.302** |
| SharpFuzz package | **2.3.0**, local project version, lockfile and restored assets agree |
| Repository-local SharpFuzz.CommandLine | `dotnet tool list --local`: **2.3.0**, command `sharpfuzz`, root `dotnet-tools.json`; nested manifest also pins 2.3.0 |
| SharpFuzz dependencies | **dnlib 4.4.0**, **SharpFuzz.Common 2.2.0**, lockfile/restored assets agree |
| LLVM/Clang | `C:\Program Files\LLVM\bin\clang.exe --version`: **23.1.2**, commit `85ac560262434c9ccfc0c183ec22d4138ed647fb`, target `x86_64-pc-windows-msvc`; not used to compile a replacement driver |
| libfuzzer-dotnet | Approved prebuilt Windows component, pinned release **v2025.05.02.0904**; **1,512,448 bytes** |
| Driver SHA-256 | `17AF5B3F6FF4D2C57B44B9A35C13051B570EB66F0557D00015DF3832709050BF` |

The driver is `tools/PiratePDF.Fuzz.SharpFuzz/tools/libfuzzer-dotnet-windows.exe`. Its size/hash were checked before execution and again by SmokeCommand. No tool, runtime, package source, dependency, or global installation was added, restored, replaced, upgraded, or downgraded. Existing SharpFuzz/dnlib/Common MIT provenance and lockfile integrity records remain unchanged. Existing transitive SkiaSharp 4.150.1 graph remains unchanged; no native Skia asset was staged or exercised.

## Isolated staging and instrumentation

All new generated material is under the existing ignored root:

`artifacts/fuzz/toolchain-smoke-20260925/`

`git check-ignore` confirmed the instrumented destination is ignored. The directory did not exist before this slice and was created without deleting or overwriting previous `artifacts/fuzz/publish` work. A finite **23-name staging allowlist** copied normal Release output files, without recursion:

* `BlackFlag.Engine.Abstractions.dll`, `BlackFlag.Engine.Core.dll`, `BlackFlag.Engine.Document.dll`, `BlackFlag.Engine.Editing.dll`, `BlackFlag.Engine.Filters.dll`, `BlackFlag.Engine.Fonts.dll`, `BlackFlag.Engine.IO.dll`, `BlackFlag.Engine.Objects.dll`, `BlackFlag.Engine.Parsing.dll`, `BlackFlag.Engine.Rendering.dll`, `BlackFlag.Engine.Rendering.Skia.dll`, `BlackFlag.Engine.Security.dll`, `BlackFlag.Engine.Services.dll`, `BlackFlag.Engine.Syntax.dll`, `BlackFlag.Engine.Writing.dll`;
* `PiratePDF.Fuzz.Targets.dll`, `PiratePDF.Fuzz.SharpFuzz.dll`, `PiratePDF.Fuzz.SharpFuzz.deps.json`, `PiratePDF.Fuzz.SharpFuzz.runtimeconfig.json`;
* `dnlib.dll`, `SharpFuzz.dll`, `SharpFuzz.Common.dll`, `SkiaSharp.dll`.

These are runtime-support copies, **not an instrumentation directory scan**. Each basename was checked, copied without overwrite, and SHA-256 checked against its original. The instrumentation allowlist was exactly **one literal destination**, `publish/PiratePDF.Fuzz.Targets.dll`. Dependencies, framework/package assemblies, production assemblies, and normal bin/obj outputs were **not instrumented**.

| Identity | SHA-256 |
|---|---|
| Original `tools/PiratePDF.Fuzz.Targets/bin/Release/net10.0/PiratePDF.Fuzz.Targets.dll` | `715B51FD5D8531131BAC4A1A58764AF50592D615C358EE3B22BEA8DFB1E36118` |
| Normal SharpFuzz output copy, before/after instrumentation elsewhere | `715B51FD5D8531131BAC4A1A58764AF50592D615C358EE3B22BEA8DFB1E36118` |
| Staged copy before instrumentation | `715B51FD5D8531131BAC4A1A58764AF50592D615C358EE3B22BEA8DFB1E36118` |
| Staged copy after instrumentation | `522025E919037552D4DD3571F06BE75F0B4385A3B6E101F3894E758380836672` |

`staging-before.json` and `staging-after.json` retain all 23 file identities/hashes. Every source file and all 22 non-instrumented copies matched their initial hashes after instrumentation. The separate Targets project output was also checked after the smoke attempt and remained unchanged. Successful instrumenter exit and changed copy are binary-rewrite evidence, **not runtime coverage evidence**.

## Commands and bounds actually attempted

Commands ran from `C:\Development\PiratePDF`, using already restored dependencies:

```powershell
dotnet build tools/PiratePDF.Fuzz.SharpFuzz/PiratePDF.Fuzz.SharpFuzz.csproj -c Release --no-restore --verbosity minimal
dotnet build tools/PiratePDF.Fuzz.Runner/PiratePDF.Fuzz.Runner.csproj -c Release --no-restore --verbosity minimal
dotnet tool run sharpfuzz artifacts/fuzz/toolchain-smoke-20260925/publish/PiratePDF.Fuzz.Targets.dll
dotnet tools/PiratePDF.Fuzz.Runner/bin/Release/net10.0/PiratePDF.Fuzz.Runner.dll smoke C:\Development\PiratePDF filter-asciihex
```

The last command is the **failed historical invocation**, not an instruction to rerun against or erase the retained directory. SmokeCommand rejects reuse of that directory.

The driver executable was the pinned absolute path above. Its exact `ProcessStartInfo.ArgumentList` is retained in `filter-asciihex/arguments.json`:

```text
--target_path=C:\Program Files\dotnet\dotnet.exe
--target_arg=C:\Development\PiratePDF\artifacts\fuzz\toolchain-smoke-20260925\publish\PiratePDF.Fuzz.SharpFuzz.dll
--target_arg=filter-asciihex
-seed=1049
-runs=128
-max_total_time=3
-timeout=1
-max_len=16384
-rss_limit_mb=512
-print_final_stats=1
-artifact_prefix=C:\Development\PiratePDF\artifacts\fuzz\toolchain-smoke-20260925\filter-asciihex\
C:\Development\PiratePDF\artifacts\fuzz\toolchain-smoke-20260925\filter-asciihex\seeds
```

The intended target was **filter-asciihex / configuration 1**, with registry maximum input **16 KiB**, matching max_len. The whole existing corpus was admitted before staging six owned ASCIIHex seeds (**39 bytes total**); each was reread through InputFile and checked against its admitted length/SHA-256 before copying. The input path is not external corpus discovery. Persisted seeds and manifest were never rewritten. No target execution count is claimed: the **128 runs / three seconds / one-second input timeout** are requested internal bounds, and startup did not reach them.

Outer execution had a **ten-second deadline**, independent **five-second cleanup budget**, fixed **64 KiB retained bytes per pipe** with continued draining, 4 KiB read scratch per pipe, and **100-byte lease admission**. Inherited environment overrides were **DOTNET_GCHeapHardLimit=0x10000000 (256 MiB managed heap)** and **DOTNET_DbgEnableMiniDump=0**. The 512 MiB fuzzer RSS option is recorded as a requested setting, not a proven memory containment mechanism.

## Actual failure, warnings, and coverage evidence

The retained result is bounded, **444 bytes**, with no capture truncation. It reports:

* driver exit **-1** after forced termination;
* **TimedOut=true**, **Cancelled=false**, **CleanupSucceeded=true**;
* empty stdout; no coverage/feature/run counters;
* glue usage diagnostic: `Specify one target ID or an explicit compatibility probe.`

All three prior sanitizer-helper warnings recurred unchanged:

```text
WARNING: Failed to find function "__sanitizer_acquire_crash_state".
WARNING: Failed to find function "__sanitizer_print_stack_trace".
WARNING: Failed to find function "__sanitizer_set_death_callback".
```

The child lease proves the managed entry point executed. No `.entered`, `.success`, or `.rejected` marker exists: its argument guard returned before the fuzz callback. The precise cause is the repeated `--target_arg` construction inherited from the earlier scaffold: the pinned driver's parser accepts the **first** target argument and ignores subsequent occurrences. Its Windows launcher constructs the child command from that one value. See the [pinned driver source, parse_flags and process construction](https://github.com/Metalnem/libfuzzer-dotnet/blob/v2025.05.02.0904/libfuzzer-dotnet-windows.cc). This explains the observed glue usage rejection; it is not a reason to change tool versions.

The source also places the initialization pipe wait before closure of inherited write handles. A child exiting during initialization can therefore leave startup waiting; this is consistent with the observed deadline, rather than proof of a sanitizer-warning failure. The report does not assert that the warnings are harmless: their impact on required crash/timeout/coverage behavior remains **uncharacterized** because startup never completed.

Expected target rejection, controlled harness exception, controlled abnormal exit, internal fuzzer timeout, and explicit outer cancellation were **not run** after the stop condition. The only demonstrated failure path is glue usage rejection followed by an actual outer startup timeout and successful cleanup of the observed processes. No unexpected production exception was accepted as malformed input, no oracle was changed, and no production defect or crash was observed.

## Validation and retained artifacts

| Check | Result |
|---|---|
| Approved versions and driver hash | Match |
| SharpFuzz Release build | Passed, zero warnings/errors; builds Targets and its references |
| Runner Release build | Passed, zero warnings/errors; builds Targets and its references |
| Instrument explicit copy | Command exit zero; hash changed |
| Original and other staged hashes | Preserved |
| Actual target callback / useful coverage | **Not established; startup failed** |
| Outer deadline / observed child cleanup | Exercised; success reported, leased managed PID absent on subsequent check |
| Expected rejection / probes | Not reached or run |
| Fresh deterministic replay / mutation regressions | **Not run after the mandatory stop** |
| Fresh complete Fuzz.Tests | **Not run after the mandatory stop**; historical 265/265 is not reused as acceptance for these changes |
| git diff --check | Passed, including separate whitespace checks for the new/untracked slice files |
| Full release gate | Not run; not requested for this slice and no broader production issue exposed |

Artifacts retained in the ignored directory: 23 staged support files including the instrumented copy, the before/after staging ledgers, initial worktree hash inventory, six admitted seed copies, exact argument array, child identity lease, and bounded result. No new coverage-generated seed, crash input, minimized artifact, or persisted corpus promotion exists. Earlier ignored artifacts were left intact. This evidence is retained for review and must not be overwritten or automatically cleaned; any later retry needs a separately owned fresh area. No private/user document, external corpus, system font, network target, or native decoding target was used.

No manual or Administrator command was required in this attempt. The previous symbolic-link validation remains historical accepted evidence. A fresh complete suite is still pending; no privileged result is claimed for this slice.

## Limitations and next boundary

BCL reparse checks are conservative snapshots, not an atomic filesystem sandbox. They do not prove protection against concurrent parent substitution or distinguish hard links. Paths and executables rely on an operator-controlled workspace; this is not a security boundary against a same-trust actor. PID plus start time reduces PID-reuse mistakes, but observed root/leased-child exit does not prove guaranteed termination of inaccessible or detached descendants. No hard total RSS cap, complete process sandbox, native sanitizer coverage, or OS-wide crash-dump suppression is claimed. The managed heap setting excludes native allocations, stacks and mappings.

**Recommended next B9 slice: correct only Windows driver-to-glue argument transport and add focused launcher regression coverage, then resume this compatibility smoke in a fresh ignored area using the same pinned tools and allowlist.** Preserve this failed evidence. Establish callback execution and meaningful coverage before attempting the controlled failure/timeout/cancellation probes, then run replay/mutation regressions and the complete Fuzz.Tests suite. No substantive guided campaign or CI integration should start from the evidence in this report. POST-P2-003, canonical font editing and P1 measurement remain out of scope.

## Corrective continuation evidence (v0.2)

### Scope and transport characterization

The continuation began on the same branch and HEAD with **84 modified/untracked files: seven tracked modifications and 77 untracked files**. Root instructions, charter, PP-AUD-029 through PP-AUD-035 context and the complete PP-AUD-036 record were reconciled with the actual tree. No prior B9 work was discarded.

Before tooling edits, the pinned source's `parse_flags` and Windows `CreateProcess` construction were inspected again against the actual historical run, whose executable still has the approved hash. `parse_flags` assigns `target_arg` only while it is unset, so a second occurrence is ignored. The launcher appends the first value verbatim after `target_path` when building its Windows child command line. The retained usage rejection is consistent with the DLL being passed without its target ID. This contract is now encoded at the caller, not by weakening the managed guard.

New helper `WindowsLibFuzzerArguments.Create` returns exactly these two driver options (each line is one `ArgumentList` element, including the literal inner quotes):

```text
--target_path="C:\Program Files\dotnet\dotnet.exe"
--target_arg="C:\Development\PiratePDF\artifacts\fuzz\toolchain-smoke-20260925-transport\publish\PiratePDF.Fuzz.SharpFuzz.dll" filter-asciihex
```

The host is quoted as well, avoiding ambiguous executable parsing at its space. The outer BCL `ArgumentList` carries these values to the driver; the retained inner quotes delimit its raw Windows child command line. Fuzzer options remain separate driver arguments. No cmd.exe, PowerShell intermediary, shell command string, environment-based target selection, package change, or driver patch was introduced. The glue still requires exactly one positional target/probe and was not edited in this continuation.

This is deliberately a narrow file-path contract, not a generic Windows quoting implementation. It accepts absolute local dotnet.exe/DLL paths up to 4,096 characters, rejects quotes/control characters, UNC paths, alternate-stream colons and invalid path characters, and requires the fixed target/probe allowlist. File suffixes exclude terminal backslashes, so wrapping a validated path in quotes cannot produce a backslash-before-closing-quote ambiguity. Unknown or multi-token target strings fail before argument construction; callers cannot submit duplicate target options through this helper.

An initial test path containing a semicolon failed **before managed Main**, with `Failed to create CoreCLR, HRESULT: 0x80070057` (host exit -2147450743). That run also included percent/bang/caret/apostrophe/bracket characters. Removing the semicolon while retaining those other characters passed. The helper therefore explicitly rejects semicolon-containing paths, with a negative test, rather than claiming support or silently rewriting them. This is a documented host-path limitation, not a PiratePDF target defect. A transient xUnit2031 assertion-style warning was corrected; the final focused run had no warnings.

### Focused tests and files changed

The **19 passing new cases** comprise four exact construction cases, four unknown/ambiguous target rejection cases, six invalid/unsupported path rejection cases, one shell-host rejection case, and four actual Windows child launches. Real-launch fixtures copy only top-level managed test runtime files into owned ignored `artifacts/fuzz/transport-tests/<unique>/<case>/` directories, preserving names. They exercise the helper's complete target-argument value through the Windows/.NET process parser and assert exactly one received `filter-asciihex` argument, successful exit, empty stderr, and no shell invocation. Paths cover ordinary names, spaces, parentheses/ampersands, and literal `% ! ^ ' [ ]`. Each fixture uses bounded capture, a five-second deadline and five-second cleanup, then deletes only its owned files and empty directories, without recursive deletion.

Exactly **five files** were added/modified by this corrective continuation:

| File | Change |
|---|---|
| `tools/PiratePDF.Fuzz.Runner/WindowsLibFuzzerArguments.cs` | New narrow pinned-driver transport helper with bounded, unambiguous path/target validation. |
| `tools/PiratePDF.Fuzz.Runner/SmokeCommand.cs` | Uses one complete target_arg, selects a fresh fixed evidence directory, and keeps every probe's internal timeout at one second (removing the unexecuted legacy 60-second override). |
| `tests/PiratePDF.Fuzz.Tests/WindowsLibFuzzerArgumentsTests.cs` | New 19-case transport/quoting and actual process-parser coverage. |
| `tests/PiratePDF.Fuzz.Tests/SupervisorTestChild.cs` | Controlled one-argument echo fixture only; no document target invocation. |
| `docs/reviews/Post-BF-1054-Batch-B9-Fuzz-Toolchain-Smoke-Completion-Report.md` | v0.2 continuation and preserved v0.1 evidence. |

The earlier ChildProcess/Program changes from v0.1 were preserved, not redesigned. No production APIs, production implementation, dependencies, friend declarations, registry, corpus/oracles, CI, other audit report, release gate, or branch changed. The initial worktree hash comparison found only the listed overlapping files changed; the new helper and tests are additions.

### Fresh staging, versions and exact retry

The old `artifacts/fuzz/toolchain-smoke-20260925/` directory was not reused or overwritten. Every historical evidence file was SHA-256 inventoried and subsequently verified unchanged.

Fresh area: **`artifacts/fuzz/toolchain-smoke-20260925-transport/`**, confirmed ignored. `worktree-before.json` records the starting 84 file hashes; `historical-before.json` records the original failed-run files. `staging-before.json` and `staging-after.json` record all 23 explicitly named copies from the original staging allowlist. No directory was recursively instrumented. Only the fresh `publish/PiratePDF.Fuzz.Targets.dll` was instrumented.

All approved versions were reverified: SDK **10.0.302**, SharpFuzz and local SharpFuzz.CommandLine **2.3.0**, dnlib **4.4.0**, SharpFuzz.Common **2.2.0**; local version/lock/restored-assets records agree. Clang remains **23.1.2**, target `x86_64-pc-windows-msvc`, same recorded commit. The driver remains **1,512,448 bytes**, SHA-256 **17AF5B3F6FF4D2C57B44B9A35C13051B570EB66F0557D00015DF3832709050BF**, rechecked immediately by SmokeCommand before launch. No install, restore, replacement or version change occurred.

Fresh original/copy-before hash: **715B51FD5D8531131BAC4A1A58764AF50592D615C358EE3B22BEA8DFB1E36118**. Fresh instrumented-copy hash: **522025E919037552D4DD3571F06BE75F0B4385A3B6E101F3894E758380836672**. Every normal source output and the other 22 staged files matched its initial hash after instrumentation. The separate Targets normal output was rechecked after the retry and remained unchanged.

Commands actually executed, from the repository root:

```powershell
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~WindowsLibFuzzerArgumentsTests --verbosity minimal
dotnet build tools/PiratePDF.Fuzz.SharpFuzz/PiratePDF.Fuzz.SharpFuzz.csproj -c Release --no-restore --verbosity minimal
dotnet tool run sharpfuzz artifacts/fuzz/toolchain-smoke-20260925-transport/publish/PiratePDF.Fuzz.Targets.dll
dotnet tools/PiratePDF.Fuzz.Runner/bin/Release/net10.0/PiratePDF.Fuzz.Runner.dll smoke C:\Development\PiratePDF filter-asciihex
```

The corrected smoke ran **only after the focused tests passed**. Runner and Targets were rebuilt by that focused Release test command; SharpFuzz's explicit Release build also passed with zero warnings/errors. Instrumentation returned zero.

The exact corrected driver array is retained in `filter-asciihex/arguments.json`. Its first two elements are shown above. The remaining options are unchanged from v0.1: **seed 1049, runs 128, max_total_time 3, timeout 1, max_len 16384, rss_limit_mb 512, print_final_stats 1**, with artifact prefix and seed path under the fresh directory. Existing corpus validation and per-seed hash rechecks admitted the same six ASCIIHex inputs (**39 bytes**). None of these requested execution counts is reported as achieved.

Outer deadline remains **10 seconds**, independent cleanup **5 seconds**, capture **64 KiB per pipe**, managed GC heap configuration **256 MiB**, and mini-dump setting unchanged. No bounds were increased. No controlled probe was launched after the failed retry.

### Second attempt outcome and remaining validation

The driver reached the managed entry point and passed its unchanged argument-count guard. It then emitted a bounded startup failure:

```text
Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
   at PiratePDF.Fuzz.Targets.TargetRegistry.Get(String id)
   at Program.<Main>$(String[] args)
```

The instrumented registry call occurs in the glue **before** `Fuzzer.LibFuzzer.Run`. A coverage-runtime initialization-order interaction is a candidate for the next investigation; this attempt does **not prove that cause** and no registry/exception-policy fix was applied. The failure is in the instrumented tooling startup path; no production PDF/filter input had been processed. The earlier repeated-target-argument usage diagnostic did not recur.

Evidence in `filter-asciihex/result.json` is **604 bytes**, no truncation: driver exit **-1**, **TimedOut=true**, **Cancelled=false**, **CleanupSucceeded=true**, empty stdout, no coverage counters. The lease exists but no `.entered`, `.success` or `.rejected` marker was created. Thus managed glue entry is established; **Fuzzer.LibFuzzer.Run entry, callback execution, useful coverage, and normal expected rejection are not established**. The leased managed process was absent on the post-cleanup identity check.

All three sanitizer-helper warnings recurred unchanged. The observed startup failure does not establish that the warnings caused it, nor that the warnings are harmless. No sanitizer/crash-reporting compatibility claim is made.

| Required validation | v0.2 evidence/status |
|---|---|
| Focused launcher tests | **19 passed, 0 failed, 0 skipped**, before smoke resumption |
| Required project builds | Runner/Targets built by focused Release tests; explicit SharpFuzz Release build passed; no final warnings/errors |
| Instrumentation / original-output preservation | Passed for the same single assembly allowlist |
| Managed glue entry / argument guard | Reached / passed |
| Fuzzer.Run / callback / useful coverage | **Not reached or established** |
| Expected rejection / harness exception probe / abnormal-exit probe | **Not exercised**, stopped before probe phase |
| Timeout / cancellation | Actual startup stall hit the outer ten-second deadline and cleanup succeeded; hanging-callback timeout and explicit cancellation probes **not exercised** |
| Fresh replay / mutation / complete Fuzz.Tests | **Not run**, because the corrected smoke did not reach the required boundary and the explicit stop condition applied |
| Full-suite count | Historical accepted **265/265**, plus 19 newly passing focused cases; **no 284/284 full-suite claim** |
| git diff --check | Passed; new/untracked changed files also whitespace-checked |
| Manual / Administrator execution | None required or requested; fresh privileged fixture validation has not been attempted |

No fuzz-generated corpus input or crash artifact was promoted. Failed-run evidence remains ignored and bounded; persisted corpus and all historical evidence hashes were preserved. No substantive campaign, CI integration, full release gate or later P2 item was started.

### Revised next recommendation

The next recommended B9 slice is **instrumented glue startup/coverage-runtime initialization characterization**, focused on the registry access before `Fuzzer.LibFuzzer.Run`. Preserve both failed attempts, retain pinned tools and the one-assembly allowlist, and require fresh isolated callback/coverage evidence before probes and full regression acceptance. Do not proceed to a substantive campaign from this report. The earlier filesystem, process-tree, managed-heap/RSS, native-sanitizer and crash-reporting limitations remain unchanged. **PP-AUD-036 stays Review/incomplete; POST-P2-002 remains open.**

## Startup characterization and correction (v0.3)

### Scope, authorization and identity

This continuation began with **86 modified/untracked files**, on the unchanged `maintenance/post-BF-1054-repository-review` branch at **93865f639acf96f0b9598c97b04d49cea38e8260**. The updated root AGENTS.md and the user's explicit corrective-iteration authorization permit evidence-supported related fixes; no architecture replacement or unrelated work was inferred. Source characterization covered TargetRegistry fields/Get/initializer, descriptors, collection construction, glue, project references and instrumentation support. PP-AUD-029 through PP-AUD-035 remain governing historical context and unchanged.

New evidence root: **`artifacts/fuzz/initialization-20260925/`**. The directory name records when this continuation started; final validation is dated **2026-09-26**. Both original evidence directories remain byte-identical. `historical-before.json` inventories **72 historical files**, all subsequently hash-verified unchanged. `worktree-before.json` records all 86 starting dirty-file hashes, distinguishing this continuation from prior B9 work. No reset, clean, revert, recreation, commit, push, merge, tag or branch switch occurred.

Toolchain retained without version/dependency changes: **SDK 10.0.302; SharpFuzz 2.3.0; local SharpFuzz.CommandLine 2.3.0; dnlib 4.4.0; SharpFuzz.Common 2.2.0; libfuzzer-dotnet v2025.05.02.0904; LLVM/Clang 23.1.2**. Driver remains **1,512,448 bytes**, SHA-256 **17AF5B3F6FF4D2C57B44B9A35C13051B570EB66F0557D00015DF3832709050BF**. SmokeCommand checks the pinned size/hash before each native launch. Each staging ledger records the actual SharpFuzz/Common/dnlib binary hashes, verified identical across all five native attempts. `tool-identities.json` and `attempt-identities.json` retain these comparisons.

The evidence established a glue initialization-order error, not a dependency defect requiring remediation. Therefore **no dependency was updated**, no new package/reference/friend assembly was added, and no broader release dependency graph changed. Existing dnlib supplied IL inspection; no decompiler or alternative toolchain was installed.

### Direct controls and exact failing operation

Seven separately owned diagnostic areas were created. Each uses the same explicit 23-file support allowlist and fresh `publish` directory; every native-driver attempt below also has its own separately staged area. Normal bin/obj outputs were never instrumented. The only instrumented file remains **PiratePDF.Fuzz.Targets.dll**.

| Diagnostic area | Actual result |
|---|---|
| `control-uninstrumented` | Direct Get("filter-asciihex") without native driver/runtime initialization returned exit 0 and the exact expected descriptor and three registry IDs. |
| `control-instrumented` | Direct lookup reproduced NullReferenceException at TargetRegistry.Get without the native driver. Exit **-1073741819**, no timeout, cleanup succeeded. |
| `il-uninstrumented` | Existing dnlib inspected Get, registry .cctor, coverage fields and TraceWrapper .ctor; exit 0, bounded output. |
| `il-instrumented` | Same narrow IL inspection of the instrumented copy; exit 0. |
| `pointer-control` | Improved diagnostic compared the actual native pointer value: **SharedMem is zero** immediately before reproducing the same direct-lookup failure. This is a negative control, not a failed retry of the correction. |
| `runtime-uninstrumented` | Initialized Fuzzer.LibFuzzer.Run control: exit 0, correct registry, success and rejection outcomes. |
| `runtime-instrumented` | Same initialized control against the instrumented copy: exit 0 and **byte-identical diagnostic output** to the uninstrumented control. |

The descriptor is **filter-asciihex / configuration 1 / 16,384-byte ceiling / SupervisedManagedWorker**. Both positive controls assert the registry remains exactly **filter-asciihex, filter-flate, font-sanitize**, in that order. `registry-comparison.json` records their equality.

The first direct diagnostics used reflection's boxed-field nullness. That was insufficient for a native pointer: a zero pointer is still boxed into a non-null Reflection.Pointer object. This diagnostic limitation was corrected, with original evidence preserved. The improved diagnostic uses a tiny BCL DynamicMethod to load the fixed SharedMem field and compare its value to zero, **without dereferencing it or reporting process addresses**. No unsafe source setting or P/Invoke was added.

Original Get IL is only `ldsfld TargetRegistry::Default`, `ldarg.0`, `callvirt Find`, `ret`. Instrumentation prepends:

```text
IL_0000: ldsfld System.Byte* SharpFuzz.Common.Trace::SharedMem
IL_0005: ldc.i4 47147
IL_000A: ldsfld System.Int32 SharpFuzz.Common.Trace::PrevLocation
IL_000F: xor
IL_0010: add
IL_0011: dup
IL_0012: ldind.u1
...
IL_0021: ldsfld PiratePDF.Fuzz.Targets.TargetRegistry::Default
```

The **IL_0012 injected counter-byte read** uses an address derived from the still-zero SharedMem pointer. It fails before Get reaches the original registry field access. Thus the null value is the coverage-memory base pointer; the evidence does not justify rewriting Default or the compiler-generated collection. Instrumentation also prefixes .cctor with a counter update. The three descriptors, delegate construction, generated read-only-array wrapper and final assignment to Default are otherwise preserved. The BeforeFieldInit attribute is unchanged.

The installed SharpFuzz TraceWrapper constructor assigns its supplied map pointer to `Trace.SharedMem`; LibFuzzer.Run creates that wrapper before invoking the callback. The runtime control confirms the field is zero before Run and nonzero **inside** its callback. This agrees with the [upstream LibFuzzer initialization flow](https://github.com/Metalnem/sharpfuzz/blob/master/src/SharpFuzz/Fuzzer.LibFuzzer.cs); the installed-binary IL and paired controls, rather than a version assumption, establish this repository's cause.

### Smallest correction and regression protection

The glue now constructs an uninstrumented **SharpFuzzTargetSession** containing only the requested ID. Its first invocation resolves `TargetRegistry.Get` from inside the initialized callback and retains that immutable descriptor for subsequent cases. There is no registry implementation change, hard-coded target substitution, static-constructor rewrite, instrumentation exclusion, null/default fallback, swallowed exception, or broadened target exception allowlist.

This preserves registry contents/configuration, ordinal lookup, exact descriptors, byte ceilings, result semantics and cancellation propagation. It changes only the fuzz glue's timing of its first registry lookup to the point where SharpFuzz has initialized the counter map. The normal input guard remains in FuzzTarget.Run; a driver max_len flag is not treated as that API's trust boundary.

The six new session tests prove construction does not resolve an unknown ID before callback invocation, repeated valid/odd-nibble/malformed outcomes match the real registered target, and oversized input/cancellation retain their failures. The session source is linked into Fuzz.Tests, keeping SharpFuzz itself outside ordinary test/release dependencies. The out-of-process instrumented runtime control additionally constructs this exact session before coverage initialization, then asserts the registry and real target success/rejection from the callback. Moving resolution back into session construction would fail that instrumented control; no native driver is needed for the regression.

For the direct initialized control only, SharpFuzz's documented no-driver path reads its first argument as a file. SmokeControlCommand creates the exact **empty, owned `registry-runtime` fixture** in the fresh diagnostic working directory. No external or document-derived path is admitted and no corpus is discovered. It invokes the glue directly through the installed dotnet host, with ArgumentList and no shell, under the existing bounded ChildProcess mechanism. Diagnostic output, failures and leases are retained in that area's mode-named JSON/lease files. Negative controls never become normal malformed-input results.

Seven additional smoke-process tests cover simultaneous pipe saturation/draining with exact 64 KiB retention, abnormal exit preservation, timeout, cancellation after observed child entry, pre-cancellation/no launch and deadline bounds. They assert the observed leased process is gone after timeout/cancellation. These supplement the older neutral-supervisor suite for the separate native-smoke launcher changes introduced in v0.1.

A linked-source nullable-context warning was corrected with an explicit nullable context on the shared session source. Final focused validation and builds have zero warnings/errors. No production defect or additional runtime/toolchain incompatibility was exposed.

### Five native smoke/probe attempts in this continuation

There were **five native driver executions**, all after the positive instrumented control and focused startup/transport validation passed: one normal instrumented smoke and four distinct controlled probes. These are the third normal compatibility-smoke attempt overall plus its probes. Negative direct controls and IL inspection are separate diagnostics, not guided attempts.

Each area has `before.json` / `after.json`, its fresh 23-file publish set, and a mode subdirectory holding the exact argument array, admitted seed copies, identity lease, bounded result and fixed markers. The areas are:

* `smoke-filter-asciihex`
* `smoke-probe-harness`
* `smoke-probe-abnormal`
* `smoke-probe-timeout`
* `smoke-probe-cancel`

Original Targets output / staged-before SHA-256 remains **715B51FD5D8531131BAC4A1A58764AF50592D615C358EE3B22BEA8DFB1E36118**. Every instrumented copy is **522025E919037552D4DD3571F06BE75F0B4385A3B6E101F3894E758380836672**. The original output and every non-allowlisted staged file were verified unchanged after instrumentation. The original Targets output was also checked after validation. No production, framework or NuGet assembly was instrumented.

All runs retained **seed 1049; at most 128 runs; max_total_time 3 seconds; timeout 1 second; max_len 16,384; outer deadline 10 seconds; cleanup budget 5 seconds; 64 KiB capture per pipe; GC heap setting 256 MiB; mini-dump setting 0**. The fuzzer RSS flag remained 512 MiB without a hard-RSS claim. Each mode admitted the same six owned ASCIIHex seeds (39 bytes), with existing whole-corpus validation and per-copy length/SHA-256 checks. No permanent seed/oracle changed.

The preserved `Stage.ps1` under the evidence root supplies the finite 23-name copy allowlist, single literal instrumentation destination and before/after checks. Native commands, after each fresh staging:

```powershell
dotnet tools/PiratePDF.Fuzz.Runner/bin/Release/net10.0/PiratePDF.Fuzz.Runner.dll smoke C:\Development\PiratePDF filter-asciihex
dotnet tools/PiratePDF.Fuzz.Runner/bin/Release/net10.0/PiratePDF.Fuzz.Runner.dll smoke C:\Development\PiratePDF probe-harness
dotnet tools/PiratePDF.Fuzz.Runner/bin/Release/net10.0/PiratePDF.Fuzz.Runner.dll smoke C:\Development\PiratePDF probe-abnormal
dotnet tools/PiratePDF.Fuzz.Runner/bin/Release/net10.0/PiratePDF.Fuzz.Runner.dll smoke C:\Development\PiratePDF probe-timeout
dotnet tools/PiratePDF.Fuzz.Runner/bin/Release/net10.0/PiratePDF.Fuzz.Runner.dll smoke C:\Development\PiratePDF probe-cancel
```

The corresponding staging command is `& ./artifacts/fuzz/initialization-20260925/Stage.ps1 -Area smoke-<mode> -Instrument`. These are retained historical commands, not permission to overwrite/reuse these evidence areas. The tool rejects existing evidence directories/leases. A future attempt must be separately owned.

| Mode | Actual result | Callback / cleanup evidence |
|---|---|---|
| filter-asciihex | **Exit 0, 128 units**, no outer timeout/cancellation | Managed lease; runtime-call, entered, success and rejected one-byte markers; cleanup true; observed child gone. |
| probe-harness | **Driver exit 77**, controlled InvalidOperationException reported as deadly signal | Callback entered; **1 unit**; cleanup true; child gone. No exception translated into target rejection. |
| probe-abnormal | Callback deliberately exits **23**; driver returns **77**, deadly signal | Callback entered; **1 unit**; cleanup true; child gone. |
| probe-timeout | **Driver exit 70**, one-second callback alarm/timeout | Callback entered; **1 unit**; outer TimedOut=false because driver timeout completed first; cleanup true; child gone. |
| probe-cancel | **Cancelled=true**, driver killed (exit -1), outer command maps to 130 | Two-second explicit cancellation request; callback entered; TimedOut=false; cleanup true; child gone. |

The cancellation probe is one observed successful cancellation, not a timing-portability claim about races between every possible host startup and the driver's alarm. The ordinary regression test requests cancellation after its controlled child's lease appears and independently verifies cancellation/cleanup semantics. Historical failed startups and the new process timeout test separately exercise outer timeout enforcement. No bounds were raised to force an outcome.

### Actual coverage and expected rejection

Normal smoke feedback:

```text
INFO: 65536 Extra Counters
#7   INITED cov: 2 ft: 40 corp: 2/5b
#18  REDUCE cov: 2 ft: 40 corp: 2/3b
#54  REDUCE cov: 2 ft: 40 corp: 2/2b
#128 DONE   cov: 2 ft: 40 corp: 2/2b
stat::number_of_executed_units: 128
stat::new_units_added: 2
```

The driver's **cov:2 native edge count is not asserted to be two managed branches**. SharpFuzz's managed feedback uses the extra-counter map, and feature feedback retained **40 features** while the fuzzer reduced its interesting corpus twice. The exact one-assembly instrumentation boundary, injected shared-map IL, initialized instrumented controls and actual callback markers tie this managed feedback to **PiratePDF.Fuzz.Targets.dll**. This establishes useful coverage-guided feedback in the tested adapter assembly; it does not claim coverage instrumentation inside uninstrumented BlackFlag.Engine.Filters.dll or a substantive campaign.

Both success and expected-rejection markers were emitted in the normal **exit-zero** run. The paired direct runtime controls separately exercise the owned literals `>` (success) and `G>` (expected rejection) through the same session/registry entry point and assert their outcomes. Expected rejection therefore remains a normal completed fuzz invocation, not harness/process failure. No production exception allowlist or persisted oracle changed.

All three helper warnings recurred in every native run: `__sanitizer_acquire_crash_state`, `__sanitizer_print_stack_trace`, `__sanitizer_set_death_callback`. They **did not prevent these tested callbacks, feature feedback, controlled exception/abnormal-exit detection, one-second timeout or observed cancellation cleanup**. They do not establish full sanitizer compatibility, complete crash reporting, native coverage or harmlessness in other cases. No warning was suppressed.

### Evidence retention and limitations

`smoke-summary.json` records all five outcomes and post-run child checks. Retained result JSON sizes were **1,482 / 1,946 / 1,687 / 1,662 / 876 bytes** respectively, with no capture truncation. Diagnostic IL output was also captured through the same 64 KiB-per-pipe limit; reports reproduce only the necessary IL/feedback. No input bytes, arbitrary memory addresses or environment contents were added to ordinary runner output.

Controlled harness/abnormal/timeout probes produced **empty** crash/timeout files with SHA-256 **E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855**. They are intentional tooling fixtures, not PiratePDF defects or regression-corpus candidates. The normal smoke retained only its two small fuzzer-generated reductions under its ignored seed copy directory. No input was imported into tests/FuzzCorpus or promoted permanently. Retained failures, support copies and bounded results are deliberate review evidence; prior attempts were not cleaned or replaced.

BCL reparse checks remain conservative snapshots, not an atomic filesystem sandbox, and do not prove concurrent parent-substitution protection or distinguish hard links. PID/start-time checks establish exit of observed cooperating children, not guaranteed termination of inaccessible/detached descendants. Managed heap limits exclude native allocation, mappings and stacks. No hard RSS, complete sandbox, native sanitizer coverage or universal crash-dump suppression claim is made. The direct diagnostic modes are operator-owned test tooling, not a general hostile-file loading interface.

### Fresh validation and closure of the manual blocker

| Check | Actual result |
|---|---|
| Paired initialized registry controls | Both exit 0; identical metadata and success/rejection assertions; instrumented negative pre-runtime control still reproduces the original condition as expected. |
| Focused startup/registry/transport tests | **34 passed**: six new session tests, nine registry tests, all 19 Windows transport tests. |
| Smoke ChildProcess tests | **7 passed**, including bounded simultaneous captures and leased-process cancellation/timeout cleanup. |
| Replay + mutation focused tests | **94 passed**: 38 replay and 56 mutation cases; `validation/replay-mutation.trx`. |
| Direct replay CLI | **15 admitted / 15 executed / 15 oracle matches**, exit 0. |
| Direct mutation CLI | Per-seed 1: **15 admitted / 15 completed / 15 expected rejections**, no failures, exit 0; no mutation-work payload/directory remained. |
| Runner / Targets / SharpFuzz Release builds | All passed, **zero warnings/errors**, with --no-restore. |
| Fresh complete Fuzz.Tests | **296 passed, 1 failed, 0 skipped, 297 total**; `validation/complete.trx`. |
| Test-count reconciliation | Prior 265 + v0.2 transport 19 + session 6 + smoke-process 7 = **297**; no test removed/skipped. |
| Sole failure | SeedLinkEscapeIsRejected could not create its Windows symbolic-link fixture: required privilege not held, before validator invocation. |
| Final permitted-context complete suite | User reported **All 297 tests passed** after the requested Administrator rerun: **297 passed, 0 failed, 0 skipped**, including the real symbolic-link fixture. This is current user-reported evidence, not historical 265/265 or an agent-executed privileged run. |
| git diff --check | Passed, including new/untracked changed-file whitespace checks. |
| Production/integration suites / full release gate | Not required/run: no production or ordinary dependency graph changed; this slice does not close overall B9. |

Validation commands include the focused filters `SharpFuzzTargetSessionTests|TargetRegistryTests|WindowsLibFuzzerArgumentsTests`, `SmokeChildProcessTests`, and `CorpusReplayTests|CorpusMutationTests|DeterministicMutationTests`, each with `dotnet test ... -c Release --no-restore`. The complete run used `--logger 'trx;LogFileName=complete.trx' --results-directory artifacts/fuzz/initialization-20260925/validation`. Direct CLI commands were the existing `replay --repository-root C:\Development\PiratePDF` and `mutate --repository-root C:\Development\PiratePDF --algorithm deterministic-v1 --per-seed 1` against the Release runner.

Root AGENTS.md's manual-validation rule requires a permitted context for the unchanged fixture. The user was asked to run **Administrator PowerShell** and return both final results:

```powershell
Set-Location C:\Development\PiratePDF
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CorpusManifestTests.SeedLinkEscapeIsRejected --verbosity normal
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --verbosity normal
```

No OS policy, test, assertion or security control was weakened. The user's **All 297 tests passed** response closes the fixture-validation blocker and supports smoke-slice acceptance / promotion to v1.0. The complete-suite result includes the fixture; no separate focused-run count is invented from the brief response. The earlier failed attempt remains preserved above and in its TRX evidence.

### Exact files changed by this continuation

1. `tools/PiratePDF.Fuzz.SharpFuzz/Program.cs` — callback-delayed session resolution, diagnostic dispatch and bounded runtime-call marker.
2. `tools/PiratePDF.Fuzz.SharpFuzz/SharpFuzzTargetSession.cs` — new narrow deferred registry adapter.
3. `tools/PiratePDF.Fuzz.SharpFuzz/RegistryDiagnostics.cs` — new bounded controls, fixed pointer-nullness comparison, registry assertions and existing-dnlib IL inspection.
4. `tools/PiratePDF.Fuzz.Runner/Program.cs` — explicit smoke-control dispatch.
5. `tools/PiratePDF.Fuzz.Runner/SmokeControlCommand.cs` — new bounded direct-control launcher under owned ignored diagnostic areas.
6. `tools/PiratePDF.Fuzz.Runner/SmokeCommand.cs` — fresh separate staging area per mode for this continuation.
7. `tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj` — links only the dependency-free session source for ordinary unit tests; no new package/project reference.
8. `tests/PiratePDF.Fuzz.Tests/SharpFuzzTargetSessionTests.cs` — six new session cases.
9. `tests/PiratePDF.Fuzz.Tests/SmokeChildProcessTests.cs` — seven new smoke-launcher cases.
10. `docs/reviews/Post-BF-1054-Batch-B9-Fuzz-Toolchain-Smoke-Completion-Report.md` — this chronological v0.3 record, preserving v0.1/v0.2.

The ignored staging script, copied assemblies, fixtures, ledgers, bounded results and TRX files are evidence artifacts, not additional source/project changes. AGENTS.md, the neutral target implementation, all persisted corpus files, PP-AUD-029 through PP-AUD-035 and the repository-wide audit were preserved.

### Next boundary

**This compatibility-smoke slice is complete.** The recommended separately authorized B9 slice is **bounded failure-artifact retention, deterministic reproduction and promotion contracts**, before the substantive guided campaign. Do not start campaign, general target expansion, CI integration, POST-P2-003, canonical font editing or P1 measurement here. **B9 / POST-P2-002 remain open.**

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-09-25 | Record verified tooling, isolated successful binary instrumentation, failed glue argument transport, bounded startup termination, and mandatory stop before coverage/probe/regression acceptance. |
| 0.2 | 2026-09-25 | Preserve v0.1; correct single-value Windows target-argument transport, pass 19 focused tests, and record one fresh bounded retry blocked by instrumented registry startup failure before callback/coverage. |
| 0.3 | 2026-09-26 | Establish zero coverage-pointer root cause, delay registry lookup into the initialized callback, pass paired controls and five native smoke/probe executions, preserve historical evidence, and record 296/297 fresh tests pending the unchanged Windows symbolic-link fixture's permitted-context rerun. |
| 1.0 | 2026-09-26 | Record the user's successful 297/297 permitted-context complete suite, including the unchanged real symbolic-link fixture; close only the compatibility-smoke slice while preserving all failed attempts and open B9 status. |
