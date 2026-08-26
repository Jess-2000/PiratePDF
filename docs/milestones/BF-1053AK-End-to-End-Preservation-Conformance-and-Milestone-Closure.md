# BF-1053AK — End-to-End Preservation Conformance and Milestone Closure

| Field | Value |
|---|---|
| **Document ID** | BF-1053AK |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## Objective

Close BF-1053 by proving the preservation-aware Writing stack through the public save boundary across full rewrite, incremental update, traditional cross-reference tables, cross-reference streams, nested page transactions, annotations, and untargeted-object preservation.

BF-1053AK introduces no new Writing subsystem.

## End-to-End Conformance Boundary

The conformance suite begins with a real authoritative `PdfDocument`, opens a real Editing session, commits real Editing transactions, calls `PdfEditingSaveService`, and inspects actual serialized bytes.

The tests therefore exercise the complete implemented path:

Editing → committed change set → Services orchestration → preservation-aware projection → write planning → concrete writer → serialized PDF revision.

## Full-Rewrite Conformance

AK verifies full-rewrite serialization using:

- traditional cross-reference tables;
- cross-reference streams;
- nested page-tree structural mutation;
- page geometry and rotation;
- preservation of an untargeted indirect object.

Full rewrite remains a complete replacement revision and is not required to preserve original source bytes.

## Incremental Conformance

AK verifies that incremental serialization:

- copies the original source revision bytes unchanged at the beginning of output;
- writes only targeted projected objects into the appended revision;
- preserves untargeted object-store identity;
- emits `/Prev` using the original revision's cross-reference offset;
- supports traditional table output;
- resolves `PreserveSource` to cross-reference-stream output when the source revision used a cross-reference stream.

## Annotation Conformance

A committed annotation common-state edit is serialized end to end through the same public save boundary.

This confirms that AJ's public domain selection reaches the completed annotation transaction writer and concrete serializer rather than only producing an intermediate write plan.

## Preservation Claims Proven by BF-1053

BF-1053 now establishes the following implemented behavior:

1. explicit full-rewrite and incremental revision strategies;
2. traditional cross-reference tables and cross-reference streams;
3. preservation-aware indirect-object planning;
4. reservation-aware new-object allocation;
5. transactional projection rollback;
6. document information projection;
7. page geometry and rotation projection;
8. flat and nested page-tree structural projection;
9. multi-node nested page-tree topology preservation;
10. annotation common/structural transaction projection;
11. public Editing-to-save orchestration;
12. actual end-to-end serialization coverage for the supported domains.

## Historical Deferred Statements

Earlier BF-1053 sub-milestones intentionally described work that was deferred **at the time those slices were completed**.

Those historical records are not rewritten retroactively. Later BF-1053 slices subsequently implemented many of those items, including incremental writing, page-tree structure, nested hierarchy support, Editing change-set orchestration, and the public save boundary.

The chronological milestone set should therefore be read as an implementation history, with this closure record describing the final BF-1053 state.

## Remaining Out-of-Scope Work

The following remain deliberately outside BF-1053 closure and do not block completion:

- encrypted PDF Writing;
- detached-object deletion/free-xref policy;
- optional page-tree compaction, splitting, merging, or rebalancing;
- creation of new intermediate `/Pages` nodes;
- mixed page + annotation domains in one public save transaction;
- annotation appearance generation and subtype-specific advanced structures;
- Widget/AcroForm coordination;
- higher-level application file-system/save UX;
- future content/resource-specific editing persistence beyond the completed BF-1053 transaction domains.

These are separate future concerns rather than incomplete foundations of preservation-aware Writing.

## Documentation Reconciliation

AK retains the historical BF-1053 milestone documents unchanged while explicitly reconciling their older deferred-work language here.

The stale BF-1053D-era runtime error in `PdfFullRewriteWriter` is also replaced with mode-specific language that accurately reflects the completed architecture.

## Acceptance Criteria

1. Full rewrite serializes through `PdfEditingSaveService` with a traditional xref table.
2. Full rewrite serializes through `PdfEditingSaveService` with an xref stream.
3. Nested structural + page-state changes serialize end to end.
4. Untargeted full-rewrite objects remain present.
5. Incremental output preserves the original source-byte prefix exactly.
6. Incremental traditional output writes `/Prev` and avoids appending untargeted objects.
7. Incremental `PreserveSource` resolves to a source xref-stream representation.
8. Annotation Editing transactions serialize through the public save boundary.
9. Historical deferred-work language is reconciled without rewriting milestone history.
10. The full PiratePDF release gate passes.

## BF-1053 Closure Decision

Upon a passing full release gate for this slice, **BF-1053 is complete**.

No additional BF-1053 sub-slice is planned unless the release gate or merge verification exposes a concrete correctness defect.

The branch may then proceed through the project's normal final commit, optional completion tag, merge-to-`develop`, push, and post-merge validation procedure.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Added end-to-end conformance coverage, reconciled BF-1053 historical scope, and established the milestone closure gate. |
