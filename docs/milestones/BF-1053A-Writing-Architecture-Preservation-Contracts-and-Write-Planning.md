# BF-1053A — Writing Architecture, Preservation Contracts, and Write Planning

| Field | Value |
|---|---|
| **Document ID** | BF-1053A |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-15 |
| **Next Review** | 2026-11-15 |

## Objective

Establish the preservation-aware PDF Writing boundary that converts the authoritative logical document state into an explicit output plan without yet serializing objects or emitting bytes. BF-1053A is the foundation for later object serialization, stream writing, cross-reference generation, incremental update, structural-edit projection, security integration, Services/SDK exposure, and hardening slices.

The milestone follows the existing project rule: **Editing decides what the document becomes; Writing decides how that result is serialized.** Writing must not introduce a second mutable document model or reinterpret accepted editing operations.

## Writing Policy

`PdfWriteOptions` captures physical output policy independently from logical Editing behavior. The initial contract distinguishes full rewrite from incremental update and separately captures cross-reference representation policy. Preservation defaults retain existing indirect-object identifiers, preserve untargeted objects, and prohibit untargeted normalization.

Contradictory policy that requests both preservation and normalization of untargeted objects is rejected before a plan is created. No option in BF-1053A silently opts into rebuilding or renumbering unrelated structures.

## Preservation Contract

`PdfWritingPreservationContract` is a Writing-owned immutable statement of the guarantees captured by a plan. It does not depend on `BlackFlag.Engine.Editing`, and Writing does not inspect editing-session history to decide output policy. The authoritative `PdfDocument` is the handoff boundary.

The contract preserves existing object identifiers and untargeted objects according to the selected options and explicitly does not promise byte-for-byte equivalence. Targeted edits, offsets, cross-reference representation, encryption, and future stream serialization may legitimately alter bytes while still preserving untargeted logical structure and stable indirect-object identity.

## Write Planning

`PdfWritePlanner` validates the minimum trailer/object-store invariants required before serialization work can begin:

- the trailer must have a root catalog reference;
- the root reference must resolve to the document object store;
- optional `/Info` and `/Encrypt` references, when present, must also resolve to the object store; and
- the current indirect-object identifier namespace is captured in deterministic object-number/generation order.

The resulting `PdfWritePlan` is immutable with respect to the captured identifier set. It records the document, requested revision and xref strategy, preservation contract, trailer identities, and the first safe object number for later allocation. New object allocation deliberately begins after the highest existing object number instead of reusing gaps, reducing the risk of accidental source-identity collisions.

An incremental-update plan declares that execution requires access to the original source revision. BF-1053A does not pretend that the current document model already carries every source byte/offset needed to execute that plan; source-revision plumbing belongs to a later BF-1053 slice.

## Deferred BF-1053 Work

Later BF-1053 slices will own primitive and composite object serialization, strings/names/numbers, indirect-object emission and allocation, stream/filter preservation, cross-reference table and stream generation, trailer/startxref output, source-revision and incremental-update mechanics, projection of BF-1052 logical structural edits into PDF dictionaries/content, security/encryption-aware writing, Services/SDK exposure, and adversarial/performance/preservation hardening.

Text editing, font replacement, arbitrary graphics editing, image replacement/import/export, form-data merge, and application UI remain separate feature milestones. Writing provides persistence infrastructure for those features but does not pull them into BF-1053A.

## Testing Strategy

Tests use real in-memory `PdfDocument` and `PdfObjectStore` instances. Coverage verifies deterministic object-identity snapshots, safe next-object numbering, snapshot isolation from later store mutation, preservation defaults, incremental source-revision requirements, trailer-reference validation, optional Info/Encrypt identity retention, and rejection of contradictory preservation policy. Moq is not required because BF-1053A introduces no substitutable service boundary.

## Optimization Review

Planning performs one ordered enumeration of existing indirect-object identifiers and copies only the identifier set. It does not serialize objects, decode streams, traverse page content, clone the document graph, inspect rendering state, or read source bytes. The plan therefore scales with indirect-object count and creates no background work or global caches.

## Acceptance Criteria

1. Writing policy is represented independently from logical editing operations.
2. Full rewrite and incremental update are explicit revision strategies.
3. Cross-reference representation policy is explicit and does not force normalization by default.
4. Existing indirect-object identifiers and untargeted objects are preserved by default.
5. Contradictory preservation/normalization policy is rejected before planning.
6. Trailer Root, Info, and Encrypt references are validated against the authoritative object store.
7. Existing indirect-object identifiers are captured deterministically without renumbering.
8. Future object allocation starts above the highest existing object number rather than filling gaps.
9. Incremental-update plans explicitly require the original source revision.
10. No object serialization, xref emission, stream rewriting, security transformation, Editing dependency, or external package dependency is introduced.
11. Tests cover policy, preservation, planning, trailer validation, object identity, allocation boundary, and snapshot isolation.
12. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-15 | Established preservation-aware Writing policy, contracts, trailer/object validation, and immutable write planning. |
