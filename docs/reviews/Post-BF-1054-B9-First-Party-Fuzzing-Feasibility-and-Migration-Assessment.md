# Post-BF-1054 B9 - First-Party Fuzzing Feasibility and Migration Assessment

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-039 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1049, BF-1054 |
| **Last Updated** | 2026-10-10 |
| **Next Review** | 2027-01-10 |

## Decision and scope

**Feasible in stages. A first-party supervised mutation campaign is feasible using existing B9 components. An equivalent first-party coverage-guided replacement is not established.** BCL-only byte mutation is already implemented; repeating it does not supply coverage guidance. The missing work is a versioned exploration schedule, immutable discovery retention before temporary-input cleanup, bounded campaign orchestration, and a trustworthy feedback-to-selection loop.

**The assessment and documentation validation are complete**, including the user's final permitted-context validation below. This slice performs assessment only. No prototype was implemented: a duplicate mutation command would add little evidence, while safe cross-process feedback needs a separately reviewed protocol, probes, scheduler and tests. The supplied prompt explicitly permits a design/tests plan when a safe prototype needs substantial new feedback architecture. Existing SharpFuzz, libFuzzer, LLVM, corpus/oracles, smoke semantics, finding lifecycle, production implementation and public APIs are preserved. **B9 / POST-P2-002 remains OPEN.** No substantive campaign, instrumentation, native-driver execution or dependency retirement occurred.

The recommendation is a separate, disabled-by-default **bounded first-party mutation-orchestration slice**, followed only under separate approval by a tooling-only feedback proof. Neither is evidence of B9 closure or authority to retire the proven toolchain. Completing the already approved substantive B9 campaign through its existing toolchain remains a separate task; this assessment does not replace its acceptance criteria or change the product roadmap.

## Starting state and governing evidence

The initial working tree was clean on `maintenance/post-BF-1054-repository-review`, exact HEAD **f51824dbc031b58497a38a6292ee11568b9ffeda**. Current SDK is **10.0.302**, runtime **10.0.10**, Windows **10.0.26200**, RID **win-x64**. These are live observations, not the historical handoff checkpoint `095ad01`.

Root [AGENTS.md](../../AGENTS.md), [PROJECT-CHARTER.md](../../PROJECT-CHARTER.md), the [accepted Part 1 handoff](../handoffs/PiratePDF-Maintenance-Handoff-Post-BF-1054-Part1.md), and all ten reports below were located and inspected. No required report was missing. The next audit identifier after PP-AUD-038 is PP-AUD-039. The handoff establishes accepted corrected hosted CI; PP-AUD-038's earlier pending-hosted statements remain historical rather than being rewritten here.

| Record | Governing meaning for this assessment |
|---|---|
| [PP-AUD-029](Post-BF-1054-Batch-B9-Bounded-Fuzz-and-Corpus-Completion-Report.md) | Approval-checkpoint proposals and overall B9 acceptance; its original absent-infrastructure inventory is historical. |
| [PP-AUD-030](Post-BF-1054-Batch-B9-Neutral-Targets-Completion-Report.md) | Three neutral managed adapters and bounded registry contracts. |
| [PP-AUD-031](Post-BF-1054-Batch-B9-Neutral-Runner-Completion-Report.md) | Single-case protocol and Windows-only file admission. |
| [PP-AUD-032](Post-BF-1054-Batch-B9-Process-Supervision-Completion-Report.md) | Fresh workers, finite capture/deadline/cleanup and containment limitations. |
| [PP-AUD-033](Post-BF-1054-Batch-B9-Owned-Corpus-and-Manifest-Completion-Report.md) | Owned persisted seeds, independent oracles, schema and provenance. |
| [PP-AUD-034](Post-BF-1054-Batch-B9-Deterministic-Replay-Completion-Report.md) | Deterministic supervised replay. |
| [PP-AUD-035](Post-BF-1054-Batch-B9-Deterministic-Mutation-Completion-Report.md) | Finite deterministic-v1 generation, not a campaign. |
| [PP-AUD-036](Post-BF-1054-Batch-B9-Fuzz-Toolchain-Smoke-Completion-Report.md) | Final accepted callback initialization and genuine instrumented smoke feedback; superseded startup failures remain history. |
| [PP-AUD-037](Post-BF-1054-Batch-B9-Failure-Artifact-Reproduction-and-Promotion-Completion-Report.md) | Immutable discovery, supervised reproduction and review-only promotion. |
| [PP-AUD-038](Post-BF-1054-Batch-B9-CI-Portability-Remediation-Report.md) | Portable-policy versus Windows file/lease tests and unchanged release gate. |

Current source, corresponding tests, manifest, solution/project references, workflow and release gate were inspected rather than treating any proposal as an implemented capability. Local evidence is fresh and ignored at `artifacts/fuzz/first-party-feasibility-20261010-01`; prior B9 evidence directories were not modified. `baseline.json` and `report-inventory.json` record initial identities and SHA-256 hashes.

## Reference material

The authoritative user prompt was available in full: 8,747 bytes, SHA-256 `521F1A0443582D539FF4C300D386E566B19CDA12414019E86E65BF97B21AC5C6`. The reference `fuzz.rs.txt` was also available in full: 18,687 bytes, SHA-256 `D1ADAFE96398C542135D70B69096DE8F2BFC95057CFE897EEE704DA18EEE7C4B`. No Rust code, external corpus or dependency was copied.

Useful ideas are owned seed recipes, reproducible mutation identity, separate crash/hang outcomes and retained minimized candidates. PDF token/numeric/stream mutations belong to appropriate future PDF targets, not these existing filter/font byte contracts. The reference is mutation-based; it contains no causal coverage feedback loop.

Its defaults and error paths are unsuitable to inherit: maximum iterations are effectively unbounded; mutation growth lacks a final target ceiling; launch/poll errors can become success; confirmed hangs need not fail the overall run; stderr is drained after waiting and read without a byte bound; root-only kill/wait errors are ignored; minimization has a separate 300-attempt budget and a shared filename; writes can fail silently or overwrite evidence; noncryptographic identity and concurrent deduplication are weaker than B9. Its shell `ulimit -v` applies only on selected Unix platforms and limits address space, not RSS; Windows/macOS lack that mechanism. Optional external PDF discovery also needs independent rights/provenance review. These are reasons to reuse B9 safety contracts instead of translating the reference.

## Dependency and deployment inventory

| Component | Current identity and boundary | Migration disposition |
|---|---|---|
| Installed .NET SDK/runtime | 10.0.302 / 10.0.10; existing required development/runtime platform. | Reuse, not a fuzz dependency to eliminate. |
| Neutral Targets/Runner | No direct NuGet additions; existing project references. Fuzz.Tests reaches them in the ordinary solution test graph. No product/sample project references a PiratePDF.Fuzz project. | Reuse; development tools are not product entry points. |
| Existing test packages | xUnit 2.9.3, runner 3.1.4, Test SDK 17.14.1, coverlet.collector 6.0.4 in central packages. | Reuse unchanged. Post-hoc test coverage is not a fuzz scheduler. |
| Target project transitive closure | Targets references Services, Parsing and Rendering.Skia, bringing existing SkiaSharp/native-assets 4.150.1 dependencies into its closure. The three current adapters do not call Skia native entry points. | Preserve. A first-party fuzzer would remove optional fuzz-engine dependencies, not all existing product/test dependencies. Reference pruning needs separate evidence/scope. |
| SharpFuzz optional glue | SharpFuzz 2.3.0; lock resolves dnlib 4.4.0 and SharpFuzz.Common 2.2.0. Separate project/central packages, PrivateAssets=all, outside normal solution restore graph. Tests link dependency-free session source, not this package. | Replace-later only after equivalence gates. No shipping/runtime removal is available to claim here. |
| Local instrumenter | SharpFuzz.CommandLine 2.3.0, root repository-local `sharpfuzz` tool manifest. | Replace-later; keep local provisioning and provenance. |
| Native driver | libfuzzer-dotnet v2025.05.02.0904, ignored Windows executable, 1,512,448 bytes; SHA-256 below. | Replace-later; required by the accepted external path. |
| LLVM/Clang | Installed Clang 23.1.2, target x86_64-pc-windows-msvc, `C:\Program Files\LLVM\bin\clang.exe`; accepted path uses the provisioned prebuilt driver. | Development component, not a shipping dependency. No authority to uninstall a system tool that may have other uses. |

Driver SHA-256: `17AF5B3F6FF4D2C57B44B9A35C13051B570EB66F0557D00015DF3832709050BF`. Driver path: `tools/PiratePDF.Fuzz.SharpFuzz/tools/libfuzzer-dotnet-windows.exe`. PP-AUD-029 records SharpFuzz/driver MIT provenance and LLVM compiler-rt Apache-2.0 with LLVM exceptions. The already installed dnlib 4.4.0 and SharpFuzz.Common 2.2.0 nuspec files both specify MIT; dnlib records upstream commit 34267646667b97717067c9f408c2b6333f438331. Preserve the existing approval/notice trail rather than invent a new project license. This assessment did not perform a new binary license audit. The Charter's final project-license decision remains separate.

The optional lock file's SHA-256 is `EDB9F89B9A99F7D1700B4FFFA874E32B9914C8E4D9A9DA4AE2E47DA88B505801`; its package content hashes remain unchanged. The actual net10.0 lock includes the two SharpFuzz transitive packages above and the existing Skia closure, not every framework-specific dependency listed by upstream package metadata. No inference of a missing System.Memory install is warranted from another target-framework group's metadata.

## Reuse / extend / replace-later map

Links below identify current files; named symbols identify the inspected contract. **Extend** means proposed follow-up work, not an edit made here.

| Capability | Exact current seam and evidence | Decision |
|---|---|---|
| Registration/configuration | [TargetRegistry](../../tools/PiratePDF.Fuzz.Targets/TargetRegistry.cs): All/Get/Find; [FuzzTarget.Run](../../tools/PiratePDF.Fuzz.Targets/FuzzTarget.cs). Ordinal owned registry, unknown/duplicate guards, admission before adapter copy. | Reuse unchanged. |
| Production adapters/oracles | [FilterTargets.Decode](../../tools/PiratePDF.Fuzz.Targets/FilterTargets.cs), [FontTargets.Sanitize](../../tools/PiratePDF.Fuzz.Targets/FontTargets.cs), [FuzzResult](../../tools/PiratePDF.Fuzz.Targets/FuzzResult.cs). Real production APIs, narrow failure enums, bounded digest/rejection. | Reuse. Never broaden exceptions to make exploration pass. |
| Owned corpus | [CorpusValidator.ValidateAsync](../../tools/PiratePDF.Fuzz.Runner/CorpusValidator.cs), [CorpusManifestParser](../../tools/PiratePDF.Fuzz.Runner/CorpusManifestParser.cs), [manifest](../../tests/FuzzCorpus/manifest.json), [CorpusSeedRecipes](../../tests/PiratePDF.Fuzz.Tests/CorpusSeedRecipes.cs). Hash/size/schema/inventory validation before execution. | Reuse; generated campaign pool remains separate. |
| Deterministic replay | [CorpusReplay.RunAsync](../../tools/PiratePDF.Fuzz.Runner/CorpusReplay.cs), ReplayCommand. Recheck each seed, fresh worker, fail-fast independent expected category. | Reuse. Existing replay has finite seed/case counts but no built-in aggregate campaign deadline; add one only to new orchestration. |
| Pure mutation | [DeterministicMutation.Generate](../../tools/PiratePDF.Fuzz.Runner/DeterministicMutation.cs). SHA-256 framed identity, seed 1049, seven operators/index 0-6, maximum 32 attempts. No stateful PRNG. | Reuse v1/golden vectors. Extend with separately versioned case/ancestry schedule; do not relabel repeated v1 cases as 10,000 distinct explorations. |
| Mutation execution/lifetime | [CorpusMutation.RunBatchAsync/RunOneAsync](../../tools/PiratePDF.Fuzz.Runner/CorpusMutation.cs), [MutationWorkspace](../../tools/PiratePDF.Fuzz.Runner/MutationWorkspace.cs). Exclusive owned slot, bounded generation, fail-fast worker outcomes. | Extend via separate orchestrator. Current ClearCase occurs before returning failure; it is not automatic discovery retention. |
| Single-case execution | [RunCommand](../../tools/PiratePDF.Fuzz.Runner/RunCommand.cs), [RunResult](../../tools/PiratePDF.Fuzz.Runner/RunResult.cs), [InputFile.ReadAsync](../../tools/PiratePDF.Fuzz.Runner/InputFile.cs). Fixed codes, bounded input, no raw output, Windows-only admission. | Reuse unchanged. |
| Worker/process supervision | [WorkerSupervisor.RunAsync/CreateStartInfo](../../tools/PiratePDF.Fuzz.Runner/WorkerSupervisor.cs), [BoundedCapture](../../tools/PiratePDF.Fuzz.Runner/BoundedCapture.cs), [WorkerProtocol](../../tools/PiratePDF.Fuzz.Runner/WorkerProtocol.cs). Fresh process, no shell, explicit cancellation, 10-second case/5-second cleanup, strict 512-byte record. | Reuse, with campaign token passed to every case. Feedback must not be smuggled into ordinary stdout. |
| Native smoke/lease | [ChildProcess.RunAsync](../../tools/PiratePDF.Fuzz.Runner/ChildProcess.cs), [SmokeCommand.ExecuteAsync](../../tools/PiratePDF.Fuzz.Runner/SmokeCommand.cs), [SmokeChildProcessTests](../../tests/PiratePDF.Fuzz.Tests/SmokeChildProcessTests.cs). 20-second API maximum, fixed 128/3/1/10-second smoke, PID/start-time lease, independent cleanup. | Reuse smoke unchanged. No RunCampaignAsync exists at this HEAD. Any native campaign entry point must be distinct and tested. |
| Immutable discovery/caps | [FindingStore.RetainAsync](../../tools/PiratePDF.Fuzz.Runner/FindingStore.cs), [FindingValidation](../../tools/PiratePDF.Fuzz.Runner/FindingValidation.cs). Validated incoming path/source; SHA-256; create-new payload/metadata; verified publication; strict aggregate caps. | Reuse before clearing any newly discovered failing input. |
| Reproduction/classification | [FindingReproduction.RunAsync/Classify](../../tools/PiratePDF.Fuzz.Runner/FindingReproduction.cs), [FindingContracts](../../tools/PiratePDF.Fuzz.Runner/FindingContracts.cs). Two fresh attempts; class/code agreement; discovery mismatch fails reproduction. | Reuse. Generic managed failure is not proof of an engine defect. |
| Minimization | FindingMinimization.CompareAsync in FindingReproduction.cs: explicit previously retained smaller input, four executions/256 KiB processed maximum, restricted failure class. | Reuse bounded comparison; no automatic search minimizer is implemented. |
| Promotion/oracle ownership | [FindingPromotion.ProposeAsync](../../tools/PiratePDF.Fuzz.Runner/FindingPromotion.cs). Prior failure evidence plus fresh agreement; one review proposal; permanentWritePerformed=false. | Reuse; engine/tooling regression and fixed-success/fixed-rejection decisions remain separate. No auto corpus/oracle/test edits. |
| Actual feedback/IL/native engine | [SharpFuzz Program](../../tools/PiratePDF.Fuzz.SharpFuzz/Program.cs), [SharpFuzzTargetSession](../../tools/PiratePDF.Fuzz.SharpFuzz/SharpFuzzTargetSession.cs), [WindowsLibFuzzerArguments.Create](../../tools/PiratePDF.Fuzz.Runner/WindowsLibFuzzerArguments.cs), PP-AUD-036. Instrumented staged target DLL plus initialized libFuzzer callback. | Replace-later, only after causal feedback and equivalence proof. Transport helper currently permits ASCIIHex/probes, not all three campaign IDs. |
| Provenance/CI/release | Local tool manifest/optional lock, PP-AUD-029/036, [CI workflow](../../.github/workflows/continuous-integration.yml), [release gate](../../Invoke-PiratePDFReleaseGate.ps1). Ubuntu portable tests, Windows full suite, catalog-count verification, bounded failure TRX upload; no native campaign job. | Reuse. Replay contracts run in tests; no dedicated CLI replay CI stage was found. Any experimental stage needs explicit review. |

## Current targets, corpus and enforced limits

All entries are configuration **1**, isolation **SupervisedManagedWorker**. That classification is metadata, not isolation without the supervisor.

| Target | Maximum input | Current fixed policy | Owned persisted corpus |
|---|---:|---|---:|
| filter-asciihex | 16,384 bytes | 32 KiB decoded, ratio 32, four-filter ceiling, 1 KiB buffer; adapter selects exactly one filter/no predictor. | 6 seeds / 39 bytes |
| filter-flate | 16,384 bytes | Same decode policy; exactly one Flate/no predictor. | 6 seeds / 8,873 bytes |
| font-sanitize | 65,536 bytes | Input/table 64 KiB, 32 tables, 256 glyphs, 16 CMap subtables, 64 name records. | 3 seeds / 579 bytes |

Total: **15 owned seeds / 9,491 bytes**, zero external seeds. Manifest schema piratepdf-fuzz-corpus v1; SHA-256 `DEEC87E7DAF3AF4F4430045AED8325AFF3776C705A5D1A864C3875467A350197`. Manifest ceiling 128 KiB, 128 seed entries, 256 enumerated filesystem entries. Independently selected corpus expectations remain authoritative; future successful explorations are not automatically permanent success-oracle cases.

Current worker capture is **64 KiB per stream**, with 4 KiB scratch, strict 512-byte JSON stdout and empty stderr required for protocol success. Dump suppression is configured, not a promise to prevent all OS reporting. Findings retain at most **16 originals / 256 KiB payload per run**, each input also subject to its registered ceiling and the store's 64 KiB maximum; metadata 8 KiB, each evidence record 4 KiB, at most four reproduction records of up to two attempts. These are stricter implemented bounds than some PP-AUD-029 proposals and must not be enlarged incidentally. Proposal and minimization storage retain their existing finite inventory rules.

## A - Minimal first-party mutation campaign design

No external engine is needed to generate bytes or launch existing supervised workers. A separate experimental orchestration command would validate the entire owned corpus once, recheck each selected source, stage a bounded owned input, call WorkerSupervisor, and stop on integrity/supervision/cleanup failure. Expected target rejection is a completed exploratory outcome, never permission to accept an unexpected exception. No in-process target shortcut, persistent worker pool or new process architecture is required.

The necessary changes are finite but real:

1. Add a separate campaign configuration/orchestrator/CLI dispatch in Runner, disabled unless explicitly selected. Preserve mutate/mutate-one v1 and their 32-attempt guard. Validate all requested bounds and corpus identities before any launch; maintain one worker and sequential targets.
2. Reuse mutation operations, but define a new versioned choice identity with monotonically bounded case index and parent input SHA-256. Existing Generate's seven indices cannot express a general exploration schedule; do not modify its replay vectors or simply remove its guard. Record seed 1049, algorithm/version, case index, parent/operation and exact resulting input hash. Deduplicate repeated bytes within a bounded pool without silently counting them as new exploration.
3. Expose only the smallest tooling-internal workspace seam needed by that orchestrator, or a cohesive campaign-owned slot using the same checks. Retain discovery through FindingStore's validated incoming area **before** temporary-input cleanup; verify payload identity, preserve original immutably, then stop normal progression. Retention failure is infrastructure failure requiring attention, not successful cleanup that loses a crash input.
4. Link a 60-second campaign token to each existing 10-second case deadline, so the last worker cannot extend campaign execution by another full case timeout. Keep independent five-second cleanup. Add a positive **75-second** finite outer launcher ceiling, separate from ChildProcess's 20-second smoke contract if that helper is used. It is not implemented here. Validate deadline/count arithmetic before work and reserve time for retention/cleanup; OS scheduling/start/kill calls are not hard real-time guarantees.
5. Enforce count/time/aggregate byte budgets before generation and launch, bounded in-memory seed selection and finite metadata retention. Never accumulate every stdout/stderr capture or case JSON without an aggregate cap. Use existing finding caps unchanged; do not launch more cases once retention/capture capacity is exhausted.

Proposed maximums remain **10,000 executions OR 60 seconds per target**, seed **1049**, per-case **10 seconds**, one worker, sequential targets, actual registered max_len. These are upper bounds, not a requirement to complete 10,000 fresh-process cases in a minute. For an initial implementation, propose an additional **16 MiB processed-input budget per target**, **128 retained pool inputs / 8 MiB aggregate pool bytes**, and **16 MiB total local telemetry** with fixed bounded records. These additional caps are design proposals requiring explicit implementation/tests, not current capabilities or permission to expand FindingStore. Stop with an explicit budget reason; never drop a failure artifact to continue. All discovery records remain within the existing stricter caps.

Tests must first cover exact/over count, time, length, aggregate/pool/log bounds; deterministic identities/selection; missing/changed seeds; malformed invocation; launch failure; timeout versus cancellation; simultaneous oversized pipes; independent root/leased-child cleanup; retention-before-clear and immutable retained bytes after failure; refusal of overwrite/full artifact store; and no permanent corpus/oracle mutation. Preserve portable root.identity versus Windows-only child.lease tests. Measure startup/target/cleanup time separately before any throughput claim.

## B - Feedback mechanisms using built-in .NET facilities

The following is engineering feasibility analysis, not measured replacement performance.

| Option | Reach and transport | Identity, overhead and correctness | Decision |
|---|---|---|---|
| Runtime events/sampling/coverage collection | EventPipe exposes runtime and EventSource tracing. Collection needs a consumer and reliable per-input attribution. Test coverage tools can instrument/report separately. | JIT/startup/GC events and samples are not exact executed branch sets; timing may miss rare branches and create false novelty. Per-case fresh processes amplify startup noise. No BCL API that directly provides the required precise per-input edge map was identified. | Not a proved dependency-free feedback mechanism. Post-hoc reports do not choose inputs. |
| Explicit static tooling probes | First-party Hit(probeId) calls in an explicitly experimental tooling adapter/copy, fixed bitset, bounded versioned child-to-parent channel. No runtime emitter or external package required for those probes. | Stable manually assigned IDs plus checked map/build identity; duplicate IDs and saturation must fail closed. Probe placement requires review and adds source/copy churn; array writes/process startup overhead must be measured. Adapter probes do not reveal internal engine branches. | Smallest plausible proof, only after separate authorization for transport/probe scope. Disabled in ordinary execution; no production probes in this assessment. |
| First-party IL rewrite using BCL metadata/PE builders | A staged copy can theoretically be read/rebuilt without dnlib. Must handle branch targets, exception regions, stack/local signatures, metadata tokens, generics, symbols, strong names and coverage callback linkage. | A correct instrumenter is a substantial engineering project. Stable map identity must include source/build/IL version; branch insertion can alter behavior or verification. No parity, signing/debug-symbol safety or maintainability evidence exists. | Not a narrow prototype; retain SharpFuzz rather than improvise one. |
| Runtime-generated code/profiler ReJIT | Reflection.Emit changes generated code, not arbitrary existing compiled methods. ReJIT's documented profiler interface is native, not a simple BCL replacement API. | Native profiler/transport adds a different architecture and compatibility burden. Dynamic emission is incompatible with Native AOT constraints. | Outside this slice; no native profiler/toolchain added. |

[Microsoft's EventPipe documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe) describes tracing facilities, not a ready-made fuzz branch-feedback API; the adequacy judgment above is an inference from those contracts. [dotnet-coverage](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/dotnet-coverage) is separately installed tooling. [ManagedPEBuilder](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.portableexecutable.managedpebuilder?view=net-10.0) supplies PE construction primitives, not a turnkey semantics-preserving instrumenter. [RequestReJIT](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilerinfo4-requestrejit-method) documents native profiler interfaces; that reference is not a .NET 10 portability guarantee. [Native AOT limitations](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) exclude runtime code generation/dynamic loading and require trimming. Static experimental probes might be compatible but require their own build/execution proof; no production AOT/trimming guarantee follows.

The initial comparison boundary must match **PP-AUD-036's instrumented staged PiratePDF.Fuzz.Targets.dll only**. Ordinary build outputs, engine/dependency/framework assemblies and deterministic replay remain uninstrumented. Instrumenting only adapters gives limited reach under both designs; replacing that baseline does not prove whole-engine coverage. Broader production coverage is a separate scope/authorization decision.

## C - Objective causal proof and safe feedback design

Output-digest diversity, rejection-enum diversity, driver startup, test-line coverage, deterministic replay and mutation counts alone are not coverage-guided proof. The following acceptance experiment is proposed, not executed:

1. Use an owned test-only synthetic branch fixture outside the operational registry. Give two paths identical normal result bytes/category so output novelty cannot masquerade as coverage. Publish fixed probe IDs and expected transitions independently of observed production output.
2. Fix algorithm/map/build/configuration identity, seed 1049, starting corpus, ordering and all budgets. Show a candidate reaching a new edge, its validated per-case edge set, the precise retain decision and parent-selection change, then a later mutation whose parent is that retained candidate and which reaches the next independently known edge.
3. Repeat the complete schedule and compare mutation/input identities, probe sets and decisions exactly. A feedback-disabled control starts from the same corpus and fixed choice schedule; prove it does not make the adaptive retain/selection decision. Include a deliberately wrong/noisy/duplicate map case to prove it cannot falsely satisfy novelty. Require causal evidence, not an invented percentage improvement threshold.
4. Then repeat the proof with the three existing targets and a reviewed adapter-probe inventory. Report which actual adapter branches were reached and which engine branches were unobservable. A synthetic fixture proves the loop only; it does not establish useful production-target guidance or parity.

A future feedback channel must be separate and explicitly versioned: preserve the ordinary worker's one-record/512-byte stdout protocol. Propose a campaign-owned fixed-size map with at most 65,536 probe bits (8 KiB), checked map hash, input hash, configuration and case identity, transferred through a bounded owned side channel. Placement, admission, publication and pipe/file cleanup must be reviewed before implementation; the current WorkerSupervisor does not support it. Missing, stale, oversized, partial, extra or mismatched feedback is a harness failure, never zero coverage/success. Reset the map per case; exclude initialization/protocol/logger probes; trust only a completed validated worker. Do not accept feedback from a crashed worker to hide its finding.

IDs must be deterministic for a reviewed map version. Do not mix feedback between builds or imply that identical offsets survive recompilation. Collision-free explicit IDs are preferable for the proof; a hashed map needs collision evidence and honest limits. Bound novelty state, pool size, duplicate bookkeeping, ancestry length, decision ledger and total bytes. Keep failure classification on the existing oracle/lifecycle, independent from seed-selection novelty. Record exact bytes/hash and path-safe provenance; never dump documents or arbitrary exceptions to the console.

## D/E - Threat, platform and resource truth

Threats include hostile lengths/expansion, managed exceptions versus expected rejection, hangs, abnormal/native exits, pipe backpressure, disk exhaustion, tampered paths/metadata, false coverage, stale case attribution, evidence overwrite and accidental automatic oracle promotion. Operator-controlled binaries/workspace remain trusted configuration, not choices supplied by document bytes.

| Boundary | Windows | Linux portable CI | macOS production platform | What is not guaranteed |
|---|---|---|---|---|
| Target byte adapters | Same managed APIs; current tests exercised them. | Pure adapters/registry/policy tests are portable. | APIs are platform-agnostic; no fresh macOS validation here. | An adapter's isolation enum does not contain execution. |
| File/corpus/campaign admission | Ordinary local regular seekable files only; device/UNC/ADS/reparse checks and bounded read. | InputFile intentionally fails closed before opening potentially blocking Unix special files. | Same non-Windows fail-closed admission; no campaign readiness established. | No atomic filesystem sandbox, parent-substitution defense, hard-link distinction or arbitrary race prevention. |
| Process supervision | BCL fresh child, tree-kill, root wait, cooperating exact PID/start-time lease where applicable. | Portable controlled-root/supervisor tests; Windows lease path remains excluded. | Cleanup not exercised in this assessment. | Root exit/EOF does not prove arbitrary detached/inaccessible descendants gone. No Job Object/container/rlimit exists here. |
| GC heap policy | DOTNET_GCHeapHardLimit=0x10000000 (256 MiB), dump suppression. | Same configured setting for supported workers, not Unix file-campaign readiness. | Same setting would need execution validation. | Not native allocations, stacks, mapped memory or hard total RSS containment. |
| Native engine memory setting | Accepted smoke passes libFuzzer rss_limit_mb=512. | Native driver not part of portable CI. | No accepted macOS native campaign path. | RSS-related driver behavior is not an OS-enforced total-memory sandbox. |
| Input/output/disk/work budgets | Actual target/capture/manifest/finding caps above; proposed campaign caps need tests. | Pure budget/policy tests are portable. | Proposed only until permitted admission/cleanup is implemented. | Byte caps do not alone bound stack depth, CPU, all process memory or OS-call latency. |

[Microsoft defines the GC limit](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector#heap-hard-limit) as GC heap and bookkeeping commit size. [Process.Kill documentation](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill?view=net-10.0) states that root wait/exit does not establish descendant exit and inaccessible descendants can be skipped. Preserve these limitations verbatim in meaning. Whole-process hard containment would require a separately implemented/tested OS mechanism, outside this prompt's dependency/process-architecture boundaries.

Windows/macOS shipping support is not a license to relax Linux/macOS campaign file admission. Keep the Engine platform-agnostic. Portable CI can prove pure mutation, metadata validation and controlled supervision; real corpus/lease/link behavior belongs to permitted Windows execution until a separate safe platform adapter exists. Existing CI does not claim cross-platform native fuzzing.

## F - Equivalence, regression and retirement gates

| Gate | Evidence required before replacement/retirement |
|---|---|
| Target scope/coverage | Same three IDs, configuration/limits, actual real APIs, starting seeds, explicit reviewed coverage reach. Causal proof on real adapters plus known reachable branches; compare semantic reach, not raw incompatible map counters. |
| Fault detection/oracle | Reproduce owned managed-failure, abnormal-exit, timeout, cancellation and cleanup/protocol probes; fail on launch error. Preserve adapter allowlists. No blanket OOM/index/arithmetic/native catch as rejection and no hangs-as-success. |
| False positives/negatives | Fixed successful/rejected controls and repeat results; stale/noisy/empty feedback controls; timeout separation from slow startup. Known controlled faults detected by both paths; a generic failure class is not a root-cause signature. |
| Throughput/resource cost | Measure host/startup, target, feedback, scheduler, retention and cleanup separately with equal caps/hardware/tool identities. Fresh-process overhead may dominate; no performance improvement is claimed here. No persistent-worker switch merely to win a benchmark. |
| Replay/evidence | Exact retained bytes/hash/ancestry, fresh neutral supervised reproduction, immutable originals and ledgers, bounded minimization/proposal, deterministic corpus replay; never regenerate accepted expectations. |
| Platform/cleanup | Preserve portable/Windows catalog counts; Linux policy CI and Windows complete real-link/lease tests. Explicit macOS and platform-admission limitations. Test independent PID/start-time exit; report TerminationFailed, never suppress it. |
| Integrity/provenance | Versioned probe map/algorithm/protocol, trusted binary hashes, strict finite input/record/inventory caps, no arbitrary metadata-selected executable, license/notice and SBOM reconciliation before any removal. |
| Operational maintenance | Reviewed probe placement/version lifecycle, supported build/AOT/trimming constraints, documented failure handling/rollback, ordinary tests/release gate without optional fuzz packages. Instrumenter ownership/cost must be justified. |

PP-AUD-036's final accepted smoke used a **copy of PiratePDF.Fuzz.Targets.dll**, resolving the registry inside the initialized SharpFuzz callback because instrumented static initialization previously accessed an uninitialized coverage pointer. Fuzzer.LibFuzzer.Run communicates with the pinned driver; that smoke completed **128 executions**, reported **65,536 extra counters / 40 features**, managed interesting-input/reduction feedback and two new units. Native cov=2 was not a managed branch count. These are historical accepted smoke observations, not new runs or a substantive three-target campaign.

The three unavailable sanitizer-helper warnings remain evidence, not suppressed diagnostics: __sanitizer_acquire_crash_state, __sanitizer_print_stack_trace and __sanitizer_set_death_callback. Controlled harness failure, abnormal exit, input timeout, outer timeout and cancellation established the tested behavior, not comprehensive sanitizer crash reporting or OS memory isolation. Replacement must reproduce the accepted controlled behavior before any dependency-retirement proposal. Neither that smoke nor PP-AUD-037's lifecycle closes B9.

Rollback for a separately implemented experiment is an explicit disabled experimental command/probe build: ordinary run/replay/mutate and the proven optional toolchain remain available with unchanged vectors, oracles and protocols. Campaign pools are ignored/review-only. Do not delete old evidence, switch the default scheduler, remove package/notice/tool manifests or change permanent data until an independently approved migration passes every gate. This assessment makes no rollback edits.

## Validation and local evidence

Before any tracked edit, the focused Release baseline passed **120/120**, zero failed/skipped, test-reported duration **26 seconds**, measured command wall time **35.490 seconds**. No tests were added or removed. This baseline exercises deterministic generation/mutation, workers, smoke process contracts, registry and deferred session; it does not execute SharpFuzz/native fuzzing. Filter:

```powershell
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~DeterministicMutationTests|FullyQualifiedName~CorpusMutationTests|FullyQualifiedName~WorkerSupervisorTests|FullyQualifiedName~SmokeChildProcessTests|FullyQualifiedName~TargetRegistryTests|FullyQualifiedName~SharpFuzzTargetSessionTests' --logger 'trx;LogFileName=focused.trx' --results-directory artifacts/fuzz/first-party-feasibility-20261010-01/focused --verbosity quiet
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

The initial focused capture attempt failed opening focused.log before its evidence directory existed. Its process was not treated as validated. After creating the directory, the retry passed; it warned that the first attempt had produced the same TRX name and replaced that current-task file. The preserved TRX therefore establishes the retry only. No pre-assessment historical evidence was overwritten. The setup error and retry attribution are retained here; no coverage or performance evidence is inferred from the failed capture.

The first unchanged gate attempt passed source-size validation (1,148 files; existing 943/844-line review candidates) but failed restore because the sandbox could not read the user's existing NuGet.Config. The outside-sandbox retry restored with **all projects up-to-date**, built successfully, and ran all **2,922 solution tests: 2,920 passed / 2 failed / 0 skipped**. The complete fuzz suite in that gate was **476 total: 474 passed / 2 failed / 0 skipped**, test-reported duration **34 seconds**. The other **2,446** tests passed. No dependencies were installed or updated. The two failures were exclusively fixture creation:

- CorpusManifestTests.SeedLinkEscapeIsRejected, CorpusManifestTests.cs:308.
- FindingStorageTests.ArtifactLinkEscapeIsRejected, FindingStorageTests.cs:159.

Both received `System.IO.IOException: A required privilege is not held by the client` in File.CreateSymbolicLink, before validator invocation. This persisted outside the sandbox. No tests were skipped, loosened or reclassified. That agent-executed gate failed, and its later DocGen/whitespace stages were not reached. Historical permitted-context 476/476 and gate passes in the handoff are not substituted for this fresh result. No code correction is justified by this environment failure. The subsequent successful user validation is recorded separately below.

Root AGENTS.md's **Manual commands and elevated execution** rule explicitly says to preserve a denied Windows link fixture and request its unchanged Administrator rerun. The requested commands, as separate lines in **Administrator PowerShell**, were:

```powershell
Set-Location C:\Development\PiratePDF
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~SeedLinkEscapeIsRejected|FullyQualifiedName~ArtifactLinkEscapeIsRejected' --verbosity normal
.\Invoke-PiratePDFReleaseGate.ps1
```

The user's first paste concatenated the commands into one Set-Location invocation and failed parameter binding before tests/gate ran. That is not validation evidence. A corrected single-line command with explicit semicolons and a gate guarded on focused exit 0 was requested:

```powershell
Set-Location -LiteralPath 'C:\Development\PiratePDF'; dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~SeedLinkEscapeIsRejected|FullyQualifiedName~ArtifactLinkEscapeIsRejected' --verbosity normal; if ($LASTEXITCODE -eq 0) { .\Invoke-PiratePDFReleaseGate.ps1 }
```

On 2026-10-10, the user replied **Everything passed** to the corrected command's request for both results. **Both unchanged real-link focused tests and the unmodified authoritative release gate passed, user-reported in the requested permitted context.** This closes the fresh fixture/gate validation blocker. No saved privileged TRX or aggregate gate log was supplied, so no independent privileged count/duration is invented. The unchanged catalog remains 476 fuzz cases plus 2,446 other solution cases, with no additions/removals. The passing link tests confirm their intended real-link escape rejection, not atomic containment, hard-link distinction or concurrent-parent-substitution protection. Both failed agent attempts and the failed paste remain historical evidence. No Windows policy or test was changed.

Evidence files: baseline.json, report-inventory.json, focused/focused.trx, focused.log, focused-result.json, baseline-gate.log, baseline-gate-result.json, baseline-gate-outside-tool-output.log, corpus-identity-check.json, evidence-hashes.json, manual-validation.json and final-evidence-hashes.json. The outside-tool log starts after the initial launch output; all per-project test summaries and both failure traces are retained. manual-validation.json attributes the privileged result to the user's reply, not to agent execution. No campaign counters, throughput benchmark, corpus evolution, new findings or prototype feedback exist in this slice. No campaign-success/cleanup claim is made from baseline test execution.

Independent documentation validation used the existing Release DocGen DLL (no restore):

```powershell
dotnet tools/PiratePDF.DocGen/bin/Release/net10.0/PiratePDF.DocGen.dll all
dotnet tools/PiratePDF.DocGen/bin/Release/net10.0/PiratePDF.DocGen.dll health
dotnet tools/PiratePDF.DocGen/bin/Release/net10.0/PiratePDF.DocGen.dll repair
git diff --check
```

All/health/repair passed: **207 documents**, zero validation errors/warnings/information; health **91.6% / A-**; repair **0 changes / 0 files / 0 diagnostics**, not applied. These are independently executed checks, not a claim that the failed gate reached documentation. `git diff --check` passed; the new assessment is additionally checked as untracked content. Focused TRX SHA-256 is `6B2461A23EFF083FB254656E2358131F250EB891C9DA191502EE25280DA78FF7`. SHA comparisons confirmed all ten historical reports, governance, handoff, release-gate script, local tool manifest, optional lock and corpus manifest unchanged; all 15 persisted seed lengths/hashes match. Release build had **0 warnings / 0 errors**, reported elapsed **17.54 seconds**. Runtime/build overhead has not been benchmarked against a replacement fuzzer.

Exact review files are **four documentation files**:

- docs/reviews/Post-BF-1054-B9-First-Party-Fuzzing-Feasibility-and-Migration-Assessment.md (new PP-AUD-039).
- docs/Documentation-Index.md (generated registration).
- docs/Documentation-Health.md (generated 207-document health inventory).
- docs/ValidationReport.md (generated 207-document validation result).

No hand-authored source/test/project/corpus/CI file changed; no public production API, friend declaration, dependency, tool version or security policy changed. DocGen repair preview changed no file. Ignored local evidence is not a permanent finding/corpus addition.

## Follow-up milestones and stop boundary

1. **Review this completed assessment.** The requested permitted-context validation is now user-confirmed; no equivalence/dependency retirement follows from a documentation review.
2. **Separately authorize finite first-party mutation orchestration.** Implement only the A design and its deterministic/resource/lifecycle tests, preserving smoke and v1. Validate focused/full fuzz tests and the unchanged release gate. Label outcomes mutation exploration, not coverage-guided.
3. **Separately authorize a tooling-only probe/transport proof.** Agree the map, per-case channel and adapter coverage boundary first; prove C causality and repeatability before expanding reach. If that requires production instrumentation, native profiler work or a general IL rewriter, stop for a new decision.
4. **Measure both paths and review equivalence.** Keep approved tooling available; run only independently authorized bounded campaigns with original preservation/lifecycle rules. Retire nothing unless F passes and the user approves exact dependency/notice/CI changes.

No new P2 item, canonical font editing or measurement-only P1 implementation began. Governance, roadmap, audit and historical PP-AUD-029 through PP-AUD-038 remain unchanged. No commit, push, merge, tag, reset, clean, stash, branch switch or history rewrite occurred. This is a completed assessment/design result, not a completed B9 implementation batch.

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-10-10 | Assess live first-party reuse and feedback feasibility; choose design/tests plan instead of duplicate/unsafe prototype; preserve toolchain and open B9 status; record focused baseline and pending real-link/release-gate validation. |
| 1.0 | 2026-10-10 | Incorporate the user's successful unchanged focused-link and authoritative release-gate rerun; complete assessment validation while preserving failed attempts, attribution, dependency boundaries and open overall B9 status. |
