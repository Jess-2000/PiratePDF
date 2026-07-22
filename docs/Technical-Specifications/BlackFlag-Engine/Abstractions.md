# BlackFlag Engine Abstractions Specification

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
