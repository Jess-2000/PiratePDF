# BF-1053K — Transactional Projection Planning and Object-Generation Safety

| Field | Value |
|---|---|
| **Document ID** | BF-1053K |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-20 |
| **Next Review** | 2026-11-20 |

## Objective

Harden the BF-1053H indirect-object projection foundation so failed incremental planning cannot leave partially projected document state behind, and so projected object identifiers cannot introduce a second active generation for an existing object number.

## Transactional Projection Planning

`PdfProjectedIncrementalWritePlanner` now validates projection identity constraints before mutation, snapshots the affected object-store state, applies projections, and restores the original projected objects if downstream incremental planning throws.

Existing projected objects are restored to the exact prior `PdfObject` references. Newly added projected objects are removed during rollback. Successful planning retains the projected state exactly as before.

## Object-Generation Safety

A new projected object may not reuse an object number that is already active under a different generation. Existing exact identifiers remain valid replacement targets. New object numbers must still use generation zero and remain at or above the write plan allocation boundary.

`PdfObjectWriteProjectionSet` validates null entries before deterministic sorting, ensuring malformed enumerable input produces the intended argument failure instead of a null-reference failure during ordering.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added. Writing remains independent from Editing. Untargeted object-store entries are not changed by projection rollback.

## Deferred Work

This slice does not create a missing `/Info` dictionary, project page geometry or other BF-1052 structural domains, support object deletion/free entries, add dirty tracking, or implement encrypted writing.

## Acceptance Criteria

1. A downstream incremental-planning failure restores every existing projected object to its original reference.
2. A downstream failure removes newly projected objects added during the failed attempt.
3. A new projection cannot create a second active generation for an existing object number.
4. Existing exact object identifiers remain valid projection replacement targets.
5. Null projection entries fail deterministically before sorting.
6. Successful projection planning preserves existing BF-1053H behavior.
7. No external dependency or CI prerequisite is added.
8. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-20 | Added transactional projection rollback, active-generation collision protection, and null-safe projection validation. |
