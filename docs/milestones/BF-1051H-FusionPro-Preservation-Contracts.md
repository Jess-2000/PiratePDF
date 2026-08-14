# BF-1051H — FusionPro Preservation Contracts

| Field | Value |
|---|---|
| **Document ID** | BF-1051H |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Formalize the preservation guarantees that BF-1051 editing sessions already follow and make the targeted FusionPro preservation requirement explicit in the editing API.

BF-1051H does not add a second document model or a serialization policy. It gives later editing, SDK, and writing work a concrete contract describing what the editing layer promises not to disturb.

## Preservation Contract

Every `PdfEditingSession` exposes `PreservationContract`.

The default contract states that editing:

- preserves logical structure outside the requested mutation scope;
- preserves existing object identity wherever the requested edit does not require replacement;
- avoids implicit normalization and rebuilding of untargeted structures;
- reuses accepted logical objects during rollback, undo, and redo rather than cloning them; and
- supports the targeted preservation behavior required for FusionPro-oriented workflows.

The editing contract explicitly does **not** promise byte-for-byte output equivalence. Final object numbering, stream encoding, cross-reference layout, incremental-update strategy, and serialized byte preservation belong to the writing/output layer.

## FusionPro Boundary

FusionPro compatibility is preservation-oriented rather than a claim that the editor reproduces FusionPro output byte-for-byte.

For BF-1051, the requirement is:

1. do not rewrite unrelated logical structures merely because editing begins;
2. do not replace existing objects when a targeted mutation can preserve them;
3. keep untargeted page and indirect-object identity stable;
4. restore the same accepted high-level objects through rollback and history replay where the mutation contracts allow; and
5. defer serialization-specific compatibility checks to the writing/output validation gates.

This boundary prevents the editing layer from taking ownership of writing decisions while still making FusionPro preservation a testable architectural requirement.

## Verification

Preservation tests use real document and editing objects and verify that:

- the session exposes the explicit FusionPro-targeted preservation contract;
- a targeted page edit preserves catalog, trailer, object-store, unrelated indirect-object, and untargeted page identity;
- a content edit preserves the identity and state of an untargeted page;
- resource history restoration retains the prior logical resource entry; and
- structural undo restores the same editable page handle.

These tests are behavioral guards against future normalization or cloning regressions.

## Acceptance Criteria

1. Editing sessions expose an explicit preservation contract.
2. The contract requires preservation of untargeted logical structure.
3. The contract requires preservation of existing object identity where replacement is unnecessary.
4. The contract prohibits implicit normalization of untargeted structures.
5. The contract requires history replay to preserve accepted logical-object identity.
6. Targeted FusionPro preservation is explicitly represented by the contract.
7. Byte-level equivalence is explicitly outside the editing-layer guarantee.
8. Targeted page edits preserve unrelated document-level object identity.
9. Targeted content edits preserve untargeted page identity and state.
10. Structural undo restores the same editable page handle.
11. No serialization or writer behavior is introduced by BF-1051H.
12. Full PiratePDF release gate passes after local review.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Formalized editing preservation guarantees and the targeted FusionPro preservation boundary. |
