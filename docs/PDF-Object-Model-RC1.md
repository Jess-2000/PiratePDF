# PDF Object Model RC1

| Field | Value |
|---|---|
| Document ID | BF-DOC-2001 |
| Category | Technical Specification |
| Version | 0.1.0 |
| Status | In Development |
| Owner | PiratePDF Project |
| Related BF Work Items | BF-2001 |
| Last Updated | 2026-07-30 |
| Next Review | 2026-08-30 |

## Purpose

This document defines the parser-independent PDF object hierarchy used by BlackFlag Engine.
Package 01 establishes the root abstraction, the stable object-kind enumeration, and the
visitor foundation.

## Scope

Package 01 includes:

- `PdfObject`
- `PdfObjectType`
- `PdfObjectVisitor<TResult>`
- Unit tests for object identity and visitor dispatch

Package 01 excludes every concrete PDF primitive, collection, and reference object.

## Architecture

The object model is independent of lexer tokens, parser state, source positions, and document
ownership. `PdfObject` exposes a concrete `Type` and generic visitor dispatch. Future concrete
objects will validate the visitor and invoke their most-specific visitor overload.

## Immutability

Production objects added to this hierarchy are immutable by default. Mutable editor state
belongs in higher-level editing or document-model abstractions.

## Compatibility Rules

- Public APIs require XML documentation.
- The object model must not reference lexer or parser projects.
- New object kinds require an explicit `PdfObjectType` member.
- Visitor overloads must preserve the base fallback path.
- Guard clauses use `if`-style validation.
- Discrete type dispatch should prefer `switch`.

## Acceptance Criteria

- The project builds for `net10.0`.
- Nullable analysis is enabled.
- All Package 01 tests pass.
- The complete repository test suite remains green.
- Documentation validation reports no errors or warnings.

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1.0 | 2026-07-30 | Established Package 01 base types and visitor foundation. |

## Related Documents

- Documentation Template RC1

## Related BF Work Items

- BF-2001 — PDF Object Model RC1
