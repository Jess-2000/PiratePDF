# BF-1052A — Structural Editing Contracts and State Foundation

| Field | Value |
|---|---|
| **Document ID** | BF-1052A |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1052 |
| **Last Updated** | 2026-08-14 |
| **Next Review** | 2026-11-14 |

## Objective

Establish reusable structural-editing contracts on top of the BF-1051 editing, transaction, history, validation, ownership, and preservation model without implementing a second mutation framework or prematurely introducing feature-specific public APIs.

BF-1052A is the common foundation for page geometry, metadata, annotations, forms, outlines, and generated page-decoration work in later BF-1052 slices.

## Structural Operation Contract

- Structural mutations remain ordinary `IPdfEditOperation` instances and therefore retain BF-1051 ownership, validation, transaction, rollback, undo, redo, and preservation behavior.
- `IPdfStructuralEditOperation` adds only structural classification through `PdfStructureKind`.
- `PdfEditScope` continues to answer **where** an operation applies: document, page, content, or resource state.
- `PdfStructureKind` answers **what structured feature** is targeted: page geometry, metadata, annotation, form, outline, or page decoration.
- Scope and structure kind are intentionally independent. The project must not expand `PdfEditScope` into a feature taxonomy as new editable structures are added.
- Concrete BF-1052 slices should use focused operation types for their own state and invariants rather than a generic operation containing nullable fields for unrelated features.

## Exact Structural State

`PdfStructuralValueState<T>` captures both a structural value and its logical representation origin. Supported origins are absent, explicit, inherited, and PDF-defaulted.

The origin is part of the state. For example, an explicit page rotation of zero and an inherited effective rotation of zero may render identically but are not the same logical document state. Rollback, undo, and redo must be able to restore the original representation when a concrete structural mutation changes it.

An absent state deliberately carries no value. This allows optional structures such as metadata entries or feature-specific declarations to distinguish absence from a present value without using `null` as an ambiguous state marker.

## Relationship to Existing Page Resolution

The document layer already exposes effective page-value origin through `PdfEffectiveValue<T>` and `PdfPageValueOrigin`. BF-1052A does not replace that parser/document contract. `PdfStructuralValueState<T>` is an editing-state contract whose additional `Absent` state supports exact mutation history and optional structural declarations across features beyond page geometry.

BF-1052B must map page-geometry source state into this editing contract without flattening inherited or defaulted values into explicit declarations merely because a page is edited.

## Ownership, Transactions, and Validation

Structural operations participate in the existing BF-1051 session ownership boundary through `IPdfEditOperation`. Cross-session structural operations are rejected by the same ownership checks as page, content, and resource operations.

Concrete structural mutations introduced in later slices must continue to:

1. establish the proposed operation and exact before/after state;
2. validate through the owning `PdfEditingSession` before mutation;
3. mutate only the targeted structure;
4. record exact undo and redo behavior through the current transaction machinery; and
5. preserve accepted logical object identity wherever replacement is not required.

BF-1052A intentionally does not expose an independent structural transaction or history system.

## Preservation and Writing Boundary

Editing decides the logical document result; Writing later decides serialization.

BF-1052 structural work must not implicitly normalize untargeted structures, materialize inherited values that were not edited, replace unrelated indirect objects, or make xref/object-stream/filter/incremental-update decisions. Those concerns remain in the preservation-aware Writing roadmap stage.

The existing targeted FusionPro preservation contract remains authoritative for BF-1052.

## Deferred to Later BF-1052 Slices

- BF-1052B implements page geometry, page boxes, crop, resize, and rotation-state editing.
- Metadata, annotations, forms, outlines, and generated page decorations receive their own focused slices.
- Cross-structure validation and preservation hardening follow after the concrete structures exist.
- Services and public SDK exposure remain deferred until the engine contracts are stable.
- BF-1052A adds no external package dependency and therefore requires no package-specific CI environment change.

## Testing Strategy

BF-1052A tests verify that structural classification remains independent of logical edit scope, structural operations participate in existing session ownership checks, cross-session use is rejected, absent state does not expose a value, value-bearing origins are retained, and equal payloads with different origins remain distinguishable for exact-state history.

Core tests use real in-memory editing sessions and do not require Moq because the contracts under test have no substitutable external service boundary.

## Optimization Review

- Structural classification is enum-based and constant time.
- Structural state snapshots are immutable value types and require no reflection, document traversal, serialization, or graph cloning.
- BF-1052A introduces no global registry, cache, background work, or package dependency.
- Feature-specific state is intentionally deferred so the foundation does not allocate or copy data irrelevant to a concrete mutation.

## Acceptance Criteria

1. Structural edit operations remain compatible with the existing `IPdfEditOperation` ownership and validation boundary.
2. Edit scope and structural feature classification are separate concepts.
3. A reusable immutable state contract distinguishes absent, explicit, inherited, and defaulted structural values.
4. Structural-state equality preserves representation origin so history can distinguish logically different states with equal effective payloads.
5. No second transaction, history, validation, preservation, or document model is introduced.
6. No page-box, metadata, annotation, form, outline, page-decoration, SDK, or Writing implementation is pulled forward from its dedicated slice.
7. No external package dependency is introduced.
8. Unit tests cover structural classification, ownership integration, cross-session rejection, origin retention, absence, and exact-state equality.
9. The full PiratePDF release gate passes, including documentation generation, health reporting, repair preview, and whitespace validation.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-14 | Established structural operation classification and immutable exact-state contracts for later BF-1052 editing slices. |
