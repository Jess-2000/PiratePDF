# BF-1053AC — Unified Page Transaction Writing Orchestration

| Field | Value |
|---|---|
| **Document ID** | BF-1053AC |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-25 |
| **Next Review** | 2026-11-25 |

## Objective

Allow one committed page Editing transaction to combine page-tree structural mutations with non-structural page state and produce one atomic preservation-aware Writing transaction.

## Supported Operations

`PdfPageTransactionWritingService` accepts:

- page `Insert`, `Remove`, `Move`, and `Replace`;
- page `SetRotation`;
- page geometry set-box, crop, and resize operations.

No caller-side pre-classification is required.

## Projection Strategy

The final authoritative page collection determines `/Kids` and `/Count` whenever structure changed.

Session-created pages that remain attached reserve identifiers from one shared allocator and receive page-dictionary projections from their final authoritative state.

Already-bound pages with geometry or rotation changes are projected through `PdfPageGeometryWriteProjector`.

This avoids rewriting existing pages solely because the page tree changed.

## Final-State Semantics

A new page may be inserted, resized, rotated, moved, or replaced within one transaction.

If it remains attached at commit time, its allocated page dictionary reflects final state.

If it is inserted and later removed before commit, it receives no object identifier and produces no object projection, even if state edits occurred while it was attached.

Removed/replaced existing page objects remain preserved in the object store.

## Atomicity

All new-page projections, existing page-state projections, and the page-tree projection are combined into one `PdfObjectWriteProjectionSet`.

New page bindings are committed only after projected planning succeeds.

Failure restores all projected objects through the existing Writing transaction and leaves new page handles unbound.

## Minimality

A page-state-only transaction omits the page-tree object.

A structure-only transaction does not rewrite unchanged existing pages.

A mixed transaction writes only:

- the page tree when structure changed;
- surviving new pages;
- existing bound pages whose page state changed.

## Compatibility

BF-1053AA remains the focused non-structural page-state service.

BF-1053AB remains the focused page-tree structural service.

BF-1053AC is the general committed page-transaction entry point.

## Acceptance Criteria

1. Structural and page-state edits may coexist in one committed transaction.
2. New pages serialize final geometry and rotation state.
3. Existing page-state targets remain minimal.
4. Structure-only changes avoid rewriting unchanged pages.
5. Page-state-only changes avoid rewriting the page tree.
6. Insert-then-remove cancellation allocates no object.
7. Removed/replaced backing page objects remain preserved.
8. All page projections share one atomic rollback boundary.
9. New page bindings commit only after successful planning.
10. The full PiratePDF release gate passes.

## Deferred Work

Multi-level page-tree hierarchy reconstruction, parent reassignment across multiple `/Pages` nodes, free-entry/deletion policy, cross-structure page+annotation transaction unification, metadata transaction orchestration, encrypted writing, and public SDK save workflows remain deferred.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-25 | Added unified structural-plus-page-state Writing orchestration. |
