# BF-1054U — Public Existing-Text Editing Services and SDK Integration

| Field | Value |
|---|---|
| **Document ID** | BF-1054U |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050, BF-1051, BF-1054 |
| **Last Updated** | 2026-09-05 |
| **Next Review** | 2026-12-05 |

## Objective

Expose the mature BF-1054 existing-text editing path through one Services façade and an isolated SDK contract without leaking parser, rendering, document-object, Editing, or Services implementation types into `BlackFlag.Engine.SDK`.

U adds orchestration only. It does not create a second text model or a second mutation engine.

## Services Façade

`PdfExistingTextEditingService` delegates to the established A–T implementations for:

- discovery;
- find and atomic replace-all;
- direct width-aware replacement;
- positioned `TJ` replacement;
- typography updates;
- style/color/script/decorations;
- already-resolved font candidate enumeration and font switching.

Existing external-font embedding services remain authoritative for newly imported font programs. U does not duplicate their sanitization, embedding-rights, subsetting, Type0/CID, or resource-projection logic.

## SDK Isolation

`IPdfExistingTextSdkSession` exposes SDK-owned contracts only.

The concrete SDK adapter registers host-created interpreted source snapshots and translates them to opaque target IDs plus immutable SDK metadata. SDK contracts do not expose:

- `PdfDocument` or `PdfObject`;
- `PdfExistingTextIdentity` or source byte offsets;
- graphics operations/display-list objects;
- Editing ownership tokens or internal operations;
- Services types.

## Source Snapshot Lifetime

Existing-text identities are preservation-sensitive. After any SDK text mutation, the source registration that produced the target is invalidated.

The host must re-render/rebuild the authoritative discovery source, register the new snapshot, and rediscover targets before a second mutation. Stale targets fail explicitly.

This is intentional: U must never guess that byte offsets, operation ordinals, resource scope, or Form placement semantics remain valid after a content mutation.

## Exposed Editing Capabilities

The SDK surface supports:

- page-scoped text discovery;
- case-sensitive or case-insensitive find;
- atomic replace-all through Editing transactions;
- direct width-aware replacement with exact/local-compensation/reflow classification;
- positioned `TJ` segment replacement;
- font size, spacing, horizontal scaling, rendering mode, and rise updates;
- superscript/subscript, device-space fill/stroke color, underline, and strikeout;
- automatic decoration width through BF-1054T;
- enumeration and selection of already-resolved fonts in the active source scope.

New external-font programs continue through the BF-1054H–R Services/font-provider pipeline. U does not make untrusted font bytes a plugin-facing SDK primitive merely to mirror internal APIs.

## Transaction and History Semantics

SDK text operations use the same underlying `PdfEditingSession` as page/document edits. There is no parallel history stack.

Find/replace retains BF-1054E's single atomic transaction semantics. Direct text/style/font mutations participate in the existing Editing transaction/history rules.

## Reflow Boundary

U exposes T's fit result; it does not turn `RequiresReflow` into a silent approximation.

No SDK method:

- moves unrelated objects;
- stretches glyphs to force fit;
- wraps paragraphs;
- synthesizes line breaks;
- performs automatic cross-object layout changes.

## Shared Forms

Shared Form editability remains visible to SDK callers, but U does not silently treat shared source content as page-local. Existing O semantics remain authoritative.

## Security and Data Protection

U introduces no persistence or network transport and does not log document text by default.

Project-wide requirements remain:

- TLS 1.2 or higher for protected data in transit;
- AES-256 authenticated encryption for protected PiratePDF-managed data persisted at rest.

## Acceptance Criteria

1. A single Services façade exposes established BF-1054 workflows without duplicating mutation logic.
2. An SDK-owned existing-text session contract exists.
3. SDK discovery returns opaque target IDs and immutable SDK metadata.
4. SDK contracts do not reference internal Document, Editing, Rendering, or Services types.
5. Direct replacement exposes BF-1054T measurement and fit classification.
6. Locally compensable direct replacement uses the existing T implementation.
7. Reflow-required replacement fails explicitly when preservation is requested.
8. Positioned `TJ` replacement is exposed without flattening numeric positioning.
9. Find/replace uses BF-1054E atomic transaction semantics.
10. Typography updates use BF-1054F semantics.
11. Script/color/decorations use BF-1054S/T semantics.
12. Existing resolved font candidates can be enumerated and selected.
13. External font import/embedding remains on the established sanitization/resource pipeline.
14. Page/document and text edits share the same Editing session/history authority.
15. A text mutation invalidates its registered interpreted source snapshot.
16. Stale SDK target use fails explicitly.
17. Shared Form impact is not silently downgraded to page-local mutation.
18. No parser/display-list/source-byte implementation type leaks into the isolated SDK.
19. No paragraph/page reflow or cross-object layout engine is introduced.
20. The full PiratePDF release gate passes.

## Deferred Work

- final BF-1054 end-to-end conformance and hardening;
- paragraph/line reflow;
- automatic cross-object layout changes;
- dedicated complex-script shaping;
- application UI integration.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-05 | Added Services orchestration, isolated SDK text contracts/adapters, explicit source-snapshot invalidation, and public access to established BF-1054 direct, positioned, find/replace, typography, style, measurement, and existing-font workflows. |
