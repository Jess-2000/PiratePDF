# BF-1050B — Plugin Discovery and Catalog

| Field | Value |
|---|---|
| **Document ID** | BF-1050B |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Implement bounded, deterministic discovery of plugin packages and a validated catalog without loading or executing plugin assemblies.

## Contract

- `BlackFlag.Engine.Plugins` is introduced as the runtime-side plugin orchestration project and references only `BlackFlag.Engine.SDK` in this slice.
- Discovery operates only on explicitly supplied roots. Each immediate child directory is a candidate plugin package; recursive filesystem crawling is prohibited.
- The package manifest is `plugin.json` by default and is parsed through the BF-1050A bounded manifest reader.
- Catalog entries contain validated manifest data and canonical package/manifest paths.
- Discovery order is deterministic. Duplicate plugin IDs retain the first deterministic package and produce a structured issue.
- Invalid, unreadable, or unavailable packages do not block unrelated valid plugins.
- Discovery does not inspect, reflect over, load, instantiate, or execute plugin assemblies.
- Dependency resolution, compatibility activation planning, assembly loading, and lifecycle remain later BF-1050 slices.

## Security and Bounds

Root count, candidate package count per root, and manifest processing are bounded. Manifest length is checked before allocation and parsing. Cancellation is propagated. Discovery avoids recursive traversal, arbitrary manifest paths, exception-object exposure, and executable loading.

## Optimization Review

- Roots and immediate package directories are normalized and sorted once for deterministic behavior.
- Enumeration stops after one entry beyond the configured package bound, avoiding eager traversal of an unbounded root.
- Manifests are read once using sequential asynchronous I/O into an exactly sized buffer after the file-length bound is checked.
- The catalog materializes descriptors once and builds a single ID lookup for O(1) lookup.
- No reflection, assembly scanning, dependency resolution, or capability initialization occurs during discovery.

## Acceptance Criteria

1. Valid plugin packages are discovered deterministically from configured roots.
2. Discovery never loads plugin executable code.
3. Invalid or duplicate packages produce structured issues while unrelated valid packages remain available.
4. Root, package, and manifest processing are bounded and cancellation-aware.
5. Catalog lookup by plugin ID is deterministic.
6. Tests cover ordering, duplicate IDs, invalid manifests, package bounds, and cancellation.
7. Full PiratePDF release gate passes, including documentation generation and validation.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added bounded deterministic plugin discovery, structured issues, and immutable cataloging. |
