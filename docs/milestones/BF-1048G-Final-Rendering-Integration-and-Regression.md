
# BF-1048G - Final Rendering Integration and Regression

| Field | Value |
|---|---|
| **Document ID** | BF-1048G |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1048 |
| **Last Updated** | 2026-08-10 |
| **Next Review** | 2026-11-10 |

## Objective

Complete the BF-1048 integration layer needed to finish the rendering-engine
milestone without introducing duplicate capability-report APIs or a large
public catalog of cryptic rendering error codes.

## Implemented Scope

- Bounded PDF FunctionType 0 sampled-function evaluation.
- Bounded PDF FunctionType 4 calculator-function evaluation.
- Existing FunctionType 2 and 3 evaluation unified with the same evaluator.
- Filter decoding for sampled/calculator function streams through the existing
  BlackFlag filter pipeline.
- Shared render-work budget infrastructure.
- Final render limits for pattern nesting, mesh work, transparency nesting,
  offscreen group pixels, and soft-mask pixels.
- Renderer-neutral Type 1 tiling-pattern cell planning using inverse pattern
  transforms and finite page bounds.
- Image-format recognition based on content signatures rather than file names.
- Automatic best-fit image compatibility decisions.
- Lossless-first conversion policy.
- Preservation decisions for encoded images that already fit a supported PDF
  representation.
- Vector-preserving decisions for SVG/EPS-style input rather than silent
  rasterization.
- Soft-mask selection for transparent raster sources.
- Public rendering diagnostic codes intentionally limited to
  `COLOR_CONVERSION_FAILED` and `SHADING_MALFORMED`.
- Human-readable rendering messages remain the primary failure-reporting
  mechanism.

## Image Compatibility Rules

PiratePDF shall recognize image formats from actual source data. Recognition
does not imply that a decoder exists. When a compatible decoder is available,
the compatibility layer chooses the closest supported PDF representation.

The default policy is lossless-first:

- preserve compatible encoded data when possible;
- use lossless grayscale/RGB/Indexed representations when appropriate;
- preserve alpha using a PDF soft mask;
- retain bilevel content as a bilevel representation;
- avoid unnecessary recompression of already-lossy photographs;
- retain vector sources as vector content;
- require user confirmation when the only available conversion would cause a
  meaningful and avoidable loss.

Opening an existing PDF does not transcode its image streams merely because a
fallback decoder is used for display.

## Diagnostics

BF-1048G does not add a separate renderer capability report.

The existing rendering/document-report pipeline remains authoritative. Most
failures are represented by clear human-readable messages. Only two public
rendering codes are retained:

- `COLOR_CONVERSION_FAILED`
- `SHADING_MALFORMED`

Internal typed exceptions and state may remain more specific when code needs
to distinguish behavior.

## Remaining Provider/Integration Boundaries

BF-1048 establishes the renderer architecture and bounded execution model, but
large specialized codecs and platform services remain provider boundaries
rather than copied implementations.

In particular, codec availability for JBIG2, JPX/JPEG 2000, and other
specialized formats is determined by the registered decoder providers.
Unsupported content must be reported clearly and must never be fabricated.

Full prepress overprint plate simulation remains separate from preserving PDF
overprint state. PiratePDF retains the source state needed for a future
overprint-preview implementation.

## Provenance

BF-1048G follows ADR-0014. The implementation was independently constructed
from PiratePDF contracts and applicable format semantics. No external PDF
repository implementation was copied into the project.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-10 | Added final bounded function execution, pattern tile planning, shared render-work limits, automatic best-fit image compatibility policy, simplified public diagnostics, regression tests, and final BF-1048 integration documentation. |
