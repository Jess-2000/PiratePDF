# BF-1045 — Content Streams and Canonical Graphics Operators

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1045 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1041, BF-1044, BF-1045 |
| **Last Updated** | 2026-08-06 |
| **Next Review** | 2026-11-06 |

## Purpose

BF-1045 translates decoded PDF content streams into immutable canonical graphics
operations. Parsing remains independent from resource lookup, graphics-state
interpretation, display-list construction, and rasterization.

## Delivered Capability

BF-1045 includes:

- immutable graphics operators, operations, operands, and source spans;
- standard PDF graphics, path, text, color, XObject, marked-content, shading,
  and Type 3 operator recognition;
- exact operator arity and basic operand-type validation;
- strict and tolerant unknown-operator behavior;
- lazy enumeration and bounded materialization;
- multiple decoded content streams in source order;
- graphics-state save and restore balance validation;
- inline-image dictionary and encoded-byte preservation; and
- configurable operation, operand, stack, dictionary, and image-byte limits.

## Canonical Graphics Language

The renderer-independent graphics model is owned by
`BlackFlag.Engine.Rendering.Graphics`. PDF-specific syntax recognition is owned
by `BlackFlag.Engine.Parsing.ContentStreams`. Downstream consumers use canonical
operations instead of depending on lexer tokens or parser implementation details.

## Inline Images

Inline images are represented as a `BI` graphics operation with an immutable
parameter dictionary and encoded image bytes. BF-1045 does not decode pixels or
resolve color spaces. Those responsibilities remain assigned to resource and
image-processing packages.

## Defensive Parsing

The parser rejects malformed operands, unsupported operators in strict mode,
invalid operator arity, invalid basic operand categories, unbalanced graphics
state, unterminated inline images, trailing operands, and configured limit
violations. Source spans retain the decoded stream index and byte offsets for
future diagnostics and editing.

## Design Boundaries

BF-1045 does not resolve fonts, images, XObjects, color spaces, ICC profiles,
patterns, shadings, or ExtGState dictionaries. It does not interpret the current
transformation matrix, construct display lists, render pixels, or modify page
content. Those capabilities remain assigned to BF-1046 through BF-1049.

## Validation

The release gate requires:

- a Release solution build;
- all automated tests;
- documentation index generation;
- documentation validation with zero errors and warnings; and
- a clean `git diff --check` result.

## Revision History

- 2026-08-06: Initial BF-1045 implementation.
