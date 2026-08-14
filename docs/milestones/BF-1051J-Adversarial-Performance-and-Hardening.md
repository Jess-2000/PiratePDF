# BF-1051J — Adversarial Testing, Performance, and Hardening

| Field | Value |
|---|---|
| **Document ID** | BF-1051J |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Close the BF-1051 editing-model milestone by hardening transaction replay,
validation ordering, session configuration ownership, and large logical edit
sequences without changing the authoritative document model or moving writing
policy into Editing.

## Replay Failure Atomicity

Undo, redo, and transaction rollback replay multiple reversible logical
changes. A later replay action can fail after earlier actions have already
succeeded, particularly when authoritative state has been modified outside the
editing session.

`PdfEditingSession.ReplayChanges` now records each successfully replayed
change. If a later action fails, the session compensates the already-applied
subset in the opposite direction before propagating the original exception.

For undo failure, successfully undone changes are redone in dependency-safe
order. For redo failure, successfully redone changes are undone in reverse
order.

History stacks move only after replay succeeds, so a compensated failure leaves
the same change set available on its original stack. If compensation itself
fails, both failures are surfaced in an `AggregateException`; the engine does
not falsely claim atomic recovery.

## Validator Configuration Ownership

Editing sessions now own a read-only snapshot of configured validators.

Both `PdfEditingSession.Validators` and
`PdfEditingSession.Options.Validators` reference the session-owned read-only
snapshot rather than a caller-owned mutable list or a publicly castable
validator array.

This closes the configuration aliasing gap identified during BF-1051G.

## Validation Ordering

Built-in structural invariants are checked before custom validators for page
insert, remove, move, and replacement operations. Unsupported page-rotation
enum values are also rejected before custom validators run.

Custom validators therefore receive operations that have passed the
editing-model's immediate structural preconditions. The mutation still occurs
only after custom validation succeeds.

The collection mutation methods retain their own invariant checks so replay
and internal mutation paths remain defensive.

## Adversarial Coverage

Hardening tests cover:

- caller mutation of the original validator collection;
- prevention of validator-array mutation through public session properties;
- invalid structural edits not reaching custom validators;
- invalid rotation values not reaching custom validators;
- partial undo failure with successful compensation and unchanged history;
- partial redo failure with successful compensation and unchanged history; and
- a 1,000-operation transaction followed by undo and redo.

The large-sequence test is deterministic rather than wall-clock based. It
guards algorithmic and state-integrity behavior without introducing
environment-dependent timing thresholds into the release gate.

## Preservation and FusionPro

The hardening changes do not normalize or rebuild untargeted PDF structures.
Replay compensation uses the same reversible logical changes and accepted
object identities already captured by BF-1051D through BF-1051H.

Byte-level serialization equivalence remains a Writing/output concern.

## Acceptance Criteria

1. Failed multi-change replay compensates the successfully applied subset.
2. History stacks remain unchanged when replay fails and compensation succeeds.
3. Compensation failure is surfaced rather than hidden.
4. Session validator configuration cannot be changed by mutating the caller's
   original list or casting the public validator collection to its backing
   array.
5. Built-in page structural invariants run before custom validators.
6. Invalid page-rotation enum values run no custom validators and mutate no
   state.
7. A deterministic 1,000-operation transaction commits, undoes, and redoes
   while preserving change-set identity and final state.
8. BF-1051H preservation guarantees remain intact.
9. BF-1051I SDK/service boundaries remain intact.
10. Full PiratePDF release gate passes after local review.

## Provenance

Implementation is independently written from PiratePDF contracts,
specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added BF-1051 adversarial, replay-atomicity, configuration, validation-order, and stress hardening. |
