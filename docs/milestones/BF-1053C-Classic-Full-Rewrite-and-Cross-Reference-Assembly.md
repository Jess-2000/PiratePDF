# BF-1053C — Classic Full Rewrite and Cross-Reference Assembly

| Field | Value |
|---|---|
| **Document ID** | BF-1053C |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-15 |
| **Next Review** | 2026-11-15 |

## Objective

Assemble a complete deterministic PDF replacement revision from the BF-1053A write plan and BF-1053B object serializer. BF-1053C establishes the first end-to-end physical file path: PDF header, ordered indirect objects, object offsets, traditional cross-reference table, trailer, `startxref`, and EOF marker.

## Full-Rewrite Boundary

`PdfFullRewriteWriter` accepts an immutable `PdfWritePlan` and writes the plan's existing indirect-object namespace in stable identifier order. It does not renumber objects, fill object-number gaps, mutate document state, or create a parallel mutable representation.

The writer deliberately supports `PdfWriteMode.FullRewrite` only. Incremental updates require original source bytes and revision chaining and remain deferred.

## Header and Binary Marker

The file header uses the authoritative `PdfDocument.Version`. A binary comment containing bytes above 127 follows the header so consumers can identify the file as binary data.

## Object Offsets

`PdfCountingWriteStream` tracks physical bytes written without requiring a seekable output stream. Existing indirect objects are serialized directly through `PdfIndirectObjectSerializer`, and the count immediately before each object header becomes that object's cross-reference offset.

The destination remains caller-owned and is not closed by Writing.

## Traditional Cross-Reference Table

`PdfTraditionalCrossReferenceWriter` emits one contiguous subsection from object number zero through the highest existing object number. Object zero is the required free entry with generation 65535. Gaps in the object-number namespace are emitted as free entries rather than reused or silently compacted.

Traditional entries retain existing generation numbers and use fixed-width ten-digit offsets and five-digit generations. Offsets beyond the representable traditional-table range are rejected. Multiple simultaneously stored generations for the same object number are also rejected because one traditional xref entry cannot describe multiple active values for one object number.

`PdfCrossReferenceWriteMode.TraditionalTable` is supported explicitly. `PreserveSource` resolves to the traditional representation for this first full-rewrite path because source-representation metadata is not yet available. Cross-reference streams remain deferred.

## Trailer and Revision Finalization

The full-rewrite trailer contains `/Size`, `/Root`, and optional `/Info`. `/Size` is highest object number plus one, not object count. The source trailer's `/Prev` value is intentionally omitted because a complete replacement revision has no preceding revision within the newly written file.

Encrypted output is rejected until encryption-aware serialization exists; copying `/Encrypt` while writing plaintext object bodies would create an invalid and unsafe document.

`startxref` points exactly to the first byte of the `xref` keyword, followed by `%%EOF`.

## Preservation Rules

BF-1053C preserves existing object identifiers and untargeted logical objects represented in the write plan. It does not claim byte-for-byte preservation because BF-1053B deterministically reserializes object syntax. Raw source-revision byte reuse and incremental preservation remain later work.

A plan is a snapshot of object identity, not a freeze of mutable document state. If a planned object is removed before execution, Writing rejects the stale plan rather than silently producing an incomplete xref table.

## Testing Strategy

Tests use real document objects and verify complete file framing, exact xref offsets, `startxref`, free gap entries, trailer `/Size` and `/Root`, optional `/Info`, omission of source `/Prev`, caller-owned streams, non-seekable output, unsupported incremental/xref-stream/encrypted paths, stale plan rejection, and duplicate active-generation rejection.

No mock library or external dependency is required.

## Deferred BF-1053 Work

Later slices will address cross-reference streams, original-source access, incremental revisions, edited-structure projection into indirect objects, preservation-aware stream/filter strategy, encryption-aware Writing, service/SDK integration, and adversarial/performance/preservation hardening.

BF-1053C does not implement text/font editing, graphics editing, image import/export, appearance generation, form-data merge, or application UI.

## Acceptance Criteria

1. Full-rewrite output starts with the authoritative PDF version and a binary marker.
2. Existing indirect objects are emitted in deterministic identifier order without renumbering.
3. Captured xref offsets point to the exact indirect-object header bytes.
4. Traditional xref output includes object zero and free entries for object-number gaps.
5. `/Size` equals highest object number plus one.
6. `/Root` and optional `/Info` retain planned object identity.
7. Source `/Prev` is not copied into a complete replacement revision.
8. `startxref` points to the `xref` keyword and output terminates with `%%EOF`.
9. Non-seekable writable destinations are supported and caller-owned streams remain open.
10. Incremental updates, xref streams, and encrypted output are explicitly rejected rather than partially implemented.
11. No external dependency or CI prerequisite is added.
12. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-15 | Added deterministic classic full-rewrite assembly and traditional cross-reference output. |
