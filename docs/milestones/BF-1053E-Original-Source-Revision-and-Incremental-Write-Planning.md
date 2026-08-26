# BF-1053E — Original Source Revision and Incremental Write Planning

| Field | Value |
|---|---|
| **Document ID** | BF-1053E |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-15 |
| **Next Review** | 2026-11-15 |

## Objective

Establish the physical-source and changed-object contracts required before PiratePDF can safely append an incremental PDF revision. This slice plans incremental output; it deliberately does not serialize or append that revision yet.

## Source Revision Contract

`IPdfOriginalSourceRevision` exposes the complete immutable preceding revision, its byte length, its active cross-reference offset, its concrete cross-reference representation, and a copy operation. `PdfMemorySourceRevision` provides the first implementation and defensively copies supplied bytes.

`PreserveSource` is a policy, not a physical source representation, so a source revision must identify either a traditional table or cross-reference stream. The active cross-reference offset must identify a byte inside the captured revision.

## Incremental Planning

`PdfIncrementalWritePlanner` accepts an existing `PdfWritePlan`, an original source revision, and an explicit changed-object set. It requires `IncrementalUpdate`, rejects an empty change set, rejects identifiers absent from the current object store, sorts and deduplicates the set, and resolves `PreserveSource` to the source revision's concrete cross-reference representation.

The explicit changed-object boundary prevents an incremental writer from superseding every current object merely because no dirty-tracking contract exists yet. Untargeted objects can therefore remain byte-for-byte resident in the preceding revision.

Encrypted incremental output remains deferred. Cross-reference-stream output requires PDF 1.5 or later.

## Deferred Work

BF-1053E does not append bytes, emit `/Prev`, serialize incremental xref sections, project BF-1052 structural edits into indirect objects, write encrypted documents, or expose Writing through Services/SDK. Those remain later BF-1053 slices.

## Acceptance Criteria

1. A preceding PDF revision can be represented without transferring stream ownership to Writing.
2. The in-memory source implementation is immutable with respect to caller buffer mutation.
3. Incremental planning requires `PdfWriteMode.IncrementalUpdate`.
4. Changed identifiers are explicit, sorted, deduplicated, and must exist in the current object store.
5. `PreserveSource` resolves to the source revision's concrete xref representation.
6. Explicit xref representation remains selectable.
7. Encrypted incremental output remains rejected.
8. No incremental bytes are emitted in this slice.
9. No external dependency or CI prerequisite is added.
10. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-16 | Added missing revision history section. |
