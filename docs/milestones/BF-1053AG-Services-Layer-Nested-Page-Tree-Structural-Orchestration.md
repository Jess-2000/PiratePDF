# BF-1053AG — Services-Layer Nested Page-Tree Structural Orchestration

| Field | Value |
|---|---|
| **Document ID** | BF-1053AG |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## Objective

Connect committed Editing page insert/remove/move/replace transactions to the AD/AE/AF nested page-tree Writing primitives without guessing source topology from the post-edit flat page collection.

## Structural Identity Capture

`PdfPageEditOperation` now retains the pre-write indirect-object identity of its primary and replacement pages internally when those identities exist.

The identities remain internal to Editing/Services integration and do not become public serialization API.

This allows committed remove, move, and replace operations to be replayed against the unchanged indirect page-tree topology after the authoritative flat page collection has already been edited.

## Topology-Only Snapshot

`PdfPageTreeHierarchySnapshot.CreateTopology` reads the existing indirect hierarchy without requiring its flattened leaf order to equal the already-mutated authoritative page collection.

The public validated snapshot behavior remains unchanged.

## Operation Replay

`PdfNestedPageTreeStructuralWritingService` begins with the original hierarchy leaf order and replays committed structural operation indexes.

Insert and cross-node move destination parents are inferred deterministically from the page occupying the destination index after removal, or from the previous page when appending.

Replacement keeps the removed page's existing nested parent.

## Projection Selection

The replayed final placements are grouped by their existing leaf-bearing `/Pages` parent.

If one node changed, the service delegates to BF-1053AE.

If two nodes changed, it delegates to BF-1053AF.

Transactions affecting more than two existing leaf-bearing nodes are rejected in this slice.

## Reservation and Atomicity

Session-created pages that survive in final placements reserve identifiers from one shared write allocator.

The AE/AF projection set is applied through the projected incremental/full-rewrite planners.

New page handles are bound only after successful planning. Failed planning leaves them unbound.

## Scope Boundary

This slice intentionally supports at most two affected existing leaf-bearing `/Pages` nodes.

It does not yet support:

- transactions spanning three or more leaf-bearing nodes;
- moving child `/Pages` nodes;
- node splitting/merging;
- tree rebalancing;
- creation of intermediate `/Pages` nodes;
- mixed nested structural plus geometry/rotation orchestration.

## Acceptance Criteria

1. Committed nested insert/remove/move/replace operations can be replayed from stored operation indexes.
2. Existing source page identities survive post-edit orchestration.
3. Same-node transactions automatically use the AE primitive.
4. Cross-node transactions automatically use the AF primitive.
5. New page identifiers reserve from one allocator.
6. New page bindings commit only after successful planning.
7. Replacements retain the target's existing nested parent.
8. Public validated hierarchy snapshot behavior remains unchanged.
9. Transactions affecting more than two leaf-bearing nodes are rejected.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Added Services-layer nested page-tree structural orchestration over AE/AF primitives. |
