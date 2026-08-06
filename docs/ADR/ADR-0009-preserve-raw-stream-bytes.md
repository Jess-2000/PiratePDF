# ADR-0009 — Preserve Raw Stream Bytes Before Filter Decoding

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-009 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1039, BF-1040, BF-1041 |
| **Last Updated** | 2026-07-31 |
| **Next Review** | 2026-10-31 |

---

**Status:** Accepted
**Date:** July 31, 2026
**Related Milestone:** BF-1039

## Context

PDF stream bytes may be encoded through one or more filters. Parsing and
decoding are separate operations with different failure modes, resource
budgets, and security concerns.

Decoding during syntax parsing would couple the parser to filter
implementations and make it harder to preserve, inspect, copy, or diagnose the
original encoded representation.

## Decision

BF-1039 stores the exact declared stream bytes in `PdfStream` without
decoding, normalization, or text conversion.

Filter interpretation and decoded-data production belong to a separate
stream-filter framework.

## Alternatives Considered

### Decode supported filters during parsing

Rejected because parser correctness would depend on optional decoder support.

### Replace raw bytes with decoded bytes

Rejected because it would lose the source representation required for
round-tripping, forensic inspection, and diagnosis.

### Store both raw and decoded bytes eagerly

Rejected because it would increase allocations and perform unnecessary work
for streams that are never decoded.

## Consequences

- Parsed streams preserve source fidelity.
- Unsupported filters do not prevent raw stream parsing.
- Decoding can use explicit limits, cancellation, and security policy.
- Consumers must request decoded data through the future filter layer.
- Future lazy or source-backed storage can be introduced without changing the
  syntax boundary.

## Future Work

BF-1040 will introduce a reusable filter registry and decoding pipeline.
BF-1041 will interpret decoded cross-reference stream data.

## Related Documents

- BF-1039 — PDF Stream Parsing Foundation
- ADR-0007 — Separate Stream Parsing from Object Parsing

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-31 | Required preservation of encoded stream bytes before filter decoding. |
