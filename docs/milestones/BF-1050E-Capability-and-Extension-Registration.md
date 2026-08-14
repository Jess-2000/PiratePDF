# BF-1050E — Capability and Extension Registration

| Field | Value |
|---|---|
| **Document ID** | BF-1050E |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Provide a typed, capability-gated SDK boundary and transactional extension registration without exposing BlackFlag implementation objects to plugins.

## Contract

- Capabilities are host-owned objects addressed by typed SDK keys and must implement `IPluginCapability`.
- A plugin may resolve only capability IDs declared in its validated manifest. Undeclared access is rejected; unavailable required capabilities prevent initialization; unavailable optional capabilities resolve as absent.
- Extension points are typed SDK keys. Registrations made during initialization are pending until initialization succeeds.
- Successful initialization commits registrations atomically. Initialization or commit failure rolls pending registrations back.
- Committed extensions are owned by plugin identity and are removed before its collectible load context is released.
- Runtime environment objects remain host-side. Plugins receive only the narrow SDK `IPluginCapabilityAccess` and `IPluginExtensionRegistration` interfaces.

## Security and Bounds

The capability boundary is an authorization/API boundary, not an operating-system sandbox. Capability access is restricted to manifest-declared IDs and contract types. Extension registration is bounded per plugin. No raw `PdfDocument`, parser, security implementation, service container, or other internal engine object is exposed through the SDK.

## Optimization Review

Capability lookup uses ordinal hash maps and a per-plugin declared-capability hash set. Required-capability validation is a single bounded manifest pass. Extension registration accumulates a bounded pending list and commits once; cleanup traverses only registered extension-point buckets and active plugins. No capability is eagerly initialized and no discovery or dependency graph is rescanned.

## Acceptance Criteria

1. Required unavailable capabilities block activation with a structured issue.
2. Declared capabilities resolve through strongly typed keys; undeclared capability access is rejected.
3. Extension registration is typed, bounded, transactional, and plugin-owned.
4. Initialization failure leaves no committed extensions.
5. Session disposal removes owned extensions before load-context release.
6. SDK contracts remain independent of BlackFlag runtime implementation types.
7. Full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added typed capability access, host capability registry, typed extension points, transactional registration, ownership cleanup, bounds, and runtime integration. |

## R2 capability access contract correction

The applied BF-1050E candidate exposes the typed SDK capability operation as `Retrieve<T>`. R2 aligns the runtime implementation and SDK-only test plugin with that public contract and adds compile-time regression coverage. The installer validates the exact observed SDK contract and known BF-1050E runtime implementation variants before replacement, and stamps applied files so incremental builds cannot reuse stale outputs.
