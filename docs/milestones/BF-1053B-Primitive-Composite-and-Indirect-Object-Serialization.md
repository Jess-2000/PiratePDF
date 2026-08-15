# BF-1053B — Primitive, Composite, and Indirect-Object Serialization

| Field | Value |
|---|---|
| **Document ID** | BF-1053B |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-15 |
| **Next Review** | 2026-11-15 |

## Objective

Establish deterministic byte serialization for the authoritative PDF object model without yet assembling a complete PDF file. BF-1053B converts primitive, composite, stream, and indirect objects into valid PDF object syntax while preserving the architectural boundary established by BF-1053A: logical Editing state remains authoritative and Writing alone owns physical representation.

## Object Serialization

`PdfObjectSerializer` writes nulls, booleans, integers, real numbers, names, strings, arrays, dictionaries, streams, and indirect references. The serializer accepts existing `BlackFlag.Engine.Document` objects directly and introduces no parallel mutable writing model.

Serialization is deterministic. Dictionaries retain the document model's ordinal name ordering; arrays retain logical order; hexadecimal output uses uppercase digits; and indirect references retain their object and generation numbers.

## Names, Strings, and Numbers

Names are emitted from their decoded bytes. Whitespace, delimiter bytes, `#`, and bytes outside the regular printable range are escaped with `#XX`, preventing decoded names from being reinterpreted as PDF syntax.

Literal strings preserve decoded byte content through PDF escape syntax. Parentheses and backslashes are escaped, standard control characters use their named escapes, and remaining non-printable bytes use fixed three-digit octal escapes. Hexadecimal strings preserve their requested representation and use two hexadecimal digits per byte.

Integer formatting is invariant-culture decimal. Real formatting is also invariant and never uses exponent notation, which PDF real syntax does not permit. Integral real values retain a decimal marker such as `1.0` so Writing does not silently change a `PdfReal` into integer syntax.

## Composite Safety

Direct arrays and dictionaries may be mutated into cyclic object graphs even though cyclic PDF document structure should normally be expressed through indirect references. The serializer detects an active direct-object cycle and rejects it instead of recursing indefinitely. Composite nesting also has a bounded depth to prevent accidental stack exhaustion from adversarial or malformed in-memory graphs.

## Stream Serialization

`PdfStream` owns encoded stream bytes. BF-1053B emits those bytes unchanged and creates a temporary dictionary copy whose `/Length` is set to the actual encoded byte count. The authoritative stream dictionary is never mutated by serialization. Filter transformation, stream recompression, encryption, and raw-source byte preservation decisions remain later Writing concerns.

## Indirect Objects

`PdfIndirectObjectSerializer` emits the existing `PdfObjectIdentifier` followed by `obj`, the serialized object body, and `endobj`. It does not allocate object identifiers and does not renumber existing objects; allocation remains governed by the BF-1053A write plan.

BF-1053B deliberately does not emit file headers, cross-reference data, trailers, `startxref`, EOF markers, or source revisions. Those belong to subsequent BF-1053 slices.

## Testing Strategy

Tests use real document-model objects and verify primitive syntax, exponent-free real formatting, name escaping, literal and hexadecimal strings, deterministic dictionary ordering, arrays, indirect references, stream length correction without source mutation, direct-cycle rejection, non-writable destinations, and complete indirect-object syntax. No mock library is required because this slice has no substitutable service boundary.

## Optimization Review

Serialization writes directly to the caller's `Stream` and does not build a parallel object graph. `Serialize` convenience methods allocate only the returned byte array. Stream data is defensively copied once because the current `PdfStream` API intentionally exposes encoded bytes through `ToByteArray`; a later streaming/source-revision slice may introduce a lower-copy internal path without changing this contract.

The active-composite set contains only the current recursion path and therefore scales with nesting depth rather than total document size.

## Deferred BF-1053 Work

Later slices will own complete-file assembly, PDF header/version output, object offset capture, cross-reference tables and streams, trailer and `startxref` emission, full rewrite execution, original-source access and incremental updates, edited-structure projection, stream/filter preservation strategy, encryption-aware writing, Services/SDK exposure, and adversarial/performance/preservation hardening.

BF-1053B does not perform content-stream generation, appearance generation, text/font editing, graphics editing, image import/export, or form-data merge.

## Acceptance Criteria

1. Every current `PdfObjectKind` has deterministic serialization behavior.
2. Names are escaped from decoded bytes using PDF name syntax.
3. Literal and hexadecimal string representations preserve decoded byte content.
4. Real numbers are culture-invariant, exponent-free, and retain real-number syntax.
5. Arrays preserve item order and dictionaries serialize deterministically.
6. Direct composite cycles are rejected and nesting is bounded.
7. Streams emit existing encoded bytes and a correct `/Length` without mutating the source dictionary.
8. Indirect-object headers retain the supplied object and generation numbers.
9. No object-number allocation, xref/trailer/file assembly, encryption transformation, or Editing dependency is introduced.
10. No external package or CI prerequisite is added.
11. Tests cover primitive, composite, stream, escaping, safety, and indirect-object emission paths.
12. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-15 | Added deterministic primitive, composite, stream, and indirect-object serialization. |
