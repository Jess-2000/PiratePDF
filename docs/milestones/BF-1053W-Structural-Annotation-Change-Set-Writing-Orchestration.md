# BF-1053W — Structural Annotation Change-Set Writing Orchestration

| Field | Value |
|---|---|
| **Document ID** | BF-1053W |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-23 |
| **Next Review** | 2026-11-23 |

## Objective

Bridge committed annotation add/remove transactions into one reservation-aware Writing transaction across all affected pages.

## Layering

The orchestration remains in `BlackFlag.Engine.Services`.

`BlackFlag.Engine.Writing` grants internals access to Services so the orchestration layer can use the existing pending-binding annotation projector without making reservation mechanics public API.

`PdfEditableAnnotation` gains an internal binding method so Services can commit a newly reserved object identity only after Writing planning succeeds.

Writing itself continues to have no production dependency on Editing.

## Supported Change Sets

`PdfAnnotationStructuralWritingService` accepts committed change sets containing only annotation `Add` and `Remove` operations.

Common-state operations remain handled by BF-1053V.

Non-annotation and unsupported annotation operations are rejected rather than silently omitted.

## Final-State Projection

Affected pages are derived from the committed structural operations, but `/Annots` is projected from the current authoritative page state.

This naturally handles operation sequences such as add-then-remove without allocating an object for an annotation that is absent from the final page state.

A remove-then-add of an already-bound annotation retains its original indirect-object identity.

## Shared Reservation

All newly attached, unbound annotations represented by the committed change set reserve identifiers from one `PdfWriteObjectAllocator`.

The allocator is shared across all affected pages, preventing duplicate reservations when one transaction adds annotations to multiple pages.

Unbound annotations found on an affected page that are not represented by the committed structural change set are rejected.

## Transaction Boundary

All page and new-annotation projections are combined into one `PdfObjectWriteProjectionSet`.

Incremental and full-rewrite planning therefore apply the complete structural annotation transaction atomically.

New editable annotation handles are bound only after projected planning succeeds. A failed Writing plan leaves the handles unbound and the object store restored by the existing projection transaction.

## Preservation

Removal changes only the owning page's `/Annots` reference array. Detached annotation objects remain in the object store; deletion/free-entry policy remains deferred.

Adding an annotation projects the new annotation object and the owning page dictionary without rewriting unrelated bound annotation objects.

## Deferred Work

Mixed structural and common-state transactions in one unified service, page structural change-set translation, page geometry change-set translation, metadata change-set translation, annotation subtype-specific state, appearance streams, AcroForm/Widget coordination, object deletion/free entries, encrypted writing, and public SDK save workflows remain deferred.

## Acceptance Criteria

1. Committed annotation add operations allocate and bind new indirect objects.
2. Committed remove operations project `/Annots` without deleting detached objects.
3. Multiple affected pages share one allocator sequence.
4. Add-then-remove cancellation does not allocate a new annotation object.
5. Existing bound annotations retain their identifiers.
6. All affected page/new-annotation projections are applied in one projected Writing transaction.
7. New annotation bindings occur only after successful planning.
8. Failed planning leaves new annotations unbound and restores projected objects.
9. Writing retains no production dependency on Editing.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added. The new Writing-to-Services relationship is `InternalsVisibleTo` friendship only, not a project dependency.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-23 | Added atomic Services-layer annotation structural change-set Writing orchestration. |
