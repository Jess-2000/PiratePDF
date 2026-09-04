# BF-1054H — External Font Infrastructure Foundation

| Field | Value |
|---|---|
| **Document ID** | BF-1054H |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-03 |
| **Next Review** | 2026-12-03 |

## Objective

Establish a reusable, platform-neutral external-font subsystem before PiratePDF begins creating and embedding new PDF font resources.

BF-1054H does not write PDF font objects and does not depend on Skia, Windows, macOS, or any platform font API.

## Architectural Decision

External font discovery is not implemented inside existing-text Services or Rendering.

A new reusable project is introduced:

`BlackFlag.Engine.Fonts`

This subsystem can later serve:

- existing-text font changes;
- newly inserted text;
- document creation;
- form appearance generation;
- other authoring workflows that require font selection or embedding.

Rendering remains responsible for consuming resolved PDF font resources, not discovering arbitrary operating-system fonts.

## Provider Contract

`IPdfExternalFontProvider` hides platform-specific discovery and font-program access behind:

- stable provider identity;
- immutable face enumeration;
- Unicode scalar glyph-coverage checks;
- readable font-program stream access.

Future Windows and macOS adapters will implement this contract.

The core Fonts project contains no OS-specific code.

## Canonical External Face Metadata

`PdfExternalFontFace` defines:

- provider and face identity;
- family and face names;
- weight;
- stretch;
- slant;
- font-program technology;
- embedding rights.

The model is intentionally independent of Rendering's resolved-PDF-resource types because an external font program is not yet a PDF font resource.

## Deterministic Catalog and Matching

`PdfExternalFontCatalog` snapshots providers and validates:

- non-null providers;
- stable unique provider IDs;
- face/provider ownership;
- unique face identities.

Faces are returned in deterministic family/face/provider order.

Matching requires an explicit family and scores eligible faces by:

1. exact requested slant;
2. absolute weight distance;
3. absolute stretch distance;
4. deterministic face/provider identity ordering.

This first matching policy is deliberately simple and deterministic. More advanced substitution/fallback policy requires explicit later design.

## Glyph Coverage

Matching can require arbitrary Unicode text.

The catalog enumerates Unicode scalars with `Rune` rather than UTF-16 code units and requires the provider to confirm glyph support for every distinct scalar.

A face with incomplete required coverage is excluded before any PDF resource is created.

## Conservative Embedding Policy

`PdfExternalFontEmbeddingPolicy` permits external font embedding only when rights are:

- `Allowed`; or
- `SubsetOnly`.

`Restricted` is rejected.

`Unknown` is also rejected for embedding by default.

Callers may enumerate/match unknown-rights fonts for non-embedding purposes only by explicitly disabling the embeddable requirement.

This prevents later Writing code from silently embedding a font whose permission state has not been established.

## Font Program Ownership

A matched face can open its font-program stream through its owning provider.

The caller owns the returned stream.

BF-1054H does not parse, subset, transform, cache, or embed that program.

## Why PDF Resource Writing Is Deferred

The current repository can read existing PDF font dictionaries but lacks the complete writer-side infrastructure for safely creating arbitrary new fonts.

The next font-writing slice must explicitly design:

- TrueType/OpenType table parsing required for embedding;
- font embedding-rights extraction from the actual program;
- glyph selection/subsetting;
- widths;
- font descriptors;
- `/FontFile2` or `/FontFile3`;
- PDF font subtype selection;
- encoding strategy;
- ToUnicode generation;
- resource naming;
- indirect-object allocation;
- page/Form resource-dictionary projection;
- incremental and full-rewrite object projections;
- PDF 1.7 and PDF 2.0 validation.

BF-1054H supplies the reusable source/matching boundary that work can trust.

## Acceptance Criteria

1. A platform-neutral `BlackFlag.Engine.Fonts` project exists.
2. The Fonts project has no Rendering, Services, Skia, OS, or native dependency.
3. External font providers have stable identities.
4. External faces have stable provider-scoped identities and canonical metadata.
5. Provider collections are defensively snapshotted.
6. Duplicate provider IDs are rejected.
7. Face/provider identity mismatches are rejected.
8. Catalog enumeration is deterministic.
9. Matching is deterministic by slant, weight, stretch, and identity.
10. Required Unicode coverage is checked by scalar (`Rune`).
11. Restricted fonts are excluded from embeddable matches.
12. Unknown embedding rights are excluded from embeddable matches by default.
13. Subset-only rights remain eligible for future subset embedding.
14. Matched font programs can be opened through their owning provider.
15. No PDF font/resource object is created.
16. No system-font platform adapter is introduced yet.
17. No external dependency is added.
18. The full PiratePDF release gate passes.

## Follow-On Direction

The immediate follow-on work should be a dedicated PDF external-font resource/embedding slice under BF-1054, followed by Windows/macOS provider adapters and integration with the existing-text font service.

The earlier decoration/shared-Form/SDK/closure slice lettering may shift; slice letters are implementation sequencing, not separate BF work-item identities.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-03 | Added platform-neutral external-font provider, catalog, matching, glyph-coverage, and embedding-policy foundation. |
