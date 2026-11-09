# PiratePDF B9 Phase A Completion-2 — Independent Supervision and Evidence Integrity

| Field | Value |
|---|---|
| **Document ID** | Draft — allocation pending |
| **Category** | Engineering Review |
| **Version** | 0.3 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1049, BF-1054 |
| **Last Updated** | 2026-10-10 |
| **Next Review** | 2026-11-10 |

## Durable progress checkpoint

**Completion-2 is not complete. B9 / POST-P2-002 remains OPEN.** This session stops after the coherent atomic-evidence and duplicate-integration sub-slice. The independent-parent ownership/handoff implementation is not present; completing and proving those separate process boundaries exceeds this bounded sub-slice. This report was saved before implementation validation to preserve the work inventory across UI interruptions.

Branch: `maintenance/post-BF-1054-repository-review`; actual HEAD: `f51824dbc031b58497a38a6292ee11568b9ffeda`. Existing dirty/untracked Phase A files, Completion-1 changes, its report and historical validation artifacts are retained. The new Completion-2 request explicitly reports the complete Completion-1 focused suite passed **151/151** in Administrator PowerShell and accepts Completion-1; that is user-reported evidence, not a new agent run. The older Completion-1 report remains historical.

Root AGENTS.md, PROJECT-CHARTER.md, the accepted handoff, PP-AUD-039 Section A, Completion-1 report, campaign code, WorkerSupervisor, ChildProcess/SmokeCommand and FindingStore/FindingFiles were reviewed. An existing process wrapper cannot simply be reused: WorkerSupervisor hardcodes single-case launch/protocol/deadline, while ChildProcess is deliberately smoke-only with a 20-second maximum.

## Current coherent slice

This slice implements the atomic evidence-directory commit and deterministic publication/duplicate integration tests. The prior Publish renamed ledger.pending before creating summary.json; a summary failure left a successful terminal ledger while the caller returned failure. Cleanup and late-expiration could also change the returned result after immutable publication. The bounded evidence snapshot is now published together, or remains explicitly uncommitted.

Changed files: `tools/PiratePDF.Fuzz.Runner/CampaignEvidence.cs`, `CampaignRunner.cs`, `CampaignResult.cs`, `CampaignConfiguration.cs`; `tests/PiratePDF.Fuzz.Tests/CampaignTestChild.cs`; new `CampaignEvidenceTests.cs`; this report. No production/SDK, package, permanent corpus/oracle, smoke, CI, roadmap or governance change is made.

CampaignEvidence now stages in `artifacts/fuzz/campaigns/.<run>-pending`; the dot-prefixed name cannot collide with a valid run ID. Prior pending/final directories are refused. All existing admission and bounded inventory checks apply. It creates the summary with CreateNew, closes owned handles and deletes its lease before one same-parent `Directory.Move` publishes ledger.jsonl and summary.json together. Foreign destination data and original finding bytes are never overwritten or deleted to force publication. Failed writes/rename preserve the pending directory. Inventory failure releases the owned lease handle even when foreign contents are preserved.

`EvidencePublished` explicitly distinguishes committed snapshots from uncommitted attempts. A publication I/O/rename failure returns `PublicationFailure` / exit **8**, rather than claiming committed evidence. Existing stop codes remain unchanged. A committed snapshot's JSON is exactly the returned JSON; cleanup cannot revise the record after commit. Pending snapshots are provisional even if their attempted terminal says EvidencePublished=true. Only the final directory is a commit indication; this alone does not authenticate a future parent/child handoff.

The cooperative deadline is checked before publication and immediately before directory commit. Post-commit deadline classification must belong to a separate parent receipt; the runner no longer mutates an already committed result on a late timer tick. **This leaves independent late-timeout detection unresolved**, rather than claiming that the publication fix enforces the whole-invocation 75-second contract. Same-parent directory rename is a namespace commit, not guaranteed disk durability, a filesystem sandbox or immunity to privileged concurrent tampering.

The owned duplicate test recipe `duplicate-006086`, seed 1049, deterministic-v2 and empty source yields only empty outputs for indices 0–7. Test-only temporary corpus copies retain the 6+6+3 manifest contract and valid hashes; permanent corpus/oracles are unchanged. Two repeated schedules must each stop at exactly eight attempts/eight duplicates, zero requests/executions, one zero-byte pool entry and two ledger records. No generator override or input-selected executable is introduced.

All original Completion-1 source remains present; the only further changes are the campaign publication/result paths and associated tooling tests above. No independent launcher or completed Completion-2 claim follows from this first slice. No commit, push, merge, tag, reset, clean, stash, branch switch or history operation occurred.

## Independent parent design and next boundary

The smallest viable next implementation is a campaign-only parent in Runner, using a fixed dotnet host and the same trusted Runner assembly with a dedicated internal child mode. Public campaign-mutate dispatch must never recurse through the public parent command. Authorize child entry through a bounded one-use invocation ticket/handshake bound to run, revision, configuration, binary hashes, nonce and the exact parent PID/start time; reject direct/stale/mismatched child invocations before campaign work. Do not accept input-selected binaries, shell text or redirect paths.

The parent must own an immutable final invocation outcome distinct from the child's provisional campaign snapshot. A completed child snapshot alone is not successful handoff: verify exact invocation identity, bounded inventory, ledger/summary integrity, exit/status agreement, fresh child identity and cleanup. Reject missing/partial/corrupt evidence, stale output and exit conflicts. A timeout during child publication remains the parent's timeout even if a child snapshot exists; never rewrite the immutable original snapshot to hide that distinction.

Use 75 seconds for the whole child invocation, including setup and child publication, plus an independent five-second root/descendant/capture cleanup allowance. A separate finite parent receipt-publication allowance must be specified and reported, rather than hidden inside “75 seconds.” Synchronous Process.Start/Kill and filesystem calls remain OS scheduling boundaries; do not claim an unconditional hard wall-clock return or hard memory sandbox.

Root-only Kill(entireProcessTree:true) is insufficient when the campaign root exits while a worker survives. WorkerSupervisor currently exposes no parent-visible owned worker identity/lease registration. Before wiring a parent, design and test a bounded campaign-only worker registration/cleanup handshake; root exit/EOF must never substitute for proof that observed exact worker instances exited. This requires explicit cross-process ownership and handoff tests, not merely wrapping the existing in-process task in WaitAsync. Preserve portable controlled-process policy versus Windows real file/lease integration routes.

Next sub-slice: implement/review the campaign invocation protocol and worker ownership seam, then independent parent supervision and its controlled timeout/cancel/root-exit tests, then validated parent receipt publication. Do not claim this design is implemented. Completion-2's permission to stop after a coherent safe sub-slice applies; remaining work stays within the authorized Completion-2 scope.

## Validation

New ignored evidence: `artifacts/fuzz/phase-a-completion-2-20261010-01`; `entry-hashes.json` records pre-existing dirty/untracked file identities. Environment: Windows 10.0.26200 / win-x64, .NET SDK 10.0.302, runtime 10.0.10; ordinary non-administrator execution.

| Run | Evidence | Actual result |
|---|---|---|
| Evidence first | evidence-first.log / evidence-first.trx | Exit 1; 9 total, 8 passed, 1 failed, 0 skipped. |
| Evidence corrected | evidence-final.log / evidence-final.trx | Exit 0; 9 total, 9 passed, 0 failed, 0 skipped; test-reported 891 ms. |
| Broader affected focused suite, final source | focused.log / focused.trx | Exit 1; 157 total, 156 passed, 1 failed, 0 skipped; test-reported 21 seconds. |
| User's permitted-context broader rerun | focused-admin.trx | Saved TRX verified: Completed; 157 total/executed, 157 passed, 0 failed, 0 skipped. Run start 2026-10-10 17:40:14 EDT; finish 17:40:36 EDT. Shell exit code was not supplied. |
| Affected-project Release build | build.log | Exit 0; 0 warnings / 0 errors; 2.77 seconds reported. |

The first failure was ExistingPendingDirectoryIsNeverReusedOrModified: WorkspaceFailure was incorrectly converted to PublicationFailure by attempting publication on an unopened evidence object. The ownership/open guard was corrected and the corrected test passed. Evidence is preserved, not overwritten. The broader run followed the final cleanup-handle correction and includes all six new Windows integration cases. Its only failure was the unchanged CampaignWorkspaceLinkIsRejected fixture: `System.IO.IOException: A required privilege is not held by the client`, Directory.CreateSymbolicLink, CampaignRunnerTests.cs:245, before validator execution. This fresh failed run is not replaced by the user's earlier successful Completion-1 run. No tests were skipped, loosened or removed.

Passing groups: CampaignEvidenceTests 6/6; CampaignAccountingTests 11/11; CampaignPolicyTests 45/45; CampaignRunnerTests 29/30 (privilege fixture failed); DeterministicMutationTests 21/21; CorpusMutationTests 35/35; SmokeChildProcessTests 9/9. Thus deterministic-v1 vectors, the separate smoke limit, original-input preservation, worker timeout/cancellation and existing descendant cleanup checks remain exercised. These are controlled tests, not a substantive campaign, and no throughput/coverage claim follows.

Exact commands from `C:\Development\PiratePDF` (all logs redirected to the new evidence directory; unique TRX names preserve attempts):

```powershell
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CampaignEvidenceTests|FullyQualifiedName~EvidenceInventoryFailureRetainsCandidateAndNeverPublishesSuccess|FullyQualifiedName~FailedRetentionKeepsExactTemporaryCandidate|FullyQualifiedName~OuterExpirationReapsWorkerAndPreservesInputWhenRetentionDeadlineHasExpired' --logger 'trx;LogFileName=evidence-final.trx' --results-directory artifacts/fuzz/phase-a-completion-2-20261010-01 --verbosity quiet
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CampaignAccountingTests|FullyQualifiedName~CampaignPolicyTests|FullyQualifiedName~CampaignRunnerTests|FullyQualifiedName~CampaignEvidenceTests|FullyQualifiedName~DeterministicMutationTests|FullyQualifiedName~CorpusMutationTests|FullyQualifiedName~SmokeChildProcessTests' --logger 'trx;LogFileName=focused.trx' --results-directory artifacts/fuzz/phase-a-completion-2-20261010-01 --verbosity quiet
dotnet build tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --verbosity quiet
git diff --check
```

The first evidence command used `LogFileName=evidence-first.trx`; its filter/arguments were otherwise identical. `git diff --check` and explicit trailing-whitespace checks for new/changed checkpoint files passed. Pre-existing files outside the stated slice retain their entry hashes, including the Completion-1 report and assessment/generated documentation. Source/evidence hashes and final inventory are saved beside the logs. All historical validation files remain untouched.

A fixture-recipe diagnostic initially used a modern SHA256 API unavailable in Windows PowerShell 5; that diagnostic was stopped, produced no recipe/test result and modified no files. A compatible bounded SHA256 search found the fixed recipe above. This is not campaign/test evidence.

The privileged validation boundary is resolved. The user confirmed the unchanged fixture passed, then ran the broader focused command in the requested permitted context with `LogFileName=focused-admin.trx`. The agent inspected that saved TRX and verified all 157 cases passed, zero failed/skipped. The failed non-administrator run remains preserved as historical evidence. The commands requested were in **Administrator PowerShell**; exact fixture command:

```powershell
Set-Location C:\Development\PiratePDF
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CampaignWorkspaceLinkIsRejected' --logger 'trx;LogFileName=link-admin.trx' --results-directory artifacts/fuzz/phase-a-completion-2-20261010-01 --verbosity normal
```

Administrator context was needed because the agent's process could not create the required symlink fixture. No tests were rerun or changed to incorporate this successful saved result. This establishes the broader focused test result for the evidence sub-slice, not completion of the pending independent supervision implementation. No full release gate or substantive campaign ran. Full suite/catalog/hosted-platform acceptance remains future Completion-3 work.

## Precise remaining Completion-2 work

The public campaign command still executes in-process. There is no dedicated internal child mode, one-use child admission, independent parent 75-second deadline, parent-visible worker ownership or parent-authenticated receipt. The future parent must test normal/failed child exit, launch failure, root exit with live observed worker, timeout/caller cancellation, cleanup failure, missing/partial/corrupt/stale handoff, mismatched exit/status and deadline expiry during/after publication. It must refuse unsafe/symlink/redirected tickets and retain portable/Windows routing. These contracts are explicitly not established by the atomic snapshot tests above.

The current cooperative runner freezes the committed snapshot; an independent parent must detect late deadline expiration and record its own failure even if that child snapshot committed. Do not use the snapshot alone to assert a successful independently supervised invocation. No significant new process/ownership architecture or external dependency was improvised to claim that gap closed. Continue from the parent design above, preserving this evidence sub-slice and its six new tests.

## Future candidates — record only

Structural anomaly metrics, adaptive resource restrictions that never relax baseline hard limits, and Avalonia MVVM security status remain user-approved future security-hardening candidates. None is implemented here; no roadmap/governance file is changed. SharpFuzz/libFuzzer/LLVM remain the approved baseline.

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-10-10 | Save pre-validation progress inventory, atomic-publication slice and independent-parent/worker-ownership design; keep Completion-2 and B9 open. |
| 0.2 | 2026-10-10 | Record atomic snapshot/publication-result changes, deterministic duplicate and failure tests, corrected 9/9 slice, final 156/157 focused result and clean build; stop with independent supervision and privileged validation explicitly pending. |
| 0.3 | 2026-10-10 | Verify the user's saved focused-admin.trx: 157/157 passed, zero failed/skipped; resolve privileged focused validation while preserving historical failure evidence and the independent-supervision gap. |
