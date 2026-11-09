# BlackFlag Engine SDK Specification

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-015 |
| **Category** | Technical Specification |
| **Version** | 1.6 |
| **Status** | In Development |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-8000, BF-1050 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

---

**Status:** Draft  
**Version:** 1.6

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

Document-script execution must follow PP-GOV-006 across SDK, Services and
desktop integration. Existing security results describe inspection/reporting.
Future supported local form events require explicit policy admission and a
qualified bounded runtime. Safe Mode disables document scripts. Plugin
capabilities cannot elevate their privileges.
[Document form policy](../../05-Governance/Document-Form-JavaScript-Policy.md).

## Plugin Discovery Boundary

`BlackFlag.Engine.Plugins` owns runtime-side plugin discovery and cataloging while depending only on public SDK contracts at this stage. Discovery examines only explicitly configured roots, treats each immediate child directory as a candidate package, reads `plugin.json` under bounded limits, and never loads plugin assemblies. Invalid or duplicate packages produce structured discovery issues without preventing unrelated valid packages from being cataloged. Dependency declarations use stable plugin identities and bounded version ranges. Dependency resolution and activation planning validate SDK compatibility, missing or incompatible dependencies, cycles, graph bounds, and deterministic dependency-first ordering without loading plugin assemblies. Managed loading uses collectible per-plugin load contexts after activation planning. Plugin lifecycle contracts remain SDK-owned; runtime orchestration is host-owned. Load contexts provide dependency isolation and unloadability, not a hostile-code security sandbox. BF-1050F adds host-side security enforcement: plugin-visible capabilities and extension points can be classified as privileged, privileged access is denied by default unless an exact host grant matches plugin identity, operation, and resource, and denials occur before host resources are exposed or pending extension registrations are mutated. Security policy types remain outside the SDK; plugins continue to receive only SDK contracts. A plugin grant never bypasses document inspection or document security policy.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.6 | 2026-11-08 | Reference shared form-event policy and distinguish inspection/reporting from future qualified runtime support. |
| 1.5 | 2026-08-13 | Added managed plugin lifecycle contracts and collectible runtime loading for BF-1050D. |
| 1.4 | 2026-08-13 | Added bounded plugin dependency contracts and deterministic non-executing activation planning for BF-1050C. |
| 1.3 | 2026-08-13 | Added bounded deterministic plugin discovery and catalog boundary for BF-1050B. |
| 1.2 | 2026-08-13 | Established plugin contract boundary and separated SDK security contracts from the internal service adapter. |
| 1.1 | 2026-08-13 | Added document security reporting and SDK service surface. |
| 1.0 | 2026-07-24 | Standardized specification metadata and governance structure. |
