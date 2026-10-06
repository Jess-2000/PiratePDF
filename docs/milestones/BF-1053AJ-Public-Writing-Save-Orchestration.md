# BF-1053AJ — Public Writing/Save Orchestration

| Field | Value |
|---|---|
| **Document ID** | BF-1053AJ |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## Objective

Provide one public Services-layer boundary that turns a committed Editing transaction into serialized PDF output without requiring callers to manually compose write planners, projection planners, and concrete writers.

## Public Save Boundary

`PdfEditingSaveService` exposes byte-array and caller-owned-stream overloads.

The caller supplies:

- an active `PdfEditingSession`;
- one committed non-empty `PdfEditChangeSet`;
- optional `PdfWriteOptions`;
- an original source revision when incremental output is requested.

The service creates the base preservation-aware write plan, selects the transaction orchestration path, creates the projected plan, and executes the matching writer.

## Mode Selection

`PdfWriteOptions.Mode` selects:

- `FullRewrite` → `PdfFullRewriteWriter`;
- `IncrementalUpdate` → `PdfIncrementalUpdateWriter`.

Incremental output requires `IPdfOriginalSourceRevision`.

`PreserveSource` cross-reference selection continues to resolve from the original source revision through the existing incremental planner.

## Editing Domain Selection

The public save boundary currently recognizes two completed BF-1053 Editing domains:

- page structural/state transactions through `PdfNestedPageTransactionWritingService`;
- annotation transactions through `PdfAnnotationTransactionWritingService`.

A single transaction mixing page and annotation domains is rejected explicitly in AJ rather than being serialized through two independent projection/rollback boundaries.

Cross-domain projection composition remains a later integration concern.

## Ownership and Atomic Planning

Every operation is ownership-validated against the supplied editing session before dispatch.

Existing transaction services retain responsibility for reservation, projection, rollback, and delayed binding.

Serialization begins only after projected planning succeeds.

## Stream Ownership

The stream overload requires a writable stream and does not close the caller-owned destination.

The byte-array overload is a convenience wrapper over the same orchestration path.

## Acceptance Criteria

1. Callers can serialize a committed Editing transaction through one public service.
2. Full rewrite is selected from `PdfWriteOptions.Mode`.
3. Incremental update is selected from `PdfWriteOptions.Mode`.
4. Incremental save requires the original source revision.
5. `PreserveSource` xref selection remains source-driven.
6. Page transactions use the nested page transaction service.
7. Annotation transactions use the annotation transaction service.
8. Unsupported/mixed Editing domains fail explicitly.
9. Caller-owned streams remain open.
10. The full PiratePDF release gate passes.

## Deferred Work

Cross-domain page + annotation projection composition, encrypted Writing, detached-object free-entry policy, optional page-tree compaction/rebalancing, and higher-level application file-system UX remain deferred.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Added public transaction-aware full-rewrite/incremental save orchestration. |
