# BF-1043 — Document Loading and Indirect Object Resolution

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1043 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1038, BF-1042, BF-1043 |
| **Last Updated** | 2026-08-06 |
| **Next Review** | 2026-11-06 |

## Purpose

BF-1043 connects the cross-reference and object-stream parsing delivered by
BF-1042 to a document-level indirect-object resolver. Callers can request an
object by object number and generation without needing to know whether the
object is stored directly in the file or inside an object stream.

## Delivered Capability

BF-1043 includes:

- immutable object-location descriptors for free, direct, and compressed
  objects;
- newest-revision-wins object-location lookup by object number;
- exact generation-number validation during lookup;
- direct indirect-object parsing from absolute random-access offsets;
- transparent object-stream decoding and compressed-object retrieval;
- lazy, thread-safe object caching;
- circular-resolution protection;
- structured failure categories for missing, free, malformed, mismatched, and
  cyclic objects; and
- unit coverage for revision replacement, direct loading, cache reuse,
  identifier mismatch, free entries, and compressed objects.

## Architecture

The implementation is contained in:

```text
src/BlackFlag.Engine.Parsing/Loading
```

The principal production types are:

- `PdfObjectLocationKind`;
- `PdfObjectLocation`;
- `PdfObjectLocationTable`;
- `IPdfIndirectObjectResolver`;
- `PdfIndirectObjectResolver`;
- `PdfObjectResolutionFailure`; and
- `PdfObjectResolutionException`.

## Resolution Flow

For each requested `PdfObjectId`, the resolver:

1. checks for an active circular resolution;
2. obtains or creates the cached lazy resolution operation;
3. finds the effective newest-revision location;
4. rejects missing, free, or generation-mismatched entries;
5. parses direct objects at their absolute random-access offset; or
6. resolves and decodes the containing object stream before retrieving a
   compressed object by index.

## Design Boundaries

BF-1043 resolves immutable objects only. It does not construct the page tree,
perform document editing, repair damaged cross-reference data, or save updated
PDF files. Those responsibilities remain assigned to later packages.

## Performance and Safety

The resolver performs position-independent reads through
`IRandomAccessSource`, parses objects only when first requested, and reuses the
resolved immutable value thereafter. Thread-safe lazy values prevent duplicate
loads under concurrency. Failed values are removed from the cache so a caller
is not permanently bound to a faulted cache entry.

Object streams are reached through the same resolver, preserving lazy loading
across direct and compressed storage. Circular object-stream dependencies are
rejected deterministically.

## Validation

The release gate requires:

- a Release solution build;
- all automated tests;
- documentation index generation;
- documentation validation with zero errors and warnings; and
- a clean `git diff --check` result.

## Revision History

- 2026-08-06: Initial BF-1043 implementation.
