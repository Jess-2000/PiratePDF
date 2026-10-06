# BF-1053N — Page Geometry Dictionary Projection

| Field | Value |
|---|---|
| **Document ID** | BF-1053N |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053, BF-1052 |
| **Last Updated** | 2026-08-22 |
| **Next Review** | 2026-11-22 |

## Objective

Introduce the first page-structure Writing projector by translating authoritative page geometry state into preservation-aware indirect page-dictionary projections.

## Scope

`PdfPageGeometryWriteProjector` accepts a document and explicit page object identifiers. Each target must identify a page currently bound into the authoritative document page tree and an existing indirect PDF dictionary.

The projector updates the standard page geometry keys:

- `/MediaBox`
- `/CropBox`
- `/BleedBox`
- `/TrimBox`
- `/ArtBox`
- `/Rotate`

Explicit geometry is serialized into the page dictionary. Inherited or defaulted geometry is represented by absence of the corresponding page-level key rather than being unnecessarily materialized.

## Preservation

Projection starts from a shallow copy of the existing indirect page dictionary. Entries outside the targeted geometry keys therefore retain their existing `PdfObject` references, including private or application-specific entries.

The source dictionary is not mutated while the projection is being constructed.

## Layer Boundary

Writing needs the Document model's existing internal object binding and effective geometry state to translate logical page state without introducing a second public identity API solely for serialization. `BlackFlag.Engine.Document` therefore grants internals access to `BlackFlag.Engine.Writing` and its test assembly.

This is an assembly-level architectural friendship between adjacent engine layers; Writing still has no production dependency on Editing.

## Integration

The resulting `PdfObjectWriteProjectionSet` can use the transactional incremental projection path from BF-1053H/K or the projection-aware full-rewrite path from BF-1053M.

This slice deliberately does not infer dirty pages. Callers must explicitly identify the page objects whose geometry is targeted.

## Deferred Work

Page insertion/removal/reordering, page-tree `/Kids` and `/Count` projection, annotations, AcroForm structures, outlines, page decorations, content-stream rewriting, deletion/free entries, encrypted writing, and public Services/SDK orchestration remain deferred.

## Acceptance Criteria

1. Explicit standard page boxes project to the corresponding PDF page dictionary keys.
2. Explicit rotation projects to `/Rotate`.
3. Defaulted or inherited page-level geometry removes the corresponding explicit page key.
4. Untargeted page dictionary entries retain their exact object references.
5. Projection construction does not mutate the source page dictionary.
6. Targets must be bound authoritative pages backed by indirect dictionaries.
7. Projections remain compatible with both existing projected incremental and projected full-rewrite planning.
8. Writing gains no production dependency on Editing.
9. No external dependency or CI prerequisite is added.
10. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-22 | Added preservation-aware page geometry dictionary projection. |
