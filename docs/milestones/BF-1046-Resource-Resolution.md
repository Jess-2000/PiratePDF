# BF-1046 — Canonical PDF Resource Resolution

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1046 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1043, BF-1044, BF-1045, BF-1046 |
| **Last Updated** | 2026-08-07 |
| **Next Review** | 2026-11-07 |

## Purpose

BF-1046 translates named PDF resources into immutable canonical resource
objects. Downstream graphics interpretation and rendering no longer need to
walk raw `/Resources` dictionaries, dereference resource objects, or duplicate
nested-scope fallback logic.

## Delivered Capability

BF-1046 includes:

- immutable canonical resource identities and resource models;
- lazy resource lookup with parent-scope fallback;
- indirect-object identity caching and deterministic cycle protection;
- font resource metadata for Type 1, TrueType, Type 0/CID, Type 3, CFF, and
  OpenType-backed PDF fonts;
- normalized font family and face names, weight, stretch, slant, subset state,
  font technology, ToUnicode, descriptors, descendants, and embedded programs;
- contracts for variable-font axes and embedding permissions so later editing
  can change fonts without redesigning the font model;
- Image and Form XObject resolution without eager pixel decoding or content
  interpretation;
- nested Form XObject resource scopes;
- DeviceGray, DeviceRGB, DeviceCMYK, CalGray, CalRGB, Lab, ICCBased, Indexed,
  Pattern, Separation, and DeviceN color-space metadata;
- preservation of spot-color names, alternate spaces, and tint transforms;
- ExtGState metadata including alpha, overprint, blend mode, and soft masks;
- tiling/shading pattern and shading-type recognition; and
- defensive resource, font, image, ICC, and nesting limits.

## Canonical Font Model

PDF subset prefixes are removed from the user-facing face name while subset
identity remains available separately. Font metadata retains real weight,
stretch, and slant information instead of reducing every family to only
regular, bold, italic, and bold-italic variants. The contracts also preserve a
future path for variable-font axes, Unicode coverage analysis, system font
catalogs, font matching, substitution, and user-directed font replacement.

BF-1046 does not perform glyph shaping, glyph rasterization, system-font
enumeration, or text re-layout. Those capabilities remain owned by later text,
editing, and rendering layers.

## Resource Scopes

A `PdfResourceScope` owns lookup for one resource dictionary and optionally
falls back to a parent scope. Local names always take precedence. Form XObjects
can expose a nested scope lazily so resource dictionaries are not traversed
until form content actually requires them.

This is the single implementation of resource fallback. Downstream systems do
not reimplement page or Form XObject resource lookup rules.

## Images and Color

Image XObjects retain encoded stream bytes, filter declarations, masks, soft
masks, color-space references, dimensions, and component depth. BF-1046 does
not decode pixels. Separation and DeviceN spaces retain spot-color names,
alternate spaces, and tint transforms rather than flattening them to RGB,
which preserves information needed for professional prepress workflows.

## Design Boundaries

BF-1046 resolves resource structure and metadata. It does not:

- mutate graphics state;
- interpret page drawing operations;
- decode image pixels;
- render fonts or images;
- perform color conversion;
- construct display lists; or
- edit document content.

Those responsibilities remain assigned to BF-1047 through BF-1049 and later
font/import subsystems.

## Maintenance Included

The BF-1046 baseline also contains the approved behavior-preserving filter
cleanup: the ASCII85 tuple writer uses a bounded loop, ASCIIHex classification
uses a switch expression, and PDF filter whitespace classification has one
shared owner.

## Validation

The release gate requires:

- a Release solution build;
- all automated tests;
- documentation index generation;
- documentation validation with zero errors and warnings; and
- a clean `git diff --check` result.

## Revision History

- 2026-08-07: Initial BF-1046 implementation.
