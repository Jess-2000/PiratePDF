# BF-1053D — Cross-Reference Stream Full-Rewrite Output

| Field | Value |
|---|---|
| **Document ID** | BF-1053D |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-15 |
| **Next Review** | 2026-11-15 |

## Objective

Extend the deterministic BF-1053 full-rewrite path with PDF cross-reference stream output while preserving the object-identity and non-mutation rules established in BF-1053A through BF-1053C.

## Cross-Reference Stream Boundary

`PdfFullRewriteWriter` remains the complete-revision façade. `PdfCrossReferenceWriteMode.CrossReferenceStream` now selects a cross-reference stream finalization path; `TraditionalTable` retains the BF-1053C path. `PreserveSource` continues to resolve to a traditional table until source-representation metadata is available.

Cross-reference streams require PDF 1.5 or later. Earlier document versions are rejected rather than silently upgrading the authoritative document version.

## Object Identity and Allocation

The cross-reference stream is a new physical indirect object allocated at `PdfWritePlan.NextObjectNumber`, above the highest existing object number. Existing gaps are not reused, existing identifiers are not renumbered, and the transient xref-stream object is not inserted into or used to mutate the authoritative `PdfDocument` object store.

The stream's `/Size` includes the xref-stream object itself. `startxref` points to the xref-stream indirect-object header.

## Entry Encoding

BF-1053D uses a deterministic `/W [1 8 2]` layout:

- one byte for entry type;
- eight bytes for the byte offset;
- two bytes for generation.

Values are encoded unsigned, big-endian. Object zero is emitted as a free entry with generation 65535. Existing objects are type 1 entries. Object-number gaps are free entries. The xref stream contains a type 1 entry for itself.

Multiple active generations for the same object number remain invalid for a complete current-revision representation and are rejected.

## Trailer Semantics

The xref-stream dictionary carries `/Type /XRef`, `/Size`, `/W`, `/Root`, and optional `/Info`. A full rewrite omits `/Prev`. No separate `trailer` keyword is emitted for this representation.

Encrypted output remains deferred; BF-1053D does not emit an `/Encrypt` reference while serializing plaintext object bodies.

## Testing Strategy

Tests verify xref-stream selection, dictionary fields, object allocation, `startxref`, exact binary entry values for object zero, existing objects, gaps and the xref stream itself, optional `/Info`, omission of `/Prev`, PDF-version rejection, and duplicate-generation rejection.

No external dependency or CI prerequisite is added.

## Deferred BF-1053 Work

Later slices will address original-source access, incremental revisions, edited-structure projection into indirect objects, preservation-aware stream/filter strategy, encryption-aware Writing, service/SDK integration, and adversarial/performance/preservation hardening.

BF-1053D does not implement object streams, text/font editing, graphics editing, image import/export, appearance generation, form-data merge, or application UI.

## Acceptance Criteria

1. Explicit cross-reference-stream mode produces a complete full-rewrite PDF revision for PDF 1.5 or later.
2. The xref stream receives a new object number above the existing namespace without mutating the document object store.
3. `/Type`, `/Size`, `/W`, `/Root`, and optional `/Info` are correct.
4. Binary entries accurately describe object zero, existing objects, gaps, and the xref stream itself.
5. `startxref` points to the xref-stream indirect-object header.
6. No traditional `xref` table or separate `trailer` keyword is emitted in xref-stream mode.
7. Source `/Prev` is omitted for a full rewrite.
8. PDF versions earlier than 1.5 are rejected for xref-stream output.
9. Existing traditional-table behavior remains supported.
10. Incremental and encrypted output remain explicitly deferred.
11. No external dependency or CI prerequisite is added.
12. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-15 | Added deterministic full-rewrite cross-reference stream output. |
