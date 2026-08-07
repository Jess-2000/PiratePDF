# BF-1047 — Graphics Interpretation and Display-List Construction

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1047 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1044, BF-1045, BF-1046, BF-1047 |
| **Last Updated** | 2026-08-07 |
| **Next Review** | 2026-11-07 |

## Purpose

BF-1047 combines canonical content-stream operations from BF-1045 with
canonical resources from BF-1046 and produces an immutable semantic display
list. This is the first layer that represents what a PDF page draws rather
than only how its syntax and resources are encoded.

## Delivered Capability

BF-1047 includes:

- immutable affine matrices, points, conservative display bounds, path
  segments, path geometry, and clipping nodes;
- immutable graphics-state snapshots with line, color, transparency, text,
  overprint, rendering-intent, dash, and clipping state;
- path construction, painting, pending clipping, graphics-state save/restore,
  and transformation interpretation;
- PDF text-object state including font selection, size, spacing, leading,
  horizontal scaling, rendering mode, rise, text matrices, and source text;
- editable text-run display items that retain resolved font identity, encoded
  bytes, source Unicode where directly available, TJ adjustments, source spans,
  and text transforms;
- canonical image placement for both Image XObjects and inline images without
  eager pixel decoding;
- recursive Form XObject interpretation with canonical resource scopes,
  inherited graphics state, Form matrices, configurable depth limits, and
  deterministic recursion detection;
- preservation of DeviceGray, DeviceRGB, DeviceCMYK, custom resolved color
  spaces, patterns, Separation/DeviceN semantics, overprint, blend-mode, alpha,
  and soft-mask references without flattening to RGB;
- semantic shading items;
- marked-content contexts and MP/DP points for future tagged-PDF,
  accessibility, extraction, and diagnostics work; and
- configurable limits for graphics-state, Form, marked-content, path, clip,
  and display-list growth.

## Display-List Ownership

BF-1047 owns graphics interpretation. It converts PDF operators and canonical
resources into renderer-independent semantic items. BF-1048 consumes those
items but does not repeat PDF operator semantics.

The primary item model includes paths, text runs, images, Forms, shadings, and
marked-content points. Items retain their source spans, active graphics state,
clip state, resource references, and marked-content ancestry where relevant.

## Text and Editing Readiness

Text is not flattened to outlines. A text run retains the resolved canonical
font, source encoded bytes, font size, text transformation, PDF text-state
information, and spacing adjustments. This preserves the path required by
future editing to select existing text, replace its font, re-layout affected
content, and rewrite PDF resources while respecting embedding rights.

BF-1047 does not perform system-font discovery, font substitution, shaping,
glyph rasterization, or final text layout. Those remain owned by later font,
editing, and rendering subsystems.

## Color and Prepress Fidelity

Display-list colors preserve the PDF color-space semantics used by the source.
DeviceCMYK, ICC-based spaces, Separation, DeviceN, and pattern relationships
are not converted to RGB during interpretation. ExtGState overprint, alpha,
blend-mode, and soft-mask metadata remain attached to the interpreted state.

This preserves the data required by later color management, separations,
preflight, print preview, and commercial-print workflows.

## Form XObjects

Rendering does not depend on the Parsing project. Form content is supplied
through `IPdfFormOperationProvider`, which lets the document/parsing layer
provide already parsed canonical operations while keeping dependency direction
clean. Nested Form resource scopes are supplied by BF-1046.

Recursive Forms and excessive nesting fail deterministically.

## Design Boundaries

BF-1047 does not:

- rasterize paths;
- rasterize or shape glyphs;
- eagerly decode image pixels;
- convert source colors to screen RGB;
- allocate GPU resources;
- perform user-interface selection;
- rewrite content streams;
- perform text editing; or
- automatically substitute fonts.

Those responsibilities remain assigned to BF-1048, BF-1049, and later font
and editing subsystems.

## Validation

The release gate requires:

- a Release solution build;
- all automated tests;
- documentation index generation;
- documentation validation with zero errors and warnings; and
- a clean `git diff --check` result.

## Revision History

- 2026-08-07: Initial BF-1047 implementation.
