# Post-BF-1054 Pre-B4 Expression and Materialization Review

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-014 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1047, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-08 |
| **Next Review** | 2026-12-08 |

## Scope and baseline

The user authorized a repository-wide expression, materialization, deconstruction and variable review before B4, with narrow behavior-preserving cleanup. The starting tree was clean on `maintenance/post-BF-1054-repository-review` at **`7b3e2f64f3b0ce2128844b0c2f62891bc9d8bcbd`**. Branch, status, recent history and diff were checked before editing. B1, B2, B3, B3A and the subsequent user-change reconciliation were already incorporated in this baseline.

Root `AGENTS.md`, `PROJECT-CHARTER.md`, `README.md`, engineering standards, bounded-materialization ADR-0010, relevant security/preservation context, the [current audit](Post-BF-1054-CSharp-Repository-Audit.md), and the [B1](Post-BF-1054-Batch-B1-Completion-Report.md), [B2](Post-BF-1054-Batch-B2-Completion-Report.md), [B3](Post-BF-1054-Batch-B3-Completion-Report.md) and [B3A](Post-BF-1054-Batch-B3A-CMap-Tokenizer-Completion-Report.md) completion reports governed the review. Their historical inventories and acceptance results remain historical records.

**B4A, B4B, B4C, canonical font editing and all later findings did not begin.** No public API, dependency, project reference, resource ceiling, security boundary, filter policy, transaction or PDF preservation behavior changed. No commit, push, merge, tag or branch switch was performed.

## Coverage and counting method

Every one of the **1,034 tracked hand-authored C# files** was included, including production, tests, tooling, reference-plugin code and ten hand-authored `Properties/AssemblyInfo.cs` files. The latter are not generated even though the source-size gate excludes that filename. No tracked vendor/generated C# was identified. The physical inventory also found **224 ignored build-generated C# files under `obj`**: 84 assembly-information files, 84 target-framework assembly-attribute files and 56 generated global-using files. These build outputs were excluded from manual editing; no C# was found under `bin`.

The review used full-file Roslyn syntax parsing with the installed SDK, a per-file inventory, repository searches, and contextual inspection of materialization producers/consumers, tuple uses and variable candidates. All 1,034 syntax trees parsed without errors. The temporary inventory utility used SDK assemblies outside the repository; it introduced no dependency or project change. Fixed literal/test-fixture constructions were reviewed as such; stateful, ownership, resource and mutation boundaries received surrounding-code inspection. This is a scoped expression review, not a new claim that every possible repository defect was audited.

Counts below describe the **starting commit**, before new characterization tests. A syntax node is counted once in its category; nested expressions are distinct nodes. Spread elements are a subset of collection expressions, not additional sites in the primary total. Conditional-access `?.ToArray()` calls are included. `ToArray` counts include span/memory/stream calls as well as enumerable materialization; those APIs were not presumed interchangeable.

| Area | Files reviewed | ToArray calls | Collection expressions | Spread elements | Deconstruction/pattern sites | Local declarators screened | Variable candidates reviewed |
|---|---:|---:|---:|---:|---:|---:|---:|
| Production | 712 | 194 | 260 | 69 | 26 | 3,160 | 30 |
| Tests and test plugin | 261 | 217 | 857 | 26 | 75 | 4,843 | 52 |
| DocGen tooling | 60 | 29 | 12 | 0 | 0 | 200 | 0 |
| Reference plugin | 1 | 0 | 0 | 0 | 0 | 0 | 0 |
| **Total** | **1,034** | **440** | **1,129** | **95** | **101** | **8,203** | **82** |

The **1,670 primary syntax/materialization sites** comprise 440 calls, 1,129 collection expressions and 101 deconstruction/pattern nodes. Collection expressions comprise **161 empty, 882 fixed-element and 86 spread-containing expressions**; those 86 expressions contain 95 spread elements. Deconstruction coverage comprises **95 tuple assignments/declarations/foreach uses and six positional-pattern clauses**. The 82 variable candidates are short identifier/member/literal initializers with one syntactic use in their containing scope; this is a conservative review shortlist, not a claim of compiler data-flow equivalence or a count of all `var` declarations. Contextual variable review also considered counters, offsets, snapshots, explicit numeric widths and paired values around the primary sites. Related `ToList`, `ToDictionary`, `ToHashSet`, `Clone`, copy operations and collection construction were cross-checked; these supporting searches are not added to the primary-node total.

The final appendix accounts for every starting C# file, including files with zero primary sites. No file needed a change merely to count as reviewed.

## Applied changes and exact hand-authored files

| File | Change and reason | Compatibility evidence |
|---|---|---|
| `src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceStreamParser.cs` | Replace `foreach ((_, int count) in ranges)` with `foreach (var range in ranges)` and use `range.Count`. The tuple's named member identifies the quantity being accumulated without extracting an unused first object number. | Same list iteration and value reads; checked addition, explicit `long` multiplication and length validation remain intact. The following loop still deconstructs both required fields. |
| `src/BlackFlag.Engine.Rendering/Fonts/PdfCMapParser.cs` | Retain the bounded tokenizer's `ToArray()` and add a short explanation of indexed lookahead and tokenizer progress/count limits. | Comment only; byte, token and range bounds and malformed-input exceptions are unchanged. |
| `src/BlackFlag.Engine.Services/Text/PdfTextAdvanceMeasurementService.cs` | Use `.Select(...).ToArray()` for the projection that advances `replacementIndex`; add a short comment explaining eager consumption before measurement. | `run.Elements` is an owned, read-only snapshot. The same selector visits elements in order once, accesses replacements only for text elements, and completes before decoding/cancellation checks in measurement. No intermediate list or policy is introduced. |
| `src/BlackFlag.Engine.Writing/PdfObjectWriteProjectionSet.cs` | Retain `[.. projections]`; explain why enumeration completes before validation and why sorting must use owned storage. | Comment only; constructor validation order, caller storage, projection identity, deterministic sorting and publication remain unchanged. |
| `tools/PiratePDF.DocGen/Parsing/MarkdownMetadataParser.cs` | Remove `.ToArray()` immediately after `string.Split`. `Split` already returns the newly owned array needed by the private row parser. | Same split call, options, strings, row order and empty-cell handling. The intermediate split array had no other reference; removing its clone cannot expose caller storage. |
| `tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfCMapTokenizerTests.cs` | Use `token.End` instead of deconstructing an unused `Value`. | Identical progress assertions and finite enumeration. |
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfTextAdvanceMeasurementServiceTests.cs` | Add three characterization cases for ordered replacement reads, mixed text/adjustment runs, pre-cancellation and exception precedence. Enable nullable annotations locally for the optional fixture exception. | Behavioral assertions; no syntax assertion or weakened expectation. |
| `tests/BlackFlag.Engine.Writing.Tests/PdfProjectedIncrementalWritePlannerTests.cs` | Add three characterization tests for independent sorted storage, single eager enumeration/disposal, and enumeration failure before entry validation. | Preserve exact exception instance, reference identity and caller ordering. |
| `tests/PiratePDF.DocGen.Tests/Parsing/MarkdownMetadataParserTests.cs` | Add four theory cases for trimmed values, empty cells and rejection of extra cells. | Preserve metadata row shape and continuation to the next valid row. |
| `docs/reviews/Post-BF-1054-Pre-B4-Expression-and-Materialization-Review.md` | Record this review, evidence, decisions, validation and per-file coverage. | Current review record; historical reports are unchanged. |

There are **nine changed C# files**: four production files, one tooling file and four test files. Of the production files, two have comments only. The main audit and B1/B2/B3/B3A reports were not changed because this review neither implements nor reclassifies an audit finding. DocGen is allowed to refresh its generated index, validation, health and repair-preview reports normally; actual changed generated paths are recorded with the validation result below.

| Transformation of existing code | Count |
|---|---:|
| ToArray to collection expression | 0 |
| Collection expression to ToArray | 1 |
| Redundant ToArray removed after an already-owned array producer | 1 |
| Deconstruction introduced | 0 |
| Deconstruction removed | 2 |
| Named loop variables clarified | 2 (`range`, `token`) |
| Existing temporary locals inlined | 0 |
| Existing standalone var/explicit-type normalization | 0 |
| Boundary comments added | 3 |

New test fixture arrays and locals are excluded from transformation counts. They use fixed collection expressions for simple owned test data.

## Intentional materialization retentions

**439 of the 440 original ToArray calls remain.** The only removed original call is the redundant DocGen clone. The new service call replaces one spread; test additions are accounted separately from baseline review counts.

| Retained sites or family | Reason |
|---|---|
| `PdfCMapParser.Parse`: `Tokenize(...).Select(...).ToArray()`; `PdfCMapTokenizerTests` finite `Take(...).ToArray()` probes | Indexed lookahead requires an eager result; tokenizer progress/count checks remain the bound on output. Tests retain finite probes rather than risking a hang if progress regresses. |
| `PdfCrossReferenceStreamParser.ReadFilters`, `PdfObjectStreamParser.ReadFilters`, `PdfFontStreamDecoder`, `PdfNestedGraphicsProvider` and function/image filter readers | Materialize validation-bearing projections before decoding. Exceptions and filter/decode-parameter alignment must not drift through deferred enumeration. |
| `PooledByteAccumulator`, `PooledPdfFilterBufferWriter`, `PdfPredictorDecoder` | Owned output snapshots must outlive reusable or pooled backing buffers; retain row-buffer swaps and bounded growth order. |
| `PdfContentStreamMutationCodec` and stream serialization/test helpers | `MemoryStream.ToArray()` and span/memory copies have concrete ownership contracts. They are not LINQ syntax alternatives. B1 decode and expansion limits remain untouched. |
| `PluginDiscoveryService` bounded directory enumeration and catalog/result snapshots | Keep eager deterministic results and the configured maximum-plus-one sentinel. Neither syntax makes an unbounded producer safe. SDK/manifest nullable snapshots retain null-coalescing behavior. |
| `PdfFunctionEvaluator.CalculatorMachine`, `copy` and `Roll` | Snapshot the selected stack region before pushes or overlapping writes. Named `copy`/`values`, `start`, `count` and `normalized` already explain the algorithm. |
| Document, Objects, Editing, Rendering display-list, SDK and Security result constructors | Defensive copies prevent mutable caller storage or lazy iterators from escaping through read-only views. Keep existing wrappers and validation order. |
| Font sanitizer, cmap builders, subsetters, Skia/native adapters and cryptographic handlers | Preserve actual buffer types, ownership and native/BCL call contracts. No pooling, span lifetime change, secret-lifetime claim or B4 implementation is justified by expression syntax alone. |
| Writing planner identifier `Distinct`/`OrderBy` pipelines and projection identifier snapshots | Stable object-number/generation order and independent published storage are preservation constraints. |
| DocGen change sets, relationship queries, sorted metadata collections and prepared-file snapshots | Deterministic order and eager completion before applying edits are clearer than a syntax-wide rewrite. Only the provably redundant `Split` clone was removed. |
| UTF-8 literal-to-byte-array fixtures, pixel/span assertions and serialized stream snapshots | The target API requires an array or the assertion intentionally captures owned bytes; replacing these calls would add churn without clarifying behavior. |

**1,128 of the 1,129 original collection expressions remain**, including **85 of 86 spread-containing expressions**. Existing collection expressions were not converted simply because they contain LINQ. Concrete source/target type, the local algorithm and existing naming control the decision.

| Retained expressions | Reason |
|---|---|
| Fixed font/image signatures, filter registrations, geometry coordinates, default colors, test fixtures and empty target-typed collections | Element construction and target type are apparent; explicit `new` or `Array.Empty` normalization would be style-only churn. |
| `PdfCMapParser.AddBigEndian`: `[.. value]` | Known byte-array copy preserves the starting code while producing the incremented code. |
| `PdfObjectWriteProjectionSet`: `[.. projections]` | The named `snapshot` is validated, sorted and wrapped before publication. Added comment and characterization establish the invariant without changing enumeration implementation. |
| Objects `PdfArray`/`PdfDictionary` snapshots | Existing copies preserve constructor ownership and dictionary-entry representation. Do not replace count-aware construction or expose backing collections. |
| Services transaction target ordering, Writing annotation identifiers, SDK adapter invalidation key snapshots | Materialized targets must survive subsequent collection mutation and preserve deterministic transaction/write order. The typed arrays and named targets make their boundary clear. |
| External-font catalog/provider sorting, subset table ordering, sanitizer record ordering | Keep ordering/comparers and present ownership; allocation or throughput differences need measurement. These paths remain outside B4 implementation. |
| The first text-only `Where` projection in `MeasureReplacement` | The typed `originalTextElements` array is a straightforward count-check snapshot. It has no advancing replacement index; matching the second projection's syntax would add no comparable clarification. |
| Test cryptographic concatenations and generated-font fixtures | Known arrays plus fixed bytes communicate exact fixture layout; preserve salt/IV/table order and independent storage. |

## Deconstruction, named variables and rejected churn

Of 95 starting deconstruction uses, **93 remain**; all six positional-pattern clauses remain. Predictor/cryptographic swaps retain simultaneous-assignment semantics. Both glyph-table rebuild results remain paired. Filter/decode-parameter results, cross-reference subsection offset/count values, pattern bounds and dictionary key/value loops retain deconstruction where both values matter. The SDK's operation/measurement pair still uses both results. Style token generation retains its color-space/stroking tuple patterns.

The two removals use existing tuple member names and do not invoke a user-defined `Deconstruct`. The cross-reference loop keeps numeric width and checked arithmetic explicit. The tokenizer test keeps the whole token available for inspection and reads only `End`. Rewriting unrelated fixture-return tuples, capitalization, or all discarded fixture values was rejected: their locally established tuple shape and document/object identifiers remain understandable, and no behavior or ownership improvement justified broad test churn.

All 82 short-variable candidates were examined and retained. In particular:

- Cursor positions and `crossReferenceOffset` are snapshots, not repeated getters; moving their evaluation would change provenance or restoration.
- `PluginRuntimeHandle` retains a load-context reference across clearing its field and unloading; it communicates lifetime.
- OOXML's attribute `value` must be captured before `MoveToElement()` changes reader state.
- Crypto limits, byte widths, glyph/code widths, rune values, `replacementIndex`, record offsets and row budgets retain descriptive names and concrete numeric types.
- Explicit `ReadOnlySpan<byte>` locals in OLE/RTF code establish the static type and copy/overload behavior; array inlining is not assumed equivalent.
- Test constants, interface-typed session references, original revision bytes, expected values and fixture identifiers describe contracts or capture state. A single use does not make them redundant.

Rejected style-only changes include repository-wide `var` normalization, renaming the SDK tuple's capitalized members, replacing every array copy with a spread, converting all LINQ materialization to one syntax, removing tuple deconstruction wherever a discard appears, replacing every `foreach`, and changing `new PdfDictionary()` versus `[]` throughout tests. Existing `Program.cs` formatting was retained; this pass did not reopen its earlier formatting reconciliation. File length alone was not treated as a defect or a reason to split files.

## Evidence and compatibility review

The DocGen allocation improvement follows directly from the producer contract: `string.Split` returns a `string[]`, and the private row parser immediately cloned it with `ToArray`. For successfully split table rows, removing that clone removes one extra array and its reference-copy pass. The split result is local and unshared. No throughput percentage or measured end-user speedup is claimed.

The service change expresses an eager boundary for a stateful selector. The source is the sealed display run's read-only array-backed collection, not a caller-supplied custom enumerable. The same text/adjustment ordering, replacement index reads and per-element byte copying occur before `MeasureElements`. Characterization verifies the mixed-run advance, unchanged source text/adjustments, exactly ordered replacement reads, cancellation token and a replacement getter's exact exception taking precedence over pre-cancellation. It does not claim fewer allocations or improved speed.

The remaining edits are tuple-member access, comments and tests. Null checks, exception types/instances, validation timing, iterator disposal, checked arithmetic, deterministic ordering, caller ownership and shallow projection identity remain unchanged. No new serialization algorithm or transactional mutation path was introduced. Existing affected-project save/reopen, rollback and undo/redo tests remain the integration coverage for Writing and Services.

**Measurement required:** changing count-aware collection spreads versus `ToArray` in hot font/catalog/image pipelines; replacing calculator snapshots; collapsing the first replacement count-check projection; pooling or streaming parser/filter buffers; changing cryptographic copies; and broad changes to ordered transaction snapshots. These are recorded as rejected performance speculation, not as implemented fixes or approved future batches. No benchmark supports choosing one general expression form over another.

## Tests and acceptance evidence

Ten new behavioral cases were first run successfully against the pre-cleanup implementation: three Writing tests, three Services cases and four DocGen cases. An initial Services test compilation exposed a missing threading import and nullable annotation context; both were corrected in the test file before characterization and production edits. No production behavior or exception expectation was changed to accommodate that setup failure.

After cleanup, Release targeted and complete affected-project runs passed:

| Test project | Targeted filter | Targeted passed | Entire project passed |
|---|---|---:|---:|
| BlackFlag.Engine.Parsing.Tests | `FullyQualifiedName~PdfCrossReferenceStreamParserTests` | 9 | 195 |
| BlackFlag.Engine.Rendering.Tests | `FullyQualifiedName~PdfCMap` | 29 | 87 |
| BlackFlag.Engine.Writing.Tests | `FullyQualifiedName~PdfProjectedIncrementalWritePlannerTests` | 10 | 138 |
| BlackFlag.Engine.Services.Tests | `FullyQualifiedName~PdfTextAdvanceMeasurementServiceTests` | 7 | 287 |
| PiratePDF.DocGen.Tests | `FullyQualifiedName~MarkdownMetadataParserTests` | 8 | 66 |
| **Total** | | **63** | **773** |

Targeted command shape: `dotnet test tests/<project>/<project>.csproj --configuration Release --no-restore --filter <filter>`. Each affected-project run used `--configuration Release --no-build` after its successful targeted build. All runs had zero failed or skipped tests. `git diff --check` passed.

Full unmodified acceptance command:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

**Acceptance: PASS.** The full unmodified gate exited successfully with **1,828 tests across 20 test projects**, zero failures/skips and **zero build warnings/errors**. Source-size validation, restore, build, tests, DocGen, health analysis, repair preview and whitespace validation all passed. The source-size checker inspected 1,026 files: its inventory excludes the ten hand-authored assembly-information files and includes two PowerShell scripts, explaining the difference from this review's 1,034 C# files. Its existing 937-line graphics interpreter and 930-line font sanitizer review notices were unchanged; neither file was edited.

DocGen validated **181 documents**, with zero errors, warnings or information diagnostics. Documentation health is **91.6%, A-**. Repair preview found **zero proposed changes, files or diagnostics**. Exactly three generated files changed normally: `docs/Documentation-Index.md`, `docs/Documentation-Health.md`, and `docs/ValidationReport.md`. The generated `docs/Document-Changes.md` remained unchanged. Together with the nine C# files and this new report, the final change set contains **13 files**.

Every changed C# file and generated documentation diff was reviewed. Final scope checks confirm unchanged public APIs, dependencies/project references, gate scripts, resource/security limits and preservation/transaction behavior. The original baseline commit and branch are unchanged. No B4 or later finding was implemented; this task stops at this review and its validation.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-08 | Repository-wide pre-B4 expression review, narrow behavior-preserving cleanup, characterization and coverage ledger. |

## Appendix - per-file baseline coverage

All rows refer to starting commit `7b3e2f64f3b0ce2128844b0c2f62891bc9d8bcbd`. Columns are ToArray calls (A), collection expressions (C), spread elements (S), deconstruction/pattern nodes (D), screened local declarators (V), and shortlisted variable candidates (VC). `Retain` records the scoped review decision; `Changed` refers to one of the nine C# paths described above. Zero counts do not exempt a file from inventory or review.

| File | A | C | S | D | V | VC | Decision |
|---|---:|---:|---:|---:|---:|---:|---|
| samples/BlackFlag.Engine.ReferencePlugin/ReferencePlugin.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Abstractions/IO/IRandomAccessSource.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Core/Objects/PdfBoolean.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Core/Objects/PdfInteger.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Core/Objects/PdfName.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Core/Objects/PdfNull.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Core/Objects/PdfObject.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Core/Objects/PdfObjectKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Core/Objects/PdfReal.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Core/Objects/PdfString.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Core/Objects/PdfStringKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Core/Primitives/ByteRange.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Annotations/PdfAnnotation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Annotations/PdfAnnotationOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Annotations/PdfAnnotationRectangle.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Annotations/PdfAnnotationSubtype.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecoration.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecorationAlignment.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecorationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecorationRegion.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecorationTarget.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecorations.cs | 0 | 1 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Decorations/PdfPageNumberFormat.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Decorations/PdfPageNumberingOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Decorations/PdfPageSelectionParity.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Forms/PdfAcroForm.cs | 0 | 1 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Forms/PdfFormField.cs | 0 | 2 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Document/Forms/PdfFormFieldOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Forms/PdfFormFieldType.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Metadata/PdfDocumentInformation.cs | 0 | 1 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Metadata/PdfDocumentInformationField.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Model/IPdfDictionaryBackedModel.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Model/PdfCatalog.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Model/PdfDocument.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Model/PdfObjectBinding.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Model/PdfTrailer.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Document/Model/PdfVersion.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/PdfArray.cs | 0 | 2 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/PdfDictionary.cs | 0 | 2 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/PdfIndirectReference.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/PdfNames.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/PdfObject.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/PdfObjectIdentifier.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/PdfObjectKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/PdfObjectStore.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/PdfStream.cs | 3 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfBoolean.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfInteger.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfName.cs | 2 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfNull.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfReal.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfString.cs | 2 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfStringFormat.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Outlines/PdfOutline.cs | 0 | 1 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Outlines/PdfOutlineDestination.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Outlines/PdfOutlineItem.cs | 0 | 1 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Document/Outlines/PdfOutlineNavigationAction.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Outlines/PdfOutlineNavigationActionType.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Pages/PdfEffectivePage.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Pages/PdfEffectivePageCollection.cs | 1 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Document/Pages/PdfEffectiveValue.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Pages/PdfPage.cs | 2 | 5 | 0 | 0 | 7 | 0 | Retain |
| src/BlackFlag.Engine.Document/Pages/PdfPageBox.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Pages/PdfPageBoxKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Pages/PdfPageGeometry.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Pages/PdfPageRotation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Pages/PdfPageTree.cs | 0 | 1 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Document/Pages/PdfPageValueOrigin.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Document/Properties/AssemblyInfo.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Annotations/PdfAnnotationMutationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Annotations/PdfAnnotationSnapshot.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Annotations/PdfEditableAnnotation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Annotations/PdfEditableAnnotationCollection.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Decorations/PdfEditablePageDecoration.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Decorations/PdfEditablePageDecorationCollection.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Decorations/PdfPageDecorationMutationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Decorations/PdfPageDecorationSnapshot.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Annotations.cs | 0 | 0 | 0 | 0 | 11 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Decorations.cs | 0 | 0 | 0 | 0 | 11 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Forms.cs | 0 | 0 | 0 | 0 | 28 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Metadata.cs | 0 | 0 | 0 | 0 | 7 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Outlines.cs | 0 | 0 | 0 | 0 | 19 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Pages.cs | 1 | 0 | 0 | 0 | 33 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Text.cs | 2 | 0 | 0 | 0 | 12 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Documents/PdfEditingPreservationMode.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Documents/PdfEditingSession.cs | 1 | 1 | 0 | 0 | 9 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Documents/PdfEditingSessionOptions.cs | 0 | 1 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Forms/PdfEditableFormField.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Forms/PdfEditableFormFieldCollection.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Forms/PdfFormFieldMutationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Forms/PdfFormFieldSnapshot.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Geometry/PdfPageGeometryMutationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Geometry/PdfPageResizeAnchor.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/History/PdfEditHistory.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Metadata/PdfDocumentInformationMutationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/IPdfEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/IPdfStructuralEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfAnnotationEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfContentEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfContentMutationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfDocumentInformationEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfEditScope.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfExistingTextEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfExistingTextResourceChangeOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfFormFieldEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfOutlineEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfPageDecorationEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfPageEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfPageGeometryEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfPageMutationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfResourceEditOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Operations/PdfResourceMutationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Outlines/PdfEditableOutlineItem.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Outlines/PdfEditableOutlineItemCollection.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Outlines/PdfOutlineItemSnapshot.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Outlines/PdfOutlineMutationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Ownership/PdfEditingOwnershipException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Ownership/PdfEditingOwnershipToken.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Pages/PdfEditablePage.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Pages/PdfEditablePageCollection.cs | 0 | 0 | 0 | 0 | 10 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Pages/PdfEditablePageOrigin.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Preservation/PdfEditingPreservationContract.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Properties/AssemblyInfo.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Structural/PdfStructuralValueOrigin.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Structural/PdfStructuralValueState.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Structural/PdfStructureKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Text/PdfExistingTextDescriptor.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Text/PdfExistingTextEditability.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Text/PdfExistingTextIdentity.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Text/PdfExistingTextSourceKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Text/PdfExistingTextTypography.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Transactions/PdfEditChange.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Transactions/PdfEditChangeSet.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Transactions/PdfEditTransaction.cs | 1 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Transactions/PdfEditTransactionState.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Validation/IPdfEditingValidator.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Validation/PdfEditingConsistencyValidator.cs | 0 | 0 | 0 | 0 | 16 | 0 | Retain |
| src/BlackFlag.Engine.Editing/Validation/PdfEditingValidationException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Filters/IPdfFilterDecodeBudget.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Filters/IPdfFilterDecoder.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Filters/IPdfFilterPipeline.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Filters/IPdfFilterRegistry.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfAscii85FilterDecoder.cs | 0 | 0 | 0 | 0 | 16 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfAsciiHexFilterDecoder.cs | 0 | 0 | 0 | 0 | 6 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfFilterByteClassifier.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfFilterDecodeBudget.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfFilterDecodeContext.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfFilterDecodeException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfFilterDecodeFailure.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfFilterDecodeLimits.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfFilterPipeline.cs | 2 | 0 | 0 | 0 | 8 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfFilterRegistrations.cs | 0 | 1 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfFilterRegistry.cs | 0 | 1 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfFlateDecodeParameters.cs | 0 | 0 | 0 | 0 | 8 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfFlateFilterDecoder.cs | 1 | 0 | 0 | 0 | 8 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfLzwBitReader.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfLzwDecodeParameters.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfLzwFilterDecoder.cs | 0 | 1 | 0 | 0 | 17 | 1 | Retain |
| src/BlackFlag.Engine.Filters/PdfPredictorDecoder.cs | 0 | 0 | 0 | 1 | 31 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PdfRunLengthFilterDecoder.cs | 0 | 0 | 0 | 0 | 8 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PooledByteAccumulator.cs | 0 | 1 | 0 | 0 | 6 | 0 | Retain |
| src/BlackFlag.Engine.Filters/PooledPdfFilterBufferWriter.cs | 1 | 1 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Filters/Properties/AssemblyInfo.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts.Platform/MacOS/PdfMacOSExternalFontProvider.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Fonts.Platform/PdfFileSystemExternalFontProvider.cs | 0 | 2 | 2 | 0 | 12 | 1 | Retain |
| src/BlackFlag.Engine.Fonts.Platform/PdfPlatformExternalFontProviders.cs | 0 | 3 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts.Platform/Windows/PdfWindowsExternalFontProvider.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfExternalTrueTypeInspector.cs | 3 | 0 | 0 | 0 | 70 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfExternalTrueTypeMetrics.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeBinary.cs | 0 | 0 | 0 | 0 | 17 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeCmap.cs | 3 | 1 | 1 | 0 | 57 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeCompositeClosure.cs | 0 | 1 | 1 | 0 | 16 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeGlyphMetricsReader.cs | 0 | 0 | 0 | 0 | 16 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeGlyphSubsetter.cs | 2 | 0 | 0 | 1 | 19 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeSfntBuilder.cs | 1 | 1 | 1 | 0 | 16 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeSubsetOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeSubsetResult.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfUnicodeTrueTypeGlyphSubsetter.cs | 2 | 1 | 1 | 1 | 28 | 1 | Retain |
| src/BlackFlag.Engine.Fonts/Embedding/PdfUnicodeTrueTypeSubsetResult.cs | 0 | 1 | 1 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/IPdfExternalFontProvider.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/PdfExternalFontCatalog.cs | 0 | 4 | 4 | 0 | 13 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/PdfExternalFontEmbeddingPolicy.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/PdfExternalFontEmbeddingRights.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/PdfExternalFontFace.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/PdfExternalFontMatch.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/PdfExternalFontMatchRequest.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/PdfExternalFontSlant.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/PdfExternalFontTechnology.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/PdfSanitizedExternalFontMetadata.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/PdfSanitizedExternalFontMetadataInspector.cs | 0 | 0 | 0 | 0 | 34 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Sanitization/IPdfExternalFontSanitizer.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Sanitization/PdfExternalFontSanitizationException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Sanitization/PdfExternalFontSanitizationFailure.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Sanitization/PdfExternalFontSanitizationOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Sanitization/PdfOpenTypeFontSanitizer.cs | 1 | 2 | 2 | 0 | 89 | 0 | Retain |
| src/BlackFlag.Engine.Fonts/Sanitization/PdfSanitizedExternalFontProgram.cs | 2 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.IO/RandomAccess/BufferedRandomAccessCursor.cs | 0 | 0 | 0 | 0 | 9 | 0 | Retain |
| src/BlackFlag.Engine.IO/RandomAccess/IRandomAccessCursor.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.IO/RandomAccess/RandomAccessCursor.cs | 0 | 0 | 0 | 0 | 6 | 0 | Retain |
| src/BlackFlag.Engine.IO/RandomAccess/RandomAccessCursorExtensions.cs | 0 | 0 | 0 | 0 | 5 | 1 | Retain |
| src/BlackFlag.Engine.IO/RandomAccess/RandomAccessDefaults.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.IO/Sources/FileRandomAccessSource.cs | 0 | 0 | 0 | 0 | 2 | 2 | Retain |
| src/BlackFlag.Engine.Objects/GlobalSuppressions.BF-POM-0005.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfArray.cs | 0 | 2 | 1 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfBoolean.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfDictionary.cs | 0 | 5 | 1 | 0 | 9 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfHexString.cs | 1 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfIndirectObject.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfIndirectReference.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfInteger.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfLiteralString.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfName.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfNull.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfObject.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfObjectType.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfObjectVisitor.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfReal.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Objects/PdfStream.cs | 1 | 1 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/ContentStreams/MemoryRandomAccessSource.cs | 1 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/ContentStreams/PdfContentParseException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/ContentStreams/PdfContentParseFailure.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/ContentStreams/PdfContentParseOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/ContentStreams/PdfContentStreamParser.cs | 2 | 1 | 0 | 0 | 37 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReference.cs | 1 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceChain.cs | 1 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceChainBuilder.cs | 0 | 2 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceEntry.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceParser.Bytes.cs | 3 | 0 | 0 | 0 | 6 | 1 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceParser.StartXRef.cs | 0 | 0 | 0 | 0 | 10 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceParser.Table.cs | 0 | 0 | 0 | 1 | 12 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceParser.Trailer.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceParser.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceSection.cs | 1 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceStreamEntry.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceStreamEntryKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceStreamParser.cs | 0 | 3 | 0 | 3 | 32 | 0 | Changed |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceStreamSection.cs | 1 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceTable.cs | 1 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfObjectId.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Documents/PdfEffectivePageTreeLoader.cs | 0 | 0 | 0 | 0 | 22 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Documents/PdfPageTreeLoadException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Documents/PdfPageTreeLoadOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Loading/IPdfIndirectObjectResolver.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Loading/PdfIndirectObjectResolver.cs | 0 | 1 | 0 | 0 | 11 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Loading/PdfObjectLocation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Loading/PdfObjectLocationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Loading/PdfObjectLocationTable.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Loading/PdfObjectResolutionException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Loading/PdfObjectResolutionFailure.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/ObjectStreams/PdfCompressedObjectEntry.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/ObjectStreams/PdfCompressedObjects.cs | 2 | 1 | 0 | 0 | 16 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/ObjectStreams/PdfObjectStreamParser.cs | 0 | 2 | 0 | 1 | 20 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/ObjectStreams/ReadOnlyMemoryRandomAccessSource.cs | 1 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/ParsingAssemblyMarker.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Arrays.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Atoms.cs | 1 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Dictionaries.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Helpers.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.IndirectObjects.cs | 0 | 0 | 0 | 0 | 7 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.IndirectReferences.cs | 0 | 0 | 0 | 0 | 6 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Numeric.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Strings.cs | 2 | 0 | 0 | 0 | 14 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.cs | 0 | 1 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Properties/AssemblyInfo.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Recovery/PdfCrossReferenceRecoveryResult.cs | 2 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Recovery/PdfCrossReferenceRecoveryService.cs | 2 | 1 | 0 | 0 | 35 | 1 | Retain |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveredEncryptionState.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveredObjectCandidate.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveryConfidence.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveryDiagnostic.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveryLimits.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveryMode.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveryOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Rendering/PdfNestedGraphicsProvider.cs | 3 | 3 | 0 | 0 | 22 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolutionException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolutionFailure.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolutionOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.ColorSpaces.cs | 0 | 0 | 0 | 0 | 6 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.Fonts.cs | 0 | 0 | 0 | 0 | 19 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.GraphicsResources.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.Helpers.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.Values.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.XObjects.cs | 0 | 0 | 0 | 0 | 11 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.cs | 0 | 1 | 0 | 0 | 14 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfEncryptionSecurityAssessment.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfEncryptionSecurityHandler.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfEncryptionSecurityState.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfPasswordAuthenticationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalysisOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalysisResult.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.AnalysisState.cs | 0 | 2 | 0 | 1 | 10 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticationResult.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.Aes256.cs | 1 | 0 | 0 | 0 | 17 | 1 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.Configuration.cs | 0 | 0 | 0 | 0 | 6 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.Legacy.cs | 3 | 0 | 0 | 0 | 32 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.cs | 2 | 1 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityCryptography.cs | 8 | 0 | 0 | 2 | 27 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityPermissions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecuritySession.cs | 2 | 1 | 0 | 0 | 10 | 1 | Retain |
| src/BlackFlag.Engine.Parsing/Streams/PdfStreamParser.Data.cs | 0 | 1 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Streams/PdfStreamParser.Header.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Streams/PdfStreamParser.Terminator.cs | 0 | 0 | 0 | 0 | 11 | 0 | Retain |
| src/BlackFlag.Engine.Parsing/Streams/PdfStreamParser.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Activation/PluginActivationIssue.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Activation/PluginActivationIssueKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Activation/PluginActivationPlan.cs | 3 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Activation/PluginActivationPlanner.cs | 1 | 4 | 2 | 0 | 18 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Activation/PluginActivationPlanningOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Capabilities/PluginCapabilityAccess.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Capabilities/PluginCapabilityRegistry.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Discovery/PluginCatalog.cs | 2 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Discovery/PluginDescriptor.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Discovery/PluginDiscoveryIssue.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Discovery/PluginDiscoveryIssueKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Discovery/PluginDiscoveryOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Discovery/PluginDiscoveryService.cs | 2 | 0 | 0 | 0 | 10 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Extensions/PluginExtensionRegistry.cs | 0 | 4 | 1 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Extensions/PluginExtensionSecurityRegistry.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Extensions/PluginExtensionTransaction.cs | 0 | 1 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Runtime/PluginEntryPoint.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Runtime/PluginLoadContext.cs | 0 | 0 | 0 | 0 | 7 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeEnvironment.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeHandle.cs | 0 | 0 | 0 | 0 | 1 | 1 | Retain |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeHost.cs | 0 | 0 | 0 | 0 | 12 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeIssue.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeIssueKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeSession.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Security/PluginSecurityAuthorization.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Plugins/Security/PluginSecurityPolicyDeniedException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering.Skia/Fonts/SkiaFontRealizer.cs | 1 | 1 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Rendering.Skia/Images/SkiaDctImageCodec.cs | 0 | 0 | 0 | 0 | 14 | 0 | Retain |
| src/BlackFlag.Engine.Rendering.Skia/Properties/AssemblyInfo.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Helpers.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Images.cs | 1 | 0 | 0 | 0 | 6 | 0 | Retain |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Paths.cs | 1 | 0 | 0 | 0 | 8 | 0 | Retain |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Shadings.cs | 2 | 0 | 0 | 0 | 11 | 0 | Retain |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Text.cs | 0 | 0 | 0 | 0 | 20 | 0 | Retain |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.cs | 0 | 2 | 0 | 0 | 11 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Color/IPdfColorConverter.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Color/PdfBlendMode.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Color/PdfBlendModeParser.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Color/PdfColorConverter.cs | 2 | 2 | 0 | 0 | 27 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Color/PdfColorTarget.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Color/PdfSrgbColor.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfClipKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfClipState.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfColorValue.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDashPattern.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayBounds.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayForm.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayImage.cs | 0 | 0 | 0 | 0 | 8 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayItem.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayList.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayMarkedContentPoint.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayPath.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayShading.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayTextRun.cs | 1 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfFillRule.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfGraphicsStateSnapshot.cs | 0 | 3 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfLineCapStyle.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfLineJoinStyle.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfMarkedContentContext.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfMatrix.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfPathGeometry.cs | 1 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfPathSegment.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfPathSegmentKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfPoint.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfTextRunElement.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfTextRunElementKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Fonts/PdfCMap.cs | 1 | 2 | 1 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Fonts/PdfCMapParser.cs | 1 | 1 | 1 | 0 | 48 | 0 | Changed |
| src/BlackFlag.Engine.Rendering/Fonts/PdfCharacterCode.cs | 1 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Fonts/PdfCodeSpaceRange.cs | 2 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Fonts/PdfDecodedGlyph.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Fonts/PdfDecodedTextRun.cs | 2 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Fonts/PdfFontDecodeDiagnostic.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Fonts/PdfFontDecoder.cs | 0 | 5 | 0 | 0 | 35 | 1 | Retain |
| src/BlackFlag.Engine.Rendering/Fonts/PdfFontStreamDecoder.cs | 3 | 3 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Fonts/PdfGlyphNameUnicodeMap.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Fonts/PdfSameFontTextEncoder.cs | 0 | 8 | 2 | 1 | 14 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Fonts/PdfWinAnsiEncoding.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluationLimits.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.CalculatorMachine.cs | 2 | 1 | 0 | 0 | 42 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.CalculatorParser.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.CalculatorTokens.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.cs | 6 | 7 | 0 | 0 | 85 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Graphics/PdfContentSourceSpan.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Graphics/PdfGraphicsOperation.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Graphics/PdfGraphicsOperator.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Graphics/PdfInlineImage.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/IBoundedPdfImageCodec.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/IPdfImageCodec.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfDecodedImage.cs | 1 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfImageCompatibilityConverter.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfImageCompatibilityDecision.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfImageCompatibilityTarget.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfImageConversionPolicy.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfImageDecodeDiagnostic.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfImageDecodeLimits.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfImageDecodeResult.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfImageDecoder.cs | 4 | 10 | 2 | 0 | 46 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfImageFormatRecognizer.cs | 0 | 9 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfImageSourceFormat.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Images/PdfImageSourceTraits.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Interpretation/IPdfFormOperationProvider.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpretationException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpretationFailure.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpretationOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Execution.cs | 0 | 2 | 0 | 0 | 39 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.GraphicsState.cs | 1 | 3 | 0 | 0 | 19 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Operands.cs | 0 | 1 | 0 | 0 | 12 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Paths.cs | 0 | 1 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Text.cs | 1 | 0 | 0 | 0 | 21 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Nested/IPdfNestedGraphicsProvider.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Nested/PdfTilingPatternCell.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Nested/PdfType3GlyphProgram.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Patterns/PdfPatternTilePlan.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Patterns/PdfPatternTilePlanner.cs | 0 | 1 | 0 | 2 | 20 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Properties/AssemblyInfo.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/IPdfPageRenderer.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/PdfPixelFormat.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/PdfRasterSurface.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderDiagnostic.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderLimits.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderMode.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderQuality.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderRegion.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderRequest.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderResult.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderWorkBudget.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderingDiagnosticCodes.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/RenderingAssemblyMarker.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfColorSpaceKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfFontEmbeddingRights.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfFontSlant.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfFontTechnology.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfFontVariationAxis.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedColorSpace.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedExtGState.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedFont.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedFormXObject.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedImageXObject.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedPattern.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedProperties.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedResource.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedShading.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedXObject.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResourceIdentity.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResourceKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Resources/PdfResourceScope.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Shadings/PdfGradientStop.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Shadings/PdfShadingDecoder.cs | 1 | 7 | 0 | 0 | 13 | 0 | Retain |
| src/BlackFlag.Engine.Rendering/Shadings/PdfShadingDescriptor.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK.Adapters/Editing/PdfEditingSdkSession.Text.cs | 2 | 7 | 3 | 1 | 51 | 1 | Retain |
| src/BlackFlag.Engine.SDK.Adapters/Editing/PdfEditingSdkSession.cs | 0 | 1 | 0 | 0 | 21 | 0 | Retain |
| src/BlackFlag.Engine.SDK.Adapters/Security/DocumentSecuritySdkService.cs | 1 | 0 | 0 | 0 | 8 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/IPdfEditingSdkSession.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/IPdfExistingTextSdkSession.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkChangeSet.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkDocumentInformationField.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPage.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPageBox.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPageBoxKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPageBoxState.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPageResizeAnchor.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPageRotation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPreservationMode.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkStructuralValueOrigin.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkStructureSummary.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkEditability.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkResults.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkSourceKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkStyle.cs | 0 | 1 | 1 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkTarget.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkTypography.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkTypographyUpdate.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/Capabilities/IPluginCapability.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/Capabilities/IPluginCapabilityAccess.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/Capabilities/PluginCapabilityKey.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/Extensions/IPluginExtension.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/Extensions/IPluginExtensionRegistration.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/Extensions/PluginExtensionPoint.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/IBlackFlagPlugin.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/PluginCapability.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/PluginCompatibility.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/PluginDependency.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/PluginId.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/PluginInitializationContext.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/PluginManifest.cs | 3 | 3 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/PluginManifestException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/PluginManifestLimits.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/PluginManifestReader.cs | 3 | 2 | 0 | 0 | 21 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Plugins/PluginVersionComparison.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkFinding.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkObservation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkObservationCategory.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkResult.cs | 2 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkSourceLocation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.SDK/Security/IDocumentSecuritySdkService.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Containers/DocumentContainerEntry.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Containers/DocumentContainerSecurityInspection.cs | 2 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedInspectionEngine.cs | 2 | 2 | 0 | 0 | 7 | 0 | Retain |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedInspectionResult.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedObject.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedObjectClassification.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedObjectClassifier.cs | 0 | 7 | 0 | 0 | 9 | 2 | Retain |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedObjectKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Containers/IDocumentContainerAdapter.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Containers/IDocumentContainerSecurityAdapter.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Containers/IDocumentEmbeddedObjectClassifier.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Findings/DocumentSecurityFinding.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/DocumentSecurityFormatAdapterFactory.cs | 0 | 1 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Markdown/MarkdownContainerSecurityAdapter.cs | 0 | 1 | 0 | 0 | 12 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Markdown/MarkdownSecurityAnalysisOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Markdown/MarkdownSecurityAnalysisResult.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Markdown/MarkdownSecurityAnalyzer.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Ole/LegacyWordBinarySecurityAnalysisResult.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Ole/LegacyWordBinarySecurityAnalyzer.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Ole/OleCompoundFileSecurityAdapter.cs | 0 | 10 | 0 | 1 | 71 | 2 | Retain |
| src/BlackFlag.Engine.Security/Formats/Ole/OleCompoundFileSecurityAnalysisOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordContainerSecurityAdapter.cs | 1 | 0 | 0 | 0 | 38 | 1 | Retain |
| src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordSecurityAnalysisOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordSecurityAnalysisResult.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordSecurityAnalyzer.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/OpenDocument/OpenDocumentTextContainerSecurityAdapter.cs | 1 | 0 | 0 | 0 | 19 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/OpenDocument/OpenDocumentTextSecurityAnalysisOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/OpenDocument/OpenDocumentTextSecurityAnalysisResult.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/OpenDocument/OpenDocumentTextSecurityAnalyzer.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Rtf/RtfContainerSecurityAdapter.cs | 0 | 5 | 0 | 0 | 26 | 1 | Retain |
| src/BlackFlag.Engine.Security/Formats/Rtf/RtfSecurityAnalysisOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Rtf/RtfSecurityAnalysisResult.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Formats/Rtf/RtfSecurityAnalyzer.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Security/Inspection/DocumentSecurityAssessment.cs | 2 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Inspection/DocumentSecurityAssessmentEngine.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Security/Inspection/DocumentSecurityInspectionBudget.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Security/Inspection/DocumentSecurityInspectionLimits.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Model/DocumentSecurityObservation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Model/DocumentSecurityObservationCategory.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Model/DocumentSecuritySourceLocation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Plugins/BlackFlagPluginSecurityPolicy.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Plugins/IPluginSecurityPolicy.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Plugins/PluginSecurityDecision.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Plugins/PluginSecurityGrant.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Plugins/PluginSecurityOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Plugins/PluginSecurityRequest.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Plugins/PluginSecuritySensitivity.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Policy/BlackFlagDocumentSecurityPolicy.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Policy/DocumentSecurityInteraction.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Policy/DocumentSecurityOperation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Policy/DocumentSecurityPolicyAction.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Policy/DocumentSecurityPolicyDecision.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Policy/DocumentSecuritySeverity.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Policy/IDocumentSecurityPolicy.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Remediation/DocumentSecurityRemediationAction.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Remediation/DocumentSecurityRemediationIntent.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Remediation/DocumentSecurityRemediationPlan.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Remediation/DocumentSecurityRemediationPlanner.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Security/Remediation/PassiveConversionSecurityResult.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Remediation/PassiveConversionSecurityService.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Security/Reporting/DocumentSecurityReport.cs | 2 | 1 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Reporting/DocumentSecurityReportFactory.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Security/Reporting/DocumentSecurityTextReportWriter.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfAnnotationCommonStateWritingService.cs | 1 | 1 | 0 | 0 | 15 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfAnnotationStructuralWritingService.cs | 0 | 1 | 0 | 0 | 24 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfAnnotationTransactionWritingService.cs | 0 | 4 | 1 | 0 | 28 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfEditingSaveService.cs | 1 | 0 | 0 | 0 | 9 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfEditingService.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfNestedPageTransactionWritingService.cs | 0 | 3 | 2 | 0 | 20 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfNestedPageTreeOperationReplay.cs | 0 | 0 | 0 | 0 | 14 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfNestedPageTreeStructuralWritingService.cs | 1 | 3 | 3 | 0 | 23 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfPageGeometryWritingService.cs | 0 | 1 | 1 | 0 | 11 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfPageRotationWritingService.cs | 0 | 1 | 1 | 0 | 7 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfPageStateWritingService.cs | 0 | 1 | 1 | 0 | 9 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfPageStructuralWritingService.cs | 0 | 0 | 0 | 0 | 19 | 0 | Retain |
| src/BlackFlag.Engine.Services/Editing/PdfPageTransactionWritingService.cs | 0 | 2 | 1 | 0 | 27 | 0 | Retain |
| src/BlackFlag.Engine.Services/Fonts/PdfCidToUnicodeCMapBuilder.cs | 0 | 1 | 1 | 0 | 9 | 0 | Retain |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalFontEmbeddingWritingService.cs | 0 | 0 | 0 | 0 | 6 | 0 | Retain |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalFontResourceProjection.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeResourceProjector.cs | 0 | 2 | 0 | 0 | 15 | 0 | Retain |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeSubsetResourceProjector.cs | 0 | 3 | 0 | 0 | 21 | 0 | Retain |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalType0TrueTypeResourceProjection.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalType0TrueTypeResourceProjector.cs | 0 | 5 | 0 | 0 | 27 | 0 | Retain |
| src/BlackFlag.Engine.Services/Fonts/PdfToUnicodeCMapBuilder.cs | 0 | 1 | 1 | 0 | 9 | 0 | Retain |
| src/BlackFlag.Engine.Services/Pages/PdfPageService.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Services/Properties/AssemblyInfo.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Resources/PdfPageResourceMutationProjection.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Resources/PdfPageResourceMutationProjector.cs | 0 | 0 | 0 | 3 | 11 | 0 | Retain |
| src/BlackFlag.Engine.Services/Resources/PdfResourceMutationOwnership.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Security/DocumentSecurityService.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Services/Security/DocumentSecurityServiceResult.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustDecision.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustEvaluation.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustEvaluator.cs | 0 | 0 | 0 | 0 | 6 | 1 | Retain |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustEvidence.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustRecord.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustRecordFactory.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfCompoundTextOperatorNormalizer.cs | 3 | 0 | 0 | 0 | 7 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfContentStreamMutationCodec.cs | 3 | 1 | 0 | 0 | 17 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingFontCandidate.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingFontSelectionService.cs | 0 | 2 | 2 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextColor.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextDecoration.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextDecorationKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextDiscoveryService.cs | 0 | 0 | 0 | 1 | 16 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextDiscoverySource.cs | 2 | 1 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextEditingService.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextFindReplaceResult.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextFindReplaceService.cs | 0 | 2 | 2 | 0 | 31 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextFontService.cs | 0 | 1 | 1 | 0 | 22 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextReplacementService.cs | 0 | 0 | 0 | 0 | 11 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextScriptStyle.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextSearchMatch.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextSearchScope.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleService.cs | 0 | 1 | 1 | 0 | 15 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleTokenEditor.cs | 2 | 0 | 0 | 6 | 21 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleUpdate.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextTransactionWritingService.cs | 0 | 1 | 1 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextTypographyService.cs | 0 | 1 | 1 | 0 | 13 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextTypographyUpdate.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExternalExistingTextFontService.cs | 0 | 1 | 1 | 0 | 27 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfExternalTextShapingGuard.cs | 0 | 0 | 0 | 0 | 2 | 1 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfFontSwitchTokenEditor.cs | 1 | 0 | 0 | 0 | 10 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfFormPlacementCloneProjection.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfFormPlacementCloneProjector.cs | 1 | 1 | 0 | 0 | 17 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfFormPlacementDescriptor.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfPositionedTextReplacementService.cs | 0 | 0 | 0 | 0 | 19 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfSharedFormEditMode.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfSharedFormExistingTextMutationService.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfTextAdvanceCompensationTokenEditor.cs | 1 | 0 | 0 | 0 | 12 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfTextAdvanceFit.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfTextAdvanceMeasurement.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfTextAdvanceMeasurementService.cs | 0 | 2 | 2 | 0 | 11 | 1 | Changed |
| src/BlackFlag.Engine.Services/Text/PdfTextArrayTokenEditor.cs | 0 | 1 | 1 | 0 | 28 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfTextStateTokenEditor.cs | 1 | 0 | 0 | 0 | 13 | 1 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfTextStringTokenCodec.cs | 1 | 2 | 2 | 0 | 21 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfTextStringTokenEditor.cs | 0 | 0 | 0 | 0 | 15 | 0 | Retain |
| src/BlackFlag.Engine.Services/Text/PdfWidthAwareTextReplacementService.cs | 0 | 1 | 0 | 0 | 16 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/IPdfLexer.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfCharacterClassifier.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Delimiters.cs | 0 | 0 | 0 | 0 | 3 | 1 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Helpers.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Hexadecimal.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.HexadecimalStrings.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Keywords.cs | 0 | 0 | 0 | 0 | 9 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.LiteralStrings.cs | 0 | 0 | 0 | 0 | 4 | 1 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Names.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Numbers.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Position.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Trivia.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexerException.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexerOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfSourcePosition.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfSourceSpan.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfToken.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Lexing/PdfTokenKind.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Parsing/PdfTokenReader.cs | 0 | 0 | 0 | 0 | 6 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Parsing/PdfUnexpectedTokenException.cs | 0 | 2 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Syntax/Properties/AssemblyInfo.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/IPdfOriginalSourceRevision.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfAnnotationCommonStateFullRewritePlanner.cs | 0 | 1 | 1 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfAnnotationCommonStateIncrementalWritePlanner.cs | 0 | 1 | 1 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfAnnotationFullRewritePlanner.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfAnnotationIncrementalWritePlanner.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfAnnotationWriteProjector.cs | 0 | 3 | 2 | 0 | 16 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfCountingWriteStream.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfCrossReferenceStreamWriter.cs | 0 | 1 | 0 | 0 | 10 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfCrossReferenceWriteMode.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfDocumentInformationFullRewritePlanner.cs | 0 | 1 | 1 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfDocumentInformationIncrementalWritePlanner.cs | 0 | 1 | 1 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfDocumentInformationWriteProjector.cs | 0 | 2 | 1 | 0 | 15 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfFullRewriteWriter.cs | 1 | 1 | 0 | 0 | 10 | 1 | Retain |
| src/BlackFlag.Engine.Writing/PdfIncrementalCrossReferenceStreamWriter.cs | 0 | 3 | 2 | 0 | 15 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfIncrementalTraditionalCrossReferenceWriter.cs | 0 | 1 | 1 | 0 | 10 | 1 | Retain |
| src/BlackFlag.Engine.Writing/PdfIncrementalUpdateWriter.cs | 1 | 0 | 0 | 0 | 10 | 1 | Retain |
| src/BlackFlag.Engine.Writing/PdfIncrementalWritePlan.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfIncrementalWritePlanner.cs | 1 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfIndirectObjectSerializer.cs | 1 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfMemorySourceRevision.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfNestedPageTreeCrossNodeProjector.cs | 0 | 0 | 0 | 0 | 18 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfNestedPageTreeStructuralProjector.cs | 0 | 0 | 0 | 0 | 17 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfNestedPageTreeTopologyAnalyzer.cs | 0 | 0 | 0 | 0 | 13 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfNestedPageTreeTopologyProjector.cs | 0 | 0 | 0 | 0 | 11 | 2 | Retain |
| src/BlackFlag.Engine.Writing/PdfNestedPageTreeWriteProjector.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfNewAnnotationFullRewritePlanner.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfNewAnnotationIncrementalWritePlanner.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfNewAnnotationProjectionBatch.cs | 0 | 1 | 1 | 0 | 14 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfNewPageFullRewritePlanner.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfNewPageIncrementalWritePlanner.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfNewPageProjectionBatch.cs | 0 | 1 | 1 | 0 | 15 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfObjectSerializer.cs | 1 | 1 | 0 | 0 | 17 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfObjectWriteProjection.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfObjectWriteProjectionSet.cs | 1 | 1 | 1 | 0 | 8 | 0 | Changed |
| src/BlackFlag.Engine.Writing/PdfPageGeometryWriteProjector.cs | 0 | 2 | 1 | 0 | 8 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfPageTreeHierarchySnapshot.cs | 0 | 0 | 0 | 0 | 16 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfPageTreeWriteProjector.cs | 0 | 1 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfProjectedFullRewritePlanner.cs | 0 | 1 | 1 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfProjectedIncrementalWritePlanner.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfTraditionalCrossReferenceWriter.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfWriteMode.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfWriteObjectAllocator.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfWriteOptions.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfWritePlan.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfWritePlanner.cs | 1 | 0 | 0 | 0 | 5 | 0 | Retain |
| src/BlackFlag.Engine.Writing/PdfWritingPreservationContract.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/Properties/AssemblyInfo.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| src/BlackFlag.Engine.Writing/WritingAssemblyMarker.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Abstractions.Tests/IRandomAccessSourceContractTests.cs | 1 | 2 | 0 | 0 | 8 | 1 | Retain |
| tests/BlackFlag.Engine.Core.Tests/Objects/PdfPrimitiveObjectTests.cs | 1 | 0 | 0 | 0 | 6 | 0 | Retain |
| tests/BlackFlag.Engine.Core.Tests/Primitives/ByteRangeTests.cs | 0 | 0 | 0 | 0 | 12 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/Annotations/PdfAnnotationModelTests.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/Decorations/PdfPageDecorationTests.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/Forms/PdfAcroFormTests.cs | 0 | 0 | 0 | 0 | 7 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/Model/PdfBackingDictionaryTests.cs | 0 | 0 | 0 | 0 | 20 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/Model/PdfDocumentInformationTests.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/Model/PdfDocumentModelTests.cs | 3 | 3 | 0 | 0 | 16 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/Objects/PdfCompositeObjectTests.cs | 1 | 5 | 0 | 0 | 17 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/Objects/PdfObjectIdentifierTests.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/Objects/Primitives/PdfPrimitiveObjectTests.cs | 0 | 1 | 0 | 0 | 15 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/Outlines/PdfOutlineModelTests.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| tests/BlackFlag.Engine.Document.Tests/Pages/PdfPageGeometryStateTests.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Annotations/PdfAnnotationEditingTests.cs | 0 | 1 | 0 | 0 | 45 | 1 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Content/PdfContentResourceMutationTests.cs | 0 | 1 | 0 | 0 | 32 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Decorations/PdfPageDecorationEditingTests.cs | 0 | 1 | 0 | 0 | 31 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Documents/PdfEditableDocumentTests.cs | 0 | 0 | 0 | 0 | 12 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Documents/PdfEditingSessionTests.cs | 0 | 0 | 0 | 0 | 7 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Forms/PdfFormFieldEditingTests.cs | 0 | 1 | 0 | 0 | 47 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Geometry/PdfPageGeometryEditingTests.cs | 0 | 1 | 0 | 0 | 31 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Hardening/PdfCrossStructureConsistencyTests.cs | 0 | 0 | 0 | 0 | 27 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Hardening/PdfEditingHardeningTests.cs | 0 | 3 | 0 | 0 | 32 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Hardening/PdfStructuralEditingAdversarialTests.cs | 0 | 0 | 0 | 0 | 34 | 1 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Hardening/PdfStructuralEditingScaleTests.cs | 0 | 0 | 0 | 0 | 9 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/History/PdfEditHistoryTests.cs | 0 | 0 | 0 | 0 | 46 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Metadata/PdfDocumentInformationEditingTests.cs | 0 | 0 | 0 | 0 | 20 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Outlines/PdfOutlineEditingTests.cs | 0 | 1 | 0 | 0 | 38 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Ownership/PdfEditingOwnershipTests.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Pages/PdfEditablePageCollectionTests.cs | 0 | 0 | 0 | 0 | 10 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Pages/PdfPageMutationTests.cs | 0 | 0 | 0 | 0 | 41 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Preservation/PdfEditingPreservationContractTests.cs | 0 | 0 | 0 | 0 | 26 | 2 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Structural/PdfStructuralEditOperationContractTests.cs | 0 | 0 | 0 | 0 | 10 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Structural/PdfStructuralValueStateTests.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Text/PdfExistingTextContractTests.cs | 1 | 3 | 1 | 0 | 21 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Transactions/PdfEditTransactionTests.cs | 0 | 0 | 0 | 0 | 34 | 0 | Retain |
| tests/BlackFlag.Engine.Editing.Tests/Validation/PdfEditingValidationTests.cs | 0 | 7 | 0 | 0 | 27 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/Corpus/PdfBinaryFilterCorpus.cs | 3 | 11 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/Corpus/PdfTextFilterCorpus.cs | 26 | 11 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfAscii85FilterDecoderTests.cs | 4 | 1 | 0 | 0 | 8 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfAsciiHexFilterDecoderTests.cs | 2 | 1 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterDecodeBudgetTests.cs | 0 | 0 | 0 | 0 | 10 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterDecodeContextTests.cs | 0 | 1 | 0 | 0 | 3 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterDecodeLimitsTests.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterIntegrationTests.cs | 7 | 4 | 0 | 0 | 26 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterLargeInputRegressionTests.cs | 5 | 1 | 0 | 0 | 17 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterPipelineTests.cs | 12 | 17 | 0 | 0 | 27 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterRegistryTests.cs | 0 | 2 | 0 | 0 | 7 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfFlateFilterDecoderTests.cs | 8 | 10 | 0 | 0 | 29 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfFlateTestData.cs | 1 | 0 | 0 | 0 | 2 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfLzwFilterDecoderTests.cs | 9 | 9 | 0 | 0 | 22 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfLzwTestEncoder.cs | 1 | 0 | 0 | 0 | 8 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfPredictorDecoderTests.cs | 3 | 3 | 0 | 0 | 6 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/PdfRunLengthFilterDecoderTests.cs | 1 | 2 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.Filters.Tests/TestFilterDecoder.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| tests/BlackFlag.Engine.Fonts.Platform.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFileSystemExternalFontProviderTests.cs | 1 | 7 | 1 | 0 | 49 | 0 | Retain |
| tests/BlackFlag.Engine.Fonts.Tests/PdfExternalFontCatalogTests.cs | 2 | 19 | 0 | 0 | 18 | 0 | Retain |
| tests/BlackFlag.Engine.Fonts.Tests/PdfOpenTypeFontSanitizerTests.cs | 7 | 0 | 0 | 0 | 43 | 0 | Retain |
| tests/BlackFlag.Engine.Fonts.Tests/PdfTrueTypeGlyphSubsetterCompositeTests.cs | 2 | 7 | 1 | 1 | 55 | 0 | Retain |
| tests/BlackFlag.Engine.Fonts.Tests/TestFontFactory.cs | 0 | 2 | 1 | 0 | 25 | 0 | Retain |
| tests/BlackFlag.Engine.IO.Tests/RandomAccess/BufferedRandomAccessCursorTests.cs | 3 | 3 | 0 | 0 | 10 | 0 | Retain |
| tests/BlackFlag.Engine.IO.Tests/RandomAccess/RandomAccessCursorTests.cs | 2 | 7 | 0 | 0 | 17 | 0 | Retain |
| tests/BlackFlag.Engine.IO.Tests/Sources/FileRandomAccessSourceTests.cs | 4 | 13 | 0 | 0 | 54 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfArrayTests.cs | 0 | 24 | 0 | 0 | 36 | 2 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfBooleanTests.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfDictionaryTests.cs | 0 | 26 | 0 | 0 | 47 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfHexStringTests.cs | 3 | 2 | 0 | 0 | 10 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfIndirectObjectTests.cs | 0 | 4 | 0 | 0 | 26 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfIndirectReferenceTests.cs | 0 | 0 | 0 | 0 | 27 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfIntegerTests.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfLiteralStringTests.cs | 0 | 0 | 0 | 0 | 6 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfNameTests.cs | 0 | 0 | 0 | 0 | 7 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfNullTests.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfObjectTests.cs | 0 | 0 | 0 | 0 | 7 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfObjectVisitorTests.cs | 0 | 0 | 0 | 0 | 8 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfPrimitiveVisitorTests.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| tests/BlackFlag.Engine.Objects.Tests/PdfRealTests.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/ContentStreams/PdfContentStreamParserTests.cs | 1 | 0 | 0 | 0 | 14 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/Documents/PdfEffectivePageTreeLoaderTests.cs | 0 | 2 | 0 | 0 | 6 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/Loading/PdfIndirectObjectResolverTests.cs | 0 | 1 | 0 | 0 | 18 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/Loading/PdfObjectLocationTableTests.cs | 0 | 3 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/PdfCrossReferenceParserTests.cs | 0 | 6 | 0 | 0 | 74 | 8 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/PdfCrossReferenceStreamParserTests.cs | 1 | 18 | 0 | 0 | 19 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/PdfObjectParserIndirectTests.cs | 0 | 0 | 0 | 0 | 66 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/PdfObjectParserTests.cs | 3 | 1 | 0 | 0 | 74 | 1 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/PdfObjectStreamParserTests.cs | 14 | 14 | 0 | 0 | 28 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/PdfStreamParserTests.cs | 13 | 3 | 0 | 0 | 45 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/Recovery/PdfCrossReferenceRecoveryServiceTests.cs | 0 | 0 | 0 | 0 | 23 | 2 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/Resources/PdfResourceResolverTests.cs | 1 | 5 | 0 | 0 | 44 | 1 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/Security/PdfEncryptionSecurityHandlerTests.cs | 0 | 0 | 0 | 0 | 21 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/Security/PdfSecurityAnalyzerTests.cs | 1 | 3 | 0 | 0 | 34 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/Security/PdfStandardSecurityAuthenticatorTests.cs | 12 | 11 | 11 | 2 | 76 | 0 | Retain |
| tests/BlackFlag.Engine.Parsing.Tests/TestRandomAccessSource.cs | 1 | 0 | 0 | 0 | 1 | 0 | Retain |
| tests/BlackFlag.Engine.Plugins.TestPlugin/SamplePlugin.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Plugins.Tests/Activation/PluginActivationPlannerTests.cs | 0 | 9 | 0 | 0 | 12 | 1 | Retain |
| tests/BlackFlag.Engine.Plugins.Tests/Architecture/PluginAssemblyDependencyTests.cs | 1 | 1 | 0 | 0 | 2 | 1 | Retain |
| tests/BlackFlag.Engine.Plugins.Tests/Discovery/PluginDiscoveryServiceTests.cs | 0 | 8 | 0 | 0 | 10 | 2 | Retain |
| tests/BlackFlag.Engine.Plugins.Tests/Extensions/PluginExtensionRegistryHardeningTests.cs | 0 | 1 | 0 | 0 | 16 | 0 | Retain |
| tests/BlackFlag.Engine.Plugins.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Plugins.Tests/Reference/ReferencePluginContractTests.cs | 0 | 1 | 0 | 0 | 7 | 0 | Retain |
| tests/BlackFlag.Engine.Plugins.Tests/Runtime/PluginRuntimeHostTests.cs | 0 | 13 | 0 | 0 | 69 | 1 | Retain |
| tests/BlackFlag.Engine.Rendering.Skia.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Skia.Tests/Images/SkiaDctImageCodecLimitTests.cs | 0 | 11 | 0 | 0 | 36 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Skia.Tests/Images/SkiaDctImageCodecTests.cs | 2 | 5 | 1 | 0 | 9 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Skia.Tests/Images/SkiaJpegRenderingTests.cs | 2 | 3 | 0 | 0 | 10 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Skia.Tests/Images/SkiaJpegTestData.cs | 1 | 0 | 0 | 0 | 6 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Skia.Tests/SkiaPdfPageRendererTests.cs | 1 | 3 | 0 | 0 | 6 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Color/PdfColorConverterTests.cs | 0 | 2 | 0 | 0 | 2 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/DisplayList/PdfClipStateTests.cs | 0 | 12 | 0 | 0 | 11 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfCMapParserTests.cs | 0 | 6 | 0 | 0 | 10 | 4 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfCMapTokenizerTests.cs | 2 | 1 | 0 | 1 | 16 | 0 | Changed |
| tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfGlyphNameUnicodeMapTests.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfWinAnsiEncodingTests.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Functions/PdfFunctionEvaluatorBf1048GTests.cs | 0 | 13 | 0 | 0 | 9 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Functions/PdfFunctionEvaluatorTests.cs | 0 | 16 | 0 | 0 | 7 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Images/PdfImageCompatibilityTests.cs | 0 | 1 | 0 | 0 | 4 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Images/PdfImageDecodeLimitTests.cs | 0 | 14 | 0 | 0 | 15 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Images/PdfImageDecoderTests.cs | 4 | 6 | 0 | 0 | 11 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Interpretation/PdfGraphicsInterpreterTests.cs | 0 | 13 | 0 | 0 | 42 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Patterns/PdfPatternTilePlannerTests.cs | 0 | 2 | 0 | 0 | 3 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Raster/PdfRenderLimitsBf1048FTests.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Raster/PdfRenderRequestTests.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Raster/PdfRenderWorkBudgetTests.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| tests/BlackFlag.Engine.Rendering.Tests/Shadings/PdfShadingDecoderTests.cs | 0 | 7 | 0 | 0 | 4 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Adapters.Tests/Editing/PdfEditingSdkHardeningTests.cs | 0 | 0 | 0 | 0 | 10 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Adapters.Tests/Editing/PdfEditingSdkSessionTests.cs | 1 | 0 | 0 | 0 | 18 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Adapters.Tests/Editing/PdfExistingTextSdkSessionTests.cs | 1 | 22 | 0 | 0 | 67 | 2 | Retain |
| tests/BlackFlag.Engine.SDK.Adapters.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Adapters.Tests/Security/DocumentSecuritySdkServiceTests.cs | 0 | 2 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Tests/Editing/PdfEditingSdkContractTests.cs | 0 | 1 | 0 | 0 | 6 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Tests/Editing/PdfExistingTextSdkContractTests.cs | 0 | 2 | 0 | 0 | 2 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Tests/Plugins/PluginCapabilityAccessContractTests.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Tests/Plugins/PluginDependencyManifestTests.cs | 0 | 0 | 0 | 0 | 6 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Tests/Plugins/PluginManifestConstructionTests.cs | 1 | 8 | 0 | 0 | 8 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Tests/Plugins/PluginManifestReaderTests.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.SDK.Tests/Plugins/PluginSdkIsolationTests.cs | 1 | 0 | 0 | 0 | 7 | 1 | Retain |
| tests/BlackFlag.Engine.SDK.Tests/Security/DocumentSecuritySdkServiceTests.cs | 0 | 0 | 0 | 0 | 9 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Containers/DocumentEmbeddedInspectionEngineTests.cs | 2 | 10 | 0 | 0 | 14 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Containers/DocumentEmbeddedObjectClassifierTests.cs | 4 | 1 | 0 | 0 | 11 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Formats/Markdown/MarkdownSecurityAnalyzerTests.cs | 0 | 0 | 0 | 0 | 8 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Formats/Ole/LegacyWordBinarySecurityAnalyzerTests.cs | 0 | 2 | 0 | 0 | 15 | 1 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Formats/Ooxml/OoxmlWordSecurityAnalyzerTests.cs | 2 | 1 | 0 | 1 | 30 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Formats/OpenDocument/OpenDocumentTextSecurityAnalyzerTests.cs | 1 | 0 | 0 | 1 | 22 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Formats/Rtf/RtfSecurityAnalyzerTests.cs | 0 | 0 | 0 | 0 | 8 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Inspection/DocumentSecurityAdversarialHardeningTests.cs | 0 | 0 | 0 | 0 | 15 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Inspection/DocumentSecurityAssessmentEngineTests.cs | 0 | 4 | 0 | 0 | 8 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Inspection/DocumentSecurityInspectionBudgetTests.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Model/DocumentSecurityObservationTests.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Plugins/BlackFlagPluginSecurityPolicyTests.cs | 0 | 1 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Policy/BlackFlagDocumentSecurityPolicyTests.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Remediation/DocumentSecurityRemediationPlannerTests.cs | 0 | 2 | 0 | 0 | 13 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Remediation/PassiveConversionSecurityServiceTests.cs | 0 | 3 | 0 | 0 | 6 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Reporting/DocumentSecurityReportFactoryTests.cs | 0 | 3 | 0 | 0 | 6 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/Reporting/DocumentSecurityTextReportWriterTests.cs | 0 | 1 | 0 | 0 | 4 | 0 | Retain |
| tests/BlackFlag.Engine.Security.Tests/ResultCollectionImmutabilityTests.cs | 0 | 14 | 0 | 0 | 27 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfAnnotationCommonStateWritingServiceTests.cs | 0 | 3 | 0 | 5 | 30 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfAnnotationStructuralWritingServiceTests.cs | 0 | 4 | 0 | 5 | 44 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfAnnotationTransactionWritingServiceTests.cs | 0 | 5 | 0 | 5 | 43 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfEditingSaveConformanceTests.cs | 0 | 7 | 0 | 0 | 55 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfEditingSaveServiceTests.cs | 0 | 3 | 0 | 0 | 29 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfEditingServiceTests.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfNestedPageTransactionWritingServiceTests.cs | 0 | 7 | 0 | 0 | 60 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfNestedPageTreeStructuralWritingServiceTests.cs | 0 | 10 | 0 | 0 | 51 | 1 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfNestedPageTreeTopologyManagementTests.cs | 0 | 5 | 0 | 0 | 36 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfPageGeometryWritingServiceTests.cs | 0 | 1 | 0 | 5 | 21 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfPageRotationWritingServiceTests.cs | 0 | 1 | 0 | 4 | 21 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfPageStateWritingServiceTests.cs | 0 | 2 | 0 | 4 | 36 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfPageStructuralWritingServiceTests.cs | 0 | 3 | 0 | 6 | 48 | 1 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfPageTransactionWritingServiceTests.cs | 0 | 2 | 0 | 6 | 47 | 1 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalFontResourceCollisionTests.cs | 0 | 0 | 0 | 0 | 13 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalTrueTypeResourceProjectorTests.cs | 1 | 4 | 1 | 0 | 59 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalTrueTypeSubsetResourceProjectorTests.cs | 1 | 4 | 6 | 0 | 65 | 1 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalType0TrueTypeResourceProjectorTests.cs | 2 | 4 | 1 | 0 | 60 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfFontCollisionTestDocument.cs | 0 | 3 | 0 | 0 | 5 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfGeneratedCMapParserTests.cs | 0 | 7 | 0 | 0 | 7 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Pages/PdfPageServiceTests.cs | 0 | 0 | 0 | 0 | 14 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Resources/PdfPageResourceMutationProjectorTests.cs | 0 | 3 | 0 | 0 | 28 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Security/DocumentSecurityServiceTests.cs | 0 | 2 | 0 | 0 | 4 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Security/Trust/DocumentTrustEvaluatorTests.cs | 0 | 0 | 0 | 0 | 35 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfCompoundTextOperatorNormalizerTests.cs | 1 | 6 | 0 | 0 | 14 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfContentStreamMutationCodecLimitTests.cs | 6 | 9 | 1 | 0 | 49 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfContentStreamMutationCodecTests.cs | 2 | 2 | 0 | 0 | 21 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextDiscoveryServiceTests.cs | 0 | 19 | 0 | 0 | 39 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextFindReplaceServiceTests.cs | 0 | 20 | 1 | 0 | 42 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextFontServiceTests.cs | 0 | 13 | 0 | 0 | 41 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextReplacementServiceTests.cs | 0 | 7 | 0 | 0 | 51 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextStyleServiceTests.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextTypographyServiceTests.cs | 0 | 7 | 0 | 0 | 33 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExternalExistingTextFontServiceTests.cs | 0 | 1 | 0 | 0 | 3 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExternalFontCollisionIntegrationTests.cs | 3 | 1 | 0 | 0 | 27 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFilteredTextMutationTests.cs | 1 | 2 | 0 | 0 | 34 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFilteredTextTestDocument.cs | 3 | 14 | 0 | 0 | 14 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFormPlacementCloneProjectorTests.cs | 1 | 9 | 0 | 0 | 36 | 1 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfPositionedTextReplacementServiceTests.cs | 0 | 16 | 0 | 0 | 28 | 0 | Retain |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfTextAdvanceMeasurementServiceTests.cs | 1 | 8 | 0 | 0 | 12 | 0 | Changed |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/LexerTestFactory.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfCharacterClassifierTests.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerCoreTests.cs | 0 | 1 | 0 | 0 | 30 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerExceptionTests.cs | 0 | 0 | 0 | 0 | 7 | 1 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerHexadecimalStringTests.cs | 0 | 1 | 0 | 0 | 36 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerKeywordTests.cs | 0 | 1 | 0 | 0 | 32 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerLiteralStringTests.cs | 0 | 0 | 0 | 0 | 41 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerNameTests.cs | 0 | 0 | 0 | 0 | 24 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerNumberTests.cs | 0 | 0 | 0 | 0 | 30 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerOptionsTests.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfSourcePositionTests.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfSourceSpanTests.cs | 0 | 0 | 0 | 0 | 7 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfTokenTests.cs | 0 | 0 | 0 | 0 | 6 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/TestRandomAccessCursor.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Parsing/PdfTokenReaderTests.cs | 0 | 1 | 0 | 0 | 38 | 0 | Retain |
| tests/BlackFlag.Engine.Syntax.Tests/Parsing/PdfUnexpectedTokenExceptionTests.cs | 0 | 3 | 0 | 0 | 18 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfAnnotationCommonStateWritePlannerTests.cs | 0 | 7 | 0 | 5 | 18 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfAnnotationWriteProjectorTests.cs | 0 | 2 | 0 | 5 | 24 | 1 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfCrossReferenceStreamWriterTests.cs | 0 | 1 | 0 | 0 | 26 | 1 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfDocumentInformationFullRewritePlannerTests.cs | 0 | 6 | 0 | 0 | 28 | 1 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfDocumentInformationIncrementalWritePlannerTests.cs | 0 | 5 | 0 | 0 | 20 | 1 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfDocumentInformationWriteProjectorTests.cs | 0 | 7 | 0 | 0 | 29 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfFullRewriteWriterTests.cs | 2 | 1 | 0 | 0 | 27 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfIncrementalUpdateWriterTests.cs | 1 | 8 | 0 | 0 | 46 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfIncrementalWritePlannerTests.cs | 2 | 7 | 0 | 0 | 16 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfIndirectObjectSerializerTests.cs | 1 | 1 | 0 | 0 | 2 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfNestedPageTreeCrossNodeProjectorTests.cs | 0 | 18 | 0 | 0 | 35 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfNestedPageTreeStructuralProjectorTests.cs | 0 | 11 | 0 | 0 | 32 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfNestedPageTreeWriteProjectorTests.cs | 0 | 6 | 0 | 0 | 31 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfNewAnnotationWritePlannerTests.cs | 0 | 0 | 0 | 5 | 25 | 1 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfNewPageWritePlannerTests.cs | 0 | 0 | 0 | 0 | 36 | 3 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfObjectSerializerTests.cs | 0 | 6 | 0 | 0 | 11 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfPageGeometryWriteProjectorTests.cs | 0 | 7 | 0 | 4 | 12 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfPageTreeWriteProjectorTests.cs | 0 | 0 | 0 | 6 | 18 | 4 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfProjectedFullRewritePlannerTests.cs | 0 | 5 | 0 | 0 | 27 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfProjectedIncrementalWritePlannerTests.cs | 0 | 9 | 0 | 0 | 23 | 0 | Changed |
| tests/BlackFlag.Engine.Writing.Tests/PdfTargetedAnnotationWritePlannerTests.cs | 0 | 6 | 0 | 4 | 19 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfWriteObjectAllocatorTests.cs | 0 | 0 | 0 | 0 | 9 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/PdfWritePlannerTests.cs | 1 | 1 | 0 | 0 | 15 | 0 | Retain |
| tests/BlackFlag.Engine.Writing.Tests/WritingAssemblyTests.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Changes/DocumentChangeApplierTests.cs | 0 | 3 | 0 | 0 | 11 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Changes/DocumentChangeSetTests.cs | 0 | 4 | 0 | 0 | 16 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Commands/CommandDispatcherTests.cs | 1 | 6 | 0 | 0 | 11 | 1 | Retain |
| tests/PiratePDF.DocGen.Tests/GlobalUsings.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Health/DocumentationHealthCalculatorTests.cs | 0 | 0 | 0 | 0 | 12 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Library/DocumentationLibraryTests.cs | 0 | 0 | 0 | 0 | 17 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Parsing/MarkdownMetadataParserTests.cs | 0 | 0 | 0 | 0 | 12 | 0 | Changed |
| tests/PiratePDF.DocGen.Tests/Repair/BrokenDocumentReferenceRepairRuleTests.cs | 0 | 0 | 0 | 0 | 21 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Repair/DocumentationRepairPlannerTests.cs | 0 | 0 | 0 | 0 | 20 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Repair/MissingNextReviewRepairRuleTests.cs | 0 | 0 | 0 | 0 | 12 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Repair/MissingRevisionHistoryRepairRuleTests.cs | 0 | 0 | 0 | 0 | 25 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/TestInfrastructure/DocumentationTestFactory.cs | 0 | 0 | 0 | 0 | 1 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/TestInfrastructure/TemporaryDirectory.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Validation/DocumentationContractIntegrationTests.cs | 0 | 0 | 0 | 0 | 8 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Validation/DocumentationValidatorTests.cs | 1 | 0 | 0 | 0 | 17 | 0 | Retain |
| tests/PiratePDF.DocGen.Tests/Validation/MarkdownStructureValidatorTests.cs | 0 | 0 | 0 | 0 | 17 | 1 | Retain |
| tools/PiratePDF.DocGen/Changes/Application/DocumentChangeApplicationResult.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Changes/Application/DocumentChangeApplier.cs | 3 | 1 | 0 | 0 | 11 | 0 | Retain |
| tools/PiratePDF.DocGen/Changes/DocumentChange.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Changes/DocumentChangeSet.cs | 3 | 0 | 0 | 0 | 5 | 0 | Retain |
| tools/PiratePDF.DocGen/Changes/DocumentChangeType.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Changes/Reporting/DocumentChangeReportWriter.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| tools/PiratePDF.DocGen/Commands/AllCommand.cs | 0 | 2 | 0 | 0 | 1 | 0 | Retain |
| tools/PiratePDF.DocGen/Commands/CommandContext.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Commands/CommandDispatcher.cs | 1 | 0 | 0 | 0 | 3 | 0 | Retain |
| tools/PiratePDF.DocGen/Commands/DoctorCommand.cs | 0 | 0 | 0 | 0 | 9 | 0 | Retain |
| tools/PiratePDF.DocGen/Commands/HealthCommand.cs | 0 | 0 | 0 | 0 | 4 | 0 | Retain |
| tools/PiratePDF.DocGen/Commands/IDocGenCommand.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Commands/IndexCommand.cs | 0 | 0 | 0 | 0 | 7 | 0 | Retain |
| tools/PiratePDF.DocGen/Commands/RepairCommand.cs | 0 | 0 | 0 | 0 | 9 | 0 | Retain |
| tools/PiratePDF.DocGen/Commands/ValidateCommand.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| tools/PiratePDF.DocGen/Generation/TrackedGeneratedFileWriter.cs | 0 | 0 | 0 | 0 | 8 | 0 | Retain |
| tools/PiratePDF.DocGen/Generation/ValidationReportWriter.cs | 1 | 0 | 0 | 0 | 6 | 0 | Retain |
| tools/PiratePDF.DocGen/Health/DocumentationHealthCalculator.cs | 0 | 0 | 0 | 0 | 9 | 0 | Retain |
| tools/PiratePDF.DocGen/Health/DocumentationHealthSnapshot.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Infrastructure/DocumentationLibraryLoader.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| tools/PiratePDF.DocGen/Infrastructure/RepositoryLocator.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| tools/PiratePDF.DocGen/Library/Coverage/DocumentationCoverage.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Library/Coverage/DocumentationCoverageSnapshot.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Library/DocumentationDocument.cs | 4 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Library/DocumentationLibrary.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Library/DocumentationSection.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Library/Queries/DocumentationQueries.cs | 6 | 0 | 0 | 0 | 1 | 0 | Retain |
| tools/PiratePDF.DocGen/Library/Relationships/DocumentationRelationships.cs | 3 | 0 | 0 | 0 | 5 | 0 | Retain |
| tools/PiratePDF.DocGen/Library/RevisionHistoryEntry.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Library/Statistics/DocumentationStatistics.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Models/DocumentMetadata.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Models/DocumentStatuses.cs | 0 | 1 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Models/DocumentationContract.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Parsing/MarkdownDocumentParser.cs | 2 | 3 | 0 | 0 | 27 | 0 | Retain |
| tools/PiratePDF.DocGen/Parsing/MarkdownMetadataParser.cs | 1 | 0 | 0 | 0 | 11 | 0 | Changed |
| tools/PiratePDF.DocGen/Program.cs | 0 | 2 | 0 | 0 | 10 | 0 | Retain |
| tools/PiratePDF.DocGen/Properties/AssemblyInfo.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Diagnostics/RepairDiagnostic.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Diagnostics/RepairDiagnosticSeverity.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/DocumentationRepairPlan.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/DocumentationRepairPlanner.cs | 0 | 0 | 0 | 0 | 3 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Metadata/MarkdownMetadataLocator.cs | 0 | 0 | 0 | 0 | 17 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Metadata/MarkdownMetadataValueSpan.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/References/DocumentationRedirectMap.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Rules/BrokenDocumentReferenceRepairRule.cs | 1 | 0 | 0 | 0 | 5 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Rules/CanonicalStatusRepairRule.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Rules/DocumentChangeBuilder.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Rules/IDocumentRepairRule.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Rules/IsoDateRepairRule.cs | 0 | 1 | 0 | 0 | 1 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Rules/MissingNextReviewRepairRule.cs | 0 | 0 | 0 | 0 | 6 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Rules/MissingRevisionHistoryRepairRule.cs | 0 | 0 | 0 | 0 | 8 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Rules/MissingTrailingNewlineRepairRule.cs | 0 | 0 | 0 | 0 | 2 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Rules/RepairRuleContext.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Repair/Rules/RepairRuleRegistry.cs | 0 | 1 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Reporting/DocumentationHealthReportWriter.cs | 0 | 0 | 0 | 0 | 5 | 0 | Retain |
| tools/PiratePDF.DocGen/Validation/DocumentationValidationResult.cs | 1 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Validation/DocumentationValidator.cs | 1 | 1 | 0 | 0 | 4 | 0 | Retain |
| tools/PiratePDF.DocGen/Validation/MarkdownStructureValidator.cs | 0 | 0 | 0 | 0 | 11 | 0 | Retain |
| tools/PiratePDF.DocGen/Validation/ValidationIssue.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
| tools/PiratePDF.DocGen/Validation/ValidationSeverity.cs | 0 | 0 | 0 | 0 | 0 | 0 | Retain |
