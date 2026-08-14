# BF-1051D — Transactions and Change Sets

| Field | Value |
|---|---|
| **Document ID** | BF-1051D |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Add transaction lifetime, commit, rollback, and ordered change-set behavior around the BF-1051C mutation vocabulary without creating a second mutation API or weakening the editing-session ownership boundary.

## Contract

`PdfEditingSession.BeginTransaction()` opens one transaction for the session. Mutations continue to be invoked through `PdfEditableDocument`; when a transaction is active, each completed mutation is recorded as a reversible change while retaining the `IPdfEditOperation` produced by BF-1051C.

A transaction may be committed or rolled back exactly once. Committing returns a `PdfEditChangeSet` containing the operations in mutation order. Rolling back reverses recorded changes in LIFO order. Disposing an active transaction rolls it back automatically.

Nested transactions are intentionally rejected. A session may own at most one active transaction.

## Mutation Compatibility

BF-1051D does not duplicate insert, remove, move, replace, or rotation APIs on the transaction type. Existing `PdfEditableDocument` calls remain the sole public page-mutation surface.

Mutations performed when no transaction is active retain BF-1051C immediate behavior. This preserves compatibility while higher-level workflows progressively adopt transactions.

## Change Sets

A committed `PdfEditChangeSet` records:

- the transaction identity,
- operation count,
- whether the transaction was empty, and
- the ordered `IPdfEditOperation` sequence.

The implementation retains internal reversible change information so BF-1051E can build deterministic undo/redo over committed change sets rather than reconstructing inverse behavior from public metadata.

## Rollback Semantics

Rollback restores page-tree order, attachment state, replacement targets, and previous page rotation for changes made through the transaction. Changes are reversed from newest to oldest so dependent structural edits unwind in the inverse order in which they were applied.

Rollback replay bypasses transaction recording while preserving the same page collection synchronization checks used by normal mutation paths.

## Preservation and FusionPro Compatibility

Transactions do not clone or normalize the document. Commit leaves the targeted mutations in place; rollback restores the same high-level page objects and stable editable handles wherever BF-1051C preservation contracts allow.

Untargeted pages remain untouched. This continues the targeted FusionPro preservation requirement while byte-level output equivalence and executable FusionPro interoperability remain writing-system gates.

## Testing Strategy

Tests use real document, page, editing-session, and transaction objects to verify:

- ordered committed change sets,
- structural and rotation rollback,
- automatic rollback on transaction disposal,
- nested-transaction rejection,
- empty commits,
- immediate non-transactional compatibility, and
- replacement rollback with stable original handles.

Moq is not required for the transaction core because BF-1051D is validating concrete state transitions rather than a substitutable external service boundary.

## Acceptance Criteria

1. A session can begin one active edit transaction.
2. Nested transactions are rejected before state changes.
3. Existing editable-document mutation APIs are used unchanged inside transactions.
4. A committed transaction returns an ordered `PdfEditChangeSet`.
5. Rollback reverses recorded changes in reverse order.
6. Disposing an active transaction performs rollback.
7. Empty transactions commit successfully with an empty change set.
8. Mutations outside transactions retain BF-1051C immediate behavior.
9. Untargeted structures and stable editable handles retain preservation-first behavior.
10. Internal reversible change information is retained for BF-1051E undo/redo integration.
11. Full PiratePDF release gate passes after local review.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added single-session transaction lifetime, commit/rollback semantics, ordered change sets, reversible mutation recording, and preservation-first rollback behavior. |
