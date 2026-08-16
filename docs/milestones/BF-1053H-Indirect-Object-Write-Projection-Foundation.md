# BF-1053H — Indirect-Object Write Projection Foundation

| Field | Value |
|---|---|
| **Document ID** | BF-1053H |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-16 |
| **Next Review** | 2026-11-15 |

## Objective

Establish the preservation-aware boundary that converts explicitly projected indirect-object values into the exact changed-object set consumed by incremental Writing.

## Projection Contract

`PdfObjectWriteProjection` pairs an indirect-object identifier with its projected value. `PdfObjectWriteProjectionSet` snapshots, sorts, and validates projections before they reach the object store. Existing objects may be replaced. New objects must use generation zero and respect the write plan allocation boundary.

`PdfProjectedIncrementalWritePlanner` applies validated projections and delegates the exact identifier set to `PdfIncrementalWritePlanner`, allowing BF-1053F/G serializers to emit only those objects.

This deliberately keeps Editing independent from Writing. Later structure-specific projectors can translate logical edited structures into these format-level contracts without introducing a parallel editing model.

## Deferred Work

BF-1053H does not yet translate page geometry, metadata, annotations, AcroForm fields, outlines, or page decorations into PDF dictionaries and arrays. Deletion/free-entry projection, encrypted writing, dirty tracking, and Services/SDK exposure remain deferred.

## Acceptance Criteria

1. Projected values have explicit stable indirect-object identifiers.
2. Projection sets are deterministic and reject ambiguous active generations.
3. Existing objects can be replaced and new objects can be allocated safely.
4. The incremental changed-object set exactly matches the projections.
5. Editing remains independent from Writing.
6. No external dependency or CI prerequisite is added.
7. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-16 | Added indirect-object write projection foundation. |
