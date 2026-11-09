# B16 Final Repository Cleanup and Managed-Fuzzer Optimization

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-048 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054, BF-1055 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Executive summary and acceptance

B16 implements a measured reduction in managed filter-target setup allocations
and removes three compiler-proven redundant imports. The public target guard,
production decoder, per-call resource budgets, owned input copy, cancellation
and rejection handling remain in the execution path. Two concurrent regression
cases exercise the shared configuration through the public target boundary.

**B16 implementation and required verification are complete. Formal acceptance
is recommended.**
The agent gate builds with zero warnings/errors and passes 3,425 of 3,430 tests.
Five Windows symbolic-link fixtures fail because this process lacks the required
privilege. The owner separately reports that the full release gate passed in
Administrator PowerShell. The existing B15 hosted checkpoint is independently
confirmed.

## Repository baseline and commits

The working branch is maintenance/post-BF-1054-repository-review. Implementation
started from clean commit 755a3711bd8678005eedb7898d67a04ece67011a after the
assignment's plan/checkpoint became available locally. The initial read had
observed b5c7285 and missing plan/checkpoint objects. No source edits occurred
against that earlier observation. The B15 checkpoint
d031cfaaf30dfcf50a0d2ed4a57854fd3295fd37 remains an ancestor. Its hosted run
[42536472888](https://github.com/Jess-2000/PiratePDF/actions/runs/42536472888)
reports success.

Source commit 4f9595d779766de9692479fcac07c2b2bbc29897 contains the implementation,
three import removals, two regression cases and the initial reproducible
benchmark. Follow-up benchmark/documentation commits retain the same production
behavior. Pushes to this maintenance branch were explicitly owner-approved.
The final source commit is a5c057c7ef99aff7596da2000e37a9512a81acd8, adding
the same-process alternating comparison and its invocation option.

## Cleanup inventory and preservation decisions

[The cleanup inventory](B16-Cleanup-Inventory.json) records every one of the
1,258 entry C# files, its hash, size, conditional status, semantic project
contexts and disposition. This includes 724 engine files, 372 test files,
161 tool files and one sample. SDK Roslyn/MSBuildWorkspace loads 46 solution
projects without workspace diagnostics or compiler errors. The compiler and
IDE member/import checks supplement contextual source review. Private-member
discovery finds no absent-identifier candidate. These conservative checks
provide candidate evidence, rather than whole-program reachability proof.

The optional SharpFuzz Program/RegistryDiagnostics and the six-line StationOnly
entry require explicit framework/conditional treatment. The normal solution
compilation covers 1,255 distinct entry files. The optional utility is built
and its registry diagnostic is exercised separately. The StationOnly entry is
retained after inspecting its Compile Include/Remove conditions and admission
guard. Its privileged execution remains deferred.

| Candidate | Disposition and concrete evidence |
|---|---|
| System.Threading imports in CampaignAccountingTests, CampaignRunnerTests and H2bProviderTests | Removed. CS8019 establishes redundancy and unchanged GlobalUsings.cs supplies the same namespace for the entire test project. No alias, conditional or using-static semantics change. |
| FilterTargets per-call decoder, registry, pipeline, filter array and limits | Consolidated into private immutable configuration after profiling. Production pipeline creates a new budget/context/writer for every call. Decoder mutable state remains method-local. |
| Public/protected members, SDK interfaces, overrides and callbacks | Retained as contracts. Baseline/final public/protected metadata exports, including enum constants, match for Runner and Targets. Engine source is unchanged. |
| SharpFuzzTargetSession and RegistryDiagnostics | Retained. Program dispatches the probe modes, delegates enter after runtime initialization and reflected field names are active diagnostic contracts. |
| StationOnly entry, native interop and ownership/refusal helpers | Retained as conditional/native contracts. B16 grants no native qualification. |
| Corpus/replay/mutation reads and hashes | Retained. Initial admission and per-dispatch rechecks observe mutable files at different times and have specific failure mappings. |
| Supervisor identity hashes and worker startup | Retained. Mutable executable identity and fresh worker lifecycle prohibit speculative caching or in-process dispatch shortcuts. |
| FindingStore inventory, pending writes, verification and rename | Retained. These enforce bounded storage, duplicate detection and coherent evidence publication. |
| Temporary SHA arrays in result/mutation code | Experiment discarded. Allocation fell, but timing evidence was inconsistent. Those implementation files match the entry checkpoint. |
| Compiler findings in generated/package or linked sources | Retained. Generated files are owned by their generator. A linked file can require an import in its owning project even when another project reports it redundant. |

No method, type, compatibility shim, test or public API was deleted. The cleanup
removes three ordinary imports and redundant per-invocation setup. The selected
change reuses existing production components and adds no package dependency.

## Profiling method and reproducibility

Measurements use Windows build 26200 x64, Intel Core i7-14650HX with 16 cores and
24 logical processors, .NET SDK 10.0.403/runtime 10.0.14, Release configuration
and all 15 immutable repository-owned seeds. Direct measurements accept only
these owned fixtures. Supervised replay and worker scenarios use the actual
WorkerSupervisor and CorpusReplay boundaries. Finding retention copies the
owned corpus into a new ignored workspace and retains explicit controlled
probes with synthetic fixture provenance.

Each scenario warms one complete workload, then measures seven repetitions.
Two alternating baseline/final process pairs supply 14 samples per variant.
Process-wide allocated-byte counters include asynchronous continuations.
Peak working set is cumulative for the benchmark parent and excludes children.
Raw samples, throughput, min/max variation and peak measurements are retained
in [the measurement record](B16-Managed-Fuzz-Measurements.json).

The final benchmark assembly is identical in both variants. The control uses
the preserved production binaries built before source modification. The final
uses rebuilt production binaries. Both full process pairs produce identical
aggregate mutation digests and execution checksums. Each includes 105 exact
mutations across the 15 seeds, complete 15-case supervised replay and every
declared measured operation. Golden-vector and boundary tests provide further
independent deterministic checks.

From regular PowerShell in C:\Development\PiratePDF, use a new output directory.

```powershell
powershell.exe -NoProfile -File .\tools\Benchmarks\Invoke-ManagedFuzzBenchmark.ps1 -OutputDirectory .\artifacts\fuzz\b16-repeat
```

Build this same harness against the entry checkpoint in an isolated source
copy to reproduce the baseline. The following commands export the committed
entry source and copy only the measurement harness into it. Choose fresh paths.

```powershell
git archive --format=zip --output=artifacts/fuzz/b16-base.zip 755a3711bd8678005eedb7898d67a04ece67011a
Expand-Archive -LiteralPath .\artifacts\fuzz\b16-base.zip -DestinationPath .\artifacts\fuzz\b16-base
Copy-Item -Path .\tools\Benchmarks\*.cs,.\tools\Benchmarks\*.ps1 -Destination .\artifacts\fuzz\b16-base\tools\Benchmarks
powershell.exe -NoProfile -File .\artifacts\fuzz\b16-base\tools\Benchmarks\Invoke-ManagedFuzzBenchmark.ps1 -OutputDirectory .\artifacts\fuzz\b16-base\artifacts\fuzz\baseline
powershell.exe -NoProfile -File .\tools\Benchmarks\Invoke-ManagedFuzzBenchmark.ps1 -OutputDirectory .\artifacts\fuzz\b16-interleaved-repeat -BaselineBinaries .\artifacts\fuzz\b16-base\artifacts\fuzz\baseline\bin
```

Retained local control binaries are under
artifacts/fuzz/b16-final-20261108-01/control-final/bin. Historical comparison
binaries are restricted to this development measurement scope.

## Before and after measurements

The following values are medians over 14 full-process samples. Allocations are
total managed bytes per measured workload. Timings on this shared host vary
even in unchanged control paths and support descriptive workload comparison.

| Workload | Cases per sample | Before ms | Final ms | Before allocated bytes | Final allocated bytes |
|---|---:|---:|---:|---:|---:|
| ASCIIHex target | 5,000 | 18.911 | 16.787 | 5,275,376 | 3,235,376 |
| Flate target | 5,000 | 105.595 | 104.347 | 51,517,760 | 49,477,760 |
| Font sanitizer | 5,000 | 60.847 | 68.987 | 753,656,528 | 753,656,528 |
| Result digest | 100,000 | 18.981 | 22.382 | 24,001,336 | 24,001,336 |
| 64-byte mutation | 14,000 | 9.984 | 10.416 | 10,833,336 | 10,833,336 |
| 16-KiB mutation | 14,000 | 137.131 | 143.717 | 207,473,336 | 207,473,336 |
| Corpus validation | 10 | 59.024 | 66.444 | 6,226,920 | 6,226,920 |
| Supervised worker | 5 | 507.348 | 546.029 | 1,085,024 | 1,084,924 |
| Supervised replay | 1 complete corpus | 1,530.331 | 1,732.146 | 4,298,548 | 4,298,700 |
| Controlled finding retention | 5 | 72.908 | 80.173 | 3,553,768 | 3,555,568 |

Each filter case saves exactly 408 bytes in both full-process pairs. This is a
38.67% ASCIIHex and 3.96% Flate allocation reduction for this corpus mix.
The selected optimization retains the complete input copy and production
decode/budget path. It adds bounded process-lifetime configuration for exactly
two fixed filter registrations.

A supplemental same-process comparison resolves filter-specific timing noise.
Separate load contexts hold baseline/final assemblies. Inputs and observations
match before timing. After 5,000 warmup calls per variant, 16 pairs of 10,000
calls alternate execution order, using synchronous allocation counters.
ASCIIHex median falls from 34.992 to 23.457 ms and wins 13 of 16 pairs.
Flate median falls from 167.533 to 161.991 ms and wins 11 of 16 pairs.
Ranges overlap and the stronger repeatable claim is allocation reduction.
Whole-replay speedup and peak-memory improvement are unestablished.

```powershell
powershell.exe -NoProfile -File .\tools\Benchmarks\Invoke-ManagedFuzzBenchmark.ps1 -OutputDirectory .\artifacts\fuzz\b16-filter-repeat -BaselineBinaries .\artifacts\fuzz\b16-final-20261108-01\control-final\bin
```

## Regression, security and release verification

The initial focused set passes 92 tests. The expanded affected set passes 149
of 150, with its sole failure at CampaignWorkspaceLinkIsRejected fixture
creation. The unchanged authoritative gate passes source-size validation,
restore and build with zero warnings/errors. It discovers 3,430 tests, including
the two new concurrency cases, with 3,425 passes, five privilege failures and
zero skips. Counts include every interleaved project summary match.

The five failed fixture creations are SeedLinkEscapeIsRejected,
ArtifactLinkEscapeIsRejected, ReparseSubstitutionIsRejectedBeforeOwnerAdmission,
TicketDirectoryLinkIsRejectedAndForeignBytesPreserved and
CampaignWorkspaceLinkIsRejected. The owner subsequently reports
“The full release gate passed” after the requested Administrator PowerShell
invocation. That elevated result is owner-reported without an agent-observed
execution log. Hosted run
[42561085262](https://github.com/Jess-2000/PiratePDF/actions/runs/42561085262)
passes both Windows Build and Test and Optional Linux Compatibility on source
commit 4f9595d. Run
[42562113984](https://github.com/Jess-2000/PiratePDF/actions/runs/42562113984)
also passes both jobs on final source commit a5c057c. Windows dependency
regeneration, all-test/category execution and repository-cleanliness steps pass.
Linux portable-test coverage, documentation and cleanliness steps also pass.
The optional Linux result retains its compatibility-only scope.

The added concurrency cases interleave successful decoding, malformed rejection
and cancellation across 64 parallel iterations for each filter and verify
input preservation. Existing exact ceilings, expansion ratio, decoding errors,
mutation vectors, ownership and callback-order tests remain effective.
No production native H2b execution is enabled.

Fresh currency verification passes six current development groups and 48
audited project scopes. Locked SharpFuzz restore/publish and registry-direct
pass. Existing SBOM/legal/hash/schema/private-path validation passes with zero
unresolved package review issues. Publisher metadata still selects SDK
10.0.403/runtime 10.0.14. Compiler-pack verification reuses the unchanged,
hash-bound B14B restore evidence for 10.0.14. B16 performs no new AOT compile.
The original vendor SBOM bundle remains blocked and the approved source-serviced
producer is verified against its package/binary origins.

DocGen validates 232 documents with zero findings, health is 91.5% A-, and
repair preview proposes zero changes/files/diagnostics. The source-size gate
retains its existing review and hard limits. Historical B9 registrations, corpus bytes,
dependencies, SBOMs and B12/B13/B14/B14A/B14B completion evidence are preserved.

The local checkout initially contained CRLF copies of six files whose existing
.gitattributes and registrations require LF. DocGen correctly refused those
bytes. After proving that LF normalization matches every existing registered
SHA-256 exactly, the checkout copies were restored to those approved bytes.
Their Git blob identities and registrations remain unchanged. This local
checkout correction is included explicitly in the preservation evidence.

Preservation checks cover all 1,752 entry files with zero unexpected changes.
The six checkout repairs are explicitly accounted for. All 123 protected
corpus/dependency/historical-registration/completion-evidence files checked
retain their entry hashes. The authoritative gate retains SHA-256
7D555BF893C2C2485969C67E2D95E00FE6CE119000B1E0688BFD7094BCFFFE38.
Exact local manifests, logs, compiler inventories and API exports are retained
under artifacts/fuzz/b16-final-20261108-01. The tracked cleanup and measurement
records carry the durable review inventory and raw performance samples.

## Remaining scope and recommendation

The owner-reported full gate, observed final-source Windows/Linux CI,
documentation/currency checks, deterministic observations, unchanged API
surface and preservation evidence support formal B16 acceptance. Local
privilege failures remain in the evidence alongside the passing permitted
executions. Final documentation is committed separately from the source
checkpoint and receives its own hosted cleanliness/validation run.

B9 coverage-guided campaign acceptance remains open. Native H2b remains
deferred, disabled, incomplete and unqualified. B11 actual desktop
distribution/startup and official signing/installer qualification remain
separate product/release work. BF-1055 requires its own task scope.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-11-08 | Record semantic cleanup, measured filter configuration reuse, reproducible benchmarks and B16 validation/acceptance evidence. |
