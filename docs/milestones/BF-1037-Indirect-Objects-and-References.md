# BF-1037 — Indirect Objects and References

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1037 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Tested |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1035, BF-1036, BF-1037 |
| **Last Updated** | 2026-07-31 |
| **Next Review** | 2026-10-31 |

## Status

Implementation and automated testing are complete. The milestone is awaiting
final release processing.

## Summary

BF-1037 extends `PdfObjectParser` with support for PDF indirect references and
document-level indirect-object declarations while preserving the existing
public parser API.

The parser now supports:

- Indirect references in the form `<object-number> <generation-number> R`
- Indirect references inside arrays and dictionaries
- Indirect-object declarations in the form
  `<object-number> <generation-number> obj <value> endobj`
- Primitive and recursively nested collection values inside indirect objects
- Object-number and generation-number validation
- Existing parser error and token-consumption behavior

## Public API

The public object-parsing entry point remains unchanged:

```csharp
public sealed class PdfObjectParser
{
    public PdfObject Parse();
}
```

Indirect-object declaration parsing is internal and reserved for later
document-loading infrastructure.

## Architecture Decisions

- Indirect references are first-class immutable `PdfObject` values.
- The existing `PdfIndirectReference` object model is reused.
- Indirect-object declarations are document-level containers rather than
  `PdfObject` values.
- The existing `PdfIndirectObject` model is reused.
- No new `PdfObjectType` value is introduced for indirect objects.
- Reference recognition uses the token reader's existing lookahead support.
- An integer is parsed as the start of a reference only when followed by:
  - another integer, and
  - `PdfTokenKind.IndirectReferenceMarker`
- Integer sequences without `R` remain independently parseable integer
  objects.
- Existing recursive parsing is reused for indirect-object values.
- Existing lexer, token model, token reader, and parser public API remain
  unchanged.

## Numeric Constraints

The parser enforces the model's supported ranges:

- Object number: `1` through `Int32.MaxValue`
- Generation number: `0` through `65535`

Out-of-range values produce `FormatException` without consuming the first
header token.

## Error Behavior

`PdfUnexpectedTokenException` is used when:

- An indirect-object header does not begin with an integer object number
- The generation number is missing or is not an integer
- The `obj` marker is missing
- The indirect-object value is missing
- The terminating `endobj` marker is missing
- End of file is reached where a required structural token is expected

`FormatException` is used when:

- An object number is outside the supported range
- A generation number is outside the supported range
- A numeric token cannot be represented by the existing numeric parser

Header validation occurs before header consumption. After a valid header has
been consumed, body-level failures leave the reader at the offending body or
terminator token, consistent with existing parser behavior.

## Test Coverage

BF-1037 includes tests for:

- Valid indirect references
- Maximum supported object and generation numbers
- Exact one-reference consumption
- References inside arrays
- References inside dictionaries
- Invalid object numbers
- Invalid generation numbers
- Integer sequences that are not references
- Valid indirect objects with primitive values
- Valid indirect objects with recursive collection values
- Exact one-declaration consumption
- Missing object number
- Missing generation number
- Missing `obj`
- Missing value
- Missing `endobj`
- Header validation without partial consumption

## DocGen Revision

BF-1037 centralizes canonical documentation status values in
`DocumentStatuses`.

`DocumentStatuses` provides:

- canonical status constants,
- the complete canonical collection through `All`,
- enumeration through `GetAll()`, and
- validation through `IsValid(string?)`.

`DocumentationValidator` uses this shared API rather than maintaining a private
status whitelist. Theory-driven tests verify that every canonical status is
accepted and that noncanonical values are rejected.

## Deferred Scope

The following remain outside BF-1037:

- Stream parsing
- Cross-reference tables
- Cross-reference streams
- Trailer parsing
- Object resolution
- Lazy loading
- Classic document loading
- Incremental updates
- Encryption
- Object streams
- Recovery parsing

## Validation

The release candidate must pass:

1. Branch verification
2. Required-file verification
3. Clean
4. Restore
5. Release build
6. Release test execution
7. Documentation generation through `PiratePDF.DocGen all`
8. Generated-document validation
9. Final Git format validation
10. Release-package readiness confirmation

## Release Tag

Planned tag: `BF-1037-RC1`

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-07 | Added missing revision history section. |
