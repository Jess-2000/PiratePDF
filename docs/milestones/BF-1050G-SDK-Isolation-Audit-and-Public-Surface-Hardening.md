# BF-1050G — SDK Isolation Audit and Public Surface Hardening

| Field | Value |
|---|---|
| **Document ID** | BF-1050G |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Audit the complete BF-1050 A–F SDK/plugin boundary before publishing the reference-plugin contract, lock dependency direction with executable architecture tests, and harden direct public manifest construction so it cannot bypass the invariants enforced by manifest parsing.

## Audit Result

`BlackFlag.Engine.SDK` remains a zero-project-reference assembly. Its exported API consists only of SDK-owned types and platform/BCL types. The plugin host depends only on `BlackFlag.Engine.SDK` and `BlackFlag.Engine.Security`; it does not reference Parsing, Syntax, Document, Services, Rendering, Editing, Writing, IO, Filters, Objects, or application/UI assemblies. The SDK-only test plugin continues to compile through its SDK project reference rather than through the runtime host.

The public `PluginManifest` constructor is now aligned with the manifest reader: duplicate capability declarations are rejected rather than silently normalized, required/optional overlap is rejected, null entries are rejected, and default capability/dependency bounds apply to direct programmatic construction. This prevents callers from bypassing the bounded declarative contract by constructing manifests directly.

## Architecture Gates

- SDK assembly references may not include another `BlackFlag.Engine.*` assembly.
- Exported SDK signatures are inspected recursively and may not expose types owned by another BlackFlag assembly.
- The plugin runtime host may reference only the SDK and Security BlackFlag layers.
- Manifest constructor tests lock duplicate, overlap, and bound behavior.

These tests are intended to fail when future changes accidentally widen the plugin boundary.

## Public Surface Decision

Runtime-host orchestration types remain public because PiratePDF and future host applications need to discover, plan, configure, activate, inspect, and shut down plugins. Those host APIs are not plugin compile-time contracts. Third-party plugin code continues to target `BlackFlag.Engine.SDK` only and receives constrained SDK interfaces through `PluginInitializationContext`.

`AssemblyLoadContext` remains an isolation/unload mechanism, not a security sandbox. BF-1050F host-side policy enforcement remains authoritative for privileged capability and extension access.

## Optimization Review

The new architecture checks execute only in the test suite. Runtime behavior gains no reflection or assembly scanning. Manifest construction performs bounded array materialization and small-set validation once at construction time; the default hard bounds cap those collections at 64 entries. Existing runtime hash-based capability/security lookups and dependency planning are unchanged. No hot-path allocation, graph rescan, eager capability creation, or repeated plugin assembly inspection was introduced.

## Acceptance Criteria

1. SDK has no project/assembly dependency on other BlackFlag layers.
2. SDK exported signatures cannot leak types from other BlackFlag assemblies.
3. Plugin host dependency direction is locked to SDK plus Security.
4. Direct manifest construction enforces the same core capability/dependency invariants as parsed manifests.
5. Existing BF-1050 A–F behavior remains compatible.
6. Full PiratePDF release gate passes with documentation validation and repair preview clean.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added executable SDK/runtime dependency gates, recursive exported-surface leakage checks, and bounded direct-manifest construction hardening. |
