# BF-1052C — Document Information and Metadata Editing

| Field | Value |
|---|---|
| **Document ID** | BF-1052C |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1052 |
| **Last Updated** | 2026-08-14 |
| **Next Review** | 2026-11-14 |

## Purpose

BF-1052C establishes preservation-aware logical editing of the standard PDF document-information metadata entries on top of the BF-1051 transaction/history model and BF-1052A structural-state contracts.

## Scope

The slice adds authoritative logical state for Title, Author, Subject, Keywords, Creator, Producer, CreationDate, and ModificationDate. It adds controlled set/clear operations, exact absent-versus-explicit state, validation before mutation, rollback, undo/redo, and preservation tests.

XMP packet parsing and serialization are intentionally not introduced here. Arbitrary XML mutation, metadata-stream encoding, object allocation, xref selection, incremental-update policy, and byte-level PDF string encoding remain parser/Writing concerns rather than Editing concerns.

## Authoritative state

`PdfDocument.Information` owns the logical document-information values. Editing does not keep a parallel metadata dictionary. `PdfEditableDocument.Information` exposes that read-only logical model while mutation remains controlled through editing operations.

An absent entry and an explicitly present empty string are different logical states. This distinction is retained through `PdfStructuralValueState<string>` so rollback, undo, and redo restore the exact targeted state.

CreationDate and ModificationDate are retained as logical PDF date text in this slice. BF-1052C does not normalize, infer, or rewrite date syntax as a side effect of unrelated edits.

## Mutation contract

`PdfDocumentInformationEditOperation` is an `IPdfStructuralEditOperation` with document scope and `PdfStructureKind.Metadata`. Each operation identifies the standard information field, mutation kind, prior state, and resulting state.

Set operations accept explicit text, including the empty string. Clear operations remove an existing entry and fail when the requested field is already absent rather than recording a no-op history entry.

Every controlled mutation is validated by the owning `PdfEditingSession` before state changes and is recorded through the existing transaction/history machinery. No metadata-specific transaction or history subsystem is introduced.

## Preservation

Changing one document-information field must not rewrite another field. Existing trailer `/Info` object identity is not replaced merely because a logical information value changes. Allocation or replacement of indirect objects is deferred to preservation-aware Writing.

The existing targeted FusionPro preservation contract remains authoritative. BF-1052C performs no automatic metadata cleanup, normalization, producer rewriting, timestamp insertion, or other unrelated mutation.

## Release-gate usability correction

The release-gate repair-preview failure message now prints the complete runnable DocGen command for applying proposed documentation repairs. This changes guidance only; the gate continues to own restore, build, tests, DocGen validation/health/repair preview, whitespace checks, and repository-status reporting.

## Testing strategy

Tests cover document-scope structural classification, absent-to-explicit set state, explicit empty strings, clear-to-absent state, rejection of clearing an absent entry, transaction rollback, committed undo/redo, untargeted-field preservation, and preservation of an existing trailer information-object identifier.

Core state and history tests use real in-memory editing sessions; no substitutable external service boundary requires Moq in this slice.

## Acceptance criteria

1. `PdfDocument` owns a single authoritative logical document-information model.
2. Standard Title, Author, Subject, Keywords, Creator, Producer, CreationDate, and ModificationDate entries can be read through that model.
3. Editing exposes controlled set and clear operations rather than public model mutators.
4. Metadata operations use document scope and `PdfStructureKind.Metadata`.
5. Absent and explicitly empty values remain distinct exact structural states.
6. Validation occurs before mutation and existing transactions/history provide rollback, undo, and redo.
7. Editing one field preserves untargeted information fields and existing trailer `/Info` identity.
8. XMP parsing/serialization and Writing allocation policy are not pulled into Editing.
9. No external package dependency is introduced.
10. The release gate passes with zero documentation validation errors, warnings, and proposed repair changes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-14 | Added controlled document-information metadata editing and exact-state preservation contracts. |
