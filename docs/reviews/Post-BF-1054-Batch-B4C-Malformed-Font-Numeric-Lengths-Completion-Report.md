# Post-BF-1054 Batch B4C - Malformed Font Numeric Lengths

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-023 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-14 |
| **Next Review** | 2026-12-14 |

## Scope, baseline and disposition

The user approved B4C / POST-P0-006: overflow-safe hostile font numeric validation, established sanitizer-domain failure and necessary malformed-candidate isolation. Starting branch: `maintenance/post-BF-1054-repository-review`. Starting commit: **18d36b265e4e6a754718e1cebf9dc29dda558b31**, Enforce TrueType subset resource limits. The tree and diff were clean; B4B was committed after POST-P0-015. Root AGENTS.md, charter, README, the current audit, B4A/B4B reports, BF-1054I/R contracts, ADR-0010, engineering/security guidance and the consolidated gate were read. POST-P0-005 and POST-P0-015 remain closed.

**Disposition: POST-P0-006 is implemented and closed. The full unmodified release gate passed 2,046 tests across 20 projects with zero build warnings/errors.** No POST-P0-007, POST-P2-001 or other subsequent batch was started. Invoke-PiratePDFReleaseGate.ps1 remains authoritative and unchanged; Test-PiratePDFSourceFileSizes.ps1 remains intentionally absent. Historical reports are unchanged.

## Baseline reproduction before production edits

Eight sanitizer regressions and one real mixed-directory provider regression failed before production changes. Fixtures are generated deterministically, checksummed through existing test SFNT builders and small: cmap bodies use tens of bytes; the separate 4,096-table search-field probe uses a 65,548-byte directory buffer with permissive MaximumTableCount. No installed fonts, random fuzzing or enormous allocations were used.

| Case | Baseline result and stage | Trusted result/publication |
|---|---|---|
| Format 12 length uint.MaxValue | Exact System.OverflowException, `Arithmetic operation resulted in an overflow.`, inner null, ValidateCmapSubtable checked cast after checksums | No result |
| Format 13/14 length uint.MaxValue | Same OverflowException/message/inner at the analogous length cast | No result |
| Tiny valid font, MaximumInputBytes=int.MaxValue | Exact System.ArgumentOutOfRangeException from MemoryStream.Read; `Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection. (Parameter 'count')`; inner null | No result; failure during initial bounded read |
| Format 12/13/14 count uint.MaxValue in a minimal declared header | No exception: outer length passed and count structure was never checked | Sanitized result incorrectly returned |
| SFNT numTables=4096 with sufficient directory bytes and MaximumTableCount=4096 | Exact OverflowException with the same arithmetic message and inner null, ValidateSearchFields narrowing power*16 to ushort | No result |
| Bad format-12 font plus a valid file | GetFaces throws the same sanitizer OverflowException through BuildSnapshot/Lazy; deleting the bad file and calling the same provider again still throws | No face list returned; Lazy caches the failed construction |

The new tests first asserted the intended corrected outcomes, so baseline failures were expected evidence, not tests weakened to pass. Test output captured type/message/inner. The provider rethrow's stack reached the second GetFaces call after removal of the bad file. The original source-traced catalog-poisoning concern is now dynamically reproduced.

## Domain and compatibility contract

PdfExternalFontSanitizationException is a sealed IOException with a PdfExternalFontSanitizationFailure property. The existing Reject helper constructs it directly with category and actionable message; it has no inner-exception constructor and these failures have null InnerException. The sanitizer does not broadly catch or normalize arbitrary exceptions. Invalid caller options remain ArgumentOutOfRangeException with their existing property parameter names, ActualValue and positive-limit rule. Unreadable input, unsupported containers, malformed headers, malformed required tables and resource limits retain distinct categories.

After correction, oversized cmap lengths fail with MalformedRequiredTable and `cmap format F declares an invalid length.` Short format-12/13/14 headers or impossible record arrays fail with MalformedRequiredTable and `cmap format F has a truncated header or record array.` A format-14 nested offset beyond its subtable fails with MalformedRequiredTable and `cmap format 14 variation-table offset lies outside its subtable.` Search-field values that cannot equal their ushort fields fail with MalformedHeader and the existing inconsistent searchRange/entrySelector/rangeShift message. Inner exceptions remain null. Tests assert exact type/category and relevant invariants, not newly mandated exact prose.

The intentional compatibility changes are domain rejection in place of incidental overflow/range exceptions, rejection of previously accepted truncated/count-impossible 12/13/14 structures, and successful handling of valid extreme input options. Existing invalid-option exceptions, unsupported format policy and valid font bytes remain. No public signature, failure enum, dependency, project reference or friend-assembly change is made. No catch clause was added to either production assembly.

## Numeric inventory and arithmetic proof

| Site | Bound/proof and disposition |
|---|---|
| ReadBounded request | Corrected: widen maximumBytes before subtracting total and adding the one-byte EOF probe; clamp to the existing 81,920-byte buffer before narrowing. MaximumInputBytes=int.MaxValue produces a valid positive read request. |
| ReadBounded accumulation | Corrected: reject read > maximumBytes-total before adding to int total or writing/growing the destination. This also protects the theoretical probe at an int.MaxValue total. Exact-limit input can perform its final one-byte EOF probe; limit+1 is InputTooLarge. |
| SFNT directory count/end/indices | ushort table count bounds 12+count*16 and index*16 within int. Directory length and configured count are checked before allocation/iteration. Unchanged. |
| Search fields | Corrected: expected searchRange/rangeShift are computed as int, then compared directly to input ushort fields. Unrepresentable expectations cannot compare equal, so they reach the existing domain rejection without a narrowing exception. All ushort-count products fit int. |
| Table offset/length | Unsigned fields are checked against the positive single-table option, alignment, directory end and actual source length. Remaining-length comparison proves both casts safe before records, slicing and checksums. Existing mixed int/uint arithmetic widens; no wraparound end calculation. Unchanged. |
| Overlap | Previous offset+length already widens to long; source range checks also establish representability. Unchanged. |
| head/maxp | Header sizes precede reads. loca format is restricted to 0/1; positive glyph count is ushort and capped. Unchanged. |
| hhea/hmtx | Metric count is ushort, positive and no larger than validated glyph count. Four-byte metrics plus two-byte bearings require at most 262,140 bytes, safely within int. Unchanged. |
| loca/glyf | At most 65,536 entries, width 2/4; required bytes at most 262,144. Short entries widen before multiplying by two. Monotonic uint offsets must fit actual glyf before later slicing. Unchanged. |
| cmap directory | ushort count, configured cap, checked 4+8*count and range before records. uint subtable offset is compared to actual table.Length-2 before cast/slice. Unchanged. |
| cmap declared lengths | Corrected across existing unsigned formats 8/10/12/13/14: keep uint through enclosing-range validation; only narrow after proving it fits the managed span. Legacy ushort fields use the same comparison. |
| cmap 12/13 groups | Corrected: require 16-byte header and uint count <= (declaredLength-16)/12. No multiplication, count narrowing, allocation or group-proportional loop precedes proof. |
| cmap 14 selectors | Corrected: require ten-byte header and uint count <= (declaredLength-10)/11 before int narrowing and selector traversal. Record offsets then fit the validated declared span. |
| cmap 14 nested tables | Zero offsets remain optional. Otherwise prove offset <= declaredLength-4 before narrowing/slicing. The four-byte count must fit remaining bytes divided by stride 4 (default ranges) or 5 (non-default mappings). No loop traverses the hostile nested count. |
| name | ushort counts/offsets/lengths; 6+12*count and stringOffset+offset fit int. Existing record/string-storage/range validation precedes reads; configured count checked first. Unchanged. |
| OS/2, CFF/CFF2 | Existing minimum length precedes field/header access; byte-sized header lengths are checked against the table. No rights interpretation changes. |
| Checksums/canonical output | Existing source-validated record ranges and compact layout remain; checksum uint wrap is intentional. Source copies, canonical allocation and hash behavior unchanged. No giant real-buffer exhaustion tests were attempted; accepting a large configured limit does not guarantee available memory. |

The search-field issue is an additional equivalent hostile-numeric failure reproduced during this inventory and fixed under the explicit B4C authorization. Format-12/13/14 impossible record counts were also directly reproduced as publication of incomplete structure. Their range validation is necessary for the requested sanitizer boundary; merely replacing length casts would leave these requested cases accepted. These are recorded within POST-P0-006 rather than claiming unrelated audit findings.

## Cmap policy, fixtures and work bounds

The [Microsoft OpenType cmap specification](https://learn.microsoft.com/en-us/typography/opentype/spec/cmap) establishes the field widths, header sizes and record strides used here. Format 12 uses sequential glyph mappings; format 13 uses constant mappings but shares the group-record size. Format 14 has selector records and optional offsets to separately counted default/non-default tables. B4C validates numeric structure; it does not add mapping, shaping or variation-sequence interpretation.

The deterministic matrix covers minimum headers/zero counts, every proper header prefix, short declarations, exact lengths, one-over lengths, int.MaxValue, int.MaxValue+1 and uint.MaxValue; valid one-group 12/13 tables, counts near signed/unsigned multiplication thresholds, missing final group bytes and data outside a shortened declaration. Format 14 covers selector-count extremes, exact selector boundary, optional zero offsets, huge/out-of-range nested offsets, exact nested header/record boundaries, default/non-default strides, huge nested counts and one-byte truncations. Enclosing trailing bytes are still allowed; they cannot satisfy records outside the declared subtable.

Accepted structural formats remain 0/2/4/6/8/10/12/13/14. New record-array validation is focused on 12/13/14 as requested. Existing outer-length handling for other formats remains, with the unsigned narrowing correction also covering 8/10. This is not a full semantic/font-conformance validator: glyph mapping semantics, ordering/Unicode rules and deeper legacy format validation are not redesigned. Existing metadata/subset interpreters support 0/4/12; structurally valid 13/14 acceptance does not imply newly supported rendering/catalog mapping. A sanitizer-accepted format-13-only font is intentionally omitted by the unchanged provider's unsupported-mapper policy.

All length/count failures use metadata from tiny real arrays, with no allocations proportional to hostile declared values. The original bounded source read, initial MemoryStream capacity, work buffer and source ToArray still precede structural validation. Rejected cmap data now fails before canonical SFNT creation, trusted publication, inspector materialization or provider entries. Formats 12/13 add O(1) count checks. Format 14 performs O(number of validated selector records) work and O(1) nested size checks per selector, with no new buffer/copy/cache. Repeated references do not trigger nested-count traversal. The new selector loop checks the existing cancellation token per record; original read/directory cancellation remains. Aggregate catalog cancellation/limits remain POST-P2-001 and were not implemented.

## Provider and Lazy behavior

BF-1054R requires best-effort omission of malformed, unreadable and unsupported individual candidates. The existing BuildSnapshot filter already includes PdfExternalFontSanitizationException/IOException, InvalidDataException, UnauthorizedAccessException and NotSupportedException. Correcting the sanitizer is sufficient; **PdfFileSystemExternalFontProvider.cs is unchanged**. No incidental CLR exception list or broad catch was added.

After correction, tests cover valid-only, malformed-only, bad-before-valid, valid-before-bad, multiple bad plus valid, unsupported-but-sanitizer-accepted format 13 plus valid, and multiple valid faces. One file per explicitly ordered search directory makes encounter-order controls deterministic. Duplicate directory inputs remain deduplicated. Only complete valid metadata appears, with expected family/face names, weight, glyph coverage and stable sorted face IDs. Entries remain local until the complete ProviderSnapshot is returned. Failed candidate metadata is never inserted.

Repeated calls use the completed snapshot. Deleting source candidates after construction does not trigger a rebuild or change cached metadata. The original malformed-candidate exception no longer reaches Lazy, so it cannot poison that snapshot. Injected ordinary IO/domain/unsupported failures are omitted using the existing sanitizer seam. Injected cancellation, programming and OutOfMemoryException objects propagate and retain the existing Lazy-cached failure behavior; the latter test throws a synthetic exception and allocates no excessive memory. This proves the failure filter stays narrow, not that all Lazy failures should be retried.

Pre-cancelled GetFaces rejects with the original token before initialization; a subsequent uncancelled call succeeds. The provider still does not forward that token into BuildSnapshot: changing that aggregate/cancellation policy is expressly excluded. Sanitizer pre-cancellation and cancellation during deterministic short reads retain OperationCanceledException/token and leave caller streams open. Invalid options still fail before any stream reads.

## Valid fonts, ownership and subsets

Canonical valid inputs compare byte-for-byte through sanitizer output and resanitization, including minimal/one-group format 12/13, format-14 structures and supported CFF/CFF2 headers. These CFF tests preserve the existing header-validation support level, not a claim of deep CFF conformance. Complete Fonts runs include B4A public-byte/reader isolation and hash preservation plus all B4B ordinary/Unicode root/dependency/generated-byte limits, format-4 subset output and resanitization tests. No trusted storage, metadata interpretation, ownership or subset production code is changed.

Complete Services validates real external-font projection, mapping/metrics, collisions, transactions, rollback, undo/redo and ownership. The existing supplemental B4AReopen executable was rerun against current built assemblies: six full/simple-subset/Type0 combinations with table/stream xrefs passed. Reopened resource identities, Length1, ToUnicode, exact embedded bytes and resanitization match. Historical hashes remain: full 1300 bytes C364857A62DEC149E4175CD5E43375383D3E3D13E450129D938F448077D37D40; simple subset 620 bytes DB3C18D7CDA8BB5A3A565CD5E9EC57428DE22E718DEF1BCD2DE0690E64F99B52; Type0 900 bytes ED2781F72F70F637D51CE459155383EB5DB7219078F87EC9686B66F11158A0AD.

## Exact files and validation

Production (two):

- src/BlackFlag.Engine.Fonts/Sanitization/PdfOpenTypeFontSanitizer.cs: safe read budget/accumulation, int search-field comparison and token forwarding.
- src/BlackFlag.Engine.Fonts/Sanitization/PdfOpenTypeFontSanitizer.Cmap.cs (new): existing cmap responsibility plus unsigned length and bounded record validation, sharing the same private readers/Reject helper through a partial class.

The sanitizer previously had 930 lines. Keeping added cmap validation there would exceed the source ceiling. Extracting the cohesive cmap responsibility leaves the main file at 844 lines and its cmap partial at 159; the main remains explicitly reviewed above 800 because it still owns sanitizer orchestration, required tables and canonical output. This is one existing sanitizer, not a new public abstraction or arbitrary line split. The 620-line provider test file remains a cohesive existing fixture; only partial declaration and an optional cmap fixture parameter changed.

Tests (five):

- tests/BlackFlag.Engine.Fonts.Tests/PdfOpenTypeFontSanitizerNumericTests.cs (new): baseline regressions and shared checksummed fixtures.
- tests/BlackFlag.Engine.Fonts.Tests/PdfOpenTypeFontSanitizerNumericTests.Cmap.cs (new): unsigned/group/variation boundary matrices.
- tests/BlackFlag.Engine.Fonts.Tests/PdfOpenTypeFontSanitizerNumericTests.InputAndTables.cs (new): input/options/cancellation/other numeric controls and CFF headers.
- tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFileSystemExternalFontProviderTests.cs: reuse existing fixture for caller-supplied cmap.
- tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFileSystemExternalFontProviderTests.NumericFailures.cs (new): mixed-directory, complete snapshot, exception-filter and cancellation tests.

Documentation (five): this report, the main audit and normal Documentation-Index.md, Documentation-Health.md and ValidationReport.md generation. Final status confirms exactly twelve changed files: two production, five test and five documentation files. No existing test was removed or weakened. Fixtures remain in focused test helpers as reusable future fuzz seeds; the existing corpus helpers are for Filters, not fonts; there is no new fuzz dependency/infrastructure or corpus sweep.

| Validation | Result |
|---|---|
| Before production changes | 8 sanitizer regressions and 1 real provider regression failed as expected |
| Focused numeric sanitizer tests | 42 passed |
| Complete Fonts | 114 passed, including B4A/B4B |
| Complete Fonts.Platform | 18 passed, including 15 new provider cases |
| Complete Services | 319 passed |
| Complete Parsing | 219 passed |
| Supplemental actual save/reopen | 6 passed |
| git diff --check | Passed before full gate |
| Full unmodified release gate | Passed, exit 0: 2,046 tests across 20 projects; zero failed/skipped; build 0 warnings/errors |
| Integrated source-size / DocGen | 1,044 files passed; 190 documents with 0 errors/warnings/information; health 91.6% A-; repair 0 changes/files/diagnostics |

Commands use dotnet test tests/PROJECT/PROJECT.csproj --configuration Release --no-restore; the focused filter is FullyQualifiedName~PdfOpenTypeFontSanitizerNumericTests. Supplemental validation uses the existing temporary B4AReopen.csproj with dotnet run --configuration Release --no-restore, without repository project-reference changes. The authoritative acceptance command is powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1.

## Limits and stop condition

Throughput, latency and GC effects are **measurement required**. Directly countable changes are arithmetic/range checks and one bounded selector loop; source/canonical/ownership copy counts are unchanged. No large allocations are necessary to reject huge declarations. This does not promise an aggregate peak-memory bound or complete font semantic validation.

POST-P0-005 and POST-P0-015 remain closed. POST-P0-007, POST-P0-008/009, POST-P2-001/002 and all other excluded findings remain untouched. No public API, dependency, reference, friend assembly, unsafe/native mechanism, pooling, pinning or memory manager is added. No commit, push, merge, tag or branch switch is performed. Stop after successful full validation and wait for explicit approval of another batch.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-14 | B4C numeric-length/domain hardening, bounded cmap structure and malformed-candidate isolation evidence. |
