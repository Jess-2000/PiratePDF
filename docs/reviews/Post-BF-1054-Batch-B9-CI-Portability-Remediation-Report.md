# Post-BF-1054 Batch B9 - CI Portability Remediation Report

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-038 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1054 |
| **Last Updated** | 2026-10-05 |
| **Next Review** | 2026-12-28 |

## Disposition

The portability correction, explicit CI routing and local repository acceptance are complete. The final compiled catalog contains **474 cases / 182 methods**: **285 portable cases / 106 methods** and **189 Windows cases / 76 methods**. The agent's final portable selection passed **285/285 on Windows**, and the exact solution-level portable command passed **2,731/2,731**; these are not Linux execution claims. **The user confirmed that the passing unmodified authoritative Windows release gate included the final fuzz suite**, closing the current-tree acceptance question. This is user-reported validation; no final gate log or per-project count was supplied. Execution of the revised hosted CI jobs remains pending a future user-published revision. No Linux runtime/environment is available here: WSL reports not installed, and Docker is unavailable. No environment or toolchain was installed to work around that limitation.

**B9 / POST-P2-002 remains OPEN. No substantive coverage-guided campaign began.** No native driver, SharpFuzz instrumentation, compatibility-smoke rerun, general target expansion, other P2 item, canonical font editing or P1 optimization occurred. PP-AUD-029 through PP-AUD-037 remain historical evidence and are unchanged. This report does not retroactively claim that PP-AUD-036 or PP-AUD-037 established cross-platform CI.

## Starting state and evidence

The tree was clean on **develop**, at **652a51848212fbf0065e87dbb9a24cf155578a0a**, following maintenance merge **2552edc** and maintenance checkpoint **e27d386**. Root AGENTS.md, PROJECT-CHARTER.md, the current Charter, engineering/architecture context, PP-AUD-029 through PP-AUD-037 and the unmodified release gate governed this slice. No branch change, reset, clean, revert, commit, push, merge or tag occurred.

The complete failed CI job was read through the repository connector: [run 36364470233](https://github.com/Jess-2000/PiratePDF/actions/runs/36364470233), job **108748022135**, push of the checkpoint above. Ubuntu restore and Release build succeeded. The full test step failed; downstream DocGen and cleanliness steps therefore did not run. New local evidence is isolated in **artifacts/fuzz/ci-portability-20260928/**. The original job log, starting status/HEAD and hashes of the historical B9 reports are retained there. No earlier artifact directory was cleaned or recreated.

## Failure families and classification

Classes: **A** portable; **B** portable logic unnecessarily coupled to a Windows fixture; **C** intentionally Windows-specific integration; **D** explicitly unsupported Linux file-admission behavior. A failure on Linux alone is not evidence for C or D.

| CI failure family | Failed cases | Root cause and disposition |
|---|---:|---|
| FindingStorageTests | 44 | FindingFixture constructs FindingStore, and FindingFiles.Root refuses non-Windows before the assertion. Metadata, incoming grammar and budget policy are B; real admission, byte identity, locks, reparse checks and publication remain C. Shared portable policy tests now exercise those B contracts without constructing the store. Original retained-file assertions remain in Windows CI. |
| FindingReproductionTests | 13 | Same fixture failure. Outcome classification was already A (seven passing cases); agreement, discovery comparison and immutable result ownership are B and now directly tested through the shared assessment operation. Actual file rechecks, persisted ledger and supervised retained-input execution remain C. |
| FindingMinimizationTests | 7 | Same fixture failure. Metadata eligibility is B and extracted without changing four-worker verification. Real retained-candidate comparisons remain C. |
| FindingPromotionTests | 18 | Same fixture failure. Eligibility and choice of ordinary tooling versus engine/corpus regression are B and shared with portable tests. Validated persisted evidence, fresh reproduction and transactional staging remain C. |
| FindingCommandTests | 10 | Nine command-shape cases were B because the test constructed FindingFixture before validation. They now use only a portable owned temporary directory and assert pure command validation before executing the command. The real retain/reproduce file flow is C. |
| WindowsLibFuzzerArgumentsTests | 8 | Native-OS Path parsing rejected literal Windows paths on Ubuntu. All 19 cases in the class belong to C, including cases which happened to pass through rejection on Linux. Literal Windows transport/path syntax is unchanged. |
| SmokeChildProcessTests | 4 | B fixture error: host path was `/usr/share/dotnet/dotnet.exe`. Select the installed host name by OS, exactly as the existing supervisor fixture does. All seven cases remain A and actually launch/drain/cancel/reap managed controlled children; no native fuzz driver runs. |
| **Total** | **104** | **92 lifecycle fixture failures + 8 Windows-path failures + 4 managed host-path failures.** |

The earlier Linux fuzz-project result was 293 passed, 104 failed, 397 total. Some of those passes were tests returning after an unsupported-platform result, not execution of their intended assertions. This slice removes those early-return helpers in RunCommandTests, CorpusManifestTests, CorpusReplayTests and CorpusMutationTests. It does not preserve misleading pass counts as portability evidence.

### Complete routing inventory

Unmarked tests mean portable, and execute on both OS jobs. `[Trait(TestPlatform.Key, TestPlatform.Windows)]` marks only the Windows path/file integration route. No Fact is skipped and no class is excluded everywhere. The compiled `platform-catalog` command gives the per-method route and inline-row count; this table groups that exhaustive catalog by class.

| Test class | Portable cases | Windows cases | Contract classification |
|---|---:|---:|---|
| CorpusManifestTests | 43 | 10 | A owned recipes; B metadata/schema/order/length grammar now uses the same bounded metadata parser; C actual inventory, bytes, file-size admission and real symbolic link. |
| CorpusMutationTests | 5 | 30 | A command/budget/pre-cancellation/exit mapping; C admitted-file batches, temporary workspace, cleanup and byte preservation. |
| CorpusReplayTests | 9 | 29 | A usage/pre-cancellation/exit mapping; C complete file validation, rechecks and replay. |
| DeterministicMutationTests | 21 | 0 | A bounded byte transformations and deterministic identities. |
| FindingCommandTests | 10 | 1 | A exit mapping; B strict command shape; C actual retained-input command flow. |
| FindingMinimizationTests | 0 | 7 | C complete retained-input workflow; portable policy coverage is in FindingPolicyTests. |
| FindingPolicyTests | 71 | 0 | B metadata, resource bounds, admitted identity, bounded/consistent evidence, agreement, immutable discovery/results, minimization and promotion policy. |
| FindingPromotionTests | 0 | 18 | C complete staged publication and filesystem preservation; portable policy coverage is in FindingPolicyTests. |
| FindingReproductionTests | 7 | 13 | A supervisor classification; C retained-file ledger/recheck/worker integration. |
| FindingStorageTests | 0 | 44 | C physical store integration, including original metadata tampering assertions; portable policy coverage is in FindingPolicyTests. |
| FuzzResultTests | 9 | 0 | A bounded results and byte identity. |
| FuzzTargetTests | 18 | 0 | A target limits, cancellation and invocation contracts. |
| ManagedTargetTests | 9 | 0 | A actual managed filter/font production boundaries. |
| PlatformBoundaryTests | 2 | 0 | D explicit refusal before unsupported file admission; assertions execute on both OSes with their declared outcomes. |
| PlatformContractTests | 4 | 0 | A routing/catalog completeness and mandatory Windows security/path fixture ownership. |
| RunCommandTests | 15 | 18 | A usage/registry/version/pre-cancellation/output validation; C worker execution after actual regular-file admission. Target-level cancellation remains independently portable in FuzzTargetTests. |
| SharpFuzzTargetSessionTests | 6 | 0 | A managed callback/session initialization; no SharpFuzz/native execution. |
| SmokeChildProcessTests | 7 | 0 | A real bounded managed-process tests after B host-path correction. |
| TargetRegistryTests | 9 | 0 | A target identity, order and registration. |
| WindowsLibFuzzerArgumentsTests | 0 | 19 | C literal Windows paths/argument transport, unchanged production policy. |
| WorkerSupervisorTests | 40 | 0 | A protocol, limits, process ownership and cleanup; two real-worker cases also assert D UnsupportedInputPlatform on non-Windows. |
| **Total** | **285** | **189** | **474 = 397 preserved cases + 71 policy + 4 routing + 2 platform-boundary cases.** |

## Implementation and preserved boundaries

FindingValidation extracts the existing source/metadata checks, canonical incoming grammar, content identity and bounded retention predicate from FindingStore. The real store calls the same checks. Its portable Inspect operation accepts already supplied bytes, enforces the existing 8 KiB metadata/64 KiB maximum artifact bounds before parsing/hashing, and returns the existing bounded result. It does not open a file or grant filesystem admission. Registered ceilings are still enforced. Tests retain the original on-disk metadata-tampering cases as integration coverage and add their portable equivalents.

FindingReproduction.Assess contains the existing bounded agreement, outcome, exit mapping and discovery comparison. It now admits only validated finding metadata and two correctly numbered, closed-classification attempts with consistent exit/failure codes before constructing a bounded result. FindingResult still snapshots the attempts into a read-only collection. FindingMinimization.CanCompare and FindingPromotion.IsEligible express necessary metadata policy only; neither authorizes publication or bypasses validated stored evidence. File-based entry points still require the original ledger, hashes, input leases, two fresh workers, matching classification, explicit decision and staged proposal validation. The early uncharacterized-finding refusal remains before reading the ledger, preserving failure precedence.

FindingCommand.IsValidInvocation validates the complete positional command before constructing a Windows store. Invalid command shapes now fail independently of platform. Valid command filesystem admission remains unchanged. Host, worker, destination and resource policy still cannot come from input metadata. CorpusValidator.ValidateMetadata reuses the existing closed parser with its 128 KiB byte ceiling. ValidateAsync calls it before the unchanged inventory/hash checks; metadata-only success explicitly does not assert safe files or matching bytes.

No production Engine/SDK public API, preservation behavior, project reference, friend declaration, NuGet/native/runtime project dependency, target/oracle, corpus byte, exception allowlist or toolchain version changed. Narrow public **tooling** operations were added to the existing runner so the same actual policy is testable without granting a trusted buffer or filesystem bypass. Existing domain failure classes remain; unexpected defects are not reclassified as malformed input. The separately documented upload-artifact action is the only new CI action dependency.

InputFile and FindingFiles.Root remain unchanged. Windows-local path, traversal, device/ADS, ancestor-reparse and regular-file checks, bounds before allocation, read leases, process deadlines, byte-capped pipes, worker protocol, tree cleanup, provenance and promotion requirements remain intact. Non-Windows file admission remains intentionally unsupported because the current BCL checks cannot exclude a blocking Unix FIFO before opening it. This is a tooling boundary, not a claim that BlackFlag Engine or its managed byte targets are Windows-only.

BCL reparse checks remain conservative snapshots, **not an atomic filesystem sandbox**, do not prove protection against concurrent parent substitution, and do not distinguish hard links. The owned-workspace trust assumption remains. A passing real-link fixture establishes rejection of that observed escape, not broader race-free filesystem containment.

## CI topology and future test additions

Previously one ubuntu-latest job attempted the complete solution, including Windows-only fuzz contracts, then DocGen. The revised workflow has two independent jobs:

* **Portable Build, Test, and Validate**: ubuntu-latest; unchanged .NET 10 setup and Skia prerequisites; full solution restore/Release build; solution tests with `Platform!=Windows`; coverage verification; DocGen Release artifact/all/health/repair; tracked generated-file, unexpected-untracked-file and whitespace checks. DocGen is owned by this job only.
* **Windows Build and Test**: windows-latest/.NET 10; full solution restore/Release build; the complete solution suite without a platform filter; verification of all **474 fuzz cases / 182 methods** within that run; whitespace and tracked/untracked cleanliness checks. Both real symbolic-link fixtures are selected and invoked unconditionally, and any failure fails the job; actual hosted execution remains pending.

Both jobs retain 20-minute deadlines, read-only repository permissions, concurrency cancellation, Release configuration, no-build/no-restore test discipline and no failure suppression. There are no job dependencies or secrets. SharpFuzz remains outside the ordinary release dependency graph and neither job performs instrumentation or a campaign.

TestPlatform.Catalog rejects ambiguous/unknown platform metadata, skipped facts, or new dynamic data mechanisms that the count check cannot yet enumerate. Default-portable tests run on both OSes; new Windows fuzz tests use the single shared trait convention. Test-PlatformCoverage.ps1 compares each expected method/inline-row count against actual passed TRX records. Missing, skipped, duplicated, extra or misrouted fuzz cases fail the check. Both routes must be nonempty. A repository source check rejects the established platform-trait forms outside PiratePDF.Fuzz.Tests until the catalog is deliberately extended. Independently, the unfiltered Windows full-solution run ensures a future platform-tagged test outside the fuzz project is still executed rather than being lost through the Ubuntu filter. This verifies actual fuzz execution rather than merely matching a nonzero aggregate total.

The workflow adds the official MIT-licensed [actions/upload-artifact v7 action](https://github.com/actions/upload-artifact) for failed-job TRX retention, limited to the explicit TestResults/OS directory and seven days. Before either failure upload, an independent failure-path gate admits at most **64 TRX files**, at most **16 MiB each** and at most **64 MiB aggregate**; a bounds failure suppresses the upload without deleting or truncating local evidence. No corpus, finding payloads, native artifacts, inputs or environment dumps are uploaded. This is a new CI action use, not a change to the approved fuzz or production dependencies. The existing checkout v7 and setup-dotnet v6 actions are unchanged.

The YAML was structurally reviewed for job ownership, indentation, folded commands, filters, PowerShell/bash shell selection, permissions, independent failures, test routing and cleanliness. Hosted execution of the changed workflow remains pending; local Windows selection is not substituted for that result.

## Symbolic-link validation and hosted Windows

Before the final 13 portable boundary cases were added, the first Windows-category run was **187 passed, 2 failed, 0 skipped, 189 total** and the complete agent-run suite was **459 passed, 2 failed, 0 skipped, 461 total**. The two failures were exactly CorpusManifestTests.SeedLinkEscapeIsRejected and FindingStorageTests.ArtifactLinkEscapeIsRejected, during File.CreateSymbolicLink with **A required privilege is not held by the client**, before either validator ran. An outside-sandbox focused diagnostic also failed both fixtures for the same reason. No test was skipped, weakened or replaced with a fake link.

The user subsequently reported **all then-current 461 tests passed** in response to the requested Administrator PowerShell validation. The saved **admin/complete-admin.trx** independently records 461 passed, zero failed/not-executed; the then-current catalog coverage checker also verified all 180 methods and 461 cases, including both real links. This remains historical user-performed permitted-context evidence. The user also reported a passing unmodified release gate and, on **2026-10-05**, confirmed in response to the final 474-case validation question that the gate included the fuzz suite. This closes the final-tree validation blocker. The final gate log and per-project count were not supplied, so the report does not present a new independently parsed **474/474** TRX claim. Earlier failed evidence remains retained.

[GitHub's hosted-runner documentation](https://docs.github.com/en/actions/reference/runners/github-hosted-runners) states that hosted Windows VMs run as administrators with UAC disabled. That supports expecting ordinary Windows CI steps to create the fixtures without an elevation command. **It does not empirically establish the result for this new job.** The new Windows job runs both fixtures without policy changes or skip conditions; actual hosted privilege behavior remains to be confirmed by its first execution. A hosted privilege failure must remain a visible failure and be characterized before acceptance.

## Validation ledger

| Validation | Actual result |
|---|---|
| Initial focused policy/routing/command correction | **71 passed, 0 failed, 0 skipped**; policy-focused.trx. |
| Final finding-boundary focus | **91 passed, 0 failed, 0 skipped**; boundary-focused/boundary-focused.trx. This includes the final invalid-identity and unbounded/inconsistent-evidence cases. |
| Initial portable selection | **270 passed**, before the two explicit platform-boundary additions. |
| Intermediate portable selection on Windows | **272 passed, 0 failed, 0 skipped**; portable-final/portable-final.trx, before the final 13 policy-boundary cases. |
| Final portable selection on Windows | **285 passed, 0 failed, 0 skipped**; portable-boundary-final/portable-boundary-final.trx; catalog **106 methods**. This is not Linux runtime evidence. |
| Solution-level portable CI command on Windows | **2,731 passed, 0 failed, 0 not executed** across 21 TRX files; the catalog verified all **285 expected portable fuzz cases / 106 methods**. This validates the exact solution filter, TRX fan-out and coverage-check path, but is not Linux runtime evidence. |
| Windows-only selection before final portable additions | **187 passed, 2 fixture-creation failures**, no skips; the user-performed then-current full pass subsequently covered all 189 Windows cases. |
| Historical complete fuzz suite | Agent **459/461**, then user **461/461**, no skips; complete/complete.trx and admin/complete-admin.trx. This predates the final 13 portable cases. |
| Historical catalog versus permitted-context TRX | **461 passed cases / 180 methods**, both then-current routes accounted for. The final source catalog is **474 cases / 182 methods**. |
| Runner Release build | Final-tree build passed, **0 warnings, 0 errors**. |
| Targets Release build | Final-tree build passed, **0 warnings, 0 errors**. |
| SharpFuzz build/native smoke | Not run: no glue/session, instrumentation, transport or native implementation changed. |
| Direct replay CLI | Final-tree rerun exit **0**; **15 admitted, attempted, executed and matched; 0 mismatched**. Earlier replay.json is retained. |
| Direct deterministic mutation CLI | Final-tree rerun exit **0**; deterministic-v1/fixed seed 1049, per-seed 1; **15 admitted/attempted/completed, 15 expected rejections, 0 failures**. Earlier mutate.json is retained. |
| Linux/hosted CI | Not executed locally; revised jobs await a user-published revision. No WSL/Docker environment was installed. |
| Authoritative Windows release gate | **Passed, user-reported; final fuzz-suite inclusion confirmed on 2026-10-05.** The earlier pre-final-tree failed attempt recorded **2,905 passed, 2 failed, 0 skipped** and stopped solely because that shell lacked symbolic-link privilege before either validator ran. The user supplied the final status and confirmation without a gate log or count. |
| DocGen / generated files | Passed: **205 documents, 0 validation errors/warnings**, health **91.6% / A-**, repair preview **0 changes/files/diagnostics/warnings/errors**. Generated tracked indexes were updated. |
| Workflow structure | YAML parsed successfully; two independent bounded jobs, explicit filters/count checks, cross-project trait guard, read-only permissions, one DocGen owner and failure-only seven-day TRX retention verified. Bounds-only positive and negative checks confirmed zero/single/multiple-file handling and rejection above the file-count, per-file and aggregate ceilings. Hosted execution remains pending. |
| git diff --check | Passed after documentation generation and final report update. |

One initial compile of the new catalog failed because xUnit v2 TraitAttribute does not expose Name/Value properties. The catalog now reads its constructor metadata through reflection; subsequent build and routing tests passed. A later local test build overlapped the user's manual suite and could not replace a loaded runner assembly (MSB3026 retries, MSB3027/MSB3021). No process was killed or artifact deleted; after the user run ended the build and then-current 272-test portable run succeeded. The final boundary-hardening pass later expanded and passed the portable route at 285 cases. Neither event is hidden as a target rejection.

Direct CLI commands used the Release Runner DLL, explicit repository root **C:\Development\PiratePDF**, replay, and mutate with **--algorithm deterministic-v1 --per-seed 1**. They launched bounded neutral managed workers only. The 15 permanent seed bytes, manifest and oracles remain unchanged.

## Exact files changed

The review tree contains exactly **27 changed files**: 23 implementation/workflow/test-infrastructure files, this report and three reproducible DocGen outputs:

1. .github/workflows/continuous-integration.yml
2. tools/PiratePDF.Fuzz.Runner/CorpusValidator.cs
3. tools/PiratePDF.Fuzz.Runner/FindingCommand.cs
4. tools/PiratePDF.Fuzz.Runner/FindingPromotion.cs
5. tools/PiratePDF.Fuzz.Runner/FindingReproduction.cs
6. tools/PiratePDF.Fuzz.Runner/FindingStore.cs
7. tools/PiratePDF.Fuzz.Runner/FindingValidation.cs (new)
8. tests/PiratePDF.Fuzz.Tests/CorpusManifestTests.cs
9. tests/PiratePDF.Fuzz.Tests/CorpusMutationTests.cs
10. tests/PiratePDF.Fuzz.Tests/CorpusReplayTests.cs
11. tests/PiratePDF.Fuzz.Tests/FindingCommandTests.cs
12. tests/PiratePDF.Fuzz.Tests/FindingMinimizationTests.cs
13. tests/PiratePDF.Fuzz.Tests/FindingPromotionTests.cs
14. tests/PiratePDF.Fuzz.Tests/FindingReproductionTests.cs
15. tests/PiratePDF.Fuzz.Tests/FindingStorageTests.cs
16. tests/PiratePDF.Fuzz.Tests/RunCommandTests.cs
17. tests/PiratePDF.Fuzz.Tests/SmokeChildProcessTests.cs
18. tests/PiratePDF.Fuzz.Tests/SupervisorTestChild.cs
19. tests/PiratePDF.Fuzz.Tests/WindowsLibFuzzerArgumentsTests.cs
20. tests/PiratePDF.Fuzz.Tests/FindingPolicyTests.cs (new)
21. tests/PiratePDF.Fuzz.Tests/PlatformBoundaryTests.cs (new)
22. tests/PiratePDF.Fuzz.Tests/TestPlatform.cs (new)
23. tests/PiratePDF.Fuzz.Tests/Test-PlatformCoverage.ps1 (new)
24. docs/reviews/Post-BF-1054-Batch-B9-CI-Portability-Remediation-Report.md (new)
25. docs/Documentation-Health.md (generated)
26. docs/Documentation-Index.md (generated)
27. docs/ValidationReport.md (generated)

## Remaining acceptance and next action

The completed working tree is ready for review, with local acceptance established by the user-confirmed release-gate pass. Both revised CI jobs must execute when the user chooses to publish a revision; no commit or push is authorized here. Hosted Ubuntu execution and hosted Windows real-link capability remain explicit limitations. No further manual local validation is required for this slice. Do not start the substantive B9 campaign or another maintenance item as part of this remediation. B9 / POST-P2-002 remains open independently of this CI remediation.

## Revision history

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-09-28 | Characterize all 104 post-merge CI failures; separate portable policy from Windows file admission; preserve original tests, add routing/count enforcement, record fresh 461-case permitted-context evidence and pending hosted/gate acceptance. |
| 0.2 | 2026-10-05 | Reconcile final 474-case routing, bounded finding-boundary hardening, 2,731-case portable solution evidence and full-solution Windows CI ownership; preserve the reported gate pass while requesting confirmation that it covered the final boundary changes. Hosted Ubuntu/Windows execution remains a post-publication validation step. |
| 1.0 | 2026-10-05 | Record the user's confirmation that the passing unmodified release gate included the final fuzz suite; close local acceptance while preserving historical failures, evidence attribution, hosted CI limitations and open overall B9 status. |
