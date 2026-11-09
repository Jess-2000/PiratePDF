# Windows Desktop Development Sequence

| Field | Value |
|---|---|
| 1.2 | 2026-11-08 | Record scoped B14 deployment evidence, metadata/plugin incompatibilities, toolchain security servicing and future desktop/packaging implications. |
| **Document ID** | PP-ROAD-003 |
| **Category** | Roadmap |
| **Version** | 1.5 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1055, BF-1056, BF-1057, BF-1058, BF-8000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Planning authority and numbering

The owner's B12 assignment approves the following planning directions. Current
milestone files and the documentation index contain no BF-1056, BF-1057 or
BF-1058 assignment. These numbers remain **provisional** until formally
reconciled with the milestone registry before implementation. The existing
BF-8000 application workstream is not silently renumbered or closed.

B12, B13 documentation and B14 deployment evaluation are complete. Continue
from the passed B15 hosted checkpoint d031cfa into current B16 after completed
B14B artifact currency and B14A runtime/toolchain security remediation
and owner-confirmed patched-toolchain gate success under
[PP-AUD-045](../reviews/Post-BF-1054-Batch-B14A-Runtime-Toolchain-Security-Report.md),
with **B16 as the final planned maintenance batch before BF-1055**. Its scope
remains semantic dead/unused/obsolete-code review and measured managed-fuzzer
optimization, preserving public APIs, serialization, extension points,
reflection/plugin/XAML/generated/platform entry points, deterministic replay,
seed ownership, mutation semantics, findings, ceilings and failure reporting.
No coverage, resource or security checks may be weakened. B16 implementation
and its formal acceptance recommendation are recorded in
[PP-AUD-048](../reviews/Post-BF-1054-Batch-B16-Final-Maintenance-Completion-Report.md).
Then proceed through the separately governed product milestones:

| Provisional milestone | Approved planning direction |
|---|---|
| BF-1055 | Canonical font editing under its existing roadmap and acceptance rules; no implementation authorized by B12 |
| BF-1056 — Windows Desktop Foundation | Avalonia desktop application; Windows executable/launcher; application lifecycle, initial document viewport, engine integration and initial Windows deployment architecture |
| BF-1057 — Desktop Editing Integration | Editing interface/document workflows, navigation, selection/editing controls, undo/redo, settings and integration of BF-1055 canonical font editing |
| BF-1058 — Windows Packaging and Installer | Windows x64 application distribution, native dependency/startup qualification, installer/install/upgrade/uninstall behavior and reconciliation of B11's outstanding application-distribution acceptance |
| V1 Release Preparation | Applicable signing/provenance, clean Windows 10/11 qualification, security, preservation and deployment verification |

Windows 10/11 x64 is the sole official v1 target. Linux/macOS remain future
community-led targets; retain practical platform-neutral implementation.
BF-1058 planning does not preapprove a new installer dependency, signing setup,
release publication or a final deployment format. Those require their own
concrete scope and acceptance. No desktop project exists yet.

B11's engine-level physical containment/fallback evidence remains valid.
B11 stays open for actual desktop distribution/startup qualification; assigning
future reconciliation to BF-1058 is not closure or waiver. B9 remains open,
native H2b stays deferred/disabled, and no native qualification is resumed.
This document authorizes planning only, not BF-1055 through BF-1058 development.

The November 8 B13 reconciliation defines local form-event policy and release
signing/provenance requirements. Runtime/event qualification, official
certificate provisioning and signed-distribution verification remain separately
scoped work. Native CSV merge/composition is planned advanced functionality
with a 25,000 record ceiling and whole-letter output integrity.
[Application specification](../Blueprint/Volume-III-PiratePDF-Application-Specification.md),
[Form policy](../05-Governance/Document-Form-JavaScript-Policy.md),
[Release contract](../releases/Windows-Release-Signing-and-Provenance.md).

B14 recommends retaining untrimmed framework-dependent JIT as the current
developer baseline. Untrimmed self-contained deployment requires a future
installer/servicing decision. Trimming is deferred after concrete metadata/
plugin diagnostics, and Native AOT conflicts with the current dynamic managed
plugin host. The installed runtime/compiler security servicing finding must
be resolved before official deployment of affected runtime inputs. Apply these
findings to the actual future Avalonia executable and final installer.
[Deployment compatibility](../04-Developer/Deployment-Compatibility.md).

PP-GOV-007 applies to future desktop and installer outputs. Rebuild affected
self-contained packages, validate framework-dependent runtime floors, review
native support/provenance and refresh final SBOM/hashes/signing evidence before
distribution. B14B's developer outputs retain their actual qualification scope.
[Artifact currency policy](../05-Governance/Runtime-Dependency-and-Artifact-Currency.md).

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.5 | 2026-11-08 | Record verified B15 checkpoint and B16 implementation/verification supporting formal acceptance. |
| 1.4 | 2026-11-08 | Apply B14B permanent artifact currency policy and servicing/verification guidance. |
| 1.3 | 2026-11-08 | Record approved B14A SDK/runtime security servicing and separate validation/acceptance evidence. |
| 1.1 | 2026-11-08 | Reconcile completed B12, current B13 and future form, native merge and signing qualification dependencies. |
| 1.0 | 2026-10-31 | Record owner-approved provisional desktop sequence, registry reconciliation requirement, final B16-before-BF-1055 order and retained B11 acceptance gap. |
