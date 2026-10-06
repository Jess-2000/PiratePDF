# BF-1054I — External Font Sanitization and Trusted Font-Program Boundary

| Field | Value |
|---|---|
| **Document ID** | BF-1054I |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-04 |
| **Next Review** | 2026-12-04 |

## Objective

Make external `.ttf` and `.otf` data cross a mandatory fail-closed sanitization boundary before later font parsing, subsetting, embedding, rendering, or PDF Writing consumes the font program.

The sanitizer reduces attack surface and rejects malformed or unsupported font structures. It does not claim that any finite validator can prove a font incapable of triggering every possible implementation vulnerability in downstream third-party software.

## Trust Boundary

Provider font bytes are untrusted.

`PdfExternalFontMatch` no longer exposes a public raw-font stream.

Its public font-program path requires an `IPdfExternalFontSanitizer` and returns `PdfSanitizedExternalFontProgram`.

Raw provider access remains internal to the Fonts assembly so future BlackFlag Engine font-writing code can be required to accept only the sanitized type.

## Accepted Containers

The initial sanitizer accepts standalone SFNT:

- TrueType scalar `0x00010000`;
- OpenType/CFF `OTTO`.

It rejects:

- TrueType Collections (`ttcf`);
- WOFF;
- WOFF2;
- unknown SFNT/container signatures.

File extensions are not trusted.

## Bounded Input

Before structural parsing, input is read through an explicit maximum-byte limit.

Additional limits bound:

- table count;
- single-table size;
- glyph count;
- cmap subtable count;
- name-record count.

Cancellation is checked throughout bounded reads and table validation.

## SFNT Validation

The sanitizer validates:

- SFNT header length;
- searchRange/entrySelector/rangeShift consistency;
- printable unique table tags;
- four-byte-aligned table offsets;
- table ranges within the input;
- table-size limits;
- non-overlapping table ranges;
- table checksums.

## Required Table Validation

All accepted fonts require:

- `head`;
- `maxp`;
- `cmap`;
- `name`;
- `hhea`;
- `hmtx`.

The sanitizer validates the `head` magic and loca format, maxp glyph count, hhea metric count, hmtx required length, cmap record/subtable bounds, and name-record/string bounds.

Optional `OS/2` is rejected when shorter than the supported minimum structure.

TrueType outlines additionally require `loca` and `glyf`; loca offsets must be monotonic and remain inside glyf.

OTTO fonts require `CFF ` or `CFF2` and a structurally valid initial CFF header.

Unsupported cmap formats are rejected rather than passed through unexamined.

## Canonical Reserialization

Successful input is not returned verbatim.

Tables are copied into a newly allocated compact SFNT with:

- deterministic table ordering;
- canonical aligned offsets;
- regenerated directory entries;
- regenerated table checksums;
- regenerated `head.checkSumAdjustment`.

The immutable result owns a defensive byte copy and a SHA-256 identity.

Downstream code therefore consumes sanitizer-produced bytes, not provider-owned storage.

## Failure Semantics

Sanitization failures are typed through `PdfExternalFontSanitizationFailure`.

Malformed input fails before any PDF document mutation.

No rejected font is written into a document, resource dictionary, renderer, or font embedder.

## Security Scope

BF-1054I is a structural managed-code sanitizer and trusted-byte boundary.

The later embedding/subsetting slice must continue to:

- parse only `PdfSanitizedExternalFontProgram`;
- maintain independent parser limits;
- avoid unsafe/native font parsing before sanitization;
- fuzz the sanitizer and future subsetter;
- preserve the fail-closed policy.

Platform adapters must not bypass this boundary.

## Acceptance Criteria

1. Raw provider font programs are not publicly returned by `PdfExternalFontMatch`.
2. Public matched-font consumption requires a sanitizer.
3. Sanitized output is immutable and defensively copied.
4. Input byte limits are enforced before unbounded allocation.
5. Table-count and table-size limits are enforced.
6. TTC/WOFF/WOFF2 and unknown containers are rejected.
7. SFNT directory fields are validated.
8. Duplicate tables are rejected.
9. Invalid, unaligned, out-of-range, and overlapping tables are rejected.
10. Table checksums are validated.
11. Core required tables are structurally validated.
12. TrueType loca/glyf consistency is validated.
13. OTTO requires a CFF/CFF2 outline table.
14. Unsupported cmap formats fail closed.
15. Successful programs are canonically reserialized.
16. Canonical output receives a SHA-256 identity.
17. Sanitization does not mutate a PDF document.
18. No native or external dependency is added.
19. The full PiratePDF release gate passes.

## Deferred Work

Deep TrueType glyph-program validation, complete CFF/CFF2 semantic validation, subsetting, widths/descriptors, ToUnicode generation, PDF font-object creation, platform font-provider adapters, and PDF resource projection remain later BF-1054 work.

Those consumers must accept only sanitized programs.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-04 | Added bounded TTF/OTF structural sanitization, canonical reserialization, typed rejection, and the mandatory trusted font-program boundary. |
