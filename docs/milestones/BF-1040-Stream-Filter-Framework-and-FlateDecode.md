# BF-1040 — Stream Filter Framework and FlateDecode

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1040 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Tested |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1039, BF-1040, BF-1041 |
| **Last Updated** | 2026-08-06 |
| **Next Review** | 2026-11-06 |

## Status

Implementation and automated testing are complete. The milestone is ready for
release processing.

## Purpose

BF-1040 introduces BlackFlag Engine's reusable PDF stream-filter framework and
the first production decoder, `/FlateDecode`.

The milestone also formalizes bounded memory use, decompression safety,
passive document conversion principles, and pure filter transformations.

## Scope

BF-1040 includes:

- filter decoder, registry, pipeline, context, limits, and budget contracts;
- immutable filter registration;
- ordered multi-filter execution;
- aligned `DecodeParms`;
- decoded-byte limits;
- expansion-ratio limits;
- filter-count limits;
- cancellation handling;
- pooled temporary output buffers;
- deterministic failure classification;
- production `/FlateDecode`;
- zlib validation;
- TIFF Predictor 2 for 8- and 16-bit components;
- PNG predictors 10 through 15;
- `Predictor`, `Colors`, `BitsPerComponent`, and `Columns`;
- default filter registration;
- controlled test access to internal predictor implementation; and
- comprehensive framework, Flate, and predictor tests.

## Architecture

The framework is implemented in:

```text
src/BlackFlag.Engine.Filters
```

The principal production components are:

- `IPdfFilterDecoder`
- `IPdfFilterRegistry`
- `IPdfFilterPipeline`
- `IPdfFilterDecodeBudget`
- `PdfFilterDecodeContext`
- `PdfFilterDecodeLimits`
- `PdfFilterDecodeFailure`
- `PdfFilterDecodeException`
- `PdfFilterDecodeBudget`
- `PdfFilterRegistry`
- `PdfFilterPipeline`
- `PooledPdfFilterBufferWriter`
- `PdfFlateFilterDecoder`
- `PdfPredictorDecoder`
- `PdfFilterRegistrations`

## Pure Transformations

Filter decoders are pure transformations.

A decoder:

- receives encoded bytes;
- produces decoded bytes;
- does not parse PDF object syntax;
- does not interpret document semantics;
- does not render;
- does not cache;
- does not perform document I/O; and
- does not execute document-supplied behavior.

## Bounded Resource Use

Every decode stage receives explicit limits.

The framework enforces:

- maximum decoded bytes;
- maximum expansion ratio;
- maximum filter count;
- cancellation;
- bounded temporary buffers; and
- classified limit failures.

The pipeline never relies on garbage collection as an enforcement mechanism.
The CLR manages ordinary object lifetimes, while BlackFlag explicitly bounds
large temporary allocations and cache-like behavior.

## FlateDecode

`PdfFlateFilterDecoder` accepts PDF Flate streams using the zlib wrapper.

Raw DEFLATE is intentionally rejected because PDF Flate streams are zlib
streams, and accepting ambiguous raw data would weaken deterministic
validation.

Malformed or incomplete zlib data is reported as
`PdfFilterDecodeFailure.InvalidData`.

## Predictors

BF-1040 implements:

- Predictor 1 — no prediction;
- Predictor 2 — TIFF horizontal differencing;
- Predictors 10–14 — fixed PNG row filters; and
- Predictor 15 — optimum PNG row selection.

The implementation validates row completeness, parameter types, permitted
component widths, fixed-filter consistency, and row filter bytes.

## Security

The framework is designed to resist decompression bombs and malformed stream
data.

The decoder:

- performs no executable behavior;
- enforces decoded-byte limits;
- enforces expansion-ratio limits;
- supports cancellation;
- clears pooled buffers before return; and
- reports deterministic failure classifications.

## Test Coverage

Automated coverage includes:

- registry lookup;
- duplicate registration rejection;
- unsupported-filter classification;
- filter ordering;
- `DecodeParms` alignment;
- no-filter immutable copying;
- decoded-byte limits;
- expansion-ratio limits;
- filter-count limits;
- cancellation;
- malformed decoder data;
- text and binary zlib round trips;
- malformed zlib streams;
- TIFF Predictor 2;
- PNG None, Sub, Up, Average, Paeth, and optimum prediction;
- incomplete predictor rows;
- unsupported predictor values; and
- default registration.

## Deferred Scope

BF-1040 does not include:

- ASCIIHexDecode;
- ASCII85Decode;
- RunLengthDecode;
- LZWDecode;
- DCTDecode;
- CCITTFaxDecode;
- JBIG2Decode;
- JPXDecode;
- cross-reference stream interpretation;
- object-stream interpretation;
- encryption; or
- repair heuristics.

## Architectural Decisions

BF-1040 is governed by:

- ADR-0000 — Project Charter Governance
- ADR-0010 — Lazy, Bounded Materialization
- ADR-0011 — Passive Document Conversion
- ADR-0012 — Pure Filter Transformations

## Acceptance Criteria

BF-1040 is ready for release when:

1. the feature branch is correct;
2. all cumulative source and test files are present;
3. the Release clean succeeds;
4. restore succeeds;
5. the complete Release build succeeds;
6. all Release tests pass;
7. `PiratePDF.DocGen all` succeeds;
8. documentation validation reports zero errors;
9. generated documentation is current;
10. Git format checks succeed;
11. no merge-conflict markers remain; and
12. the release commit, merge, and tag are reproducible.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-06 | Documented the tested stream filter framework and FlateDecode implementation. |

## Release Tag

Planned tag: `BF-1040-RC1`
