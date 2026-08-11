
# BF-1048 Rendering Capability Matrix

| Field | Value |
|---|---|
| **Document ID** | BF-1048-MATRIX |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1048 |
| **Last Updated** | 2026-08-10 |
| **Next Review** | 2026-11-10 |

## Purpose

This document records BF-1048 support directly. It is documentation, not a
runtime renderer-capability report.

## Matrix

| Construct | BF-1048 status |
|---|---|
| Paths and path painting | Implemented |
| Path clipping | Implemented |
| Graphics-state save/restore | Implemented |
| Form XObjects | Implemented |
| Text and glyph rendering | Implemented |
| Type 3 nested CharProc architecture | Implemented |
| Image XObjects and inline images | Implemented foundation |
| DCT/JPEG image decoding | Implemented |
| Raw packed image samples | Implemented |
| Automatic best-fit external image compatibility decisions | Implemented |
| sRGB screen target | Implemented |
| DeviceGray/RGB/CMYK preview | Implemented |
| CalGray/CalRGB/Lab preview | Implemented |
| Separation/DeviceN semantic preservation and alternate preview | Implemented foundation |
| Axial/radial shadings | Implemented |
| PDF Functions Type 0/2/3/4 | Implemented |
| Type 1 tiling-pattern planning/nested-cell architecture | Implemented |
| Blend modes | Implemented |
| Overprint state preservation | Implemented |
| Broken-xref structural recovery | Implemented |
| Password-authentication handoff | Implemented |
| Damaged encryption reconstruction | Intentionally unsupported |
| JBIG2 decoding | Provider boundary |
| JPX/JPEG 2000 decoding | Provider boundary |
| Full prepress overprint plate simulation | Deferred |
| External SVG/EPS rasterization | Intentionally avoided; vector import path preferred |

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-10 | Added the BF-1048 rendering support matrix without introducing a runtime capability-report API. |
