# BF-1051B — Editable Document and Page Model

| Field | Value |
|---|---|
| **Document ID** | BF-1051B |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Introduce the session-owned editable document and page model that later BF-1051 mutation and transaction slices can operate on without exposing unrestricted mutation of the authoritative high-level document model.

## Contract

- Every `PdfEditingSession` exposes one stable `PdfEditableDocument` facade.
- `PdfEditableDocument` wraps the authoritative `PdfDocument`; it does not copy or replace the document model.
- Source pages receive stable `PdfEditablePage` handles in their opening order.
- A source-page handle records the page index observed when the editing session opens. This is provenance, not a promise that the page remains at that index after later controlled mutations.
- Session-created pages are detached editing state until a later explicit mutation operation inserts them into the document page tree.
- Creating editable state alone does not mutate the source document.
- The raw mutable `PdfDocument`, `PdfPageTree`, and `PdfPage` instances are not exposed publicly through the editable facade.
- Page handles carry session ownership internally so later mutation operations can deterministically reject cross-session and cross-document use.

## Preservation and FusionPro Compatibility

BF-1051B preserves source page identity and order without cloning, normalizing, or reconstructing page objects merely because editing begins. Opening a session and enumerating editable pages therefore produces no structural change to the source document.

This preservation behavior supports targeted FusionPro compatibility by ensuring that untouched page structures are not gratuitously replaced or reordered before an edit explicitly requires a change. Byte serialization and final FusionPro output interoperability remain responsibilities of later writing milestones.

## Transaction Boundary

BF-1051B intentionally does not expose page-tree insertion, removal, movement, rotation changes, or other concrete mutation methods. Those operations begin in BF-1051C and are subsequently made transactional in BF-1051D. This keeps the editable state model from creating an accidental mutation path that bypasses later validation and transaction rules.

## Testing Strategy

- Real `PdfDocument` and `PdfPage` objects verify facade behavior and identity preservation.
- Tests verify stable source-page handles, opening indexes, page order, detached session-created pages, and absence of source-tree mutation during page creation.
- Foreign-session handles are distinguishable by ownership and are not members of another document's editable page collection.
- Moq is not required in BF-1051B because the behavior under test is the concrete editing model rather than an external service boundary.

## Optimization Review

- Opening an editing session creates one lightweight editable-page handle per existing high-level page.
- No PDF object graph, page dictionary, resource dictionary, content stream, or byte buffer is cloned.
- Page lookup by index remains constant time through the backing list.
- Creating a detached page allocates only the high-level `PdfPage` and its editable handle.
- The model performs no parsing, resource resolution, serialization, or reflection.

## Acceptance Criteria

1. An editing session exposes a stable editable-document facade.
2. The facade reports document version and source-page count without exposing unrestricted raw-model mutation.
3. Existing pages receive stable handles that preserve high-level page identity and opening order.
4. Source-page handles record their opening page index.
5. Session-created pages remain detached until a later explicit mutation operation attaches them.
6. Creating a session-created page does not change the source document page tree.
7. Editing-page ownership is session-specific and suitable for later cross-session mutation rejection.
8. No concrete page-tree mutation or transaction behavior is introduced before its dedicated BF-1051 slice.
9. Full PiratePDF release gate passes, including documentation generation and validation.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added the session-owned editable document facade, stable source-page handles, detached session-created pages, and preservation-first page-model contracts. |
