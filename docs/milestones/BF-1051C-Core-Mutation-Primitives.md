# BF-1051C — Core Mutation Primitives

| Field | Value |
|---|---|
| **Document ID** | BF-1051C |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Introduce the first controlled logical mutations over the BF-1051 editable document and page model while preserving ownership, stable editable handles, untargeted structures, and the transaction boundary reserved for BF-1051D.

## Contract

BF-1051C introduces controlled page operations for:

- inserting a detached session-owned page,
- removing an attached page,
- moving an attached page,
- replacing an attached page with a detached page, and
- updating page rotation.

Each mutation is performed through `PdfEditableDocument`. The raw mutable `PdfPageTree` and `PdfPage` instances remain unavailable through the public editing facade.

Every completed mutation produces a `PdfPageEditOperation` implementing `IPdfEditOperation`. The operation records its session ownership, page scope, mutation kind, relevant page identities, indexes, and rotation value when applicable. BF-1051D can therefore make the same logical operations transactional without inventing a separate mutation vocabulary.

## Ownership and Structural Integrity

- Every page participating in a mutation must belong to the editing session performing that mutation.
- Foreign-session page handles are rejected before the authoritative page tree changes.
- An attached page cannot be inserted again or used as a replacement while it remains attached.
- Removed pages retain their stable editable handle and may be reinserted by the owning session.
- Source-page `SourceIndex` remains opening-session provenance and is not rewritten after moves, removals, or reinsertion.
- Before structural mutations, the editable page collection verifies that its handles still correspond to the authoritative page tree. Direct page-tree changes performed outside the editing facade are detected rather than silently merged into editing state.
- Replacement locally restores the removed source page if attachment of the replacement fails.

## Preservation and FusionPro Compatibility

Mutation is target-specific. Insert, remove, move, replace, and rotation changes operate only on the requested page state. Untargeted editable-page handles and underlying high-level page identities are retained.

This is an explicit targeted FusionPro preservation requirement. BF-1051C does not rebuild, normalize, clone, or reorder unrelated page structures merely because another page is edited. Byte-level output preservation and executable FusionPro interoperability remain later writing-system gates.

## Transaction Boundary

BF-1051C mutations execute immediately after built-in ownership and structural validation. The returned `PdfPageEditOperation` is the logical mutation description that BF-1051D will use to add commit, rollback, and change-set behavior.

BF-1051C intentionally does not add transaction lifetime, commit/rollback APIs, undo/redo stacks, content-stream mutation, resource mutation, or serialization.

## Testing Strategy

Tests use real `PdfDocument`, `PdfPageTree`, and editable-session objects to verify:

- insertion and attachment,
- removal and reinsertion,
- movement while retaining opening provenance,
- replacement while retaining untargeted page handles,
- targeted rotation changes,
- operation metadata,
- rejection of foreign-session handles,
- rejection of already-attached insertion, and
- detection of page-tree changes made outside the editing facade.

Moq is not required because BF-1051C exercises concrete model behavior. Moq remains reserved for later service, validator, policy, observer, SDK, and other substitutable boundaries.

## Optimization Review

- Page insert, remove, move, and replace use the existing high-level `PdfPageTree` and editable handle list; no PDF object graph is cloned.
- Structural synchronization checks are linear in page count and occur only at mutation boundaries.
- Page rotation remains a constant-time targeted mutation.
- No parsing, content decoding, resource resolution, reflection, or serialization is introduced.
- Untargeted page objects are not reconstructed.

## Acceptance Criteria

1. Page insert, remove, move, replace, and rotation-update operations are available only through the controlled editable-document facade.
2. Every mutation enforces session ownership.
3. Every completed mutation returns a page-scoped `IPdfEditOperation` description suitable for later transaction/change-set work.
4. Removed pages retain stable editable handles and can be reinserted by their owning session.
5. Moves do not rewrite source-page opening provenance.
6. Replacement preserves untargeted page handles and underlying page identities.
7. Foreign-session and already-attached page misuse fails before mutation.
8. External structural changes to the authoritative page tree are detected before a structural editing mutation.
9. FusionPro-oriented preservation remains target-specific and avoids gratuitous changes to unrelated page structures.
10. Transaction execution, undo/redo, content/resource mutation, and writing remain deferred to their dedicated slices.
11. Full PiratePDF release gate passes, including documentation generation and validation.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added controlled page insert, remove, move, replace, and rotation mutations with ownership enforcement, operation metadata, structural synchronization checks, and preservation-first behavior. |
