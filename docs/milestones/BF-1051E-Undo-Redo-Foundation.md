# BF-1051E — Undo/Redo Foundation

| Field | Value |
|---|---|
| **Document ID** | BF-1051E |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051 |
| **Last Updated** | 2026-08-14 |
| **Next Review** | 2026-11-14 |

## Objective

Build deterministic session-owned undo and redo behavior over the reversible committed change sets introduced by BF-1051D without creating a second mutation model or reconstructing inverse operations from public metadata.

## Contract

Each `PdfEditingSession` owns one `PdfEditHistory`. A non-empty committed transaction enters that history as one atomic undo unit. `Undo()` replays the committed change set in reverse order and `Redo()` replays it in original order.

Undo and redo return the `PdfEditChangeSet` that was replayed so callers retain the original transaction identity and ordered operation metadata.

## History Semantics

The history maintains separate undo and redo stacks.

- A successful non-empty transaction commit is pushed onto the undo stack.
- Undo moves the most recent committed change set from undo to redo.
- Redo moves the most recent undone change set from redo back to undo.
- A newly committed non-empty transaction clears redo history.
- Empty commits and rolled-back transactions do not create history entries.
- Mutations performed outside a transaction retain BF-1051C/BF-1051D immediate behavior and do not implicitly enter history.

Undo and redo are rejected while a transaction is active. This keeps transaction lifetime and history replay as distinct session states and prevents history replay from being accidentally recorded into an open transaction.

## Replay and Atomicity Foundation

BF-1051E reuses the internal reversible `PdfEditChange` instances retained by BF-1051D. It does not infer inverses from `IPdfEditOperation` metadata.

Undo replays changes newest-to-oldest. Redo replays changes oldest-to-newest. Replay uses the same page collection and model mutation paths retained by the transaction implementation while the session suppresses change recording during replay.

History stacks are moved only after replay completes successfully. If replay throws, the failing change set remains on its original stack rather than being reported as successfully transferred.

## Preservation and FusionPro Compatibility

Undo and redo preserve the same high-level page instances and editable handles used by the committed transaction wherever the existing editing contracts allow. History replay does not clone, normalize, or rebuild untargeted page structures.

This continues the targeted FusionPro preservation requirement. Byte-level serialization preservation and executable FusionPro interoperability remain writing-system validation responsibilities.

## Testing Strategy

Tests use real document, page, session, transaction, and history objects. They verify:

- committed transactions become undoable,
- redo reapplies undone transactions,
- structural page identity survives undo/redo,
- new committed edits clear redo history,
- rolled-back and empty transactions do not enter history,
- history replay is rejected during active transactions,
- unavailable undo/redo operations are rejected, and
- operation ordering and transaction identity remain stable through replay.

Moq is not used because BF-1051E exercises deterministic in-memory editing state rather than an external or substitutable service boundary.

## Acceptance Criteria

1. Every editing session exposes session-owned undo/redo history.
2. Non-empty committed transactions enter history atomically.
3. Undo replays a committed change set in reverse mutation order.
4. Redo replays an undone change set in original mutation order.
5. Undo and redo preserve the original committed change-set identity and operation ordering.
6. New non-empty commits clear redo history.
7. Empty commits and rolled-back transactions do not enter history.
8. Undo and redo are rejected while a transaction is active.
9. Failed replay does not transfer the change set between history stacks.
10. Existing non-transactional mutation behavior remains unchanged.
11. Stable page handles and untargeted structures retain preservation-first behavior.
12. Full PiratePDF release gate passes after local review.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-14 | Added session-owned undo/redo history over committed reversible transaction change sets. |
