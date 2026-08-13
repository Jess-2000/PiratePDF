# BF-1050A — Plugin Contracts, Identity, Manifest, Compatibility, and Initial SDK Boundary Separation

| Field | Value |
|---|---|
| **Document ID** | BF-1050A |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Establish the non-executing public foundation for BlackFlag Engine plugins and begin enforcing the architectural separation between public SDK contracts and internal engine adapters.

## Contract

- `BlackFlag.Engine.SDK` is the plugin-facing compile-time contract and contains no references to internal engine implementation projects.
- Plugin identity is stable and independent of display name and plugin version.
- Version-1 manifests are declarative, bounded, deterministic, and validated before any plugin assembly is loaded.
- SDK compatibility is explicit and independent of plugin version.
- Required and optional capabilities are declared as data; BF-1050A does not grant or execute capabilities.
- Existing document-security SDK inputs are represented by SDK-owned immutable models.
- `BlackFlag.Engine.SDK.Adapters` translates SDK security contracts to the BF-1049 internal security/service model.
- BF-1050A performs no plugin discovery, dependency resolution, assembly loading, activation, or extension registration; those belong to later BF-1050 slices.

## Security and Bounds

Manifest processing rejects empty or oversized input, excessive capability declarations, invalid IDs, duplicate capability declarations, malformed JSON, unsupported manifest versions, and invalid version values. JSON nesting is bounded. No plugin executable code is inspected or executed by the manifest reader.

## Optimization Review

- Manifest validation occurs before any executable loading and is bounded by byte count, JSON depth, and capability count.
- Manifest parsing is single-pass and does not use reflection.
- Capability collections are materialized once and exposed read-only.
- Compatibility checks are deterministic and require no discovery or assembly work.
- Security adapter mapping is explicit rather than reflection-based.
- The SDK no longer loads internal engine assemblies merely to expose its public security contracts.

## Acceptance Criteria

1. SDK builds without project references to internal BlackFlag engine projects.
2. Valid manifests produce immutable validated descriptors.
3. Invalid and over-limit manifests fail deterministically.
4. Compatibility checks distinguish plugin version from SDK compatibility.
5. Document security evaluation remains behaviorally equivalent through the adapter using SDK-owned request types.
6. Unit tests cover manifest validation, identity, compatibility, SDK immutability, and security adapter behavior.
7. Full PiratePDF release gate passes, including documentation generation and validation.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Established plugin contracts, bounded manifest validation, compatibility rules, and initial SDK/internal-adapter separation. |
