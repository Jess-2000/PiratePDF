# BF-1054S — Existing-Text Styling, Script Positioning, Color, Compound Operators, and Decorations

| Field | Value |
|---|---|
| **Document ID** | BF-1054S |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-05 |
| **Next Review** | 2026-12-05 |

## Objective

Complete the local existing-text style semantics deferred from BF-1054F without adding a second text-editing architecture.

## Superscript and Subscript

S models superscript/subscript as derived PDF text state:

- reduced `Tf`;
- positive or negative `Ts`;
- immediate restoration of the original `Tf` and `Ts`.

Default superscript profile:

- 65% of current font size;
- rise of 35% of current font size.

Default subscript profile:

- 65% of current font size;
- drop of 20% of current font size.

Profiles are explicit and customizable.

`Normal` does not invent alternate font metrics.

## Text Color

S supports directly serializable device-space colors:

- DeviceGray;
- DeviceRGB;
- DeviceCMYK.

Fill color uses `g`, `rg`, or `k`.

Stroke color uses `G`, `RG`, or `K`.

The active pre-target color is restored after the target.

Pattern and custom resource color spaces remain unsupported for direct style rewriting because safe restoration would require resource-aware color serialization.

## Compound Quote Operators

The PDF `'` and `"` text-showing operators are normalized to their defined primitive semantics before style insertion.

Single quote becomes:

```text
T*
string Tj
```

Double quote becomes:

```text
aw Tw
ac Tc
T*
string Tj
```

This allows F/S scoped state insertion to work without being overridden unexpectedly by the compound operator.

The normalization is local to the target source span.

## Decorations

S supports explicit local:

- underline;
- strikeout.

Decoration geometry is supplied in text-space advance units.

S intentionally does not guess the text advance from glyphs.

Automatic width-derived decoration is deferred to BF-1054T, which owns width/advance measurement.

Default offsets and line thickness are derived from font size when explicit values are omitted.

Decoration drawing is isolated with `q`/`Q`.

## Filtered Streams

S uses BF-1054P decoded-space mutation and safe re-encoding.

## Scope

S supports unshared page-owned Tj/TJ/quote text.

Form styling remains governed by O's explicit shared-source/placement semantics and is not silently enabled.

## Security

S adds no persistence or network transport.

Project-wide requirements remain:

- TLS 1.2+ for protected data in transit;
- AES-256 authenticated encryption for protected PiratePDF-managed data at rest.

## Acceptance Criteria

1. Superscript is represented by scoped Tf/Ts.
2. Subscript is represented by scoped Tf/Ts.
3. Original font size and rise are restored.
4. Default script profiles are deterministic.
5. Script profiles may be customized.
6. DeviceGray fill/stroke colors are supported.
7. DeviceRGB fill/stroke colors are supported.
8. DeviceCMYK fill/stroke colors are supported.
9. Original device colors are restored.
10. Pattern/custom resource color restoration fails closed.
11. Single-quote text is normalized correctly.
12. Double-quote text is normalized correctly.
13. Existing F typography can operate on quote targets after normalization.
14. Underline is supported with explicit advance geometry.
15. Strikeout is supported with explicit advance geometry.
16. Decoration drawing is graphics-state isolated.
17. Automatic width-derived decoration is not guessed.
18. P filtered-stream policy remains active.
19. No new persistence/network path is introduced.
20. The full PiratePDF release gate passes.

## Deferred Work

- automatic text-advance measurement and width-aware local adjustment;
- whole-document orchestration and cross-run replacement;
- public Services/SDK text editing surface;
- final BF-1054 conformance and hardening;
- complex-script shaping;
- Form-specific style orchestration.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-05 | Added superscript/subscript semantics, device-space fill/stroke colors, quote-operator normalization, and explicit underline/strikeout geometry. |
