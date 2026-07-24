# BlackFlag Engine Core Specification

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-011 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | In Development |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-2150 |
| **Last Updated** | 2026-07-24 |
| **Next Review** | 2026-10-24 |

---

**Status:** Draft  
**Version:** 1.0

## Purpose

`BlackFlag.Engine.Core` contains the smallest stable types used across the engine.

## Allowed contents

- Primitive value types.
- Diagnostics records.
- Error codes.
- Result types.
- Version types.
- Geometry values.
- Byte ranges.

## Prohibited contents

- File I/O.
- PDF parsing.
- Rendering.
- Avalonia types.
- PiratePDF application services.

## Initial type

`ByteRange` represents an offset and length using 64-bit values.

Required behavior:

- Reject negative offsets.
- Reject negative lengths.
- Detect end-offset overflow.
- Support containment checks.
- Support intersection checks.
- Treat the end offset as exclusive.
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized specification metadata and governance structure. |
