# BF-1051F — Content and Resource Mutation

| Field | Value |
|---|---|
| **Document ID** | BF-1051F |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Extend the controlled BF-1051 editing surface from page-tree mutations to page content-stream and direct page-resource mutations while preserving the transaction, undo/redo, ownership, and preservation contracts established by BF-1051A through BF-1051E.

## Contract

`PdfEditableDocument` remains the public mutation boundary. BF-1051F adds controlled operations for replacing page content bytes, setting a named direct page resource, and removing a named direct page resource.

The authoritative `PdfPage` model owns the corresponding content and resource state. Editing receives internal access to those mutation primitives through an explicit friend-assembly boundary; applications do not receive a second raw mutation API.

## Content Mutation

Page content replacement:

- accepts a byte span and stores a defensive copy,
- exposes content reads as defensive byte-array copies,
- records previous and replacement lengths in `PdfContentEditOperation`,
- participates in transaction rollback and committed undo/redo, and
- does not parse, normalize, or rewrite untouched content bytes.

Content parsing and higher-level operator editing remain separate concerns. This slice establishes mutation ownership and reversible state.

## Resource Mutation

Direct page resources are addressed by PDF resource category and resource name. Set operations may add a new entry or replace an existing entry. Remove operations require the named entry to exist.

Resource mutations retain the original `PdfObject` reference for rollback and undo rather than cloning or reconstructing the resource. Empty resource-category dictionaries created by editing are removed when their last entry is removed.

Inherited-resource editing and resource-resolution policy remain outside this slice; BF-1051F operates on direct page-owned resource entries.

## Operations and History

`PdfContentEditOperation` uses `PdfEditScope.Content`. `PdfResourceEditOperation` uses `PdfEditScope.Resource`.

Both mutation families use the BF-1051D reversible change mechanism and BF-1051E history. Mutations outside a transaction retain immediate behavior and do not implicitly create undo entries.

## Preservation and FusionPro Compatibility

Untargeted content and resources are not normalized or cloned. Content replacement changes only the targeted page-owned content byte sequence. Resource replacement changes only the targeted category/name entry and retains untargeted object identity.

This preserves the project requirement to avoid collateral structural rewriting, including targeted FusionPro compatibility. Serialization-level preservation remains a writing-system responsibility.

## Testing Strategy

Tests use real document, page, session, transaction, and history objects. They verify:

- defensive content copying,
- content undo/redo,
- resource set/remove rollback,
- replacement participation in undo/redo,
- resource operation metadata, and
- rejection of missing-resource removal without recording a transaction change.

Moq is not used because this slice validates authoritative in-memory state transitions.

## Acceptance Criteria

1. Page content can be replaced only through the controlled editing surface.
2. Content reads and writes do not expose mutable byte-array ownership.
3. Direct page resources can be added, replaced, and removed through controlled editing APIs.
4. Content mutations produce `PdfEditScope.Content` operations.
5. Resource mutations produce `PdfEditScope.Resource` operations.
6. Content and resource mutations participate in transaction rollback.
7. Committed content and resource mutations participate in undo/redo.
8. Failed mutations do not create transaction changes.
9. Untargeted resources retain object identity and are not normalized.
10. Existing page mutation, transaction, and history behavior remains compatible.
11. Full PiratePDF release gate passes after local review.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added reversible page content replacement and direct page-resource mutation through the editing session. |
