# B9 Completion-2 Stabilization and Reassessment

Status: STABILIZATION VALIDATED / assessment complete. Completion-2 PARTIAL; B9 / POST-P2-002 OPEN.

Branch: maintenance/post-BF-1054-repository-review. HEAD: f51824dbc031b58497a38a6292ee11568b9ffeda.

Evidence: artifacts/fuzz/phase-a-completion-2-stabilization-20261010-01

Initial preservation checkpoint: 9 tracked modifications and 41 untracked files; prior 84-file inventory exists. Existing work and historical evidence preserved. Entry status, tracked diff, untracked inventory and hashes captured before validation. git diff --check passed (line-ending notice only).

Prior 269/269 is historical only. Partial broker hooks remain dormant under public dispatch. No feature development authorized.

## Preservation and exact inventory

Entry inventory records 901 file hashes, including historical ignored artifacts. `entry-status.txt`, `entry-untracked.txt`, and `entry-diff.patch` retain the exact pre-validation inventory and tracked patch. The 84-file previous final-integration inventory has no missing entries. Compared with that inventory, CampaignRunner.cs, CampaignEvidence.cs, and MutationWorkspace.cs differ; SupervisionResult.cs was previously clean and absent from that inventory, and its tracked diff adds the broker-reply constructor. New integration files are CampaignBrokerProtocol.cs, CampaignBrokerSchedule.cs, ICampaignWorkerExecutor.cs, and the Final-Integration report. None was discarded.

Entry tracked modifications (all pre-existing): docs/Documentation-Health.md, docs/Documentation-Index.md, docs/ValidationReport.md; tests/PiratePDF.Fuzz.Tests/SupervisorTestChild.cs; tools/PiratePDF.Fuzz.Runner/DeterministicMutation.cs, MutationWorkspace.cs, Program.cs, SupervisionResult.cs, WorkerSupervisor.cs. All 41 entry untracked paths are listed verbatim in entry-untracked.txt.

Post-validation comparison checked 455 captured source/document/script/text/JSON/TRX/log/patch files: all unchanged (`preservation-check.json`). Build products are not claimed immutable; the initial 901-file inventory retains their prior hashes. `final-source-hashes.json` records all 41 changed/untracked C# source hashes. No source or test edits, repairs, deletions, or Git history operations occurred. The only repository document added by stabilization is this report; all new validation files are in the evidence directory. Final status is saved separately. The full prior source/report/evidence inventory remains present.

## Fresh current-source validation

| Check | UTC completion | Exit | Result / evidence |
|---|---|---|---|
| Release fuzz test project build, --no-restore, quiet | 2026-10-11 00:42:40 | 0 | Zero warnings/errors; shell elapsed 10.294 s; build-first.log, build-first-result.json, build-command.txt. |
| Complete affected focused suite, Release, --no-restore, normal | 2026-10-11 00:43:20 | 1 | 269 executed, 267 passed, 2 failed, 0 skipped; shell elapsed 30.166 s; focused-first.log, focused-first-result.json, stabilization-focused.trx, test-command.txt. |
| Administrator focused rerun, same filter/current source | 2026-10-11 00:45:01 | Not supplied | TRX outcome Completed: 269 executed/passed, 0 failed/skipped; stabilization-focused-admin.trx. User reports all 269 passed. |
| git diff --check, before/after validation | 2026-10-11 | 0 | No whitespace errors; existing LF-to-CRLF advisory on DeterministicMutation.cs. |

Both failed tests stop in Directory.CreateSymbolicLink with `A required privilege is not held by the client`: CampaignRunnerTests.CampaignWorkspaceLinkIsRejected (line 245) and CampaignTicketAdmissionTests.TicketDirectoryLinkIsRejectedAndForeignBytesPreserved (line 121). Their validators did not run. These are environmental fixture failures, not demonstrated source regressions. Tests were neither weakened nor skipped. No minimal repair was justified or applied. No full release gate, substantive campaign, or open-ended test run occurred.

The fresh Administrator TRX was inspected after the user reported all 269 tests passed. Both previously blocked symlink cases passed. All 41 final-source hashes still match, and branch/HEAD are unchanged. A buildable, focused-test-passing stabilization baseline is established on current source; this is not Completion-2 acceptance. The earlier failed run remains preserved. Shell exit code was not supplied and is not inferred. The completed Administrator rerun command is retained below for provenance; no further rerun is needed:

```powershell
Set-Location C:\Development\PiratePDF
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Campaign|FullyQualifiedName~DeterministicMutationTests|FullyQualifiedName~CorpusMutationTests|FullyQualifiedName~SmokeChildProcessTests|FullyQualifiedName~WorkerSupervisorTests|FullyQualifiedName~PlatformContractTests' --logger 'trx;LogFileName=stabilization-focused-admin.trx' --results-directory artifacts/fuzz/phase-a-completion-2-stabilization-20261010-01 --verbosity normal
$LASTEXITCODE
```

## Routing and risks

Confirmed: Program's campaign-mutate dispatch calls CampaignCommand, which constructs CampaignRunner without an executor. The new broker schedule/protocol have no concrete client/server callers. The existing dedicated campaign-worker-internal entry remains separate. Public routing was not changed or activated in stabilization. Case-record serialization and the default workspace preparation refactor do affect the existing runner and are included in the current-source focused validation; dormant broker behavior is not thereby tested.

Confirmed missing: parent-owned campaign case execution/input verification and broker lifecycle; independent whole-child 75-second supervisor; authenticated immutable child snapshot verifier; separate immutable parent receipt. Worker gate/ticket tests do not establish these missing campaign contracts.

Untested concerns, not experimentally demonstrated regressions: repeated terminal scheduling/accounting may need idempotence; parent schedule starts its target clock before corpus/pool setup while child timing differs; EndAsync uses a later target lifetime token outside the inner cancellation handler, while the outer handler checks only the first token; interrupted atomic preparation can retain case.pending and report cleanup failure. No deletion or recursive cleanup shortcut was added. Broker reply coherence and replay/lifecycle admission also remain unproven. Passing existing tests does not validate the dormant hooks or resolve these concerns.

## Decision checkpoint

| Subsequent option | Scope / risk | Recommendation |
|---|---|---|
| Finish first-party Completion-2 later | High integration scope: concrete broker, exact ownership/cleanup, independent parent, snapshot verification, receipt, negative real-process matrix; dormant accounting/cancellation/staging need direct proof before activation. | Separate authorization and bounded dependency-ordered work; public dispatch stays unchanged until ownership/cleanup proof. |
| Freeze dormant first-party integration and prioritize existing SharpFuzz/libFuzzer/LLVM three-target coverage-guided campaigns | Lower new architecture burden because an accepted compatibility-smoke toolchain exists; substantive campaign budgets, provenance, controlled fault handling, finding retention/replay and coverage evidence still require preparation and authorization. | Preferred roadmap direction toward the actual B9 objective after the permitted-context stabilization result; preserve dormant files, do not revert them. |

No coverage, timing, equivalence, OS sandbox, OS-hard deadline, release acceptance, or campaign success is inferred. Completion-2 remains PARTIAL and B9 / POST-P2-002 remains OPEN. Stabilization is complete with current-source Release build and 269/269 permitted-context focused validation. Stop here; neither future option was implemented. Recommended next decision: freeze dormant first-party integration and separately authorize preparation for the existing coverage-guided toolchain, preserving all unfinished work and evidence.
