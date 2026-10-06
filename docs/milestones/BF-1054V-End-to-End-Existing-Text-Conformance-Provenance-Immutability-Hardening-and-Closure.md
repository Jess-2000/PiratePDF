# BF-1054V — End-to-End Existing-Text Conformance, Provenance/Immutability Hardening, and BF-1054 Closure

| Field | Value |
|---|---|
| **Document ID** | BF-1054V |
| **Category** | Milestone / Closure |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1047, BF-1050, BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-05 |
| **Next Review** | 2026-12-05 |

## Objective

Close BF-1054 with end-to-end hardening of the existing-text editing surface established through A–U. V fixes stale-provenance lifetime gaps, strengthens immutable public/read-only boundaries, removes duplicated PDF string-token logic, tightens SDK validation/mapping, and applies only evidence-backed allocation/performance improvements on the existing-text/rendering path.

V does not add reflow, paragraph layout, OCR, application UI, or unrelated repository refactoring.

## Provenance and SDK Source Lifetime

Existing-text SDK targets are valid only for the registered interpreted source snapshot that produced them.

V makes this invariant apply to all SDK operations capable of invalidating the interpreted source, including page/content/geometry mutations and history replay. Rollback also invalidates registered text sources because the adapter must not assume source offsets remain valid across a reverted mutation sequence.

Invalidation now removes source registrations, target registrations, and identity-to-ID mappings rather than retaining stale entries indefinitely. Target IDs are scoped to a source registration; replacing/re-registering the source produces fresh opaque target IDs.

Find and replace-all create one ordered current-source/search-scope snapshot. `ScopeIndex` addresses that snapshot directly, avoiding repeated linear searches across registrations for every match.

## SDK Boundary Validation and Mapping

V rejects malformed SDK style values before they reach internal mutation services:

- undefined device color spaces;
- wrong color component counts;
- non-finite/out-of-range device components;
- undefined decoration kinds;
- non-finite/non-positive explicit decoration widths/thickness;
- non-finite decoration offsets.

Internal validation remains in place as defense in depth.

SDK/internal enum translation for source kind, editability, and advance-fit classification now uses explicit switch mappings. The isolated SDK no longer depends on matching internal enum ordinals.

## Immutable Collection Hardening

Several rendering and existing-text values copied caller input into arrays but exposed those arrays directly through `IReadOnlyList<T>`. `IReadOnlyList<T>` alone does not prevent a caller from casting the runtime array and replacing elements.

V stores cached read-only wrappers for the BF-1054/rendering provenance values touched by this milestone:

- `PdfExistingTextColor.Components`;
- `PdfDisplayTextRun.Elements`;
- `PdfColorValue.Components`;
- `PdfDashPattern.Elements`;
- `PdfPathGeometry.Segments`;
- `PdfClipState.TextRuns`;
- `PdfDecodedTextRun.Glyphs` and `Diagnostics`;
- `PdfCMap.CodeSpaces`;
- `PdfShadingDescriptor.Stops`.

Public signatures remain `IReadOnlyList<T>`; only the concrete backing exposure is hardened.

## Shared PDF String Token Codec

`PdfTextStringTokenEditor` and `PdfTextArrayTokenEditor` previously duplicated literal/hex string scanning, comment skipping, escape/octal decoding, hexadecimal decoding, and literal/hex encoding.

V extracts those rules to `PdfTextStringTokenCodec` and leaves the editors responsible only for their distinct Tj/TJ mutation orchestration.

This keeps malformed-token behavior and byte-preservation rules aligned between primitive and positioned text replacement while substantially reducing duplicated hand-authored code.

## Targeted Allocation and Performance Improvements

V makes two bounded changes with direct evidence:

1. `PdfDisplayTextRun.EncodedBytes` concatenates text bytes with two bounded loops instead of `Where` + `SelectMany` + per-element `ToArray()` allocations. `UnicodeText` likewise avoids materializing a temporary filtered array.
2. `PdfCMap` precomputes distinct descending code-space lengths once when the immutable CMap is built, rather than rebuilding/sorting that sequence for every `ReadCode` call.

No performance claim is made beyond removal of those observable repeated allocations/work. No unrelated optimization is included.

## Validation Coverage

V adds or strengthens regression coverage for:

- non-text SDK content mutation invalidating existing-text targets;
- undo/redo invalidating registered source snapshots;
- fresh target IDs after source re-registration;
- SDK color and decoration validation;
- existing-text color cast-mutation resistance;
- display-list collection cast-mutation resistance;
- CMap code-space immutability and longest-code behavior;
- the existing Tj/TJ replacement suites exercising the shared token codec;
- the complete A–U existing-text behavior through the repository release gate.

## Preservation and Security Invariants

V preserves these requirements:

- `PdfDocument` remains authoritative state;
- Editing owns mutation/history/transactions;
- Rendering remains immutable interpreted state, not a serialization source;
- SDK contracts remain SDK-owned and do not expose parser/object/provenance internals;
- stale provenance fails explicitly rather than being fuzzy-matched;
- unsupported or malformed text is never silently approximated;
- content-stream mutation remains smallest-authoritative-source oriented;
- BF-1053 Writing remains the persistence boundary;
- no embedded content, fonts, actions, or external behavior is executed merely for inspection/editing.

## Out of Scope

- paragraph/column/page reflow;
- automatic line wrapping or text-box resizing;
- complex-script shaping redesign;
- OCR/vector text reconstruction;
- graphics/image/object editing beyond text provenance consumed here;
- application UI;
- unrelated parser/security/writer refactors;
- speculative performance changes without evidence.

## Acceptance Criteria

BF-1054V is complete when:

1. Structural/content/history mutations cannot leave SDK existing-text targets usable against stale authoritative state.
2. Invalidated source/target/identity registrations are removed and repeated source cycles do not accumulate stale adapter state.
3. Re-registering a source snapshot yields fresh target IDs while repeated discovery within one current snapshot remains stable.
4. Find/replace scope-to-source mapping is direct from one ordered snapshot and does not linearly rescan registrations per match.
5. SDK/internal enum mappings are explicit and reject unknown values.
6. Malformed SDK color/decoration values fail before internal mutation.
7. The listed immutable/read-only collection surfaces cannot be mutated through a concrete array/list cast.
8. Tj and TJ token mutation share one scanner/codec while retaining preservation and malformed-input behavior.
9. Display-text byte/text aggregation avoids the prior avoidable temporary allocations.
10. CMap code-space length ordering is computed once per immutable CMap.
11. No unrelated semantic/refactoring work is mixed into V.
12. All affected tests and the full PiratePDF release gate pass cleanly.
13. Documentation/DocGen/repository hygiene checks pass.
14. After a clean V commit and post-V snapshot, BF-1054 may be declared closed.

## Closure Decision

After BF-1054V passes the release gate and is committed from a clean working tree, create the authoritative BF-1054V snapshot. BF-1054 existing-text editing is then closed at the engine/Services/SDK level. Future paragraph reflow/layout, application editing UX, and non-text object editing proceed as separate roadmap work rather than extending BF-1054.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-05 | Initial BF-1054V implementation and closure specification. |
