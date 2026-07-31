# BF-1038 — Traditional Cross-Reference Tables and Trailers

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1038 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Tested |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1036, BF-1037, BF-1038 |
| **Last Updated** | 2026-07-31 |
| **Next Review** | 2026-10-31 |

## Status

Implementation and automated testing are complete. The milestone is ready for
final release processing.

## Purpose

BF-1038 adds support for traditional PDF cross-reference tables, trailer
dictionaries, final `startxref` discovery, and incremental-update traversal.

This allows BlackFlag Engine to locate indirect objects without scanning the
entire PDF source.

## Scope

BF-1038 includes:

- traditional `xref` tables,
- one or more cross-reference subsections,
- free and in-use entries,
- trailer dictionaries,
- final `startxref` discovery,
- `/Prev` traversal,
- `/Prev` cycle detection,
- physical table preservation,
- oldest-to-newest chain ordering, and
- an immutable effective cross-reference lookup.

## Architecture

The implementation lives in:

```text
src/BlackFlag.Engine.Parsing/CrossReferences
```

The public immutable model consists of:

- `PdfObjectId`
- `PdfCrossReferenceEntry`
- `PdfCrossReferenceSection`
- `PdfCrossReferenceTable`
- `PdfCrossReferenceChain`

Construction uses the internal:

- `PdfCrossReferenceChainBuilder`

Parsing uses:

- `PdfCrossReferenceParser`
- `IRandomAccessSource`
- `BufferedRandomAccessCursor`
- the existing `PdfLexer`
- the existing `PdfTokenReader`
- the existing `PdfObjectParser`

The trailer dictionary is parsed through the existing object parser rather
than through a duplicate dictionary implementation.

## Object Identifiers

`PdfObjectId` represents:

- object number `0` through `Int32.MaxValue`, and
- generation number `0` through `65535`.

Object number zero is supported because traditional cross-reference tables use
object zero as the free-list head.

This differs intentionally from ordinary indirect-object identifiers, which
require positive object numbers.

## Physical Tables

`PdfCrossReferenceTable` preserves one physical traditional table, including:

- its absolute source offset,
- its subsections in source order, and
- its trailer dictionary.

`PdfCrossReferenceSection` preserves:

- the first declared object number, and
- sequential entries in source order.

Duplicate object identifiers inside one physical table are rejected.

## Cross-Reference Chain

`PdfCrossReferenceChain` preserves physical tables from oldest to newest.

Its effective lookup is constructed by applying each physical table in that
order. A later entry with the same `PdfObjectId` replaces the earlier entry.

Entries with different generation numbers remain distinct.

The mutable construction process is isolated inside
`PdfCrossReferenceChainBuilder`. Public results remain immutable.

## StartXRef Discovery

The parser searches backward near the end of the source for:

```text
startxref
<offset>
%%EOF
```

The final valid section is used.

The parser rejects:

- an empty source,
- a missing `%%EOF`,
- a missing `startxref`,
- a nonnumeric offset,
- unexpected data before `%%EOF`, and
- an offset outside the source.

## Traditional Table Parsing

The parser supports subsection headers in the form:

```text
<first-object-number> <entry-count>
```

Traditional entries use the form:

```text
0000000000 65535 f
0000000017 00000 n
```

The parser validates:

- nonnegative subsection values,
- sequential object numbering,
- ten-digit offset fields,
- five-digit generation fields,
- `n` and `f` entry markers,
- required line endings,
- required trailer dictionaries, and
- truncated input.

Both LF and CRLF line endings are supported.

## Trailer and Incremental Updates

The trailer must be a PDF dictionary.

When `/Prev` is present:

- it must be a nonnegative PDF integer,
- it must identify a byte inside the source,
- the referenced table is parsed,
- traversal continues until no `/Prev` remains, and
- repeated offsets are rejected as cycles.

## Test Coverage

Automated coverage includes:

- object identifier range validation,
- object-zero free-list support,
- entry and section validation,
- immutable collection copying,
- duplicate table-entry rejection,
- single-table parsing,
- multiple subsections,
- LF and CRLF input,
- free and in-use entries,
- zero-entry subsections,
- final `startxref` selection,
- incremental-update chains,
- oldest-to-newest table ordering,
- newest-entry-wins behavior,
- different-generation retention,
- `/Prev` cycles,
- malformed subsection headers,
- malformed cross-reference entries,
- missing trailers,
- nondictionary trailers,
- invalid `/Prev` values, and
- out-of-range offsets.

## Deferred Scope

BF-1038 does not include:

- cross-reference streams,
- object streams,
- stream decoding,
- encryption,
- linearization,
- object resolution,
- document loading,
- page-tree loading, or
- damaged-document repair scanning.

These capabilities remain assigned to later milestones.

## Acceptance Criteria

BF-1038 is ready for release when:

1. the feature branch is correct,
2. all required production and test files are present,
3. the Release clean succeeds,
4. restore succeeds,
5. the full Release build succeeds,
6. all Release tests pass,
7. `PiratePDF.DocGen all` succeeds,
8. documentation validation reports zero errors,
9. generated documentation is current, and
10. `git diff --check` succeeds.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-31 | Documented the tested BF-1038 traditional cross-reference parser and release criteria. |

## Related Documents

- BF-1037 — Indirect Objects and References
- BlackFlag Engine — Parser Infrastructure RC1
- BlackFlag Engine 0.5.0 — PDF Object Model RC1

## Related BF Work Items

- BF-1036 — Collection Parser
- BF-1037 — Indirect Objects and References
- BF-1038 — Traditional Cross-Reference Tables and Trailers

## Release Tag

Planned tag: `BF-1038-RC1`
