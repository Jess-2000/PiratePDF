# ADR-0007 — Separate Stream Parsing from Object Parsing

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-007 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1037, BF-1039, BF-1040 |
| **Last Updated** | 2026-07-31 |
| **Next Review** | 2026-10-31 |

---

**Status:** Accepted
**Date:** July 31, 2026
**Related Milestone:** BF-1039

## Context

A PDF stream combines ordinary object syntax with an exact-length region of
raw bytes. The existing object parser is token-based, while stream payloads
may contain arbitrary binary values that must not be interpreted as tokens.

Adding raw-byte logic directly to `PdfObjectParser` would mix unrelated
responsibilities and complicate future stream filters, cross-reference
streams, and object streams.

## Decision

`PdfObjectParser` remains responsible for the indirect-object envelope and
dictionary syntax.

A dedicated internal `PdfStreamParser` owns:

- `/Length` validation,
- stream-header line endings,
- exact raw-byte extraction,
- `endstream` validation, and
- token-reader resynchronization.

## Alternatives Considered

### Parse streams entirely inside `PdfObjectParser`

Rejected because it would enlarge the central parser and couple token parsing
to raw-byte handling.

### Teach the lexer to tokenize stream payloads

Rejected because arbitrary payload bytes are not lexical PDF tokens and must
be bounded by the declared length.

### Search for `endstream`

Rejected because the byte sequence may legally occur inside the payload.

## Consequences

- Stream syntax is independently testable.
- Object parsing remains focused.
- Filter decoding can be added without redesigning the object parser.
- Cross-reference and object streams can reuse the same raw-stream foundation.
- The token reader needs a controlled internal reset operation.

## Future Work

BF-1040 will add filter decoding above the raw stream parser. Later milestones
will interpret decoded stream data for cross-reference and object streams.

## Related Documents

- BF-1039 — PDF Stream Parsing Foundation
- ADR-0009 — Preserve Raw Stream Bytes Before Filter Decoding

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-31 | Separated raw stream parsing from the general object parser. |
