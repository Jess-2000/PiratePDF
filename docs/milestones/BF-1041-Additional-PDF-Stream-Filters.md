# BF-1041 — Additional PDF Stream Filters

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1041 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Tested |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1041, BF-1042 |
| **Last Updated** | 2026-08-06 |
| **Next Review** | 2026-11-06 |

## Purpose

BF-1041 extends the BF-1040 filter framework with four standard PDF stream
decoders:

- `/ASCIIHexDecode`
- `/ASCII85Decode`
- `/RunLengthDecode`
- `/LZWDecode`

## Delivered Capability

### ASCIIHexDecode

- uppercase and lowercase hexadecimal digits;
- PDF whitespace handling;
- odd final nibble padding;
- strict `>` end-marker validation;
- invalid-character detection; and
- trailing-data rejection.

### ASCII85Decode

- PDF whitespace handling;
- `z` shorthand;
- partial final groups;
- strict `~>` end-marker validation;
- tuple-overflow detection;
- malformed shorthand detection; and
- trailing-data rejection.

### RunLengthDecode

- literal runs;
- repeated runs;
- end-of-data marker validation;
- incomplete-run detection; and
- trailing-data rejection.

### LZWDecode

- clear and end-of-data codes;
- 9- through 12-bit code widths;
- dictionary reset;
- KwKwK special case;
- `/EarlyChange` values 0 and 1;
- TIFF and PNG predictor reuse;
- invalid-code detection; and
- missing end-of-data detection.

## Shared Architecture

All four decoders use the BF-1040 framework and inherit:

- decoded-byte limits;
- expansion-ratio limits;
- filter-count limits;
- cancellation;
- pooled temporary buffers;
- deterministic failure classification; and
- ordered multi-filter execution.

## Integration Coverage

BF-1041 adds regression coverage for:

- ASCII85 followed by FlateDecode;
- ASCIIHex followed by RunLengthDecode;
- complete default-registry contents;
- filter-count enforcement;
- large deterministic ASCIIHex input;
- repeated maximum-size RunLength runs; and
- LZW code-width transitions.

## Test Corpus

The Filters test project contains reusable valid and invalid corpus definitions
for text and binary filters.

The corpus is intended to grow with later codecs and parser fuzzing.

## Security

The new decoders:

- execute no document behavior;
- perform no file or network I/O;
- enforce bounded output;
- reject malformed terminators and codes;
- honor cancellation; and
- preserve deterministic error classification.

## Deferred Scope

BF-1041 does not include:

- DCTDecode;
- CCITTFaxDecode;
- JBIG2Decode;
- JPXDecode;
- cross-reference stream interpretation;
- object-stream interpretation;
- encryption; or
- repair heuristics.

## Acceptance Criteria

BF-1041 is ready for release when:

1. all cumulative filter source files are present;
2. all new tests compile and are discoverable;
3. the complete Release build succeeds;
4. all Release tests pass;
5. DocGen completes;
6. documentation validation reports zero errors;
7. Git format validation succeeds;
8. no merge-conflict markers remain; and
9. release commit, merge, and tag automation completes.

## Release Tag

Planned tag: `BF-1041-RC1`

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-07 | Added missing revision history section. |
