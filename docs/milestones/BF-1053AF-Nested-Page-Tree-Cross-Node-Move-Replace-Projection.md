# BF-1053AF — Nested Page-Tree Cross-Node Move/Replace Projection

| Field | Value |
|---|---|
| **Document ID** | BF-1053AF |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## Objective

Extend nested page-tree structural projection from AE's single-node insert/remove primitive to coordinated page-leaf movement and replacement across two existing `/Pages` nodes.

## Coordinated Source and Destination Projection

`PdfNestedPageTreeCrossNodeProjector` receives distinct source and destination node identifiers plus their final `/Kids` sequences.

Both node dictionaries are shallow-copied and receive rebuilt `/Kids` arrays and recomputed `/Count` values.

Existing page leaves may move from the source node to the destination node. Pending newly created pages may be introduced in the destination sequence for replacement scenarios.

## Parent Reassignment

Every destination page leaf that participates in the final destination sequence is projected with `/Parent` pointing to the destination node.

Moved page objects retain their indirect-object identity.

Pending replacement pages receive their destination `/Parent` when projected.

Detached/replaced page objects are preserved in the object store.

## Ancestor Delta Aggregation

Source and destination descendant-count deltas are propagated separately through their ancestor chains and aggregated by ancestor identifier.

When the two nodes share ancestors, including the root, the ancestor receives one projection with the net count delta.

A pure cross-node move therefore leaves the shared root `/Count` unchanged and avoids writing a redundant root projection.

## Validation

The projector rejects:

- identical source and destination nodes;
- duplicate children;
- a child appearing in both final node sequences;
- unknown destination children;
- page-tree node movement across parents;
- source children that were not originally attached to the source.

Cross-node movement is intentionally limited to `/Page` leaves.

## Scope Boundary

This slice does not split, merge, create, or rebalance `/Pages` nodes.

It does not yet translate committed Editing operations into nested source/destination node selections.

## Acceptance Criteria

1. A page leaf can move between existing `/Pages` nodes.
2. Source and destination `/Kids` arrays are rebuilt together.
3. Moved pages retain indirect-object identity.
4. Moved pages receive the destination `/Parent`.
5. Pending replacement pages receive the destination `/Parent`.
6. Source and destination `/Count` values reflect final descendants.
7. Shared ancestor deltas are aggregated into one net projection.
8. Pure moves do not rewrite a shared root when its count is unchanged.
9. Cross-node `/Pages` movement is rejected.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Deferred Work

Services-layer nested structural orchestration, automatic source/destination node selection, node splitting/merging, hierarchy rebalancing, intermediate `/Pages` allocation, deletion/free-entry policy, encrypted writing, and public SDK save workflows remain deferred.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Added coordinated cross-node page-leaf move/replace projection with ancestor delta aggregation. |
