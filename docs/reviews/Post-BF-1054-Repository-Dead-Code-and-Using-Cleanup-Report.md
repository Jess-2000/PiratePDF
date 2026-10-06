# Post-BF-1054 Repository Dead-Code and Using Cleanup

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-027 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049, BF-1050, BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-22 |
| **Next Review** | 2026-12-22 |

## Outcome and scope

This conservative hygiene pass removes **151 imports, 16 implementation members and six unused private-helper parameters**. No public/protected API, interface, override, plugin, serialization, native, dependency or project-reference contract changes are intended. No behavioral test or assertion was removed or changed.

All fifteen recorded P0 findings remain closed. POST-P2-001 through POST-P2-008 remain open and untouched. Canonical font editing was not started. No commit, push, merge, tag or branch switch was performed.

## Baseline and inventory

- Branch: `maintenance/post-BF-1054-repository-review`.
- Starting HEAD: `8450d7ecec0e6d63e0ce2939b9d4edae7ffedcd1` (B7 committed).
- Initial working tree and diff: clean.
- Baseline Release solution build: passed, **0 warnings / 0 errors**.
- Baseline full gate was not repeated before edits; the accepted B7 checkpoint is **2,378 tests across 20 projects**.
- Governing context: current AGENTS.md, PROJECT-CHARTER.md, README.md, engineering standards, current audit and B7 report, BF-1050 plugin/SDK contracts, preservation/ownership contracts, native-loader and platform-provider paths, and existing AOT/trimming limitations.
- The release gate is unchanged. The intentionally removed standalone `Test-PiratePDFSourceFileSizes.ps1` remains absent. Historical documentation was not rewritten.

| Scope | Hand-authored C# files | Projects |
|---|---:|---:|
| Production, including SDK/adapters/platform/plugins | 719 | 19 |
| Tests, including the runtime test plugin | 295 | 21 |
| Tools | 60 | 1 |
| Samples | 1 | 1 |
| **Total** | **1,075** | **42** |

The file set reconciles exactly with tracked C# and evaluated project compilations, including **10 hand-authored AssemblyInfo.cs files**. Generated bin/obj assembly attributes, implicit global usings and target-framework attributes were included as compilation context, but excluded from hand edits/counts. No tracked vendored/generated C# was identified by project conventions, source headers or generated-code attributes. Package caches, generated documentation and external temporary acceptance/review harnesses are excluded. The pre-existing empty DocumentChangeBuilder.cs remains unchanged; no deletion made another source file empty.

## Analysis method and limitations

A temporary external review program used the installed .NET SDK 10.0.302 Roslyn/MSBuild assemblies; no repository dependency, analyzer policy or project reference was added. Every hand-authored syntax tree was scanned in evaluated Debug and Release compilations. Compilation errors were treated as analysis failures. All projects target net10.0; project/props and source inspection found no additional conditional target or hand-authored #if/#elif path. Runtime Windows/macOS branches were reviewed and preserved.

Reference inventory used assembly-qualified original symbols across projects, all partial declarations, constructors, expressions, method groups, attributes, nameof/typeof, generic references, operators and XML cref syntax. It is an **apparent explicit-reference inventory, not a reachability proof**: generated record/indexer members, interface/runtime dispatch and external callers require retention. Friend assemblies, reflection, plugin manifests/activation, native loading, test discovery, factories, scripts and documentation were reviewed separately before deleting a candidate.

A candidate inventory was classified before member deletion, with exact symbol, file/line, accessibility/kind, explicit reference count, contract/dynamic risk and disposition. Every removed whole member had zero semantic repository references, private/internal accessibility, no interface/override duty, and no identified reflection, plugin, test-discovery, native, serialization, conditional or documented external entry path. Internal removals were checked against friend assemblies. Public/protected members were not eligible for removal.

The final semantic pass reports **zero remaining compiler-identified unused hand-authored imports** and **no newly orphaned members**. All **5,914 public/protected declaration signature records** match the baseline, including public members of non-public containing types. This is a source-symbol API comparison, not a claim of byte-identical assemblies or exhaustive binary-equivalence proof. The diff contains only the listed removals, their private call-site argument deletions and minimal deletion-related whitespace.

No benchmark or assembly-size claim is made. Runtime performance, AOT/trimming, platform packaging and signed-release readiness are not established by this cleanup.

## Imports

**150 baseline imports plus one newly orphaned import were removed across 73 files: 150 ordinary, one global, zero aliases, zero static imports.** Roslyn CS8019 supplied unused-import evidence; CS8933 corroborated seven duplicates of global imports. Duplicate diagnostics for the same directive are counted once. Debug and Release baseline import sets agree.

The global removal is `global using System.Collections.Generic;` in tests/BlackFlag.Engine.Document.Tests/GlobalUsings.cs, proven unnecessary at project scope. Of 43 hand-authored global directives, 42 remain. Generated implicit global-using policy is unchanged. Aliases, static imports, extension-method resolution and XML documentation references were considered semantically; no live expression was qualified or rewritten to remove an import.

Removing the unused string-tuple CreatePackage overload orphaned `using System.Linq;` in OoxmlWordSecurityAnalyzerTests.cs. It was removed in the final pass. No newly orphaned member was removed.

## Candidate accounting

The baseline inventory contains **2,989 apparent zero-explicit-reference candidates: 2,330 non-parameter symbols and 659 parameters**. These are deliberately broader than truly unused code. **16 members and six parameters were removed; 2,967 candidates were retained**, including five uncertain internal symbols. Of retained non-parameter symbols, 2,314 remain. Counts below use declared accessibility; Roslyn assigns parameters NotApplicable, and all six removed parameters belong to private methods.

| Accessibility | Candidates | Removed | Retained | Uncertain subset |
|---|---:|---:|---:|---:|
| Private | 37 | 14 | 23 | 0 |
| Internal | 9 | 2 | 7 | 5 |
| Protected | 7 | 0 | 7 | 0 |
| Public | 2,277 | 0 | 2,277 | 0 |
| Parameters / NotApplicable | 659 | 6 | 653 | 0 |
| **Total** | **2,989** | **22** | **2,967** | **5** |

| Kind | Candidates | Removed | Retained |
|---|---:|---:|---:|
| Method | 1,874 | 5 | 1,869 |
| Constructor | 7 | 0 | 7 |
| Property | 121 | 0 | 121 |
| Field, including enum values/constants | 54 | 11 | 43 |
| Type | 258 | 0 | 258 |
| Operator/conversion | 16 | 0 | 16 |
| Parameter | 659 | 6 | 653 |
| Event / delegate / local function / other | 0 | 0 | 0 |

Overload candidates are a subset of methods/constructors: **69 candidates, one removed, 68 retained**. Six parameters were removed from five surviving private helpers. Parameters and the lambda contained within deleted methods are not counted again as standalone removals. No constructor, property, event, delegate, type, local function, public constant or enum value was deleted. No file or substantial commented-out implementation was deleted; reviewed code-like comments retain explanatory value. No suppression was changed and no unreachable live control-flow block was rewritten.

## Retained candidates and contract evidence

The primary dispositions partition retained candidates as follows; examples below may overlap these categories.

| Primary disposition | Count |
|---|---:|
| Interface/override, including 232 parameters | 607 |
| Plugin/framework/test discovery | 1,774 |
| Public/protected API | 160 |
| Synthesized data-model/primary-constructor parameters | 406 |
| Delegate/lambda callback parameters | 15 |
| Uncertain internal architecture/convention | 5 |
| **Total retained** | **2,967** |

- **API and dispatch:** all 2,277 public and seven protected candidates remain, regardless of their primary disposition. Explicit IEnumerable implementations, dictionary snapshot interfaces, bounded image decoding, equality/hash, ToString, visitors, disposal and throw/no-op implementations remain. Interface-map and SDK-boundary tests remain intact.
- **Plugins and reflection:** both zero-reference entry types, ReferencePlugin and SamplePlugin, remain with their constructors and lifecycle/disposal methods. Manifests name the entry types; PluginRuntimeHost resolves them and calls Activator.CreateInstance. PluginPackagePath.Contains and the non-public resource-change constructor used by reflection tests remain; they also have ordinary references and are not additional zero-reference candidates. Reflection roots were reviewed, not removed.
- **Native/platform:** PluginLoadContext.Load and LoadUnmanagedDll remain as runtime overrides, including the native-loading callback. No hand-authored DllImport/LibraryImport callback was found. Skia integration and both platform font providers remain. Zero candidates required retention because of preprocessor-only target branches; runtime platform behavior is still contractual.
- **Framework/tooling:** attributed facts/theories, MemberData, fixtures, test constructors, the tool entry point, registrations and sample lifecycle remain. Of the 1,774 plugin/framework candidates, two are plugin entry types and the remaining 1,772 are test/framework candidates. The entry point is retained regardless of whether a direct-call query identifies it as a candidate.
- **Data models/serialization:** 406 zero-explicit-use parameters belong to synthesized data-model/primary-constructor signatures; they were not treated as unused based on body references. A further 15 delegate/lambda callback parameters remain because the callable signature is required. Public model properties, enum values, constants, exceptions and serialization-facing members remain.
- **Cancellation:** all 27 apparent unused CancellationToken parameter candidates remain as interface/override/adapter contracts. No cancellation propagation or POST-P2-001 implementation was attempted.
- **Architecture/uncertainty:** PdfCatalog.ObjectIdentifier and GetRequiredObjectIdentifier remain beside the established binding model; ParsingAssemblyMarker, RenderingAssemblyMarker and WritingAssemblyMarker remain with their public constants. Their five internal-symbol candidates were retained because the broader identity/marker convention does not justify deletion on static counts alone. This is conservative uncertainty, not a claim that each exact member is explicitly mandated by documentation.

## Exact removed symbols

Names and source locations below refer to the baseline. Whole-member removals share the zero-reference and contract review above. Field initializers were inspected: fixed valid PdfName construction only stores/copies known values, and PdfFunctionEvaluator construction has no custom constructor, initializer or side effect. Removing those unread fields does not alter a live security/classification/conversion path. No allocation-performance claim follows.

For private parameter removals, every caller is direct and controlled, no method-group/delegate/reflection binding exists, and removed arguments are variable reads or the side-effect-free KeyValuePair.Value getter. Existing validation remains: style graphics state is still validated and used by the suffix, and DocGen diagnostics are still validated at the interface boundary. Live exception timing, cancellation and resource disposal remain unchanged. The exceptions inside the two dead lexer helpers were unreachable; the active dictionary/hexadecimal recognizer is retained.

| Fully qualified baseline symbol | File | Accessibility / kind | Reason |
|---|---|---|---|
| `BlackFlag.Engine.Editing.Documents.PdfEditingSession.GetDocument()` | `src/BlackFlag.Engine.Editing/Documents/PdfEditingSession.cs` | Internal / Method | Uncalled implementation; no dynamic or compatibility contract. |
| `BlackFlag.Engine.Fonts.PdfExternalFontMatch.OpenUntrustedFontProgram()` | `src/BlackFlag.Engine.Fonts/PdfExternalFontMatch.cs` | Internal / Method | Uncalled implementation; no dynamic or compatibility contract. |
| `BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.AnalysisState.ShouldRecordPresence(BlackFlag.Engine.Objects.PdfName, BlackFlag.Engine.Objects.PdfDictionary, BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.VisitContext) :: parent` | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.AnalysisState.cs` | Private owner / Parameter | Unused private parameter; equivalent controlled argument removal. |
| `BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.AnalysisState.RecordPresenceObservation(BlackFlag.Engine.Objects.PdfName, BlackFlag.Engine.Objects.PdfObject, BlackFlag.Engine.Objects.PdfDictionary, string, BlackFlag.Engine.Parsing.CrossReferences.PdfObjectId?, BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.VisitContext) :: value` | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.AnalysisState.cs` | Private owner / Parameter | Unused private parameter; equivalent controlled argument removal. |
| `BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.AnalysisState.RecordPresenceObservation(BlackFlag.Engine.Objects.PdfName, BlackFlag.Engine.Objects.PdfObject, BlackFlag.Engine.Objects.PdfDictionary, string, BlackFlag.Engine.Parsing.CrossReferences.PdfObjectId?, BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.VisitContext) :: parent` | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.AnalysisState.cs` | Private owner / Parameter | Unused private parameter; equivalent controlled argument removal. |
| `BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.NamesKey` | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.cs` | Private / Field | Unread field; fixed side-effect-free initializer. |
| `BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.FileAttachmentName` | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.cs` | Private / Field | Unread field; fixed side-effect-free initializer. |
| `BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.RichMediaName` | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.cs` | Private / Field | Unread field; fixed side-effect-free initializer. |
| `BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.ScreenName` | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.cs` | Private / Field | Unread field; fixed side-effect-free initializer. |
| `BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.MovieName` | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.cs` | Private / Field | Unread field; fixed side-effect-free initializer. |
| `BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.SoundName` | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.cs` | Private / Field | Unread field; fixed side-effect-free initializer. |
| `BlackFlag.Engine.Parsing.Security.PdfSecurityAnalyzer.WidgetName` | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.cs` | Private / Field | Unread field; fixed side-effect-free initializer. |
| `BlackFlag.Engine.Rendering.Color.PdfColorConverter.Lab(BlackFlag.Engine.Rendering.Resources.PdfResolvedColorSpace, System.Collections.Generic.IReadOnlyList<double>, double) :: r` | `src/BlackFlag.Engine.Rendering/Color/PdfColorConverter.cs` | Private owner / Parameter | Unused private parameter; equivalent controlled argument removal. |
| `BlackFlag.Engine.Rendering.Fonts.PdfFontDecoder.EncodingKey` | `src/BlackFlag.Engine.Rendering/Fonts/PdfFontDecoder.cs` | Private / Field | Unread field; fixed side-effect-free initializer. |
| `BlackFlag.Engine.Rendering.Shadings.PdfShadingDecoder.functions` | `src/BlackFlag.Engine.Rendering/Shadings/PdfShadingDecoder.cs` | Private / Field | Unread field; fixed side-effect-free initializer. |
| `BlackFlag.Engine.Services.Text.PdfExistingTextStyleTokenEditor.BuildPrefix(BlackFlag.Engine.Editing.Text.PdfExistingTextDescriptor, BlackFlag.Engine.Rendering.DisplayList.PdfGraphicsStateSnapshot, BlackFlag.Engine.Services.Text.PdfExistingTextStyleUpdate) :: state` | `src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleTokenEditor.cs` | Private owner / Parameter | Unused private parameter; equivalent controlled argument removal. |
| `BlackFlag.Engine.Syntax.Lexing.PdfLexer.ReadDictionaryStart()` | `src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Delimiters.cs` | Private / Method | Uncalled implementation; no dynamic or compatibility contract. |
| `BlackFlag.Engine.Syntax.Lexing.PdfLexer.GetHexadecimalDigitValue(byte)` | `src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Hexadecimal.cs` | Private / Method | Uncalled implementation; no dynamic or compatibility contract. |
| `BlackFlag.Engine.Security.Tests.Formats.Ooxml.OoxmlWordSecurityAnalyzerTests.CreatePackage(params (string Name, string Content)[])` | `tests/BlackFlag.Engine.Security.Tests/Formats/Ooxml/OoxmlWordSecurityAnalyzerTests.cs` | Private / Method | Uncalled implementation; no dynamic or compatibility contract. |
| `BlackFlag.Engine.Services.Tests.Editing.PdfEditingSaveConformanceTests.ContentsName` | `tests/BlackFlag.Engine.Services.Tests/Editing/PdfEditingSaveConformanceTests.cs` | Private / Field | Unread field; fixed side-effect-free initializer. |
| `BlackFlag.Engine.Writing.Tests.PdfNewAnnotationWritePlannerTests.ParentName` | `tests/BlackFlag.Engine.Writing.Tests/PdfNewAnnotationWritePlannerTests.cs` | Private / Field | Unread field; fixed side-effect-free initializer. |
| `PiratePDF.DocGen.Repair.Rules.IsoDateRepairRule.PlanDateRepair(PiratePDF.DocGen.Repair.Rules.RepairRuleContext, System.Collections.Generic.Dictionary<string, PiratePDF.DocGen.Repair.Metadata.MarkdownMetadataValueSpan>, string, System.Collections.Generic.List<PiratePDF.DocGen.Changes.DocumentChange>, System.Collections.Generic.List<PiratePDF.DocGen.Repair.Diagnostics.RepairDiagnostic>) :: diagnostics` | `tools/PiratePDF.DocGen/Repair/Rules/IsoDateRepairRule.cs` | Private owner / Parameter | Unused private parameter; equivalent controlled argument removal. |

## Validation

| Check | Result |
|---|---|
| Baseline and post-cleanup Release solution builds | Passed, 0 warnings / 0 errors |
| Individual deletion-group builds/tests | Syntax 304; Parsing 219; Security 92; Rendering 97; Writing 153; Services 524; Editing 133; Fonts 228; DocGen 66 passed |
| Final complete affected coverage before gate | Full solution: 2,378 passed across 20 projects, zero failed/skipped |
| Final compiler import/API review | 0 unused imports; 5,914 API signature entries unchanged; no newly orphaned members |
| Full unmodified release gate | Passed, exit 0: restore, build/analyzers, all 2,378 tests across 20 projects, documentation and whitespace; zero failed/skipped tests |
| Integrated source size | Passed, 1,066 files; existing reviewed files remain GraphicsInterpreter.Execution 943 lines and OpenTypeFontSanitizer 844 lines |
| DocGen generation / validation / health / repair | Passed: 194 documents, 0 errors/warnings/information, 91.6% A-, repair 0 proposed changes/files |
| git diff --check | Passed before the gate, inside the gate and after final report reconciliation |

The acceptance command was `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1`, with no gate changes or skipped stages. No test was added, deleted or weakened: the final count is exactly the B7 checkpoint, a delta of zero. Source-size scope differs from the C# inventory because the gate includes PowerShell and excludes AssemblyInfo by its established rules; this cleanup included hand-authored AssemblyInfo in analysis.

## Source reduction and complete C# file inventory

**85 C# files touched: 54 production, 30 tests, one tool; six lines added, 253 removed, net reduction 247.** Added lines are the surviving private method/call syntax after parameter deletion. These are source metrics, not a quality or performance score. Files deleted: zero. Newly orphaned members removed: zero. Substantial commented-out code removed: zero.

The table records every changed C# file and the exact import count; member/parameter removals are enumerated above. Imports in files with member edits were analyzed again after those edits.

| Changed C# file | Imports removed |
|---|---:|
| src/BlackFlag.Engine.Editing/Annotations/PdfEditableAnnotationCollection.cs | 1 |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.cs | 11 |
| src/BlackFlag.Engine.Editing/Documents/PdfEditingSession.cs | 0 |
| src/BlackFlag.Engine.Fonts/Embedding/PdfUnicodeTrueTypeGlyphSubsetter.cs | 1 |
| src/BlackFlag.Engine.Fonts/PdfExternalFontMatch.cs | 0 |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReference.cs | 3 |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Helpers.cs | 1 |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.ColorSpaces.cs | 3 |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.Fonts.cs | 3 |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.GraphicsResources.cs | 3 |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.Helpers.cs | 2 |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.Values.cs | 2 |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.XObjects.cs | 3 |
| src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.AnalysisState.cs | 0 |
| src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.cs | 2 |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.Aes256.cs | 1 |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.Configuration.cs | 3 |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.Legacy.cs | 1 |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.cs | 1 |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Helpers.cs | 8 |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Images.cs | 7 |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Paths.cs | 7 |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Shadings.cs | 5 |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Text.cs | 6 |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.cs | 1 |
| src/BlackFlag.Engine.Rendering/Color/PdfColorConverter.cs | 0 |
| src/BlackFlag.Engine.Rendering/Fonts/PdfFontDecoder.cs | 0 |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.CalculatorMachine.cs | 4 |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.CalculatorParser.cs | 3 |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.CalculatorTokens.cs | 4 |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.cs | 1 |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.GraphicsState.cs | 1 |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Operands.cs | 2 |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Paths.cs | 3 |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.cs | 2 |
| src/BlackFlag.Engine.Rendering/Nested/IPdfNestedGraphicsProvider.cs | 1 |
| src/BlackFlag.Engine.Rendering/Shadings/PdfShadingDecoder.cs | 0 |
| src/BlackFlag.Engine.SDK/Plugins/Capabilities/PluginCapabilityKey.cs | 1 |
| src/BlackFlag.Engine.Security/Formats/Markdown/MarkdownSecurityAnalyzer.cs | 1 |
| src/BlackFlag.Engine.Security/Formats/Ole/LegacyWordBinarySecurityAnalyzer.cs | 1 |
| src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordSecurityAnalyzer.cs | 1 |
| src/BlackFlag.Engine.Security/Formats/OpenDocument/OpenDocumentTextSecurityAnalyzer.cs | 1 |
| src/BlackFlag.Engine.Security/Formats/Rtf/RtfSecurityAnalyzer.cs | 1 |
| src/BlackFlag.Engine.Services/Editing/PdfNestedPageTreeStructuralWritingService.cs | 1 |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeSubsetResourceProjector.cs | 1 |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalType0TrueTypeResourceProjector.cs | 1 |
| src/BlackFlag.Engine.Services/Text/PdfExistingFontSelectionService.cs | 1 |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextFontService.cs | 1 |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleTokenEditor.cs | 0 |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextTypographyService.cs | 1 |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Delimiters.cs | 0 |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Hexadecimal.cs | 0 |
| src/BlackFlag.Engine.Writing/PdfDocumentInformationIncrementalWritePlanner.cs | 1 |
| src/BlackFlag.Engine.Writing/PdfPageTreeHierarchySnapshot.cs | 1 |
| tests/BlackFlag.Engine.Document.Tests/GlobalUsings.cs | 1 |
| tests/BlackFlag.Engine.Editing.Tests/Hardening/PdfStructuralEditingAdversarialTests.cs | 1 |
| tests/BlackFlag.Engine.Editing.Tests/Hardening/PdfStructuralEditingScaleTests.cs | 1 |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterDecodeContextTests.cs | 2 |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterDecodeLimitsTests.cs | 2 |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterPipelineTests.cs | 1 |
| tests/BlackFlag.Engine.Fonts.Tests/PdfExternalFontCatalogTests.cs | 1 |
| tests/BlackFlag.Engine.Fonts.Tests/PdfOpenTypeFontSanitizerNumericTests.Cmap.cs | 2 |
| tests/BlackFlag.Engine.Objects.Tests/PdfBooleanTests.cs | 1 |
| tests/BlackFlag.Engine.Objects.Tests/PdfHexStringTests.cs | 1 |
| tests/BlackFlag.Engine.Objects.Tests/PdfIntegerTests.cs | 1 |
| tests/BlackFlag.Engine.Objects.Tests/PdfLiteralStringTests.cs | 1 |
| tests/BlackFlag.Engine.Objects.Tests/PdfNameTests.cs | 1 |
| tests/BlackFlag.Engine.Objects.Tests/PdfNullTests.cs | 1 |
| tests/BlackFlag.Engine.Objects.Tests/PdfObjectTests.cs | 1 |
| tests/BlackFlag.Engine.Objects.Tests/PdfObjectVisitorTests.cs | 1 |
| tests/BlackFlag.Engine.Objects.Tests/PdfPrimitiveVisitorTests.cs | 1 |
| tests/BlackFlag.Engine.Objects.Tests/PdfRealTests.cs | 1 |
| tests/BlackFlag.Engine.Rendering.Tests/Raster/PdfRenderWorkBudgetTests.cs | 1 |
| tests/BlackFlag.Engine.Security.Tests/Formats/Ooxml/OoxmlWordSecurityAnalyzerTests.cs | 1 |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfEditingSaveConformanceTests.cs | 0 |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalFontEmbeddingRightsTests.Projectors.cs | 1 |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextDecorationGeometryTests.cs | 1 |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExternalExistingTextFontServiceTests.cs | 11 |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExternalFontCollisionIntegrationTests.cs | 1 |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFilteredTextMutationTests.cs | 1 |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFormPlacementCloneProjectorTests.Transactions.cs | 1 |
| tests/BlackFlag.Engine.Writing.Tests/PdfIncrementalWritePlannerTests.cs | 1 |
| tests/BlackFlag.Engine.Writing.Tests/PdfNewAnnotationWritePlannerTests.cs | 0 |
| tests/BlackFlag.Engine.Writing.Tests/PdfWritePlannerTests.cs | 1 |
| tools/PiratePDF.DocGen/Repair/Rules/IsoDateRepairRule.cs | 0 |

## Compatibility and final scope

| Contract or scope change | Yes / No |
|---|---|
| Public API removed | No |
| Protected API removed | No |
| Interface contract changed | No |
| Override contract changed | No |
| Plugin contract changed | No |
| Serialization contract changed | No |
| Native contract changed | No |
| Dependency changed | No |
| Project reference changed | No |
| Observable behavior intended to change | No |

Final scope is **90 files: 85 C# and five documentation files**. Documentation changes are this report, Post-BF-1054-CSharp-Repository-Audit.md, and the normal generated docs/Documentation-Index.md, docs/Documentation-Health.md and docs/ValidationReport.md. Every changed file was reviewed. Historical completion reports remain unchanged.

No new correctness/security finding requiring separate implementation approval was established. Existing fuzzing, conformance, native resolution, SBOM, signing, AOT/trimming, CI and font-catalog cancellation findings remain deferred. No existing finding was created or closed by this cleanup. All fifteen P0 closures remain intact. No canonical-font feature, dependency change, API deprecation or subsequent batch was started. Work stops after validation, with changes uncommitted for review.

## Project scan inventory

| Evaluated project | Hand-authored C# files |
|---|---:|
| BlackFlag.Engine.ReferencePlugin | 1 |
| BlackFlag.Engine.Abstractions | 1 |
| BlackFlag.Engine.Core | 10 |
| BlackFlag.Engine.Document | 57 |
| BlackFlag.Engine.Editing | 70 |
| BlackFlag.Engine.Filters | 25 |
| BlackFlag.Engine.Fonts.Platform | 4 |
| BlackFlag.Engine.Fonts | 31 |
| BlackFlag.Engine.IO | 6 |
| BlackFlag.Engine.Objects | 16 |
| BlackFlag.Engine.Parsing | 86 |
| BlackFlag.Engine.Plugins | 27 |
| BlackFlag.Engine.Rendering.Skia | 9 |
| BlackFlag.Engine.Rendering | 116 |
| BlackFlag.Engine.SDK.Adapters | 3 |
| BlackFlag.Engine.SDK | 45 |
| BlackFlag.Engine.Security | 63 |
| BlackFlag.Engine.Services | 80 |
| BlackFlag.Engine.Syntax | 22 |
| BlackFlag.Engine.Writing | 48 |
| BlackFlag.Engine.Abstractions.Tests | 1 |
| BlackFlag.Engine.Core.Tests | 2 |
| BlackFlag.Engine.Document.Tests | 12 |
| BlackFlag.Engine.Editing.Tests | 24 |
| BlackFlag.Engine.Filters.Tests | 20 |
| BlackFlag.Engine.Fonts.Platform.Tests | 4 |
| BlackFlag.Engine.Fonts.Tests | 10 |
| BlackFlag.Engine.IO.Tests | 3 |
| BlackFlag.Engine.Objects.Tests | 15 |
| BlackFlag.Engine.Parsing.Tests | 20 |
| BlackFlag.Engine.Plugins.Tests | 7 |
| BlackFlag.Engine.Rendering.Skia.Tests | 6 |
| BlackFlag.Engine.Rendering.Tests | 20 |
| BlackFlag.Engine.SDK.Adapters.Tests | 5 |
| BlackFlag.Engine.SDK.Tests | 9 |
| BlackFlag.Engine.Security.Tests | 19 |
| BlackFlag.Engine.Services.Tests | 58 |
| BlackFlag.Engine.Syntax.Tests | 16 |
| BlackFlag.Engine.Writing.Tests | 27 |
| PiratePDF.DocGen.Tests | 16 |
| BlackFlag.Engine.Plugins.TestPlugin | 1 |
| PiratePDF.DocGen | 60 |

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-22 | Record conservative post-B7 source hygiene, removal evidence, retained contracts and acceptance validation. |
