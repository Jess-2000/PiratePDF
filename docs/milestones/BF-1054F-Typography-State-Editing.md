# BF-1054F — Typography State Editing

| Field | Value |
|---|---|
| **Document ID** | BF-1054F |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-03 |
| **Next Review** | 2026-12-03 |

## Objective

Add same-font editing for the PDF text-state values already interpreted for existing text:

- font size (`Tf`, retaining the same font resource);
- character spacing (`Tc`);
- word spacing (`Tw`);
- horizontal scaling (`Tz`);
- text rendering mode (`Tr`);
- text rise (`Ts`).

BF-1054F does not change font family/face/resources and does not implement text color.

## Typography Snapshot

`PdfExistingTextDescriptor` now exposes `PdfExistingTextTypography`, populated from the immutable `PdfGraphicsStateSnapshot` associated with the display text run.

This captures the original values required for deterministic local restoration after a scoped typography edit.

The existing `FontSize` descriptor property remains for compatibility.

## Scoped Mutation Strategy

BF-1054F supports `Tj` and `TJ` targets only.

Requested text-state operators are inserted immediately before the source operation.

The original text-showing operation remains unchanged.

Immediately after the source operation, BF-1054F restores the previously interpreted values for every state field it changed.

This prevents a local typography edit from leaking into later text that inherits the surrounding PDF text state.

Font-size changes use the existing font resource name and therefore retain the same resolved font/resource identity.

## Why Quote Operators Are Deferred

The quote (`'`) and double-quote (`"`) text-showing operators combine text display with line movement and, for `"`, word/character-spacing changes.

BF-1054F does not rewrite those compound semantics.

They remain supported for same-font text replacement from BF-1054C but are rejected for scoped typography insertion until a dedicated compound-operator strategy is defined.

## Stale-Provenance Validation

Before inserting any state operator, BF-1054F verifies that the current source string bytes still match the discovered text.

Direct text uses the BF-1054C string-token validation path.

Positioned `TJ` text uses the BF-1054D array-token validation path.

No coordinate/text fuzzy matching is introduced.

## Editing and Writing

Typography edits mutate the same authoritative indirect content-stream object and record the existing `PdfExistingTextEditOperation`.

Therefore BF-1054F automatically participates in:

- transaction rollback;
- undo/redo;
- BF-1053 full rewrite;
- BF-1053 incremental update.

No new Editing operation family or save domain is required.

## Conservative Boundaries

BF-1054F intentionally defers:

- font family/face changes;
- font embedding/substitution;
- resource allocation;
- fill/stroke text color;
- alpha/blend changes;
- underline;
- superscript/subscript convenience semantics beyond direct text rise;
- quote/double-quote compound typography rewriting;
- filtered streams;
- Form/shared-source mutation;
- layout reflow or width compensation.

Color is deferred because arbitrary PDF color restoration may require color-space/resource-aware graphics-state handling rather than only text-state operands.

## Acceptance Criteria

1. Existing-text discovery captures font size, character spacing, word spacing, horizontal scaling, rendering mode, and rise.
2. Existing descriptor constructor compatibility is preserved.
3. Partial typography updates change only requested state fields.
4. Font-size editing retains the existing font resource name.
5. Requested state is inserted immediately before the target `Tj`/`TJ`.
6. Original state is restored immediately after the target.
7. Later text therefore retains its previous text state.
8. Original text-showing operation bytes remain unchanged.
9. Direct and positioned source bytes are validated before mutation.
10. Stale source provenance fails without insertion.
11. Quote and double-quote targets are rejected for typography editing.
12. Typography edits participate in transaction undo/redo.
13. Existing Writing integration serializes typography edits without a new save path.
14. No Rendering interpreter code is changed.
15. No font/resource object is changed or allocated.
16. No external dependency is added.
17. The full PiratePDF release gate passes.

## PDF Version Policy

BF-1054F uses the existing BF-1053/BF-1054C Writing path and does not alter document version selection.

The established PDF 1.7 and PDF 2.0 version-preservation tests remain authoritative for existing-text Writing.

## Deferred Work

Font selection/resource mutation (BF-1054G), decorations/derived styles, color editing, shared Forms, SDK exposure, and end-to-end hardening remain later work.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-03 | Added scoped same-font PDF text-state editing and descriptor typography snapshots. |
