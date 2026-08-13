# BF-1050H — Reference Plugin and Developer Contract

| Field | Value |
|---|---|
| **Document ID** | BF-1050H |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Publish a canonical plugin that compiles only against `BlackFlag.Engine.SDK`, exercise it through the real discovery/planning/runtime path, and document the supported third-party plugin contract before BF-1050 adversarial hardening.

## Implementation

The new `samples/BlackFlag.Engine.ReferencePlugin` project references only the SDK. Its version-1 manifest uses a stable opaque identity and assembly-qualified entry point. The implementation demonstrates cancellation-aware initialization/deactivation, optional typed capability retrieval, typed extension registration, and disposal without importing runtime-host or engine-internal types.

The sample is included in the solution as a sample project. Runtime integration coverage stages its built assembly as a real plugin package, discovers it, plans activation, activates it, observes its committed extension, then disposes the session and verifies owned extension cleanup.

`PP-DEV-002` defines the public developer contract: project boundary, manifest authoring, dependencies, lifecycle, capabilities, extensions, security policy, compatibility/versioning, failure semantics, unload behavior, and the explicit limitation that `AssemblyLoadContext` is not a hostile-code sandbox.

## Contract Decisions

The reference plugin has no privileged exception or private host hook. It uses the same SDK interfaces available to third-party plugins. Optional capability absence is a supported state. Extension registration remains transactional and host-owned. Plugins do not receive `PdfDocument`, service containers, policy implementations, or internal BlackFlag objects.

The sample deliberately does not introduce a general-purpose UI extension model. BlackFlag remains UI-independent; PiratePDF application-side UI contribution contracts are a separate concern.

## Optimization Review

H adds no production runtime hot path. The reference plugin performs one bounded optional capability lookup and one bounded extension registration during initialization. The integration test runs only in the test suite. No reflection scan, discovery rescan, eager capability initialization, background worker, or additional runtime graph traversal is introduced.

Moq remains unnecessary: H validates the real SDK-only sample through the real runtime boundary, which provides stronger contract coverage than a mocked plugin lifecycle.

## Acceptance Criteria

1. Canonical reference plugin compiles with `BlackFlag.Engine.SDK` as its only BlackFlag project dependency.
2. Reference manifest is version 1 and uses only supported manifest fields.
3. Real discovery, planning, activation, extension commit, and cleanup are integration-tested.
4. Developer documentation defines supported and unsupported plugin behavior, security limitations, compatibility, and failure semantics.
5. No privileged shortcut, engine-internal type, UI dependency, or security bypass is introduced.
6. Full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added the canonical SDK-only reference plugin, real runtime contract test, and plugin developer documentation. |
