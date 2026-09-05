# BF-1054N — Canonical Resource Ownership and Collision-Safe Mutation

| Field | Value |
|---|---|
| **Document ID** | BF-1054N |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053, BF-1054 |
| **Last Updated** | 2026-09-05 |
| **Next Review** | 2026-12-05 |

## Objective

Add the mutation-side resource ownership architecture required for real-world existing-text resource changes.

BF-1054N removes the J–M limitation that external font projection requires direct-or-absent page resource dictionaries.

## Canonical Page Resource Mutation

`PdfPageResourceMutationProjector` accepts a page, resource category, preferred resource name, and resource object.

It returns:

- the actual collision-safe resource name;
- the ownership/copy-on-write strategy;
- whether an indirect category was copied;
- the page object projection.

The service operates on PDF object-graph resources and emits BF-1053 write projections. It does not mutate Rendering state.

## Copy-on-Write Policy

### Direct page resources

A direct page `/Resources` dictionary is copied and projected back onto the page.

### Indirect page resources

If the page directly references an indirect `/Resources` dictionary, that dictionary is copied onto the page before mutation.

The referenced object remains unchanged.

### Inherited resources

If the page has no local `/Resources`, N walks the actual indirect `/Parent` chain and resolves the first inherited resource dictionary.

The effective inherited dictionary is copied onto the target page.

The `/Pages` ancestor remains unchanged, preventing a page-local edit from silently changing sibling pages.

### Indirect resource categories

If the effective resource category itself is indirect, such as `/Resources /Font 15 0 R`, the category dictionary is copied before mutation.

The indirect category object remains unchanged.

## Ownership Classification

`PdfResourceMutationOwnership` explicitly distinguishes:

- direct page resources;
- indirect page resources copied onto the page;
- inherited resources copied onto the page;
- newly created page resources.

Shared impact is therefore represented rather than hidden.

## Collision-Safe Naming

A preferred resource name remains unchanged when free.

When it already exists in the effective category, N allocates a deterministic next suffix.

Examples:

- `F1` → `F2`, then `F3`;
- `FUnicode1` → `FUnicode2`;
- `PirateFont` → `PirateFont1`.

Because inherited resources are resolved before allocation, N does not accidentally shadow inherited names.

## Font Pipeline Integration

The J full TrueType, K WinAnsi subset, and M Type0/CID Unicode projectors all delegate page resource insertion to N.

Their returned `ResourceName` is the actual name selected by the canonical mutation projector.

This removes duplicate resource ownership logic from the three font projectors.

## Malformed Structure Policy

N rejects:

- non-indirect `/Parent` values;
- unresolved/non-dictionary parent objects;
- malformed direct or inherited `/Resources`;
- malformed resource categories;
- resource-inheritance cycles.

No fuzzy ownership inference is introduced.

## Scope Boundary

N handles page resource ownership.

It intentionally does not mutate Form XObject text, clone Forms, or decide shared-placement semantics.

BF-1054O will build those behaviors on top of this ownership foundation.

## Project-Wide Data Protection Policy

N introduces no persistence or networking path.

All remaining work remains subject to the approved project-wide invariant:

- protected data transmitted by PiratePDF uses TLS 1.2 or higher;
- protected PiratePDF-managed data persisted at rest uses AES-256 authenticated encryption.

## Acceptance Criteria

1. Direct page resources remain supported.
2. Missing page resources remain supported.
3. Indirect page resources use page-local copy-on-write.
4. Inherited resources use page-local copy-on-write.
5. Indirect resource categories use copy-on-write.
6. Shared indirect resource objects remain unchanged.
7. `/Pages` ancestors remain unchanged for page-local edits.
8. Inheritance cycles are rejected.
9. Malformed parent/resource structures are rejected.
10. Resource ownership strategy is explicit.
11. Free preferred names remain unchanged.
12. Resource collisions allocate deterministic suffixes.
13. Inherited names participate in collision detection.
14. Existing effective resources are preserved in the page copy.
15. J uses the canonical resource mutation projector.
16. K uses the canonical resource mutation projector.
17. M uses the canonical resource mutation projector.
18. Font projection reports the actual allocated resource name.
19. No Form/shared-placement mutation is introduced.
20. The full PiratePDF release gate passes.

## Deferred Work

- Form XObject resource ownership and shared-source editing;
- placement-only Form cloning;
- filtered content-stream mutation and safe re-encoding;
- newly embedded external-font integration into existing text;
- Windows/macOS font adapters;
- superscript/subscript, color, compound operators, and decorations;
- whole-document orchestration and width-aware local adjustment;
- public SDK exposure;
- final BF-1054 conformance and hardening.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-05 | Added canonical page resource ownership, inherited/indirect copy-on-write mutation, collision-safe allocation, and J–M integration. |
