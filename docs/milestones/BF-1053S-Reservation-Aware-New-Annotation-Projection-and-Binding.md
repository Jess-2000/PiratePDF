# BF-1053S — Reservation-Aware New-Annotation Projection and Binding

| Field | Value |
|---|---|
| **Document ID** | BF-1053S |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-23 |
| **Next Review** | 2026-11-23 |

## Objective

Extend preservation-aware annotation Writing from already-bound annotations to newly created annotations by reserving indirect-object identities and committing model bindings only after planning succeeds.

## Scope

BF-1053S adds full-rewrite and incremental planners for annotations attached to a bound page but not yet assigned indirect-object identifiers.

A shared reservation-aware batch reserves one generation-zero identifier for each unbound annotation on the target page. The BF-1053R projector gains an internal pending-binding path that can construct new annotation dictionaries before those objects exist in the document object store.

The owning page's `/Annots` array is projected in authoritative collection order and may contain both existing bound annotations and newly reserved annotation identifiers.

## Transaction Boundary

New annotation bindings are committed only after the underlying projected planner succeeds.

Until that point, reserved identifiers exist only as Writing-layer pending assignments. If projected full-rewrite or incremental planning fails, the existing projection transaction restores the object store and the authoritative annotations remain unbound.

No general-purpose annotation unbind operation is introduced solely for Writing rollback.

## Preservation

Existing annotation dictionaries continue to be shallow-copied and preserve untargeted entries according to BF-1053R.

New annotation dictionaries contain the common state supported by BF-1053R: `/Type`, `/Subtype`, `/Rect`, optional `/Contents`, and `/F`.

Existing bound annotations on the same page participate in the authoritative `/Annots` ordering but retain their existing identifiers.

## Output Modes

Full rewrite refreshes the write plan so newly projected annotation identifiers participate in complete serialization.

Incremental planning targets the newly created annotation objects and the owning page dictionary.

## Deferred Work

Annotation subtype-specific dictionaries, appearance streams, action/destination structures, popup relationships, Widget/AcroForm coordination, object deletion/free entries, parser-to-authoritative-model hydration, encrypted writing, and public Services/SDK orchestration remain deferred.

## Acceptance Criteria

1. Every unbound annotation on the target page receives a unique generation-zero identifier.
2. `/Annots` contains existing and newly allocated annotation references in authoritative order.
3. Newly created annotation dictionaries serialize BF-1053R common state.
4. Full-rewrite plans include newly projected annotation identifiers.
5. Incremental plans target new annotation objects and the owning page dictionary.
6. Annotation bindings are committed only after projected planning succeeds.
7. Failed projected planning leaves new annotations unbound and removes their projected objects from the object store.
8. Direct BF-1053R projection remains strict for unbound annotations.
9. No external dependency or CI prerequisite is added.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-23 | Added reservation-aware new-annotation projection and post-planning model binding. |
