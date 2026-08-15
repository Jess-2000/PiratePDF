# BF-1052I — Services, SDK Exposure, and Integration Contracts

| Field | Value |
|---|---|
| **Document ID** | BF-1052I |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1052, BF-1051 |
| **Last Updated** | 2026-08-15 |
| **Next Review** | 2026-11-15 |

## Purpose

BF-1052I exposes stabilized structural-editing integration contracts through the existing Services, SDK, and SDK.Adapters boundaries without introducing a second editable document model or allowing the isolated SDK assembly to reference mutable engine types.

## Boundary model

The dependency direction remains unchanged:

`BlackFlag.Engine.SDK` → no other BlackFlag assembly

`BlackFlag.Engine.SDK.Adapters` → SDK + Document + Editing + Services

`BlackFlag.Engine.Services` → Document + Editing

The SDK owns only BCL- and SDK-valued contracts. The adapter performs all translation to the authoritative BF-1052 editing session.

## Structural SDK contracts

The SDK now exposes document-information fields, standard page-box kinds, normalized page-box values, structural value origins, resize anchors, and a structural summary. Metadata set/clear and page box/crop/resize mutations route through the existing editing operations, validators, transaction recorder, preservation contract, and history.

The structural summary provides safe inventory counts for annotations, AcroForm fields, outline items, page decorations, pages, and explicit document-information entries. It is intentionally descriptive rather than a parallel mutable object graph. Rich annotation, form, outline, and decoration mutation contracts remain owned by the internal editing model until dedicated SDK-owned value contracts for those structures are introduced; no internal mutable handles are leaked as a shortcut.

## Consistency service

`PdfEditingService.ValidateConsistency` and `IPdfEditingSdkSession.ValidateConsistency` expose the BF-1052H cross-structure postcondition check through application-service and SDK boundaries. Both delegate to the same authoritative `PdfEditingSession.ValidateConsistency()` implementation.

## Detached identity integration

The SDK adapter now retains session-owned page handles by stable ID, including detached pages created through the SDK and pages removed from the page tree. This closes the prior integration gap where `CreatePage` returned an SDK page descriptor but `InsertPage` could only resolve already attached pages. Detached page identity remains session-scoped and all insertion/replacement validation still occurs inside the editing subsystem.

## Change-set mapping

Structural operations map their public mutation kind into SDK change-set descriptions. Metadata, page geometry, annotations, forms, outlines, and page decorations therefore report logical mutation names instead of exposing internal operation class names.

## Preservation and Writing boundary

All structural mutations continue to use `PreserveUntargetedStructure`. BF-1052I does not serialize edits, regenerate appearances, materialize page-decoration content, parse missing high-level structures, or add a second state store. Writing remains a later serialization concern.

## Acceptance criteria

1. `BlackFlag.Engine.SDK` continues to reference no other BlackFlag assembly.
2. Structural metadata and page-geometry values are represented by SDK-owned contracts.
3. Metadata and geometry SDK mutations delegate to the authoritative editing session and participate in normal validation, transactions, history, and preservation.
4. SDK callers can invoke the BF-1052H consistency check without receiving an internal editing-session reference.
5. Structural inventory can be inspected without exposing mutable document, annotation, form, outline, or decoration objects.
6. SDK-created detached pages can subsequently be inserted by their stable SDK identity.
7. Structural SDK change sets report logical mutation kinds rather than internal operation type names where a stabilized mutation contract exists.
8. No Writing, appearance-generation, parser-hydration, external dependency, or CI behavior is added.
9. Existing SDK isolation guarantees remain intact.
10. Full PiratePDF release gate passes after local review.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-15 | Added BF-1052 structural service and SDK integration contracts. |
