# ADR-0003 — Use Random-Access File I/O

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
