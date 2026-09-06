# BF-1054E — Find/Replace Transaction Integration

| Field | Value |
|---|---|
| **Document ID** | BF-1054E |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-03 |
| **Next Review** | 2026-12-03 |

## Objective

Add logical existing-text find/replace above the provenance and mutation layers completed by BF-1054A through BF-1054D.

BF-1054E does not introduce another content-rewrite mechanism.

Search creates provenance-backed logical matches and replace-all routes each target through either:

- BF-1054C direct same-font replacement; or
- BF-1054D positioned `TJ` replacement.

## Search Scope

`PdfExistingTextSearchScope` pairs one session-owned displayed page with one authoritative `PdfExistingTextDiscoverySource`.

A search may receive multiple scopes, allowing page/source traversal to be orchestrated by callers without flattening source ownership.

Search remains deterministic and culture-independent:

- `matchCase: true` uses ordinal comparison;
- `matchCase: false` uses ordinal-ignore-case comparison.

Empty search strings are rejected.

## Logical Matches

`PdfExistingTextSearchMatch` retains:

- the search-scope index;
- the immutable provenance-backed descriptor;
- the text-element index;
- UTF-16 offset and length;
- matched Unicode snapshot.

Direct text uses text-element index zero.

Positioned `TJ` search operates inside each original text element independently.

BF-1054E deliberately does not synthesize matches spanning positioned `TJ` element boundaries because such a replacement would require redistributing text across preserved positioning boundaries.

## Replace-All Transaction

`ReplaceAll` owns one Editing transaction.

If no supported matches exist, no transaction/history entry is created.

If any mutation fails, the transaction is rolled back before the failure is returned.

On success, all logical replacements are committed as one change set, so one undo reverses the entire replace-all operation.

An already-active transaction is rejected; nested transaction semantics remain unchanged.

## Mutation Routing

Each unique descriptor is mutated once even when it contains multiple logical matches.

Direct descriptors receive one whole-run replacement string with every non-overlapping match replaced.

Position-preserving descriptors receive one replacement string per original text element; replacements never merge or split those element boundaries.

The established C/D services remain responsible for:

- provenance validation;
- same-font encoding;
- stale-source checking;
- filtered-stream rejection;
- authoritative stream mutation;
- undo/redo recording.

## Offset-Stability Rule

Length-changing edits can shift later byte offsets in the same stream.

Therefore BF-1054E applies unique mutation targets in deterministic source-safe order:

1. by content-stream identity;
2. within each stream, by descending source start offset;
3. then descending operation index/run ordinal.

This ensures edits at higher byte offsets cannot invalidate still-pending lower-offset provenance.

Multiple editable display runs for the same exact source operation are rejected as requiring a dedicated deterministic multi-run strategy.

## Search Boundaries

BF-1054E searches only targets currently supported by the established mutation strategies:

- `DirectlyEditable`;
- `EditableWithPositionPreservation`.

Read-only, ambiguous, shared-source, unsupported-encoding, unsupported-font, malformed, and resource-change targets are not returned as replaceable matches.

Search does not inspect raw PDF bytes for text and does not use coordinates, bounding boxes, or fuzzy matching.

## Acceptance Criteria

1. Find returns provenance-backed matches from direct existing text.
2. Case-sensitive and ordinal-ignore-case matching are deterministic.
3. Positioned `TJ` matches remain inside their original text-element boundaries.
4. Replace-all routes direct targets through BF-1054C.
5. Replace-all routes positioned targets through BF-1054D.
6. Multiple matches in one descriptor produce one source mutation.
7. All replacements commit as one Editing transaction.
8. One undo reverses the complete successful replace-all.
9. Failure after an earlier mutation rolls the transaction back atomically.
10. Same-stream targets are processed from higher offsets to lower offsets.
11. No-match replacement creates no history entry.
12. Stale provenance remains an explicit failure; no fuzzy remapping is added.
13. Existing C/D encoding, filtered-stream, Form/shared-source, and font boundaries remain unchanged.
14. No new Writing save domain is introduced.
15. No Rendering interpreter code is changed.
16. The full PiratePDF release gate passes.

## Deferred Work

Cross-run phrase matching, cross-`TJ`-segment replacement, regex/wildcard search, whole-document traversal orchestration, UI search state, typography-state changes, font changes, shared Form handling, public SDK exposure, reflow, and locale-sensitive linguistic search remain later work.

## Preservation Review

Find is non-mutating.

Replace-all performs only mutations already authorized by BF-1054C and BF-1054D.

Ordering is chosen specifically to preserve provenance offsets during multi-target edits.

No external dependency is added.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-03 | Added provenance-backed find and atomic replace-all transaction orchestration over BF-1054C/D mutation strategies. |
