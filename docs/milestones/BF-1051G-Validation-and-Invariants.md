# BF-1051G — Validation and Invariants

| Field | Value |
|---|---|
| **Document ID** | BF-1051G |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Activate the validation boundary established by BF-1051A so controlled page, content, and resource edits can be approved or rejected before they mutate authoritative document state.

## Validation Contract

`PdfEditingSessionOptions.Validators` configures the validators for a session. The session snapshots that sequence when it is created so later changes to a caller-owned list do not silently change editing policy.

For every controlled mutation:

1. ownership and required arguments are checked,
2. operation metadata is created from the current pre-mutation state,
3. configured validators run in registration order,
4. the authoritative model is mutated only after all validators return successfully, and
5. transaction recording occurs only after successful mutation.

A validator rejects an operation by throwing, normally with `PdfEditingValidationException`. A rejected operation therefore does not mutate page, content, or resource state and does not enter the active transaction.

## Invariants

BF-1051G reinforces the following editing invariants:

- validators cannot bypass session ownership;
- validators observe operation metadata before the target state changes;
- rejected edits leave authoritative state unchanged;
- rejected edits do not create undo/redo history through transaction recording;
- validator ordering is deterministic;
- a session owns a stable validator snapshot for its lifetime; and
- existing page-tree synchronization and mutation preconditions remain authoritative structural guards.

Validation is not replayed during rollback, undo, or redo. Those paths restore or reapply a change that was already accepted when originally executed. Revalidating history against a later policy state could prevent deterministic restoration.

## Preservation and FusionPro Compatibility

Validation adds no normalization, cloning, or serialization behavior. It is a pre-mutation policy boundary around the same targeted mutations introduced by BF-1051C and BF-1051F.

This preserves the project rule that untargeted structures and object identity remain unchanged wherever the requested edit does not require replacement, including targeted FusionPro compatibility.

## Testing Strategy

Tests use concrete documents, sessions, transactions, and editable state. A lightweight recording validator is used as the substitutable validation boundary.

Coverage verifies:

- page rejection before structural mutation,
- content rejection before byte replacement and transaction recording,
- resource rejection before dictionary mutation,
- deterministic validator ordering,
- session snapshotting of configured validators, and
- rejection of invalid null validator entries.

No mocking package is added because the validator contract can be exercised directly without introducing additional test dependencies.

## Acceptance Criteria

1. Session validators are configurable through `PdfEditingSessionOptions`.
2. Validator configuration is snapshotted when the session opens.
3. Every controlled page mutation is validated before mutation.
4. Every controlled content mutation is validated before mutation.
5. Every controlled resource mutation is validated before mutation.
6. Rejected operations leave authoritative state unchanged.
7. Rejected operations are not recorded in active transactions.
8. Validators execute in deterministic registration order.
9. Null validator collections or entries are rejected.
10. Undo, redo, and rollback remain deterministic and do not revalidate accepted history.
11. Existing preservation-first and FusionPro compatibility requirements remain intact.
12. Full PiratePDF release gate passes after local review.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Activated pre-mutation validation for controlled page, content, and resource editing operations. |
