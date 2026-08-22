# BF-1053P — Bound Page-Tree Dictionary Projection

| Field | Value |
|---|---|
| **Document ID** | BF-1053P |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-22 |
| **Next Review** | 2026-11-22 |

## Objective

Introduce preservation-aware structural projection of the authoritative page order onto an existing bound page-tree dictionary.

## Scope

`PdfPageTreeWriteProjector` projects the authoritative `PdfDocument.Pages` order into the bound page-tree object's `/Kids` array and updates `/Count`.

This slice supports structural states composed entirely of pages that already have indirect-object identities and dictionary backing objects. It therefore establishes serialization support for reordering and removing already-bound pages without prematurely introducing new-page allocation or page-tree hierarchy reconstruction.

## Preservation

Projection starts from a shallow copy of the existing page-tree dictionary. Only `/Kids` and `/Count` are targeted. Untargeted entries retain their existing `PdfObject` references.

Removed pages are not deleted from the object store by this slice. The authoritative page tree simply stops referencing them. Object deletion and free-entry policy remain separate Writing concerns.

## Validation

The page-tree object must be bound, present in the object store, and backed by a dictionary. Every page currently in the authoritative page tree must also be bound to an existing dictionary object before projection can be created.

This deliberately rejects newly created, still-unbound pages. Assigning identities to new pages requires reservation-aware allocation plus rollback-safe model binding and is deferred to a later slice.

## Integration

The resulting `PdfObjectWriteProjectionSet` is compatible with the transactional projected incremental planner and the projection-aware full-rewrite planner.

Page geometry remains independently projectable through BF-1053N. Callers can combine compatible projections when both structural order and page geometry are targeted.

## Deferred Work

New-page indirect-object allocation and binding, hierarchical `/Pages` reconstruction, page-parent reassignment, object deletion/free entries, annotations, AcroForm structures, outlines, page decorations, content-stream rewriting, encrypted writing, authoritative parser-to-document hydration, and public Services/SDK orchestration remain deferred.

## Acceptance Criteria

1. The authoritative bound page order is projected into `/Kids`.
2. `/Count` reflects the authoritative page count.
3. Page removal is represented by omission from `/Kids` without deleting the detached backing object.
4. Untargeted page-tree dictionary entries retain their exact object references.
5. Projection construction does not mutate the source page-tree dictionary.
6. Unbound pages are rejected.
7. Missing or non-dictionary page and page-tree backing objects are rejected.
8. The projection remains compatible with existing incremental and full-rewrite projection planners.
9. No external dependency or CI prerequisite is added.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-22 | Added preservation-aware bound page-tree dictionary projection. |
