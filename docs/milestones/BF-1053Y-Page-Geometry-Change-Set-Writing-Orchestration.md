# BF-1053Y — Page Geometry Change-Set Writing Orchestration

| Field | Value |
|---|---|
| **Document ID** | BF-1053Y |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-25 |
| **Next Review** | 2026-11-25 |

## Objective

Bridge committed page-geometry Editing transactions into the preservation-aware page-geometry Writing projector introduced by BF-1053N.

## Supported Operations

`PdfPageGeometryWritingService` accepts committed `PdfPageGeometryEditOperation` entries for set-box, crop, and resize mutations.

Repeated geometry changes to the same page are deduplicated before projection. The projector reads the final authoritative page state, so only one page-dictionary projection is required regardless of how many geometry operations affected that page in the transaction.

## Preservation

Only targeted bound page dictionaries are projected. Untargeted page objects and unrelated dictionary entries remain preserved according to the existing projection and write-plan contracts.

Inherited or defaulted geometry continues to be represented by omission of the corresponding explicit page-dictionary key, matching BF-1053N behavior.

## Ownership and Layering

Every operation must belong to the supplied editing session, and the Writing plan must reference that session's authoritative document.

The orchestration remains in `BlackFlag.Engine.Services`; Writing does not gain a production dependency on Editing.

## Scope Boundary

This slice intentionally handles `PdfPageGeometryEditOperation` only. `PdfPageEditOperation.SetRotation` remains outside this service because it belongs to the general page-operation transaction stream. Structural page insert/remove/move/replace orchestration also remains deferred.

## Acceptance Criteria

1. Set-box, crop, and resize change sets resolve to bound page object identifiers.
2. Repeated geometry operations on one page produce one page target.
3. Incremental planning writes only the targeted page dictionaries.
4. Full-rewrite planning applies final authoritative geometry.
5. Non-geometry operations are rejected rather than silently omitted.
6. Foreign-session operations are rejected.
7. Unbound or detached target pages are rejected.
8. Writing retains no production dependency on Editing.
9. No external dependency or CI prerequisite is added.
10. The full PiratePDF release gate passes.

## Deferred Work

General page-operation transaction orchestration, page-tree structural editing, content/resource transaction orchestration, metadata transaction orchestration, cross-structure mixed transactions, encrypted writing, and public SDK save workflows remain deferred.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-25 | Added Services-layer page-geometry change-set Writing orchestration. |
