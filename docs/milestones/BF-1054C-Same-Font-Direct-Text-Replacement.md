# BF-1054C — Same-Font Direct Text Replacement

| Field | Value |
|---|---|
| **Document ID** | BF-1054C |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1047, BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-08-27 |
| **Next Review** | 2026-11-27 |

## Objective

Introduce the first mutation-capable existing-text path while preserving the font/resource context and limiting replacement to provenance that can be rewritten without guessing.

## Supported Direct Replacement

BF-1054C supports replacement and deletion when all of the following are true:

- the target was discovered as `DirectlyEditable`;
- the source is unshared page content;
- the source content stream is an existing indirect object;
- the content stream has no `/Filter`;
- the source operation is `Tj`, `'`, `"`, or a `TJ` operation containing exactly one text element/string token;
- source span, operation index, run ordinal, encoded bytes, page, owner, and session provenance still match;
- replacement Unicode is reversibly encodable through the existing simple font mapping.

No font resource is added or changed.

## Same-Font Encoding

`PdfSameFontTextEncoder` derives a reverse map from the same simple-font mapping sources used by the existing decoder:

1. ToUnicode when available;
2. Encoding Differences;
3. WinAnsi or the currently supported Standard-encoding ASCII fallback.

Ambiguous reverse mappings are rejected rather than selecting an arbitrary character code.

Type 0 composite fonts and Type 3 fonts are deferred.

## Minimal Syntax Mutation

BF-1054C does not reconstruct a page or an entire content operation from the display list.

The authoritative unfiltered stream bytes are read from the identified indirect object.

Within the exact source-operation span, the service locates the single original PDF string token, decodes it, and verifies that its bytes still match the discovered descriptor.

Only that token is rewritten. Bytes before and after the token remain untouched.

The replacement preserves the source string representation class:

- literal strings remain literal strings;
- hexadecimal strings remain hexadecimal strings.

If the current source token differs from the descriptor, replacement fails as stale provenance rather than attempting text/coordinate matching.

## Editing Transaction Integration

The authoritative indirect stream object's data is mutated through `PdfEditableDocument` and recorded as `PdfExistingTextEditOperation`.

The operation participates in transaction rollback, undo, and redo using defensive stream-data snapshots.

The editing identity and indirect content-stream object identifier remain part of the operation contract.

## Writing Integration

`PdfExistingTextTransactionWritingService` validates committed text-only change sets.

For full rewrite, the already-mutated authoritative object store is serialized by the standard full-rewrite plan.

For incremental update, the changed indirect content-stream identifiers are passed to the existing incremental planner so only those objects are appended as changed objects.

`PdfEditingSaveService` recognizes existing-text transactions as a separate save domain.

Mixed page/annotation/text domains remain deferred.

## PDF 1.7 and PDF 2.0

BF-1054C explicitly exercises both PDF 1.7 and PDF 2.0 full-rewrite output.

The document version remains authoritative:

- PDF 1.7 text edits serialize with `%PDF-1.7`;
- PDF 2.0 text edits serialize with `%PDF-2.0`.

Incremental PDF 2.0 coverage also verifies the original source revision prefix remains unchanged while the modified content-stream object is appended.

This establishes version-preserving text-edit behavior without claiming complete ISO 32000-2 feature conformance.

## Conservative Deferrals

BF-1054C intentionally rejects:

- filtered content streams;
- direct `/Contents` streams without an indirect stream identity;
- Form XObject mutation;
- shared-source mutation;
- composite/multi-element `TJ`;
- Type 0 encoding;
- Type 3 text;
- font substitution or new font resources;
- layout reflow;
- fuzzy stale-identity recovery.

These cases require dedicated later contracts rather than normalization inside the first mutation slice.

## Acceptance Criteria

1. Simple same-font replacement mutates only the source string token.
2. Deletion writes an empty string token without removing the text-showing operator.
3. Replacement fails when Unicode cannot be encoded through the existing font mapping.
4. Ambiguous reverse font mappings are rejected.
5. Filtered streams are rejected without mutation.
6. Stale source bytes fail instead of fuzzy matching.
7. Session/page/source ownership and provenance are validated.
8. Existing-text edits participate in transaction rollback, undo, and redo.
9. Full rewrite serializes the mutated authoritative stream.
10. Incremental update appends the changed content-stream object through the existing planner.
11. PDF 1.7 full rewrite retains `%PDF-1.7`.
12. PDF 2.0 full rewrite retains `%PDF-2.0`.
13. PDF 2.0 incremental output preserves the original revision prefix.
14. No font/resource object is changed or allocated.
15. No unrelated PDF object is rewritten by the incremental text edit.
16. The full PiratePDF release gate passes.

## Deferred Work

Composite `TJ` positioning, filtered-stream re-encoding, composite-font encoding, font changes, find/replace, typography-state edits, shared Form handling, public SDK text-editing APIs, and general reflow remain later BF-1054 work.

## Preservation and Dependency Review

No external dependency is added.

Editing remains independent of Rendering.

Services performs the cross-layer orchestration and same-font validation.

No broad Rendering refactor is introduced.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-27 | Added conservative same-font existing-text replacement, Editing transaction integration, Writing integration, and PDF 1.7/2.0 preservation coverage. |
