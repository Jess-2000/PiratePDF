# BF-1054T — Text Advance Measurement and Width-Aware Existing-Text Adjustment

| Field | Value |
|---|---|
| **Document ID** | BF-1054T |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-05 |
| **Next Review** | 2026-12-05 |

## Objective

Add an editing-facing text-advance measurement layer so PiratePDF can reason about geometry changes instead of guessing from character counts or font size.

T remains a local geometry-preservation milestone, not a paragraph/page reflow engine.

## Renderer-Aligned Measurement

`PdfTextAdvanceMeasurementService` measures text with `PdfFontDecoder`, using the same resolved PDF font width data already used by renderer-neutral decoding.

Measurement includes:

- simple-font `/Widths`;
- descriptor `/MissingWidth`;
- Type0/CID `/W`;
- Type0/CID `/DW`;
- character spacing;
- word spacing where applicable;
- `TJ` numeric adjustments;
- font size;
- horizontal scaling.

The result is the horizontal user-space advance produced by the current PDF text state.

## Comparison Contract

`PdfTextAdvanceMeasurement` reports:

- original advance;
- replacement advance;
- signed delta;
- absolute delta;
- fit classification.

Fit is:

- `Exact`;
- `LocallyAdjustable`;
- `RequiresReflow`.

A configurable tolerance determines exactness.

## Local Compensation

For a directly editable primitive `Tj` run, `PdfWidthAwareTextReplacementService` can preserve the original final text-matrix position.

When replacement text has a different advance, T converts the target to a `TJ` array and appends a numeric adjustment whose translation cancels the measured delta.

This preserves the following text position without changing glyph scale or unrelated page content.

T does not use horizontal scaling as a visual compression workaround.

## Reflow Boundary

If a target cannot safely receive local compensation, T reports/rejects the preserve-advance operation as requiring reflow.

T does not:

- move unrelated page objects;
- resize text boxes;
- wrap paragraphs;
- synthesize line breaks;
- stretch glyphs to force a fit.

That remains outside BF-1054's direct existing-text mutation model.

## Automatic Decorations

BF-1054S required explicit decoration advance geometry.

T adds `PdfExistingTextDecoration.Automatic`.

When selected, S now resolves the decoration width from the measured original text advance.

Underline and strikeout therefore no longer require callers to estimate width.

Explicit decoration widths remain supported.

## Existing Positioned Text

The measurement service accepts ordered text/adjustment elements and preserves existing `TJ` adjustments during measurement.

T does not rewrite arbitrary multi-element `TJ` arrays merely to hide a width delta.

Those runs can still be measured and classified, and existing BF-1054D position-preserving replacement remains available.

## Filtered Streams

Width-aware replacement uses BF-1054P decoded-space mutation and safe re-encoding.

## Security and Data Protection

T introduces no persistence or network transport.

The project-wide invariant remains:

- TLS 1.2 or higher for protected data in transit;
- AES-256 authenticated encryption for protected PiratePDF-managed data persisted at rest.

## Acceptance Criteria

1. Original text advance can be measured.
2. Replacement advance can be measured.
3. Simple-font explicit widths are honored.
4. Missing widths are honored.
5. CID `/W` widths are honored.
6. CID `/DW` is honored.
7. character spacing is included.
8. word spacing is included for simple fonts.
9. `TJ` adjustments are included.
10. horizontal scaling is applied.
11. measurement reports signed delta.
12. exact replacements are classified.
13. locally compensable replacements are classified.
14. non-local cases report reflow requirement.
15. primitive `Tj` can receive a trailing `TJ` compensation.
16. local compensation preserves subsequent text position.
17. glyph horizontal scaling is not abused as a fit mechanism.
18. automatic underline/strikeout width uses measured advance.
19. no full paragraph/page reflow is introduced.
20. the full PiratePDF release gate passes.

## Deferred Work

- paragraph/line reflow;
- automatic cross-object layout changes;
- dedicated complex-script shaping;
- public Services/SDK text-editing exposure;
- final BF-1054 end-to-end conformance and hardening.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-05 | Added renderer-aligned advance measurement, comparison/fit classification, primitive Tj local compensation, and automatic decoration width measurement. |
