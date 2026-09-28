# Post-BF-1054 Batch B6 - External Font Embedding Rights

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-025 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-16 |
| **Next Review** | 2026-12-16 |

## Scope, baseline and disposition

B6 implements POST-P0-008 technical external-font permission interpretation and operation enforcement. It does not decide licensing validity, ownership or other legal questions. No permission override or bypass was added.

Starting branch: `maintenance/post-BF-1054-repository-review`. Starting commit: **62ab3907e2422052929bb5af2e7908d7cef6205e**, Correct existing text decoration geometry. B5 was committed; initial git status/diff were clean. POST-P0-005/006/007/015 remain closed. Root AGENTS.md, charter, README, current audit/B5 report, BF-1054H/I/J/K/L/M/Q/R contracts, engineering standards, BF-1049A, bounded-materialization guidance and the consolidated release gate were reviewed. Historical reports remain unchanged.

**Disposition: POST-P0-008 is implemented and closed. The full unmodified release gate passed 2,308 tests across 20 projects, with clean Release build, integrated source-size validation and clean DocGen.**

The consolidated Invoke-PiratePDFReleaseGate.ps1 remains authoritative and unmodified. Test-PiratePDFSourceFileSizes.ps1 remains intentionally absent.

## Specification and product policy

Technical authority: [Microsoft OpenType 1.9.1 OS/2, fsType and version differences](https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fstype), checked 2026-09-16.

The declaration distinguishes installable (0), restricted (2), preview/print (4) and editable (8) embedding. Preview/print requires a read-only document; editable permits document edits. Independent bits 0x0100 and 0x0200 prohibit subsetting and restrict embedding to bitmap data, respectively. Versions 0/1 ignore bits 4-15; versions 2-5 recognize these independent restrictions. Versions 0-2 permit least-restrictive precedence among combined base bits; versions 3-5 require exclusive base levels. Reserved bits have no assigned permission meaning. Embedding must retain the declaration rather than clear restrictions.

**Repository decision:** existing J/K/M/Q operations produce editable PDFs containing TrueType outlines, with no read-only preview/print workflow or bitmap-only embedder. They accept installable or editable base permission, subject to independent program/provider restrictions. Preview/print is recognized but rejected for these operations. This resolves the previously ambiguous coarse mapping conservatively without adding another product workflow. Restricted, unknown and invalid/ambiguous v3+ base combinations reject. No user override is available.

## Baseline reproduction before production changes

Two permanent regressions first failed against unchanged production:

| Fixture | Sanitizer and inspection | Actual baseline outcome |
|---|---|---|
| Valid OS/2 v3, fsType=0x0100, provider Allowed, public Type0 projector, required text A | Sanitization succeeds; SHA-256 B30932FD30D789FD29D340FCCB9627ACFB09A17F9C4CC45861EEE606D163F8AA; writer reports Allowed | Type0 subset projection succeeds instead of throwing. The projector invokes Unicode subset generation before its program-rights check and returns the projection. No document publication is needed to reproduce the forbidden subset work. |
| Valid OS/2 v3, fsType=0x0008 | Metadata inspector returns SubsetOnly; writer inspector returns Allowed | Assert.NotEqual(SubsetOnly) fails. Metadata ReadStyle maps bit 3 to a provider-style subset requirement, losing editable meaning. |

Fixtures use correct v3 table length (96 bytes), deterministic SFNT directory/checksums, valid name/cmap/glyph tables and actual sanitization. Existing unrelated v0 fixtures retain their defaults. Full permitted embedding of the same 0x0100 fixture later reopens with all 1,308 bytes unchanged. The original Type0 failure was authorization, not malformed-font handling or a sanitizer bypass.

Root causes were duplicated incompatible bit reduction, absence of OS/2 version interpretation, loss of independent restrictions, and subset generation preceding program authorization.

## Trusted interpretation and authorization

PdfSanitizedExternalFontProgram now records one internal immutable PdfExternalFontProgramRights value after owning and hashing its canonical validated bytes. It retains OS/2 version, raw ushort flags, base level and independent restrictions. No caller-supplied permission is trusted as a replacement for those bytes. Both inspectors consume that value; their unrelated metric/name/cmap parsing remains separate.

The same operation decision is applied to provider and program evidence. Provider Allowed/Editable permits either full or subset embedding in principle; provider SubsetOnly still requires a subset. Provider NoSubsetting rejects subset operations; BitmapEmbeddingOnly rejects all current outline operations. No provider setting can clear a program restriction, and no program setting can clear a provider restriction. Conflicting provider SubsetOnly plus NoSubsetting permits neither operation.

All three projectors call the central policy before metrics/subset work, object allocator creation or resource-name selection. Both public low-level subsetters also check trusted program restrictions before their source-byte copy, glyph mapping/closure, table generation and re-sanitization. These checks prevent direct subsetter calls from bypassing the same program boundary. The existing-text external-font service always selects the Type0 subset projector and has no fallback to full embedding when subsetting is prohibited.

The internal interpretation preserves reserved raw bits without assigning them new semantics. Tests cover bit 0 and bit 15 as ignored reserved bits. Supported versions 0, 1, 2, 3, 4 and 5 are individually exercised; version 6 is an unknown-version control and cannot authorize embedding. Multiple base bits in v0-v2 use the documented historical precedence; v3+ combinations map to Unknown and fail closed. This is explicit version handling, not a generic most-permissive rule.

## Deterministic program decision matrix

The permanent Fonts matrix executes each row for each OS/2 version 0-5: **108 cases**, followed by six ownership/missing/version/malformed/API controls. Provider in this table is permissive. D means NotSupportedException; Y means authorization succeeds. P means PreviewAndPrint, E means Editable, A means Allowed/installable and U means Unknown. B denotes BitmapEmbeddingOnly. Values are raw fsType; all legal-version fixtures have correct lengths/checksums.

| fsType | Base v0-2 / v3-5 | Independent restrictions v0-1 / v2-5 | Full v0-1 / v2 / v3-5 | Subset v0-1 / v2 / v3-5 |
|---|---|---|---|---|
| 0x0000 | A / A | none / none | Y / Y / Y | Y / Y / Y |
| 0x0002 | Restricted / Restricted | none / none | D / D / D | D / D / D |
| 0x0004 | P / P | none / none | D / D / D | D / D / D |
| 0x0008 | E / E | none / none | Y / Y / Y | Y / Y / Y |
| 0x0100 | A / A | none / NoSubsetting | Y / Y / Y | Y / D / D |
| 0x0200 | A / A | none / B | Y / D / D | Y / D / D |
| 0x0102 | Restricted / Restricted | none / NoSubsetting | D / D / D | D / D / D |
| 0x0104 | P / P | none / NoSubsetting | D / D / D | D / D / D |
| 0x0108 | E / E | none / NoSubsetting | Y / Y / Y | Y / D / D |
| 0x0204 | P / P | none / B | D / D / D | D / D / D |
| 0x0208 | E / E | none / B | Y / D / D | Y / D / D |
| 0x0308 | E / E | none / NoSubsetting+B | Y / D / D | Y / D / D |
| 0x8000 | A / A | none / none | Y / Y / Y | Y / Y / Y |
| 0x0001 | A / A | none / none | Y / Y / Y | Y / Y / Y |
| 0x0006 | P / U | none / none | D / D / D | D / D / D |
| 0x000A | E / U | none / none | Y / Y / D | Y / Y / D |
| 0x000C | E / U | none / none | Y / Y / D | Y / Y / D |
| 0x000E | E / U | none / none | Y / Y / D | Y / Y / D |

Missing OS/2 remains structurally acceptable to the sanitizer and supplies Unknown permission, not fabricated permission or a fabricated restriction. Previously metadata already used Unknown while the writer inspector required the table and threw InvalidDataException. The writer now consistently exposes Unknown metrics; requested embedding/subsetting fails with NotSupportedException. This intentionally replaces a missing-metadata structural-looking error with the conservative operation boundary. Truncated OS/2 lengths 0, 9 and 77 continue to produce PdfExternalFontSanitizationException/MalformedRequiredTable. Sanitizer structural validation itself was not broadened or weakened; B6 does not claim complete validation of every optional OS/2 field.

## Provider intersection, editing and failure ordering

The Services matrix has 19 provider/program combinations across all three projectors (57 cases): both permitted, provider-only restriction, program-only restriction, both restricted, preview/print, editable, no-subset disagreement in both directions, bitmap disagreement, provider SubsetOnly, contradictory provider flags, and v3 conflicting base bits. Exact serialized object graphs remain unchanged by projection or rejection; the available allocation sequence and undo history are unchanged.

Two ordering cases use unmapped text: prohibited fonts report subsetting prohibition before text/glyph selection; permitted controls reach their existing unsupported-text failure. Direct subsetter tests use an insufficient retained-glyph budget and prove permission rejection precedes glyph admission. Pre-cancelled calls retain OperationCanceledException and the original token. Permission checks are before resource allocation by control-flow inspection as well as deterministic document-state checks; no instrumentation hook was added.

Eight actual PdfExternalExistingTextFontService cases reject without changing content, object count, resource dictionaries or history. An earlier color edit inside the caller's transaction remains intact after denial and rolls back normally when that transaction is disposed. The original target then accepts a permitted external font, returns FExt2 after an FExt1 collision, and supports exact graph/content undo and redo. No Form clone is created; the existing unshared page-only external-font boundary remains unchanged.

**SDK boundary:** IPdfExistingTextSdkSession.ChangeExistingTextFont takes an existing resource name, not a provider/match/program. It invokes PdfExistingTextFontService, not PdfExternalExistingTextFontService. No external embedding SDK route currently exists. The new adapter regression proves an unknown external resource name fails with KeyNotFoundException without mutation or target invalidation, after which registered F2 succeeds and undo restores bytes. The full SDK suite passes. B6 does not invent an external-font SDK API or claim a nonexistent route was tested.

**Save orchestration boundary:** PdfEditingSaveService currently rejects PdfExistingTextResourceChangeOperation as an unsupported save domain. This surfaced during acceptance and remains untouched because it is unrelated to rights interpretation/enforcement. Committed external-font edits already publish a coherent object graph; the acceptance test serializes that graph with the established PdfWritePlanner/PdfFullRewriteWriter. No generic EditingSaveService support is claimed. This remains a separate conformance concern, not a B6 permission bypass.

## Exception and public API compatibility

Well-formed prohibited operations retain the projectors' NotSupportedException family. Diagnostics identify program subsetting prohibition, bitmap-only outline incompatibility, preview/print workflow mismatch or provider prohibition, without legal conclusions. Unknown/conflicting declarations fail at the same operation boundary. Malformed structures remain sanitizer-domain failures; no broad catch was introduced. The legacy provider-only EnsureCanEmbed(face) retains InvalidOperationException on refusal. Catalog matching remains a provider eligibility filter, not final authorization from font bytes.

Public changes are additive and necessary to carry evidence across Fonts, Platform and Services without friend assemblies or duplicate semantics:

- Enum numbers remain Unknown=0, Allowed=1, SubsetOnly=2, Restricted=3; add PreviewAndPrint=4 and Editable=5. The enum is not Flags. SubsetOnly retains its provider-required-subset meaning; it is never produced from editable program bit 0x0008.
- PdfExternalFontFace gains init-only NoSubsetting and BitmapEmbeddingOnly, both default false. Its existing constructor signature is unchanged. Providers can express independent restrictions, including combinations, without overloading a base level.
- PdfSanitizedExternalFontMetadata and PdfExternalTrueTypeMetrics gain read-only properties for the same restrictions. Their constructors are internal. Raw fsType/version stay internal.
- PdfExternalFontEmbeddingPolicy gains EnsureCanEmbed(face, program, bool subset), needed by Services to intersect immutable program evidence with provider policy before expensive work. Existing signatures remain. The face-aware catalog predicate is internal, avoiding an unnecessary public overload.

Existing binaries retain enum numbers, constructor and method bindings. Existing source call forms remain valid. Consumers enumerating every enum member or matching every output value must accommodate the two new values. Consumers authorizing solely from EmbeddingRights must instead include independent restrictions and trusted program policy: that property now explicitly describes the base level, not every restriction. Metadata now distinguishes editable and preview/print; bitmap-only can have an installable/editable base plus a separate prohibition. All repository consumers were inventoried and updated where they make embedding decisions. Rendering's separate resolved-PDF-font enum is not an external-program authorization path and is unchanged. No breaking API redesign or SDK signature change was required.

## Preservation and save/reopen evidence

Existing B4A byte ownership tests and a new cached-permission mutation test pass. Public byte snapshots and read-only streams cannot upgrade the immutable program restrictions or hash. Existing B4B glyph zero/root/composite closure, retained-glyph ceilings and generated-byte preflight tests pass. Existing B4C unsigned cmap lengths/counts, extreme input budget and mixed-directory malformed-candidate isolation pass. No subset builder or sanitizer validation policy was changed.

Twelve permanent writing cases cover full v3/0x0100, full v3/0x0108, full editable, simple editable subset, Type0 editable subset and actual external-text editing, each with traditional xref and xref streams. Original resource FExt1 object 9 generation 7 remains; new resource FExt2 resolves to the projection's actual font. Retained original glyph IDs, CID ordering, widths, ToUnicode including supplementary Unicode, FontFile2/Length1 and exact re-sanitization are asserted. OS/2 version and fsType are preserved byte-for-byte in permitted subsets; permission flags are never cleared.

The supplemental real-parser harness passed **12 combinations**, separate from permanent/release-gate totals. It reopens through PdfCrossReferenceParser or PdfCrossReferenceStreamParser and PdfIndirectObjectResolver, resolves page resources and font/descriptor/program objects, checks subtype, Length1, exact font bytes/hash, re-sanitization, WinAnsi A width 550, CID/GID and ToUnicode mappings, and preserved rights metadata. The editing cases parse decoded content through PdfContentStreamParser and verify FExt2 Tf plus restored F1, preserving the /Fl representation. Pure denied projections publish no document to reopen.

Harness: `C:\Users\Sufan\AppData\Local\Temp\PiratePDF-B6-Validation\B6Reopen.cs`; references already built repository assemblies only. Command:

```powershell
dotnet run --project C:\Users\Sufan\AppData\Local\Temp\PiratePDF-B6-Validation\B6Reopen.csproj --configuration Release --no-restore
```

The temporary project adds no repository dependency or project reference. Its first build needed access to user NuGet configuration; the approved retry succeeded. A harness width oracle initially used 600; inspection of the fixture established A/GID1 is 550, and the corrected exact assertion passes. Production widths and tests were not loosened. An initial concurrent project build also encountered a shared compiler-output lock; subsequent builds were serialized, with clean final results.

## Work and allocation implications

Program construction performs one bounded directory scan using existing SFNT table-reading infrastructure and retains a small rights value. Authorization after sanitization is O(1), without public byte snapshots or repeated font parsing. Both inspectors stop interpreting fsType independently. Forbidden subsets avoid the existing whole-font working copy and all glyph/table/resource work. Permitted subset processing and all resource limits remain unchanged. No caching subsystem, pooling, unsafe/native code, new dependency, project reference or platform font API was introduced. Throughput/GC/latency claims are **measurement required**; no speedup is claimed.

The direct-subsetter guard and catalog use of independent provider restrictions are necessary expanded corrections within POST-P0-008: otherwise bypassing projectors or retaining coarse provider eligibility would preserve the same defect. They have the same P0 engineering-policy classification, no separate roadmap scope, no document mutation on denial, and deterministic regression coverage. No unrelated finding was fixed.

## Validation

| Check | Result |
|---|---|
| Baseline regressions before production edits | 2 failed as expected, exact outcomes recorded above |
| Focused Fonts program-rights suite | 114 passed |
| Focused Fonts.Platform rights suite | 12 passed |
| Focused Services rights/projector/editing/writing suite | 83 passed |
| Focused SDK resource boundary | 1 passed |
| Complete BlackFlag.Engine.Fonts.Tests | 228 passed |
| Complete BlackFlag.Engine.Fonts.Platform.Tests | 30 passed |
| Complete BlackFlag.Engine.Services.Tests | 454 passed |
| Complete BlackFlag.Engine.SDK.Adapters.Tests | 35 passed |
| Complete BlackFlag.Engine.Editing.Tests | 133 passed |
| Complete BlackFlag.Engine.Writing.Tests | 153 passed |
| Complete BlackFlag.Engine.Parsing.Tests | 219 passed |
| Release solution build | Passed, 0 warnings/errors |
| Supplemental actual parser reopen | 12 passed |
| git diff --check before gate | Passed |
| Full unmodified release gate | Passed: 2,308 tests across 20 projects, 0 failures/skips; clean Release build |
| Integrated source-size check | Passed: 1,059 files; no hard-ceiling violation; existing 943/844-line review files unchanged |
| DocGen all/health/repair | Passed: 192 documents; 0 errors/warnings/information; health 91.6%, A-; repair 0 changes/files/diagnostics |

Focused runs used the new class/method FullyQualifiedName filters; full suites used `dotnet test tests/<project>/<project>.csproj --configuration Release --no-build`. There are **210 new permanent cases**. No existing tests were removed or weakened. Required acceptance command: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1`, with no skip flags.

Decomposition review: the existing Type0 projector test fixture is 687 lines and provider test fixture 621 lines. Only parameterized OS/2 construction was added to these cohesive fixture builders; new rights tests are in focused files/partials. Splitting unrelated fixture infrastructure would add churn. No production file in this batch reaches the 600-line review range; existing gate-threshold files are untouched.

## Exact change inventory

Sixteen production files:

- `src/BlackFlag.Engine.Fonts/PdfExternalFontProgramRights.cs` (new)
- `src/BlackFlag.Engine.Fonts/Sanitization/PdfSanitizedExternalFontProgram.cs`
- `src/BlackFlag.Engine.Fonts/PdfExternalFontEmbeddingRights.cs`
- `src/BlackFlag.Engine.Fonts/PdfExternalFontEmbeddingPolicy.cs`
- `src/BlackFlag.Engine.Fonts/PdfExternalFontFace.cs`
- `src/BlackFlag.Engine.Fonts/PdfExternalFontCatalog.cs`
- `src/BlackFlag.Engine.Fonts/PdfSanitizedExternalFontMetadata.cs`
- `src/BlackFlag.Engine.Fonts/PdfSanitizedExternalFontMetadataInspector.cs`
- `src/BlackFlag.Engine.Fonts/Embedding/PdfExternalTrueTypeInspector.cs`
- `src/BlackFlag.Engine.Fonts/Embedding/PdfExternalTrueTypeMetrics.cs`
- `src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeGlyphSubsetter.cs`
- `src/BlackFlag.Engine.Fonts/Embedding/PdfUnicodeTrueTypeGlyphSubsetter.cs`
- `src/BlackFlag.Engine.Fonts.Platform/PdfFileSystemExternalFontProvider.cs`
- `src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeResourceProjector.cs`
- `src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeSubsetResourceProjector.cs`
- `src/BlackFlag.Engine.Services/Fonts/PdfExternalType0TrueTypeResourceProjector.cs`

Ten test files:

- `tests/BlackFlag.Engine.Fonts.Tests/PdfExternalFontProgramRightsTests.cs` (new)
- `tests/BlackFlag.Engine.Fonts.Tests/TestFontFactory.cs`
- `tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFileSystemExternalFontProviderTests.EmbeddingRights.cs` (new)
- `tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFileSystemExternalFontProviderTests.cs`
- `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalFontEmbeddingRightsTests.cs` (new)
- `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalFontEmbeddingRightsTests.Projectors.cs` (new)
- `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalFontEmbeddingRightsTests.Editing.cs` (new)
- `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalFontEmbeddingRightsTests.Writing.cs` (new)
- `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalType0TrueTypeResourceProjectorTests.cs`
- `tests/BlackFlag.Engine.SDK.Adapters.Tests/Editing/PdfExistingTextSdkSessionTests.cs`

Five documentation files:

- `docs/reviews/Post-BF-1054-Batch-B6-External-Font-Embedding-Rights-Completion-Report.md` (new)
- `docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md`
- `docs/Documentation-Index.md` (generated)
- `docs/Documentation-Health.md` (generated)
- `docs/ValidationReport.md` (generated)

**Final changed-file count: 31** (16 production, 10 test, five documentation). The generated repair report has no content diff. Every changed file was reviewed; final git diff --check passes. Acceptance-only documentation updates after the passing gate were followed by clean DocGen all/health/repair checks. No production or test changes followed the gate.

## Remaining scope and stop boundary

POST-P0-009 and POST-P2-001 were not started. Fuzz infrastructure, broad conformance closure, native resolution, SBOM, signing, AOT/trimming, CI parity, canonical font editing, licensing UI and external provider integrations remain untouched. POST-P0-005/006/007/015 remain closed; earlier ownership and resource-name fixes remain intact. No commit, push, merge, tag or branch switch occurred. B6 stops after acceptance; another batch needs explicit approval.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-16 | Record B6 version-aware rights, provider/program intersection, early enforcement and preservation validation. |
