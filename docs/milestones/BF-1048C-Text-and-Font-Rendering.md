# BF-1048C — Text and Font Rendering

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1048C |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1046, BF-1047, BF-1048 |
| **Last Updated** | 2026-08-09 |
| **Next Review** | 2026-11-09 |

## Purpose

BF-1048C adds renderer-neutral PDF font decoding and text rasterization to the
BF-1048 rendering engine while preserving source character codes, CIDs, glyph
identity, Unicode mappings, metrics, and positioning for future editing.

## Implemented Scope

- Variable-length PDF character-code model.
- CMap codespaces, `cidchar`, `cidrange`, `bfchar`, and `bfrange`.
- UTF-16BE ToUnicode destinations including surrogate pairs.
- Identity-H and Identity-V Type 0 decoding.
- Stream-based custom Type 0 CMaps.
- Simple-font WinAnsi decoding.
- `/Encoding` dictionaries and `/Differences` glyph names.
- Common list-marker and symbol glyph-name mappings.
- Simple-font `/Widths`, `/FirstChar`, and `/MissingWidth`.
- CID `/DW` and `/W` widths.
- CIDToGIDMap stream and Identity handling.
- Embedded font-program decoding through the BlackFlag filter pipeline.
- Skia embedded-font realization with explicit system-font fallback diagnostics.
- Text rendering modes 0 through 7.
- ET-time accumulated glyph-outline clipping through generalized immutable `PdfClipState` nodes.
- Source-code/CID/glyph/Unicode relationship preservation.
- Cancellation and existing BF-1048 render diagnostics.

## List-Marker Fidelity

Bullets, open bullets, squares, arrows, hyphens, en/em dashes, letters,
numbers, and Roman-numeral text use the same canonical font/glyph path as all
other PDF text. The renderer does not assign list semantics; later editing can
reconstruct list structure without BF-1048 flattening markers into a special
UI-only representation.

## Deliberate Boundaries

Named predefined CMaps other than Identity-H/Identity-V require a future
registered predefined-CMap data provider. BF-1048C does not copy predefined
CMap tables from external PDF repositories.

Type 3 CharProc execution remains tied to the graphics interpretation
pipeline and requires a dedicated integration package before BF-1048 is
declared complete. New/edited text shaping is also downstream work; existing
PDF text is not arbitrarily reshaped.

## Provenance

Implementation follows ADR-0014. CMap parsing and encoding logic is
independently implemented from PDF semantics and official API documentation.
No external PDF/font repository implementation is incorporated.

## Validation

Run the repository release gate after application:

```powershell
.\Invoke-PiratePDFReleaseGate.ps1;
```

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-09 | Added BF-1048C text and font rendering. |
