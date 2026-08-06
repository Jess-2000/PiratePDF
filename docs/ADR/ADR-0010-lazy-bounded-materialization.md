# ADR-0010 — Use Lazy, Bounded Materialization for Large Data

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-010 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1039, BF-1040, BF-1042 |
| **Last Updated** | 2026-08-06 |
| **Next Review** | 2026-11-06 |

---

**Status:** Accepted
**Date:** August 6, 2026
**Related Milestone:** BF-1040

## Context

PiratePDF must handle very large documents without eagerly expanding every
object, stream, image, page, and resource into memory. Garbage collection does
not solve retention, unbounded caching, or unnecessary duplication.

## Decision

Large PDF data is materialized lazily and under explicit resource budgets.
Subsystems must declare ownership of large buffers, bound caches by memory
cost, pool temporary buffers only at controlled boundaries, and dispose
unmanaged resources deterministically.

Routine calls to `GC.Collect` are prohibited as a memory-management strategy.

## Consequences

- Large-document work remains responsive and predictable.
- APIs must expose cancellation and resource limits.
- Some implementations require source-backed or disk-backed representations.
- Ownership and lifetime require explicit documentation and tests.

## Related Documents

- `docs/architecture/MEMORY-ARCHITECTURE.md`
- BF-1040 — Stream Filter Framework

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-06 | Adopted lazy, bounded materialization as the large-data policy. |
