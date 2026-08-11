
# BF-1048F - Patterns, Shadings, and Type 3

| Field | Value |
|---|---|
| **Document ID** | BF-1048F |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1046, BF-1047, BF-1048 |
| **Last Updated** | 2026-08-10 |
| **Next Review** | 2026-11-10 |

## Objective

Extend BF-1048 with renderer-neutral PDF function evaluation, axial/radial
shading realization, and a nested-content boundary for Type 1 tiling patterns
and Type 3 CharProcs.

## Implemented Scope

- Reusable bounded Type 2 exponential-interpolation function evaluation.
- Reusable bounded Type 3 stitching-function evaluation.
- Canonical axial and radial shading descriptors.
- Deterministic sampled sRGB realization for axial/radial shadings.
- Skia linear and two-point-conical shading realization.
- Shading display-list rendering through the existing `PdfDisplayShading`
  semantic item.
- Renderer-neutral nested-content contract for Type 1 pattern cells and
  Type 3 glyph CharProcs.
- Parsing-side provider that decodes, parses, resolves nested resources, and
  interprets pattern/CharProc streams without introducing a Rendering-to-
  Parsing dependency.
- Type 3 glyph rendering through the same nested semantic display-list
  renderer used by ordinary PDF graphics.
- Explicit resource limits for Type 3, tiling-pattern, and shading work.
- Regression tests for Type 2/3 functions, axial shading decoding, and new
  nested-rendering limits.

## Deliberate Boundaries

BF-1048F does not claim approximate support for PDF constructs that still
require deeper bounded infrastructure.

The following remain explicit BF-1048G integration/fidelity work:

- Shading types 1 and 4 through 7, including free-form, lattice, Coons-patch,
  and tensor-product patch meshes.
- Full Type 1 tiling-pattern raster repetition and uncolored PaintType 2
  substitution in the Skia backend.
- Type 0 sampled and Type 4 PostScript calculator functions.
- Indirect function members that have not been resolved by the resource layer.
- Advanced shading color spaces beyond the currently supported device-space
  preview path.
- Full Type 3 clipping-mode interaction and nested Type 3 recursion accounting
  across document-wide render sessions.

Unsupported cases remain diagnosable rather than silently flattened or
fabricated.

## Provenance

BF-1048F follows ADR-0014. The implementation was independently constructed
from PiratePDF contracts and PDF semantics; no external PDF repository
implementation was copied into the project.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-10 | Added reusable PDF function evaluation, axial/radial shading rendering, nested pattern/Type 3 contracts, parsing provider, Type 3 display-list rendering, limits, and tests. |
