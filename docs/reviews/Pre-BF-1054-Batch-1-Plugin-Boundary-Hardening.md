# Pre-BF-1054 Batch 1 — Plugin Boundary Hardening

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-002 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050, BF-1054 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## Executive summary

Batch 1 addresses the two P0 plugin-boundary findings from the pre-BF-1054 C# repository audit:

- `AUD-P0-001` — platform-incorrect plugin package containment.
- `AUD-P0-002` — cast-mutable plugin contracts and lifecycle state.

The batch is limited to security and lifecycle hardening. It must preserve public API signatures, supported plugin behavior, activation ordering, capability decisions, extension ownership, cleanup ordering, SDK isolation, and package-local dependency loading. It adds no dependency and implements no BF-1054 feature.

## Approved implementation scope

### Package containment

Affected production files:

- `src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeHost.cs`
- `src/BlackFlag.Engine.Plugins/Runtime/PluginLoadContext.cs`

Required behavior:

1. Introduce one internal package-containment predicate shared by entry-assembly and dependency resolution.
2. Normalize the package root and candidate with `Path.GetFullPath`.
3. Use `Path.GetRelativePath` to evaluate containment.
4. Reject candidates when the relative result:
   - is rooted;
   - equals `..`; or
   - begins with `..` followed by either directory-separator character.
5. Accept the package root itself only where the caller's file-resolution contract permits it; assembly candidates must still identify an existing file.
6. Apply the predicate to entry assemblies and to managed and unmanaged dependency paths.
7. Preserve the existing structured runtime issues and exception containment.

The change must not claim to provide symlink isolation or turn `AssemblyLoadContext` into a security sandbox. Those guarantees require an operating-system/process boundary and remain outside this batch.

### Immutable plugin-facing collections

Affected production files:

- `src/BlackFlag.Engine.SDK/Plugins/PluginManifest.cs`
- `src/BlackFlag.Engine.Plugins/Discovery/PluginCatalog.cs`
- `src/BlackFlag.Engine.Plugins/Activation/PluginActivationPlan.cs`
- `src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeSession.cs`

Required behavior:

1. Keep the existing `IReadOnlyList<T>` public property signatures.
2. Materialize constructor inputs once and expose cached `ReadOnlyCollection<T>` views rather than raw arrays.
3. For `PluginRuntimeSession`, retain the private mutable lists required for cleanup, but expose cached `AsReadOnly()` views.
4. Do not allocate a new collection on each property access.
5. Preserve ordering, duplicate validation, capability/dependency validation, and lookup behavior.
6. Ensure callers cannot cast a public collection back to an array or mutable `List<T>` and change validated state.
7. Preserve reverse-order deactivation/disposal, extension removal, load-context release, issue recording, and idempotent disposal.

## Test-first changes

Add focused regression coverage before production edits.

### Path-boundary tests

- Accept an entry assembly directly inside the selected package.
- Reject a traversal or normalized sibling path.
- Reject a sibling whose name merely shares the package prefix.
- On a case-sensitive filesystem, reject a case-different sibling path.
- Preserve managed dependency loading from inside the package.
- Preserve unmanaged dependency containment behavior where the existing test infrastructure supports it.
- Continue returning the established structured issue for an unavailable entry assembly.

Platform-dependent tests should detect filesystem case sensitivity and skip only the case-sensitive assertion when the host filesystem cannot represent the scenario. The common traversal and sibling-prefix assertions must run everywhere.

### Collection-integrity tests

- Verify `PluginManifest` capability and dependency properties cannot be cast to their backing arrays and mutated.
- Verify `PluginCatalog` plugin and issue properties cannot be mutated through a concrete collection cast.
- Verify `PluginActivationPlan` activation order, blocked plugins, and issues remain stable after attempted casts.
- Verify `PluginRuntimeSession.ActivePlugins` cannot be cleared or reordered by a caller.
- Verify session disposal still deactivates plugins in reverse order, removes owned extensions, disposes plugin instances, releases load contexts, records cleanup issues, and remains idempotent.
- Retain the existing SDK-only dependency and exported-surface architecture tests.

Likely affected test files:

- `tests/BlackFlag.Engine.SDK.Tests/Plugins/PluginManifestTests.cs`
- `tests/BlackFlag.Engine.Plugins.Tests/Discovery/PluginDiscoveryServiceTests.cs`
- `tests/BlackFlag.Engine.Plugins.Tests/Activation/PluginActivationPlannerTests.cs`
- `tests/BlackFlag.Engine.Plugins.Tests/Runtime/PluginRuntimeHostTests.cs`
- `tests/BlackFlag.Engine.Plugins.Tests/Extensions/PluginExtensionRegistryHardeningTests.cs`

Use the nearest existing test file if an exact listed path differs. Do not create a parallel test abstraction solely for this batch.

## Behavioral and security risks

| Risk | Control |
|---|---|
| A valid dependency is rejected after containment changes. | Cover direct child, nested child, normalized path, managed dependency, and native dependency cases. |
| Windows path behavior changes unintentionally. | Use BCL path normalization and relative-path semantics; run all focused tests on the current Windows environment. |
| Concrete collection runtime types change for callers. | Public contracts promise `IReadOnlyList<T>`, not arrays or mutable lists; preserve signatures and enumeration order. |
| Runtime cleanup loses access to mutable state. | Keep private mutable lists and expose separate cached read-only views. |
| Cleanup order or diagnostics change. | Retain reverse iteration and existing issue types/messages; assert behavior. |
| Scope expands into hostile-code sandboxing. | Explicitly exclude process isolation, permission sandboxing, and symlink guarantees. |

## Validation sequence

Run in this order:

```powershell
dotnet test .\tests\BlackFlag.Engine.SDK.Tests\BlackFlag.Engine.SDK.Tests.csproj --configuration Release --no-restore

dotnet test .\tests\BlackFlag.Engine.Plugins.Tests\BlackFlag.Engine.Plugins.Tests.csproj --configuration Release --no-restore

dotnet test .\PiratePDF.slnx --configuration Release --no-restore

git diff --check

powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

After each production edit, inspect `git status --short` and the focused diff. Before acceptance, confirm no generated, dependency, roadmap, PDF, or unrelated files changed.

## Acceptance criteria

Batch 1 is complete only when:

1. Entry and dependency assembly paths cannot escape the package through traversal, similar-prefix, or case-different sibling paths.
2. Valid package-local managed and unmanaged resolution behavior remains intact.
3. Plugin manifest, catalog, activation-plan, and runtime-session collections are not publicly cast-mutable.
4. Public API signatures and enumeration order remain unchanged.
5. Capability, dependency, activation, extension, and security-policy behavior remains unchanged.
6. Runtime cleanup remains reverse-ordered, complete, diagnostic, and idempotent.
7. Focused SDK and plugin tests pass.
8. The solution test suite passes.
9. `git diff --check` passes.
10. The full PiratePDF release gate passes.
11. The final diff contains only Batch 1 production tests/code and any explicitly required documentation update.

## Out of scope

- `AUD-P0-003` security-result collection hardening; reserved for Batch 2.
- Process or operating-system sandboxing for hostile plugins.
- Symlink/reparse-point isolation guarantees.
- New plugin capabilities, extension points, manifest fields, or SDK APIs.
- Dependency changes or new packages.
- Performance refactoring unrelated to the two findings.
- BF-1054 existing-text-editing or typography work.
- Any PDF parsing, editing, writing, preservation, or serialization behavior.

## Approval boundary

This document specifies Batch 1 but does not implement it. Production and test changes must not begin until Batch 1 is explicitly approved for implementation.

## Revision history

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Defined the implementation and validation contract for pre-BF-1054 Batch 1 plugin boundary hardening. |
