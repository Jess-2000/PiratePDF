# BlackFlag Engine Abstractions Specification

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-010 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | In Development |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-2000 |
| **Last Updated** | 2026-07-24 |
| **Next Review** | 2026-10-24 |

---

**Status:** Draft  
**Version:** 1.0

## Purpose

`BlackFlag.Engine.Abstractions` defines cross-module contracts without providing concrete implementations.

## Design rules

- Keep interfaces cohesive.
- Prefer capability-oriented contracts.
- Avoid leaking implementation classes.
- Use asynchronous APIs only when useful.
- Accept cancellation tokens for long-running operations.
- Document ownership and disposal.

## Initial contract

`IRandomAccessSource` provides:

- `long Length`
- Synchronous reads from absolute offsets
- Asynchronous reads from absolute offsets
- `IDisposable`
- `IAsyncDisposable`

The interface must support files larger than 2 GB.
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized specification metadata and governance structure. |
