# BF-1054K — TrueType Glyph Subsetting, Subset-Only Rights, and ToUnicode Generation

| Field | Value |
|---|---|
| **Document ID** | BF-1054K |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053, BF-1054 |
| **Last Updated** | 2026-09-04 |
| **Next Review** | 2026-12-04 |

## Objective

Add a conservative real TrueType outline-subset path so fonts whose embedding policy requires subsetting can be used without full-embedding the original program.

BF-1054K also generates a ToUnicode CMap for the new simple WinAnsi font resource.

## Trusted Input

K accepts only `PdfSanitizedExternalFontProgram`.

The BF-1054I trust boundary remains mandatory.

The generated subset is re-sanitized before it is accepted as trusted output.

## Initial Subset Algorithm

The first subsetter supports sanitized TrueType `glyf/loca` fonts.

It retains original glyph IDs rather than renumbering them.

For the required WinAnsi Unicode set it:

1. maps Unicode through the sanitized cmap;
2. always retains glyph zero;
3. retains the selected glyph outlines;
4. removes outline bytes for unselected glyphs;
5. rebuilds `glyf`;
6. rebuilds `loca`;
7. rebuilds the cmap to contain only the selected mappings;
8. rebuilds the SFNT directory and checksums;
9. re-runs BF-1054I sanitization over the generated font.

Other required font tables remain sanitizer-produced copies.

This is a real outline reduction: unselected glyph outline programs are not embedded.

## Composite Glyph Boundary

Selected composite TrueType glyphs are rejected in K.

A correct composite subsetter must compute transitive component-glyph closure and may need to rewrite composite references if glyph IDs are renumbered.

K does not guess or silently retain the entire font to handle that case.

Composite-glyph closure is a follow-on hardening slice.

## WinAnsi Boundary

K remains aligned with J's simple `/Encoding /WinAnsiEncoding` representation.

Required text outside WinAnsi is rejected.

General Unicode external fonts require the later Type0/CID path.

## Embedding Rights

The subset projector accepts provider rights:

- `Allowed`;
- `SubsetOnly`.

`Restricted` and `Unknown` remain rejected.

The sanitized program's `OS/2.fsType` is independently inspected after subsetting.

A program that prohibits embedding is rejected regardless of provider metadata.

## ToUnicode

K allocates an indirect `/ToUnicode` stream for the new subset font.

The CMap:

- uses one-byte source codes;
- maps only the subset's selected WinAnsi codes;
- emits deterministic `bfchar` sections;
- maps to UTF-16BE Unicode values.

This preserves copy/search/extraction semantics for newly embedded subset text.

## Subset Font Naming

Subset fonts receive a deterministic six-uppercase-letter prefix followed by `+` and the normalized face name.

The prefix is derived from the sanitized subset SHA-256 identity so equivalent generated subsets produce stable names.

## Writing Projection

The subset projection allocates:

- `/FontFile2` stream;
- `/ToUnicode` stream;
- `/FontDescriptor`;
- `/Font`.

It also projects the target page resource dictionary.

Full-rewrite and incremental Writing use the existing BF-1053 projection planners.

## Preservation and Security

No raw provider bytes enter K.

No font hinting or glyph bytecode is executed.

Generated font bytes are re-sanitized.

Resource collisions and ambiguous indirect/inherited resource scopes remain rejected.

## Acceptance Criteria

1. K consumes only sanitized TrueType programs.
2. Required text is mapped through the sanitized cmap.
3. Glyph zero is always retained.
4. Selected simple-glyph outlines are retained.
5. Unselected glyph outlines are removed.
6. `loca` is rebuilt consistently.
7. cmap is rebuilt for selected mappings only.
8. Generated SFNT checksums are rebuilt.
9. Generated subset is re-sanitized.
10. Selected composite glyphs are rejected explicitly.
11. Non-WinAnsi required text is rejected.
12. `SubsetOnly` provider rights can use the subset path.
13. Restricted provider/program rights remain rejected.
14. A deterministic subset font name is generated.
15. `/ToUnicode` is generated and referenced by the new font.
16. Full rewrite supports the subset projection.
17. Incremental projection remains available through BF-1053.
18. No font bytecode is executed.
19. No external/native dependency is added.
20. The full PiratePDF release gate passes.

## Deferred Work

- composite-glyph dependency closure;
- glyph-ID renumbering/compaction;
- aggressive metric/name/table pruning;
- CFF/CFF2 subsetting;
- Type0/CID general-Unicode external fonts;
- broader ToUnicode/CMap strategies;
- inherited/indirect resources;
- Form resources;
- platform font adapters;
- existing-text integration with newly embedded resources.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-04 | Added sanitizer-backed TrueType outline reduction, subset-only rights support, deterministic subset naming, and ToUnicode generation. |
