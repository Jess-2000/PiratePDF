# BF-1036 — Collection Parser

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1036 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1035, BF-1036 |
| **Last Updated** | 2026-07-30 |
| **Next Review** | 2026-10-30 |

## Status

Release candidate implementation complete.

## Summary

BF-1036 extends `PdfObjectParser` from primitive-object parsing to recursive PDF collection parsing while preserving the existing public API.

The parser now supports:

- Arrays
- Dictionaries
- Nested arrays
- Nested dictionaries
- Mixed recursive collection nesting
- Primitive objects inside collections
- Parser-level duplicate dictionary resolution

## Public API

The public entry point remains unchanged:

```csharp
public sealed class PdfObjectParser
{
    public PdfObject Parse();
}
```

## Architecture Decisions

- Collection parsing uses recursive descent.
- Arrays are parsed until `PdfTokenKind.ArrayEnd`.
- Dictionaries are parsed until `PdfTokenKind.DictionaryEnd`.
- Dictionary keys must parse as `PdfName` objects.
- Duplicate dictionary keys use last-value-wins behavior.
- Duplicate keys are resolved before immutable `PdfDictionary` construction.
- Existing primitive parsing behavior remains unchanged.
- The lexer, token model, token reader, and public parser API were not changed.

## Error Behavior

`PdfUnexpectedTokenException` is used when collection syntax contains:

- An invalid dictionary key
- An unterminated array
- An unterminated dictionary
- Unexpected end of file inside a collection

Unsupported but lexically valid syntax remains unread when parsing fails at the public parser entry point.

## Deferred Scope

The following remain outside BF-1036:

- Indirect references
- Indirect objects
- Streams
- Cross-reference tables
- Trailers
- Object streams
- Encryption
- Filters
- Recovery parsing

## Validation

The implementation was validated through:

- Full solution clean
- Full solution restore
- Full solution build
- Full solution test execution
- Release configuration build and test gate
- Documentation generation through `PiratePDF.DocGen all`

## Release Tag

Planned tag: `BF-1036-RC1`

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-07 | Added missing revision history section. |
