# BlackFlag Engine SDK Specification

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-015 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | In Development |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-8000 |
| **Last Updated** | 2026-07-24 |
| **Next Review** | 2026-10-24 |

---

**Status:** Draft  
**Version:** 1.1

## Purpose

`BlackFlag.Engine.SDK` exposes stable APIs to PiratePDF and future plugins.

## Principles

- Do not expose parser implementation types.
- Prefer immutable result models.
- Support cancellation.
- Return structured diagnostics.
- Version public capabilities.
- Keep UI concerns outside the SDK.

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

`DocumentSecuritySdkService` exposes normalized BlackFlag security-policy
results without exposing PDF parser implementation types. SDK results are
immutable snapshots containing reportable findings, requested interaction
names, blocked state, manual-inspection state, and conditional report metadata.

Automatic clean evaluations remain silent and do not create a report. The SDK
does not persist reports or perform UI interaction. Format-specific analyzers
remain responsible for producing normalized observations.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.1 | 2026-08-13 | Added document security reporting and SDK service surface. |
| 1.0 | 2026-07-24 | Standardized specification metadata and governance structure. |
