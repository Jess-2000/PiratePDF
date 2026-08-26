# BF-1053AE — Nested Page-Tree Insert/Remove Structural Projection

| Field | Value |
|---|---|
| **Document ID** | BF-1053AE |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## Objective

Add the first structural mutation primitive for an existing nested page-tree hierarchy: insert/remove within one existing `/Pages` node while preserving surrounding topology.

## Targeted Node Projection

`PdfNestedPageTreeStructuralProjector` accepts the target `/Pages` node, its final child sequence, and pending identifiers for newly inserted pages.

The target node's `/Kids` array is rebuilt from the requested final sequence. `/Count` is recomputed from page leaves plus descendant counts of retained child `/Pages` nodes.

## Ancestor Count Propagation

The target node's descendant-count delta is propagated through every ancestor `/Pages` node.

Ancestor `/Kids` arrays remain unchanged. Only `/Count` is updated in ancestors.

## Parent Assignment

Existing page leaves retained in the target node receive `/Parent` pointing to that node.

New pending pages are projected from authoritative page dictionary snapshots and receive the same `/Parent`.

## Preservation

Removed page objects are not deleted.

Existing `/Pages` nodes retain identity. Untargeted dictionary entries remain preserved through shallow-copy projection. Sibling branches are not rewritten.

## Validation

The projector rejects duplicate child identifiers, unknown children, existing leaves or child nodes belonging to another parent, and malformed hierarchy detected by BF-1053AD.

Cross-node reparenting is intentionally rejected.

## Scope Boundary

BF-1053AE is a Writing primitive, not yet a Services-layer committed-operation orchestrator for nested trees.

It does not implement cross-node move/replace, node splitting/merging, rebalancing, or creation of new intermediate `/Pages` nodes.

## Acceptance Criteria

1. `/Kids` can be rebuilt for one existing nested `/Pages` node.
2. Target `/Count` reflects final descendants.
3. Count delta propagates through all ancestors.
4. Existing leaf `/Parent` references are normalized.
5. New pages receive the target node as `/Parent`.
6. Sibling branches remain untouched.
7. Removed page objects remain preserved.
8. Existing children from another parent are rejected.
9. Existing hierarchy topology remains otherwise preserved.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Added nested single-node insert/remove structural projection with ancestor count propagation. |
