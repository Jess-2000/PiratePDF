# BF-1052D — Annotation Model and Annotation Mutations

| Field | Value |
|---|---|
| **Document ID** | BF-1052D |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1052 |
| **Last Updated** | 2026-08-14 |
| **Next Review** | 2026-11-14 |

## Purpose

BF-1052D establishes the authoritative logical annotation model and controlled annotation mutations on top of the BF-1051 transaction/history model and BF-1052 structural-editing contracts.

## Scope

The slice adds stable annotation identity, standard subtype classification, page ownership, ordered page annotation collections, common rectangle/content/flag state, detached annotation creation, add/remove operations, common-state updates, validator-first mutation, rollback, undo/redo, and preservation-oriented tests.

Subtype-specific payloads, appearance-stream regeneration, action/destination editing, popup relationship policy, AcroForm field ownership, dictionary serialization, indirect-object allocation, xref behavior, and byte writing remain outside this slice. Widget annotations are represented as annotations here; BF-1052E establishes their form-field relationship.

## Authoritative state and ownership

`PdfPage` owns the authoritative ordered annotation collection. `PdfAnnotation` owns stable identity and common logical state. Editing exposes `PdfEditableAnnotation` handles rather than a parallel annotation model.

A session may create a detached annotation and later attach it to a page. Removing an annotation detaches it without destroying the session handle or changing its identity. This permits transaction rollback and history replay to restore the same logical object rather than synthesizing replacements.

## Mutation contract

`PdfAnnotationEditOperation` is an `IPdfStructuralEditOperation` with page scope and `PdfStructureKind.Annotation`. Add, remove, rectangle, contents, and flags edits carry exact before/after `PdfAnnotationSnapshot` states. Presence transitions use `PdfStructuralValueState<PdfAnnotationSnapshot>` so add is absent-to-explicit and remove is explicit-to-absent.

Explicit empty contents and absent contents remain distinct. Validators run before authoritative state changes. Annotation changes use the existing editing transaction/history machinery; no annotation-specific history subsystem is introduced.

## Preservation

Changing one common annotation property preserves untargeted common properties and annotation identity. Removing and reinserting an annotation preserves identity and page order. Editing does not regenerate appearance streams, normalize subtype-specific dictionaries, change unrelated page state, or serialize PDF objects.

The targeted FusionPro preservation contract remains authoritative. Writing later decides how logical annotation changes map to source dictionaries, indirect objects, appearance streams, xref entries, and output bytes.

## Testing strategy

Tests cover annotation identity and page ownership, rectangle validation, structural operation classification, add/remove presence states, detached-handle preservation, untargeted common-state preservation, empty-versus-absent contents, transaction rollback with original ordering, committed undo/redo, validator-before-mutation behavior, and cross-session ownership rejection.

## Acceptance criteria

1. Pages own one authoritative ordered annotation collection.
2. Annotations have stable identity independent of attachment state.
3. Editing exposes stable annotation handles and controlled creation/add/remove/update APIs.
4. Annotation operations use page scope and `PdfStructureKind.Annotation`.
5. Add/remove operations preserve exact presence state and identity.
6. Common rectangle, contents, and flags edits preserve untargeted annotation state.
7. Explicit empty contents remain distinct from absent contents.
8. Validation occurs before mutation and existing transactions/history provide rollback, undo, and redo.
9. Appearance regeneration, subtype-specific structures, forms, and Writing remain deferred to their owning milestones.
10. No external package dependency is introduced.
11. The release gate passes with zero compiler/analyzer/test/documentation failures.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-14 | Added authoritative annotation state, stable editing handles, controlled mutations, and history/preservation coverage. |
