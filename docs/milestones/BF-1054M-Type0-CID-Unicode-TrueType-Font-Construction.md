# BF-1054M — Type0/CID Unicode TrueType Font Construction

| Field | Value |
|---|---|
| **Document ID** | BF-1054M |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053, BF-1054 |
| **Last Updated** | 2026-09-05 |
| **Next Review** | 2026-12-05 |

## Objective

Move external TrueType font embedding beyond BF-1054J/K's WinAnsi simple-font boundary by constructing a Unicode PDF Type0 font with a CIDFontType2 descendant.

## Unicode Subset Strategy

`PdfUnicodeTrueTypeGlyphSubsetter` accepts arbitrary Unicode scalars supported by the sanitized font's cmap.

It reuses BF-1054L's:

- sanitizer-only trusted input;
- original-GID subset strategy;
- composite dependency closure;
- generated-size limits;
- SFNT rebuilding;
- mandatory re-sanitization.

Unlike the WinAnsi subset façade, it does not require one-byte PDF codes.

## CID and GID Separation

PDF CIDs are assigned deterministically from the sorted required Unicode scalar set.

CID zero remains reserved.

The embedded TrueType subset retains original glyph IDs.

A `/CIDToGIDMap` stream bridges each PDF CID to its retained TrueType GID.

This avoids rewriting composite TrueType component references while allowing compact PDF text codes independent from font glyph numbering.

## Type0 Font Structure

M constructs:

- `/Type /Font`;
- `/Subtype /Type0`;
- `/Encoding /Identity-H`;
- `/DescendantFonts` containing one indirect CIDFontType2;
- indirect `/ToUnicode`.

The CIDFontType2 contains:

- `/CIDSystemInfo`;
- indirect `/FontDescriptor`;
- indirect `/CIDToGIDMap`;
- `/DW`;
- `/W` width entries.

The descriptor references the sanitized subset through `/FontFile2`.

## Unicode CMap Support

The shared TrueType cmap reader now supports format:

- 0;
- 4;
- 12.

Format 12 enables supplementary-plane Unicode scalar mapping.

The Type0 subset result exposes `EncodeText`, which writes each assigned CID as a two-byte big-endian Identity-H code.

## Widths

CID widths are derived from sanitized TrueType `head`, `hhea`, `hmtx`, and `maxp` metrics and scaled to 1000-unit PDF text space.

Widths are associated with PDF CIDs rather than TrueType GIDs.

## ToUnicode

M generates a two-byte `/ToUnicode` CMap:

- code space `<0000> <FFFF>`;
- one CID-to-Unicode mapping per selected scalar;
- UTF-16BE output, including surrogate pairs for supplementary-plane scalars.

This preserves copy/search/extraction semantics.

## Embedding Rights and Security

Provider rights must be `Allowed` or `SubsetOnly`.

The sanitized subset program is independently inspected for restricted `OS/2.fsType`.

Raw provider bytes remain impossible to pass to the Type0 builder.

No TrueType bytecode is executed.

Generated subsets remain re-sanitized through BF-1054I.

## Resource Projection

M allocates new objects for:

1. subset `/FontFile2`;
2. `/CIDToGIDMap`;
3. `/ToUnicode`;
4. `/FontDescriptor`;
5. CIDFontType2 descendant;
6. Type0 parent font.

The target page resource dictionary is projected to reference the Type0 font.

As in J/K, page `/Resources` and `/Resources /Font` must be direct or absent. Broader resource-scope ownership remains BF-1054N.

## Writing

Full-rewrite and incremental projection methods are added through the existing BF-1053 Writing planners.

No new serializer, xref writer, or save domain is introduced.

## Acceptance Criteria

1. Unicode subset selection accepts scalars outside WinAnsi.
2. cmap format 12 is supported.
3. Supplementary-plane Unicode can be selected.
4. PDF CIDs are deterministic and independent from TrueType GIDs.
5. CID zero remains reserved.
6. Original TrueType GIDs remain unchanged.
7. Composite dependency closure from L remains active.
8. `/CIDToGIDMap` maps each CID to the retained GID.
9. Type0 uses `/Identity-H`.
10. The descendant is `/CIDFontType2`.
11. CID widths are generated from sanitized metrics.
12. `/ToUnicode` maps two-byte CIDs to Unicode.
13. Supplementary Unicode emits UTF-16 surrogate pairs.
14. The subset result can encode required text to Identity-H CID bytes.
15. Provider/program embedding restrictions remain enforced.
16. The generated subset is re-sanitized.
17. Full rewrite supports the Type0 projection.
18. Incremental projection supports the Type0 projection.
19. No raw font path or native dependency is added.
20. The full PiratePDF release gate passes.

## Deferred Work

- CFF/CFF2 Type0 fonts;
- glyph-ID compaction;
- vertical writing modes;
- resource inheritance/indirection;
- Form XObject resource mutation;
- filtered stream mutation;
- platform font providers;
- integration of newly embedded Type0 fonts into existing-text replacement;
- public SDK exposure.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-05 | Added Unicode sanitizer-backed TrueType subsetting, format-12 cmap support, deterministic CID assignment, CIDFontType2/Identity-H construction, CIDToGIDMap, widths, and two-byte ToUnicode. |
