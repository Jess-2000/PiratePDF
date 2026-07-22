# BlackFlag Engine SDK Specification

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
