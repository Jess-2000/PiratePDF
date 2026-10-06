# BF-1053AB — Page-Tree Structural Change-Set Writing Orchestration

| Field | Value |
|---|---|
| **Document ID** | BF-1053AB |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-25 |
| **Next Review** | 2026-11-25 |

## Objective

Bridge committed page `Insert`, `Remove`, `Move`, and `Replace` operations into the preservation-aware page-tree and new-page Writing machinery.

## Structural Transaction

`PdfPageStructuralWritingService` accepts committed structural `PdfPageEditOperation` entries.

The current authoritative editable-page collection defines the final page order. The page-tree `/Kids` array and `/Count` are projected from that final state.

Session-created pages still attached to the final collection reserve generation-zero indirect-object identifiers from one shared allocator.

## Final-State Allocation

Allocation is based on final authoritative state rather than blindly replaying operations.

A page inserted and then removed in the same committed transaction receives no object identifier and produces no page-object projection.

A replacement page that remains attached receives an identifier and a projected page dictionary.

Existing bound pages retain their identifiers.

## Preservation

Move and remove operations rewrite only the page-tree dictionary.

Removed or replaced page objects remain in the object store; object deletion/free-entry policy remains separate.

New pages are projected from authoritative dictionary snapshots and participate in `/Kids` through pending identifiers.

## Transaction Boundary

All new-page projections and the page-tree projection are combined into one projection set.

New editable-page bindings commit only after projected planning succeeds. Failed planning restores projected objects and leaves newly created pages unbound.

## Compatibility

BF-1053AA remains the unified entry point for non-structural page state.

BF-1053AB is the focused entry point for page-tree structural transactions.

Cross-category page transactions containing both structure and page-state changes remain deferred.

## Acceptance Criteria

1. Insert operations allocate and bind final-state new pages.
2. Remove operations update `/Kids` and `/Count` without deleting detached objects.
3. Move operations reorder `/Kids` without rewriting unaffected page objects.
4. Replace operations support session-created replacement pages.
5. Insert-then-remove cancellation allocates no object.
6. One allocator is shared across all final-state new pages.
7. New page bindings commit only after successful planning.
8. Failed planning leaves new pages unbound and projected objects restored.
9. Writing retains no production dependency on Editing.
10. The full PiratePDF release gate passes.

## Deferred Work

Unified structural-plus-page-state transactions, multi-level page-tree hierarchy reconstruction, parent reassignment across multiple `/Pages` nodes, object deletion/free entries, encrypted writing, and public SDK save workflows remain deferred.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-25 | Added Services-layer structural page-tree change-set Writing orchestration. |
