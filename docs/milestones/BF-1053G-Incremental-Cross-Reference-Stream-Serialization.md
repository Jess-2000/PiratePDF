# BF-1053G — Incremental Cross-Reference Stream Serialization

| Field | Value |
|---|---|
| **Document ID** | BF-1053G |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-16 |
| **Next Review** | 2026-11-15 |

## Objective

Extend preservation-aware incremental writing to support PDF cross-reference streams without changing the BF-1053F source-copy or explicit changed-object boundaries.

## Incremental Cross-Reference Stream

`PdfIncrementalUpdateWriter` now finalizes an incremental revision according to the concrete cross-reference representation selected by `PdfIncrementalWritePlan`. Traditional-table output remains unchanged. Cross-reference-stream output allocates a new generation-zero indirect object using the write plan's next object number and rejects allocation drift if that object number is no longer available.

`PdfIncrementalCrossReferenceStreamWriter` emits a sparse cross-reference stream containing type-1 entries only for explicitly changed objects and for the newly allocated cross-reference-stream object itself. Untargeted source objects are omitted from the stream instead of being incorrectly marked free.

The stream uses fixed widths `/W [1 8 2]`. `/Index` is generated from contiguous ranges represented by the sparse entry set, so the binary stream payload aligns exactly with the advertised ranges.

## Trailer Semantics

The cross-reference stream dictionary carries the incremental trailer entries: `/Size`, `/Root`, optional `/Info`, and `/Prev`. `/Prev` points to `IPdfOriginalSourceRevision.CrossReferenceOffset`, which is the active cross-reference section of the complete preceding revision.

`startxref` points to the indirect-object header of the newly appended cross-reference stream. No standalone `trailer` keyword is emitted for this representation.

`PreserveSource` continues to resolve during incremental planning. When the preceding revision uses a cross-reference stream, Writing therefore preserves that representation automatically. Explicit cross-reference-stream selection remains supported for PDF 1.5 or later.

## Preservation and Validation

The complete preceding source revision is still copied byte-for-byte before any appended bytes are written, and only `ChangedObjectIdentifiers` are serialized as updated document objects. The writer revalidates the changed-object set and PDF version before output. It also rejects collisions between the planned new cross-reference-stream object number and any current object generation.

Encrypted incremental output remains unsupported. BF-1053G does not project BF-1052 logical edits into indirect objects and does not introduce generalized dirty tracking.

## Acceptance Criteria

1. Traditional incremental output remains unchanged.
2. Incremental cross-reference streams are emitted for PDF 1.5 or later.
3. Original source bytes remain unchanged.
4. Only explicitly changed objects plus the new xref-stream object receive entries in the appended xref stream.
5. `/Index` accurately describes sparse contiguous entry ranges.
6. `/W [1 8 2]` encodes in-use offsets and generations.
7. `/Prev` points to the preceding revision's active cross-reference offset.
8. `/Size`, `/Root`, and optional `/Info` are carried by the xref-stream dictionary.
9. `startxref` points to the appended xref-stream object.
10. `PreserveSource` retains cross-reference-stream representation when the source used it.
11. Planned xref-stream object-number collisions are rejected.
12. Encrypted incremental output remains explicitly rejected.
13. No external dependency or CI prerequisite is added.
14. The full PiratePDF release gate passes.

## Deferred Work

Later BF-1053 slices still need preservation-aware projection of logical edits into indirect objects, deletion/free-entry handling, encrypted writing, Services/SDK exposure, and adversarial hardening across complete write workflows.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-16 | Added incremental cross-reference-stream serialization. |
