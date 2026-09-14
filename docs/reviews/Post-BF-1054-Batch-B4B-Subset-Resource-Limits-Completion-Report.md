# Post-BF-1054 Batch B4B - TrueType Subset Resource Limits

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-022 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-13 |
| **Next Review** | 2026-12-13 |

## Authority, baseline and disposition

The user approved POST-P0-005 and necessary directly exposed subset-generation resource corrections, including generated-byte preflight. This is B4B following the existing B4A/B4B/B4C naming sequence. Work started on `maintenance/post-BF-1054-repository-review` at **a615740ffa96c8decd6443317dafbb596aceb419**, Reject incomplete Flate streams. The working tree and diff were clean; POST-P0-015/F3-OBS-01 was committed after F4. No branch switch occurred.

Root AGENTS.md, PROJECT-CHARTER.md, README.md, the current audit, B4A trusted-font ownership and applicable F1/F2/F3/F4 ownership reports, F3-OBS-01, BF-1054I/K/L/M, ADR-0010 and Blueprint engineering/security rules govern this work. The [B4A report](Post-BF-1054-Batch-B4A-Sanitized-Font-Ownership-Completion-Report.md) remains the trusted-font contract. The consolidated Invoke-PiratePDFReleaseGate.ps1 was read and remains authoritative and unchanged. Test-PiratePDFSourceFileSizes.ps1 remains absent. Historical reports are not rewritten.

**Disposition: POST-P0-005 is implemented and closed after the full unmodified release gate passed all 1,989 tests across 20 projects, with zero build warnings/errors.** POST-P0-006 and later findings are not part of this batch. POST-P0-015 and B4A/F1/F2/F3/F4 ownership remain closed.

## Reproduction and retained-glyph invariant

Before production changes, two public-API regressions used a tiny sanitized simple font, required text A and MaximumRetainedGlyphs=1. Both the ordinary and Unicode subsetters returned successfully; both new Assert.Throws assertions failed because no exception was thrown. Glyph zero plus glyph 1 were retained despite the one-glyph maximum. This reproduces the audit precisely without large fonts or composite dependencies.

Previously both callers initialized HashSet<int> with zero and populated roots before Expand. Visit checked Count only when Add returned true, so existing roots were invisible to the limit. The previous dependency check also inserted the N+1 glyph before throwing. Unicode mapped sorted distinct scalars to original GIDs and built four CID dictionaries before adding each root. Its roots could exceed the default 16,384, although source/sanitizer glyph bounds still applied; this batch does not claim unbounded font input.

Now both root collectors and Visit use the same assembly-internal RetainGlyph admission helper. Existing members return immediately. A new member is rejected when Count is already at the maximum, before HashSet.Add and before new traversal-state insertion. Expand defensively validates the initial count before allocating its root snapshot or visit-state dictionary. Counts are O(1); there is no second retained set or repeated root enumeration. Glyph zero still enters the initial singleton set, which is valid because options require a positive maximum; it consumes one slot. Duplicate requests, different scalars mapping to the same GID, repeated components, shared dependencies and dependencies already selected as roots consume no extra slot.

Exactly N distinct retained glyphs succeed; the attempted N+1 member fails with the retained set still at N. All GIDs remain original. Hash-set insertion order and traversal order for successful inputs are unchanged. Root admission occurs after the existing encoding/range/usable-outline checks but before the corresponding mapping dictionary updates. Unicode CIDs remain assigned in sorted-scalar order, starting at 1, independently of GIDs. Two CIDs can still share one retained glyph and width.

The Unicode distinct/sorted scalar materialization remains before glyph selection. It is not a retained-glyph set and cannot be limited to MaximumRetainedGlyphs because many Unicode values may map to one GID. Its existing Identity-H scalar-count guard remains 65,534, checked after materialization; Unicode's finite scalar domain bounds that temporary distinct collection. This batch bounds retained-glyph admission and subsequent mappings for a rejected root, not all text enumeration work. No accepted CID/mapping policy or ordering is changed.

## Composite safety, cancellation and exceptions

The traversal algorithm, visited/visiting cycle detection, depth check, component-count check, component-range validation, transforms and instruction validation remain intact. Tests isolate cycle, depth, range, truncated component and component-count failures from retained-glyph failures. Glyph zero reached as an already-present component consumes no second slot. Directly requesting a character mapped to GID zero remains unsupported by the existing usable-root contract.

Retained-glyph failure remains exact InvalidDataException with `TrueType subset dependency closure exceeded the configured N-glyph limit.` Earlier rejection is intentional: a root budget failure can now precede later roots, metrics, composite diagnostics or generated-byte failures that the old bypass allowed execution to reach. Existing argument validation, positive-option parameter names, technology rejection and usable-root checks retain their order. No public wrapper or domain classification changes.

Cancellation before nonempty root processing remains exact OperationCanceledException carrying the original token, preceding root admission. Empty text cancellation is checked at closure entry before root-snapshot allocation, rather than at its first Visit. Existing Visit/component-loop and resanitization checks remain. No new high-frequency checks, timer-based cancellation tests or global switches are introduced. Both subsetters pass deterministic pre-cancel tests with empty/composite selections and conflicting retained limits. There is no existing public callback seam to cancel synchronously inside their private traversal or table-building loops, so no claim of deterministic mid-traversal injection is made; those checks were source-reviewed and retained.

## Generated-byte characterization and correction

Before correction both callers rebuilt/copy-materialized tables, called PdfTrueTypeSfntBuilder.Build, and only then compared sfnt.Length to MaximumGeneratedBytes. Build already knew exact final size while laying out the directory and padded tables, immediately before new byte[cursor]. Thus the old check bounded accepted output before resanitization but did not prevent allocation of the oversized final result.

The shared builder now receives the existing maximum and the caller's diagnostic label. Its single layout pass computes exact length and offsets, then rejects over-budget output before the final byte array, table writes, checksums or resanitization. The directory is 12 + 16 * tableCount bytes, already four-byte aligned; each table contributes its length rounded up to four bytes, including final padding. The head checksum adjustment occupies existing head bytes and adds no length. Rebuilt glyf/loca/cmap and copied table lengths are known at this point. Exactly the maximum is accepted; maximum+1 is rejected, tested as a known final output length with a ceiling one byte smaller.

Length/offset arithmetic is widened before multiplication, addition and alignment: checked 12L + count * 16L and checked accumulation of (LongLength + 3L) & ~3L. An int count of int-sized arrays cannot overflow long even at their theoretical maxima. The configured maximum is a positive int; any larger final length therefore produces the existing InvalidDataException before narrowing or allocation. Offset casts occur only after total length passes. There is one table-layout pass, one offset dictionary (now long values), and no extra table enumeration or generated-byte copy.

Existing messages are preserved exactly: `The generated TrueType subset is L bytes, exceeding the configured M-byte limit.` and `The generated Unicode TrueType subset is L bytes, exceeding the configured M-byte limit.` No broad OverflowException catch is added. Directory record/index arithmetic is bounded by the sanitizer-validated ushort table count and search fields. RebuildGlyphTables retains checked offset conversions and checked loca allocation; glyph count is at most 65,535, long loca at most 262,144 bytes. Short-loca representability still raises its existing NotSupportedException. Checksum addition intentionally wraps uint as required by the existing checksum algorithm; size arithmetic does not wrap.

Int-overflow-sized table metadata cannot be injected through the public subset APIs without correspondingly enormous actual trusted arrays. No new friend access, reflection seam, project reference or destructive OOM fixture was introduced solely to fabricate that state. Widening is established by the arithmetic/source proof; deterministic public tests cover all four length residues around alignment boundaries. No POST-P0-006 sanitizer numeric-length path was changed or characterized anew.

## Allocation accounting and limits of the correction

| Boundary | Before | After |
|---|---|---|
| Initial source | One owned full-font copy from TrustedBytes; parsed table/cmap/loca/metrics allocations | Unchanged; occurs before root-budget rejection, bounded by trusted source/sanitizer structure |
| Root retention | Initial roots could exceed N; mapping collections grew before closure enforcement | At most N retained members; new-root check precedes insertion and corresponding mapping updates |
| Dependency retention | N+1 insertion before count failure | Failure before insertion, with retained count N; no N+1 visit-state insertion |
| Closure snapshot/state | Allocated after any oversized initial roots | Initial count validated first; one bounded snapshot and one traversal-state dictionary |
| Table copies/rebuilds | Copies of source tables, glyf MemoryStream growth/ToArray, loca and ordinary cmap allocations | Unchanged, still precede exact final-size check and may individually exceed a small MaximumGeneratedBytes |
| Final layout | Sorted table array and int offset dictionary | Same sorted array, long offset dictionary; slightly larger offset entries, no new table or font copies |
| Oversized final SFNT | Full array allocated and populated before rejection | Exact rejection before final array allocation; no output or resanitization on this failure |
| Successful final SFNT | Final array then resanitization/owned trusted output | Same arrays, bytes and ownership |

This is a final-output allocation bound, not an aggregate peak-memory ceiling. Table generation remains bounded by validated source/table/glyph limits; rebuilt glyf can add up to three padding bytes per retained glyph, and preserving original GIDs requires loca/metrics entries for unselected slots. Defaults remain sanitizer input 64 MiB, single table 32 MiB, tables 128, glyphs 65,535; subset retained glyphs 16,384 and generated bytes 64 MiB. Configured limits are not weakened. Caller-selected large valid limits do not guarantee available memory. Throughput, GC, large-font latency and peak-memory savings are **measurement required**; no percentage or benchmark claim is made.

## Preservation and real consumers

Successful tests compare every retained glyf slice to the original, empty ranges for unselected slots, unchanged maxp/hhea/hmtx/name/OS/2 tables, original GIDs/composite records and the complete SFNT checksum. Generated programs independently resanitize to the identical SHA-256. Exact-byte-budget and default-budget output bytes/hashes match. Ordinary A/C aliases preserve WinAnsi mappings and width 510; Unicode A/C aliases receive CIDs 1/2 mapping to GID 1 and width 510, with deterministic Identity-H encoding. Existing Type0 tests cover non-WinAnsi and supplementary-plane scalars.

Both PdfExternalTrueTypeSubsetResourceProjector.Create and PdfExternalType0TrueTypeResourceProjector.Create finish CreateSubset before CreateObjectAllocator/ReserveNext, creating font streams, or invoking the page-resource mutation projector. These projectors currently expose no subset-options injection; no artificial public option was added just for testing a tiny limit through Services. New limit failures propagate as InvalidDataException before resource projection or mutation. That specific limit-to-projector ordering is source-traced. Existing resource collision, ownership and successful projection tests execute the real paths.

PdfExternalExistingTextFontService.ChangeFont obtains the Type0 projection before content reencoding and ApplyExistingTextResourceChange. A subset rejection therefore creates no operation/history entry and does not require rollback of document changes. Existing Services tests cover external-font apply/undo/redo and resource-name collision preservation; Services/Editing production code is unchanged. B4A private trusted storage remains inaccessible publicly; subsetters retain the same single independent working copy, and no new public snapshot or borrowed callback path is introduced.

## Exact changes and validation

Production files (five):

- src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeCompositeClosure.cs: common distinct admission and initial-count check.
- src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeGlyphSubsetter.cs: root admission and generated budget passed to builder.
- src/BlackFlag.Engine.Fonts/Embedding/PdfUnicodeTrueTypeGlyphSubsetter.cs: admission before CID maps and generated budget passed to builder.
- src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeSfntBuilder.cs: exact widened layout and preallocation rejection.
- src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeSubsetOptions.cs: clarify existing limit XML documentation; no signature/model/default change.

Test files (two, both in tests/BlackFlag.Engine.Fonts.Tests/): PdfTrueTypeGlyphSubsetterCompositeTests.cs becomes partial and extends its tiny cmap fixture with B-to-2 and C-to-1 aliases; new PdfTrueTypeGlyphSubsetterCompositeTests.ResourceLimits.cs adds 44 regression cases. The existing fixture and tests remain cohesive and unchanged otherwise. The 716-line existing file was reviewed: it already combines six composite tests with shared synthetic-SFNT helpers; the new resource-limit partial uses those same helpers without duplicating a font builder. Both files remain below the hard ceiling; no arbitrary source-size split was made.

Documentation files: this report, the main repository audit, and normal DocGen Documentation-Index.md, Documentation-Health.md and ValidationReport.md outputs. Final status review confirms exactly 12 changed files: five production, two test and five documentation files.

| Validation | Result |
|---|---|
| Pre-correction root regressions | 2 failed as expected: neither subsetter threw |
| Focused composite/resource tests | 50 passed (44 new, 6 existing) |
| Focused Services font/projector/external-text tests | 79 passed |
| Complete Fonts | 72 passed |
| Complete Services | 319 passed |
| Complete Parsing | 219 passed |
| Supplemental B4A save/reopen | 6 passed: full/simple subset/Type0, each with table/stream xrefs |
| Full unmodified consolidated gate | Passed, exit 0: 1,989 tests across 20 projects; zero failed/skipped; build 0 warnings/errors |
| Integrated source-size / DocGen / whitespace | 1,039 source files passed; 189 documents, 0 errors/warnings/information; health 91.6% A-; repair 0 changes/files/diagnostics; git diff --check passed |

Generated-byte preflight and widened generated-layout arithmetic are the directly exposed correction authorized with POST-P0-005; leaving the final allocation behind a late guard would not satisfy the requested resource-bound review. They use the same two callers and shared builder, not a separate broad font cleanup. No additional finding or unrelated fix is claimed. Public signatures, exception families, dependencies, references, sanitizer contracts, font permissions and native/platform behavior are unchanged. No unsafe code, pooling, pinning or memory manager was added. Fonts.Platform, Rendering and SDK.Adapters are evaluated by the full gate; Parsing is additionally run for actual font save/reopen coverage.

Commands: dotnet test tests/BlackFlag.Engine.Fonts.Tests/BlackFlag.Engine.Fonts.Tests.csproj --configuration Release --no-restore (focused filter FullyQualifiedName~PdfTrueTypeGlyphSubsetterCompositeTests, then complete suite); the same Services project command with filter FullyQualifiedName~Fonts|FullyQualifiedName~PdfExternalExistingTextFont, then the full built Services suite; complete Parsing with --configuration Release --no-restore; git diff --check; powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1. The existing temporary B4AReopen.csproj was rerun against current built assemblies with dotnet run --configuration Release. Its missing scratch assets and sandbox NuGet.Config read restriction were resolved by an approved restore/run, with no repository dependency/reference change. All six real reopen cases passed. The 620-byte simple subset hash DB3C18D7CDA8BB5A3A565CD5E9EC57428DE22E718DEF1BCD2DE0690E64F99B52 and 900-byte Type0 hash ED2781F72F70F637D51CE459155383EB5DB7219078F87EC9686B66F11158A0AD match the historical B4A controls. Reopened font bytes, resource identities, Length1 and Type0 ToUnicode were verified.

## Stop and remaining work

POST-P0-006/007/008/009, P2 work, performance measurement candidates and unrelated provenance views remain untouched. POST-P0-015 remains closed. No commit, push, merge, tag or branch switch is performed. Stop after successful acceptance validation and await explicit user approval for another batch.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-13 | POST-P0-005 root/dependency admission, generated-size preflight and preservation/resource validation. |
