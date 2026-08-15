# BF-1052F — Outlines, Bookmarks, and Navigation Structures

| Field | Value |
|---|---|
| **Document ID** | BF-1052F |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1052 |
| **Last Updated** | 2026-08-14 |
| **Next Review** | 2026-11-14 |

## Purpose

BF-1052F establishes authoritative logical document-outline ownership and controlled bookmark/navigation editing on top of the BF-1051 transaction/history model and BF-1052 structural-editing contracts.

## Scope

The slice adds one authoritative `PdfOutline` per document, stable outline-item identity, ordered root and child collections, titles, expanded/collapsed state, direct page destinations, URI/named navigation actions, controlled hierarchy and state mutations, validator-first mutation, rollback, undo/redo, and preservation-oriented tests.

Named-destination trees, arbitrary PDF action dictionaries, JavaScript actions, destination coordinate modes, appearance/UI concerns, dictionary serialization, indirect-object allocation, xref behavior, and byte writing remain outside this slice.

## Authoritative state and ownership

`PdfDocument` owns one `PdfOutline`. The outline owns ordered root items; each `PdfOutlineItem` owns stable identity, common navigation state, and ordered child items. Editing exposes stable `PdfEditableOutlineItem` handles rather than the mutable source objects.

Removing and reinserting an outline item preserves the same logical object identity. Root and child attachment are explicit structural states, and cycles are rejected.

## Navigation targets

An outline item can have either a direct `PdfOutlineDestination` or a `PdfOutlineNavigationAction`, never both. Setting one clears the other. Direct destinations reference the stable logical page ID rather than a page index, so page reordering does not retarget the bookmark.

Removing a page that is still referenced by a direct outline destination is rejected. The caller must clear or retarget the bookmark first, preventing an invalid dangling logical destination.

URI and named actions establish the action relationship without attempting to model arbitrary PDF action dictionaries. JavaScript and other action types remain deferred.

## Mutation contract

`PdfOutlineEditOperation` is an `IPdfStructuralEditOperation` with document scope and `PdfStructureKind.Outline`. Root presence transitions use absent/explicit structural state. Hierarchy and common-state changes carry exact before/after `PdfOutlineItemSnapshot` values.

Validators run before authoritative state changes. Existing editing transactions/history provide rollback, undo, and redo; there is no outline-specific history subsystem.

## Preservation

Editing one outline property preserves untargeted title/open/target state, hierarchy, child order, stable page identity, and unrelated document structures. The engine does not flatten the outline tree, rewrite unrelated bookmarks, convert destinations into actions, or serialize PDF objects.

The targeted FusionPro preservation contract remains authoritative. Editing decides the logical result; Writing later decides preservation-aware serialization.

## Testing strategy

Tests cover outline initialization, hierarchy/cycle protection, stable identity, root/child operations, title/open-state preservation, destination/action exclusivity, page-destination integrity, rollback, committed undo/redo, validator-before-mutation behavior, and cross-session ownership rejection.

## Acceptance criteria

1. Every logical document owns one authoritative outline root.
2. Outline items have stable identity independent of root/child/detached state.
3. Root and child collections preserve order and reject hierarchy cycles.
4. Titles and expanded/collapsed state are controlled mutations.
5. Direct page destinations reference stable page identity, not page indexes.
6. URI and named navigation actions are represented without premature arbitrary-action modeling.
7. Destination and action relationships are mutually exclusive and exactly replayable.
8. Pages referenced by direct outline destinations cannot be removed until the target is cleared or changed.
9. Outline edits use `PdfStructureKind.Outline`, validator-first mutation, and existing rollback/undo/redo machinery.
10. Named-destination trees, arbitrary actions, JavaScript, destination-coordinate modes, and Writing remain deferred.
11. No external package dependency is introduced.
12. The release gate passes with zero compiler/analyzer/test/documentation failures.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-14 | Added authoritative outline ownership, bookmark hierarchy, navigation targets, and preservation/history coverage. |
