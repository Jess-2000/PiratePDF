# BF-1039 — PDF Stream Parsing Foundation

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1039 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Tested |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1037, BF-1038, BF-1039 |
| **Last Updated** | 2026-07-31 |
| **Next Review** | 2026-10-31 |

## Status

Implementation and automated testing are complete. The milestone is ready for
release processing.

## Purpose

BF-1039 adds the foundation required to parse raw PDF stream objects while
preserving their encoded bytes exactly.

This milestone deliberately separates stream syntax from future filter
decoding and object resolution.

## Scope

BF-1039 includes:

- detection of a stream following an indirect-object dictionary,
- validation of direct integer `/Length` values,
- LF, CRLF, and CR handling after `stream`,
- exact raw-byte extraction,
- required line-ending validation before `endstream`,
- exact `endstream` keyword validation,
- immutable `PdfStream` construction,
- token-reader resynchronization after raw-byte parsing, and
- continuation through `endobj` and subsequent indirect objects.

## Architecture

Stream parsing is implemented under:

```text
src/BlackFlag.Engine.Parsing/Streams
```

The implementation consists of:

- `PdfStreamParser.cs`
- `PdfStreamParser.Header.cs`
- `PdfStreamParser.Data.cs`
- `PdfStreamParser.Terminator.cs`

`PdfObjectParser` retains responsibility for the indirect-object envelope. It
detects a dictionary followed by `stream`, delegates the raw-byte work, and
then continues with `endobj`.

`PdfTokenReader` provides an internal reset operation so the parser can
discard stale lookahead and resume lexing at the byte immediately after
`endstream`.

## Stream Length

BF-1039 accepts only a direct `PdfInteger` `/Length`.

The parser rejects:

- a missing `/Length`,
- a noninteger value,
- an indirect reference,
- a negative value,
- a value larger than `Int32.MaxValue`, and
- a declared range extending beyond the source.

Indirect `/Length` resolution remains deferred to the future object-resolution
layer.

## Raw Data Preservation

The parser reads exactly the declared byte count.

The raw encoded data is copied into the existing immutable `PdfStream` model
without decoding, normalization, text interpretation, or terminator scanning.

Consequently, payload bytes may include:

- binary zeroes,
- non-ASCII values,
- line-ending bytes, and
- the text `endstream`.

The declared length—not a keyword search—defines the payload boundary.

## Terminator Validation

After the declared data, the parser requires a valid PDF line ending followed
by the exact `endstream` keyword and a valid delimiter or white-space byte.

The parser then creates a new lexer at the byte immediately after
`endstream`, allowing normal token parsing to resume.

## Test Coverage

Automated coverage includes:

- LF, CRLF, and CR stream headers,
- empty streams,
- binary data,
- embedded `endstream` text inside payload bytes,
- direct `/Length`,
- missing `/Length`,
- name, boolean, and indirect-reference lengths,
- negative and oversized lengths,
- malformed stream headers,
- truncated data,
- missing pre-terminator line endings,
- malformed `endstream`,
- invalid keyword delimiters,
- missing `endobj`,
- consecutive indirect objects,
- reader resynchronization, and
- immutable data ownership.

## Deferred Scope

BF-1039 does not include:

- filter decoding,
- `DecodeParms`,
- indirect `/Length` resolution,
- cross-reference streams,
- object streams,
- encryption,
- repair scanning, or
- lazy source-backed stream storage.

## Architectural Decisions

BF-1039 is governed by:

- ADR-0000 — Project Charter Governance
- ADR-0007 — Separate Stream Parsing from Object Parsing
- ADR-0008 — Defer Indirect Stream Length Resolution
- ADR-0009 — Preserve Raw Stream Bytes Before Filter Decoding

## Acceptance Criteria

BF-1039 is ready for release when:

1. the feature branch is correct,
2. all cumulative production and test files are present,
3. the Release clean succeeds,
4. restore succeeds,
5. the complete Release build succeeds,
6. all Release tests pass,
7. `PiratePDF.DocGen all` succeeds,
8. documentation validation reports zero errors,
9. generated documentation is current, and
10. Git format checks succeed.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-31 | Documented the tested raw PDF stream parsing foundation. |

## Release Tag

Planned tag: `BF-1039-RC1`
