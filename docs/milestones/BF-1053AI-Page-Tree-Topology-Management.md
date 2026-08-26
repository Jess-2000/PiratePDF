# BF-1053AI — Page-Tree Topology Management

| Field | Value |
|---|---|
| **Document ID** | BF-1053AI |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## Objective

Remove AG/AH's two-leaf-node structural transaction limit while preserving existing `/Pages` node identities and avoiding unnecessary balancing, splitting, or merging.

## Generalized Topology Projection

`PdfNestedPageTreeTopologyProjector` accepts the complete final child map for every existing `/Pages` node plus pending page bindings.

It validates the full map, prevents duplicate leaf attachment, preserves child `/Pages` parentage, and supports page-leaf movement across any number of existing leaf-bearing nodes.

## Recursive Final Counts

Final `/Count` values are computed recursively from the complete projected child map rather than by pairwise source/destination deltas.

This naturally handles:

- three or more affected leaf-bearing nodes;
- shared ancestors;
- nested affected ancestors and descendants;
- empty leaf-bearing nodes;
- unchanged root counts after pure redistribution.

Only nodes whose `/Kids` or `/Count` actually change are projected.

## Empty Node Policy

An existing `/Pages` node that loses its last page is preserved with:

- its existing indirect-object identity;
- an empty `/Kids` array;
- `/Count 0`;
- its existing parent relationship and unrelated dictionary state.

AI deliberately does not delete, merge, or rebalance empty nodes.

This preservation-first policy avoids unnecessary topology churn and remains valid for later insertion into the same node.

## Leaf Parent Projection

Existing page leaves are rewritten only when their final `/Parent` differs from the original hierarchy.

Pending new pages are projected once with their final `/Parent`.

Detached removed page objects remain preserved in the object store.

## Services Integration

`PdfNestedPageTreeStructuralWritingService` now delegates all nested structural transactions to the generalized topology projector.

The one-node AE and two-node AF primitives remain available and tested as focused lower-level contracts, but Services no longer has a two-node routing limit.

`PdfNestedPageTransactionWritingService` inherits the expanded topology support for mixed structural + geometry/rotation transactions.

## Acceptance Criteria

1. Structural transactions may affect three or more existing leaf-bearing `/Pages` nodes.
2. Final `/Count` values are computed recursively from final topology.
3. Shared ancestors receive one deterministic projection.
4. Pure page redistribution preserves unchanged ancestor counts.
5. Empty `/Pages` nodes remain present with empty `/Kids` and `/Count 0`.
6. Existing `/Pages` node identities are preserved.
7. Existing page leaves are rewritten only when `/Parent` changes.
8. Pending pages receive their final parent exactly once.
9. No generalized splitting, merging, or balancing is introduced.
10. The full PiratePDF release gate passes.

## Deferred Work

Creation of new intermediate `/Pages` nodes, optional topology compaction/rebalancing, deletion/free-entry policy, encrypted writing, and public SDK save workflows remain deferred.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Added generalized multi-node topology projection and preservation-first empty-node handling. |
