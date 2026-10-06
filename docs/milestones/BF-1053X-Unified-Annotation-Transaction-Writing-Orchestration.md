# BF-1053X — Unified Annotation Transaction Writing Orchestration

| Field | Value |
|---|---|
| **Document ID** | BF-1053X |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-23 |
| **Next Review** | 2026-11-23 |

## Objective

Allow one committed annotation Editing transaction to contain both structural and common-state mutations while producing one minimal, atomic preservation-aware Writing transaction.

## Unified Orchestration

`PdfAnnotationTransactionWritingService` accepts committed annotation operations from the same editing session and classifies them per page.

Structural `Add` and `Remove` operations mark the page dictionary for `/Annots` projection.

Common-state rectangle, contents, clear-contents, and flags operations target annotation dictionaries only.

The final projection set is created once and applied once through the existing projected incremental or full-rewrite transaction.

## Mixed Transactions

A transaction may edit an existing bound annotation and add a new annotation on the same page. The resulting projection contains:

- the existing annotation when its common state changed;
- the new annotation object with a reservation-aware identifier;
- the owning page because `/Annots` changed.

A common-state-only transaction continues to omit the owning page.

## Final-State Semantics

Projection follows the authoritative state after the committed transaction.

An annotation added and then removed in the same transaction is absent from the final page, receives no object allocation, and does not produce an annotation-object projection.

If an existing annotation is edited and then removed, its detached backing object is preserved unchanged while the owning page's `/Annots` array records the removal. This avoids rewriting an object no longer reachable through the edited page structure.

## Reservation and Rollback

All new annotations across all affected pages share one `PdfWriteObjectAllocator`.

Pending annotation identities are visible only inside Writing projection until the projected plan succeeds.

Bindings are committed afterward. On planning failure, new annotation handles remain unbound and the object store is restored by the projected planner transaction.

## Compatibility

BF-1053V and BF-1053W remain valid focused entry points for common-state-only and structural-only callers.

BF-1053X is the general committed-annotation-transaction entry point when a caller does not want to pre-classify a transaction.

Writing still has no production dependency on Editing.

## Deferred Work

Page transaction orchestration, metadata transaction orchestration, page geometry transaction orchestration, annotation subtype-specific state, appearance streams, AcroForm/Widget coordination, object deletion/free entries, encrypted writing, cross-structure mixed transaction orchestration, and public SDK save workflows remain deferred.

## Acceptance Criteria

1. One transaction may combine structural and common annotation edits.
2. Common-state-only transactions omit the owning page.
3. Structural changes project the authoritative final `/Annots` array.
4. Existing bound annotations with common-state changes are targeted once.
5. Newly added annotations are reservation-aware and bound only after planning succeeds.
6. Add-then-remove cancellation allocates no annotation object.
7. Edit-then-remove preserves the detached backing object unchanged.
8. Mixed transaction planning is atomic across all projections.
9. Writing retains no production dependency on Editing.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-23 | Added unified mixed annotation transaction Writing orchestration. |
