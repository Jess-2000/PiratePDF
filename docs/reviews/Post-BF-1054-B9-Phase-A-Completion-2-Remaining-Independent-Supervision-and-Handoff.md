# B9 Phase A: Remaining Completion-2 Supervision and Handoff

Document ID: Draft - governance allocation pending. Status: PARTIAL, not READY FOR REVIEW as completed Completion-2.

## Initial durable checkpoint

Verified branch `maintenance/post-BF-1054-repository-review`, HEAD `f51824dbc031b58497a38a6292ee11568b9ffeda`. All existing dirty files are intentional. Entry status and SHA-256 inventory, including previous completion evidence, saved in `artifacts/fuzz/phase-a-completion-2-remaining-20261010-01`. Prior elevated Completion-2B TRX was inspected: 186 executed/passed, zero failed/skipped. Earlier 151/151 and 157/157 remain historical accepted evidence, not validation of subsequent edits.

## Ordered implementation plan and boundaries

1. Add bounded one-use Windows ticket storage using the existing canonical identity. Parent generates nonce; fixed invocation paths never come from artifact content. Bind exact live parent and admitted child PID/start time, loaded Runner/Targets hashes, repository identity and expected ticket digest. Consume by exclusive atomic rename; preserve consumed/invalid/foreign evidence. Real tests must cross a controlled process boundary.
2. Establish gated workers: registration proof, exact-instance observation, authenticated ACK before input execution and ownership retained until verified exit. Preserve the ordinary single-case supervisor and smoke semantics. No child campaign work is safe before this prerequisite is tested.
3. Only after gating is proven, activate fixed internal child/public parent routing. Parent owns the whole 75-second invocation, independent at-most-five-second cleanup, plus a separately declared bounded receipt allowance. A cooperative child clock is defense in depth, not independent supervision.
4. Authenticate bounded immutable child snapshot, ledger/summary/exit agreement and cleanup; commit separate atomic parent receipt. Timeout wins even when provisional child snapshot committed. Preserve all forensic artifacts.
5. Run focused tests/build after slices and final complete affected suite; retain actual exits/logs/TRX, privilege failures and exact rerun commands. No release gate or substantive campaign.

Same-user trusted workspace is the threat model. HMAC establishes invocation-secret knowledge, not an OS authentication/sandbox boundary. PID alone is insufficient; exact start time and held process handles matter. Checked local Windows file paths refuse observed links/reparse/device/redirects; they do not prevent hostile privileged concurrent filesystem replacement. Atomic namespace rename does not promise disk durability. No unconditional wall-clock/RSS guarantees.

## Acceptance and failure matrix

Pending. Public dispatch is still in-process. A ticket codec alone cannot justify independent supervision; a root exit/EOF cannot justify worker cleanup. Do not activate an unsafe next boundary or silently claim a fallback is supervised. B9 / POST-P2-002 remains OPEN regardless of Completion-2 review readiness.

## Checkpoint A: one-use storage and real child admission

Added `CampaignProcessIdentity.cs`, `CampaignTicketBinding.cs`, `CampaignTicketStore.cs`, `CampaignAdmissionTestChild.cs`, `CampaignTicketAdmissionTests.cs`; extended only the controlled `SupervisorTestChild` dispatch. Ticket storage is fixed to `artifacts/fuzz/invocations/<run>`; issuance stages in a separate dot-prefixed directory. Existing/pending storage is refused. Cryptographic random nonce, actual loaded binary identities, normalized repository digest, exact issuing parent and MAC-bound exact child are validated. Non-overwriting ticket-to-consumed rename arbitrates one consumer. Failed/consumed/foreign storage is retained; no ticket recycling or public secret receipt. Inventory is at most two ordinary files. The child must match its exact issued binding and observe the exact live parent before admission returns.

First validation stopped at seven test compile errors (missing explicit Tasks imports); no tests ran. Corrected `admission-second.trx`: exit 0, 50 total/passed, zero failed/skipped, about one second. Includes preserved 29 protocol cases and 21 new Windows admission cases crossing an actual controlled child process. New cases exercise valid one-use/replay, direct invocation, wrong digest/parent PID/start/binaries/repository/run/config/revision/nonce, truncated/oversized/noncanonical tickets, foreign retention and unsafe run paths. No production child entry point is activated yet; gating is prerequisite. A library admission result is not a successful supervised campaign.

## Checkpoint B: real worker ACK and ownership seam

Added `CampaignWorkerAcknowledgement.cs`, `CampaignGateTransport.cs`, `CampaignWorkerGate.cs`, `CampaignGatedWorkerCommand.cs`, portable `CampaignWorkerGateTests.cs`, `CampaignBindingPolicyTests.cs`, controlled `CampaignGateTestChild.cs` and Windows `CampaignGatedWorkerTests.cs`. Narrow `WorkerSupervisor.RunCampaignAsync` seam shares existing bounded capture/ten-second deadline/five-second cleanup and ordinary protocol implementation. Its private core awaits gate registration before worker input execution; gate failures remain failures and cleanup shares the existing allowance. `Program.cs` adds only the distinct `campaign-worker-internal` dispatch, which never dispatches a public campaign. Existing public campaign scheduling is not switched.

One-use, current-user named pipe transfers bounded identity, canonical invocation-authenticated registration and registration-specific ACK. The gate acquires a separate exact-process handle before ACK and retains it until ConfirmExit/cleanup. Same-instance PID/start must match; parent cannot register itself as a cleanup target. Disposal closes handles and never certifies exit; callers must verify cleanup before success. Framing bounds are 2048/512/256 bytes with a positive at-most-five-second handshake (inside the ten-second case deadline). The control channel is separate from unchanged 512-byte worker stdout/empty successful stderr, 64 KiB captures and smoke limits. Secret identity stays on private invocation storage/control channel, not command line or stdout. Namespace binding publication was refined to stage child.pending then rename after closing, avoiding a consumer observing partially written binding.

`gate-first.trx`: 8/8 passed, exit 0. `gated-worker-first.log` failed compilation before tests due to calling instance TargetRegistry.Find statically; corrected registry admission. `gated-worker-second.trx`: 34/34 passed, exit 0, about two seconds (overlaps admission/gate/policy; not additive unique totals). Real neutral Runner worker execution is exercised only after ACK. Controlled fixtures cover missing/late ACK, forged/oversized/truncated ACK, one-use gate, cancellation cleanup and a root that exits with a still-live exact registered worker; the parent then independently reaps that worker. Additional wrong-case/failed-launch/self-cleanup and symlink tests were added before final validation.

## Safe routing boundary and precise remaining implementation

This is PARTIAL. A and B now have tested reusable storage/ownership seams, but public campaign child orchestration is not implemented. The campaign runner still owns worker dispatch in-process. The future independent parent should own actual worker launches through a bounded authenticated child-to-parent case-request broker; this avoids transferring kill responsibility from an already-exited campaign root. That broker must bind target, fixed incoming slot/input identity, sequence and remaining case/target allowance, serialize one worker, propagate cancellation/root exit and retain exact ownership until exit. Current gate tests prove the worker boundary, not the absent campaign broker.

Without that broker, wiring public dispatch would run ungated workers under a child and leave independent cleanup unproven. Stop before that unsafe activation. Remaining A activation: dedicated admitted campaign-child entry; remaining B integration: broker connected to CampaignRunner; C: independent whole-child supervisor, root/capture cleanup and stable parent outcomes; D: authenticated child snapshot/ledger/status verifier and separate immutable parent receipt; E: their full launch/timeout/publication/cancellation/cleanup/forensic failure matrix. No independent supervision or Completion-2 readiness claim is made. Final focused validation and affected build follow this saved boundary checkpoint; Completion-3 remains separate.

## Final verification and recovery state

Final inspection corrected an attachment race: a candidate process handle is now assigned to ownedWorker only after its exact start time matches the originally observed worker. A replacement PID instance on an attachment failure is disposed without becoming a kill target. Code inspection establishes this fail-closed ordering; no OS PID-recycling experiment was performed. The complete suite was rerun after this correction.

Final gate framing refinement: the worker confirms the authenticated ACK digest and waits for channel EOF before executing. The parent closes only after validating that confirmation, while retaining worker ownership. Any trailing/duplicate ACK frame is rejected. EOF is used only as a handshake terminator after proof validation; it is never evidence of process cleanup. A real duplicate-ACK regression case was added. This adds one case to the final-source suite; earlier 268-case runs remain historical.

The first 269-case run (`focused-terminal.trx`, exit 1, 266 passed/3 failed/0 skipped, 23 seconds) exposed a controlled sender-fixture deadlock: its default zero-buffer pipe blocked writing the duplicate ACK while the worker wrote confirmation. The fixture now uses explicit bounded 4096-byte duplex buffers matching the production gate and writes both frames as one bounded operation. Assertions remain unchanged: the worker must exit 94 with zero input-execution output. `gate-terminal.trx`: 14/14 passed, zero failed/skipped, exit 0, one second. Final broader validation follows; previous failed evidence is retained.

| Validation | Evidence | Actual result |
|---|---|---|
| Initial admission compilation | admission-first.log | Exit 1; missing Tasks imports, no tests executed. |
| Admission/protocol corrected | admission-second.log / .trx | Exit 0; 50/50 passed, zero failed/skipped. |
| Initial controlled gate | gate-first.log / .trx | Exit 0; 8/8 passed, zero failed/skipped. |
| Initial gated worker compilation | gated-worker-first.log | Exit 1; invalid static registry call, no tests executed. |
| Gated worker/admission/policy corrected | gated-worker-second.log / .trx | Exit 0; 34/34 passed, zero failed/skipped. |
| Broader focused before final attachment refinement | focused-final.log / .trx | Exit 1; 268 executed, 266 passed, 2 failed, zero skipped; 22 seconds. |
| Final-source broader focused | focused-confirmed.log / .trx | Exit 1; 268 executed, 266 passed, 2 failed, zero skipped; 22 seconds. |
| Affected Release build before attachment refinement | build-final.log | Exit 0; zero warnings/errors, 3.06 seconds. |
| Release build after attachment refinement | build-confirmed.log | Exit 0; zero warnings/errors, 2.14 seconds. |
| Final ACK framing, first broader attempt | focused-terminal.log / .trx | Exit 1; 269 executed, 266 passed, 3 failed, zero skipped; 23 seconds. |
| Corrected final ACK/neutral worker focused | gate-terminal.log / .trx | Exit 0; 14/14 passed, zero failed/skipped. |
| Final-source complete affected focused suite | focused-complete.log / .trx | Exit 1; 269 executed, 267 passed, 2 failed, zero skipped; 23 seconds. |
| Final-source affected Release build | build-complete.log | Exit 0; zero warnings/errors, 9.38 seconds. |

Both final failures are fixture creation privilege errors, before the validators execute: unchanged CampaignWorkspaceLinkIsRejected, and new TicketDirectoryLinkIsRejectedAndForeignBytesPreserved, both `Directory.CreateSymbolicLink` / `A required privilege is not held by the client`. No fixture was weakened or skipped. Final results include 39 new cases (38 passed plus the new privileged fixture), all preserved 186 cases (185 passed plus the original privileged fixture), 40 ordinary WorkerSupervisor cases and four platform-catalog cases. Overlapping focused runs are not added as unique tests. Existing cancellation/timeout/bounded capture, mutation-v1 golden, deterministic-v2/budgets/duplicate exhaustion, immutable findings/publication failure, smoke and ordinary supervision regressions pass. Linux execution is not claimed.

The initial inventory contains 30 dirty-tree files and 21 previous checkpoint evidence files. Hash comparison confirms 49/51 unchanged; only pre-existing Program.cs and SupervisorTestChild.cs receive additive dispatch branches. Their previous campaign branches are retained. WorkerSupervisor.cs is the sole previously clean tracked source touched. The thirteen new source/test files and this report are the remaining scope. Earlier reports, their evidence, permanent corpus/oracles and all unrelated changes remain untouched. No history operation, dependency/production engine/UI/SDK/CI/roadmap change, release gate or substantive campaign occurred.

Exact commands from `C:\Development\PiratePDF` (logs redirected into the fresh evidence directory):

```powershell
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CampaignTicketAdmissionTests|FullyQualifiedName~CampaignInvocationProtocolTests' --logger 'trx;LogFileName=admission-second.trx' --results-directory artifacts/fuzz/phase-a-completion-2-remaining-20261010-01 --verbosity quiet
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CampaignWorkerGateTests' --logger 'trx;LogFileName=gate-first.trx' --results-directory artifacts/fuzz/phase-a-completion-2-remaining-20261010-01 --verbosity quiet
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CampaignGatedWorkerTests|FullyQualifiedName~CampaignWorkerGateTests|FullyQualifiedName~CampaignBindingPolicyTests|FullyQualifiedName~CampaignTicketAdmissionTests' --logger 'trx;LogFileName=gated-worker-second.trx' --results-directory artifacts/fuzz/phase-a-completion-2-remaining-20261010-01 --verbosity quiet
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Campaign|FullyQualifiedName~DeterministicMutationTests|FullyQualifiedName~CorpusMutationTests|FullyQualifiedName~SmokeChildProcessTests|FullyQualifiedName~WorkerSupervisorTests|FullyQualifiedName~PlatformContractTests' --logger 'trx;LogFileName=focused-confirmed.trx' --results-directory artifacts/fuzz/phase-a-completion-2-remaining-20261010-01 --verbosity quiet
dotnet build tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --verbosity quiet
git diff --check
```

Earlier failed compilation attempts used the same corresponding filters with admission-first.trx / gated-worker-first.trx. The pre-refinement broader run used focused-final.trx. Evidence/logs are preserved under separate names.

Subsequent complete-suite runs used the same complete filter with focused-terminal.trx and focused-complete.trx. The corrected gate-only filter was `FullyQualifiedName~CampaignWorkerGateTests|FullyQualifiedName~CampaignGatedWorkerTests`, gate-terminal.trx. Tracked git diff --check and explicit trailing-whitespace inspection of all untracked/new and three touched tracked files passed. Permanent corpus git diff is empty. Entry comparison, final source/binary SHA-256 and status inventory are saved beside logs. No tests ran again merely to update this report.

**Pending permitted-context validation: Administrator PowerShell.** The agent cannot create either required symbolic-link fixture. The exact complete affected-suite rerun, including both unchanged fixture bodies, is below. An earlier command supplied focused-admin.trx before the final attachment refinement; preserve that result if already executed and use the fresh name for final-source evidence. Provide the resulting TRX/totals, not an assumed shell success.

```powershell
Set-Location C:\Development\PiratePDF
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Campaign|FullyQualifiedName~DeterministicMutationTests|FullyQualifiedName~CorpusMutationTests|FullyQualifiedName~SmokeChildProcessTests|FullyQualifiedName~WorkerSupervisorTests|FullyQualifiedName~PlatformContractTests' --logger 'trx;LogFileName=focused-admin-final.trx' --results-directory artifacts/fuzz/phase-a-completion-2-remaining-20261010-01 --verbosity normal
```

Even a passing privileged rerun validates only these seams and regressions. It does not complete the absent broker, independent parent, snapshot verifier, receipt or their failure matrix. Continue under this same authorized remaining-work scope from the precise boundary above; B9 stays OPEN.

## User validation follow-up

The user reported all 268 tests passed. Saved `focused-admin.trx` was inspected: 268 total/passed, zero failed; start 2026-10-10 18:28:46 EDT. It does not contain the subsequently added duplicate-ACK case. No `focused-admin-final.trx` is present. This resolves the earlier 268-case permitted-context result, but final-source 269-case permitted-context validation remains pending. Run the final command above; do not substitute this earlier result for the final source or overwrite its evidence.

Subsequently, the user reported all 269 tests passed. The saved `focused-admin-final.trx` was inspected: outcome Completed, 269 total/executed/passed, zero failed/skipped, including the duplicate-ACK regression case. Shell exit code was not supplied. Final-source permitted-context validation is now resolved; earlier failed and 268-case results remain preserved. Completion-2 still remains PARTIAL because the campaign broker, independent parent, authenticated snapshot verifier and immutable parent receipt are not implemented. B9 remains OPEN.
