# BF-1048D â€” Image and XObject Rendering

| Field | Value |
|---|---|
| **Document ID** | BF-1048D |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1048 |
| **Last Updated** | 2026-08-09 |
| **Next Review** | 2026-11-09 |

## Objective

Add the first production image-rasterization path to BF-1048 without leaking
PDF image semantics into the Skia backend.

## Implemented Scope

- Canonical immutable decoded-image representation.
- Backend-neutral image codec contract.
- Image decode limits.
- Image XObject and inline-image convergence.
- Inline-image abbreviated parameter names.
- Raw 1, 2, 4, 8, and 16 bit sample unpacking.
- Per-row bit padding.
- DeviceGray, DeviceRGB, and DeviceCMYK sample conversion.
- `/Decode` arrays.
- Generic BlackFlag filter-pipeline reuse.
- Skia-backed `DCTDecode` JPEG codec.
- Image placement through the canonical display-item transform.
- Strict/tolerant image-decode failure behavior.
- Cancellation and pre-allocation pixel/byte limits.
- Unit coverage for packed samples, Decode inversion, RGB, CMYK, and limits.

## Deliberate Boundaries

This checkpoint does not claim silent or approximate support for image
semantics that require additional governed infrastructure. Indexed,
ICCBased, Separation, DeviceN, explicit masks, soft masks, CCITT, JBIG2, and
JPX/JPEG 2000 remain explicit subsequent BF-1048 image/color work. Unsupported
content produces a diagnostic in tolerant mode and fails in strict mode.

This is intentional: the project will not fabricate color or transparency
semantics, nor import external codec implementations merely to claim format
coverage.

## Provenance

Implementation follows ADR-0014. No external repository implementation was
used as an implementation source for this package.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-09 | Added the BF-1048D canonical image pipeline, raw sample decoding, JPEG realization, bounds, diagnostics, and regression tests. |
