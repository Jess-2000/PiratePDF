# BF-1053U — Annotation Common-State Object-Only Planning

| Field | Value |
|---|---|
| **Document ID** | BF-1053U |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-23 |
| **Next Review** | 2026-11-23 |

## Objective

Separate annotation common-state edits from page annotation-collection edits so incremental Writing does not rewrite the owning page when `/Annots` has not changed.

## Scope

BF-1053U adds an annotation-object-only projection path to `PdfAnnotationWriteProjector`.

The existing BF-1053T projection remains unchanged for structural annotation operations: it projects the authoritative page `/Annots` array and selected annotation dictionaries.

The new object-only path validates the owning page and explicit annotation targets but emits projections only for the selected annotation dictionaries.

## Common-State Planning

`PdfAnnotationCommonStateFullRewritePlanner` and `PdfAnnotationCommonStateIncrementalWritePlanner` are intended for edits to the common annotation state already supported by BF-1053R:

- rectangle;
- contents;
- flags.

These operations do not alter annotation membership or ordering. Therefore the page dictionary does not need to be projected merely because one of these properties changes.

## Incremental Minimality

For a common-state edit to one already-bound annotation, the incremental changed-object set contains that annotation object and does not contain its owning page object.

This is narrower than BF-1053T structural planning, where the page dictionary is deliberately projected because `/Annots` may have changed.

## Compatibility

BF-1053R's all-bound-annotation compatibility path, BF-1053S's reservation-aware new-annotation path, and BF-1053T's structural targeting path remain available.

New-annotation insertion still requires page projection because `/Annots` changes.

## Validation

Every object-only target must identify a bound annotation currently attached to the supplied page. Unrelated identifiers and annotations belonging to another page are rejected.

Incremental common-state planning requires at least one target. Full-rewrite planning returns the unchanged base plan for an empty target set.

## Deferred Work

Automatic translation from Editing change sets to Writing targets, annotation subtype-specific state, appearance streams, action/destination structures, popup relationships, Widget/AcroForm coordination, object deletion/free entries, parser-to-authoritative-model hydration, encrypted writing, and public Services/SDK orchestration remain deferred.

## Acceptance Criteria

1. Common-state annotation projection can omit the owning page dictionary.
2. Incremental common-state planning targets only selected annotation objects.
3. The page's existing `/Annots` object reference remains untouched by object-only projection.
4. Full-rewrite common-state planning applies selected annotation projections.
5. Structural BF-1053T projection continues to update `/Annots`.
6. New-annotation BF-1053S planning continues to update `/Annots`.
7. Object-only targets must belong to the supplied page.
8. Empty incremental common-state target sets are rejected.
9. No external dependency or CI prerequisite is added.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-23 | Added annotation-object-only projection and minimal common-state write planners. |
