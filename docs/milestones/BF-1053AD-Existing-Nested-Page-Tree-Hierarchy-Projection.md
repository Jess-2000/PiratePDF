# BF-1053AD — Existing Nested Page-Tree Hierarchy Projection

| Field | Value |
|---|---|
| **Document ID** | BF-1053AD |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-25 |
| **Next Review** | 2026-11-25 |

## Objective

Establish preservation-aware Writing support for existing nested `/Pages` hierarchies without forcing the authoritative `PdfPageTree` model to become hierarchical prematurely.

## Hierarchy Snapshot

`PdfPageTreeHierarchySnapshot` reads the indirect-object page-tree topology rooted at the authoritative page-tree identifier.

It distinguishes `/Pages` nodes from `/Page` leaves, records actual parent relationships, retains child order, computes descendant page counts, rejects cycles/repeated objects, and validates that flattened leaf order exactly matches `PdfDocument.Pages`.

The authoritative page collection therefore remains the semantic page order while the Writing layer gains access to the original structural topology.

## Nested Projection

`PdfNestedPageTreeWriteProjector` shallow-copies every existing page-tree node and leaf.

For `/Pages` nodes it:

- recomputes `/Count` from actual descendants;
- restores `/Parent` for non-root nodes;
- removes `/Parent` from the root node;
- leaves the existing `/Kids` array object untouched.

For `/Page` leaves it restores `/Parent` to the actual containing `/Pages` node.

Untargeted dictionary entries retain their existing object references.

## Preservation Boundary

This slice does not mutate hierarchy membership or reconstruct topology.

It deliberately preserves the existing `/Kids` arrays and node identities. The goal is to create a validated hierarchy representation and a preservation-aware projection contract before structural editing is permitted across nested nodes.

## Validation

The hierarchy rejects:

- cycles;
- repeated page-tree objects;
- non-dictionary tree objects;
- unsupported `/Type` values;
- root `/Page` objects;
- leaf count mismatches;
- leaf order mismatches against authoritative pages;
- unbound authoritative pages.

## Compatibility

The existing flat `PdfPageTreeWriteProjector` remains the structural projector for the current flat-authoritative transaction path.

BF-1053AD provides the topology foundation that later nested structural slices will use rather than flattening an existing hierarchy.

## Acceptance Criteria

1. Existing nested `/Pages` nodes can be traversed from the root object.
2. Child order and actual parent relationships are captured.
3. Descendant `/Count` values are computed independently of stored `/Count`.
4. Flattened leaf order must match authoritative document page order.
5. Nested projection preserves existing `/Kids` array object references.
6. `/Parent` references are normalized for nodes and leaves.
7. Root `/Parent` is removed.
8. Untargeted dictionary entries retain exact object references.
9. Malformed/repeated/cyclic topology is rejected.
10. The full PiratePDF release gate passes.

## Deferred Work

Structural insert/remove/move/replace across nested `/Pages` nodes, new intermediate-node allocation, page redistribution/rebalancing, parent reassignment caused by structural edits, multi-level incremental minimality, object deletion/free entries, encrypted writing, and public SDK save workflows remain deferred.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-25 | Added existing nested page-tree topology snapshot and preservation-aware projection. |
