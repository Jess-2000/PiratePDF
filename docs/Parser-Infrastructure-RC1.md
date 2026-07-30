# BlackFlag Engine — Parser Infrastructure RC1

| Field | Value |
|---|---|
| Document ID | BF-DOC-0004 |
| Category | Architecture |
| Version | 0.4.0 |
| Status | Tested |
| Owner | PiratePDF Project |
| Related BF Work Items | BF-0004 |
| Last Updated | 2026-07-30 |
| Next Review | 2026-10-30 |

## Scope

Parser Infrastructure RC1 adds the token-consumption layer between `PdfLexer` and the future `PdfParser`.

```text
IRandomAccessCursor
        ↓
    PdfLexer
        ↓
 PdfTokenReader
        ↓
    PdfParser
```

This milestone deliberately does **not** parse PDF objects. It freezes the mechanics that every parser routine will use for lookahead, consumption, matching, expectations, end-of-file handling, and diagnostics.

## Public API

`PdfTokenReader` provides:

- `Peek()`
- `Peek(int lookahead)`
- `Read()`
- `Match(PdfTokenKind)`
- `Expect(PdfTokenKind)`
- `EndOfFile`
- `HasCurrent`
- `Current`
- `Position`

`PdfUnexpectedTokenException` provides:

- expected token kind
- actual token
- exact source position
- consistent location-aware diagnostic text
- optional inner exception

## Behavioral contract

### Lookahead

Lookahead is zero-based. `Peek()` and `Peek(0)` return the next unread token. Peeking never changes `Current` or consumes input.

### Current token

`Current` is the most recently consumed token. It throws `InvalidOperationException` until the first successful `Read`, `Match`, or `Expect` operation. `HasCurrent` allows callers to test this state without an exception.

### Position

`Position` is the starting position of the next unread token. At the logical end of the source, it is the lexer-provided end-of-file position.

### End of file

The first end-of-file token produced by the lexer is cached. Arbitrarily distant lookahead and repeated reads beyond the source return that same token. The reader never asks the lexer to read beyond its first end-of-file token.

### Failed matches and expectations

A failed `Match` or `Expect` does not consume the unexpected token and does not change `Current`.

## Ring-buffer design

The token reader uses a dynamically growing circular array rather than `Queue<PdfToken>`.

Benefits:

- constant-time access to buffered lookahead
- constant-time consumption from the head
- no shifting of buffered tokens
- no per-token node allocations
- compact contiguous storage
- predictable behavior for large lookahead windows

The initial capacity defaults to eight tokens and can be configured for tests or specialized parser workloads. Capacity doubles only when required. Growth copies buffered tokens in logical order and resets the ring head to zero.

## Complexity

| Operation | Typical cost | Notes |
|---|---:|---|
| `Peek()` | O(1) | May lex one token when the buffer is empty |
| `Peek(n)` | O(n) first time | Only newly required tokens are lexed |
| `Read()` | O(1) | Ring-head advancement |
| `Match()` | O(1) | One peek and optional read |
| `Expect()` | O(1) | One peek and optional read |
| Buffer growth | O(b) | Amortized; `b` is buffered-token count |

## Ownership and concurrency

The reader does not own or dispose the lexer. Callers must not read the lexer independently while its reader is active. `PdfTokenReader` is intentionally not thread-safe; a parser instance and its token reader are expected to be confined to one parsing operation.

## Test coverage

The supplied tests cover:

- constructor validation
- zero-based lookahead
- non-consuming peeks
- sequential reads
- current-token state
- position tracking
- empty input
- stable end-of-file behavior
- successful and unsuccessful matches
- successful and unsuccessful expectations
- diagnostic metadata and messages
- ring-buffer growth
- wrapped-ring growth and ordering

## Integration

Copy production files to:

```text
src/BlackFlag.Engine.Syntax/Parsing/
```

Copy test files to:

```text
tests/BlackFlag.Engine.Syntax.Tests/Parsing/
```

No project-file changes should be necessary when the projects use normal SDK-style wildcard compilation.

## RC1 definition of done

- [x] Public API established
- [x] Ring-buffer implementation complete
- [x] Stable end-of-file behavior
- [x] Parser-oriented exception type
- [x] XML documentation
- [x] Defensive argument validation
- [x] Regression test suite
- [x] Architecture and performance notes
- [ ] Build and tests verified in the full repository
- [ ] Internal review completed
- [ ] Milestone merged and tagged

The final three items must be completed in the actual `PiratePDF` repository because this package contains only the files supplied for the milestone, not the complete solution or all lexer partial files.

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.4.0 | 2026-07-30 | Added missing revision history section. |
