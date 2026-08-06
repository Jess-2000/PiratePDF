# ADR-0012 — Implement Filter Decoders as Pure Transformations

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-012 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1039, BF-1040, BF-1041 |
| **Last Updated** | 2026-08-06 |
| **Next Review** | 2026-11-06 |

---

**Status:** Accepted
**Date:** August 6, 2026
**Related Milestone:** BF-1040

## Context

Stream filters are reusable byte transformations. Mixing parsing, I/O,
caching, rendering, or security policy into decoders would make them harder to
test and reuse.

## Decision

A filter decoder receives encoded bytes, writes decoded bytes, observes a
bounded decode context, and performs no source I/O, network I/O, document
parsing, rendering, caching, or document interpretation.

The pipeline owns filter ordering, registry resolution, output ownership,
resource enforcement, and multi-filter orchestration.

## Consequences

- Decoders are deterministic and independently testable.
- The same decoder can serve parsing, validation, optimization, and writing.
- Pipeline infrastructure must enforce cross-filter limits.
- Image interpretation remains separate from byte-filter transformation.

## Related Documents

- BF-1040 — Stream Filter Framework
- ADR-0009 — Preserve Raw Stream Bytes Before Filter Decoding
- `docs/architecture/MEMORY-ARCHITECTURE.md`

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-06 | Established the pure filter-transformation boundary. |
