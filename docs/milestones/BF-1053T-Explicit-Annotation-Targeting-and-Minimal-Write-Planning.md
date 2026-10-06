# BF-1053T — Explicit Annotation Targeting and Minimal Write Planning

| Field | Value |
|---|---|
| **Document ID** | BF-1053T |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-23 |
| **Next Review** | 2026-11-23 |

## Objective

Reduce annotation write churn by separating page-level `/Annots` projection from annotation-dictionary targeting.

## Explicit Targeting

`PdfAnnotationWriteProjector` retains its existing two-argument compatibility path, which projects all bound annotations currently attached to the page.

A new overload accepts explicit annotation object identifiers. The page's authoritative `/Annots` array is always projected, but only the selected annotation dictionaries are rewritten.

An empty target set is valid and produces a page-only projection. This supports annotation removal: `/Annots` can change without rewriting or deleting any detached annotation object.

## New-Annotation Integration

The BF-1053S pending-binding path now targets only newly allocated annotations. Existing bound annotations on the same page still participate in `/Annots` ordering but are not added to the changed-object set merely because a new annotation was inserted.

This preserves existing identifiers and reduces unnecessary incremental output.

## Bound-Annotation Planning

`PdfAnnotationFullRewritePlanner` and `PdfAnnotationIncrementalWritePlanner` provide explicit orchestration for already-bound annotation edits. Both accept the owning page identifier and the exact annotation identifiers whose dictionaries are targeted.

The incremental planner therefore changes only the owning page plus the selected annotation objects.

## Validation

Every explicitly targeted annotation identifier must correspond to an annotation currently attached to the target page. A target from another page or an unrelated object is rejected.

The existing strict two-argument projector continues to reject unbound annotations unless the internal reservation-aware BF-1053S path supplies pending identities.

## Preservation

Untargeted annotation dictionaries are not projected and their object-store references remain unchanged. The page dictionary continues to be shallow-copied and only `/Annots` is targeted by annotation structural projection.

Detached annotation backing objects are not deleted.

## Deferred Work

Dirty-state inference, annotation subtype-specific state, appearance streams, action/destination structures, popup relationships, Widget/AcroForm coordination, object deletion/free entries, parser-to-authoritative-model hydration, encrypted writing, and public Services/SDK orchestration remain deferred.

## Acceptance Criteria

1. Explicit annotation targeting projects only selected annotation dictionaries.
2. The owning page `/Annots` array is always projected from authoritative order.
3. An empty annotation target set supports page-only structural projection.
4. Annotation removal does not require rewriting or deleting detached annotation objects.
5. BF-1053S new-annotation planning does not rewrite unrelated existing annotations.
6. Incremental bound-annotation planning changes only the owning page and selected annotation objects.
7. Full-rewrite targeted projection leaves untargeted annotation object references stable.
8. Targets not attached to the supplied page are rejected.
9. No external dependency or CI prerequisite is added.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-23 | Added explicit annotation targeting and minimal bound/new annotation write planning. |
