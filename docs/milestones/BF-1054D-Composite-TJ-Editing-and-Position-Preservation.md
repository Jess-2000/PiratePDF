# BF-1054D — Composite TJ Editing and Position Preservation

| Field | Value |
|---|---|
| **Document ID** | BF-1054D |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1047, BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-08-27 |
| **Next Review** | 2026-11-27 |

## Objective

Extend existing-text editing to composite positioned `TJ` arrays without flattening their text/positioning structure.

BF-1054D edits one replacement string per original text element and preserves every numeric positioning adjustment and every non-string source byte in the `TJ` operation.

## Rendering Characterization

BF-1047 already exposes `PdfDisplayTextRun.Elements` as an ordered sequence of text and adjustment elements.

The pre-BF-1054 Rendering characterization test `TextArrayPreservesOrderedTextAndAdjustments` verifies that `TJ` order and numeric adjustments reach the immutable display list without flattening.

BF-1054D consumes that established contract and does not modify the graphics interpreter.

## Editability Classification

Composite `TJ` runs with complete Unicode provenance are classified as:

`EditableWithPositionPreservation`

This classification is distinct from `DirectlyEditable`.

The distinction prevents BF-1054C's simple one-string replacement path from accidentally handling positioned arrays.

Existing enum numeric values are preserved; the new classification is appended as value 8.

## Segment-Preserving Replacement

`PdfPositionedTextReplacementService.Replace` requires exactly one replacement Unicode string for each original `PdfTextRunElementKind.Text` element.

The service:

1. validates session, page, source, stream, operation, and run provenance;
2. requires an unshared page-owned indirect content stream;
3. requires a `TJ` source operation;
4. encodes each replacement segment through the existing same-font encoder;
5. verifies the current source string tokens still match the discovered encoded segments;
6. rewrites only those string tokens;
7. preserves all numeric adjustments and surrounding array/operator bytes;
8. records the resulting stream mutation through the existing BF-1054C Editing operation.

The service never redistributes one replacement string across multiple positioned text elements.

## Source Syntax Preservation

`PdfTextArrayTokenEditor` works only inside the exact provenance-identified source-operation span.

Literal-string tokens remain literal strings.

Hexadecimal-string tokens remain hexadecimal strings.

Bytes between text tokens—including numeric `TJ` adjustments, whitespace, comments, delimiters, and operator syntax—are copied unchanged.

If the number or bytes of source text tokens no longer match discovery, the edit fails as stale provenance.

## Transaction and Writing Integration

BF-1054D reuses the BF-1054C `PdfExistingTextEditOperation`, transaction recording, undo/redo, full-rewrite, and incremental Writing paths.

No new save domain is introduced.

Changed indirect content-stream objects continue to be the only incremental objects identified by an existing-text-only transaction.

## Conservative Boundaries

BF-1054D intentionally does not:

- infer new numeric positioning adjustments;
- merge or split source text elements;
- reflow text;
- rebalance spacing because replacement glyph widths changed;
- mutate filtered streams;
- mutate direct `/Contents` streams;
- mutate shared/Form XObject text;
- change font resources;
- add composite-font encoding support.

Preserving numeric adjustments does not imply that arbitrary replacement text will have identical visual width. Width-aware repositioning belongs to later typography/layout work.

## Acceptance Criteria

1. Composite `TJ` discovery receives a distinct position-preserving editability classification.
2. Existing BF-1054C direct editability behavior is unchanged.
3. Replacement requires one replacement segment per original text element.
4. Numeric `TJ` adjustments remain byte-for-byte unchanged.
5. Literal and hexadecimal text token classes are preserved.
6. Source bytes outside the replaced string tokens remain unchanged.
7. Each replacement segment must encode through the existing font mapping.
8. Segment-count mismatch fails without mutation.
9. Stale source bytes fail without fuzzy matching.
10. Positioned edits participate in transaction rollback, undo, and redo.
11. Existing BF-1054C Writing integration serializes positioned edits without a new save path.
12. Rendering interpreter code remains unchanged.
13. No font/resource object is changed or allocated.
14. No general reflow or width compensation is introduced.
15. The full PiratePDF release gate passes.

## Deferred Work

Find/replace across provenance fragments, typography-state editing, font changes, width-aware local layout adjustment, filtered-stream rewriting, composite-font encoding, shared Form handling, SDK exposure, and general reflow remain later BF-1054 work.

## Preservation Review

BF-1054D preserves positioning semantics by retaining the original numeric `TJ` operands exactly.

It does not claim visual-width equivalence for replacement strings whose glyph metrics differ.

No external dependency is added.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-27 | Added explicit positioned-TJ editability and segment-preserving composite TJ replacement. |
