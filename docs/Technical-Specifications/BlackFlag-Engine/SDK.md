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
**Version:** 1.0

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
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized specification metadata and governance structure. |
