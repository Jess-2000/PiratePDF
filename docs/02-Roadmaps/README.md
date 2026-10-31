# PiratePDF Roadmaps

| Field | Value |
|---|---|
| **Document ID** | PP-ROAD-000 |
| **Category** | Roadmap |
| **Version** | 1.5 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1025, BF-1054, BF-1055 |
| **Last Updated** | 2026-10-31 |
| **Next Review** | 2027-01-31 |

---

# Purpose

This directory contains the planning documents that guide PiratePDF's engineering, architecture, and product development.

These roadmaps describe where the project is headed and provide a high-level view of completed, active, and planned work.

---

# Available Roadmaps

## Development Roadmap

Tracks engineering milestones, BlackFlag work items, and implementation progress.

Current maintenance order is defined by
[PP-GOV-004](../05-Governance/H2b-Deferral-and-Maintenance-Sequencing.md),
the [amended handoff](../handoffs/PiratePDF-Maintenance-Handoff-Post-BF-1054-Part1.md)
and [Future Roadmap](../Blueprint/Volume-VI-Future-Roadmap.md): B10 /
POST-P2-003 complete, POST-P2-004 / B11 active, then POST-P2-005 through POST-P2-008, independently of
native H2b qualification. Native H2b is deferred to an unspecified future
update, incomplete, unqualified and disabled; managed fuzzing remains
partially verified and B9 open. All item acceptance, security/refusal and
release-gate requirements remain. BF-1055 is separate product development.

[PP-GOV-005](../05-Governance/Platform-Support-and-Native-Lifetime.md) records
Windows 10/11 x64 as the sole official v1 target, deferred community-led
Linux/macOS support and permitted process-resident native modules. B11 remains
partial because no desktop project exists to qualify its distribution/startup.

The owner reserves **B16** as the final planned maintenance batch after
POST-P2-008: repository cleanup with semantic usage analysis and regression
coverage, plus managed-fuzzer optimization of measured bottlenecks. See the
Future Roadmap for scope and preservation requirements. B16 is not started
by B11, does not reopen native H2b and does not close B9.

B12 / POST-P2-005 is complete: [dependency accountability](../dependencies/README.md)
and all initial licensing/provenance findings are reconciled, with owner-confirmed
remediation gate success. B13 requires its own task scope before implementation.
B16 is the final planned maintenance batch **before BF-1055**. The approved
[desktop sequence](Windows-Desktop-Development-Sequence.md) reserves provisional
BF-1056 foundation, BF-1057 editing integration and BF-1058 packaging/installer
planning, followed by v1 release preparation. The numbers remain subject to
registry reconciliation; no implementation or B11 closure follows from planning.

---

## Product Roadmap *(Planned)*

Describes the capabilities planned for each PiratePDF release from a user perspective.

---

## Release Roadmap *(Planned)*

Tracks release planning, feature freezes, beta milestones, and production releases.

---

# Related Documents

- PP-GOV-002 — Project Governance Guide
- PP-GOV-002 — Documentation Standards
- PP-GOV-003 — Documentation Validation Rules

---

# Revision History

| Version | Date | Description |
|---|---|---|
| 1.5 | 2026-10-31 | Record B12 completion after licensing/provenance remediation and owner-confirmed full-gate success; preserve later-batch authorization boundaries. |
| 1.4 | 2026-10-31 | Link B12 dependency evidence and approved provisional desktop development sequence; retain B16 as final maintenance before BF-1055 and B11's open distribution acceptance. |
| 1.3 | 2026-10-31 | Reconcile Windows-only v1 and accepted process-resident native lifetime; retain actual desktop qualification as B11's open gap. |
| 1.2 | 2026-10-27 | Reconcile B10/B11 status and link the final planned B16 cleanup and measured managed-fuzzer optimization scope after POST-P2-008. |
| 1.0 | 2026-07-24 | Initial roadmap index. |
| 1.1 | 2026-10-26 | Link the approved H2b deferral and current independent B10/P2 maintenance sequence; preserve product/release and security boundaries. |

