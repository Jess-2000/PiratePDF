# B14 Native AOT, Trimming and Deployment Compatibility

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-044 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054, BF-1055, BF-1056, BF-1058, BF-8000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Disposition

**B14 COMPLETE — DEPLOYMENT EVALUATION VERIFIED**

Retain the framework-dependent, untrimmed JIT developer baseline. Investigate
untrimmed self-contained deployment for the future Windows installer after its
requirements and servicing model are defined. Defer trimming until its concrete
metadata/plugin diagnostics are resolved and qualified. Reject Native AOT for
the current runtime-loaded managed-plugin host architecture. Separate closed-world
tools may be reconsidered after toolchain security and metadata remediation.

The evaluation supplies a deployment recommendation and explicit unsupported
mode evidence. The owner confirmed the B14 release gate passed on November 8.
Documentation and preservation checks pass. POST-P2-007 is closed as a scoped
deployment evaluation, with trim/AOT adoption deferred/rejected as described.
B14-SEC-001 remains an unresolved release-readiness servicing item.

## Repository checkpoint and scope

- Branch `maintenance/post-BF-1054-repository-review` at
  `67f1e622024cb50e8be210d244e4ede2955f6f3c`.
- Entry tree includes the accepted uncommitted B13 documentation changes.
  `entry-status.txt` and `entry-dirty-hashes.json` record exact status and hashes.
- Evidence directory `artifacts/fuzz/b14-deployment-20261108-01/` retains all
  publish scripts, isolated workload sources, command arguments, intermediate
  outputs, logs, repetitions, module hashes and validation results.
- Root instructions, current handoff, PP-GOV-004/005/006, PP-REL-001, Blueprint
  architecture, SDK/Editing/Writing boundaries and actual B13 report were read.
  B13 documentation reconciliation is accepted with owner-confirmed full-gate
  success. Official signed-distribution qualification remains partial.
- B9 remains open and native H2b deferred/disabled/unqualified. B11 retains real
  desktop distribution/startup qualification. B12 remains complete. B15 is next
  in the approved sequence, followed by final B16 before BF-1055. BF-1056 through
  BF-1058 remain provisional pending milestone registry reconciliation.

The host has Windows 11 build 26200 x64, Intel Core i7-14650HX, 24 logical
processors, SDK 10.0.302 and .NET runtime 10.0.10. Experimental orchestration
uses Windows PowerShell 5.1 for initial publishes and runtime measurement.
Warm publish repetitions and outer inventory use PowerShell 7.6.9.
Initial sandbox `dotnet --info` hit a Service Control Manager access denial.
A permitted read returned the installed SDK/runtime/workload inventory.

## Project and executable inventory

All **47 repository projects target net10.0**. No shared PublishAot,
PublishTrimmed, IsAotCompatible or IsTrimmable configuration was found.
Existing deterministic compilation and warnings-as-errors settings are retained.
The isolated experiment project is ignored evidence and excluded from the
repository project/dependency inventory.

| Executable | Actual role | Experiment selection |
|---|---|---|
| PiratePDF.DocGen | Documentation validation/metadata tool | Existing production tool entry point used for all four publish attempts |
| BlackFlag.Engine.Plugins.TestPlugin | Existing B11 reflection/native probe and sample plugin | Consumed through published workload plugin fixtures and compatibility analysis |
| PiratePDF.Fuzz.Tests | Controlled test child entry points | Ordinary unchanged gate coverage |
| PiratePDF.Fuzz.Runner | Managed/fuzz supervision and deferred native refusal paths | Source analysis and ordinary gate coverage, with no native fuzz activation |
| PiratePDF.Fuzz.SharpFuzz | Separately scoped instrumentation worker | Retained configuration, outside deployment performance workloads |
| PiratePDF.NativeProvisioning | Privileged provisioning tool | Inventory only, outside these experiments |

There is no Avalonia desktop entry project. The existing tools and test fixture
cannot supply application installation/startup or signing qualification.

## Compatibility findings with source references

| Boundary | File and symbol | Finding and impact |
|---|---|---|
| Runtime plugin assemblies | src/BlackFlag.Engine.Plugins/Runtime/PluginLoadContext.cs, LoadManagedAssembly line 39 | LoadFromStream retains runtime discovery and collectible managed lifetime. Trim analysis emits IL2026, and Native AOT's dynamic-loading restriction conflicts with this contract |
| Type activation | src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeHost.cs, TryActivateAsync lines 50, 51 and 55 | Manifest-selected Assembly.GetType, GetConstructor and Activator.CreateInstance emit IL2026, IL2075 and IL2072 |
| Native fallback/containment | PluginLoadContext.LoadUnmanagedDll and ValidateResolvedPath | Existing physical containment and unresolved-name fallback remain exercised by the original gate, with native workload packaging checked separately |
| Manifest JSON | src/BlackFlag.Engine.SDK/Plugins/PluginManifestReader.cs, Read | Bounded JsonDocument parsing is explicit. Plugin admission/activation/denial pass through existing public contracts |
| DocGen JSON | tools/PiratePDF.DocGen/Infrastructure/HistoricalMetadataSupplements.cs, Apply line 33 | Reflection-based JsonSerializer.Deserialize emits IL2026 under trimming. Historical evidence validation must remain intact during any future repair |
| PDF serialization | src/BlackFlag.Engine.Writing/PdfObjectSerializer.cs, WriteObject | Explicit type dispatch supports the exercised object graphs. GetType in failure diagnostics does not itself prove dynamic activation |
| Editing and SDK | PdfEditingSession, PdfEditingSaveService and the SDK existing-text interface | Published selected conformance cases validate ownership, rollback, undo/redo, full/incremental save/reopen and stale-target behavior |
| Fonts | PdfFileSystemExternalFontProvider, PdfWindowsExternalFontProvider, existing font resource projectors | Explicit filesystem/sanitizer/provider paths, matching and embedding exercised with installed-font discovery and owned embedded-font fixtures |
| Skia | SkiaPdfPageRenderer and SkiaDctImageCodec | Native runtime dependency retained. Text/path/clipping render and native image initialization are exercised, with separate image regression tests in the full gate |
| Dynamic code generation | Source scan for Reflection.Emit, DynamicMethod and Expression.Compile | No such production implementation found in the scanned engine/tool scope. Runtime plugin loading remains the concrete incompatible dynamic behavior |

Microsoft documents runtime dynamic assembly loading as a Native AOT limitation.
This grounds the architectural recommendation alongside the actual plugin source.
No Native AOT runtime result is inferred from static analysis.
[Native AOT limitations](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).

Avalonia documents Native AOT support with compiled XAML/bindings. Future
qualification must use the actual chosen Avalonia version, controls, resources,
bindings and plugin contracts. The present repository has no Avalonia dependency
graph to publish or qualify.
[Avalonia compilation](https://docs.avaloniaui.net/docs/xaml/compilation),
[Compiled bindings](https://docs.avaloniaui.net/docs/data-binding/compiled-bindings).

## Controlled publish results

Both subjects use Release, win-x64, explicit self-contained selection,
PublishReadyToRun=false and mode-specific PublishTrimmed/PublishAot. Every
subject/mode has its own --artifacts-path intermediate/build root and output
directory. Shared project files and normal obj/bin assets retain their configuration.
Default warnings and NuGet security auditing remain enforced.

The reusable command form and complete scripts are linked from PP-DEV-003.
Each raw publish result records its exact argument array.
[Reproduction instructions](../04-Developer/Deployment-Compatibility.md).

| Subject/mode | Exit | Files | Uncompressed bytes | First publish duration |
|---|---:|---:|---:|---:|
| DocGen framework-dependent | 0 | 5 | 386,254 | 1.212 s |
| DocGen self-contained | 0 | 192 | 80,575,748 | 6.860 s |
| DocGen trimmed self-contained | 1 | 0 | No artifact | 3.279 s |
| DocGen Native AOT | 1 | 0 | No artifact | 3.087 s |
| Engine workload framework-dependent | 0 | 145 | 110,105,903 | 80.213 s |
| Engine workload self-contained | 0 | 332 | 190,295,359 | 71.336 s |
| Engine workload trimmed self-contained | 1 | 0 | No artifact | 89.154 s |
| Engine workload Native AOT | 1 | 0 | No artifact | 10.747 s |

The first engine builds compile a large existing test-reference graph in fresh
isolated directories. The first DocGen self-contained publish includes pack
restore. Two additional warm/incremental publish runs each succeed with the same
file counts/sizes. DocGen fdd takes 1.855/1.823 s and scd 2.198/1.916 s.
Engine fdd takes 4.455/4.092 s and scd 4.456/4.203 s. All exact durations are
retained in the measurement JSON. These are restore/build/publish wall times,
with their cache/build-state differences identified.

The test-dependent engine output includes xUnit/test-platform assemblies and
debug data. Framework-dependent output excludes the machine's shared runtime.
The approximate 80.2 MB self-contained increment is observed for these fixtures.
The absolute output sizes cannot predict a future desktop package's size.

### Trim and AOT failures

DocGen trim fails IL2026 at HistoricalMetadataSupplements.cs:33 under the
existing warning policy. The engine workload additionally exposes actual
production plugin errors IL2026/IL2075/IL2072. Its test-reference graph also
has separate NETSDK1191 RID inference errors and test-probe reflection warnings.
Those fixture errors are distinguished from the production plugin diagnostics.
No trimmed runnable artifact or trim performance result was produced.

Both Native AOT attempts stop at restore with **NU1903** for SDK-selected
Microsoft.DotNet.ILCompiler 10.0.10. Native compilation, linker qualification and
AOT runtime/performance measurements are therefore unperformed. The experiments
preserve this security refusal rather than adjusting warning/audit policy.

Initial orchestration had an argument-concatenation error, retained in the
argument-error logs and corrected before the publish results above. Restricted
NuGet DNS/cache access required a permitted-context retry. Those setup failures
are excluded from the technical compatibility timing/result table.

## Toolchain security finding B14-SEC-001

The compiler restore diagnostic identifies GHSA-p3jp-9v83-76w9 /
CVE-2026-111497. The publisher describes a Brotli decoder memory-safety issue
and identifies .NET 10.0.14 as patched. The installed/measured runtime and the
self-contained runtime files are 10.0.10. Runtime/toolchain servicing therefore
remains a concrete release-readiness remediation item for separate approval
and regression validation. Shared runtime updates do not service runtime bytes
already embedded in self-contained/AOT outputs.
[Publisher advisory](https://github.com/dotnet/runtime/security/advisories/GHSA-p3jp-9v83-76w9).

This evaluation records package/runtime exposure and the audit refusal. It
does not establish a reachable PiratePDF Brotli exploit path. The current source
scan found no direct engine Brotli API call. The recommendation retains a
deployment architecture, with security servicing required before release of
the affected runtime/compiler inputs. Existing package/SDK versions remain intact.

## Benchmark methodology and repeated measurements

The retained EngineWorkload directly invokes all 29 existing B10 conformance
cases and six existing security/plugin cases. It leaves each assertion intact.
The existing fixtures construct small owned PDFs and use real parsing,
rendering, editing, embedding and writing APIs. Font discovery separately uses
the existing default Windows provider and found **497 faces** in each run.
DocGen validates an unchanged private copy of the 225-document B13 checkpoint.

Each supported mode/workload has one warmup and **five fresh-process samples**.
Final measurements start after publish and gate build/test activity completes.
An earlier pilot overlapped compilation and is retained separately, excluded
from the final summary. Modes run sequentially with warm OS file caches. CPU
affinity, machine power state and external background activity are uncontrolled.
This is a local fixture comparison with min/median/max variability.

BOOT is the first managed stdout marker, READY follows the complete workload.
The internal workload timer excludes process launch. The parent polls every
5 ms, records observed peak working set and samples idle working set 250 ms
after READY. It then releases stdin so the child exits. GC is not forced.
The idle point does not establish long-running steady-state behavior.
DocGen exits after validation, so its result is process lifetime with no idle
memory measurement. No UI-ready or desktop-startup latency is measured.

| Workload/mode | First managed marker median | Workload ms min / median / max | Ready median | Peak / idle MiB median |
|---|---:|---:|---:|---:|
| 35 engine checks, fdd | 77.934 ms | 714.555 / 860.101 / 912.887 | 947.083 ms | 57.59 / 57.41 |
| 35 engine checks, scd | 92.557 ms | 818.066 / 839.917 / 872.239 | 935.122 ms | 58.00 / 57.94 |
| Font discovery, fdd | 75.970 ms | 805.191 / 815.150 / 831.182 | 883.650 ms | 216.29 / 125.29 |
| Font discovery, scd | 91.952 ms | 833.552 / 899.364 / 935.580 | 992.769 ms | 216.33 / 125.16 |

DocGen fdd process lifetime is 256.772 / 259.754 / 268.928 ms, with 49.36 MiB
median observed peak working set. Scd is 275.958 / 283.577 / 303.272 ms and
51.70 MiB. All 36 final warmup/measured processes exit 0. Raw measurements,
variability for every metric and publish repetitions are retained.
[Measurement data](B14-Deployment-Measurements.json).

The small differences and overlapping engine ranges do not establish a general
performance advantage for self-contained deployment. No trim/AOT speed or size
benefit is claimed because those modes produced no runnable artifacts.

## Native, plugin, security and preservation validation

Published workload checks preserve the same public entry points and assertions.
Successful cases exercise traditional and stream xrefs, full/incremental saves,
original-byte prefix preservation, untouched graph/paint invariants, embedded
fonts, Form placement, clipping, rollback, undo/redo, save failure/retry and SDK
stale-target behavior. Plugin activation/disposal and privileged capability and
extension denial pass. The unchanged gate separately covers broader native,
image, font, SDK, security and malformed-input regressions.

The engine runs from an unrelated owned working directory containing an invalid
libSkiaSharp.dll decoy. A separate ready-process module inspection confirms
the loaded native DLL comes from the fdd published directory, SHA-256
`c8770c219e0d3cd9bb119fad46f2d00ae1855b11315816e86e909e3332826212`.
The loaded framework runtime is .NET 10.0.10. Both supported publish sets carry
the original native asset. A fresh owned package missing libSkiaSharp fails
with DllNotFoundException wrapped by Skia type initialization and exits 1.
Its diagnostic and result are retained in missing-native.log/.exit.

PP-GOV-006 remains authoritative for future form runtimes. Passive script
assessment and conversion/remediation checks pass without executing scripts.
No source/API/default or H2b gate changed. The B13 report, policy and accepted
checkpoint inventory retain their original bytes.

## Decision matrix

Correctness/preservation and security are evaluated before compatibility,
maintainability, measured benefits and deployment/support complexity.

| Option | Compatibility | Required source/operational work | Benefits and costs | Recommendation |
|---|---|---|---|---|
| Framework-dependent untrimmed JIT | Supported selected public-boundary and tool workloads pass | No production source change for this baseline. Runtime servicing and final desktop qualification remain required | Smallest developer package, requires an appropriate installed runtime and servicing policy | Retain existing baseline and use as starting desktop candidate |
| Self-contained untrimmed JIT | Same selected workloads pass | No production source change for experiments. Final installer selection, runtime patch/rebuild ownership and provenance required | Bundles runtime, around 80.2 MB increment here, no demonstrated general runtime performance win | Investigate for actual Windows packaging requirements |
| Trimmed JIT | Current DocGen and production plugin analysis fail, with additional fixture RID limitations | Future deliberate metadata/source-generated JSON and external plugin preservation contracts, full regression/publish qualification | No runnable result or measured benefit. Additional metadata maintenance burden | Defer |
| Native AOT | Restore blocked by compiler security advisory. Current runtime-managed-plugin contract conflicts with documented model | Patched toolchain and static metadata work would still leave the dynamic plugin architecture incompatible. Changing that architecture exceeds B14 | No compiled artifact, measurements or proven benefit. High plugin/API compatibility cost | Reject for current plugin-enabled host, reconsider independent closed-world tools separately |

## Documentation and validation

PP-DEV-003 centralizes deployment compatibility notes and reproduction commands.
The active handoff/audit, release contract and desktop/maintenance roadmap receive
concise B14 reconciliation. B13's completed report and inventory remain historical
evidence. Shared build defaults, project dependencies and CI are unchanged.

The unchanged release gate was run in restricted and permitted contexts.

| Validation | Result |
|---|---|
| Scoped publishes and runtime checks | Six supported publishes per subject including two warm repetitions, all exit 0. Trim/AOT failures retained separately |
| Published workload measurements | 36 final processes exit 0, all 35 conformance/security/plugin checks pass in each applicable process, 497 font faces per discovery run |
| Native module and missing dependency | Original packaged Skia hash loaded from output despite owned current-directory decoy. Missing-native package exits 1 with DllNotFoundException |
| DocGen `all` | Exit 0, 227 documents, zero errors/warnings/information |
| DocGen `health` | Exit 0, 91.4%, A-, zero errors/warnings |
| DocGen `repair` | Exit 0, zero changes/files/diagnostics, no repairs applied |
| Whitespace | `git diff --check` exits 0, with existing checkout line-ending notices retained |
| Preservation | 29 preexisting dirty files compared, zero unexpected changes. Source/build/dependency/CI diffs empty. All 106 existing accountability/corpus files compared match |
| Gate sandbox | Exit 1 after source-size success and restricted restore failure |
| Gate permitted retry | Exit 1, build zero warnings/errors, 3,428 tests across 21 projects with 3,423 passed, five failed and zero skipped |
| Owner B14 gate | Owner confirmed **the release gate passed** on 2026-11-08. User-reported evidence, no additional count or warning detail supplied |

Exact gate command from the repository root is

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

The five agent failures occur while creating symbolic-link fixtures before
their validators execute. They are CorpusManifestTests.SeedLinkEscapeIsRejected,
FindingStorageTests.ArtifactLinkEscapeIsRejected,
H2bStationAclApplicabilityTests.ReparseSubstitutionIsRejectedBeforeOwnerAdmission,
CampaignTicketAdmissionTests.TicketDirectoryLinkIsRejectedAndForeignBytesPreserved
and CampaignRunnerTests.CampaignWorkspaceLinkIsRejected. The agent gate stops
before documentation. Its passing suites include DocGen 88, Plugins 48, Skia 25
and Services 553. The owner reran the requested unchanged command from the
permitted Administrator context and supplied success separately.

All raw logs and exit files are retained. Earlier B13 owner success remains
separate validation evidence. The gate SHA-256 is unchanged at
`7D555BF893C2C2485969C67E2D95E00FE6CE119000B1E0688BFD7094BCFFFE38`.
Hosted CI was not executed for this uncommitted investigation. No production
source, public API, dependency version, shared build default, CI setting,
signing credential, native H2b gate or immutable B13 report/inventory was changed.

## Remaining dependencies and acceptance

The required B14 outcome is an evidence-based recommendation. Incompatible and
blocked modes are established with retained diagnostics. Recommend accepting
B14 evaluation completion based on the supported published workloads, measured
tradeoffs, actual failure evidence, owner-confirmed gate success and passing
documentation/preservation checks. Toolchain security servicing remains a
separate release-readiness item requiring its own scope.

B11 desktop distribution and POST-P2-006 official signing qualification remain
open. Future Avalonia/installer qualification uses the real application, chosen
dependencies, runtime servicing model and final artifacts. B15 requires its
own task scope, and B16 remains the final maintenance batch before BF-1055.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-11-08 | Record controlled deployment experiments, metadata/plugin incompatibilities, toolchain audit finding, repeated measurements and recommendations. |
