# BF-1054P — Filtered Content-Stream Mutation and Safe Re-encoding

| Field | Value |
|---|---|
| **Document ID** | BF-1054P |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-05 |
| **Next Review** | 2026-12-05 |

## Objective

Remove BF-1054's blanket filtered-stream rejection for the common rewrite-safe case without silently changing unsupported filter semantics.

## Decoded Provenance Rule

Existing-text source offsets are offsets in canonical decoded content.

P therefore requires all token mutation to occur on decoded bytes.

Encoded `PdfStream` bytes must never be passed directly to provenance-based token editors.

## Preservation-Safe Filter Policy

P supports two stream representations.

### Unfiltered

Unfiltered streams remain unfiltered.

Decoded and encoded mutation bytes are identical.

### Single FlateDecode

A single `/FlateDecode` or `/Fl` stream is decoded with zlib, edited in decoded space, and re-encoded with zlib.

The existing stream dictionary is preserved.

`/Filter` is not removed or normalized.

`/DecodeParms` remains unchanged.

Flate `/DecodeParms` is rewrite-safe only when:

- absent;
- null;
- a single dictionary with no `/Predictor`;
- a single dictionary with `/Predictor 1`.

## Explicitly Unsupported Re-encoding

P rejects rather than normalizes:

- multi-filter chains;
- ASCIIHex;
- ASCII85;
- RunLength;
- LZW;
- Flate predictors other than 1.

The existing read pipeline may decode more formats, but mutation support is deliberately narrower because a decoder alone does not prove representation-preserving re-encoding.

## Shared Codec

`PdfContentStreamMutationCodec` centralizes the policy.

The following BF-1054 paths now use it:

- direct same-font replacement;
- positioned TJ replacement;
- typography state editing;
- existing-resource font switching;
- Form placement cloning;
- explicit shared Form stream mutation.

This prevents drift between editing services.

## Form Placement

Placement-only Form cloning now supports unfiltered or preservation-safe single-Flate:

- source Form data is encoded according to the original Form representation;
- page content is decoded before the selected `Do` operation is redirected;
- rewritten page content is re-encoded according to the original page stream representation.

The original Form and unrelated placements remain unchanged.

## Security and Resource Bounds

P performs in-memory zlib transformation only.

No temporary plaintext file is introduced.

Cancellation is observed during chunked decode/encode.

The project-wide data-protection policy remains:

- TLS 1.2 or higher for protected data in transit;
- AES-256 authenticated encryption for protected PiratePDF-managed data persisted at rest.

P does not introduce persistence or network transport.

## Acceptance Criteria

1. Provenance offsets are applied only to decoded bytes.
2. Unfiltered mutation remains supported.
3. Single `/FlateDecode` mutation is supported.
4. `/Fl` abbreviation is accepted.
5. Flate Predictor 1 is supported.
6. Missing/null decode parameters are supported.
7. Existing filter dictionary representation remains unchanged.
8. Edited bytes are re-compressed with zlib.
9. Multi-filter mutation is rejected.
10. Non-Flate filtered mutation is rejected.
11. Predictive Flate mutation is rejected.
12. Direct replacement uses the shared codec.
13. Positioned replacement uses the shared codec.
14. Typography editing uses the shared codec.
15. Existing-resource font switching uses the shared codec.
16. Form placement cloning uses the shared codec.
17. Explicit shared-Form mutation uses the shared codec.
18. No plaintext temporary file is created.
19. Unsupported filter semantics never silently normalize.
20. The full PiratePDF release gate passes.

## Deferred Work

- additional symmetric filter encoders if later justified;
- external/newly embedded font integration into existing text;
- Windows/macOS font adapters;
- superscript/subscript, color, compound operators, and decorations;
- whole-document orchestration and width-aware local adjustment;
- public SDK exposure;
- final BF-1054 conformance and hardening.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-05 | Added decoded-space mutation and preservation-safe single-Flate re-encoding across existing-text and Form editing paths. |
