# BF-1053J — Reservation-Aware Object Allocation and Documentation Contract Hardening

| Field | Value |
|---|---|
| **Document ID** | BF-1053J |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-16 |
| **Next Review** | 2026-11-16 |

## Objective

Remove the projected-object/cross-reference-stream allocation collision identified in BF-1053I and eliminate recurring DocGen release-gate surprises by aligning documentation parsing, validation, repair, templates, and canonical policy.

## Reservation-Aware Writing Allocation

`PdfWriteObjectAllocator` is created from a `PdfWritePlan` and snapshots the document's current object-number namespace at allocator creation time. It reserves generation-zero identifiers monotonically from the plan allocation boundary without reusing historical gaps.

Incremental cross-reference-stream finalization creates its allocator after changed projections are present in the document object store. The transient xref-stream identifier is therefore reserved after those projected objects and cannot collide with a projection occupying the original `NextObjectNumber`.

Callers reserving multiple new indirect objects should share one allocator instance.

## Documentation Contract Hardening

DocGen centralizes canonical metadata names, the Revision History heading, and the default review cadence in `DocumentationContract`.

A missing Revision History section is now both a validation error and a repairable condition. This removes the prior mismatch where validation could succeed but the release-gate repair preview could still propose a required structural change.

When `Next Review` is missing and `Last Updated` is a valid ISO date, repair deterministically inserts a review date three calendar months later. Existing dates continue through the existing ISO-date normalization path.

The developer template is restored to plain Markdown rather than containing its generating PowerShell here-string wrapper. The governance documentation standard is also restored to canonical Markdown and documents the shared validation/repair policy.

An integration regression test requires a canonical newly authored milestone to produce both zero validation issues and zero repair proposals.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added. Writing remains independent from Editing. DocGen changes affect repository tooling only.

## Deferred Work

This slice does not yet create a missing `/Info` dictionary, project page geometry or other structural domains, support free/deleted object entries, or implement encrypted writing.

## Acceptance Criteria

1. A newly occupied `NextObjectNumber` cannot collide with the incremental xref-stream object.
2. Allocated identifiers are generation zero, monotonic, and do not reuse historical gaps.
3. Missing Revision History is visible to validation and repair consistently.
4. Missing `Next Review` can be repaired deterministically from valid `Last Updated`.
5. Canonical metadata and structural names are centralized across relevant DocGen paths.
6. The developer template is plain canonical Markdown.
7. A canonical new milestone produces zero validation issues and zero repair changes.
8. No external dependency or CI prerequisite is added.
9. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-16 | Added reservation-aware allocation and hardened the DocGen documentation contract. |
