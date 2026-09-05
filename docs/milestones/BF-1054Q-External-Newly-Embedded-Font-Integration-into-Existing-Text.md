# BF-1054Q — External/Newly Embedded Font Integration into Existing Text

| Field | Value |
|---|---|
| **Document ID** | BF-1054Q |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053, BF-1054 |
| **Last Updated** | 2026-09-05 |
| **Next Review** | 2026-12-05 |

## Objective

Connect the external-font architecture built in BF-1054H–M to actual existing-text editing.

Q makes `EditableWithResourceChange` a real supported editing strategy by allowing eligible page-owned existing text to change to a newly embedded external TrueType font.

## Integration Flow

```text
existing text
    ↓
external font match
    ↓
mandatory sanitizer
    ↓
BF-1054L/M Unicode TrueType subset
    ↓
Type0 / Identity-H / CIDFontType2
    ↓
BF-1054N collision-safe page resource mutation
    ↓
encode existing Unicode as subset CIDs
    ↓
BF-1054P decoded-space stream mutation
    ↓
scoped Tf switch + restoration
    ↓
atomic Editing operation
```

## External Font Input

Q accepts a `PdfExternalFontMatch`.

The provider's raw bytes are available only through `PdfExternalFontMatch.SanitizeFontProgram`.

No Q API accepts raw font bytes or a raw provider stream.

The supplied sanitizer therefore remains the mandatory BF-1054I trust boundary.

## Supported Font Technology

Q initially supports external TrueType outlines through M's Type0/CIDFontType2 path.

CFF/CFF2 remains deferred.

Provider and sanitized-program embedding rights continue to be enforced by the existing font pipeline.

## Existing-Text Scope

Q supports unshared page-owned text classified as:

- `DirectlyEditable`;
- `EditableWithPositionPreservation`.

Form XObject font-resource integration remains a later extension over O's explicit shared/placement semantics.

## Unicode Encoding

The new Type0 subset assigns PDF CIDs independently from original TrueType GIDs.

Q replaces existing source text bytes with `PdfUnicodeTrueTypeSubsetResult.EncodeText` output.

For `TJ`, numeric positioning adjustments and text-element boundaries are preserved.

The existing font is restored immediately after the target through the established scoped `Tf` token editor.

## Shaping Boundary

M provides scalar-to-glyph mapping, not a text shaping engine.

Q therefore fails closed for text that conservatively requires sequence-aware or contextual shaping, including:

- combining-mark sequences;
- join-control sequences;
- variation-selector sequences;
- Arabic-family contextual scripts;
- Indic and other complex scripts covered by the shaping guard.

This prevents PiratePDF from emitting text that is technically encoded but visually incorrect.

A dedicated shaping subsystem remains outside Q.

## Filtered Streams

Q operates on decoded content bytes through BF-1054P.

The final stream is re-encoded using P's preservation-safe stream representation policy.

## Atomic Editing

A font-resource change modifies both:

- the existing content stream;
- the PDF object graph containing the page resource projection and newly allocated Type0 support objects.

`PdfEditableDocument.ApplyExistingTextResourceChange` applies those changes as one Editing operation.

If application fails, the previous stream and object graph are restored.

When executed inside an edit transaction, undo/redo restores both parts together.

## Resource-Change Operation

`PdfExistingTextResourceChangeOperation` explicitly reports:

```text
Editability = EditableWithResourceChange
```

This activates the BF-1054A enum value as a concrete supported operation rather than a dormant architectural placeholder.

## Resource Naming

Q supplies a preferred resource name but trusts N to select the final collision-safe name.

The returned operation records the actual assigned resource name.

## Security

Q preserves the approved project-wide rules:

- external font bytes must pass the mandatory sanitizer;
- no font bytecode is executed;
- no plaintext temporary font or document files are introduced;
- protected data in transit requires TLS 1.2 or higher;
- protected PiratePDF-managed persisted data requires AES-256 authenticated encryption.

## Acceptance Criteria

1. Q accepts external font matches rather than raw font bytes.
2. Raw provider data passes through the sanitizer.
3. TrueType external fonts use M's Type0/CID path.
4. Newly allocated font resources use N.
5. Collision-safe resource names are honored.
6. Existing text is encoded with the new subset's CIDs.
7. Direct `Tj` text can be re-encoded.
8. Positioned `TJ` text preserves numeric adjustments.
9. Existing font state is restored after the target.
10. Filtered supported streams use P decoded-space mutation.
11. Unsupported filter semantics remain rejected by P.
12. Content and resource changes are one logical Editing operation.
13. Partial object-graph application is rolled back on failure.
14. Transaction undo/redo restores content and resource objects together.
15. `EditableWithResourceChange` is represented by a concrete operation.
16. Complex-shaping-dependent text fails closed.
17. Page-owned unshared text is supported.
18. Form resource changes are not silently introduced.
19. No new persistence/network path is introduced.
20. The full PiratePDF release gate passes.

## Deferred Work

- Windows/macOS external-font provider adapters;
- Form-specific external-font resource integration;
- dedicated complex-script shaping;
- superscript/subscript, color, compound operators, and decorations;
- whole-document orchestration and width-aware local adjustment;
- public SDK exposure;
- final BF-1054 hardening.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-05 | Connected sanitized external TrueType Type0 embedding to existing-text mutation with CID re-encoding, N resource allocation, P stream handling, shaping fail-closed behavior, and atomic Editing undo/redo. |
