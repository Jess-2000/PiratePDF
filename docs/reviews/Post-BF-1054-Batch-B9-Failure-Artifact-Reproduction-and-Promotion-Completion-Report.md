# Post-BF-1054 Batch B9 - Failure Artifact Reproduction and Promotion Report

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-037 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1054 |
| **Last Updated** | 2026-09-27 |
| **Next Review** | 2026-12-27 |

## Disposition

**The bounded finding-lifecycle slice is complete.** The user reported **All 397 tests passed** after the requested permitted-context rerun, establishing **397 passed, 0 failed, 0 skipped**, including both real Windows symbolic-link fixtures. This is user-performed validation, not an agent-executed privileged run. The earlier agent-executed **395 passed, 2 failed, 0 skipped, 397 total** result remains historical evidence: both failures occurred during fixture creation because Windows denied the symbolic-link privilege, before either validator ran. No test was removed, skipped or weakened. The earlier accepted 297/297 result is historical baseline evidence, not substituted for this slice's fresh validation.

**B9 / POST-P2-002 remains OPEN.** No substantive coverage-guided campaign, native driver execution, new instrumentation, general target expansion, CI integration, other P2 item, canonical font editing or P1 work occurred. PP-AUD-036 v1.0 remains the accepted compatibility-smoke result, including its sanitizer-warning limitations. No production defect was found. All discoveries exercised here are explicitly owned synthetic fixtures or a copy of the earlier controlled harness-probe artifact. No permanent regression or corpus seed was promoted.

## Starting state and preserved evidence

Branch throughout: `maintenance/post-BF-1054-repository-review`. HEAD throughout: **93865f639acf96f0b9598c97b04d49cea38e8260**. Root AGENTS.md, PROJECT-CHARTER.md, PP-AUD-029 through PP-AUD-036 and the current target, worker, supervision, corpus, replay, mutation and smoke contracts govern this slice.

The initial lifecycle attempt began with **91 modified/untracked files** (seven tracked modifications and 84 untracked files). It added nine source/test files and modified three existing B9 files. One Runner build failed with CA1822 on TargetIdentity; the property was made static, but no subsequent validation ran before that attempt stopped. The completion report did not yet exist. Its inventories remain in **artifacts/fuzz/lifecycle-20260927/**. A read-only recovery confirmed the partial state and all **455 historical evidence files** were unchanged.

This continuation began with **100 modified/untracked files** (seven tracked modifications and 93 untracked files). New evidence uses **artifacts/fuzz/lifecycle-validation-20260927/**. The continuation's worktree-before.json and status-before.txt record its exact starting tree; previous-lifecycle-before.json records hashes of the earlier lifecycle ledgers. No old evidence directory was reused. The historical toolchain-smoke-20260925, toolchain-smoke-20260925-transport and initialization-20260925 areas are preserved. No reset, clean, revert, branch change, commit, push, merge or tag occurred.

## Lifecycle and operator authority

The implemented states are separate: an explicitly supplied incoming artifact; an immutable retained discovery; numbered reproduction evidence; a repeatable current classification (with a separate comparison to discovery); optional comparison to a smaller retained candidate; and an explicit review proposal. Refused, inconsistent, non-reproducing and expected-rejection candidates remain retained without promotion. There is no automatic permanent-write command or automatic oracle update.

`FindingStore` handles admission and identity. `FindingReproduction` uses the existing WorkerSupervisor and fresh managed workers. `FindingMinimization` compares one explicitly chosen smaller retained candidate. `FindingPromotion` stages a proposal. CLI launch configuration derives the installed dotnet host and this Runner assembly exactly as replay does. Artifact bytes, names and metadata cannot select an executable, shell, target implementation, output destination or resource policy. Target/configuration selection is operator-supplied metadata validated against the unchanged real registry, never extracted from a crash filename.

All three existing targets retain configuration 1: filter-asciihex and filter-flate at 16 KiB, and font-sanitize at 64 KiB. No target registration, production API, trusted buffer access, exception allowlist, dependency, friend declaration or ordinary project graph changed.

## Metadata and storage

Discovery schema: **piratepdf-fuzz-finding**, version **1**. Every constructor field is required. Unknown or duplicate JSON properties, invalid types, unsupported schemas/versions, unknown enum values, numeric enum encodings, unregistered targets, configuration mismatches, inconsistent limits, malformed hashes and unsupported provenance fail closed. JSON has a depth ceiling of six. Immutable records contain:

* Run/finding IDs, target/configuration, exact length, uppercase SHA-256 and registered input ceiling.
* Kind (Crash, Timeout, UnexpectedResult or ControlledFixture) and observed closed outcome class.
* Origin (OwnedGenerated, OwnedSeedMutation or ControlledProbe), bounded origin ID, parent SHA-256, related finding ID and optional nonnegative deterministic seed. Unreviewed origin is refused before retention.
* A closed toolchain profile (neutral-net10-v1 or pp-aud-036), 40-character lowercase source revision and worker/target assembly SHA-256 supplied by the operator.
* Fixed rights statement `repository-owned-no-external-license-asserted` and initial reproduction/minimization/promotion states. These discovery fields never change in place; later records/proposals describe later states.

These are provenance assertions in an operator-controlled workspace, not signatures or an automatic license/root-cause verification service. An ID/hash alone does not prove ownership. Future operators must retain the source recipe/campaign record identified by origin ID and parent hash and exclude private/user documents, downloaded unknown-license material, system fonts and other external content. The rights statement preserves PP-AUD-033 and the Charter's separate license decision; it does not invent an MIT license grant.

Canonical candidate storage is `artifacts/fuzz/findings/<run-id>/<finding-id>/input.bin` plus `metadata.json` and at most four numbered `reproduction-01.json` through `reproduction-04.json` records. An explicit source admission path is limited to `artifacts/fuzz/incoming/...`; no directory discovery is used to select an incoming input. A libFuzzer filename is just an untrusted source name: the tool calculates length/hash itself and assigns the operator-selected canonical finding identity.

Retention obtains a cooperating exclusive run lock, validates existing candidates and the complete permanent corpus, detects duplicates, writes a new `pending` directory and renames it to the final finding directory only after byte verification. Existing candidate/pending files fail closed. Partial owned output is cleaned through explicit filenames and nonrecursive directory removal; unknown contents are not recursively removed. A failed cleanup remains infrastructure failure. The fixed empty active.lock file is reusable synchronization metadata, not discovery evidence.

## Hard bounds

| Resource | Limit |
|---|---|
| Payload | Actual registered ceiling, additionally at most **65,536 bytes** |
| Findings in one selected run | **16**; no enumeration of other runs |
| Aggregate retained original payload in one run | **262,144 bytes** |
| Discovery/source metadata | **8,192 bytes** each before parsing |
| Reproduction record | **4,096 bytes**; at most **4 create-new records per candidate**, never replaced |
| Reproduction attempts | **2 fresh workers per operation**, at most **8 per retained candidate** across its ledger |
| Run directory inventory | At most **18 entries**: 16 candidates, fixed lock and one proposal |
| Candidate inventory | At most **6 files**: input, metadata and four evidence records |
| IDs / related reference | At most **64 characters**; restricted ASCII; canonical lowercase path IDs |
| Repository-root argument | At most **1,024 characters**, absolute local supported path |
| Incoming relative path | At most **256 characters**, 2-4 components including incoming, each at most 96 characters |
| Parsed JSON collections | At most **128 elements**; domain limits are smaller where required |
| Console result | At most **1,536 UTF-8 bytes** by bounded field construction and tests; no raw input/output/path/message |
| Reduction comparison | At most **4 worker executions / 262,144 input bytes**; one original and one smaller retained candidate |
| Proposal per run | **1**, create-new; no overwrite |
| Staged corpus | Existing **128-seed / 128 KiB manifest / 256 filesystem-entry** validator limits; at most 128 x 64 KiB payload (8 MiB), plus manifest and 8 KiB proposal |
| Supervision | Unchanged **10-second case deadline**, independent **5-second cleanup**, **64 KiB capture per pipe**, strict **512-byte worker protocol** |
| Managed memory configuration | Unchanged **256 MiB GC heap setting** and mini-dump setting 0; not total-process memory containment |

Thus retained metadata/evidence is also finite: at most 16 x (8 KiB + 4 x 4 KiB), separate from the 256 KiB original-payload cap. The single proposal is separately bounded as above. Incoming operator staging is not an automatically managed campaign collection: this slice reads one explicit bounded file, not all files in incoming. Operators choose run lifetimes; there is no unlimited automatic retention loop or run scan.

## Integrity, reproduction and classification

Each reproduction validates storage/metadata and rereads the input, verifies length/hash, re-resolves the target and checks its ceiling before each worker launch. A read-only sharing handle is held through consumption, reducing ordinary Windows write/delete races. Metadata is compared with the initial immutable record between workers. Any identity change fails before the next launch; original discovery bytes/metadata remain unchanged.

Two attempts must agree in both classification and fixed worker code. Numbered evidence records bind attempt numbers to run/finding/hash/length/target/configuration and actual worker/target assembly hashes used by the trusted supervisor, under neutral-net10-v1. Evidence validation also checks code/classification consistency. A stable outcome different from discovery has `agrees: true`, `matchesDiscovery: false`, and **NonReproducible exit 22**; it does not confirm the discovered failure. Alternating outcomes are NonReproducible. There is no indefinite retry. Existing, malformed or exhausted evidence slots fail closed rather than overwrite history.

| Classification | Interpretation |
|---|---|
| Success / ExpectedRejection | Valid worker TargetResult with exit 0 / 1; expected malformed input is not a defect |
| UnexpectedManagedFailure | Valid UnexpectedTargetFailure record, exit 4; requires later cause analysis, not automatic engine attribution |
| WorkerProtocolFailure / AbnormalExit | Invalid protocol / process exit outside worker contract |
| Timeout / Cancellation | Supervisor deadline / explicit caller cancellation; distinct, never rejection |
| CleanupFailure | Supervisor termination/reap failure; infrastructure failure takes precedence |
| IntegrityFailure | Admission/metadata/length/hash mismatch, including before the next launch |
| NonReproducible | Inconsistent repetitions; also a distinct exit when stable current behavior disagrees with discovery |
| Duplicate | Exact SHA-256 duplicate established against retained candidates in the selected run or permanent corpus |
| Indeterminate | Start/capture/input validation, spontaneous worker cancellation or other evidence insufficient for attribution |

Ordinary console JSON includes only fixed status/exit, bounded identities, length/hash and at most two classification/code records. It does not include raw payload, stdout/stderr, exception messages, stack traces, environment, credentials, memory addresses or absolute paths. The existing worker remains responsible for exception capture and process isolation; no new exception is accepted as malformed input.

## Reduction and deliberate promotion

There is **no general minimizer or external minimization campaign**. `compare-minimization` accepts one already retained, strictly smaller candidate with the same target/configuration, the same nonempty related finding ID and a parent hash equal to the original. Both originals must declare UnexpectedManagedFailure, and both must independently reproduce that class and the same fixed code twice. Expected rejection, success, an unrelated finding, wrong parent, larger input or inconsistent behavior refuses reduction. It creates no transient candidate files; original and derived evidence remain separately retained. Class preservation does **not** prove the same root cause; human characterization remains required before describing the smaller input as the defect's semantic minimization.

Promotion is deliberately a **staged proposal**, not a permanent write. Decisions are OrdinaryEngineRegression, OrdinaryToolingRegression, CorpusFixedSuccess and CorpusFixedRejection. A prior valid agreeing failure ledger, related finding identity and fresh agreeing execution are mandatory. Ordinary engine proposals require UnexpectedManagedFailure and exclude controlled-probe origin; tooling failures belong in tooling tests. Success/rejection alone cannot become a defect proposal. Explicit fixed-success/fixed-rejection corpus decisions require historical unexpected managed failure, acceptable owned provenance, non-probe/non-controlled kind and two fresh results matching the selected post-fix oracle. A refusal never rewrites the retained discovery or corpus oracle.

Byte-oriented parser/filter regressions can justify corpus proposals. Semantic state/transaction/API behavior can justify an ordinary unit/integration test. Harness/toolchain defects justify tooling tests, not production corpus; controlled timeout/crash probes are never attributed automatically to PiratePDF. A changed hash with an apparently similar failure is not automatically called the same root cause.

Corpus proposal staging copies the complete currently validated corpus, rechecks each copied identity, adds exactly one canonical seed entry, orders paths ordinally and validates the **entire proposed corpus** using the existing schema/validator. Existing IDs/hashes, oversized/invalid corpus state, bad provenance or wrong oracle refuse. Only after validation does the owned proposal-work directory become proposal. The proposal records provenance, prior reproduction, explicit decision/oracle and reviewRequired=true/permanentWritePerformed=false. An ordinary regression proposal contains recommendation metadata without manufacturing a C# test or corpus payload.

No two-file transaction is claimed against the permanent manifest/seed pair: the implementation never writes them. A future explicitly reviewed integration must preserve these validations. Tests prove validation failure and corpus changes during execution leave no partial published proposal and remove owned intermediate staging. Existing foreign staging is preserved and reported, not deleted.

## CLI

All options below are mandatory in the shown order; unknown, duplicate, missing or extra options fail. Root is explicit and absolute; run/finding/seed IDs are bounded. No arbitrary host/worker option exists.

```text
retain-finding --repository-root <root> --run <run-id> --finding <id> --input incoming/<file> --source incoming/<source.json>
reproduce-finding --repository-root <root> --run <run-id> --finding <id>
compare-minimization --repository-root <root> --run <run-id> --finding <original> --smaller <derived-id>
propose-promotion --repository-root <root> --run <run-id> --finding <id> --seed <seed-id> --decision <PromotionDecision>
```

Invoke with `dotnet tools/PiratePDF.Fuzz.Runner/bin/Release/net10.0/PiratePDF.Fuzz.Runner.dll`. Exit mapping: **0 Success; 20 InvalidInvocation; 21 IntegrityFailure; 22 NonReproducible; 23 ReproducedFailure; 24 MinimizationRefused; 25 PromotionRefused; 26 Cancelled; 27 InfrastructureFailure; 28 Duplicate; 29 LimitExceeded**. Native/child exit values are not forwarded as lifecycle exit codes. Expected rejection can be successful reproduction without being a promotion-eligible defect.

## Filesystem and process limitations

InputFile admission remains Windows-local-only and fails closed on unsupported platforms/forms. Canonical IDs/expected filenames, regular-file checks, bounded reads, ancestor reparse checks, handle attributes and create-new publication reject observed traversal, UNC/device/ADS forms, directories and link escapes. No recursive artifact discovery or arbitrary-directory cleanup is performed.

**BCL reparse checks are conservative snapshots, not an atomic filesystem sandbox. They do not prove protection against concurrent parent substitution and do not distinguish hard links.** Read sharing reduces ordinary writes during execution but does not make verification-to-worker consumption an atomic filesystem transaction. Same-trust local workspace and trusted installed runtime/build outputs remain assumptions; SHA-256 identity is not a signature. Persisted metadata/evidence is validated, not cryptographically authenticated against a malicious workspace owner.

The unchanged WorkerSupervisor limitations apply: finite BCL tree-kill/root-reap/pipe cleanup does not guarantee discovery or termination of all inaccessible or detached descendants. OS calls/filesystem operations are not real-time guarantees. The managed heap setting excludes native memory/stacks/mappings. Non-Windows file execution and portable CI readiness are not claimed. The successful permitted-context real link fixtures establish only their tested rejection paths; they do not broaden these filesystem claims.

## Validation and corrective evidence

| Check | Actual result |
|---|---|
| Initial continuation Runner Release build | Passed, 0 warnings/errors; confirmed previous CA1822 correction |
| Initial finding-test compilation | Failed CS0246: missing System.Threading.Tasks import in the partial FindingFixture; fixed locally |
| Focused FindingStorageTests | **43 passed, 1 failed, 0 skipped / 44**, solely link fixture creation privilege; storage-second.trx |
| Focused reproduction/reduction/promotion/command tests | **54 passed, 0 failed, 0 skipped** before two additional promotion guards; lifecycle-focused-first.trx |
| Final focused promotion tests | **18 passed**, including contradictory evidence refusal and cleanup after corpus changes during staging; promotion-final.trx |
| Earlier complete continuation run | **393 passed, 2 failed / 395**, before those two additions; complete-first.trx preserved |
| Fresh final complete Fuzz.Tests Release | **395 passed, 2 failed, 0 skipped / 397**; complete-final.trx |
| Final permitted-context validation | User reported **All 397 tests passed**: **397 passed, 0 failed, 0 skipped**, including both real link fixtures; no separate focused manual count inferred |
| New lifecycle reconciliation | **100 cases**: storage 44, reproduction 20, reduction 7, promotion 18, command 11; 99 passed and one link-creation failure |
| Existing tests retained | All **297** remain: 296 passed, original corpus link-creation failure; no removal/discovery loss |
| Affected existing complete-suite groups | Replay **38/38**, mutation **56/56**, WorkerSupervisor **40/40**, session **6/6**, transport **19/19**, smoke-process **7/7**; manifest **52/53** with link fixture blocker |
| Runner / Targets Release builds | Passed, **zero warnings/errors**, --no-restore |
| SharpFuzz build/native smoke | Not repeated: no change to glue, instrumented target assembly, transport or native smoke path |
| Direct replay CLI | **15 admitted / 15 executed / 15 matched**, exit 0 |
| Direct mutation CLI | **15 admitted / 15 completed / 15 expected rejections / 0 failures**, exit 0; mutation-work absent afterward |
| Direct finding CLI | Three create-new retentions exit 0; success and rejection each agree over two real neutral workers, exit 0; preserved controlled native artifact returns exit 22 with stable ExpectedRejection different from discovery |
| Direct proposal CLI | Non-defect expected-rejection candidate refused with **exit 25**; positive staged corpus and ordinary-tooling proposals validated by focused integration tests in owned temporary repositories |
| git diff --check | Passed; final untracked-file checks and scope reconciliation recorded below |
| Production/integration/full release gate | Not required/run: no production implementation/API or ordinary dependency graph changed; overall B9 remains open |

Commands include `dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore` with the named focused filters, and the complete command with `--no-build --logger 'trx;LogFileName=complete-final.trx' --results-directory artifacts/fuzz/lifecycle-validation-20260927`. Builds use `dotnet build <project> -c Release --no-restore --verbosity quiet`. Direct replay uses `replay --repository-root C:\Development\PiratePDF`; mutation adds `--algorithm deterministic-v1 --per-seed 1`.

The retained Validate-Cli.ps1 is a finite evidence fixture, not a campaign: it creates cli-repository beneath this new evidence root, copies the existing 15-seed corpus, supplies owned source descriptors and invokes the real CLI. Each command/exit/result is separately retained; cli-identities.json records hashes/lengths. Success input F0> and malformed G0> are tiny owned literals. The native-probe input is a copy of the old controlled harness's **zero-byte** artifact, SHA-256 **E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855**. Its filename did not become a classification or destination. It is still a controlled harness fixture, not a newly discovered engine bug. The driver was not executed.

Review/testing corrected only lifecycle tooling issues: evidence replacement became a create-new four-record ledger; stable changed outcomes no longer confirm the discovery; code/classification contradictions are rejected; between-worker metadata changes stop the next launch; staging rechecks detect corpus changes and clean owned partial output. Controlled regressions exercise the corrected contracts. There was no engine discrepancy, new malformed-input exception allowance or third-party defect, and **no dependency update**.

Accepted toolchain remains .NET SDK **10.0.302** (rechecked), SharpFuzz **2.3.0**, SharpFuzz.CommandLine **2.3.0**, SharpFuzz.Common **2.2.0**, dnlib **4.4.0**, libfuzzer-dotnet **v2025.05.02.0904**, LLVM/Clang **23.1.2**. Provisioned tools/packages were not installed, upgraded, downgraded or replaced. The accepted Windows driver remains 1,512,448 bytes, SHA-256 **17AF5B3F6FF4D2C57B44B9A35C13051B570EB66F0557D00015DF3832709050BF**; compatibility claims remain those of PP-AUD-036, not a new campaign.

## Manual validation and blocker closure

Both `CorpusManifestTests.SeedLinkEscapeIsRejected` and `FindingStorageTests.ArtifactLinkEscapeIsRejected` failed with **A required privilege is not held by the client** from File.CreateSymbolicLink before validator invocation. This is an actual Windows fixture boundary, not an observed validator failure. Root AGENTS.md requires preserving the tests and requesting permitted-context validation. The user was asked to run in **Administrator PowerShell**:

```powershell
Set-Location C:\Development\PiratePDF
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~SeedLinkEscapeIsRejected|FullyQualifiedName~ArtifactLinkEscapeIsRejected' --verbosity normal
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --verbosity normal
```

Final complete count is **397 = 297 + 100**. On 2026-09-27, the user replied **All 397 tests passed**. This closes both fixture-validation blockers and supports v1.0 for this lifecycle slice. The complete suite includes **100/100 new lifecycle cases** and **297/297 existing cases**, including both link tests. No separate numerical result for the standalone focused command was supplied or inferred. The passing fixtures now confirm their intended real symbolic-link escape rejection behavior, without claiming an atomic filesystem sandbox, concurrent parent-substitution protection or hard-link detection. Both earlier failures remain in this report and their TRX records. No OS security policy was changed and no previous privileged result was substituted for this fresh result.

## Exact files changed

This continuation changes **12 source/test/report files** relative to its 100-file dirty baseline:

| Path | Change in this continuation |
|---|---|
| tools/PiratePDF.Fuzz.Runner/FindingContracts.cs | Separate current agreement from discovery confirmation |
| tools/PiratePDF.Fuzz.Runner/FindingFiles.cs | Remove evidence-replacement path; preserve create-new writes |
| tools/PiratePDF.Fuzz.Runner/FindingStore.cs | Bound and validate four immutable reproduction records |
| tools/PiratePDF.Fuzz.Runner/FindingReproduction.cs | Preserve interruption evidence, require ledger capacity, distinguish changed outcomes and validate evidence consistency |
| tools/PiratePDF.Fuzz.Runner/FindingPromotion.cs | Consume validated prior failure ledger and preserve history |
| tests/PiratePDF.Fuzz.Tests/FindingFixture.cs | Correct missing Task import and remove unused import |
| tests/PiratePDF.Fuzz.Tests/FindingTestChild.cs | Controlled post-fix rejection and corpus-change fixtures |
| tests/PiratePDF.Fuzz.Tests/FindingReproductionTests.cs | New 20 cases |
| tests/PiratePDF.Fuzz.Tests/FindingMinimizationTests.cs | New 7 cases |
| tests/PiratePDF.Fuzz.Tests/FindingPromotionTests.cs | New 18 cases |
| tests/PiratePDF.Fuzz.Tests/FindingCommandTests.cs | New 11 cases |
| docs/reviews/Post-BF-1054-Batch-B9-Failure-Artifact-Reproduction-and-Promotion-Completion-Report.md | New PP-AUD-037 |

The original partial attempt additionally introduced FindingCommand.cs, FindingStorageTests.cs and modified Runner Program.cs, WorkerSupervisor.cs and SupervisorTestChild.cs. Those five files were preserved in this continuation. Across both attempts the slice therefore touches **17 source/test/report files**, with ignored evidence listed separately. No production project, package/project reference, permanent tests/FuzzCorpus file, PP-AUD-029 through PP-AUD-036, existing audit or CI file changed during this continuation.

Final scope verification in `final-scope.json` confirms **seven existing files modified, five new files, 93 other starting files unchanged**, no missing files, all **455 historical smoke/initialization evidence files unchanged**, and all three prior lifecycle ledgers unchanged. Branch/HEAD remained identical. `git diff --check` and individual untracked/new-file whitespace checks passed; Git emitted only its existing LF-to-CRLF conversion notices. The provisioned native driver's SHA-256 was rechecked and still matches the approved identity above. No validation artifacts or partial proposal directories were left outside the deliberately retained owned evidence areas; the ordinary mutation staging directory was absent after its successful command.

## Remaining boundary

With this slice's permitted-context validation complete, the next separately authorized B9 slice is the **substantive bounded coverage-guided campaign**, under the approved limits, provenance and discovery handling. Broader target coverage, portable file admission before cross-platform CI, CI integration and eventual full B9 acceptance/release-gate evidence remain separately scoped work. No campaign starts from this report. **B9 / POST-P2-002 remains OPEN.**

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-09-27 | Record bounded retention/reproduction/reduction/proposal infrastructure, 100 new cases, fresh 395/397 results and two Windows symbolic-link fixture privilege blockers; preserve prior partial implementation and all historical smoke evidence. |
| 1.0 | 2026-09-27 | Record the user's successful 397/397 permitted-context complete suite, including both real symbolic-link fixtures; close only the lifecycle slice while preserving historical failures and open overall B9 status. |
