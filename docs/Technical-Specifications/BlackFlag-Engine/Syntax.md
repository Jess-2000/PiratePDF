# BlackFlag Engine Syntax Specification

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-013 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | In Development |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-2100, BF-2200, BF-2300, BF-2400, BF-2500 |
| **Last Updated** | 2026-07-24 |
| **Next Review** | 2026-10-24 |

---

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
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized specification metadata and governance structure. |
