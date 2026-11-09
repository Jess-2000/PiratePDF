# PiratePDF B9 Phase A Completion-1 — Correctness and Resource Accounting

| Field | Value |
|---|---|
| **Document ID** | Draft — ID pending allocation |
| **Category** | Engineering Review |
| **Version** | 0.2 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1049, BF-1054 |
| **Last Updated** | 2026-10-10 |
| **Next Review** | 2026-11-10 |

## Outcome and recovery state

Completion-1 implements bounded work admission and a cooperative outer deadline, with focused evidence. The user subsequently confirmed that the requested symbolic-link test rerun passed. **Acceptance remains pending**: the complete permitted-context focused-suite rerun has not been reported, and a truly independent outer launcher remains unimplemented. B9 / POST-P2-002 stays OPEN. This is mutation-only tooling, not coverage-guided equivalence or permission to retire SharpFuzz/libFuzzer/LLVM.

Actual branch: `maintenance/post-BF-1054-repository-review`. Actual HEAD: `f51824dbc031b58497a38a6292ee11568b9ffeda`. Starting status contained seven modified and thirteen untracked files from interrupted Phase A and the preceding assessment. That dirty state was expected and preserved.

Root AGENTS.md, PROJECT-CHARTER.md, the accepted Part 1 handoff and PP-AUD-039 Section A were read in the recovery/continuation review. The attached Completion-1 instructions were read. An original Phase A implementation prompt was not located; the supplied Downloads document is the same Completion-1 prompt, not the original. This slice follows Section A and the explicit Completion-1 constraints. No original implementation was recreated.

The four assessment/documentation files listed in the existing Phase A `baseline.json` retain their recorded SHA-256 hashes. Existing campaign work, historical TRX files and corpus/oracles were preserved. No production/SDK, dependency, CI, smoke implementation, Git history or roadmap changes occurred. No full release gate or substantive campaign ran.

## Exact changes in this checkpoint

Paths below are relative to the repository; most campaign files were already untracked at entry.

- `tools/PiratePDF.Fuzz.Runner/CampaignRunner.cs`: reserve setup, manifest, selected-source and attempt work before reads/copies/generation; use exact admitted seed-length read buffers; check conservative candidate pool capacity before generation; use bounded attempt policy; wire outer deadline through setup, workers and retention, preserve original bytes when retention expires, and report late outer expiration.
- `tools/PiratePDF.Fuzz.Runner/CampaignConfiguration.cs`: add overflow-safe wide work admission and a finite attempt/duplicate policy; append the distinct outer-deadline stop reason.
- `tools/PiratePDF.Fuzz.Runner/CampaignPool.cs`: add payload-free source description and admission-before-copy access. Existing independently owned source-copy behavior remains available.
- `tools/PiratePDF.Fuzz.Runner/CampaignResult.cs`: expose `OuterExpired` separately from cleanup/retention status; outer deadline has nonzero exit code 7.
- `tools/PiratePDF.Fuzz.Runner/CampaignWork.cs` (new): central conservative payload-byte-pass reservation calculations.
- `tools/PiratePDF.Fuzz.Runner/CampaignDeadline.cs` (new): campaign-only cooperative 75-second maximum timer and caller/expiration distinction.
- `tests/PiratePDF.Fuzz.Tests/CampaignAccountingTests.cs` (new): deterministic accounting, retry, wide arithmetic, outer timer and smoke-limit checks.
- `tests/PiratePDF.Fuzz.Tests/CampaignRunnerTests.cs`: add controlled outer-expiration/retained-input checks and zero/exact-attempt work-exhaustion integration cases.

This report is new. The draft ID is deliberately pending; no document-index generation or unrelated documentation updates were performed.

## Work and duplicate semantics

`ProcessedBytes` now reports conservative reserved payload-byte-pass units, not unique inputs, exact observed IO, total RSS or CPU work. Limits remain 16 MiB per target, 10,000 worker requests, 60 seconds active time, 128 pool entries / 8 MiB, registered input ceilings, and 16 MiB run telemetry. Setup is charged to the first selected target, never reset away before its exploration. Metadata parsing/inventory and downstream target expansion retain their existing independent finite contracts. Trusted build-assembly identity hashing is operator configuration, outside the untrusted payload allowance; this is not a whole-process allocation meter.

Let `M = 3 × (128 KiB + 1)`, `L` be source length, `R` the original seed length, `T` the registered target ceiling, and `O = min(T, L + 8)`.

- Every manifest read/hash reservation costs `M`, conservatively covering its ceiling-sized read buffer, read and hash passes plus EOF probing.
- Before full corpus validation, metadata is admitted and checked for the exact 15-owned-seed/6+6+3 contract. Validation is reserved at `M + Σ(3 × seed target ceiling + 1)` before that validator runs. The initial metadata read and post-validation manifest reread each have separate `M` reservations. Manifest identity must remain unchanged.
- Initial target-pool seed loading reserves `5 × seed length + 1`: exact-sized read buffer/read/hash, pool digest and independent owned copy. Pool capacity is checked first.
- Every attempt reserves another manifest read, then `3R + 2L + 9O + 2T + 3` for seed reread/hash, mutation source/output processing, staging write/read/compare, worker input and eventual pool insertion. Source copying is additionally reserved at `L` by `TrySource` before `Source` allocates. The upper envelope includes branches that may not ultimately run; it is intentionally conservative.
- Duplicate, shrink, cancellation and partial-failure paths do not refund reservations. Each physical pipeline reservation is made once; retry costs remain charged. Repeated hashes are not fresh exploration or worker requests. Counters are bounded and wide costs are compared to remaining allowance before narrowing.
- Eight consecutive duplicate hashes stop with `DuplicateLimit`; a unique candidate resets that streak. The independent total-attempt ceiling is 80,000. Full pools stop with `PoolLimit` before source copying/generation. Conservative upper output capacity can stop earlier than actual shrink/output size would require; this is deliberate bounded behavior.

Discovery retention remains a separate, bounded recovery lane under unchanged FindingStore payload/metadata/inventory limits. It is not charged against an already exhausted exploration budget, which must not cause evidence deletion. No permanent corpus bytes or expectations are promoted automatically.

## Deadline semantics and limitations

The first 75-second cooperative outer timer starts before setup; subsequent sequential targets receive separate positive, at-most-75-second timers. Each active target still has its own at-most-60-second deadline linked into the existing at-most-10-second worker deadline. Worker cleanup retains its independent five-second allowance. Retention has its own five-second allowance linked to outer expiration, independently of caller cancellation. An expired retention allowance preserves the temporary candidate and reports `RetentionFailure`, `CleanupSucceeded=false`, and `OuterExpired=true` rather than claiming successful retention or deleting it.

The final target timer remains alive through workspace cleanup and evidence publication. Caller cancellation, active deadline, outer expiration, worker timeout and cleanup/retention failure remain distinguishable. Controlled test timers exercise outer expiry without wall-clock sleeps. Smoke `ChildProcess.RunAsync` and its 20-second maximum are untouched.

**Remaining outer-launcher blocker:** no independent parent process supervises the entire command. Tokens cannot interrupt synchronous file admission, build-identity hashing, publication/disposal or blocked OS start/kill calls. Cleanup may outlast outer expiration. A terminal ledger written immediately before late expiry can differ from the subsequently returned failure result. The code detects observed late expiry and fails closed, but does not prove a hard 75-second end-to-end return or an OS containment guarantee. A separately reviewed campaign-only independent launcher/publication contract is still needed if strict outer supervision is required; this slice does not pretend that it exists.

## Fresh verification evidence

Environment: Windows 10.0.26200, win-x64; SDK 10.0.302; runtime 10.0.10. Ordinary non-administrator execution. New ignored evidence directory: `artifacts/fuzz/phase-a-completion-1-20261010-01`.

Focused command, run from `C:\Development\PiratePDF` (TRX name changed per attempt, preserving each result):

```powershell
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CampaignAccountingTests|FullyQualifiedName~CampaignPolicyTests|FullyQualifiedName~CampaignRunnerTests|FullyQualifiedName~DeterministicMutationTests|FullyQualifiedName~CorpusMutationTests|FullyQualifiedName~SmokeChildProcessTests' --logger 'trx;LogFileName=focused-final.trx' --results-directory artifacts/fuzz/phase-a-completion-1-20261010-01 --verbosity quiet
dotnet build tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --verbosity quiet
git diff --check
```

| Attempt | Evidence | Result |
|---|---|---|
| First | `focused-first.log` | Analyzer CA1068 stopped compilation; no tests ran. Deadline constructor parameter order corrected. |
| Second | `focused-second.log`, `focused-second.trx` | 150 total: 149 passed, 1 failed, 0 skipped; test-reported duration 23 seconds. |
| Final, after setup admission and exact-attempt integration | `focused-final.log`, `focused-final.trx` | 151 total: 150 passed, 1 failed, 0 skipped; test-reported duration 21 seconds. |
| Explicit affected-project build | `build.log` | Passed, 0 warnings / 0 errors; includes referenced Runner/Targets and their existing dependency graph. |
| Whitespace / final inventory | final status and diff review | `git diff --check` passed; untracked checkpoint source/report also checked separately. |

The sole failure in the agent-executed focused runs was `CampaignRunnerTests.CampaignWorkspaceLinkIsRejected`: `System.IO.IOException: A required privilege is not held by the client` at `Directory.CreateSymbolicLink`, CampaignRunnerTests.cs:245, before the validator executes. It was executed, not skipped or weakened. Those failed-run results remain historical evidence.

The user subsequently replied, "The test passed," in response to the requested Administrator PowerShell focused link-test rerun. This confirms the focused link check through user-reported evidence and resolves that fixture's privilege blocker. No raw output, totals or TRX were supplied; this statement does not establish a successful complete focused-suite rerun or alter the saved 150-pass/1-failure run.

All other focused cases passed, including deterministic-v1 golden vectors, campaign policy, controlled real target execution, pool/processed limits, timeout/cancellation, independent root/child checks, simultaneous oversized streams and preservation on retention failure. Historical 176/176, 66/66, 22/22 and 26/26 runs are recovery evidence only, overlap, and are not added to these totals.

The requested permitted-context command, now user-confirmed passed, was in **Administrator PowerShell** (elevation was needed only for the existing symlink fixture):

```powershell
Set-Location C:\Development\PiratePDF
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CampaignWorkspaceLinkIsRejected' --logger 'trx;LogFileName=link-admin.trx' --results-directory artifacts/fuzz/phase-a-completion-1-20261010-01 --verbosity normal
```

The remaining validation is the complete focused command above in the same Administrator PowerShell, using `LogFileName=focused-admin.trx`. Return totals/exit status or that TRX file. Do not run the full release gate in Completion-1. The successful complete focused result remains pending; no tests or implementation changes were made merely to incorporate the user's link-test result.

## Completion-2 / Completion-3 gaps and stop

Completion-2 should reconcile any original Phase A prompt subsequently recovered, resolve/review the true independent outer-launcher and late-publication boundary, and complete remaining lifecycle/evidence failure coverage. Duplicate exhaustion is freshly proven by the same finite policy used by Runner; a campaign-level fixture forcing eight consecutive duplicate hashes remains additional integration coverage to consider. Full fuzz-suite, portable-route/catalog reconciliation and final implementation usage/acceptance documentation remain unestablished for this tree.

Completion-3 should perform separately authorized broader acceptance validation, permitted Windows link fixtures, documentation/index allocation, final source/API review and the unchanged release gate. No Phase A acceptance, campaign throughput, coverage guidance, toolchain equivalence or dependency-retirement claim follows from Completion-1.

No commit, push, merge, tag, branch switch, reset, clean, stash or history operation was performed. Stop at this checkpoint with the original dirty tree and all evidence preserved.

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-10-10 | Record Completion-1 work accounting and cooperative deadline changes, fresh focused/build evidence, unchanged symlink privilege blocker and remaining independent-launcher/acceptance gaps. |
| 0.2 | 2026-10-10 | Incorporate the user's successful focused link-test rerun; preserve failed agent-run evidence and distinguish the still-pending complete focused suite and independent-launcher contract. |
