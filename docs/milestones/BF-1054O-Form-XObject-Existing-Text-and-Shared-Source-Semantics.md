# BF-1054O — Form XObject Existing-Text Editing and Shared-Source Semantics

| Field | Value |
|---|---|
| **Document ID** | BF-1054O |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-05 |
| **Next Review** | 2026-12-05 |

## Objective

Activate the Form XObject provenance architecture established in BF-1054A/B without ever treating a reused Form as page-local by accident.

O defines two explicit meanings of a Form text edit:

1. edit the authoritative shared Form source;
2. clone the Form and redirect one explicitly identified placement.

No API silently chooses between them.

## Shared-Source Editing

`PdfSharedFormExistingTextMutationService` authorizes mutation only when:

- the discovery source is `FormXObject`;
- the source is explicitly marked `HasSharedSourceImpact`;
- the descriptor is `EditableWithSharedSourceImpact`;
- session/page/source identity still matches.

The Editing layer retains its conservative default: normal page-text mutation APIs still reject Form sources.

Only the explicit shared-Form path can opt into Form stream mutation.

Undo/redo remains handled by the existing `PdfExistingTextEditOperation`.

## Placement Identity

`PdfFormPlacementDescriptor` identifies one concrete page-content `Do` invocation by:

- page object identifier;
- page content-stream identifier;
- source Form identifier;
- XObject resource name;
- exact operation source span.

A descriptor is created by matching a `PdfDisplayForm` placement to the canonical page-content operation.

Placement identity is therefore provenance-based, not coordinate-based.

## Placement-Only Clone

`PdfFormPlacementCloneProjector`:

1. validates the original Form and page content stream;
2. reserves a new Form object identifier;
3. clones the original Form dictionary;
4. uses caller-supplied edited Form stream bytes for the clone;
5. adds the clone to the target page's `/XObject` resources through BF-1054N;
6. receives a collision-safe resource name;
7. rewrites only the selected `Do` operation to the clone resource;
8. projects the page, page content stream, and cloned Form through BF-1053 Writing.

The original Form object remains untouched.

Other placements that still reference the original resource therefore remain unchanged.

## Shared Impact

The two supported semantics are intentionally different.

### Edit shared source

```text
Form 12 0 R
 ├─ placement A
 ├─ placement B
 └─ placement C

edit Form 12 0 R
→ A, B, and C change
```

### Clone one placement

```text
Form 12 0 R
 ├─ placement A
 ├─ placement B
 └─ placement C

clone for placement B
→ B references new Form
→ A and C remain on Form 12 0 R
```

## Resource Ownership

Placement cloning reuses N's canonical page resource mutation projector.

This means direct, indirect, and inherited page `/Resources` continue to obey copy-on-write and collision-safe naming.

O does not add a competing resource mutation system.

## Filter Boundary

O remains limited to unfiltered Form and page content streams.

Filtered stream mutation/re-encoding is BF-1054P.

This avoids silently discarding or misrepresenting existing filter chains.

## Security

O introduces no disk cache or network transfer.

The project-wide data-protection invariant remains:

- TLS 1.2 or higher for protected data in transit;
- AES-256 authenticated encryption for protected PiratePDF-managed persisted data.

## Acceptance Criteria

1. Existing Form provenance remains authoritative.
2. Normal page-text mutation still rejects Form sources.
3. Shared Form mutation requires an explicit API.
4. Shared Form mutation requires `HasSharedSourceImpact`.
5. Shared Form mutation requires `EditableWithSharedSourceImpact`.
6. Session/page/source identity is validated before shared mutation.
7. Shared-source mutation affects the authoritative Form object.
8. Placement identity is based on the page `Do` operation provenance.
9. Placement-only cloning requires an indirect Form.
10. Placement-only cloning requires an indirect page content stream.
11. A new Form object identifier is allocated through BF-1053.
12. The original Form dictionary is preserved on the clone.
13. Caller-supplied edited Form data is isolated to the clone.
14. The original Form remains unchanged.
15. N allocates a collision-safe XObject resource name.
16. Only the selected page `Do` operation is redirected.
17. Other page placements retain the original Form reference.
18. Filtered Form/page streams remain rejected until P.
19. No new persistence/network path is introduced.
20. The full PiratePDF release gate passes.

## Deferred Work

- filtered stream decode/edit/re-encode;
- newly embedded external-font integration into existing text;
- platform font adapters;
- superscript/subscript, color, compound operators, and decorations;
- whole-document orchestration and width-aware local adjustment;
- public SDK exposure;
- final BF-1054 hardening.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-05 | Added explicit shared-Form mutation authorization, provenance-backed placement identity, placement-only Form cloning, XObject redirection, and N resource integration. |
