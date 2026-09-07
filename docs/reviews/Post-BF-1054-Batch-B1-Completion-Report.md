# Post-BF-1054 Batch B1 - Bounded Mutation Decode

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-010 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-06 |
| **Next Review** | 2026-12-06 |

## Scope and authorization

The user approved Phase A and **B1 only: POST-P0-001, bounded mutation decode** from the [repository audit](Post-BF-1054-CSharp-Repository-Audit.md). The working branch remains `maintenance/post-BF-1054-repository-review`, based on `92e26a416091e521bdc3fc8e6964ed0678e1f588`. The pre-existing untracked Phase A report was retained and updated to link this implementation record.

B1 is implemented and validated. The full unmodified release gate passed with 1,697 tests and no build warnings or errors. No other batch, roadmap feature, dependency or project-reference change is included. No commit, push, merge, tag or branch switch was performed.

## Exact implementation and test files

| File | Change |
|---|---|
| `src/BlackFlag.Engine.Services/Text/PdfContentStreamMutationCodec.cs` | Apply the established decode budget before output allocation/growth; reject incomplete Flate members; preserve valid empty Flate encoding |
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfContentStreamMutationCodecLimitTests.cs` | New bounded-input, exception, cancellation and representation regressions: 26 cases |
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfFilteredTextMutationTests.cs` | New caller, history, rollback and serialized-save regressions: 16 cases |
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfFilteredTextTestDocument.cs` | New focused fixture with valid document structure, filtered content, generation 2 and unknown dictionary entries |
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfFormPlacementCloneProjectorTests.cs` | Two additional filtered Form/page preservation and budget-failure cases |

Exact documentation files added or updated:

- `docs/reviews/Post-BF-1054-Batch-B1-Completion-Report.md`
- `docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md`
- `docs/Documentation-Index.md` (generated)
- `docs/Documentation-Health.md` (generated)
- `docs/ValidationReport.md` (generated)

The generated repair preview remained unchanged with zero proposed repairs. No other production or test C# is changed.

## Behavior fixed

The mutation codec previously expanded a single Flate stream into an unrestricted `MemoryStream`. It now uses `PdfFilterDecodeLimits` and `PdfFilterDecodeBudget`, with their unchanged defaults: **256 MiB decoded bytes, expansion ratio 200 and a 64 KiB temporary buffer**. The existing single-filter mutation policy remains stricter than the shared model's general filter-chain count. An internal overload accepts smaller instances of that same limits model for deterministic boundary tests; no public API is added.

Each decoded chunk is accounted before changing destination capacity or writing bytes. Spare capacity is capped against the same byte/ratio limits and the managed array ceiling. Unfiltered content is checked before copying. The fixed work buffer is bounded by the configured buffer and byte limits. The budget is per decode, as in the existing filter model; this is not a whole-document, whole-transaction or total-process memory quota. Existing source/snapshot copies remain subject to their established ownership rules.

Incomplete zlib data also fails predictably. A local input stream rejects another non-empty read after input exhaustion; a completed inflater stops requesting input. This closes the runtime's permissive incomplete-input behavior without a process-wide compatibility switch or a second inflater. The relevant control flow was reviewed in the [.NET 10 DeflateStream source](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.IO.Compression/src/System/IO/Compression/DeflateZLib/DeflateStream.cs). Header, body, trailer and checksum regressions exercise that boundary.

Encoding an empty filtered replacement now emits a complete eight-byte zlib member. This is necessary to keep valid empty mutations round-trippable under the complete-member check: the previous no-write compression path could emit zero bytes. Non-empty compression behavior is unchanged.

## Exception and compatibility review

| Condition | Result and compatibility |
|---|---|
| Decoded-byte or expansion limit exceeded | `PdfFilterDecodeException` with `DecodedByteLimitExceeded` or `ExpansionRatioExceeded`; uses the established filter exception, an `IOException`. This is the approved new rejection of previously unbounded input |
| Corrupt or incomplete Flate data | Exact `InvalidDataException` type and the existing mutation-codec message, with the underlying exception retained. Previously tolerated truncation is now rejected |
| Cancellation | `OperationCanceledException` with the original token; cancellation is checked directly rather than converted to the shared budget's `Cancelled` exception wrapper |
| Unfiltered cancellation | Pre-cancelled decode now observes the token before copying; the previous unfiltered branch ignored it. Encode already observed cancellation |
| Unsupported filters or predictors | Existing `NotSupportedException` policy and messages remain unchanged |
| Invalid filter/parameter types | Existing `InvalidDataException` policy and messages remain unchanged |
| Valid empty Flate / trailing bytes after a complete member | Valid empty content and the established trailing-byte acceptance are retained |

The filter aliases `/FlateDecode` and `/Fl`, name versus single-element-array representation, decode-parameter dictionary/array/null handling and Predictor 1 restriction are retained. Filter chains, other codecs and active predictors remain unsupported for mutation. Unknown dictionary entries are preserved. The codec does not normalize filters or resource/object identity.

Direct, positioned, style and typography consumers retain their current APIs. The typography service does not expose a cancellation token; B1 does not add one. Decode failures propagate before the affected mutation is recorded, and disposal of an enclosing transaction restores earlier edits. Form tests preserve the original shared Form and other placements; they do not change the separately reported stale-placement behavior.

## Regression evidence and coverage

Before the production change, the initial executable regression set had **9 failures and 4 passes**: the high-expansion fixture and eight truncation cases demonstrated the missing guards. Subsequent tests cover exact byte limits and limit plus one across multiple chunks for both unfiltered and Flate content; exact expansion ratio and the adjacent smaller limit; a 1 MiB highly compressible input rejected under default limits; corrupt headers/checksums; missing headers/body/trailer; valid empty input; and original-token cancellation without source mutation.

The focused callers cover direct replacement, positioned `TJ` replacement, fill-color styling and font-size typography. Successful mutations retain following text and dictionary representations, with exact original encoded bytes restored by undo and exact changed bytes restored by redo. Failed budget checks leave bytes, dictionaries and history unchanged. A transaction that first edits one filtered stream and then encounters an over-budget second stream rolls back the first edit. Pre-cancelled direct, positioned and style calls leave history unchanged. These are deterministic pre-cancellation tests; mid-read cancellation checks were reviewed in the codec rather than tested using timing races.

Form projection tests exercise filtered page and Form streams, preservation of the original Form, other placements and dictionaries, and failure without partial object-store changes when page decoding exceeds the expansion budget.

Four permanent save tests exercise direct filtered replacement with full rewrite and incremental update, each using traditional xref tables and xref streams. The incremental source is a complete serialized PDF with its actual `startxref`; its complete original prefix is asserted unchanged. Serialized filter/parameter and unknown entries are checked.

Actual parser-based reopen was additionally exercised in a temporary harness referencing the already-built Services test and Parsing assemblies. This avoids adding a Parsing dependency to the isolated Services test project. All four saved outputs resolved the latest catalog/page/font and content object **4 2 R**, retained `[/Fl]`, the parameter array with Predictor 1 and private parameter 42, private page/stream entries, correct encoded length and exact edited text including the following operator. Full saves had one revision; incremental saves had two. This cross-project reopen validation is recorded evidence, **not an additional permanent release-gate test**.

## Validation results

| Check | Result |
|---|---|
| Focused codec/caller/Form tests | 52 passed, 0 failed, 0 skipped |
| Entire Services test project | 224 passed, 0 failed, 0 skipped |
| Entire SDK.Adapters test project | 34 passed, 0 failed, 0 skipped |
| Actual parser reopen: full/incremental, traditional/xref-stream | 4 combinations passed |
| `git diff --check` | Passed; new untracked files also checked separately |
| Full unmodified release gate | Passed; 1,697 tests, 0 failures, 0 skips across 20 test assemblies |
| Release build and analyzers | 0 warnings, 0 errors |
| Source-size gate | Passed; 1,015 files inspected; existing large-file review entries unchanged |
| DocGen validation | 177 documents; 0 errors, 0 warnings, 0 informational issues |
| Documentation health | 91.6%, A- |
| Documentation repair preview | 0 proposed changes in 0 files; 0 diagnostics |

The first full gate passed its build and all tests, then identified a missing Revision History section in this new report. The required section was added, and the complete unchanged gate was rerun successfully. No production/test adjustment or gate suppression was needed. Final result wording was then updated and documentation generation/validation, health, repair preview and whitespace checks refreshed.

Commands from the repository root:

```powershell
dotnet test tests/BlackFlag.Engine.Services.Tests/BlackFlag.Engine.Services.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~PdfContentStreamMutationCodec|FullyQualifiedName~PdfFilteredTextMutationTests|FullyQualifiedName~PdfFormPlacementCloneProjectorTests' --verbosity quiet
dotnet test tests/BlackFlag.Engine.Services.Tests/BlackFlag.Engine.Services.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet test tests/BlackFlag.Engine.SDK.Adapters.Tests/BlackFlag.Engine.SDK.Adapters.Tests.csproj --configuration Release --no-restore --verbosity quiet
git diff --check
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

The separate reopen command was:

```powershell
dotnet run --project C:\Users\Sufan\AppData\Local\Temp\PiratePDF-PostBF1054-Audit-92e26a4\B1Reopen.csproj --configuration Release --no-restore
```

## Remaining findings and stop point

POST-P0-001 is the only implemented finding. POST-P0-002 through POST-P0-009, all eight POST-P2 findings and both measurement-required P1 candidates remain untouched. The prior structural deferrals stand. B1 adds bounded deterministic decoder cases suitable for future fuzz regression replay; it does not implement B9's fuzz infrastructure or make performance benchmark claims.

B2 and all other implementation batches require separate explicit approval. Canonical font editing, expanded filter/predictor support, plugin isolation, native loading, SBOM, signing and AOT/trimming work are not part of B1.

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-09-06 | PiratePDF Project | Record approved POST-P0-001 implementation, regression evidence, compatibility review and validation |
