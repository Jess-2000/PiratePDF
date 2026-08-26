# BF-1053F — Traditional Incremental Update Serialization

| Field | Value |
|---|---|
| **Document ID** | BF-1053F |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-16 |
| **Next Review** | 2026-11-15 |

## Objective

Emit the first preservation-aware incremental PDF revision by copying the complete preceding revision unchanged, serializing only the explicitly changed indirect objects, and appending a traditional cross-reference table and trailer.

## Incremental Serialization

`PdfIncrementalUpdateWriter` copies `IPdfOriginalSourceRevision` bytes first and verifies that the copied byte count matches the source contract. It then writes only `PdfIncrementalWritePlan.ChangedObjectIdentifiers`. Object offsets are absolute offsets in the combined output.

A separator newline is appended after the preceding revision before new indirect objects. This leaves every original byte untouched while ensuring a lexical boundary even when the source revision does not end in whitespace.

The caller-owned destination does not need to be seekable and remains open.

## Traditional Incremental Cross-Reference Table

`PdfIncrementalTraditionalCrossReferenceWriter` emits only changed-object subsections. It does not mark unchanged source objects free. Contiguous changed object numbers are grouped into a subsection; gaps begin new subsections.

Traditional ten-digit offset limits and the single-active-generation-per-object-number constraint are enforced.

## Incremental Trailer

The appended trailer contains:

- `/Size` based on the highest object number in the current document store plus one;
- `/Root` from the write plan;
- optional `/Info`;
- `/Prev` pointing to the active cross-reference offset of the preceding source revision.

`startxref` points to the appended cross-reference table.

## Deferred Work

BF-1053F does not emit incremental cross-reference streams, perform encrypted incremental writing, project BF-1052 structural edits into indirect objects, implement generalized dirty tracking, or expose Writing through Services/SDK.

## Acceptance Criteria

1. Original source bytes are copied without modification.
2. Only explicitly changed indirect objects are appended.
3. Appended object offsets are absolute offsets in the combined output.
4. Traditional xref output contains changed-object subsections only.
5. `/Prev` points to the preceding revision's active cross-reference offset.
6. `/Size`, `/Root`, and optional `/Info` are correct.
7. `startxref` points to the appended xref table.
8. Non-seekable caller-owned destinations are supported and remain open.
9. Incremental xref-stream and encrypted output remain explicitly rejected.
10. No external dependency or CI prerequisite is added.
11. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-16 | Added traditional incremental-update serialization. |
