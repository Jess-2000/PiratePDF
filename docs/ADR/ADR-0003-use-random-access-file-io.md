# ADR-0003 — Use Random-Access File I/O

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-003 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-2100 |
| **Last Updated** | 2026-07-24 |
| **Next Review** | 2026-10-24 |

---

**Status:** Accepted  
**Date:** July 22, 2026

## Context

PiratePDF must handle very large PDFs, including documents containing 6,000 pages or more, without loading entire files into memory.

## Decision

BlackFlag Engine will use a position-independent random-access source abstraction.

File implementations should support explicit 64-bit offsets, concurrent reads, asynchronous reads, and bounded memory usage.

## Consequences

- Large files remain manageable.
- Lazy parsing becomes practical.
- Concurrent page rendering and analysis are possible.
- The parser does not rely on shared stream position.
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized document metadata and governance structure. |
