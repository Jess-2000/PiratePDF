# PiratePDF Master Blueprint - Volume VI: Future Roadmap

| Field | Value |
|---|---|
| 1.7 | 2026-11-08 | Record scoped B14 deployment evidence, metadata/plugin incompatibilities, toolchain security servicing and future desktop/packaging implications. |
| **Document ID** | PP-ROAD-002 |
| **Category** | Roadmap |
| **Version** | 1.8 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-1054, BF-1055, BF-2000, BF-8000, BF-9000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

---
## Current maintenance roadmap — 2026-10-31

The owner-approved [PP-GOV-004](../05-Governance/H2b-Deferral-and-Maintenance-Sequencing.md)
defers native H2b execution and security qualification to an unspecified
future PiratePDF update. H2b remains incomplete, unqualified, blocked and
disabled; B9 / POST-P2-002 remains open. Preserve its implementation,
evidence, immutable seeds and security/refusal gates. No unsandboxed
fallback or production native fuzzing is authorized.

Managed fuzzing is partially verified: passing direct tests cover
`filter-asciihex`, `filter-flate` and `font-sanitize`; bounded deterministic
validation is available for trusted repository-owned fixtures. End-to-end
campaign verification and substantive coverage-guided completion remain
outstanding. PP-GOV-004 tracks the separate R1 broader/later filtered
validation scopes and their blocked link-security assertions.

Proceed independently of native H2b qualification in this authorized order:

1. **POST-P2-003 / B10 — reopen/render conformance** (complete; PP-AUD-040).
2. **POST-P2-004 / B11** — [Windows qualification remains; PP-AUD-041](../reviews/Post-BF-1054-Batch-B11-Packaged-Native-Resolution-Completion-Report.md). Windows 10/11 x64 is the sole v1 target; process-resident native modules are accepted under [PP-GOV-005](../05-Governance/Platform-Support-and-Native-Lifetime.md). No desktop project exists to publish/start; Linux/macOS and deterministic native-image unloading no longer block v1 acceptance.
3. **POST-P2-005 / B12 — complete**, dependency provenance, notices and SBOM ([current evidence](../dependencies/README.md)); package/tool evidence reconciled and owner-confirmed remediation gate success.
4. **POST-P2-006 / B13 — documentation reconciliation complete**, with owner-confirmed November 8 full-gate success. Official certificate provisioning and signed-distribution qualification remain partial pending the future executable/packaging scope.
5. **POST-P2-007 / B14 — evaluation complete**, retaining the untrimmed JIT baseline, deferring trimming and rejecting Native AOT for the current dynamic managed-plugin host. The [B14 report](../reviews/Post-BF-1054-Batch-B14-Deployment-Compatibility-Report.md) retains its original runtime/toolchain finding, subsequently serviced under completed B14A. B14B artifact currency is complete under PP-GOV-007 and PP-AUD-046, with owner-reported elevated gate success. B15 is next.
6. POST-P2-008 — CI release-gate/source-size parity.
7. **B16 — final repository cleanup and measured managed-fuzzer optimization** (after POST-P2-008 and the final planned maintenance batch before BF-1055; reserved on 2026-10-27).

The owner's B12 assignment records the subsequent [desktop development
sequence](../02-Roadmaps/Windows-Desktop-Development-Sequence.md): BF-1055
canonical font editing, provisional BF-1056 Windows Desktop Foundation,
BF-1057 Desktop Editing Integration, BF-1058 Windows Packaging and Installer,
then V1 Release Preparation. Reconcile provisional numbers with the milestone
registry before implementation; existing BF-8000 is not renumbered. B11's
actual application-distribution gap is reconciled during BF-1058, not closed
by planning. No BF-1055 through BF-1058 implementation occurs during B12.

The November 8 B13 assignment adds repository-wide Markdown reconciliation.
The canonical form policy governs supported local form events and prohibited
external/privileged effects. Script runtime and desktop event qualification
require separate implementation. Planned advanced native CSV merge/composition
has a maximum 25,000 records per job, independent letter pagination,
whole-letter atomicity across output PDFs, bounded memory with hybrid
disk-backed spooling, safe cancellation/recovery and output verification.
Optional FusionPro interoperability remains a separate integration. Feature
delivery requires a scoped milestone before inclusion in v1.
[Application specification](Volume-III-PiratePDF-Application-Specification.md),
[Form policy](../05-Governance/Document-Form-JavaScript-Policy.md),
[Windows release contract](../releases/Windows-Release-Signing-and-Provenance.md).

### B16 scope and acceptance boundaries

Review dead, unused, obsolete and unreachable code, obsolete compatibility
shims and duplicated implementation. Remove only demonstrably unnecessary
code with regression coverage. Preserve public APIs, serialization
compatibility and intentional extension points. Absence of static references
does not establish dead code: account for reflection, dependency injection,
plugins, XAML, generated references and platform entry points.

Profile the current managed fuzzer before optimizing. Investigate allocations,
corpus duplication, repeated parsing, mutation throughput, replay overhead,
process supervision and artifact retention; change only measured bottlenecks.
Preserve deterministic replay, seed ownership, mutation semantics, finding
lifecycle, process ceilings and failure reporting. Do not weaken coverage,
resource limits or security checks. B16 does not reopen native H2b or close
the outstanding B9 coverage-guided acceptance. This reservation authorizes
planning only; B16 is not implemented during B11.

Preserve each item's existing [audit acceptance requirements](../reviews/Post-BF-1054-CSharp-Repository-Audit.md).
POST-P2-004 concerns normal packaged-platform dependencies and does not
activate H2b. Sequencing permission does not constitute batch/release
acceptance, authorize credentials/signing/publication or waive the unchanged
release gate. The [current maintenance handoff](../handoffs/PiratePDF-Maintenance-Handoff-Post-BF-1054-Part1.md)
provides continuation instructions and historical checkpoint context.

BF-1055 remains separate product development subject to the charter,
existing roadmap/planning and acceptance requirements. This maintenance
decision neither starts nor accepts it and invents no missing specification.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.8 | 2026-11-08 | Apply B14B permanent artifact currency policy and servicing/verification guidance. |
| 1.6 | 2026-11-08 | Reconcile B13 policy, current guidance, authority references and release/maintenance sequencing. |
| 1.5 | 2026-10-31 | Reconcile B12 licensing/provenance remediation without starting later batches or desktop implementation. |
| 1.4 | 2026-10-31 | Record active B12 accountability evidence, final B16-before-BF-1055 ordering and owner-approved provisional desktop milestones through Windows packaging and v1 release preparation; retain open B11 acceptance. |
| 1.3 | 2026-10-31 | Reconcile B11-D1 through D4, missing desktop qualification target and retained B16 reservation; do not close B11 solely from platform/lifetime scope decisions. |
| 1.2 | 2026-10-27 | Reconcile completed B10 and active B11; reserve owner-approved B16 after POST-P2-008 for final cleanup and measured managed-fuzzer optimization, preserving API, replay and security boundaries. |
| 1.1 | 2026-10-26 | Record owner-approved native H2b deferral, partial managed verification and independent B10/P2 order; retain security, feature and release boundaries. |
| 1.0 | 2026-07-24 | Standardized document metadata and Blueprint structure. |
