# BF-1054G — Canonical Existing-Font Selection and Resource Integration

| Field | Value |
|---|---|
| **Document ID** | BF-1054G |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1046, BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-03 |
| **Next Review** | 2026-12-03 |

## Objective

Deliver real existing-text font switching using font resources that are already resolved and available in the target PDF resource scope.

BF-1054G establishes a canonical document-resource font candidate model and changes `Tj`/`TJ` text to a selected existing font while preserving the surrounding font state.

## Scope Refinement

The post-BF-1054F repository contains:

- canonical `PdfResolvedFont` metadata;
- font technology/slant/weight/stretch metadata;
- ToUnicode and encoding contracts;
- embedding-rights metadata;
- resource resolution;
- same-font encoding;
- Editing resource mutation;
- Writing object allocation.

It does **not** yet contain a system-font catalog/provider, general font-program writer, subsetter, glyph-table builder, or external-font embedding pipeline.

Therefore BF-1054G does not pretend to import arbitrary operating-system fonts.

External font discovery, font-program embedding, subsetting, ToUnicode generation, widths/descriptor construction, and new font-object allocation remain a dedicated follow-on font-infrastructure slice.

## Active Resource Font Catalog

`PdfExistingTextDiscoverySource` may now carry an immutable map of already-resolved font resources in its active resource scope.

Existing constructors remain source-compatible because the map is optional.

`PdfExistingFontCandidate` exposes stable selection metadata:

- resource name;
- family and face;
- weight;
- stretch;
- slant;
- technology;
- embedding-rights metadata;
- subset state;
- ToUnicode availability.

`PdfExistingFontSelectionService` enumerates candidates deterministically and supports exact family matching with optional weight/slant constraints.

This is a document-resource catalog, not a system-font catalog.

## Font Switching

`PdfExistingTextFontService.ChangeFont` supports unshared page-owned `Tj` and `TJ` targets that are already editable under BF-1054C/D.

The selected font must already exist in the source's active resource map.

Before mutation, every existing Unicode text segment is encoded through the selected font's existing encoding/ToUnicode-compatible mapping using the established font encoder.

If any text cannot be represented, the entire font switch is rejected before mutation.

## Source Mutation

Direct `Tj` text is re-encoded with the BF-1054C string-token path.

Positioned `TJ` text is re-encoded one text element at a time with the BF-1054D array-token path, preserving numeric adjustments.

After re-encoding, BF-1054G inserts:

```text
/<selected-font> <current-size> Tf
<original text-showing operation>
/<original-font> <current-size> Tf
```

The original resource is restored immediately after the target, preventing the font change from leaking to later text.

The current font size is preserved.

## Resource and Embedding Semantics

Selecting an already-present PDF font resource does not embed a new font program, allocate a new font object, or modify the resource dictionary.

Embedding-rights metadata is exposed on candidates for later external-font workflows, but no new embedding action occurs in BF-1054G.

A missing resource is rejected rather than synthesized.

## Preservation

BF-1054G:

- retains source provenance validation;
- retains literal/hex string token classes through C/D token editors;
- preserves positioned `TJ` numeric adjustments;
- changes only the target's encoded string bytes plus scoped `Tf` operators;
- restores the previous font immediately after the target;
- uses the existing `PdfExistingTextEditOperation`, transaction, undo/redo, and Writing path;
- does not introduce a new save domain.

## Acceptance Criteria

1. Existing-text sources can expose an immutable active-resource font map.
2. Existing source-constructor compatibility is preserved.
3. Font candidates expose canonical family/face/weight/stretch/slant/technology metadata.
4. Candidate ordering and matching are deterministic.
5. A direct `Tj` target can switch to another existing font resource.
6. Positioned `TJ` text can switch fonts without changing numeric adjustments.
7. Existing Unicode must encode completely through the selected font.
8. Unrepresentable text fails before mutation.
9. Missing font resources are rejected.
10. The selected `Tf` is scoped immediately before the target.
11. The original font resource and font size are restored immediately after the target.
12. Later text remains on the original font.
13. Font switching participates in transaction undo/redo.
14. No new font object/resource is allocated.
15. No font program is embedded or subset.
16. Existing C/D/F preservation boundaries remain intact.
17. No Rendering interpreter change is required.
18. The full PiratePDF release gate passes.

## Deferred Font Infrastructure

A later font-infrastructure slice is required before PiratePDF can claim arbitrary font-family changes from operating-system or user-installed fonts.

That work must define:

- platform-neutral font catalog/provider contracts;
- family/face matching and substitution policy;
- glyph coverage;
- font-program loading;
- embedding-rights enforcement;
- subsetting/full embedding policy;
- font descriptors and widths;
- encoding/CMap and ToUnicode generation;
- resource-name allocation;
- indirect-object allocation;
- full-rewrite/incremental projections;
- Windows/macOS adapters.

This deferral prevents BF-1054G from creating a partial or unsafe embedding system.

## PDF Version Policy

Font switching uses existing PDF text/resource constructs valid in the established PDF 1.7 and PDF 2.0 writing paths.

Document version selection remains unchanged.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-03 | Added canonical existing-resource font candidates and provenance-backed font switching without external font embedding. |
