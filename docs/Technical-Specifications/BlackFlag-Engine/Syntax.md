# BlackFlag Engine Syntax Specification

**Status:** Draft  
**Version:** 1.0

## Purpose

`BlackFlag.Engine.Syntax` converts bytes into low-level PDF syntax objects.

## Planned layers

1. Byte reader.
2. Whitespace and delimiter handling.
3. Tokenizer.
4. Primitive object parser.
5. Indirect object parser.
6. Cross-reference parser.
7. Stream-filter pipeline.

## Initial token categories

- Integer.
- Real.
- Boolean.
- Null.
- Name.
- Literal string.
- Hex string.
- Array start and end.
- Dictionary start and end.
- Object and end-object keywords.
- Stream and end-stream keywords.
- Reference marker.
- Other keywords.

## Safety requirements

- Bound token lengths.
- Detect malformed escapes.
- Detect numeric overflow.
- Preserve source offsets.
- Produce structured diagnostics.
