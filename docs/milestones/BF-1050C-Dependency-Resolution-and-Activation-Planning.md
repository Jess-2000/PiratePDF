# BF-1050C — Dependency Resolution and Activation Planning

| Field | Value |
|---|---|
| **Document ID** | BF-1050C |
| **Category** | Milestone |
| **Version** | 1.1 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Add bounded plugin dependency declarations and produce a deterministic, non-executing activation plan from the BF-1050B catalog.

## Contract

- Manifests may declare required plugin dependencies by stable plugin ID and a minimum version with an optional exclusive maximum version.
- Duplicate and self dependencies are rejected during manifest validation.
- Activation planning validates host SDK compatibility, dependency presence, dependency version compatibility, cycles, blocked transitive dependencies, and configured graph bounds.
- Dependencies appear before dependents in the activation order.
- A broken plugin or dependency chain does not prevent unrelated plugins from remaining activatable.
- Planning loads, reflects over, instantiates, or executes no plugin assembly. Managed loading remains BF-1050D.

## Security and Bounds

Dependency count is bounded at manifest parsing. Planning bounds total plugins and dependency depth, propagates cancellation, and operates only on validated catalog entries. Diagnostics expose stable plugin identities and deterministic messages rather than exception objects.

## Optimization Review

The planner builds one ID dictionary, memoizes terminal visit state, sorts plugin/dependency identity once for deterministic traversal, and emits activation order during dependency-first traversal. No repeated filesystem scan, assembly scan, reflection, or capability initialization occurs. Graph work is linear in plugins plus dependency edges apart from deterministic ordering and bounded blocked-state propagation.

## Acceptance Criteria

1. Dependency ranges parse and validate under explicit bounds.
2. Missing or version-incompatible dependencies block only affected chains.
3. Cycles are detected deterministically and unrelated plugins remain ready.
4. SDK incompatibility and transitive blocked state are represented by structured issues.
5. Activation order is deterministic and dependency-first.
6. Planning is bounded and cancellation-aware.
7. Full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added bounded dependency declarations, deterministic graph validation, and non-executing activation planning. |
| 1.1 | 2026-08-13 | Corrected plugin version comparison so omitted build and revision components are treated as zero, preserving semantic lower and exclusive upper bounds. |
