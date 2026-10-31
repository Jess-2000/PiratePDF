# PiratePDF Maintenance Handoff

| Field | Value |
|---|---|
| **Document ID** | PP-HANDOFF-001 |
| **Category** | Engineering Handoff |
| **Version** | 1.9 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1054 |
| **Last Updated** | 2026-10-31 |
| **Next Review** | 2027-01-31 |


**Checkpoint:** Post-BF-1054 Maintenance Part 1
**Checkpoint date:** 2026-10-05
**Integrated checkpoint commit:** `095ad01` —
`Merge pull request #21 from Jess-2000/develop`
**Main:** `095ad01`
**Origin/main:** `095ad01`
**Continued maintenance branch:**
`maintenance/post-BF-1054-repository-review`
**Working tree at handoff:** clean
**Remote maintenance branch at handoff:** `e27d386` (local maintenance
branch is seven commits ahead)
**Recovery snapshot:**
`PiratePDF-BF-Post-1054-Maintenance-Part1-Snapshot.zip`
**CI state:** corrected cross-platform CI passed; `main` CI after the
GitHub pull-request merge is green.

## Current dependency provenance continuation — 2026-10-31

The owner's B12 / POST-P2-005 assignment authorizes dependency accountability,
not modernization or desktop implementation. [PP-DEP-001](../dependencies/README.md)
provides resolved inventory, native-origin hashes, preserved package notices,
three schema-validated CycloneDX SBOMs and regeneration/review instructions.
The pinned CycloneDX 6.3.1 development tool was explicitly approved. Existing
production package versions and native assets are unchanged. The union covers
21 resolved package/version pairs across 47 projects; four are used by engine
projects. [PP-DEP-002](../dependencies/License-Provenance-Resolution.md) now
reconciles all 13 initial package findings and 26 unique generator identities
with 33 original legal texts, 45 manifest/historical evidence files and exact
archive/hash records. All 117 bundled DLLs match their archive origins.
The owner confirmed non-revenue-generating use and accepted the documented
OSMFEULA terms for the three json-everything components. Reassess changed use
or terms; this is not a general legal-compliance opinion.

[PP-AUD-042](../reviews/Post-BF-1054-Batch-B12-Dependency-Provenance-and-Sbom-Completion-Report.md)
records **B12 COMPLETE — DEPENDENCY PROVENANCE AND SBOM VERIFIED**;
POST-P2-005 is closed. All seven negative mutations are
rejected, including the original five. Repeated generation compares 86 evidence
files with zero differences; the three original SBOMs and native inventory
remain byte-identical. Plugin/Skia/DocGen suites pass 48/48, 25/25 and 88/88.
The remediation agent gate builds without warnings/errors but has 3,423 passes
and the same five existing symbolic-link privilege failures out of 3,428,
zero skips. The owner's earlier October 31 full-gate success remains
user-reported validation of the preceding B12 checkpoint, not a new agent pass.
The owner also confirmed **the full remediation release gate passed** after
the corrected single-line command. This is distinct user-reported evidence;
current gate validation is no longer pending. Documentation validates all
222 documents with zero findings; repair preview proposes zero changes.
No unresolved current-use B12 licensing/provenance blocker remains. B13 is
next in the approved sequence but needs its own task scope before implementation.
Prior owner-reported B11 gate successes remain separate historical evidence.

The [approved desktop sequence](../02-Roadmaps/Windows-Desktop-Development-Sequence.md)
retains B16 as final maintenance after POST-P2-008 **before BF-1055**, followed
by provisional BF-1056 Windows Desktop Foundation, BF-1057 Desktop Editing
Integration and BF-1058 Windows Packaging and Installer, then v1 release
preparation. Numbers need registry reconciliation; existing BF-8000 is not
renumbered. BF-1058 must reconcile B11's actual application-distribution gap.
B11 stays partial; B9 stays open and native H2b stays deferred/disabled.
No B13-B16 or BF-1055-BF-1058 implementation is included in B12.

## Windows qualification amendment — 2026-10-31

[PP-GOV-005](../05-Governance/Platform-Support-and-Native-Lifetime.md) records
the owner's B11-D1 through D4 decisions: Windows 10/11 x64 is the sole official
v1 target; Linux/macOS are deferred to community-led support. Managed plugin
unload/disposal remains required, but native modules may remain process-resident;
replacement may require restarting the application. Deterministic native-image
unloading is not a v1 requirement and no manual native-handle release is added.

The owner confirmed **there is no desktop entry project yet**. Inventory of
47 projects finds only engine libraries and test/tool executables; no application
distribution can be published or started. Current disposition is **B11 PARTIAL —
WINDOWS QUALIFICATION REMAINS** (PP-AUD-041 v1.3). Existing plugin, Skia and
Services suites pass 48/48, 25/25 and 553/553. These are engine/fixture results,
not application startup or clean-machine distribution qualification.

Windows CI retains unfiltered security/tests and adds blocking documentation
checks; Linux becomes a workflow-level optional compatibility signal. A real
distribution CI step remains absent because its application target does not
exist. Hosted workflow/remote branch protection is not validated locally.
The prior owner-reported full-gate success and all failed agent runs remain
distinct evidence. The October 31 agent gate builds without warnings/errors
but has 3,423 passes/five existing symbolic-link privilege failures out of
3,428, zero skips. DocGen validates 218 documents with no findings; repair
preview proposes zero changes. The changed CI PowerShell step and its repair
guard were validated locally; hosted execution remains unverified.
The owner subsequently confirmed **the full release gate passed** on
2026-10-31. This clears the current gate result as user-reported evidence;
no rerun log/TRX, exact count or execution context was supplied. Preserve
the failed agent run separately. Actual application distribution/startup
qualification remains open because no desktop project exists.
Native H2b remains deferred, disabled and unqualified; B9
remains open. B16 remains reserved as the final batch after POST-P2-008.
No desktop application, B12 or B16 implementation is authorized by this update.

## Earlier continuation amendment — 2026-10-26 (historical)

The checkpoint/CI statements above and sections 2-3 describe the original
Part 1 handoff, not a claim that the current tree is clean or at `095ad01`.
On amendment, the maintenance branch is at
`f51824dbc031b58497a38a6292ee11568b9ffeda`, with intentionally dirty approved
B9/H2b work. Verify actual state on every continuation; preserve it.

**B10 continuation update, 2026-10-27:** [PP-AUD-040](../reviews/Post-BF-1054-Batch-B10-Reopen-Render-Conformance-Completion-Report.md)
closes POST-P2-003 with 29 new reopen/render cases and four demonstrated scoped
corrections. The agent's gate had 3,406 passes/five existing denied link fixtures
out of 3,411, zero skips. The user subsequently reports all tests passed and
explicitly confirms the entire unchanged release gate passed in the requested
permitted context. This is user-reported evidence; earlier failed runs remain.
POST-P2-004 / B11 is now active under the owner's October 27 assignment;
cross-platform acceptance remains pending. B9/native H2b dispositions
and all remaining acceptance/privilege boundaries are unchanged.

**B11 update, 2026-10-27:** [PP-AUD-041](../reviews/Post-BF-1054-Batch-B11-Packaged-Native-Resolution-Completion-Report.md)
records **B11 PARTIAL — REMEDIATION REQUIRED**. Owner-approved physical
package containment preserves unresolved-name runtime fallback. Windows
plugin tests pass 48/48 (17 new cases), Skia tests 25/25 and the published
fixture 17/17. The final unchanged gate builds without warnings/errors but
has 3,423 passes and five existing denied symbolic-link fixture creations
out of 3,428, zero skips. DocGen, health, zero-change repair preview and
whitespace checks pass separately. The owner subsequently confirmed the
**full release gate passed with no failures or warnings** on 2026-10-27.
This is user-reported success; no rerun log/TRX or exact count was supplied.
The Windows gate result is no longer pending. Linux/macOS execution,
application-package coverage and native-image lifetime
qualification remain outstanding. POST-P2-004 is not closed. Preserve the
earlier six-failure gate and passing timeout followup as distinct evidence.

The owner's approved [PP-GOV-004](../05-Governance/H2b-Deferral-and-Maintenance-Sequencing.md)
is the current sequencing authority:

- **Native H2b:** deferred to an unspecified future PiratePDF update;
  incomplete, unqualified, blocked and disabled. Preserve code, evidence,
  original security requirements and native refusal. No unsandboxed fallback
  for untrusted PDFs is authorized.
- **Managed fuzzing:** partially verified. The three managed targets have
  passing direct tests; bounded deterministic validation is available for
  trusted repository-owned fixtures. End-to-end campaign acceptance and
  substantive coverage-guided completion remain outstanding; POST-P2-002 is open.
- **Next sequenced maintenance:** POST-P2-004 through POST-P2-008; B10 /
  POST-P2-003 is complete per the October 27 update. Native H2b qualification and B9 closure do not gate
  their progress. Existing acceptance and release requirements still apply.
- **Validation scopes:** R1 recorded 909/914 fuzz passes and five denied
  link fixtures; its full gate had 3,377 passes/five failures/zero skips.
  The later filtered run had 239 passes/two failures/zero skips out of 241.
  These are separate scopes. Exact names and evidence provenance are in
  PP-GOV-004. No blocked assertion is passed; the R1 gate is not green.
- **BF-1055:** separate product development subject to existing roadmap,
  planning and acceptance requirements; not started by this amendment.

## 1. Purpose

This document is the authoritative continuation and recovery handoff for
PiratePDF / BlackFlag Engine after BF-1054 and Post-BF-1054 Maintenance
Part 1. A new maintenance or Codex session should read this document
together with root `AGENTS.md`, `PROJECT-CHARTER.md`, and the review
documents relevant to the active batch.

The recovery snapshot named above represents the integrated, CI-green
Part 1 checkpoint. Treat it as immutable recovery/reference evidence; do
not modify the snapshot in place.

## 2. Repository state at original handoff

The accepted repository checkpoint is commit `095ad01`.

At handoff:

- `main`, `origin/main`, and the local
    `maintenance/post-BF-1054-repository-review` branch point to
    `095ad01`.
- The maintenance working tree is clean.
- `develop` / `origin/develop` point to `17c8bc3`,
    `Fix portable fuzz process cleanup validation`.
- The maintenance branch's remote still points to `e27d386`, the
    earlier Part 1 maintenance checkpoint.
- Do not push, merge, tag, reset, clean, delete branches, or rewrite
    history unless the user explicitly authorizes it.

The local maintenance branch is intentionally the continuation branch
for the next maintenance work. There is no need to create another merge
solely to synchronize it with `main`.

## 3. Completed Part 1 work

The following is complete at this checkpoint:

- BF-1054 Existing Text Editing & Typography completed and integrated.
- All 15 post-BF-1054 P0 audit findings closed.
- Repository-wide dead-code and unused-import cleanup completed
    without removing public/protected API surface.
- B8 / POST-P2-001 font-catalog responsiveness and aggregate-work
    hardening completed.
- B9 infrastructure/review work PP-AUD-029 through PP-AUD-037
    completed.
- PP-AUD-038 CI Portability Remediation completed and accepted.
- Corrected hosted Linux and Windows CI passed.
- `develop` was merged into `main` manually through GitHub pull
    request #21.
- CI on the resulting `main` branch passed.
- The Post-BF-1054 Maintenance Part 1 recovery snapshot was created
    after that green integrated checkpoint.

## 4. B9 / POST-P2-002 status

**B9 / POST-P2-002 remains OPEN.**

Current status is the dated amendment above: managed verification remains
partial and native H2b is deferred. The following records the accepted
Part 1 infrastructure history. It does not impose B9 closure before B10.

The infrastructure needed to conduct bounded fuzzing is established, but
the substantive bounded coverage-guided campaign has not begun. Do not
describe B9 as complete merely because the toolchain, deterministic
replay/mutation, finding lifecycle, smoke validation, and CI portability
work are complete.

Completed B9 sequence:

- PP-AUD-029 — bounded fuzz/corpus architecture and checkpoint.
- PP-AUD-030 — neutral target registry.
- PP-AUD-031 — neutral single-case runner.
- PP-AUD-032 — process supervision.
- PP-AUD-033 — owned corpus and manifest.
- PP-AUD-034 — deterministic supervised replay.
- PP-AUD-035 — deterministic mutation.
- PP-AUD-036 — fuzz toolchain smoke.
- PP-AUD-037 — failure artifact reproduction and promotion
    lifecycle.
- PP-AUD-038 — CI portability remediation.

**Original next B9 activity at Part 1:** substantive bounded coverage-guided
campaign. This is not the current next maintenance action; resumption needs
separately scoped authorization and unchanged security requirements.
B10 is next under PP-GOV-004.

## 5. Accepted fuzz and CI baseline

At PP-AUD-038 v1.1:

- Fuzz catalog: **476 cases / 183 methods**.
- Portable route: **285 cases / 106 methods**.
- Windows route: **191 cases / 77 methods**.
- Final permitted-context Windows fuzz suite: **476 passed, 0 failed,
    0 skipped**.
- Both real symbolic-link fixtures passed in the permitted Windows
    context.
- The unmodified authoritative release gate passed on the corrected
    tree.
- Corrected hosted CI subsequently passed.
- CI on `main` after pull-request integration also passed.

PP-AUD-038 preserved the intended portability model: portable
managed-process contracts remain portable, while the bounded native
cooperating-child lease-reader path remains explicit Windows integration
coverage. `InputFile`'s fail-closed non-Windows admission policy was not
weakened.

## 6. B9 toolchain and target baseline

Ordinary .NET 10 projects:

- `tools/PiratePDF.Fuzz.Targets`
- `tools/PiratePDF.Fuzz.Runner`
- `tests/PiratePDF.Fuzz.Tests`

Optional instrumentation/toolchain project:

- `tools/PiratePDF.Fuzz.SharpFuzz`

Approved compatibility-smoke toolchain at this checkpoint:

- SharpFuzz 2.3.0
- SharpFuzz.CommandLine 2.3.0
- dnlib 4.4.0
- SharpFuzz.Common 2.2.0
- libfuzzer-dotnet Windows driver v2025.05.02.0904
- LLVM clang 23.1.2

The owned corpus contains 15 persisted PiratePDF-owned seeds. PP-AUD-038
did not change corpus bytes, manifest, or oracles.

Existing neutral operational target IDs:

- `filter-asciihex`
- `filter-flate`
- `font-sanitize`

Begin the substantive campaign with these existing targets. Do not
expand target scope and campaign scope simultaneously without evidence
and explicit batch justification.

## 7. Finding lifecycle

PP-AUD-037 established the bounded finding lifecycle:

1. Retain immutable discovery evidence.
2. Reproduce through the supervised worker path.
3. Classify using the closed outcome set.
4. Optionally compare a smaller retained candidate.
5. Stage a review proposal.
6. Never automatically write a permanent regression or corpus seed.

Promotion requires validated evidence and fresh agreement. Controlled
probe failures cannot become engine regressions. Tooling failures belong
in tooling regression coverage. Permanent corpus changes remain
review-controlled.

The lifecycle must remain bounded and must not allow an artifact to
choose executable, shell, target, destination, or policy.

## 8. CI portability model

CI intentionally distinguishes portable contracts from Windows-specific
file/path/native integration rather than making the fuzz suite
Windows-only.

The accepted model includes:

- Ubuntu portable build/test/validation with `Platform!=Windows`.
- Windows full-solution build/test without a platform filter.
- Fuzz catalog/coverage verification that rejects missing, skipped,
    duplicated, extra, unknown, or misrouted cases.
- DocGen owned by the portable job.
- Generated-file, unexpected-untracked-file, and whitespace checks.
- Failure-only bounded TRX retention.

Do not weaken routing or move a failing portable test to Windows merely
to make CI green. Characterize a platform failure first.

## 9. Security and engineering boundaries

Treat PDFs, embedded content, plugin input, paths, URIs, external
metadata, corpus/finding artifacts, serialized data, and native inputs
as untrusted.

Preserve these principles:

- Bound lengths, offsets, object counts, recursion/depth,
    decompression, allocations, and aggregate work.
- No implicit PDF JavaScript, launch action, executable/embedded
    executable, external command, or network execution.
- Fuzz high-risk byte boundaries and promote reproducible crashes,
    hangs, corruption, or invariant violations to deterministic
    regression coverage.
- Minimize sensitive-data lifetime and copies; do not claim guaranteed
    erasure of arbitrary managed memory.
- Keep native/PInvoke boundaries narrow and validated.
- Do not rely on writable/current-directory native-library search
    paths.
- Track dependency purpose, version, license, provenance, and
    security-update path.
- AOT, trimming, and symbol stripping are
    deployment-hardening/engineering concerns, not secrecy boundaries.
- Preserve signing/provenance expectations for official releases.
- Do not weaken validation, sanitization, capability checks, tests,
    timeouts, or failure handling merely to make validation pass.

## 10. Git and Codex operating rules

- Work in small, coherent, reviewable batches.
- Read root `AGENTS.md`, `PROJECT-CHARTER.md`, this handoff, and
    relevant PP-AUD reports before editing.
- Characterize failures before changing production behavior.
- Prefer evidence-backed fixes over speculative refactors.
- Directly exposed security, correctness, or performance defects may
    be fixed when necessary to the active authorized batch.
- Do not perform unrelated repository-wide sweeps.
- Do not mechanically modernize syntax.
- Preserve user/IDE edits and reconcile rather than overwrite them.
- Corrective iteration inside an approved validation slice is allowed
    when each change is directly supported by evidence. Do not stop
    merely because a required fix touches production code.
- When explicitly authorized and evidence directly implicates an
    existing dependency/toolchain component, a current stable upstream
    update of that same component may be used after reviewing
    provenance, release notes, licensing, transitive changes, and
    compatibility. Do not turn this into an unrelated dependency sweep
    or architecture replacement.
- No commit, push, merge, tag, reset, clean, branch deletion, or
    history rewrite unless explicitly authorized.
- Codex should run ordinary commands itself. If a required validation
    genuinely needs a context it lacks, provide the exact manual command
    and required context; never weaken the test instead.
- Never claim a user/manual validation passed until the user reports
    the result.

Authoritative release gate:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

The former standalone source-size script is intentionally removed. Do
not recreate it.

## 11. Substantive B9 campaign entry criteria

This campaign is deferred, not authorized by the current continuation.
These requirements remain for a future separately approved run and do not
gate the independent B10/P2 maintenance sequence.

Before launching the coverage-guided campaign:

1. Confirm `maintenance/post-BF-1054-repository-review` is the active
    branch and the working tree is clean.
2. Confirm the branch begins from accepted checkpoint `095ad01` or a
    later explicitly accepted maintenance checkpoint.
3. Read PP-AUD-029, PP-AUD-036, PP-AUD-037, and PP-AUD-038.
4. Preserve the approved toolchain unless evidence requires an
    explicitly authorized remediation.
5. Define campaign budgets before execution: target, duration/run
    bound, corpus input, output/finding location, process/memory
    ceilings, cancellation/timeout behavior, and evidence retention.
6. Start with the existing three neutral targets.
7. Demonstrate genuine coverage-guided execution; deterministic
    mutation/replay alone is not campaign evidence.
8. Preserve every reproducible unexpected failure through PP-AUD-037.
9. Replay findings deterministically before proposing promotion.
10. Do not automatically alter permanent corpus bytes or oracles.
11. Keep unexpected tooling/infrastructure failures distinct from engine
    defects and expected rejection.
12. Keep B9 open until coverage-guided execution and replay acceptance
    are demonstrated.

## 12. Remaining P2 sequence

Owner-approved sequence effective 2026-10-26 under PP-GOV-004. The former
"After POST-P2-002 is actually closed" prerequisite is superseded. Proceed
independently of native H2b qualification while B9 remains open:

- POST-P2-003 / B10 — reopen/render conformance, completed separately on 2026-10-27 (PP-AUD-040).
- POST-P2-004 — native resolution/fallback packaged-platform
    fixtures.
- POST-P2-005 — dependency provenance, notices, and SBOM.
- POST-P2-006 — release signing and provenance.
- POST-P2-007 — AOT/trimming validation.
- POST-P2-008 — full CI release-gate/source-size parity work not
    already satisfied by PP-AUD-038.
- **B16 — final repository cleanup and measured managed-fuzzer optimization**,
    reserved after POST-P2-008 by the owner on 2026-10-27. No existing batch is
    renumbered; B16 implementation is outside B11.

B16 must identify dead, unused, obsolete and unreachable code, obsolete
compatibility shims and duplicate implementations, and remove only proven
unnecessary code with regression coverage. Preserve public APIs,
serialization compatibility and intentional extension points; account for
reflection, dependency injection, plugins, XAML, generated references and
platform entry points rather than relying only on static references.

B16 must profile managed-fuzzer execution and investigate unnecessary
allocations, corpus duplication, repeated parsing, mutation throughput,
replay overhead, process supervision and artifact retention. Optimize only
measured bottlenecks while preserving deterministic replay, seed ownership,
mutation semantics, finding lifecycle, process ceilings and failure reporting.
Coverage, resource limits and security checks must not be weakened. Native
H2b stays deferred; B16 does not claim B9 coverage-guided acceptance.

Each item's existing acceptance requirements remain mandatory. POST-P2-004
is packaged-platform native dependency resolution/fallback coverage, not
authority to activate H2b native fuzzing. Signing/credentials/publication,
dependency changes and privileged operations retain their separate approval
boundaries. This documentation amendment does not implement a P2 batch.

P1 optimization items remain measurement-driven and should not be
performed without evidence.

## 13. Product-development boundary

Canonical font editing remains the next major feature-development area
after maintenance. Do not mix canonical font editing into the active B9
campaign or remaining maintenance batches.
BF-1055 is a separate product-development milestone; PP-GOV-004 changes
maintenance sequencing without implementing or accepting that milestone.

## 14. Recovery rule

If later work becomes uncertain:

1. Preserve and inspect the actual branch, HEAD and dirty working tree.
2. Use `PiratePDF-BF-Post-1054-Maintenance-Part1-Snapshot.zip` and `095ad01`
   only as immutable historical reference; do not restore/reset over current work.
3. Read this amendment, root `AGENTS.md` and PP-GOV-004.
4. Compare current state with accepted history without changing Git state.
5. Continue the scoped POST-P2-004 / B11 assignment, with remaining managed
   verification tracked separately. Do not resume deferred native H2b from
   historical continuation instructions.

## Revision history

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-10-05 | Establish the authoritative Post-BF-1054 Maintenance Part 1 continuation and recovery handoff at integrated checkpoint `095ad01`; record the accepted PP-AUD-038 CI state, open B9 / POST-P2-002 campaign boundary, recovery snapshot, security and Git/Codex operating rules, and remaining P2 sequence. |
| 1.1 | 2026-10-26 | Record native H2b deferral and partial managed status, distinct five broader/two filtered fixture failures, independent B10/P2 continuation and unchanged security/release criteria; preserve original checkpoint history. |
| 1.2 | 2026-10-27 | Record separate B10 conformance completion, 29 added cases and user-confirmed full-gate success while retaining denied attempts, open B9 and deferred native H2b; identify POST-P2-004 as next without beginning it. |
| 1.3 | 2026-10-27 | Record the authorized B11 start and reserve owner-approved B16 as final maintenance after POST-P2-008; retain semantic cleanup, measured optimization and all API, replay, coverage and security constraints. |
| 1.4 | 2026-10-27 | Record user-reported full B11 Windows release-gate success without failures or warnings; preserve failed agent runs and outstanding cross-platform, packaging and native-lifetime acceptance. |
| 1.5 | 2026-10-31 | Apply B11-D1 through D4 and record the missing desktop entry project, current Windows qualification gap, engine-suite evidence, CI alignment and preserved B16 reservation. |
| 1.6 | 2026-10-31 | Record user-reported success of the current full Windows gate while preserving failed agent evidence and the missing desktop application qualification gap. |
| 1.9 | 2026-10-31 | Complete B12 after package/tool evidence remediation, owner-confirmed use terms and full-gate success; preserve original artifacts and distinct validation scopes. |
| 1.8 | 2026-10-31 | Record owner-confirmed B12 full release-gate success; preserve the agent-run evidence and outstanding license/provenance remediation. |
| 1.7 | 2026-10-31 | Record B12 resolved dependency/SBOM evidence and outstanding license/provenance review; retain final B16-before-BF-1055 ordering and approved provisional Windows desktop milestones without implementing them or closing B11. |
