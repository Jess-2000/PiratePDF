# BF-1051A — Editing Architecture, Contracts, and Ownership Boundaries

| Field | Value |
|---|---|
| **Document ID** | BF-1051A |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Establish the architectural boundary for controlled PDF editing without introducing transaction, undo/redo, or serialization behavior before their dedicated BF-1051 slices.

## Contract

- `BlackFlag.Engine.Editing` owns controlled logical mutation of the high-level document model.
- A `PdfDocument` may have at most one active `PdfEditingSession`.
- Editing-session identity is explicit and is propagated through ownership tokens so later operations, transactions, and mutable editing state can reject cross-session use.
- Editing operations identify the logical area they target: document, page, content, or resource state.
- Validation is an explicit boundary and runs before a controlled edit is permitted to mutate state.
- Existing parsed/effective structures remain authoritative source state; BF-1051 does not introduce a competing second PDF document model.
- Writing and byte serialization remain outside `BlackFlag.Engine.Editing`.
- Transaction commit/rollback, change sets, undo/redo, and concrete page/content/resource mutations are intentionally deferred to later BF-1051 slices.

## Preservation and FusionPro Compatibility

The default editing contract is preservation-first. An editing operation may change only the state it owns or explicitly targets. Untargeted document state must remain semantically unchanged.

This rule is a targeted FusionPro compatibility requirement as well as a general PDF-fidelity requirement. BF-1051 editing work must avoid gratuitous normalization, replacement, reordering, or loss of untouched structures and should preserve existing indirect-object identity wherever the requested edit does not require replacement. Byte-level FusionPro interoperability remains a later writing/output validation gate.

## Ownership and Lifetime

- Opening an editing session acquires exclusive editing ownership of the supplied `PdfDocument`.
- Opening a second active editing session for the same document fails deterministically.
- Disposing a session releases its editing ownership.
- Ownership tokens are session-specific and later editing operations must reject tokens from another session.
- Disposed sessions reject ownership-sensitive work.

## Testing Strategy

BF-1051A uses real in-memory `PdfDocument` instances for core ownership and lifecycle behavior. Moq is not required for these tests because no external boundary benefits from substitution in this slice. Moq remains approved for later BF-1051 boundary tests where validators, policies, observers, SDK adapters, or other service dependencies need isolated interaction verification.

## Optimization Review

- Session acquisition and release are constant-time ownership operations.
- Document ownership uses weak-key tracking so the editing subsystem does not create a global strong-reference registry of documents.
- Ownership checks use stable session identifiers and require no document traversal.
- No object graph cloning or normalization occurs merely by opening an editing session.
- BF-1051A adds no reflection, parsing, serialization, or resource resolution work.

## Acceptance Criteria

1. `BlackFlag.Engine.Editing` builds as an active subsystem rather than an assembly-marker placeholder.
2. A document can have only one active editing session at a time.
3. Session disposal releases ownership and disposed sessions reject ownership-sensitive operations.
4. Session ownership tokens distinguish state belonging to different editing sessions.
5. Editing contracts distinguish document, page, content, and resource mutation scopes.
6. Preservation-first behavior and targeted FusionPro compatibility are documented architectural requirements.
7. Writing, transaction execution, undo/redo, and concrete mutations remain outside BF-1051A.
8. Unit tests cover session defaults, lifecycle, exclusivity, and ownership identity.
9. Full PiratePDF release gate passes, including documentation generation and validation.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Established editing-session ownership, preservation-first contracts, mutation scopes, validation boundaries, and targeted FusionPro preservation requirements. |
