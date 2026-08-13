# BF-1050D — Managed Plugin Loading and Lifecycle

| Field | Value |
|---|---|
| **Document ID** | BF-1050D |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Load dependency-safe plugin activation plans into collectible managed load contexts and provide deterministic initialization, deactivation, disposal, failure containment, and unload requests.

## Contract

- Plugin executable contracts remain in `BlackFlag.Engine.SDK`; runtime loading remains in `BlackFlag.Engine.Plugins`.
- `IBlackFlagPlugin` provides asynchronous initialization and deactivation. Initialization receives only immutable SDK-owned identity/version context in this slice; capabilities arrive in BF-1050E.
- Manifest entry points used for activation are assembly-qualified type names. Entry assemblies are resolved only as an immediate DLL within the validated plugin package.
- Each plugin receives a collectible `AssemblyLoadContext`; the host SDK assembly is shared from the default context so plugin contracts retain type identity.
- Managed and unmanaged dependency paths resolved by the plugin context must remain within the plugin package.
- Activation follows the BF-1050C dependency-first plan. Runtime failure of a dependency prevents its dependents from activating while unrelated plugins may continue.
- Initialization failure is contained and triggers best-effort cleanup. Session disposal deactivates and disposes active plugins in reverse activation order and requests context unload.
- `AssemblyLoadContext` is an isolation/unload mechanism, not a security sandbox. In-process plugins retain the operating-system permissions of the PiratePDF process.

## Security and Bounds

Runtime activation is bounded by a configured active-plugin limit and propagates cancellation. Entry assembly paths cannot escape the discovered package. Exceptions from plugin code are converted to structured diagnostics without exposing exception objects. Host SDK contract identity cannot be replaced by a plugin-private SDK copy.

## Optimization Review

Activation consumes the already-materialized BF-1050C plan, maintains one active-ID set for dependency checks, creates load contexts only for plugins that reach runtime activation, and performs no repeated discovery or graph construction. Cleanup is reverse linear traversal. No eager capability initialization is introduced.

## Acceptance Criteria

1. A valid SDK-only plugin can be loaded, instantiated, initialized, deactivated, disposed, and submitted for collectible unload.
2. Invalid entry points, missing/bad assemblies, constructor failures, and initialization failures are contained as structured runtime issues.
3. Runtime dependency failures block affected dependents without blocking unrelated plugins.
4. SDK contract type identity is shared with the host and cannot be shadowed by a plugin-private SDK assembly.
5. Entry/dependency assembly resolution remains package-bounded.
6. Runtime activation is bounded and cancellation-aware.
7. Full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added collectible managed plugin loading, lifecycle orchestration, runtime failure containment, and unload requests. |

## R3 Windows managed-assembly lifetime correction

The BF-1050D release gate exposed Windows file-handle retention when collectible plugin contexts loaded managed assemblies directly from package paths. R3 loads managed entry and dependency assemblies from bounded file streams into the collectible context instead. The package-boundary validation and SDK identity sharing rules remain unchanged. This prevents managed plugin DLLs from being held open by path-based assembly loading while preserving collectible-context lifecycle semantics. Native libraries remain path-loaded and are not covered by this managed-assembly guarantee.

The standalone test-plugin fixture project is no longer a direct solution test target; it remains a project dependency of `BlackFlag.Engine.Plugins.Tests`, so it is built as the runtime fixture without being incorrectly invoked as a test assembly. Regression coverage verifies that the managed plugin DLL can be opened exclusively after session disposal on Windows-compatible file systems.

### Optimization review

The correction adds no graph scans, polling, retry loops, forced garbage collections, or repeated assembly discovery. Each managed assembly is read once when the load context requests it. Stream lifetime is scoped to that load operation, and the existing resolver and package-boundary checks remain O(1) per resolved assembly apart from filesystem work.
