# Windows Desktop Development Sequence

| Field | Value |
|---|---|
| **Document ID** | PP-ROAD-003 |
| **Category** | Roadmap |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1055, BF-1056, BF-1057, BF-1058, BF-8000 |
| **Last Updated** | 2026-10-31 |
| **Next Review** | 2027-01-31 |

## Planning authority and numbering

The owner's B12 assignment approves the following planning directions. Current
milestone files and the documentation index contain no BF-1056, BF-1057 or
BF-1058 assignment. These numbers remain **provisional** until formally
reconciled with the milestone registry before implementation. The existing
BF-8000 application workstream is not silently renumbered or closed.

Complete approved maintenance B12 through POST-P2-008 in their scoped batches,
with **B16 as the final planned maintenance batch before BF-1055**. Its scope
remains semantic dead/unused/obsolete-code review and measured managed-fuzzer
optimization, preserving public APIs, serialization, extension points,
reflection/plugin/XAML/generated/platform entry points, deterministic replay,
seed ownership, mutation semantics, findings, ceilings and failure reporting.
No coverage, resource or security checks may be weakened. B16 is not implemented
during B12. Then proceed through the separately governed product milestones:

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

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-10-31 | Record owner-approved provisional desktop sequence, registry reconciliation requirement, final B16-before-BF-1055 order and retained B11 acceptance gap. |
