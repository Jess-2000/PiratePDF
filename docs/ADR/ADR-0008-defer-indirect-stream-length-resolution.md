# ADR-0008 — Defer Indirect Stream Length Resolution

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-008 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1039, BF-1043 |
| **Last Updated** | 2026-07-31 |
| **Next Review** | 2026-10-31 |

---

**Status:** Accepted
**Date:** July 31, 2026
**Related Milestone:** BF-1039

## Context

The PDF specification permits `/Length` to be an indirect reference. Resolving
that reference requires cross-reference lookup, object loading, cycle
handling, and resolution policy.

BF-1039 provides syntax parsing but does not yet include the object-resolution
engine required to resolve indirect values safely.

## Decision

BF-1039 accepts only a direct integer `/Length`.

Missing, indirect, noninteger, negative, oversized, and out-of-range lengths
produce deterministic failures.

Indirect length support is deferred to the object-resolution milestone rather
than approximated inside the syntax parser.

## Alternatives Considered

### Resolve the reference inside `PdfStreamParser`

Rejected because it would couple syntax parsing to document loading and
cross-reference resolution.

### Search for `endstream` when length is indirect

Rejected because the keyword may occur in valid stream data and repair
heuristics require a separate policy.

### Silently treat the remaining file as stream data

Rejected because it is unsafe, unbounded, and structurally incorrect.

## Consequences

- BF-1039 remains deterministic and testable.
- Parser responsibilities stay narrow.
- Some valid PDFs with indirect `/Length` cannot yet be fully loaded.
- The future resolver must supply an explicit, bounded mechanism for indirect
  lengths.

## Future Work

The object-resolution engine will support indirect `/Length` after safe
cross-reference lookup and cycle detection are available.

## Related Documents

- BF-1039 — PDF Stream Parsing Foundation
- ADR-0007 — Separate Stream Parsing from Object Parsing

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-31 | Deferred indirect stream-length resolution to the object-resolution layer. |
