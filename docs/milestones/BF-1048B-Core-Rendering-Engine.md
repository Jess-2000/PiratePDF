# BF-1048B â€” Core Rendering Engine

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-BF-1048B |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1045, BF-1046, BF-1047, BF-1048 |
| **Last Updated** | 2026-08-07 |
| **Next Review** | 2026-11-07 |

## Purpose

BF-1048B establishes PiratePDF's first production raster-rendering boundary. It consumes the immutable semantic display list produced by BF-1047 and introduces a platform-neutral raster API plus an isolated SkiaSharp CPU backend.

## Scope

BF-1048B includes:

- engine-owned raster surfaces and pixel-format contracts;
- page-region and tile-ready render requests;
- strict and tolerant rendering modes;
- bounded surface and Form-recursion limits;
- ordered rendering diagnostics;
- cancellation support;
- conservative display-list bounds culling;
- path fill and stroke rendering;
- PDF line caps, joins, miter limits, dash patterns, and hairlines;
- DeviceGray, DeviceRGB, and preview DeviceCMYK conversion;
- nested Form display-list rendering;
- an isolated `BlackFlag.Engine.Rendering.Skia` implementation project;
- unit coverage for render-request limits and real Skia raster output.

## Deliberate slice boundary

BF-1048B does not claim support for text, images, shadings, patterns, advanced color management, soft masks, or transparency groups. In tolerant mode those items generate explicit diagnostics. In strict mode they fail with `NotSupportedException`.

Those capabilities are owned by subsequent BF-1048 slices and are not silently approximated by this package.

## Platform policy

PiratePDF Version 1.0 officially targets Windows and macOS. The canonical rendering contracts contain no Windows, macOS, or Skia types. SkiaSharp is isolated in its own backend project so future Ubuntu and Linux Mint support does not require redesigning BlackFlag Engine.

## Performance policy

Rendering is region-based from the first implementation. Items with conservative bounds outside the requested region are skipped. Surface pixel counts and recursive Form depth are bounded. Render calls accept cancellation.

## Validation

After applying this package, run the repository-level release gate:

```powershell
.\Invoke-PiratePDFReleaseGate.ps1;
```

BF-1048B is not considered validated until the Release build, tests, DocGen generation, DocGen repair preview, and Git whitespace checks all pass on the project workstation.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-07 | Added BF-1048B core rendering engine production slice. |
