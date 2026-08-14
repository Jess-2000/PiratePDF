# BF-1051I — SDK and Service Boundaries

| Field | Value |
|---|---|
| **Document ID** | BF-1051I |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051, BF-1050 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Expose the BF-1051 editing model through application-service and plugin-SDK boundaries without leaking the mutable document model, editing ownership tokens, transactions, validators, or internal editing operation types into the isolated SDK assembly.

## Boundary Model

The dependency direction is:

`BlackFlag.Engine.SDK` → no other BlackFlag assembly

`BlackFlag.Engine.SDK.Adapters` → SDK + Document + Editing + Services

`BlackFlag.Engine.Services` → Document + Editing

The SDK owns DTOs and interfaces only. The adapter maps those public contracts to the controlled `PdfEditingSession` facade. The SDK never receives `PdfDocument`, `PdfPage`, `PdfEditablePage`, `PdfEditTransaction`, `IPdfEditOperation`, or editing ownership tokens.

## Service Boundary

`PdfEditingService` is the application-facing session factory. It accepts the authoritative document internally and creates the single controlled editing session.

It does not introduce an alternate editable model and does not bypass BF-1051 ownership, validation, transactions, history, or preservation contracts.

## SDK Boundary

`IPdfEditingSdkSession` exposes:

- stable session identity;
- isolated page descriptors;
- preservation mode;
- controlled page and content mutations;
- transaction begin/commit/rollback;
- undo and redo availability and execution; and
- SDK-owned operation/change-set descriptions.

Resource-object mutation is intentionally not exposed in this slice because the current resource API accepts internal `PdfObject` instances. A future SDK resource contract must use SDK-owned value contracts rather than leak Document assembly types.

## Preservation and FusionPro

The adapter opens the internal session with `PreserveUntargetedStructure`. Therefore SDK editing follows the same BF-1051H targeted FusionPro preservation contract as direct internal editing.

Serialization and byte-level equivalence remain outside the editing and SDK boundary.

## Acceptance Criteria

1. SDK editing contracts reference no other BlackFlag assembly.
2. SDK consumers cannot obtain the mutable document model through the editing interface.
3. SDK page descriptors contain only SDK/BCL-owned values.
4. SDK operations are mapped to internal controlled editing mutations.
5. SDK transactions map committed change sets without exposing internal operations.
6. SDK undo and redo use the internal session history.
7. Application services create editing sessions through a dedicated service boundary.
8. The adapter preserves the BF-1051H preservation mode.
9. Direct resource-object mutation is not leaked through the SDK.
10. Existing SDK isolation tests continue to pass.
11. Full PiratePDF release gate passes after local review.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added editing service and isolated SDK adapter boundaries. |
