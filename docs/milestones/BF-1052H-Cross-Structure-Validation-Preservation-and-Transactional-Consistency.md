# BF-1052H — Cross-Structure Validation, Preservation, and Transactional Consistency

| Field | Value |
|---|---|
| **Document ID** | BF-1052H |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1052 |
| **Last Updated** | 2026-08-15 |
| **Next Review** | 2026-11-15 |

## Purpose

BF-1052H hardens the structural-editing work from BF-1052A through BF-1052G by validating relationships that cross individual structure boundaries and by exercising multi-structure rollback, undo, redo, identity, and preservation behavior.

## Scope

The slice adds an explicit editing-session consistency check for the authoritative logical document state. It validates page/annotation ownership, AcroForm field hierarchy and Widget associations, outline hierarchy, mutually exclusive outline destination/action state, and direct outline destinations to attached pages. It also adds adversarial integration tests spanning metadata, page geometry, annotations, forms, outlines, and page decorations.

The validator does not parse or serialize PDF dictionaries, regenerate form appearances, generate page-decoration content, or replace the per-operation validator pipeline. It verifies logical postconditions of the editing model already established by earlier BF-1052 slices.

## Cross-structure invariants

Attached annotations must belong to exactly one attached page. Form-field Widget associations must reference Widget annotations attached to pages in the same document, and the annotation and field must agree about ownership. A Widget cannot belong to more than one field. Form-field and outline hierarchies must have coherent parent relationships and unique logical identity. Direct outline destinations must reference pages still attached to the document. An outline item cannot simultaneously own a direct destination and a navigation action.

## Transactional consistency

A transaction may span independent structural categories. Rollback restores the exact prior logical state in reverse mutation order. A committed multi-structure transaction is recorded as one history unit and must undo and redo coherently without replacing stable page, field, annotation, outline, or decoration identities.

## Preservation

Cross-structure hardening does not normalize or materialize untargeted structures. Exact representation state established by earlier slices remains authoritative, including defaulted versus explicit page geometry and absent versus explicit metadata values. The targeted FusionPro preservation contract remains in force.

## Acceptance criteria

1. `PdfEditingSession.ValidateConsistency()` succeeds for valid integrated state and throws `PdfEditingValidationException` when a cross-structure invariant is violated.
2. Page/annotation, field/Widget, and outline/page relationships cannot silently become dangling or contradictory.
3. Multi-structure rollback restores all targeted structures to their pre-transaction state.
4. Multi-structure undo/redo remains atomic at the committed transaction boundary.
5. Stable object identity is retained across rollback and history replay.
6. No new serialization, appearance-generation, SDK, or external dependency surface is introduced.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-15 | Added missing revision history section. |
