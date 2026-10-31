# BlackFlag Engine Plugin Development Contract

| Field | Value |
|---|---|
| **Document ID** | PP-DEV-002 |
| **Category** | Developer Documentation |
| **Version** | 1.1 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050 |
| **Last Updated** | 2026-10-31 |
| **Next Review** | 2027-01-31 |

## Purpose

This document defines the supported BF-1050 plugin authoring contract. Third-party plugin assemblies compile against `BlackFlag.Engine.SDK` only. `BlackFlag.Engine.Plugins` is host orchestration and is not a plugin dependency.

The canonical source example is `samples/BlackFlag.Engine.ReferencePlugin`.

## Project Boundary

A plugin project references `src/BlackFlag.Engine.SDK/BlackFlag.Engine.SDK.csproj` while developing in this repository, or the corresponding supported SDK distribution when published. Plugin code must not reference BlackFlag Parsing, Syntax, Document, Security, Services, Rendering, Editing, Writing, IO, Filters, Objects, the plugin host, or PiratePDF UI assemblies to gain engine access.

All engine interaction is through SDK contracts supplied in `PluginInitializationContext`. The context intentionally does not expose `PdfDocument`, a service provider, a security-policy implementation, or an internal object graph.

## Manifest Version 1

Each plugin package is one immediate child directory of a configured discovery root and contains `plugin.json`. The manifest is parsed and validated before executable code is loaded.

```json
{
  "manifestVersion": 1,
  "id": "org.example.plugin",
  "name": "Example Plugin",
  "version": "1.0.0",
  "minimumSdkVersion": "1.0.0",
  "entryPoint": "Example.Plugin.MainPlugin, Example.Plugin",
  "requiredCapabilities": [],
  "optionalCapabilities": [],
  "dependencies": []
}
```

`id` is the stable opaque plugin identity. `name` is display text. `entryPoint` is an assembly-qualified type name; the runtime resolves the simple assembly name to a DLL inside the plugin package. The entry type must be concrete, implement `IBlackFlagPlugin`, and have a public parameterless constructor.

Dependencies identify other plugins by stable ID and bounded numeric version range. Dependency planning occurs before code loading, is deterministic, rejects missing/incompatible dependencies and cycles, and activates dependencies before dependents.

## Lifecycle

`InitializeAsync` receives the immutable plugin identity/version, host SDK version, constrained capability access, and transactional extension registration. Honor the supplied cancellation token. Do not start unmanaged background work that outlives the plugin lifecycle.

Initialization is transactional for extension registration. If initialization fails, pending registrations are rolled back and the plugin does not become active. During shutdown the host calls `DeactivateAsync`, disposes the plugin when it implements `IAsyncDisposable` or `IDisposable`, removes extensions owned by the plugin, and requests unload of its collectible load context.

Plugin authors must not depend on immediate garbage collection after unload. Managed plugin assemblies are loaded without retaining the package DLL handle; native library lifetime has separate operating-system semantics.

Under [PP-GOV-005](../05-Governance/Platform-Support-and-Native-Lifetime.md),
native modules may remain loaded for the application's process lifetime.
Immediate deterministic native-image release is not required for v1. Process
termination is the reliable final release boundary; replacing an already-loaded
native dependency may require restarting the application. Managed deactivation,
disposal and collectible-context unload requests remain required. Do not free
runtime-owned handles manually or introduce speculative native ownership.

## Capabilities

Capabilities are host-defined, typed resources identified by `PluginCapabilityKey<T>`. A plugin may retrieve only a capability declared in its manifest. Required capabilities must be available and authorized before initialization. Optional capabilities may return `null`; plugins must handle absence without treating it as a host failure.

Capability declaration is not authorization. Host-classified privileged capabilities require an explicit policy grant for the exact plugin identity, operation, and resource ID. Plugins cannot use capability access to bypass document inspection, trust, remediation, or other BlackFlag security policy.

## Extensions

Extensions implement `IPluginExtension` and register against a typed `PluginExtensionPoint<T>` during initialization. Registration is bounded and transactional. Ownership belongs to the registering plugin and registrations are removed when its runtime session shuts down. Privileged extension points are subject to the same host-side policy enforcement as privileged capabilities.

Plugins should not cache host implementation details or attempt to mutate the host registry directly; those registries are runtime-host APIs, not SDK contracts.

## Isolation and Security

Each active plugin uses a collectible `AssemblyLoadContext` for managed dependency resolution, version isolation, and unloadability. The host shares its SDK identity so a plugin cannot substitute a private incompatible SDK copy.

`AssemblyLoadContext` is **not a security sandbox**. In-process plugin code executes with the operating-system permissions of the PiratePDF process. BF-1050 capability and extension policy constrains supported engine access, but it cannot make arbitrary hostile managed code safe. Truly hostile plugin isolation requires a separate process and operating-system boundary and is outside BF-1050.

## Compatibility and Versioning

Manifest and SDK compatibility use numeric `System.Version` values with omitted build/revision components treated as zero for plugin comparisons. The current compatibility rule requires the same SDK major version and a host SDK version at least as high as `minimumSdkVersion`.

Plugin developers should depend only on documented SDK public contracts. Runtime-host types, internal implementation behavior, reflection over engine internals, filesystem layout outside the plugin package, and undocumented assembly-loading behavior are not compatibility promises.

## Failure Model

One invalid or failing plugin must not prevent unrelated valid plugins from activating. A dependent plugin is blocked when its dependency is unavailable. Runtime diagnostics distinguish invalid entry points, unavailable assemblies, load/creation/initialization failures, missing required capabilities, policy denials, extension commit failures, and dependency unavailability.

Do not use exceptions as a way to request optional host features. Declare optional capabilities and handle `null` instead.

## Reference Plugin

`BlackFlag.Engine.ReferencePlugin` demonstrates the intended minimum pattern: SDK-only compilation, a version-1 manifest, cancellation-aware lifecycle, optional capability retrieval, typed transactional extension registration, and normal cleanup. It intentionally has no privileged shortcut and receives no internal engine type.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.1 | 2026-10-31 | Clarify owner-approved process-resident native module lifetime and restart requirements while preserving managed lifecycle and runtime handle ownership. |
| 1.0 | 2026-08-13 | Published the BF-1050 plugin developer contract and canonical SDK-only reference pattern. |
