# BF-1048E — Advanced Color and Transparency

| Field | Value |
|---|---|
| **Document ID** | BF-1048E |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1048 |
| **Last Updated** | 2026-08-09 |
| **Next Review** | 2026-11-09 |

## Objective

Establish renderer-neutral advanced color conversion and standard blend-mode realization while preserving PDF source color semantics.

## Implemented Scope

- Explicit `PdfColorTarget.Srgb` default display target.
- Renderer-neutral sRGB conversion contract.
- DeviceGray, DeviceRGB, DeviceCMYK preview conversion.
- CalGray and CalRGB preview path.
- Lab-to-sRGB conversion.
- Separation and DeviceN alternate-space preview foundation with Type 2 tint transforms.
- ICCBased alternate-space fallback foundation; source ICC profile remains preserved by the resolved resource model.
- Full standard PDF blend-mode normalization and Skia realization.
- Stroking/nonstroking alpha retained in paint realization.
- Image nonstroking alpha and blend-mode realization.
- Source overprint state remains preserved in `PdfGraphicsStateSnapshot` rather than being flattened into sRGB.

## Deliberate Boundaries

This checkpoint does not claim a full ICC color-management engine, Type 0/3/4 function evaluation, overprint plate simulation, transparency-group isolation/knockout, graphics-state soft-mask groups, or the deferred CCITT/JBIG2/JPX codec implementations. Those require additional bounded execution and/or reviewed codec/color dependencies and remain explicit BF-1048 work before the overall milestone is closed.

No unsupported color space is silently redefined as sRGB in the canonical document model. sRGB is a render target only.

## Provenance

Implementation follows ADR-0014. No external repository implementation was used as an implementation source.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-09 | Added the sRGB display target, advanced preview conversion foundation, blend modes, and alpha integration. |
