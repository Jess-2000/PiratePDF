# BF-1050F — Security Boundary and Policy Integration

| Field | Value |
|---|---|
| **Document ID** | BF-1050F |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Make BlackFlag security policy an enforced host-side boundary for plugin-originated capability access and extension registration without exposing security implementation objects through the SDK.

## Contract

- `BlackFlag.Engine.Security` owns plugin security requests, decisions, sensitivity classifications, exact grants, and the default policy.
- `BlackFlag.Engine.Plugins` is the enforcement adapter. Plugins continue to compile only against `BlackFlag.Engine.SDK`.
- Standard resources are allowed by the default policy after the BF-1050E manifest/type checks. Privileged resources are denied by default and require an exact host grant matching plugin identity, operation, and resource ID.
- Required privileged capabilities are authorized before plugin initialization. A denial prevents activation and produces `SecurityPolicyDenied`.
- Optional capability retrieval is authorized before the implementation is returned.
- Extension registration is authorized before a pending registration is added, preserving transactional rollback semantics.
- `AssemblyLoadContext` remains dependency isolation/unload infrastructure, not a security sandbox. In-process plugin code retains the process's operating-system authority.

## Security and Bounds

Security decisions occur host-side and cannot be replaced by a private plugin SDK assembly. Grants are exact rather than wildcard-based and are bounded to 4,096 entries. Resource and plugin identifiers are bounded. No raw `PdfDocument`, parser, service container, security policy implementation, or other engine object is added to the SDK surface. Future document-operation capabilities must remain host adapters that invoke BlackFlag services and document security policy before engine mutation; a plugin grant does not override document inspection or document policy.

## Optimization Review

The default policy compiles grants once into an O(1) hash set. Capability sensitivity is stored with the existing capability binding and checked without rescanning the manifest or activation graph. Extension-point sensitivity uses an ordinal dictionary. Required capabilities retain one bounded pre-initialization pass. No security service or capability is eagerly initialized, and denied requests stop before host resource exposure.

## Acceptance Criteria

1. Privileged capabilities are denied by default even when registered and manifest-declared.
2. Exact host grants authorize only the matching plugin, operation, and resource.
3. Required capability denial occurs before plugin initialization and is reported structurally.
4. Privileged extension registration is denied before transaction mutation and leaves no committed extension.
5. Standard BF-1050E capability and extension behavior remains compatible.
6. Plugin assemblies still compile only against the SDK and receive no BlackFlag security implementation types.
7. Full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added deny-by-default privileged plugin policy, exact grants, capability/extension enforcement, structured denial, bounds, and integration tests. |
