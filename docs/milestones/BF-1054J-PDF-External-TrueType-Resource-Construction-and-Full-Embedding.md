# BF-1054J — PDF External TrueType Resource Construction and Full Embedding

| Field | Value |
|---|---|
| **Document ID** | BF-1054J |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053, BF-1054 |
| **Last Updated** | 2026-09-04 |
| **Next Review** | 2026-12-04 |

## Objective

Construct and preservation-project a valid simple TrueType PDF font resource from **sanitized** external font bytes.

BF-1054J is the first writer-side external-font embedding slice. It does not permit raw provider bytes.

## Trusted Input Rule

The embedding API accepts `PdfSanitizedExternalFontProgram`.

There is no byte-array, file-path, or raw provider-stream overload.

This preserves BF-1054I's mandatory trust boundary.

## Font-Program Inspection

`PdfExternalTrueTypeInspector` reads only the metrics needed for conservative PDF font construction:

- units per em;
- bounding box;
- ascent/descent;
- glyph count;
- horizontal advances;
- Unicode cmap mapping;
- `OS/2.fsType` embedding rights.

The inspector operates only on sanitizer-produced bytes.

## Independent Embedding-Rights Check

Provider metadata alone does not authorize embedding.

BF-1054J requires:

1. the selected `PdfExternalFontFace` to report `Allowed`; and
2. the sanitized font program's `OS/2.fsType` to permit full embedding.

Restricted program rights reject the projection even when provider metadata says Allowed.

`SubsetOnly` provider permission is not full-embedded in J. A real subsetter is required before that policy can be supported safely.

## Initial PDF Font Representation

BF-1054J constructs a conservative simple TrueType font:

- `/Type /Font`;
- `/Subtype /TrueType`;
- `/Encoding /WinAnsiEncoding`;
- `/FirstChar 32`;
- `/LastChar 255`;
- `/Widths` generated from sanitized cmap/hmtx data;
- indirect `/FontDescriptor`;
- indirect `/FontFile2` containing the sanitized font program.

The full sanitized font program is embedded; it is not subsetted.

The initial J representation is therefore aimed at WinAnsi-representable text. General Unicode/CID font construction remains later work.

## Metrics and Widths

Widths are derived from sanitized `head`, `hhea`, `hmtx`, `maxp`, and `cmap` data and scaled to 1000-unit PDF text space.

Supported cmap forms for writer metrics are 0, 4, and 12.

Undefined WinAnsi characters safely fall back to glyph zero width rather than guessing another glyph.

## Resource Projection

J uses `PdfWriteObjectAllocator` and explicit `PdfObjectWriteProjectionSet` objects.

It reserves new identifiers for:

- the embedded font-program stream;
- the font descriptor;
- the font dictionary.

The target page dictionary is projected to add the chosen `/Font` resource reference.

No historical object-number gap is reused.

## Resource-Scope Boundary

This first embedding slice supports an existing indirect page whose `/Resources` and `/Resources /Font` dictionaries are direct or absent.

Indirect/inherited resource dictionaries are rejected rather than rewritten ambiguously.

Form XObject resource projection remains later work.

## Writing Integration

`PdfExternalFontEmbeddingWritingService` connects the projection to:

- `PdfProjectedFullRewritePlanner`;
- `PdfProjectedIncrementalWritePlanner`.

Both PDF 1.7 and PDF 2.0 continue to use the established BF-1053 version-preserving Writing architecture.

No separate serializer or xref writer is introduced.

## Preservation

The projection changes only:

- the target page resource dictionary;
- the newly allocated font dictionary;
- the newly allocated font descriptor;
- the newly allocated font-program stream.

Unrelated document objects remain untargeted.

## Security

J does not weaken I.

Downstream font construction cannot accept unsanitized bytes.

Writer parsing is intentionally bounded to sanitizer-produced tables and does not execute TrueType hinting programs.

The font program is embedded as inert stream data; PiratePDF does not execute glyph bytecode during J.

## Acceptance Criteria

1. J accepts only `PdfSanitizedExternalFontProgram`.
2. Only sanitized TrueType-outline programs are accepted.
3. Provider embedding rights must permit full embedding.
4. `OS/2.fsType` is independently inspected from the sanitized program.
5. Restricted font programs are rejected even if provider metadata says Allowed.
6. `SubsetOnly` provider permission is rejected for full embedding.
7. Font widths are generated from trusted cmap/hmtx metrics.
8. Font descriptor metrics are generated from trusted font tables.
9. Sanitized bytes are embedded through `/FontFile2`.
10. New indirect IDs use `PdfWriteObjectAllocator`.
11. The page `/Font` resource receives an indirect reference to the new font.
12. Existing resource-name collisions are rejected.
13. Indirect/inherited resource scopes are rejected rather than guessed.
14. Full-rewrite planning supports the font projection.
15. Incremental planning supports the font projection.
16. No raw external-font overload is added.
17. No font bytecode is executed.
18. No external/native dependency is added.
19. The full PiratePDF release gate passes.

## Deferred Work

The following remain after J:

- true glyph subsetting;
- `SubsetOnly` embedding;
- CFF/CFF2 PDF resource construction;
- CID/Type0 general-Unicode font construction;
- ToUnicode generation for new fonts;
- automatic collision-free resource-name selection;
- indirect/inherited page resource projection;
- Form XObject resource projection;
- Windows/macOS font-provider adapters;
- integration of newly embedded resources with the existing-text font-switch API.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-04 | Added sanitized TrueType metrics inspection, PDF font/descriptor/FontFile2 construction, resource projection, and BF-1053 Writing integration. |
