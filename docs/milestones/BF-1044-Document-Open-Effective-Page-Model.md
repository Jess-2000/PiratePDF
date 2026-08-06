# BF-1044 — Document Open and Effective Page Model

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1044 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1042, BF-1043, BF-1044 |
| **Last Updated** | 2026-08-06 |
| **Next Review** | 2026-11-06 |

## Purpose

BF-1044 introduces the immutable effective-page layer used by rendering,
measurement, editing, printing, and prepress systems. Page-tree inheritance is
resolved centrally so downstream components do not walk `/Pages` ancestors.

## Delivered Capability

BF-1044 includes:

- immutable effective-page objects and collections;
- MediaBox, CropBox, BleedBox, TrimBox, and ArtBox resolution;
- explicit, inherited, and defaulted value tracking;
- inherited Resources and Rotate handling;
- UserUnit handling;
- rotation-aware display geometry;
- page-tree cycle and duplicate-page detection;
- maximum depth and page-count safeguards; and
- malformed `/Kids` and page-node validation.

## Effective Geometry

The effective page model exposes MediaBox, CropBox, BleedBox, TrimBox, ArtBox,
Rotate, and UserUnit together with each value's origin. BleedBox, TrimBox, and
ArtBox fall back to the effective CropBox when not explicitly present.

The implementation keeps effective values separate from the existing editable
page model so authored PDF dictionaries remain available for later editing and
round-trip preservation.

## Page-Tree Loading

The loader traverses indirect `/Pages` and `/Page` nodes through the BF-1043
object resolver. Inherited MediaBox, CropBox, Rotate, and Resources values are
resolved once while traversing the tree. Downstream systems consume the
resulting effective page model instead of repeating inheritance logic.

## Defensive Loading

The loader rejects:

- cyclic page trees;
- duplicate page objects;
- unsupported page-tree node types;
- malformed or non-reference `/Kids` entries;
- missing effective MediaBox values;
- invalid page-box arrays;
- excessive page-tree depth; and
- excessive page counts.

## Design Boundaries

BF-1044 establishes the effective page model and page-tree loader. It does not
yet parse page content operators, resolve fonts or XObjects, render pages, edit
page dictionaries, or save PDF files. Those responsibilities remain assigned
to later packages.

## Validation

The release gate requires:

- a Release solution build;
- all automated tests;
- documentation index generation;
- documentation validation with zero errors and warnings; and
- a clean `git diff --check` result.

## Revision History

- 2026-08-06: Initial BF-1044 implementation.
