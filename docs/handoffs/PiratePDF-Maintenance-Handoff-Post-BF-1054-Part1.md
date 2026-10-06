# PiratePDF Maintenance Handoff

| Field | Value |
|---|---|
| **Document ID** | PP-HANDOFF-001 |
| **Category** | Engineering Handoff |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1054 |
| **Last Updated** | 2026-10-05 |
| **Next Review** | 2026-12-28 |


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

## 1. Purpose

This document is the authoritative continuation and recovery handoff for
PiratePDF / BlackFlag Engine after BF-1054 and Post-BF-1054 Maintenance
Part 1. A new maintenance or Codex session should read this document
together with root `AGENTS.md`, `PROJECT-CHARTER.md`, and the review
documents relevant to the active batch.

The recovery snapshot named above represents the integrated, CI-green
Part 1 checkpoint. Treat it as immutable recovery/reference evidence; do
not modify the snapshot in place.

## 2. Repository state at handoff

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

**Next B9 activity:** substantive bounded coverage-guided campaign.

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

After POST-P2-002 is actually closed:

- POST-P2-003 / B10 — reopen/render conformance.
- POST-P2-004 — native resolution/fallback packaged-platform
    fixtures.
- POST-P2-005 — dependency provenance, notices, and SBOM.
- POST-P2-006 — release signing and provenance.
- POST-P2-007 — AOT/trimming validation.
- POST-P2-008 — full CI release-gate/source-size parity work not
    already satisfied by PP-AUD-038.

P1 optimization items remain measurement-driven and should not be
performed without evidence.

## 13. Product-development boundary

Canonical font editing remains the next major feature-development area
after maintenance. Do not mix canonical font editing into the active B9
campaign or remaining maintenance batches.

## 14. Recovery rule

If later work becomes uncertain:

1. Return to `PiratePDF-BF-Post-1054-Maintenance-Part1-Snapshot.zip`.
2. Re-establish accepted checkpoint `095ad01`.
3. Read this handoff and root `AGENTS.md`.
4. Compare the current maintenance branch against the accepted
    checkpoint.
5. Resume from the first uncompleted B9 action rather than
    reconstructing state from assumptions.

## Revision history

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-10-05 | Establish the authoritative Post-BF-1054 Maintenance Part 1 continuation and recovery handoff at integrated checkpoint `095ad01`; record the accepted PP-AUD-038 CI state, open B9 / POST-P2-002 campaign boundary, recovery snapshot, security and Git/Codex operating rules, and remaining P2 sequence. |
