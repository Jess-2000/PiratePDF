# BF-1054L — Composite TrueType Glyph Dependency Closure and Subset Hardening

| Field | Value |
|---|---|
| **Document ID** | BF-1054L |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-05 |
| **Next Review** | 2026-12-05 |

## Objective

Complete the first TrueType subsetter's composite-glyph correctness before BF-1054 moves into Type0/CID Unicode font construction.

BF-1054L retains K's original-glyph-ID strategy and computes the transitive component dependency closure for selected composite `glyf` entries.

## Architectural Boundary

L changes only the trusted TrueType subset pipeline.

It does not introduce:

- glyph-ID renumbering;
- Type0/CID fonts;
- CFF/CFF2;
- platform font providers;
- PDF resource-scope changes;
- existing-text integration changes.

Because K deliberately preserves original glyph IDs, composite component references remain valid without rewriting.

## Composite Glyph Parsing

For every selected or dependency glyph, L reads the standard TrueType composite component records and handles:

- byte or word arguments;
- single scale;
- independent X/Y scales;
- two-by-two transforms;
- multiple components;
- optional composite instructions.

Component glyph IDs are validated against `maxp.numGlyphs`.

Malformed/truncated component data is rejected rather than ignored.

## Transitive Dependency Closure

Every selected glyph is visited recursively.

Composite component glyphs are added to the retained set and visited transitively until the dependency graph closes.

Glyph zero remains retained as established by K.

Shared component dependencies are processed once.

## Cycle and Resource Protection

`PdfTrueTypeSubsetOptions` provides independent subsetter limits:

- maximum composite recursion depth;
- maximum components in one composite glyph;
- maximum retained glyph count;
- maximum generated SFNT byte length.

Composite dependency cycles are detected explicitly through visiting/visited state.

These limits are defense-in-depth and remain separate from BF-1054I's external-font sanitizer limits.

## Original Glyph IDs

L does not compact the glyph array or rewrite component glyph IDs.

Unselected glyph slots remain represented by empty `loca` ranges.

Selected simple and composite glyph bytes are copied unchanged into rebuilt `glyf`.

This keeps component references stable and sharply reduces the mutation surface.

## Generated Font Trust

The generated subset continues through K's SFNT rebuilding and is then re-sanitized through `PdfOpenTypeFontSanitizer`.

Generated output exceeding the explicit subset byte limit is rejected before re-sanitization.

## Acceptance Criteria

1. A selected composite glyph is no longer rejected solely for being composite.
2. Direct component glyph dependencies are retained.
3. Nested composite dependencies are retained transitively.
4. Original glyph IDs and composite references remain unchanged.
5. Shared dependencies do not create duplicate traversal work.
6. Out-of-range component glyph references are rejected.
7. Truncated component records are rejected.
8. Component argument and transform lengths are bounds checked.
9. Composite instruction ranges are bounds checked.
10. Dependency cycles are rejected.
11. Composite recursion depth is bounded.
12. Components per composite glyph are bounded.
13. Total retained glyphs are bounded.
14. Generated subset byte size is bounded.
15. Generated fonts are re-sanitized.
16. Existing simple-glyph K behavior remains intact.
17. Existing subset-only embedding and ToUnicode behavior remains intact.
18. No raw font bytes bypass BF-1054I.
19. No external/native dependency is added.
20. The full PiratePDF release gate passes.

## Deferred Work

The following remain after L:

- glyph-ID compaction and composite-reference rewriting;
- Type0/CID Unicode TrueType font construction;
- CFF/CFF2 subsetting;
- inherited/indirect resource mutation;
- Form XObject resource ownership;
- filtered content-stream mutation;
- platform font-provider adapters;
- existing-text integration with newly embedded fonts.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-05 | Added recursive composite-glyph dependency closure, cycle/reference validation, subset resource limits, and generated-size hardening. |
