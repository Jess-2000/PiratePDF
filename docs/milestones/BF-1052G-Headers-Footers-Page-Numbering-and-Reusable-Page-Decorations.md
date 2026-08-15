# BF-1052G — Headers, Footers, Page Numbering, and Reusable Page Decorations

| Field | Value |
|---|---|
| **Document ID** | BF-1052G |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1052 |
| **Last Updated** | 2026-08-14 |
| **Next Review** | 2026-11-14 |

## Purpose

BF-1052G establishes reusable logical page-decoration definitions for headers, footers, and generated page numbers on top of the BF-1051 transaction/history model and BF-1052 structural-editing contracts.

## Scope

The slice adds one authoritative ordered `PdfPageDecorations` owner per document, stable decoration identity, header/footer regions, left/center/right logical placement, declarative page targeting, literal text decorations, generated page-number decorations, controlled add/remove/update mutations, validator-first mutation, rollback, undo/redo, and preservation-oriented tests.

Content-stream realization, font selection, text measurement, collision avoidance, margin UI, appearance styling, serialization, indirect-object allocation, xref behavior, and byte writing remain outside this slice.

## Authoritative state and ownership

`PdfDocument` owns one `PdfPageDecorations` collection. Each `PdfPageDecoration` has stable logical identity independent of attached/detached state. Editing exposes stable `PdfEditablePageDecoration` handles rather than the mutable source objects.

A decoration is either literal text or a generated page number. Its logical placement is represented by a header/footer region plus left/center/right alignment. Physical coordinates are intentionally deferred until content realization because they depend on page geometry, font metrics, and application/output policy.

## Page targeting

`PdfPageDecorationTarget` uses a zero-based inclusive first page index, an optional inclusive last page index, and an all/odd/even physical-page parity filter. An omitted last index means the rule continues through the logical end of the document.

Parity is based on the one-based physical page ordinal. Targets are declarative filters rather than references to page objects, so inserting, moving, or removing pages cannot create dangling decoration references. A range can temporarily select no pages after structural editing without invalidating the document model.

## Page numbering

Page-number decorations own immutable `PdfPageNumberingOptions` containing the first displayed number, decimal/lower-Roman/upper-Roman formatting intent, and optional literal prefix/suffix text.

The first displayed number is assigned to the target's first page index. Later targeted pages advance by physical page offset from that start. If an odd/even filter skips a page, numbering still reflects the physical pagination rather than compressing the selected pages into a separate sequence.

Formatting intent is logical state only. Converting the numeric value to final glyphs and placing those glyphs in a page content stream belong to later content/font/Writing work.

## Mutation contract

`PdfPageDecorationEditOperation` is an `IPdfStructuralEditOperation` with document scope and `PdfStructureKind.PageDecoration`. Attachment transitions use absent/explicit structural state. Common-state changes carry exact before/after `PdfPageDecorationSnapshot` values.

Literal text can only be set on text decorations. Numbering options can only be set on page-number decorations. Validators run before authoritative state changes. Existing editing transactions/history provide rollback, undo, and redo; there is no decoration-specific history subsystem.

## Preservation

Editing one decoration property preserves untargeted kind, placement, target, text/numbering state, other decoration definitions, document pages, annotations, forms, outlines, metadata, content, and resources. The engine does not materialize page content streams or rewrite unrelated page structures during logical decoration editing.

The targeted FusionPro preservation contract remains authoritative. Editing decides what the logical document becomes; later content realization and Writing decide how that state is represented in PDF objects and bytes.

## Testing strategy

Tests cover empty-document ownership, page-range/parity behavior, physical-offset numbering semantics, stable decoration identity, structural operation classification, type-specific state restrictions, untargeted-state preservation, rollback, committed undo/redo, validator-before-mutation behavior, and cross-session ownership rejection.

## Acceptance criteria

1. Every logical document owns one authoritative ordered page-decoration collection.
2. Decoration handles retain stable identity across attach/remove/undo/redo transitions.
3. Text and page-number decorations share reusable placement and targeting primitives without conflating their type-specific state.
4. Header/footer region and left/center/right alignment are explicit logical state.
5. Page targets support inclusive ranges plus all/odd/even physical-page parity.
6. Page-number definitions support a first displayed number, decimal/Roman formatting intent, and literal prefix/suffix state.
7. Numbering advances by physical page offset from the target start even when parity filters skip pages.
8. Invalid text-on-page-number and numbering-on-text transitions are rejected before mutation.
9. Page-decoration edits use `PdfStructureKind.PageDecoration`, validator-first mutation, and existing rollback/undo/redo machinery.
10. Content-stream realization, typography, physical coordinate layout, application UI, and Writing remain deferred.
11. No external package dependency is introduced.
12. The release gate passes with zero compiler/analyzer/test/documentation failures.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-14 | Added reusable logical header/footer/page-number decoration ownership, targeting, numbering semantics, and preservation/history coverage. |
