# Post-BF-1054 Batch B9 - Owned Corpus and Manifest Report

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-033 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1054 |
| **Last Updated** | 2026-09-24 |
| **Next Review** | 2026-12-24 |

## Disposition

**The owned corpus + manifest slice is complete.** The owned corpus, versioned manifest, read-only validator and focused tests are implemented. Following the user's successful Windows reruns on 2026-09-24, the final corpus/manifest focused result is **53 passed, 0 failed, 0 skipped**; the complete B9 suite result is **171 passed, 0 failed, 0 skipped (171 total)**. The previously blocked symbolic-link fixture test passed in a context permitted to create the fixture. The test was preserved without skipping or weakening it. No production behavior or corpus oracle discrepancy was exposed.

**Overall B9 is not complete; POST-P2-002 remains open.** No corpus replay or fuzz engine ran. This report supplements PP-AUD-029 through PP-AUD-032; their earlier checkpoint records and the repository audit remain unchanged.

## Starting inventory and scope

Branch and HEAD at start: `maintenance/post-BF-1054-repository-review`, **93865f639acf96f0b9598c97b04d49cea38e8260**. The tree already contained **42 modified/untracked files: six modified tracked files and 36 untracked files**. The tracked changes were .gitignore, PiratePDF.slnx, docs/Documentation-Health.md, docs/Documentation-Index.md, docs/ValidationReport.md and docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md. Untracked work comprised PP-AUD-029/030/031/032 reports, dotnet-tools.json and the Fuzz.Tests, Fuzz.Targets, Fuzz.Runner and Fuzz.SharpFuzz directories. The existing test baseline was 118.

`tests/FuzzCorpus` did **not** exist. Source inspection confirmed exactly **filter-asciihex, filter-flate, font-sanitize**, all configuration **1**, all SupervisedManagedWorker. Their input caps remain 16 KiB, 16 KiB and 64 KiB respectively. No proposed/unimplemented target received a directory, seed or registration.

Root AGENTS.md, PROJECT-CHARTER.md, the four existing B9 reports, engineering/testing guidance, BF-1049A shared security limits, actual target adapters, manifest-relevant Runner code and owned fixture sources were consulted. Existing work was preserved; no reset, clean, revert, commit, push, merge, tag or branch switch occurred.

## Corpus and independently selected oracles

There are **15 persisted seeds**, totaling **9491 bytes**:

| Target | Seeds |
|---|---:|
| filter-asciihex | 6 |
| filter-flate | 6 |
| font-sanitize | 3 |

Categories: valid-minimal **6**, malformed-structural **2**, truncated **2**, resource-boundary **4**, historical-regression **1**, numeric-boundary **0**. Not every category is needed for every target.

Origin is **repository-owned for all 15**. External, private, user-document, system-font, downloaded and unknown-license counts are **zero**. The manifest says "Repository-owned synthetic fixture; no external license is asserted." This does not invent a project license; the Charter's separate license-selection decision is preserved.

Expectations were chosen from existing contracts **before generation**, not inferred by invoking a target and recording its result:

- ASCIIHex uses the exact owned cases Empty, InvalidCharacter, MissingTerminator, OddNibble, TrailingData and Whitespace in `tests/BlackFlag.Engine.Filters.Tests/Corpus/PdfTextFilterCorpus.cs`. In particular, trailing non-whitespace is rejected; it was not assumed to be ignored.
- Flate empty-member bytes and validity come from `tests/BlackFlag.Engine.Filters.Tests/PdfFlateCompletenessTests.cs#CompleteEmptyMemberRemainsValid`. Hello and its last-byte truncation come from `tests/PiratePDF.Fuzz.Tests/ManagedTargetTests.cs#FlateDecodesOwnedInputAndRejectsTruncation`, consistent with the permanent proper-prefix tests and F3-OBS-01.
- Flate decoded exact/over and expansion cases use `ManagedTargetTests.cs#FlateUsesDecodedByteLimitAndExpansionRatio`. The former repeat the Random(1049) 4,096-byte block to 32,768/32,769 decoded bytes; both encoded lengths are 4,407, keeping expansion below 32 so the decoded-byte boundary is isolated. The 4,096-zero-byte case encodes to 26 bytes, exceeding ratio 32. These preserve B1 / POST-P0-001 intent.
- The valid SFNT comes directly from the already linked `tests/BlackFlag.Engine.Fonts.Tests/TestFontFactory.cs#CreateMinimalTrueType`: one glyph, independently authored required tables and coherent checksums. No system-font lookup or sanitized production output is used.
- The 33-table header follows `ManagedTargetTests.cs#FontSanitizerUsesDomainRejectionsAndConfiguredTableCeiling`, above the target's 32-table cap.
- The eleven-zero-byte SFNT header is independently authored in `CorpusSeedRecipes.TruncatedHeader`. The sanitizer's established source-length-before-header-read guard rejects fewer than twelve bytes as MalformedHeader; this is the same guard underlying existing short-header regressions.

The manifest stores expected **success** or **rejected** categories, with the specific intended failure explained in purpose text. It does not store a newly observed production result as an oracle. This slice validates metadata, identity and generation only; execution of these corpus files remains deferred.

## Exact seed ledger

| Repository-relative path | Bytes | SHA-256 | Expected category | Related finding/work |
|---|---:|---|---|---|
| `tests/FuzzCorpus/filter-asciihex/asciihex-empty.bin` | 1 | `62B67E1F685B7FEF51102005DDDD27774BE3FEE38C42965C53AAB035D0B6B221` | success | BF-1040 |
| `tests/FuzzCorpus/filter-asciihex/asciihex-invalid-character.bin` | 5 | `F001FDD680B3D864964A8F89FA1C87CF1E023DADD1E53CA0298F1CDBA637704D` | rejected | BF-1040 |
| `tests/FuzzCorpus/filter-asciihex/asciihex-missing-terminator.bin` | 10 | `53550CB11AD44FB2BE20148386008A98A3F2A7F397E937DE956D7CFD846BCD7C` | rejected | BF-1040 |
| `tests/FuzzCorpus/filter-asciihex/asciihex-odd-nibble.bin` | 2 | `0AA65EAF6D4238AC157CFE34671E1BA9479BC12F026074EBDF11C1AD77F994AF` | success | BF-1040 |
| `tests/FuzzCorpus/filter-asciihex/asciihex-trailing-data.bin` | 4 | `FC449BDFF908E6A5848B19124B85BD6063DA9839F434E1DF1B6C5FA380A38479` | rejected | BF-1040 |
| `tests/FuzzCorpus/filter-asciihex/asciihex-whitespace.bin` | 17 | `E906EEF435D0A766CF5C63E5EEFB9B292F082613461028ED143EB2838920F5AB` | success | BF-1040 |
| `tests/FuzzCorpus/filter-flate/flate-decoded-exact.bin` | 4407 | `8433795C56D9F66E2F245EBA161E0B2C1D1AB316BC3E31FFD33A7270235A7DDD` | success | POST-P0-001 |
| `tests/FuzzCorpus/filter-flate/flate-decoded-over.bin` | 4407 | `2FB48465387A66D03462AABEF6E1D11E35D5463558F9A9F7F0819EAE369A533C` | rejected | POST-P0-001 |
| `tests/FuzzCorpus/filter-flate/flate-empty.bin` | 8 | `ACA503DEF43DC086A5AAA5C3D6F86BCBBC2AA6A80007DDEA4A81BCE2B376FCEB` | success | F3-OBS-01 |
| `tests/FuzzCorpus/filter-flate/flate-expansion-over.bin` | 26 | `77B1F27DFE4747505C3676510175FAD1E048EC0413DF6E848FF024D4E110C3A8` | rejected | POST-P0-001 |
| `tests/FuzzCorpus/filter-flate/flate-hello.bin` | 13 | `589A31FA3790A670E020C0C8502CBDAFB87D0B470FE29B3BA18598C466895FFB` | success | F3-OBS-01 |
| `tests/FuzzCorpus/filter-flate/flate-truncated.bin` | 12 | `3DB8C1010A538BBD067E72AC708B4B8237F5103CC1C85E73B67C9D2BED89C2D2` | rejected | F3-OBS-01 |
| `tests/FuzzCorpus/font-sanitize/font-minimal.bin` | 556 | `40222B2C3E2E801FF12A412C7590F9A2C9B84A885A585B6B59675D5E09FEB04A` | success | BF-1054 |
| `tests/FuzzCorpus/font-sanitize/font-too-many-tables.bin` | 12 | `B9978A78BFA4A6132A732C95E8BE7157A72A77F8564F4BD2B16C8FA1EC803DF9` | rejected | BF-1054 |
| `tests/FuzzCorpus/font-sanitize/font-truncated-header.bin` | 11 | `71B6C1D53832F789A7F2435A7C629245FA3761AD8487775EBF4957330213A706` | rejected | BF-1054 |

## Schema and parsing bounds

`manifest.json` has schema **piratepdf-fuzz-corpus**, version **1**. The root permits exactly schema, version and seeds. Every seed permits exactly id, target, configurationVersion, path, length, sha256, sourceRecipe, origin, rights, category, purpose, expectedOutcome and related. Every field is required; extra/duplicate properties are rejected, rather than using last-value-wins interpretation.

| Bound / vocabulary | Contract |
|---|---|
| Manifest bytes | **131,072** before JSON parsing, with bounded overflow probing. |
| JSON nesting | **4** maximum depth; no type metadata, comments or extensions. |
| Seeds | **1-128** entries, checked before per-entry materialization. |
| Filesystem inventory | **256** total enumerated entries across the root and immediate target directories. No recursive discovery. |
| Seed/target IDs | At most **64** characters; seed IDs start a-z and otherwise contain a-z, 0-9 or hyphen. Targets must resolve in TargetRegistry. |
| Seed path | At most **200** characters and exactly tests/FuzzCorpus/&lt;target&gt;/&lt;seed-id&gt;.bin, ordinally. |
| SHA-256 | Exactly **64 uppercase hex** characters. |
| Source recipe / purpose / rights | At most **256 / 256 / 160** characters; nonempty, no control characters. Source starts tests/ and excludes backslashes, colon and traversal. |
| Classifications | At most **32** characters plus closed vocabularies: repository-owned origin, success/rejected outcome, and the six task-specified categories. |
| Related references | At most **4**, each at most **64** uppercase ASCII letters/digits/hyphens. |
| Seed byte limits | The selected registry target's actual ceiling. Declared and actual oversized seeds reject. |
| Ordering | Manifest paths strictly increase under StringComparer.Ordinal. Global seed IDs and seed paths are unique. Filesystem entries are bounded before sorting ordinally. |

The parser validates integer types/ranges, schema, exact target configuration, canonical paths and bounded metadata. The validator checks inventory, missing/unlisted files, actual lengths and hashes. A failure publishes no partially validated seeds. Its normal diagnostic JSON contains only a fixed failure code and seed count (tested at no more than 100 UTF-8 bytes), never paths, input bytes or exception messages.

The successful result is a read-only metadata snapshot, **not** a lease on filesystem bytes or a security capability. A future replay reader must recheck admitted bytes/identity after changes; this slice does not implement that reader.

## Filesystem policy and platform limits

Manifest and seed reads reuse the existing bounded InputFile reader, including its regular-file, local-path and reparse checks. Consequently this validator currently **fails closed outside Windows** with UnsupportedPlatform. Worker admission remains unchanged; no Linux/macOS replay readiness is claimed.

Canonical manifest paths exclude absolute/rooted/UNC paths, traversal, alternate separators, duplicate separators, case aliases, ADS syntax, dot segments and encoded traversal forms. File paths used for reading are assembled from validated target/ID components. Inventory examines only the root and one target-directory level, rejects observed reparse/device attributes before entering directories, rejects unexpected directories/files, and admits only manifest.json and the corpus-local .gitattributes as root metadata. All unlisted .bin files are rejected, regardless of Git staging state.

BCL reparse checks are conservative snapshots, not an atomic filesystem sandbox. Concurrent parent substitution and general physical-link confinement cannot be proven; hard links are not distinguished by these checks. No new native API was added to claim stronger protection. The user's successful privileged rerun confirms the intended real symbolic-link escape rejection for the tested fixture; it does not establish stronger filesystem containment or remove these caveats.

The corpus-local .gitattributes marks **all .bin files -text** to prevent Git newline conversion from changing intentional ASCIIHex bytes; manifest JSON uses LF. `git check-attr text -- tests/FuzzCorpus/filter-asciihex/asciihex-whitespace.bin` confirmed text is unset. This small metadata file is intentionally outside the seed manifest.

## Generation and reproducibility

`CorpusSeedRecipes.cs` provides the fixed owned byte recipes and preselected oracle categories. A separately invoked `generate-owned-corpus <repository-root>` branch in the existing Fuzz.Tests executable writes a new corpus and refuses to overwrite an existing corpus directory. It generates input bytes and hashes only; it never invokes target adapters, decoders or sanitizers to determine expectations.

The explicit generation command used was:

```powershell
dotnet tests/PiratePDF.Fuzz.Tests/bin/Release/net10.0/PiratePDF.Fuzz.Tests.dll generate-owned-corpus C:\Development\PiratePDF
```

This persisted intentional generated inputs, not copies of compiled test artifacts. Ordinary validation/tests do **not** rewrite or automatically regenerate accepted files. Tests generate owned bytes in memory twice, compare both to persisted bytes and SHA-256/length, and independently compare the manifest categories to the recipe's preselected expectations.

Generation is offline, uses no system fonts, timestamps or machine randomness. Compressed recipes use existing BCL ZLibStream/SmallestSize, as the source regressions do; the boundary payload uses the fixed Random(1049) fixture. Byte identity was established on **.NET 10.0.10 / SDK 10.0.302**. Compression encoding may change with a different runtime implementation; a future mismatch requires review and must not automatically rewrite seeds or expectations. Persisted bytes/hashes remain authoritative.

## Exact files changed by this slice

**24 files** relative to the starting dirty tree:

- Added Runner tooling: CorpusManifest.cs, CorpusManifestParser.cs, CorpusValidator.cs under tools/PiratePDF.Fuzz.Runner.
- Added tests/PiratePDF.Fuzz.Tests/CorpusSeedRecipes.cs and CorpusManifestTests.cs.
- Modified tests/PiratePDF.Fuzz.Tests/SupervisorTestChild.cs only to add the explicit generation entry point.
- Added tests/FuzzCorpus/manifest.json, tests/FuzzCorpus/.gitattributes and the **15 seed paths** listed above.
- Added docs/reviews/Post-BF-1054-Batch-B9-Owned-Corpus-and-Manifest-Completion-Report.md.

No production implementation/API, dependency, friend declaration, project reference, target registration/implementation, existing worker/supervisor behavior, CI or existing report/audit changed. Scope comparison confirmed the other **41 pre-existing modified/untracked files** were unchanged and no pre-existing file disappeared.

## Actual validation and resolved fixture blocker

| Check | Actual result |
|---|---|
| New corpus/manifest tests | Final: **53 passed, 0 failed, 0 skipped (53 total)**, reconciled from the user's successful focused link test and complete suite reruns. |
| Complete Fuzz.Tests Release suite | Final: **171 passed, 0 failed, 0 skipped (171 total)**, reported by the user; previous 118 preserved plus 53 new cases. |
| Runner Release build | Passed, **0 warnings/errors**. |
| Test project Release build | Passed, **0 warnings/errors**. |
| git diff --check | Passed; new text files additionally checked. Intentional binary seed whitespace is governed by -text. |
| New seeds / generated-oracle comparison | Persisted inventory/hash/length, recipe byte identity and preselected category comparisons passed. No target replay performed. |
| Full release gate | Not run, per scope; no production/repository-wide issue exposed. |

Commands:

```powershell
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CorpusManifestTests --verbosity quiet
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --verbosity quiet
dotnet build tools/PiratePDF.Fuzz.Runner/PiratePDF.Fuzz.Runner.csproj -c Release --no-restore --verbosity quiet
git diff --check
```

New tests cover the committed manifest/inventory, recipe reproducibility, repeat results, readonly publication, hash/length/missing/unlisted/oversize seeds, schema/configuration/ID errors, duplicate IDs/paths/properties, ordering, closed classifications/schema, manifest size/depth/count bounds, filesystem enumeration bound, bounded text/diagnostics, canonicalization/root/traversal cases, cancellation, refused regeneration and real symbolic-link escape rejection.

Historical validation: the earlier unelevated attempt reported **52 passed, 1 failed, 0 skipped** for the corpus/manifest tests and **170 passed, 1 failed, 0 skipped** for the complete suite. The failure was **CorpusManifestTests.SeedLinkEscapeIsRejected**: Windows File.CreateSymbolicLink reported **"A required privilege is not held by the client"** during fixture creation, before the validator was invoked. A retry outside the tool sandbox failed identically. The user's initial manual focused retry also reported the same privilege error: **1 failed, 0 succeeded, 0 skipped**. No test was skipped, no privilege was enabled by the tooling, no OS policy was changed by the tooling and no replacement native API/dependency was introduced.

Blocker closure on **2026-09-24**: the user subsequently reported that both commands below passed in a Windows context permitted to create the symbolic-link fixture. This is user-reported validation evidence, not a new agent-executed test run. The focused rerun passed **CorpusManifestTests.SeedLinkEscapeIsRejected**, and the complete suite passed **171/171**, including all **53/53** corpus/manifest cases. Successful fixture creation now allowed the validator to run and confirmed the intended real symbolic-link escape rejection behavior for that fixture. The fixture validation blocker is closed; the containment limitations above remain unchanged.

```powershell
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CorpusManifestTests.SeedLinkEscapeIsRejected --verbosity normal
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj -c Release --no-restore --verbosity normal
```

The v1.0 reconciliation changed only this report. Implementation, tests, corpus files, PP-AUD-029 through PP-AUD-032 and the repository-wide audit were not modified. `git diff --check` passed for this documentation-only update; no implementation work or tests were rerun.

No expected outcome was changed to make a test pass. No replay, mutation, SharpFuzz execution, libfuzzer-dotnet execution, instrumentation, fuzz campaign or CI change occurred. POST-P2-003 and later P2 work, canonical font editing and measurement-only P1 work remain untouched. **POST-P2-002 remains open.**

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-09-24 | Record 15 owned seeds, bounded manifest validation and 53 tests; preserve the unresolved Windows symbolic-link fixture privilege failure. |
| 1.0 | 2026-09-24 | Close the symbolic-link fixture validation blocker using the user's successful permitted-context reruns; record 53/53 focused and 171/171 complete tests and completion of this slice, preserving historical failure evidence and containment caveats. |
