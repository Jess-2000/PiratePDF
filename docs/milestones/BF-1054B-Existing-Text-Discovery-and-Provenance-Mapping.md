# BF-1054B — Existing Text Discovery and Provenance Mapping

| Field | Value |
|---|---|
| **Document ID** | BF-1054B |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1047, BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-08-27 |
| **Next Review** | 2026-11-27 |

## Objective

Bridge immutable Rendering text runs into the BF-1054A existing-text Editing contracts without introducing text mutation.

BF-1054B maps one authoritative page or Form content source at a time from:

- canonical graphics operations;
- the interpreted display list;
- content-owner identity;
- optional indirect content-stream identities;
- displayed-page/session ownership;

into deterministic `PdfExistingTextDescriptor` instances.

## Dependency Boundary

`BlackFlag.Engine.Editing` remains independent of Rendering.

The mapping lives in `BlackFlag.Engine.Services`, which is the orchestration layer allowed to consume both Editing and Rendering contracts.

BF-1054B adds a Services-to-Rendering project reference; it does not add an Editing-to-Rendering reference.

## Provenance Mapping

A display text run is accepted only when its `PdfContentSourceSpan` exactly matches one supplied canonical graphics operation.

The operation's array position becomes the `OperationIndex`.

Multiple text runs emitted from one source operation receive deterministic run ordinals in display-list order.

No fallback uses text content, coordinates, bounds, or approximate offsets.

If source provenance does not map exactly, discovery fails.

## Source Scope

`PdfExistingTextDiscoverySource` represents one exact authoritative source:

- page content; or
- Form XObject content.

The source includes its owning indirect-object identity and may include indirect content-stream identities by decoded stream index.

Form discovery is intentionally source-scoped rather than recursively inferred from the parent page display list. Later orchestration can discover a Form using its own canonical operation list and display list.

This prevents page-local identity from being assigned accidentally to nested Form text.

## Font Provenance

BF-1054B captures:

- the active `Tf` resource name when deterministically available;
- the resolved font's indirect object identity when the resource is indirect;
- font size;
- encoded bytes and directly derivable Unicode from the display run.

The font resource name is tracked through canonical `Tf`, `q`, and `Q` operations so graphics-state save/restore remains reflected in discovery.

## Initial Editability Classification

BF-1054B is intentionally conservative.

Direct eligibility is limited to simple text-showing operations with complete directly derivable Unicode.

Composite `TJ` arrays containing positioning adjustments or multiple text elements remain `ReadOnlyAmbiguous` until BF-1054D.

Type 3 text remains `ReadOnlyUnsupportedFont`.

Runs without a complete Unicode mapping remain `ReadOnlyUnsupportedEncoding`.

A Form source explicitly known to affect multiple placements is classified `EditableWithSharedSourceImpact`.

These classifications describe the currently supported editing strategy; they do not mutate the source.

## Ownership

Discovery requires an active Editing session and a page owned by that session.

The generated existing-text identities use the session ID and displayed-page ID from those authoritative Editing handles.

## Acceptance Criteria

1. Rendering text runs map to BF-1054A descriptors through Services.
2. Editing does not gain a Rendering project dependency.
3. Session/page ownership is validated.
4. Source spans map exactly to canonical operations.
5. Operation index and run ordinal are deterministic.
6. Stream index and optional indirect stream identity are retained.
7. Page and Form source kinds remain explicit.
8. Shared Form impact is not silently classified as page-local direct editing.
9. Active `Tf` resource names respect `q`/`Q` save/restore.
10. Indirect resolved-font identity maps to the Document object-identifier contract.
11. Composite `TJ` editing remains deferred.
12. Unsupported encoding/font cases receive explicit editability reasons.
13. Missing provenance fails rather than fuzzy matching.
14. No content stream is rewritten.
15. The full PiratePDF release gate passes.

## Deferred Work

- content-stream mutation;
- same-font replacement/deletion;
- composite `TJ` mutation;
- find/replace;
- typography changes;
- font substitution/resource mutation;
- recursive page-to-Form discovery orchestration;
- Form reuse-count discovery;
- public SDK text-editing API;
- general reflow.

## Preservation and Dependency Review

No external package or native dependency is added.

BF-1054B adds only the existing Rendering project as a Services dependency so the orchestration layer can perform the approved cross-layer mapping.

No PDF object, content stream, xref, resource, or serialized byte sequence is mutated.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-27 | Added source-scoped existing-text discovery and deterministic Rendering-to-Editing provenance mapping. |
