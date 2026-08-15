# BF-1052E — AcroForm and Form-Field Structural Editing

| Field | Value |
|---|---|
| **Document ID** | BF-1052E |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1052 |
| **Last Updated** | 2026-08-14 |
| **Next Review** | 2026-11-14 |

## Purpose

BF-1052E establishes authoritative logical AcroForm ownership and controlled form-field structural editing on top of the BF-1051 transaction/history model, BF-1052 structural-editing contracts, and BF-1052D annotation model.

## Scope

The slice adds one authoritative `PdfAcroForm` per document, stable form-field identity, the four core PDF field types, common field options, ordered root and child-field collections, partial names, values, default values, Widget annotation relationships, controlled root/child add/remove operations, common-state updates, validator-first mutation, rollback, undo/redo, and preservation-oriented tests.

Subtype-specific button state, choice option arrays, signature dictionaries, calculation order, actions and JavaScript, appearance-stream regeneration, dictionary serialization, indirect-object allocation, xref behavior, and byte writing remain outside this slice.

## Authoritative state and ownership

`PdfDocument` owns one `PdfAcroForm`. `PdfAcroForm` owns the ordered root-field collection. `PdfFormField` owns stable identity, common field state, ordered child fields, and its Widget relationships.

Editing exposes `PdfEditableFormField` handles. A session-created field remains the same logical object as it moves between detached, root, and child states. Removing a field does not synthesize a replacement, which lets rollback and history replay preserve field identity.

## Field hierarchy

Root fields and child fields are distinct structural states. A field cannot simultaneously be a root and a child, and the authoritative model rejects hierarchy cycles. Child order is preserved by add/remove rollback and replay.

The slice models partial field names rather than flattening qualified names into a second source of truth. Qualified-name policy can be layered over the hierarchy later without losing source structure.

## Widget relationships

BF-1052D introduced Widget as an annotation subtype. BF-1052E establishes the form-field relationship: only an attached Widget annotation can be associated with a field, and one Widget cannot be associated with multiple fields.

A linked Widget cannot be removed from its page until the relationship is detached. This prevents a half-linked logical state. Detaching a Widget from a field does not remove the annotation from the page or change either object's identity.

Appearance streams are not regenerated when field values or Widget relationships change. Writing later decides how logical field/Widget relationships map to `/AcroForm`, `/Fields`, `/Kids`, `/Parent`, annotation dictionaries, indirect objects, and output bytes.

## Mutation contract

`PdfFormFieldEditOperation` is an `IPdfStructuralEditOperation` with document scope and `PdfStructureKind.Form`. Root presence transitions use absent/explicit structural state. Child hierarchy operations and common-state changes carry before/after `PdfFormFieldSnapshot` state.

Explicit empty strings remain distinct from absent partial names, values, and default values. Validators run before authoritative state changes. Existing editing transactions/history provide rollback, undo, and redo; there is no form-specific history subsystem.

## Preservation

Editing one field property preserves untargeted field state, hierarchy, Widget identity, page ownership, and unrelated annotations. The engine does not auto-generate appearances, normalize field names, rewrite unrelated fields, synthesize calculation order, or serialize PDF objects.

The targeted FusionPro preservation contract remains authoritative. Editing decides the logical result; Writing later decides preservation-aware serialization.

## Testing strategy

Tests cover AcroForm initialization, hierarchy cycle protection, field identity, root/child structural operations, absent-versus-empty values, untargeted-state preservation, Widget association and detach behavior, linked-Widget removal protection, rollback, committed undo/redo, validator-before-mutation behavior, and cross-session ownership rejection.

## Acceptance criteria

1. Every logical document owns one authoritative AcroForm root.
2. Form fields have stable identity independent of root/child/detached state.
3. Root and child field collections preserve order and reject invalid hierarchy states.
4. Text, button, choice, and signature field types are represented without premature subtype-specific payload modeling.
5. Partial name, value, default value, and common field options are controlled mutations.
6. Explicit empty strings remain distinct from absent values.
7. Widget relationships reuse BF-1052D annotation identity and require attached Widget annotations.
8. Linked Widgets cannot be removed from pages without first detaching their field relationship.
9. Form edits use `PdfStructureKind.Form`, validator-first mutation, and existing rollback/undo/redo machinery.
10. Appearance regeneration, actions, subtype-specific payloads, and Writing remain deferred.
11. No external package dependency is introduced.
12. The release gate passes with zero compiler/analyzer/test/documentation failures.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-14 | Added authoritative AcroForm ownership, field hierarchy, common field mutations, Widget relationships, and preservation/history coverage. |
