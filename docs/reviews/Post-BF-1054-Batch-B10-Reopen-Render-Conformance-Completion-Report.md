# Post-BF-1054 Batch B10 - Reopen/Render Conformance

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-040 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1049, BF-1054 |
| **Last Updated** | 2026-10-27 |
| **Next Review** | 2027-01-27 |

## Disposition

**B10 COMPLETE — CONFORMANCE VERIFIED. POST-P2-003 is closed.**
The batch adds **29 deterministic integration cases** and corrects four
demonstrated production defects. New cases parse actual original/saved bytes,
edit through supported public services or the SDK interface, validate saved
object graphs and render supported reopened content using owned embedded
outlines. No machine-installed font is required for the asserted appearance.

The agent-observed unchanged release gate built cleanly and ran **3,411
tests: 3,406 passed, five failed, zero skipped**. The five failures were
existing Windows symbolic-link fixture-creation privilege failures before
validator invocation. On the requested permitted-context rerun, the user
reported **“All tests pass”**, then explicitly confirmed **“Yes, the entire
release gate passed”**, including documentation, repair preview and whitespace
validation. That final success is **user-reported**, not an agent-observed
privileged run; no privileged TRX or console log was supplied. The earlier
denied attempts remain evidence, not passing assertions.

This closes only POST-P2-003. Native H2b remains deferred, disabled and
unqualified; B9 / POST-P2-002 remains open and managed fuzzing remains
partially verified. POST-P2-004 through 008 and BF-1055 were not started.

## Baseline and preservation

- Branch: `maintenance/post-BF-1054-repository-review`.
- HEAD: `f51824dbc031b58497a38a6292ee11568b9ffeda`.
- Entry tree: **135 modified/untracked file paths**, intentionally dirty,
  including the prior B9 implementation and governance assignment. These
  changes were preserved; B10 did not reset, clean, stash, stage or replace them.
- Fresh ignored evidence: `artifacts/fuzz/b10-conformance-20261026-01/`.
  Work began on October 26 and validation/reporting continued October 27.
  `entry-status.txt` and `entry-hashes.json` retain the starting inventory.
- Authority: the owner's B10 assignment, root AGENTS.md, charter, accepted
  maintenance handoff, PP-GOV-004, current audit, engineering standards and
  unchanged authoritative release gate. Historical milestone reports remain
  implementation history.

## Fixture and execution boundary

The new suite lives in the existing Services test project. It reuses
`PdfFilteredTextTestDocument`, the existing test SFNT writer and random-access
source, rather than creating a new test project or copying engine parsers.
The test project references the existing Parsing, Skia and SDK Adapters
projects to exercise their real integration boundaries. No NuGet/package,
native/runtime version, production project reference or friend declaration
was added or changed.

Fixtures contain two 200-by-200-point pages, a Flate content stream at
`4 2`, a sibling at `6 3`, its content at `7 1`, an embedded font program at
`9 4`, and an unrelated collision font at `10 7`. The Form variant adds a
filtered Form at `12 5` reused twice. Catalog, page, resource, font, stream
and predictor dictionaries retain private entries. `/Filter` and
`/DecodeParms` use single-element arrays with predictor 1. Ordinary stream
filters use the canonical `/FlateDecode` name; the older mutation-unit
helper's `/Fl` shorthand is not assumed supported by the general pipeline.

`ReopenedPdf` uses `PdfCrossReferenceParser` for traditional revision chains,
`PdfCrossReferenceStreamParser` for stream sections, `PdfObjectLocationTable`,
`PdfIndirectObjectResolver`, `PdfEffectivePageTreeLoader`, resource resolution,
content parsing, graphics interpretation and `PdfFontDecoder`. The font decoder
supplies authoritative Unicode for host-created discovery sources; the
interpreter intentionally retains encoded bytes. The Skia renderer consumes
the reopened/interpreted page, not an intermediate authoring display list.

There is **no general public parser-to-editing-model importer** in the current
repository. A fixture-only adapter maps the known small parsed graph into the
editable object model. It fails on unexpected object types, admits at most
32 location entries, limits fixture files to 128 KiB and xref-stream chains
to four revisions, and verifies the two pages' parsed geometry. It preserves
encoded stream bytes and generations. This is not a new production loader,
general round-trip guarantee or claim about arbitrary imported documents.

## Scenario matrix

| New scenario | Cases | Actual reopened checks |
|---|---:|---|
| Same-font A-to-B replacement | 4 | Traditional/stream xref crossed with full/incremental save; decoded text, affine geometry, visible changed glyph, untouched sibling/resources, undo/redo reopen/render |
| Rotated replacement, painted or text-clipped | 4 | Both xref modes; 90-degree text matrix, upright glyph bounds, changed area, preserved baseline and graph |
| Transformed red text with automatic underline | 4 | Both xref/save modes; decoded text, continuation run, 12-point semantic path width, transform, line thickness, red raster and untouched graph |
| External ChangeFont and resource collision | 4 | Both xref/save modes; Type0/CIDFontType2 graph, FontFile2, ToUnicode, resolved CIDToGIDMap, 12-point decoded advance, collision-safe name, preserved aliases/unrelated font, embedded-font byte ceiling, appearance, undo/redo |
| Placement-only Form clone | 2 | Both full-rewrite xref modes; original Form/filter bytes retained, one clone redirect, two distinct resolved placements/text/transforms, original second placement and visible raster |
| Painted versus text-clipped outline | 2 | Both xref modes; matching upright positions/areas after actual parse and render |
| Unsupported shaping | 2 | Both xref modes; authoritative multi-scalar ToUnicode mapping, rejection before resource installation, zero transaction changes, exact graph rollback, saved/reopened text/content/CMap preservation |
| Failed edit and destination I/O failure | 4 | Both xref/save modes; unencodable same-font rejection before mutation; injected 64-byte partial output, caller ownership, unchanged committed graph, successful retry/reopen/render and undo |
| SDK invalidation and stale targets | 2 | Both xref modes; actual parsed discovery source, calls through IPdfExistingTextSdkSession, failed resource lookup, rollback, rediscovery, stale-ID refusal, full-save reopen/render, undo/redo |
| Descendant admission ceiling | 1 | A two-entry Type0 descendant array is rejected at configured limit 1 before either nonexistent descendant can resolve |
| **Total** | **29** | **No skipped cases** |

Incremental cases start with a complete parseable original PDF, verify its
**entire original prefix byte-for-byte**, follow `/Prev` to two real revisions,
retain object generations and verify untouched objects still resolve at their
original physical offsets. Changed content resolves in the appended revision.
Full rewrites independently parse a single valid revision. Assertions inspect
resolved dictionaries/references and semantic text rather than relying on PDF
substrings. Unknown keys and filter/predictor representation survive; only
the stream `/Length` is allowed to change with replacement bytes.

## Demonstrated defects and corrections

| Observation | Classification / pre-fix evidence | Smallest correction and regression |
|---|---|---|
| B10-OBS-01: inverted glyph paths | P0 correctness. The owned 700-unit-high glyph rendered below the PDF baseline: raster bottom 53 instead of approximately 67.5 in the sheared fixture. | Convert Skia's downward-Y outline to upward-Y PDF text space when painting and clipping. Independent baseline, area, shear and rotation assertions pass. |
| B10-OBS-02: external-font save refused | P0 correctness. `PdfEditingSaveService` threw NotSupportedException for the resource-change operation returned by the supported ChangeFont service. | Route that operation to existing-text Writing. Retain an owned, internal read-only identifier snapshot on the operation and include exactly the changed resource objects plus content in incremental planning. Full/incremental reopen, collision ownership and history pass. |
| B10-OBS-03: Form placement applied twice | P0 correctness. Two interpreted placements at (20,30) and (80,100) rendered with a second canvas transform; the clone comparison had area ratio 0.5 instead of 0.75. | Render already-interpreted Form children in their existing effective page coordinates without another placement matrix. Both visible placements, semantic transforms and unchanged second Form pass. |
| B10-OBS-04: Type0 reopen lost embedded program/mapping | P0 correctness. Reopened generated Type0 fonts emitted FONT_SUBSTITUTED despite a valid embedded descendant program. | Resolve bounded Type0 descendant dictionaries and CIDToGIDMap for the canonical consumer view and use the descendant descriptor/program. Keep the original font dictionary untouched. Count admission precedes descendant resolution; the existing encoded-font ceiling still applies. |

The new internal identifier snapshot does not change public SDK/API signatures,
constructor signatures, serialization or persisted contracts, and exposes no
trusted mutable buffer. The transaction continues to own graph installation,
rollback and undo/redo. No unrelated object is added to the incremental changed
set. Unknown operation domains remain unsupported.

Valid Type0 resource resolution now exposes usable canonical descendants and
their embedded program instead of falling back. Oversized descendants/programs
still fail with `PdfResourceResolutionException` / `LimitExceeded`; malformed
descendant references/types now fail during resolution rather than silently
yielding missing glyph data. The original dictionary remains the preservation
source. No filter/security ceiling, accepted fuzz exception oracle or resource
policy was relaxed. Destination IOException still propagates; a caller stream
may contain partial bytes, while the document and transaction history remain
coherent. Atomic filesystem replacement is not claimed.

## Exact source and test files changed

Modified production files:

1. `src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Text.cs`
2. `src/BlackFlag.Engine.Editing/Operations/PdfExistingTextResourceChangeOperation.cs`
3. `src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.Fonts.cs`
4. `src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Text.cs`
5. `src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.cs`
6. `src/BlackFlag.Engine.Services/Editing/PdfEditingSaveService.cs`
7. `src/BlackFlag.Engine.Services/Text/PdfExistingTextTransactionWritingService.cs`

Modified test project: `tests/BlackFlag.Engine.Services.Tests/BlackFlag.Engine.Services.Tests.csproj`.
Added test files under its `Conformance/` directory:

- `ConformanceFont.cs`
- `ConformanceFixture.cs`
- `ReopenedPdf.cs`
- `PdfTextReopenConformanceTests.cs`
- `PdfFontAndFormReopenConformanceTests.cs`
- `PdfTransactionalReopenConformanceTests.cs`

Documentation changes: this report, the current repository audit, the current
maintenance handoff and root AGENTS.md continuation pointer. Generated
`docs/Documentation-Index.md`, `docs/Documentation-Health.md` and
`docs/ValidationReport.md` are refreshed through DocGen. Existing B9 reports,
PP-GOV-004's dated decision, roadmap scope and historical qualification evidence
are preserved. The final evidence inventory records task-specific file hashes
and compares the pre-existing dirty files separately from the full Git diff.

## Validation and reproducibility

Evidence root below means `artifacts/fuzz/b10-conformance-20261026-01/`.

| Check | Actual result / evidence |
|---|---|
| Pre-change focused Services baseline | 103 passed, zero failed/skipped; `baseline/baseline.trx` |
| New conformance focus | 29 passed, zero failed/skipped; `focused-29/focused-29.trx` |
| Final complete Services rerun after the last preservation assertions | 553 passed, zero failed/skipped; `affected-services-final/services-final.trx` |
| Complete affected suites in the unchanged gate | Services 553, Parsing 219, Skia 25, SDK Adapters 35, Editing 133, Rendering 97, Writing 153, Fonts 228, SDK 26: all passed, zero failures/skips |
| Separate affected runs | Services 549 before four rotation cases were added; Parsing 219, Skia 25, SDK Adapters 35, Editing 133; corresponding `affected-*/` TRX files |
| Unchanged release gate, file sandbox | Exit 1 at restore because the existing user NuGet.Config could not be read; `release-gate-sandbox.log` and `.exit` |
| Same unchanged gate, ordinary user outside file sandbox | Source-size check passed (1,266 files); restore passed; build zero warnings/errors; 21 test assemblies, 3,406 passed/five fixture failures/zero skipped of 3,411; stopped before DocGen; `release-gate-user-context.log`, `.exit`, `gate-summary.json` |
| User's permitted-context rerun | User reports all tests passed and explicitly confirms the **entire unchanged release gate passed**. No privileged TRX/log supplied; exact post-rerun test totals are not independently observed. |
| Final DocGen all | 216 documents, zero errors/warnings/information; `docgen-all.log` |
| Final DocGen health | 216 documents, zero errors/warnings, 91.4%, A-; `docgen-health.log` |
| Final DocGen repair preview | Zero changes/files/diagnostics/warnings/errors; no repairs applied; `docgen-repair.log` |
| Roslyn unused-import analysis | `dotnet format style PiratePDF.slnx --no-restore --diagnostics IDE0005 --verify-no-changes --include <the changed production files and new Conformance directory> --verbosity quiet`: exit 0; `unused-import-analysis.log` |
| Final whitespace validation | `git diff --check`: exit 0; pre-existing line-ending advisories are not whitespace errors |
| Preservation inventory | All 135 entry paths retained; only the six intended existing documentation files changed. All 16 previously inventoried corpus/manifest files and 200 historical evidence files match their SHA-256 values. Branch/HEAD unchanged; `preservation-final.json`. |

The solution test count reconciles the earlier R1 3,382 with **29 additions =
3,411**; Services reconciles **524 + 29 = 553**. No prior test was removed or
silently excluded. The independent final documentation/whitespace and preservation
results are also retained in the evidence root's final validation summary. The
batch's reviewable delta is **21 files**: seven production files, one test
project, six new test/fixture files, this report and six existing documentation
files.

Representative exact ordinary commands, with the evidence root substituted
as shown above:

```powershell
dotnet restore tests/BlackFlag.Engine.Services.Tests/BlackFlag.Engine.Services.Tests.csproj --ignore-failed-sources --verbosity quiet
dotnet test tests/BlackFlag.Engine.Services.Tests/BlackFlag.Engine.Services.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~Services.Tests.Conformance --logger 'trx;LogFileName=focused-29.trx' --results-directory artifacts/fuzz/b10-conformance-20261026-01/focused-29 --verbosity quiet
dotnet test tests/BlackFlag.Engine.Services.Tests/BlackFlag.Engine.Services.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=services.trx' --results-directory artifacts/fuzz/b10-conformance-20261026-01/affected-services --verbosity quiet
powershell.exe -NoProfile -File .\Invoke-PiratePDFReleaseGate.ps1
dotnet run --project tools/PiratePDF.DocGen/PiratePDF.DocGen.csproj -c Release --no-build -- all
dotnet run --project tools/PiratePDF.DocGen/PiratePDF.DocGen.csproj -c Release --no-build -- health
dotnet run --project tools/PiratePDF.DocGen/PiratePDF.DocGen.csproj -c Release --no-build -- repair
git diff --check
```

The separate affected commands were:

```powershell
dotnet test tests/BlackFlag.Engine.Parsing.Tests/BlackFlag.Engine.Parsing.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=parsing.trx' --results-directory artifacts/fuzz/b10-conformance-20261026-01/affected-parsing --verbosity quiet
dotnet test tests/BlackFlag.Engine.Rendering.Skia.Tests/BlackFlag.Engine.Rendering.Skia.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=skia.trx' --results-directory artifacts/fuzz/b10-conformance-20261026-01/affected-skia --verbosity quiet
dotnet test tests/BlackFlag.Engine.SDK.Adapters.Tests/BlackFlag.Engine.SDK.Adapters.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=sdk-adapters.trx' --results-directory artifacts/fuzz/b10-conformance-20261026-01/affected-sdk-adapters --verbosity quiet
dotnet test tests/BlackFlag.Engine.Editing.Tests/BlackFlag.Engine.Editing.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=editing.trx' --results-directory artifacts/fuzz/b10-conformance-20261026-01/affected-editing --verbosity quiet
dotnet test tests/BlackFlag.Engine.Services.Tests/BlackFlag.Engine.Services.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=services-final.trx' --results-directory artifacts/fuzz/b10-conformance-20261026-01/affected-services-final --verbosity quiet
```

Their full discovery also ran in the gate. No fuzz campaign, instrumentation,
driver execution, dependency change,
H2b qualification or privileged provisioning was performed by this batch.

### Historical failures retained

All development TRX files remain under the evidence root. Initial fixture
corrections addressed canonical filter naming, decoding host discovery text,
the interpreter's already-composed text matrix and explicit continuation run.
The baseline geometry assertions were retained to expose B10-OBS-01, not widened
to accommodate inverted rendering. Subsequent saved-font/Form failures exposed
B10-OBS-02 through 04 before their corrections.

`focused-transactions/` records an **aborted test host** caused by recursion in
the newly authored failing MemoryStream fixture's span/array overrides. The
fixture now calls the byte-array base implementation directly. This was a test
fixture defect, not an engine crash. The unsupported shaping fixture deliberately
maps a code to multiple Unicode scalars; its assertions cover rejection and
preservation, not visual shaping that the owned font does not support. An initial
assumption that WinAnsi `Z` was unencodable was corrected to an actually
unencodable CJK scalar; no production encoding/oracle was weakened.

The five unchanged release-gate failures were:

1. `CorpusManifestTests.SeedLinkEscapeIsRejected`
2. `FindingStorageTests.ArtifactLinkEscapeIsRejected`
3. `CampaignTicketAdmissionTests.TicketDirectoryLinkIsRejectedAndForeignBytesPreserved`
4. `CampaignRunnerTests.CampaignWorkspaceLinkIsRejected`
5. `H2bStationAclApplicabilityTests.ReparseSubstitutionIsRejectedBeforeOwnerAdmission`

Each failed while creating its symbolic-link fixture because Windows reported
`A required privilege is not held by the client`, before the validator ran.
None was skipped, changed or declared passed based on the denied attempt.
The requested user rerun was the exact five-name OR filter on the Release fuzz
project followed, only on success, by `.\Invoke-PiratePDFReleaseGate.ps1` from
Administrator PowerShell in the repository. The user's later confirmations
apply to that rerun; they do not qualify native H2b or retroactively make the
agent's earlier gate successful.

## Limits and next boundary

- B10 proves the listed owned fixtures and supported public editing/write/render
  paths. It does not add a general PDF importer, canonical font editing, arbitrary
  shaping, arbitrary composite-font editing, encrypted save, a platform font
  guarantee or a full PDF conformance certification.
- The unhinted font has owned wide/narrow rectangle glyphs. Rendering uses a
  200-by-200 CPU RGBA surface at one pixel per point and an ink threshold of 240.
  Expected geometric edges have one/two-pixel tolerance; area comparisons allow
  antialias fringes, including 2% for unchanged subset outlines. These are
  evidence-backed tolerances, not exact cross-platform pixel identity. Only the
  available Windows x64 environment was executed; other platforms remain CI work.
- Multi-scalar shaping is explicitly rejected before resources are installed;
  that rejection fixture makes no visual shaping claim.
- Form clone coverage exercises the existing placement projector plus full
  rewrite, not a newly invented transactional Form/font-edit API.
- Source prefix preservation applies to supported incremental writes. Caller
  streams are not atomic destinations; the failure test verifies document/history
  integrity and retry rather than claiming partial output is a valid PDF.
- User-reported full-gate success is separately attributed. This is not native
  H2b qualification, completion of B9 or a waiver of any security assertion.

The next sequenced maintenance item is POST-P2-004, under its existing scope;
this batch does not begin it. No Git history operation occurred.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-10-27 | Record 29 reopen/render integration cases, four demonstrated scoped corrections, affected-suite evidence, preserved denied attempts and user-confirmed permitted-context full-gate success; close POST-P2-003 only. |
