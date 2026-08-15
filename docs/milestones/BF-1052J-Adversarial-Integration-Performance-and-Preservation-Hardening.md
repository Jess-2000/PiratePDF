# BF-1052J — Adversarial, Integration, Performance, and Preservation Hardening

| Field | Value |
|---|---|
| **Document ID** | BF-1052J |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1052 |
| **Last Updated** | 2026-08-15 |
| **Next Review** | 2026-11-15 |

## Purpose

BF-1052J closes the editable-document-primitives milestone by hardening the structural editing model introduced across BF-1052A through BF-1052I. The slice concentrates on adversarial state transitions, mixed-structure transaction behavior, stable identity, SDK integration boundaries, bounded scale, and preservation-oriented regression coverage.

## Hardening scope

The final slice exercises structural editing as one integrated system rather than as isolated feature areas. Coverage combines page order and geometry, document information, annotations, AcroForm fields and Widget relationships, outlines, page decorations, transactions, history, consistency validation, Services/SDK boundaries, and stable detached identities.

The tests deliberately reuse the authoritative editing model. No parallel mutable representation, serialization layer, appearance-generation system, or application UI is introduced.

## Adversarial structural transitions

Hardening verifies that mixed transactions can be rolled back without changing stable page or structural identities, that committed mixed transactions round-trip through undo and redo, and that cross-structure dependencies reject destructive edits until their references are explicitly detached or cleared.

Widget annotations remain protected while associated with AcroForm fields. Direct outline destinations continue to protect referenced pages from removal. Detached handles remain session-owned and reusable through history transitions.

## SDK integration

SDK hardening verifies the BF-1052I detached-page identity contract in both directions: a page created detached can be inserted, and an attached page removed through the SDK can later be reinserted by the same stable SDK identity.

Structural metadata and page geometry are exercised together inside an SDK transaction and verified across commit, undo, redo, structural summary projection, and consistency validation. Unknown page identities are rejected without mutating the document.

## Scale and bounded behavior

A mixed structural transaction creates hundreds of form fields, outline items, and reusable page decorations, commits them atomically, and then exercises full undo and redo while validating collection counts and cross-structure consistency. This is a deterministic bounded regression test rather than a wall-clock benchmark, so normal CI is not coupled to machine-specific timing.

Existing BF-1052 and earlier editing hardening tests continue to cover large transaction replay, failed replay compensation, validator ordering, preservation contracts, ownership, transaction rules, and collection synchronization.

## Preservation boundary

BF-1052J does not add Writing behavior. Preservation hardening remains focused on logical editing state, stable identity, untargeted-structure guarantees, rollback, undo/redo, and cross-structure consistency. Serialization, appearance regeneration, parser hydration for structures not yet projected into the high-level model, and byte-level output preservation remain later Writing/parser integration work.

## Acceptance criteria

1. Mixed structural edits roll back atomically without replacing stable identities.
2. Mixed committed structural edits remain consistent through undo and redo.
3. Widget-field and outline-page dependencies reject operations that would create dangling references.
4. Detached structural/page identities remain reusable within their owning session where the editing contract permits it.
5. SDK structural state remains coherent across transaction commit, undo, redo, summary projection, and consistency validation.
6. Invalid SDK page identities fail without mutating document state.
7. A deterministic bounded large mixed-structure transaction completes commit, undo, redo, and consistency validation.
8. No Writing, appearance generation, parser hydration, external dependency, or CI prerequisite is added.
9. Existing preservation and SDK isolation contracts remain intact.
10. Full PiratePDF release gate passes after local review.

## Deferred work

BF-1052J does not implement arbitrary existing-text editing, typography/font replacement, arbitrary graphics-object editing, image replacement/import/export, form-data merge, appearance-stream generation, or preservation-aware serialization. Those capabilities remain in later milestones that build on the BF-1052 editing foundation.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-15 | Added final BF-1052 adversarial, integration, scale, and preservation hardening coverage. |
