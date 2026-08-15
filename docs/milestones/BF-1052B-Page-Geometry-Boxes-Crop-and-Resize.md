# BF-1052B — Page Geometry, Boxes, Crop and Resize

| Field | Value |
|---|---|
| **Document ID** | BF-1052B |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1052 |
| **Last Updated** | 2026-08-14 |
| **Next Review** | 2026-11-14 |

## Purpose

BF-1052B establishes controlled structural editing for PDF page geometry on top of the BF-1051 transaction/history model and the BF-1052A structural-state contracts.

## Scope

The milestone covers the five standard page boxes (`MediaBox`, `CropBox`, `BleedBox`, `TrimBox`, and `ArtBox`), page rotation state, cropping, and geometry-only page resizing. Arbitrary content transformation, graphics selection, image editing, typography, serialization policy, and SDK exposure remain outside this slice.

## Authoritative geometry state

`PdfPage` remains the authoritative logical page model. It owns effective page-box values together with their `PdfPageValueOrigin`. Editing does not create a parallel geometry model. `PdfEditablePage` maps those authoritative values to BF-1052A `PdfStructuralValueState<T>` values for callers that need to distinguish explicit, inherited, and PDF-defaulted representations.

A newly created page explicitly owns its `MediaBox`. Its `CropBox` defaults to the effective `MediaBox`; `BleedBox`, `TrimBox`, and `ArtBox` default to the effective `CropBox`. Rotation defaults to zero unless explicitly supplied or later edited.

When an effective dependency changes, only boxes that are still defaulted follow that dependency. Explicit production boxes are not rewritten when the crop box changes, and an explicit crop box is not rewritten when the media box changes. This prevents an unrelated geometry edit from materializing or normalizing untargeted structures.

## Crop semantics

Cropping is a `CropBox` edit. It changes the visible/production boundary represented by the crop box and does not move, rewrite, or scale page content. Defaulted production boxes continue to resolve from the new crop box while remaining defaulted rather than becoming explicit.

## Resize semantics

Resize is geometry-only. It changes the `MediaBox` dimensions and never scales content streams, annotations, widgets, resources, or other page-owned structures. The operation records an explicit `PdfPageResizeAnchor` policy:

- `LowerLeft` keeps the existing lower-left media-box coordinate fixed.
- `Center` keeps the existing media-box center fixed.

A future higher-level operation may combine geometry resize with explicit content transformation, but BF-1052B does not silently do so.

## Transactions and exact restoration

Page geometry mutations use the existing editing-session validator and transaction machinery. Change records capture the exact prior effective state and representation origin. Rollback, undo, and redo restore that state rather than merely reconstructing numerically equivalent geometry. Rotation rollback likewise restores the prior origin instead of forcing an originally defaulted value to become explicit.

## Preservation

Only the targeted page geometry is mutated. Editing one production box does not materialize sibling boxes. Defaulted geometry remains defaulted where it remains untargeted. Existing content and resources are untouched by crop and resize operations. Dictionary serialization remains the responsibility of the document/Writing boundary; this milestone only ensures dictionary snapshots emit page boxes that are logically explicit.

## API boundary

BF-1052B adds engine editing operations and state exposure. It intentionally does not expand the isolated SDK contracts; BF-1052I owns structural SDK/service integration after the engine contracts have stabilized.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-14 | Added missing revision history section. |
