# Native H2b Deferral and Maintenance Sequencing

| Field | Value |
|---|---|
| **Document ID** | PP-GOV-004 |
| **Category** | Governance |
| **Version** | 1.3 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054, BF-1055 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Owner decision and authority

**November 8 reconciliation:** B10 and B12 are complete. B11 retains actual
Windows desktop distribution/startup qualification. B13 documentation and
signing/provenance reconciliation is complete, with official signed-release
qualification deferred. B14 evaluation and B14A security servicing are complete.
B14B artifact currency is complete. B15 and final B16 precede
BF-1055. BF-1056 through BF-1058 remain provisional. The October 26 order and
dated qualification evidence below retain their original scope. The active
handoff supplies the latest execution state.
[Maintenance handoff](../handoffs/PiratePDF-Maintenance-Handoff-Post-BF-1054-Part1.md).

PP-GOV-007 governs runtime/dependency and artifact currency. Servicing or
rebuilding tooling supplies developer evidence only and does not qualify or
activate deferred H2b. All native execution/admission gates below retain their
fail-closed requirements. [Currency policy](Runtime-Dependency-and-Artifact-Currency.md).

**October 31 continuation:** [PP-GOV-005](Platform-Support-and-Native-Lifetime.md)
records B11-D1 through D4: Windows 10/11 x64-only v1, deferred community-led
Linux/macOS support, accepted process-resident native modules and narrow CI
alignment. These settle B11 platform/lifetime decisions without enabling H2b.
No desktop entry project exists, so actual application distribution/startup
qualification remains open. B16 stays reserved after POST-P2-008 for final
semantic cleanup and measured managed-fuzzer optimization. The October 26
decision and historical evidence below retain their original scope.

Effective **2026-10-26**, the project owner defers native H2b execution and
security qualification to an **unspecified future PiratePDF update**. H2b
is incomplete, unqualified, blocked and disabled. B9 / POST-P2-002 remains
open; deferral is not completion, acceptance or a finding closure.

Native H2b qualification and POST-P2-002 closure are **not prerequisites
for starting or progressing B10 and POST-P2-004 through POST-P2-008**.
This current sequencing decision supersedes the original handoff's
"After POST-P2-002 is actually closed" dependency and the historical
Phase A batch order. It does not change those items' acceptance criteria.

This is a scheduling and development-readiness decision, not a security
policy exception or a release-readiness finding. It supersedes earlier
requests to continue C1/native-controller implementation or complete a
campaign as the immediate next maintenance action. Those requests do not
authorize additional work in this documentation-only assignment.

## Mandatory security boundaries remain

- Preserve H2b code, historical reports/evidence, NOT-AUTHORIZED guards and
  all 15 immutable corpus seeds. Do not remove or enable the infrastructure.
- Keep production native execution refusal and fail-closed protections.
  FAILED, BLOCKED or UNKNOWN cannot authorize native execution.
- No production native fuzzing, privileged station/desktop experiment,
  protected deployment or new identity/service/security-policy operation
  follows from this decision.
- No blocked native operation may fall back to an ordinary unsandboxed
  managed process for hostile/external PDF inputs. A .NET process is not
  a security sandbox.
- Preserve the native gates for executable/dependency trust, LPAC identity,
  creation-time Job/no-breakaway/handle control, filesystem confinement,
  bounded storage, network/local IPC/brokers, attestation, actual runtime
  initialization, resource bounds and verified cleanup. Controller
  compromise/capability escape and final USER-object lifetime remain
  unresolved security questions, not accepted residual risks.

Future resumption requires an explicit scoped decision and the original
security/qualification evidence. No future version or delivery date is
promised here.

## Managed fuzzing status and validation scopes

Managed fuzzing is **partially verified**. Existing managed target IDs are
`filter-asciihex`, `filter-flate` and `font-sanitize`; the retained registry
limits are 16,384, 16,384 and 65,536 input bytes respectively. Passing direct
tests establish these trusted-fixture boundaries, not broad PDF coverage.
Bounded deterministic mutation, replay, corpus validation and finding
reproduction/promotion contracts are available for trusted repository-owned
fixtures. End-to-end campaign acceptance remains partial. Substantive
coverage-guided campaign completion has not been demonstrated; earlier
smoke feedback is not campaign closure.

The following historical scopes are separate and must not be combined or
reported as new executions during this documentation update:

| Evidence | Recorded result | Meaning |
|---|---|---|
| [R1 completion](../../artifacts/fuzz/h2b-a1-r1-20261026-01/H2b-A1-R1-Preflight-Security-Remediation-Completion.md), 2026-10-26, full Release fuzz suite | 909 passed, 5 failed, 0 skipped / 914 | Original TRX identifies all five fixture-creation failures below |
| Same R1 unmodified release gate, 2026-10-26 | 3,377 passed, 5 failed, 0 skipped / 3,382 | Gate failed; documentation stage was not reached |
| [Later managed report](../../artifacts/fuzz/Managed-Fuzzing-Completion-and-H2b-Simplification-Report.md), corroborated by owner; [dated companion report](../../artifacts/fuzz/PdfCraft-Inspired-Managed-Fuzzer-Completion-Report-20261026-232600.md) | 239 passed, 2 failed, 0 skipped / 241 | Filtered scope; configuration not explicitly specified in recorded command; no original filtered TRX located in this review |
| Later direct managed-target report, dated companion filename 20261026; also nine passing ManagedTargetTests cases in original R1 TRX | 9 passed, 0 failed, 0 skipped | Direct tests of the three targets; not an end-to-end campaign |

The managed reports do not embed an exact run timestamp. Their original
content, dated companion filename and owner corroboration are retained;
this amendment does not fabricate a timestamp or rewrite those reports.
The later filtered command was:

```powershell
dotnet test C:\Development\PiratePDF\tests\PiratePDF.Fuzz.Tests\PiratePDF.Fuzz.Tests.csproj --filter "Campaign|DeterministicMutation|CorpusMutation|NativeProcessSupervisor" --logger "console;verbosity=minimal"
```

The five exact names below were retrieved from the original
[R1 full-suite TRX](../../artifacts/fuzz/h2b-a1-r1-20261026-01/fuzz-complete-final/fuzz-complete-final.trx), not inferred:

1. `CorpusManifestTests.SeedLinkEscapeIsRejected`.
2. `FindingStorageTests.ArtifactLinkEscapeIsRejected`.
3. `CampaignTicketAdmissionTests.TicketDirectoryLinkIsRejectedAndForeignBytesPreserved`.
4. `CampaignRunnerTests.CampaignWorkspaceLinkIsRejected`.
5. `H2bStationAclApplicabilityTests.ReparseSubstitutionIsRejectedBeforeOwnerAdmission`.

The later 241-case run reports failures **3 and 4**, matching its narrower
filter. The two original corpus/artifact symlink-security tests **1 and 2**
remain blocked in the broader R1 scope; test 5 is additional R1 coverage.
All five failures occurred when Windows denied fixture creation before
the relevant validator/security assertion. No blocked assertion is marked
passed, skipped, waived or deleted. A filtered result is not evidence for
excluded cases, the full suite or release acceptance.

## Independent remaining managed-verification work

| Tracking key | Outstanding acceptance evidence |
|---|---|
| B9-MV-01 | Obtain permitted-context results for the exact five real-link fixtures and the required broader suite; retain original denied attempts |
| B9-MV-02 | Finish end-to-end bounded managed campaign verification, cancellation/cleanup/retention/reproduction evidence and unchanged full-gate acceptance where required; do not claim a safe hostile-input campaign |
| B9-MV-03 | Track substantive coverage-guided campaign acceptance separately; no claimed completion, new execution authorization or unsandboxed fallback is granted |

These are verification tracking labels, not new closed audit findings or
changes to PP-AUD-029/security acceptance. They remain visible while
maintenance proceeds. H2b implementation/qualification remains a separate
deferred workstream.

## Authorized maintenance execution order

| Order | Work item | Acceptance retained |
|---|---|---|
| 1 | **POST-P2-003 / B10 — reopen/render conformance** | Existing parse/edit/save/reopen/render and preservation requirements; deterministic fixtures, rollback/undo/redo and SDK invalidation where affected |
| 2 | POST-P2-004 — native resolution/fallback packaged-platform fixtures | Packaged native dependency loading, resolution/fallback and physical-path policy evidence; required platform/package fixtures |
| 3 | POST-P2-005 — dependency provenance, notices and SBOM | Actual resolved/packaged inventory, licenses/provenance/notices/SBOM; no implicit dependency upgrade |
| 4 | POST-P2-006 — release signing and provenance | Existing signing, credential protection, provenance and artifact checks; actual signing/publishing/credential use still requires its own authorization |
| 5 | POST-P2-007 — AOT/trimming validation | Existing reflection, plugin, serialization and native/platform compatibility requirements; no warning suppression or security-boundary claim |
| 6 | POST-P2-008 — CI release-gate/source-size parity | Reuse authoritative checks, preserve portable/Windows routing and test discovery; reconcile PP-AUD-038's completed portion without closing remaining gaps prematurely |

The owner authorizes these maintenance items to proceed independently of
native H2b qualification. **B10 is the next active maintenance item.** This
assignment records that authorization; it does not begin any implementation
batch. Existing scope, preservation, tests, analyzer/build, documentation,
dependency/API approval and release-gate requirements still apply.

POST-P2-004 concerns normal packaged-platform native dependency resolution
and fallback. It is **not** authorization for H2b native fuzzing, privileged
worker execution or a security-policy bypass.

## Development readiness versus release readiness

The unchanged `Invoke-PiratePDFReleaseGate.ps1` remains authoritative for
acceptance where required. No mandatory test or release condition is removed,
filtered away or silently converted to advisory. Known environment failures
remain failures/pending validation. Successful documentation validation in
this assignment does not make the previous R1 full gate green or accept B9.

BF-1055 is a **separate product-development milestone**, subject to the
charter, roadmap, milestone planning and its existing acceptance rules.
This deferral neither implements/accepts BF-1055 nor makes it part of B10.
No dedicated BF-1055 milestone specification was found in the inspected
repository; no new feature acceptance criteria are invented here.

Historical milestone/audit/qualification reports retain their original
statements, counts and dates. Current sequencing authority is this dated
decision, the amended active handoff, root instructions and maintained
audit/roadmap status. No source/test/security-policy/release-script changes
are authorized by this documentation assignment.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.3 | 2026-11-08 | Link B14B artifact currency governance and retain independent native/plugin qualification. |
| 1.2 | 2026-11-08 | Add current maintenance reconciliation and move the misplaced prior revision entry to the history table. |
| 1.1 | 2026-10-31 | Link approved B11 Windows-only v1 and native-residency decisions, missing desktop qualification target and retained final B16 reservation; preserve all native H2b deferral/refusal requirements. |
| 1.0 | 2026-10-26 | Record owner-approved native H2b deferral, partial managed status with separate validation scopes, unchanged security/release requirements and independent B10/P2 maintenance authorization. |
