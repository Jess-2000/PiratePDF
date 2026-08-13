# BlackFlag Engine SDK Specification

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-015 |
| **Category** | Technical Specification |
| **Version** | 1.2 |
| **Status** | In Development |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-8000, BF-1050 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-10-24 |

---

**Status:** Draft  
**Version:** 1.2

## Purpose

`BlackFlag.Engine.SDK` exposes stable public contracts to PiratePDF and future plugins. Internal engine implementations are reached through adapter/composition layers rather than by exposing internal domain types through SDK signatures.

## Principles

- Do not expose parser implementation types.
- Prefer immutable result models.
- Support cancellation.
- Return structured diagnostics.
- Version public capabilities.
- Keep UI concerns outside the SDK.

## Plugin contract boundary

Plugin-facing code compiles against `BlackFlag.Engine.SDK`. The SDK must not acquire project references to parser, document, security, service, rendering implementation, or UI projects merely to expose functionality. Internal adapters translate SDK-owned request/result models to internal engine models. Plugin manifests are declarative, bounded, versioned, and validated before executable plugin loading.

## Planned service areas

- Document opening.
- Page access.
- Rendering.
- Search.
- Editing.
- Color analysis.
- Saving.
- Events.
- Options.
## Document security API

`IDocumentSecuritySdkService` defines the public security-policy contract using only SDK-owned types. `BlackFlag.Engine.SDK.Adapters` provides the current in-process adapter to the internal BlackFlag security service. SDK results are
immutable snapshots containing reportable findings, requested interaction
names, blocked state, manual-inspection state, and conditional report metadata.

Automatic clean evaluations remain silent and do not create a report. The SDK
does not persist reports or perform UI interaction. Format-specific analyzers
remain responsible for producing normalized observations.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.2 | 2026-08-13 | Established plugin contract boundary and separated SDK security contracts from the internal service adapter. |
| 1.1 | 2026-08-13 | Added document security reporting and SDK service surface. |
| 1.0 | 2026-07-24 | Standardized specification metadata and governance structure. |
