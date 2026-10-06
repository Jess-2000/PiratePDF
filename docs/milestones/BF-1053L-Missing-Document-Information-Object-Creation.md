# BF-1053L — Missing Document Information Object Creation

| Field | Value |
|---|---|
| **Document ID** | BF-1053L |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-20 |
| **Next Review** | 2026-11-20 |

## Objective

Complete the document-information writing path introduced in BF-1053I by safely creating a new indirect `/Info` dictionary when a document has logical information values but no existing information object.

## Allocation and Projection

`PdfDocumentInformationIncrementalWritePlanner` coordinates document-information projection with a `PdfWritePlan` and original source revision.

When an `/Info` object already exists, the planner preserves the BF-1053I behavior and projects only explicitly targeted fields onto the existing dictionary.

When `/Info` is absent, the planner reserves a generation-zero indirect object through the reservation-aware allocator introduced in BF-1053J, projects the targeted logical fields into a new dictionary, assigns the trailer `/Info` reference, and delegates to transactional projected incremental planning.

An empty information dictionary is not created when all targeted logical fields are absent.

## Transactional Safety

Creating `/Info` changes both the indirect-object store and the trailer. BF-1053K already restores projected object-store mutations when downstream incremental planning fails. This slice adds the corresponding trailer rollback: if planning fails after a new `/Info` reference is assigned, the trailer returns to its previous absent state while BF-1053K removes the newly projected object.

This keeps object and trailer state consistent across failure paths.

## Preservation

Existing information dictionaries continue to be shallow-copied so untargeted entries retain their exact `PdfObject` references. Existing information object identifiers are never replaced merely because targeted metadata changed.

New dictionaries contain only explicitly present targeted logical values. Explicit empty strings remain present values.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added. Writing remains independent from Editing; Editing is referenced only by Writing tests where existing public editing APIs are used to establish logical metadata state.

## Deferred Work

This slice does not project page geometry, annotations, AcroForm fields, outlines, page decorations, object deletion/free entries, encrypted writing, or application UI.

## Acceptance Criteria

1. Missing `/Info` can be allocated and projected for an incremental update.
2. New information objects use generation zero and reservation-aware allocation.
3. The trailer `/Info` reference points to the newly projected object after successful planning.
4. Existing `/Info` objects retain their identifiers.
5. Failed downstream planning restores an originally absent trailer `/Info` reference and removes the new projected object.
6. All-absent targeted values do not create an empty information dictionary.
7. Existing BF-1053I preservation and string-encoding behavior remains intact.
8. No external dependency or CI prerequisite is added.
9. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-20 | Added reservation-aware missing `/Info` creation and transactional trailer rollback. |
