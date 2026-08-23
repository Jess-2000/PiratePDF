# BF-1053V — Editing Change-Set Annotation Writing Orchestration

| Field | Value |
|---|---|
| **Document ID** | BF-1053V |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-23 |
| **Next Review** | 2026-11-23 |

## Objective

Bridge committed annotation common-state Editing transactions into preservation-aware Writing without adding an Editing dependency to the Writing assembly or requiring callers to manually translate stable editing IDs into PDF object identifiers.

## Layer Placement

The orchestration lives in `BlackFlag.Engine.Services`.

Services already coordinates higher-level engine workflows and depends on Editing. BF-1053V adds a Services dependency on Writing so it can translate Editing operations into Writing projections while preserving the lower-level rule that `BlackFlag.Engine.Writing` does not reference `BlackFlag.Engine.Editing`.

`BlackFlag.Engine.Editing` grants internals access to Services. Editable page and annotation handles expose their bound object identifiers internally only; no new public serialization identity is added to the editing API.

## Supported Change Sets

`PdfAnnotationCommonStateWritingService` accepts a committed `PdfEditChangeSet` containing only annotation common-state mutations:

- rectangle changes;
- contents changes;
- contents clearing;
- flags changes.

Structural annotation add/remove operations are rejected and remain on the BF-1053S/T structural paths.

Non-annotation operations are also rejected so the service cannot silently omit unrelated committed changes.

## Target Resolution

Each operation must belong to the supplied editing session.

Stable `PageId` and `AnnotationId` values from the committed operations are resolved against the current session-owned editable handles. The corresponding bound PDF object identifiers are collected per page.

Repeated edits to the same annotation in one transaction are deduplicated before projection.

The Writing plan must refer to the exact authoritative document owned by the editing session.

## Writing Modes

Incremental orchestration combines annotation-object-only projections across all affected pages and delegates once to the transactional projected incremental planner.

Full-rewrite orchestration uses the same combined projection set and delegates once to projection-aware full-rewrite planning.

Because BF-1053U object-only projection is used, owning page dictionaries are not rewritten for common-state-only transactions.

## Deferred Work

Automatic structural add/remove annotation orchestration, page edit change-set translation, metadata change-set translation, geometry change-set translation, dirty-state tracking outside committed transactions, subtype-specific annotation state, appearance streams, AcroForm/Widget coordination, object deletion/free entries, encrypted writing, and public SDK save workflows remain deferred.

## Acceptance Criteria

1. Services can translate committed common-state annotation operations into Writing targets.
2. Writing retains no production dependency on Editing.
3. Editable object identities remain internal rather than becoming public API.
4. Repeated changes to one annotation are deduplicated.
5. Incremental plans omit unchanged owning page objects for common-state-only transactions.
6. Full-rewrite plans apply the committed annotation state.
7. Change sets from another editing session are rejected.
8. Structural and non-annotation operations are rejected rather than silently ignored.
9. No external dependency or CI prerequisite is added.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added. The new Services-to-Writing project reference is an internal engine-layer dependency only.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-23 | Added Services-layer annotation common-state change-set Writing orchestration. |
