# Post-BF-1054 Batch B3 - Allocated Font Resource Name

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-012 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053, BF-1054 |
| **Last Updated** | 2026-09-08 |
| **Next Review** | 2026-12-08 |

## Scope and status

The user approved **B3 only: POST-P0-003, allocated font resource name discarded after a collision**, from the [repository audit](Post-BF-1054-CSharp-Repository-Audit.md). The starting tree was clean on `maintenance/post-BF-1054-repository-review` at `261c848248a6a279fff3a626b4d29a9ddd36ff27`, with B1 and B2 incorporated. Root `AGENTS.md` and relevant governing architecture, preservation, resource and external-text contracts were read before implementation.

The B3 correction and validation are complete. The full unmodified release gate passed with **1,789 tests**, zero build warnings/errors, and clean documentation/repair/whitespace checks. No other approved or proposed batch is implemented here. No commit, push, merge, tag or branch switch was performed.

## Characterization before production changes

| Projector | Preferred name input | Canonical allocation and result | Original defect |
|---|---|---|---|
| `PdfExternalTrueTypeResourceProjector.Create` | `resourceName` argument | `PdfPageResourceMutationProjector.Create` inserts the font reference and returns `ResourceName` | Returned `PdfExternalFontResourceProjection` used the original argument |
| `PdfExternalTrueTypeSubsetResourceProjector.Create` | `resourceName` argument | Same existing mutation projector and result | Returned `PdfExternalFontResourceProjection` used the original argument |
| `PdfExternalType0TrueTypeResourceProjector.Create` | `resourceName` argument | Same existing mutation projector and result | Returned `PdfExternalType0TrueTypeResourceProjection` used the original argument |

The resource mutation projector already resolves resource ownership, allocates the collision-free name and preserves other mappings using copy-on-write. With occupied `FExt1`, it inserts `FExt2` but the font projectors reported `FExt1`. `PdfExternalExistingTextFontService.ChangeFont` passes its preferred name into the Type0 projector, then uses `projection.ResourceName` in `PdfFontSwitchTokenEditor.Wrap` and the Editing operation. Consequently the original service emitted a `Tf` selecting the old font. The consumer was correct; its projection metadata was wrong.

Before the production fix, the new projector suite produced **21 failures and 12 passes**: collision and sequential-allocation assertions failed while free-name assertions passed. The four external-text transaction cases also failed. This confirmed the complete allocation-to-`Tf` chain before editing production code.

## Exact production files changed

| File | B3 correction |
|---|---|
| `src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeResourceProjector.cs` | Return `resourceMutation.ResourceName` instead of `resourceName` |
| `src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeSubsetResourceProjector.cs` | Return `resourceMutation.ResourceName` instead of `resourceName` |
| `src/BlackFlag.Engine.Services/Fonts/PdfExternalType0TrueTypeResourceProjector.cs` | Return `resourceMutation.ResourceName` instead of `resourceName` |

Each correction changes one constructor argument. The user independently changed `new PdfDictionary()` to `[]` in the subset projector and confirmed ownership of that edit during validation; it is preserved. It is not part of the resource-name correction.

The text service, allocator, Writing and Editing implementations are unchanged. No public signature, dependency, project reference, embedding permission, sanitized-font ownership, subset bound, malformed-font handling, resource inheritance policy or exception branch changes.

## Exact test files changed or added

| File | Change |
|---|---|
| `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalFontResourceCollisionTests.cs` | Added 33 projector cases: all three projectors, free and occupied preferred names across four ownership layouts, and sequential allocations |
| `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfFontCollisionTestDocument.cs` | Added shared deterministic fixture with a real sibling page, nonzero generations, unknown entries and exact object/byte snapshots |
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfExternalFontCollisionIntegrationTests.cs` | Added 24 cases: four transaction/undo/redo, four later-failure rollback and sixteen supported Writing save combinations |
| `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalType0TrueTypeResourceProjectorTests.cs` | Expose the existing synthetic font builder internally and parameterize its permission bits; existing default remains unchanged |
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfFilteredTextTestDocument.cs` | Parameterize direct-show fixture text, preserving the existing default, to reuse the synthetic font's supported `A` glyph |

The new fixture explicitly uses embedding-permitted synthetic input; it does not change embedding policy or use system fonts. Existing assertions remain intact.

## Behavior and preservation evidence

- Free `FExt1` remains `FExt1`. An occupied name returns `FExt2`; a subsequent allocation returns `FExt3`. All three projectors retain the old mapping and point each new name to its own projected font object.
- Direct resources, indirect resources, inherited resources and an indirect Font category retain established copy-on-write behavior. Original source owners and the sibling page retain their original objects and serialized bytes. Unrelated aliases, `ProcSet` and private dictionary keys survive.
- The old font remains `9 7 R`; indirect resource/category owners remain `10 3 R` and `11 4 R`; the sibling remains `12 1 R`. New fonts have generation zero. Content remains `4 2 R`.
- Real external-text editing emits `/FExt2 12 Tf`, then restores `/F1 12 Tf` for the following unchanged `Next` text. The new Type0 resource is `18 0 R`; the old `FExt1` remains bound to Courier.
- Undo restores the exact original object identifiers and serialized bytes; redo restores the complete changed snapshot. A second, cancelled change after a successful first change throws the original `OperationCanceledException` with its token. Transaction disposal rolls back every earlier content/resource/font mutation and creates no undo entry.
- Existing exception behavior is preserved: the production fix adds no validation, catch, translation or cancellation change. The only intentional observable change is reporting and emitting the canonical allocated name after a collision.

## Save and reopen validation

All **16 combinations** passed: four ownership layouts, full rewrite or incremental update, and traditional xref tables or xref streams. Permanent tests invoke the existing Writing APIs after an actual committed external-text change. Incremental planning includes the eight changed/new objects and verifies the entire original PDF prefix byte-for-byte.

A temporary validation executable, `B3Reopen.csproj`, in the existing local audit scratch directory (`%TEMP%/PiratePDF-PostBF1054-Audit-92e26a4`) references already-built assemblies without adding repository project references or dependencies. Run after the Release test build with:

```powershell
dotnet run --project "$env:TEMP\PiratePDF-PostBF1054-Audit-92e26a4\B3Reopen.csproj" --configuration Release --no-restore
```

It reopens bytes returned by `PdfExternalFontCollisionIntegrationTests.CreateSavedDocument`, parses xref chains, resolves indirect objects and parses decoded content operations using the repository Parsing APIs. It verifies `F1, FExt2, F1` as the actual `Tf` sequence; `PdfResourceResolver` resolves `FExt2` to Type0 font identity `18 0`. It follows descendant `17 0`, descriptor `16 0` and embedded font program `13 0`, and checks the saved ToUnicode mapping bytes `<0001> <0041>` for the shown CID. It checks one/two revisions, preserved generations, old font bindings, shared resource ownership, unknown keys, following text, filter alias/array, decode-parameter representation and encoded length.

The parser executable is supplemental local validation, not a permanent release-gate test. Permanent tests retain all sixteen saved-document generators and serialization assertions. No visual rendering or successful CMap Unicode decoding is claimed.

## Separate limitations discovered during validation

### Existing public save facade gap

`src/BlackFlag.Engine.Services/Editing/PdfEditingSaveService.cs`, `ResolveDomain`, does not handle `PdfExistingTextResourceChangeOperation`. All sixteen initial save cases failed with `NotSupportedException` identifying that operation as lacking a public save orchestration path. Save/reopen evidence therefore uses supported `PdfFullRewriteWriter` and explicit changed-object `PdfIncrementalWritePlanner`/`PdfIncrementalUpdateWriter` APIs. This validates B3 resource serialization, not public save-facade support for this operation. The gap predates and is independent of the returned-name correction; it remains unchanged and requires separately authorized work.

### Existing CMap tokenizer resource-exhaustion defect (P0 candidate)

File: `src/BlackFlag.Engine.Rendering/Fonts/PdfCMapParser.cs`; boundary: `Parse` materializes `Tokenize(source).ToArray()`.

The initial reopen executable resolved the saved font and then attempted to parse its ToUnicode CMap. It terminated with `Out of memory`. Source inspection identifies a progress defect: the `<` branch consumes through the first `>` of a `<< ... >>` dictionary, leaving the second `>`; the general token loop excludes `>` but has no branch that consumes or rejects it. It repeatedly yields an empty token at the same position, and `ToArray()` grows without a bound. The existing `PdfCidToUnicodeCMapBuilder` emits this dictionary form in `/CIDSystemInfo`. A standalone `>` is also a source-established minimal trigger; do not run such a reproduction without process time/memory limits.

Classification: P0 resource-exhaustion/correctness candidate, observed process failure plus source-established non-progress. Scope and impact: CMap inputs containing an unconsumed closing delimiter can stall parsing and exhaust memory, including valid generated dictionary syntax. The B3 name change neither creates nor modifies the CMap bytes or parser. Required future work is a separately approved tokenizer correction with deterministic progress/delimiter tests, valid dictionary/CMap coverage, bounded materialization and process-isolated hostile-input validation, followed by font decode integration and the release gate. It is not fixed, and no existing test or gate is weakened. The reopened resource-name assertions use the real PDF parser/resource resolver; only the additional CMap decode experiment was replaced with an explicit saved-mapping-byte assertion. This limits the validation claim to font selection and preservation.

## Validation results

| Validation | Result |
|---|---|
| New focused B3 cases | 57 passed, 0 failed, 0 skipped |
| Focused B3 plus existing font projectors, page-resource mutation and external-text service cases | 92 passed, 0 failed, 0 skipped |
| Complete Services project | 281 passed, 0 failed, 0 skipped |
| Complete SDK.Adapters project | 34 passed, 0 failed, 0 skipped |
| Supplemental parser-based reopen | 16 combinations passed, with limitations above |
| `git diff --check`, including new files | Passed |
| Full unmodified PiratePDF release gate | Passed: 1,789 tests across 20 projects; 0 failed/skipped; 0 build warnings/errors |

The gate inspected 1,024 source files and validated 179 documents with zero documentation errors, warnings or information findings. Documentation health is 91.6% (A-); repair preview proposes zero changes. Its initial sandboxed restore could not read the user NuGet.Config; the identical, already-authorized gate rerun outside the sandbox completed successfully without configuration or dependency changes. After recording these results, DocGen validation/health/repair and whitespace checks were refreshed for the final documentation wording.

Targeted command (Release, no restore):

```powershell
dotnet test tests/BlackFlag.Engine.Services.Tests/BlackFlag.Engine.Services.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~PdfExternalFontResourceCollisionTests|FullyQualifiedName~PdfExternalFontCollisionIntegrationTests|FullyQualifiedName~PdfExternalExistingTextFontServiceTests|FullyQualifiedName~PdfPageResourceMutationProjectorTests|FullyQualifiedName~PdfExternalTrueTypeResourceProjectorTests|FullyQualifiedName~PdfExternalTrueTypeSubsetResourceProjectorTests|FullyQualifiedName~PdfExternalType0TrueTypeResourceProjectorTests' --verbosity quiet
```

The complete Services and SDK.Adapters projects were run with `dotnet test <project> --configuration Release --no-restore --verbosity quiet`. The acceptance gate is the unchanged `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1`.

## Documentation and remaining findings

This completion report and the audit's B3 disposition are the only hand-authored documentation changes. DocGen refreshed `docs/Documentation-Index.md`, `docs/ValidationReport.md` and `docs/Documentation-Health.md` through its normal commands. `docs/Document-Changes.md` remains unchanged with zero proposed repairs.

**POST-P0-004 onward and B4 through B11 remain untouched.** No canonical font architecture, embedding-permission fix, sanitizer ownership/bounds fix, other P0/P2 finding or historical milestone was changed. The separate save-facade and CMap limitations above are recorded for approval, not implemented. B3 does not authorize another batch.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-08 | Record B3 resource-name propagation, collision and transaction tests, save/reopen evidence, and separate validation limitations. |
