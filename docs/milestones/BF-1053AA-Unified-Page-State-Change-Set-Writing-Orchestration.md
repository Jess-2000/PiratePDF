# BF-1053AA — Unified Page-State Change-Set Writing Orchestration

| Field | Value |
|---|---|
| **Document ID** | BF-1053AA |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-25 |
| **Next Review** | 2026-11-25 |

## Objective

Provide one Services-layer Writing bridge for committed page-state transactions that may combine page geometry and page rotation edits.

## Unified Page-State Operations

`PdfPageStateWritingService` accepts:

- `PdfPageGeometryEditOperation` entries for page boxes, crop, and resize;
- `PdfPageEditOperation` entries whose mutation kind is `SetRotation`.

All affected page identities are deduplicated before projection.

The existing `PdfPageGeometryWriteProjector` remains the single page-dictionary projection implementation and writes final authoritative page boxes plus `/Rotate`.

## Mixed Transactions

One committed editing transaction may resize a page, set or clear page boxes, and change its rotation multiple times.

The page is projected once from final authoritative state.

A transaction affecting several pages produces one target per page, ordered by indirect-object identity for deterministic Writing.

## Preservation

Untargeted pages are not projected.

Targeted page dictionaries are shallow-copied by the existing projector, preserving unrelated entries exactly while updating only page boxes and rotation according to the current authoritative model state.

## Compatibility

BF-1053Y remains the focused page-geometry service.

BF-1053Z remains the focused rotation service.

BF-1053AA is the general page-state entry point when callers do not want to pre-classify committed non-structural page edits.

## Validation

The Writing plan must belong to the editing session document.

Every accepted operation must belong to that session.

Structural page mutations (`Insert`, `Remove`, `Move`, and `Replace`) are rejected because they require page-tree projection/allocation semantics.

Non-page-state operations, including annotation transactions, are also rejected rather than silently ignored.

## Acceptance Criteria

1. Geometry and rotation operations may coexist in one committed change set.
2. Each affected page is projected once.
3. Repeated rotation/geometry edits resolve to final authoritative page state.
4. Incremental plans target only affected page objects.
5. Full-rewrite plans use the same preservation-aware page projector.
6. Multi-page transactions produce deterministic target ordering.
7. Structural page operations are rejected.
8. Non-page-state operations are rejected.
9. Writing retains no production dependency on Editing.
10. The full PiratePDF release gate passes.

## Deferred Work

Page-tree structural change-set orchestration, new-page allocation through committed Editing transactions, page removal/free-entry policy, cross-structure mixed transaction orchestration, metadata transaction orchestration, content/resource transaction orchestration, encrypted writing, and public SDK save workflows remain deferred.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-25 | Added unified geometry-plus-rotation page-state Writing orchestration. |
