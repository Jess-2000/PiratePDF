# BF-1053R — Bound Annotation Common-State Projection

| Field | Value |
|---|---|
| **Document ID** | BF-1053R |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-22 |
| **Next Review** | 2026-11-22 |

## Objective

Introduce preservation-aware Writing projection for the common state of already-bound annotations and the owning page's annotation reference array.

## Scope

BF-1053R adds indirect-object identity binding to the authoritative `PdfAnnotation` model and introduces `PdfAnnotationWriteProjector`.

For one explicitly targeted bound page, the projector translates its authoritative annotation collection into the page dictionary's `/Annots` array. Each attached bound annotation is projected from a shallow copy of its existing source dictionary.

The common annotation keys targeted in this slice are `/Type`, `/Subtype`, `/Rect`, `/Contents`, and `/F`.

## Preservation

Untargeted entries in each annotation dictionary retain their existing `PdfObject` references. The owning page dictionary is also shallow-copied and only `/Annots` is targeted.

Removing all authoritative annotations from the page removes `/Annots` from the projected page dictionary without mutating the source dictionary.

Annotation removal does not delete detached annotation objects from the object store. Object deletion and free-entry policy remain separate Writing concerns.

## Text Encoding

Explicit annotation contents use a literal Latin-1 string when every character is representable in Latin-1. Otherwise Writing emits a UTF-16BE string with a byte-order mark in hexadecimal form.

An absent logical contents value removes `/Contents`; an explicit empty string remains explicit.

## Validation

The target page must be bound to the supplied object identifier and backed by an indirect dictionary. Every annotation currently attached to that page must also be bound to an existing indirect dictionary.

New, still-unbound annotations are deliberately rejected. Reservation-aware allocation for newly created annotations is deferred so identity assignment and rollback can be introduced as a dedicated transaction.

## Integration

The resulting projection set can be passed to the existing transactional incremental or projection-aware full-rewrite planners.

This slice does not add a dependency from Writing to Editing. It projects the authoritative Document model state established by the editing layer.

## Deferred Work

New-annotation allocation and binding, annotation subtype-specific dictionaries, appearance streams, action/destination structures, popup relationships, Widget/AcroForm coordination, object deletion/free entries, parser-to-authoritative-model hydration, encrypted writing, and public Services/SDK orchestration remain deferred.

## Acceptance Criteria

1. Bound annotations expose internal indirect-object identity to Writing.
2. `/Annots` reflects authoritative annotation order.
3. Common annotation state projects to `/Type`, `/Subtype`, `/Rect`, `/Contents`, and `/F`.
4. Missing logical contents removes `/Contents`; explicit empty contents remains explicit.
5. Non-Latin-1 contents use UTF-16BE hexadecimal PDF strings.
6. Untargeted annotation dictionary entries retain exact object references.
7. Empty authoritative annotation collections remove `/Annots` without deleting detached objects.
8. Unbound annotations and missing/non-dictionary backing objects are rejected.
9. No external dependency or CI prerequisite is added.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-22 | Added bound annotation identity and common-state dictionary projection. |
