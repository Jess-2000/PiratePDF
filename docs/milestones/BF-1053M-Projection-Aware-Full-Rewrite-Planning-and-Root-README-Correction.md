# BF-1053M — Projection-Aware Full-Rewrite Planning and Root README Correction

| Field | Value |
|---|---|
| **Document ID** | BF-1053M |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-22 |
| **Next Review** | 2026-11-22 |

## Objective

Close the planning asymmetry between incremental and full-rewrite projection paths and replace the repository root README with an accurate project-facing introduction.

## Projection-Aware Full-Rewrite Planning

`PdfProjectedFullRewritePlanner` accepts an existing full-rewrite plan and an explicit `PdfObjectWriteProjectionSet`. It validates projection identifiers using the same generation and allocation-boundary rules established for projected incremental planning, applies the projections, and creates a refreshed `PdfWritePlan` from the resulting authoritative object store.

Refreshing the plan is required because a full rewrite serializes the identifier snapshot captured at planning time. A newly projected indirect object cannot safely participate in full-rewrite output unless that snapshot is rebuilt after the projection is applied.

The refreshed plan preserves the original write mode, cross-reference policy, and preservation options. This prevents replanning from silently reverting caller choices.

## Failure Safety

Projection application is guarded by an object-store snapshot. If refreshed write planning fails, exact previously existing projected objects are restored and newly introduced projected objects are removed before the exception is rethrown.

The planner rejects incremental base plans, different-generation collisions, generation-nonzero new objects, and new objects below the original allocation boundary.

## Root README Correction

The former repository root `README.md` was a documentation-index document and did not accurately introduce PiratePDF to someone arriving at the repository root.

The replacement README now describes:

- PiratePDF's purpose and active-development status;
- BlackFlag Engine's role and preservation-aware architecture;
- implemented engineering areas without representing the full application as complete;
- representative project goals and workflows;
- repository layout;
- open-source and contribution expectations; and
- direct links to the Charter, Blueprint volumes, Roadmap, ADRs, technical specifications, plugin guidance, engineering history, milestones, and documentation library.

The README is intentionally repository-facing rather than part of the DocGen-controlled `docs/` metadata corpus.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added. Writing remains independent from Editing.

## Deferred Work

This slice does not add new structure-specific projectors, deletion/free-entry projection, encrypted writing, application UI, or Services/SDK write orchestration.

## Acceptance Criteria

1. Full-rewrite projection planning includes newly projected indirect objects in the refreshed identifier snapshot.
2. Refreshed planning preserves the caller's cross-reference and preservation choices.
3. Invalid projection identifiers are rejected before mutation.
4. Planning failures restore projected object-store state.
5. A refreshed plan can serialize a newly projected object through `PdfFullRewriteWriter`.
6. The root README accurately describes PiratePDF and links only to repository documentation paths that exist at this baseline.
7. No external dependency or CI prerequisite is added.
8. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-22 | Added projection-aware full-rewrite replanning and replaced the root README with a project-facing introduction. |
