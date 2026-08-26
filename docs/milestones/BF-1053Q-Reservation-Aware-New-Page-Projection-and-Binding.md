# BF-1053Q — Reservation-Aware New-Page Projection and Binding

| Field | Value |
|---|---|
| **Document ID** | BF-1053Q |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-22 |
| **Next Review** | 2026-11-22 |

## Objective

Extend preservation-aware structural Writing from already-bound page trees to newly created pages by assigning safe indirect-object identities and projecting their page dictionaries together with the page-tree update.

## Scope

BF-1053Q adds full-rewrite and incremental planners for pages currently present in the authoritative `PdfDocument.Pages` collection but not yet bound to indirect objects.

A shared reservation-aware batch reserves one generation-zero identifier for each unbound page from the write plan's current allocation boundary. Each new page is projected from its authoritative dictionary snapshot, including its `/Parent` reference to the already-bound page tree.

The page-tree projector is extended with an internal pending-binding path so `/Kids` can be assembled before new page objects have been applied to the object store. Its existing public behavior remains strict: direct BF-1053P projection still requires every page to be bound and stored.

## Transaction Boundary

New page model bindings are committed only after the underlying projected planner succeeds.

Before successful planning, newly assigned identifiers exist only as pending Writing assignments. If full-rewrite replanning or incremental planning fails, the existing transactional projection planner restores the object store and the high-level pages remain unbound.

This avoids adding a general-purpose unbind operation to the Document model solely for Writing rollback.

## Preservation

Existing bound page objects are not rewritten merely because a new page is inserted. The structural projection contains the page-tree dictionary plus only the newly created page objects.

The page-tree projection continues to shallow-copy the existing dictionary and target only `/Kids` and `/Count`.

## Output Modes

The allocation/binding path is available to both full-rewrite and incremental Writing.

Full rewrite refreshes the write plan so newly projected page identifiers participate in complete serialization.

Incremental planning targets the newly created page objects and the page-tree dictionary while leaving unaffected existing page objects out of the changed-object set.

## Deferred Work

Hierarchical multi-level `/Pages` reconstruction, page-parent reassignment across multiple page-tree nodes, object deletion/free entries, authoritative parser-to-document hydration, annotations, AcroForm structures, outlines, page decorations, content-stream rewriting, encrypted writing, and public Services/SDK orchestration remain deferred.

## Acceptance Criteria

1. Every unbound authoritative page receives a unique generation-zero identifier from the write plan allocator.
2. Newly projected page dictionaries contain the bound page-tree `/Parent` reference.
3. `/Kids` reflects the authoritative order containing both existing and newly allocated pages.
4. Full-rewrite plans include newly projected page identifiers.
5. Incremental plans target new page objects and the page-tree dictionary without targeting unchanged existing page objects.
6. Page bindings are committed only after projected planning succeeds.
7. Failed projected planning leaves new pages unbound and removes their projected objects from the object store.
8. Direct BF-1053P page-tree projection remains strict for unbound pages.
9. No external dependency or CI prerequisite is added.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-22 | Added reservation-aware new-page projection and post-planning model binding. |
