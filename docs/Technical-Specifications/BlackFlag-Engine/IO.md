# BlackFlag Engine I/O Specification

**Status:** Draft  
**Version:** 1.0

## Purpose

`BlackFlag.Engine.IO` provides concrete sources, buffers, streams, and temporary storage.

## Initial implementation

`FileRandomAccessSource` should:

- Normalize file paths.
- Open read-only handles.
- Use absolute offsets.
- Support concurrent reads.
- Return partial reads at end of file.
- Return zero when reading at or beyond end of file.
- Reject negative offsets.
- Throw after disposal.
- Support cancellation for asynchronous reads.
- Avoid loading the full file.

## Required tests

- Existing-file construction.
- Missing-file failure.
- Invalid-path failure.
- Beginning and middle reads.
- Partial end-of-file reads.
- Reads at and beyond end of file.
- Empty destinations.
- Negative offsets.
- Async reads.
- Cancelled reads.
- Reads after disposal.
- Concurrent independent reads.
- Repeated disposal.
