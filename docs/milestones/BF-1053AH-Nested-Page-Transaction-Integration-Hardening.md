# BF-1053AH — Nested Page Transaction Integration and Hardening

| Field | Value |
|---|---|
| **Document ID** | BF-1053AH |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## Objective

Harden the AD–AG nested page-tree architecture by integrating nested structural changes with page geometry/rotation and exercising multi-operation, rollback, and write-mode behavior before expanding topology scope.

## Unified Nested Page Transaction

`PdfNestedPageTransactionWritingService` accepts one committed transaction containing:

- nested page `Insert`, `Remove`, `Move`, and `Replace`;
- page `SetRotation`;
- page geometry set-box, crop, and resize operations.

Structural operations are filtered into the AG orchestration path while state targets are projected through the existing page-state projector.

## Overlapping Projection Merge

A moved existing page may require both a structural `/Parent` change and geometry/rotation updates.

AH merges those two dictionary projections by retaining the structural dictionary as the preservation base and applying only the standard page-state keys from the geometry projection:

- `/MediaBox`;
- `/CropBox`;
- `/BleedBox`;
- `/TrimBox`;
- `/ArtBox`;
- `/Rotate`.

This preserves the one-projection-per-object invariant and prevents page-state projection from overwriting a structural `/Parent` reassignment.

## New Page Final State

Session-created pages that survive structural replay are already projected from authoritative final page state by AG.

Geometry and rotation changes to those pending pages therefore require no second projection.

Their identifiers remain reservation-aware and are bound only after successful combined planning.

## Atomicity and Rollback

Structural and page-state projections are combined before entering the projected incremental/full-rewrite planner.

A planning failure restores page-tree dictionaries, page dictionaries, and newly projected objects through one rollback boundary.

New page handles remain unbound when planning fails.

## Hardening Coverage

AH adds integration coverage for:

- cross-branch move plus rotation;
- insert plus resize plus rotation;
- state-only nested transactions;
- full-rewrite mixed transactions;
- planning failure and object-store restoration;
- multi-operation structural/state transactions remaining within AG's two-node boundary.

## Compatibility

`PdfNestedPageTreeStructuralWritingService` remains the focused structural-only entry point.

`PdfNestedPageTransactionWritingService` is the general nested page transaction entry point when structural and page-state mutations may coexist.

AG's current limit of at most two affected existing leaf-bearing `/Pages` nodes remains unchanged.

## Acceptance Criteria

1. Nested structural and page-state edits can coexist in one committed transaction.
2. Structural `/Parent` changes survive overlapping page-state projection.
3. Geometry and rotation survive overlapping structural projection.
4. New pages serialize final geometry/rotation without duplicate projections.
5. State-only nested transactions avoid rewriting page-tree nodes.
6. Incremental and full-rewrite modes use the same combined projection model.
7. Planning failure restores projected objects.
8. Planning failure leaves new pages unbound.
9. Multi-operation transactions remain deterministic within the two-node boundary.
10. The full PiratePDF release gate passes.

## Deferred Work

Transactions spanning more than two leaf-bearing nodes, node splitting/merging, hierarchy rebalancing, intermediate `/Pages` allocation, deletion/free-entry policy, encrypted writing, and public SDK save workflows remain deferred.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Added unified nested structural/state transaction integration and hardening coverage. |
