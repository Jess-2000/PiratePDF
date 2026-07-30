# BlackFlag Engine 0.5.0 — PDF Object Model RC1

| Field | Value |
|---|---|
| Document ID | BF-DOC-0005 |
| Category | Architecture |
| Version | 0.5.0 |
| Status | In Development |
| Owner | PiratePDF Project |
| Related BF Work Items | BF-0005 |
| Last Updated | 2026-07-30 |
| Next Review | 2026-10-30 |

## Scope

This milestone introduces the parser-independent object values that future parser,
document, editing, validation, and writing layers will consume.

## Design decisions

- Objects represent data only and do not parse or serialize themselves.
- Primitive values use value equality.
- Arrays and dictionaries intentionally retain reference equality to avoid implicit,
  potentially expensive deep graph comparisons.
- `PdfNull` and both Boolean values use shared instances.
- Names and strings store immutable byte content rather than assuming a text encoding.
- Literal and hexadecimal strings remain distinct so a writer can preserve representation
  choices when appropriate.
- Arrays and dictionaries defensively copy constructor input.
- Dictionaries preserve insertion order and provide constant-time lookup.
- Null CLR references are prohibited inside PDF collections; callers use
  `PdfNull.Instance` for a PDF null value.
- Duplicate dictionary keys are rejected by the object model. A tolerant parser may apply
  a documented recovery policy before constructing the dictionary.
- Dispatch based on known object categories uses switch expressions where appropriate.

## Deliberately deferred

The following APIs are deferred until their consumers establish concrete requirements:

- Visitor interfaces
- Mutable collection builders
- Structural/deep equality helpers
- Stream objects
- Indirect object containers
- Text decoding
- Parser and writer integration

Deferring these prevents freezing speculative public APIs in RC1.

## Performance notes

- Primitive objects are compact immutable references.
- Names and strings allocate exactly one owned byte array.
- `PdfArray` stores one contiguous object-reference array.
- `PdfDictionary` stores an ordered entry array plus a name-to-index lookup table.
- Enumeration follows the arrays and does not reorder entries.
- Deep equality is never performed implicitly.

## Definition of done

- Complete RC1 object hierarchy
- XML documentation on public APIs
- Defensive validation and immutable storage
- Primitive value equality
- Ordered dictionary semantics
- Comprehensive unit tests
- Full repository build and test suite passing
- Performance and API review completed
