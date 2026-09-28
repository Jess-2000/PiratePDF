# Post-BF-1054 Batch B9 - Bounded Fuzz and Corpus Approval Checkpoint

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-029 |
| **Category** | Engineering Review |
| **Version** | 0.1 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1049, BF-1054 |
| **Last Updated** | 2026-09-23 |
| **Next Review** | 2026-12-23 |

## Disposition and baseline

**B9 is not complete. POST-P2-002 remains open.** This is the dependency-approval checkpoint requested by B9 sections 6, 95, 97 and 99, saved at the required completion-report path. The target/corpus/runner design below is proposed, not implemented. No fuzz engine, adapter, runner, seed, dependency, native executable or CI stage has been added. No coverage-guided campaign has run.

The current finding explicitly identifies missing coverage-guided infrastructure. This report conservatively interprets acceptance as requiring demonstrated coverage-guided execution, alongside ordinary replay. That is an interpretation of the finding, not a claim that deterministic corpus replay requires an external engine. Section 95 expressly permits an engine-neutral proposal at this stop. A passing existing test suite cannot close this operational gap.

Branch is `maintenance/post-BF-1054-repository-review`; starting and final uncommitted HEAD is **93865f639acf96f0b9598c97b04d49cea38e8260**, `Harden font catalog responsiveness and bounds`. Initial working tree and diff were clean. B8 is committed and POST-P2-001 remains closed. All fifteen recorded P0 findings remain closed. POST-P2-003 through POST-P2-008 are untouched; AUD-P1-001/002 remain measurement-only; canonical font editing has not started. No commit, push, merge, tag or branch switch occurred.

Governing context is root AGENTS.md, charter/README, the current audit and B1/B2/B3A/B4A/B4B/B4C/F3-OBS-01/B7/B8 and cleanup records, ADR-0010, security contracts and testing/engineering guidance. The current consolidated `Invoke-PiratePDFReleaseGate.ps1` is authoritative and unchanged. The standalone `Test-PiratePDFSourceFileSizes.ps1` remains intentionally absent; historical references do not reinstate it.

## Existing inventory and reuse decision

| Existing capability | Evidence and B9 disposition |
|---|---|
| Deterministic signature exercise | `DocumentSecurityAdversarialHardeningTests.SignatureClassifierToleratesDeterministicAdversarialBytes` uses Random(1049), 4,096 iterations and lengths 0 through 256. It exercises classification, not complete parsing/native execution. Retain unchanged; do not relabel as coverage-guided or count its iterations as committed corpus files. |
| Other finite adversarial tests | The same Security fixture checks bounded counts/overflow and truncated MZ signatures. B3A `PdfCMapTokenizerTests` enumerates 1,331 delimiter combinations with a progress oracle and tests repeated closing delimiters. Retain these permanent regressions. |
| Filter source corpora | `Filters.Tests/Corpus/PdfBinaryFilterCorpus.cs`: eight RunLength cases, four valid/four invalid. `PdfTextFilterCorpus.cs`: eight ASCIIHex cases (five/three), thirteen ASCII85 cases (seven/six). These are 29 source-defined cases, not a cross-target disk corpus or mutation engine. Reuse their owned bytes and expected results. |
| PDF recovery corpus | `Parsing.Tests/TestFixtures/BF-1048R/Fixture-Manifest.json`: ten owned PDF fixtures with provenance and SHA-256 values. Includes valid, damaged xref, misleading stream content, passive actions and security-metadata cases. AuthenticationRequiredMetadata is a structural metadata fixture, not encrypted conformance evidence. Reuse selectively without duplicating the entire corpus. |
| Font fixture builders | Fonts.Tests TestFontFactory, sanitizer numeric/CMap fixtures and subsetter composite/resource-limit fixtures provide owned valid and malformed SFNTs. Preserve checksums/table coherence when the tested fault is elsewhere. No system-font harvesting is needed. |
| Filter/mutation regressions | Filters Flate tests, Parsing `PdfFlateCompletenessIntegrationTests`, Services `PdfContentStreamMutationCodecTests`/limit tests and `PdfFilteredTextTestDocument` supply complete/truncated zlib, ratio and dictionary controls. Ordinary pipeline and mutation policy must remain distinct targets. |
| Text/Form regressions | Services token, compound-operator and Form placement test fixtures provide exact source spans, graph snapshots, B7 stale/context/binding cases and supported transaction/save paths. Use real descriptors, not a duplicate provenance implementation. |
| Security format fixtures | Existing OLE, OOXML, ODT and RTF analyzer tests generate passive, synthetic containers. Reuse generation recipes and bounded analysis options. PDF action analysis lives in Parsing. |
| Native fixtures | Skia `SkiaDctImageCodecLimitTests` and `SkiaJpegTestData` generate tiny JPEGs and dimension mismatches. Fonts fixtures can supply owned embedded programs for actual Skia realization. No downloaded image/font corpus is necessary. |
| Dependencies | Central packages are xUnit 2.9.3, runner 3.1.4, Test SDK 17.14.1, coverlet.collector 6.0.4, SkiaSharp and Linux native assets 4.150.1. No approved coverage-guided engine/instrumenter was found. Coverage collection alone supplies no input-mutation feedback loop. |
| Runner/process infrastructure | No suitable existing single-case fuzz runner, cross-target manifest/replay implementation, failure minimizer or fuzz child-process supervisor was identified in tests/tools. Ordinary test timeouts are not native process containment. |
| Current automation | `.github/workflows/continuous-integration.yml` runs Ubuntu restore/build/test and DocGen in a 20-minute job. It has no fuzz/corpus replay stage or fuzz artifact handling. Existing checkout/setup-dotnet actions and native prerequisites should be reused. |

This inventory is not a claim that every unit-test byte array has been counted. The 29 filter cases and ten PDF files are specifically identified reusable inventories. **New B9 committed seeds: zero.**

## Proposed architecture and access

Use three ordinary net10.0 projects: `tools/PiratePDF.Fuzz.Targets` for target adapters/registry, `tools/PiratePDF.Fuzz.Runner` for bounded replay/single-case execution and process supervision, and `tests/PiratePDF.Fuzz.Tests` for runner/manifest/oracle tests using already approved test packages. A fourth, separately restored `tools/PiratePDF.Fuzz.SharpFuzz` project is thin engine glue. Ordinary solution tests and replay must not require restoring or executing SharpFuzz. Keep that optional project outside the normal solution restore graph; keep its central package declarations local to its tooling subtree.

Each immutable target registration contains a stable ID, configuration version, maximum input bytes, isolation policy and deterministic callable byte-input adapter. A small result contains success or a target-specific rejection code plus bounded structural observations. An unexpected exception propagates to the supervisor as failure. No plugin-style discovery, reflection framework or copied production parser is needed. Registry ordering is ordinal; duplicate/unknown IDs fail before input execution.

Production references flow from tooling into existing engine assemblies. Public sanitization/subsetting APIs suffice for Fonts; never construct a trusted sanitized program by bypassing validation. Existing internal mutation/token/CMap/Skia-realizer seams may require a narrowly named friend declaration in Services, Rendering and Rendering.Skia. Review each assembly's ownership-sensitive internals first; do not expose mutable trusted buffers or add public hooks. A friend declaration is an access grant, not a production runtime dependency. If acceptable access cannot be demonstrated, use an existing public end-to-end path or report the target gap.

Native adapters must be invoked only in a child. The general supervisor should default every arbitrary-input target to child execution initially. The table identifies bounded managed candidates that can later execute repeatedly inside a supervised worker, not permission to run dangerous targets inside xUnit. Resource caps are test configurations below existing production ceilings, not replacements for production policy.

## Proposed target matrix

All IDs below are **proposed; implemented IDs and per-target B9 seed counts are zero**. KiB/MiB mean powers of 1,024. Target caps include any small deterministic fixture envelope, checked before reading/allocating the payload. Fixed dictionaries/configuration belong to the target or manifest, never an unbounded deserialized input configuration.

| Proposed ID | Real production boundary | Max input | Limits and isolation decision |
|---|---|---:|---|
| font-sanitize | PdfOpenTypeFontSanitizer.Sanitize | 64 KiB | Sanitization options: 64 KiB input/table, 32 tables, 256 glyphs, 16 CMap subtables, 64 names. Managed bounded-worker candidate. |
| font-subset-simple | PdfTrueTypeGlyphSubsetter after real sanitization | 64 KiB | Same sanitizer; subset depth 8, 16 components/glyph, 64 retained glyphs, 64 KiB generated output, fixed bounded glyph requests. Managed worker. |
| font-subset-unicode | PdfUnicodeTrueTypeGlyphSubsetter after real sanitization | 64 KiB | Same caps and deterministic bounded Unicode requests; exercise closure/metrics/CMap through supported public paths. Managed worker. |
| cmap-tokenize | PdfCMapParser tokenizer seam | 16 KiB | 4,096 tokens and advancing end-offset oracle; internal bounded seam. Managed worker candidate. |
| cmap-parse | PdfCMapParser.Parse | 16 KiB | 4,096 lexical tokens; existing per-range expansion cap is not an aggregate mapped-output budget. Isolated single-case child until aggregate allocation/work is characterized. |
| filter-flate | PdfFilterPipeline with Flate configurations | 16 KiB | PdfFilterDecodeLimits: 32 KiB decoded, ratio 32, four filters, 1 KiB buffer. Distinct fixed predictor configurations and exact-bound controls. Managed worker candidate. |
| filter-text-binary | PdfFilterPipeline ASCIIHex/ASCII85/RunLength and bounded chains | 16 KiB | Same limits; deterministic filter selection/envelope; no new decoder. Managed worker candidate. |
| mutation-codec | PdfContentStreamMutationCodec.Decode/Encode | 16 KiB | Existing internal limit overload, 32 KiB decoded/ratio 32; unfiltered or single Flate only, Predictor 1 only. Managed worker candidate. |
| text-string | PdfTextStringTokenEditor.Replace | 4 KiB | Valid fixed descriptor plus bounded literal/hex source and replacement; invalid-span subcases explicit. Child initially; establish progress/output bound before worker reuse. |
| text-array | PdfTextArrayTokenEditor.Replace | 4 KiB | Bounded TJ program, actual source mapping and fixed replacement budget; exact unchanged-byte oracle. Child initially. |
| text-compound | PdfCompoundTextOperatorNormalizer | 4 KiB | Bounded quote/double-quote source and bounded state; verify equivalent selected operation and untouched spans. Child initially. |
| form-placement | PdfFormPlacementDescriptor.Create and PdfFormPlacementCloneProjector.Create | 4 KiB | Fixed tiny graph; input controls selected source/binding mutation. Capture before stale mutation. Isolated child; graph and allocation-state checks. |
| syntax-lexer | PdfLexer/PdfTokenReader | 16 KiB | Maximum token length 4 KiB, explicit adapter token/step cap 16,385; child until forward-progress cases establish worker suitability. |
| syntax-object | PdfObjectParser.Parse | 16 KiB | Raw nested arrays/dictionaries have no depth option in this parser seam. Input length alone is not stack safety. Isolated single-case child. |
| pdf-xref | PdfCrossReferenceParser and PdfCrossReferenceStreamParser | 64 KiB | Finite source, shared small decode policy wherever API accepts it; declared counts/offsets still hostile. Isolated child. |
| pdf-object-stream | PdfObjectStreamParser | 64 KiB | Tiny source wrapper, bounded filter policy, adversarial N/First/offsets. Isolated child; do not trust encoded length as allocation bound. |
| pdf-revisions | Existing xref chain/resolver loading path | 64 KiB | Reuse owned minimal revision builder; cycles/extreme Prev and offsets; isolated child. No new revision parser. |
| pdf-recovery | PdfCrossReferenceRecoveryService | 64 KiB | Recovery scan 64 KiB, 256 recovered objects, 32 trailers and finite object-number ceiling via existing options; isolated child. |
| security-ole | LegacyWordBinarySecurityAnalyzer | 64 KiB | Existing inspection limits narrowed as described below; cycles/sector counts. Isolated child. |
| security-ooxml | OoxmlWordSecurityAnalyzer | 64 KiB | At most 32 archive entries and 32 KiB XML parts plus aggregate expansion/work limits. Isolated child. |
| security-odt | OpenDocumentTextSecurityAnalyzer | 64 KiB | Same bounded package strategy using actual supported options; isolated child. |
| security-rtf | RtfSecurityAnalyzer | 16 KiB | Nested groups/control words under existing inspection policy; isolated child until parser-wide depth/work proof. |
| security-pdf-actions | PdfSecurityAnalyzer with actual parser/resolver | 64 KiB | At most 256 resolved objects, depth 16; isolated child. Passive observation only; never execute actions or extract/launch files. |
| native-jpeg | PdfImageDecoder through SkiaDctImageCodec | 64 KiB | Image limits: 64 KiB encoded, 4,096 pixels, dimension 64. Includes intrinsic/PDF-dictionary mismatch. Native child mandatory. |
| native-font | SkiaFontRealizer through a resolved embedded PDF font | 64 KiB | Small fixed font dictionary, bounded font-stream decoding, fresh realizer/disposal per case. Native child mandatory. |

Security adapters should use existing DocumentSecurityInspectionLimits with depth 8, at most 64 embedded objects, 256 KiB aggregate expansion, 64 KiB single-object expansion, ratio 32, 256 observations and 10,000 work units where available. These proposed values need exact-limit fixture validation; no setting may silently become a no-op. If a public boundary lacks a required override, retain its established policy, document the gap and isolate it rather than claiming the proposed cap is enforced.

The embedded-PDF-font realization path is distinct from externally sanitized font embedding: it decodes the embedded program and calls SKTypeface.FromStream. Do not assume external-font sanitization protects this path. Existing fallback can depend on system fonts; use owned embedded success seeds and diagnostic/category oracles for malformed programs, not machine-specific fallback glyph metrics. No packaged native loading/fallback policy change is proposed.

## Rejection and preservation oracles

| Boundary | Proposed expected rejection contract and successful oracle |
|---|---|
| Sanitization/subsetting | PdfExternalFontSanitizationException for sanitizer rejection; downstream InvalidDataException or narrowly evidenced NotSupportedException for unsupported technology/mapping/subsetting. Invalid options supplied by the harness are harness failures. Sanitize twice and compare stable metadata/program output; repeat subset requests and compare bytes/IDs/widths and limits. CMap formats accepted structurally need not all support metadata mapping. |
| Filters | PdfFilterDecodeException with explicit non-cancellation failure categories. Compare exact known-seed output or repeat successful decode hashes/counts. Dispose output. Low-level writer bytes can be provisional on failure; only pipeline/mutation results promise no successful publication. |
| Mutation codec | PdfFilterDecodeException for budgets, InvalidDataException for malformed/incomplete Flate, documented NotSupportedException for unsupported filter/predictor representation. Preserve dictionary representation, decode/encode/decode bytes and source snapshot. Complete-member trailing bytes retain current semantics. Cancellation is not malformed input. |
| CMap | InvalidDataException and established FormatException at the actual parser boundary; characterize other cases individually. Token ends strictly advance. Compare supported mappings deterministically, bounded by isolation where aggregate expansion is unresolved. |
| Token/Form | Allow only documented failures at the precise invalid-span/stale/unsupported call site. An InvalidOperationException is not globally acceptable. Assert exact prefix/suffix bytes, object identities/counts, allocator state and original graph on failure. Valid fixed cases exercise existing commit/rollback/undo/redo and supported save/reopen paths without broadening POST-P2-003. |
| Syntax/xref/recovery | Existing FormatException/InvalidDataException and specific filter failures where the actual call exposes them. Never catch every ArgumentException or InvalidOperationException. Repeated bounded structural summaries must match; decoded xref/object content must agree with owned valid controls. |
| Security | Prefer bounded analysis results/findings where the analyzer converts malformed input to diagnostics. Allow only source-traced domain failures that escape that specific API. Compare stable classifications/observation counts; no network/process/action side effects. |
| Native | Existing managed decode diagnostics/domain failures only. Validate successful dimensions/buffer lengths or stable embedded-font properties. Access violations, abnormal exits, timeouts, OOM and inconsistent output always fail. Do not reinterpret process failure as unsupported font/image. |

OutOfMemoryException, StackOverflowException, arithmetic/index/null defects, hangs, missing child results and crashes are never normal input rejection. Expected cancellation is tested with an explicit cancellation scenario and token assertion; spontaneous cancellation or budget timeout is failure. Exception allowlists are adapter-specific implementation work, not a blanket catch table to copy.

## Corpus ownership, failure handling and automation design

Proposed committed layout is `tests/FuzzCorpus/<target-id>/<seed-id>.bin` plus a versioned manifest. Every seed records ID, target/config version, relative path, SHA-256, generator/source-test provenance, owned/manual/external origin, applicable rights/license statement, purpose, expected outcome category and related finding. Repository-owned is not an invented open-source license: preserve the Charter's current licensing status. Initially use only independently authored repository fixture recipes; external/private/user/system-font/unknown-license seed counts must remain zero.

Categories are valid-minimal, malformed-structural, truncated, numeric-boundary, resource-boundary and historical-regression. Include B1 decoded/ratio exact and over-limit cases; B2 tiny JPEG/intrinsic mismatch; B3A lone closer/generated dictionary; B4B composite depth/cycle/retained glyph/generated-byte limits; B4C unsigned CMap lengths; F3-OBS-01 incomplete/complete/trailing Flate; B7 same-length stale operator, resource binding and changed lexical context. Add small OLE/ZIP/XML/RTF/PDF fixtures from existing tests. Persist minimized bytes and generation recipes; never regenerate accepted expectations from current production output during replay.

Replay must validate manifest schema/version, path confinement (including links), file sizes and hashes before target calls, reject missing/unlisted/duplicate/oversized inputs, sort ordinally, run every listed seed offline and produce a deterministic summary. Successful replay writes no artifacts or corpus changes. Single-case execution uses the same target and config. Harness tests must cover valid success, expected rejection, unexpected exception, oversized input, unknown ID, missing seed, deterministic ordering/repeat output, timeout termination, abnormal child exit and bounded output capture. Existing 4,096 signature iterations remain ordinary tests.

Proposed CLI contract, **not available commands yet**:

```text
PiratePDF.Fuzz.Runner replay --corpus tests/FuzzCorpus --all
PiratePDF.Fuzz.Runner run --target mutation-codec --input <case.bin> --config v1
PiratePDF.Fuzz.Runner mutate --target mutation-codec --corpus tests/FuzzCorpus --seed 1049 --cases 256
```

The optional mutation mode uses a specified stable integer PRNG algorithm/version, bounded bit/byte insertion/deletion/replacement and fixed structural recipes. It records its seed and case index; maximum input and total case counts apply before allocation. It is deterministic mutation, not coverage-guided fuzzing. No PRNG/cases have been implemented or executed at this checkpoint.

The proposed supervisor starts children with ArgumentList and UseShellExecute=false, supplies no shell command from corpus bytes, captures at most 64 KiB per output stream while continuing to drain, and kills the process tree on timeout. Use a ten-second hard wall-clock timeout per isolated replay case and a ten-minute replay budget; supervised managed workers may additionally request cooperative cancellation after two seconds. Confirm termination and treat failure to reap as failure. Cancellation is insufficient for native hangs. Avoid inherited document paths/credentials in results; show target, seed hash, bounded failure code and exit status instead of raw document bytes or arbitrary exception messages.

**Hard whole-process memory enforcement is not currently available or implemented.** A proposed child DOTNET_GCHeapHardLimit of 256 MiB restricts the managed heap, not native allocations, stacks, mapped memory or the entire process. This distinction follows the [.NET GC documentation](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector#heap-hard-limit). A 512 MiB working-set termination threshold, if used, is sampled best effort and can overshoot. Small production budgets, input caps and child lifetime reduce exposure but do not establish a hard RSS quota. Do not introduce Job Object P/Invoke, Unix rlimit or a new container/runtime at this approval stage. No arbitrary long native campaign is justified by a managed heap cap.

Failure artifacts should go to ignored `artifacts/fuzz/<target>/<hash>/`, outside the committed corpus. Bound each input to its target cap, metadata/logs to 128 KiB per case and total local artifact retention to 16 MiB/64 cases. Disable automatic dumps/core files in campaign environments where supported. Corpus-derived file names never become paths. A failure record carries commit, target/config, platform/runtime, tool versions/hashes, seed/recipe, input hash, bounded failure signature and exact reproduction arguments. Raw reproducer bytes are retained only in the explicit local failure directory, never console logs or automatic external uploads.

Minimize manually first with byte-range removal and structure-aware fixture reduction under the same isolated single-case command, preserving the same failure signature. For crashes after approval, bounded libFuzzer minimization is optional; [LLVM documents crash minimization and corpus merging as separate modes](https://llvm.org/docs/LibFuzzer.html). Re-run the reduced case twice in a fresh child and the normal uninstrumented path. A smaller different rejection is not a successful minimization. Promote a reviewed owned reproducer plus manifest provenance and a permanent deterministic subsystem regression only after triage; significant new production defects require a separate authorized correction. Do not silently enlarge an exception allowlist to make replay green.

Proposed CI change is one ten-minute bounded replay step after build/tests in the existing 20-minute Ubuntu job. No new GitHub Action is needed; no release-gate replacement/parity change is included. Measure whether the existing overall job budget is sufficient before accepting the stage. Keep coverage-guided campaigns opt-in and separate from PR replay, with one worker, explicit case/time/input limits and no uploads. No scheduled workflow is proposed initially. A manual local campaign can begin with at most 60 seconds/10,000 executions per managed target; a longer run requires explicit configured bounds, never an infinite default. Windows and Linux execution must be recorded separately; macOS replay/native availability and guided support must be reported honestly.

## Exact external-tool approval proposal

Approval is requested for the following testing-only toolchain; **nothing in this table has been installed, restored, downloaded as a binary or added to a manifest during B9**.

| Component | Exact proposed version, provenance and role |
|---|---|
| SharpFuzz | **2.3.0**, MIT, NuGet package owned by Metalnem; optional glue project only. Targets .NET Standard 2.0. Metadata lists dnlib >=4.4.0, SharpFuzz.Common >=2.2.0 and System.Memory >=4.6.0. These transitive packages are part of the approval scope, with resolved versions/license inventory and lock file reviewed before execution. [Package metadata](https://www.nuget.org/packages/SharpFuzz/2.3.0). |
| SharpFuzz.CommandLine | **2.3.0**, MIT, local .NET tool, not a global installation. Instrumentation command targets .NET 8 or higher; metadata reports release on 2026-06-16, as does SharpFuzz. Tool/runtime compatibility on this repository's net10.0 output still needs a real smoke test. [Tool metadata](https://www.nuget.org/packages/SharpFuzz.CommandLine/2.3.0). |
| libfuzzer-dotnet | **v2025.05.02.0904**, upstream Metalnem release (short commit bd39d4e). Native libFuzzer driver for the managed callback. Release asset names, architecture, hashes and bundled runtime/license details must be inspected and pinned after download approval, before execution; no unverified hash is asserted. [Pinned release](https://github.com/Metalnem/libfuzzer-dotnet/releases/tag/v2025.05.02.0904). |

The driver repository's published license is MIT; LLVM compiler-rt uses Apache-2.0 with LLVM exceptions and includes additional legacy notices. These source licenses do not replace inspection of the chosen binary's bundled notices. See the [driver license](https://raw.githubusercontent.com/Metalnem/libfuzzer-dotnet/master/LICENSE) and [LLVM license](https://raw.githubusercontent.com/llvm/llvm-project/main/compiler-rt/LICENSE.TXT). Package release dates show recent SharpFuzz publication; they do not establish a security audit, support guarantee or compatibility result for this project.

Upstream documents libFuzzer integration on Linux and Windows. Native Windows execution avoids requiring WSL/AFL. Native macOS support for this pinned driver has not been established; do not promise it or silently add AFL/Clang. Engine-independent replay should remain net10.0 portable, subject to existing Skia assets. No new .NET runtime, compiler, package source or native Skia dependency is proposed. If the instrumenter requires a separately installed runtime or the driver requires additional libraries, return for approval instead of installing them implicitly. [Upstream integration](https://github.com/Metalnem/sharpfuzz/blob/master/docs/libFuzzer.md).

Instrumentation would touch **copies** of explicitly allowlisted PiratePDF target assemblies in an ignored publish directory. Never rewrite normal bin outputs used by the release gate, shipping assemblies, framework assemblies, dependencies or the neutral replay binary. SharpFuzz supplies managed IL coverage feedback; it does **not** instrument the existing prebuilt Skia native library or establish ASan/native coverage. Coverage evidence must name the instrumented assemblies and observed counters. The native-target callback runs inside the driver's supervised managed child; adding another uninstrumented subprocess hop would lose feedback unless explicitly supported and verified. Start native execution with isolated deterministic replay; enable guided native-boundary callbacks only after driver timeout/exit propagation is verified.

Proposed campaign setup is publish optional glue, instrument the allowlisted copies, then launch the pinned driver with target/config, `-seed=1049`, target-specific `-max_len`, `-runs=10000`, `-max_total_time=60`, `-timeout=10` and an ignored artifact prefix. These are a proposed configuration, not a successfully executed command. A repository wrapper must add an outer deadline and verified child termination. Driver RSS flags must not be claimed to cap the separate managed child's native allocations. Neither an empty engine run nor replay-only execution demonstrates coverage-guided mutation.

Expected future files are the three neutral project directories, optional SharpFuzz project/local tool manifest and dependency lock/provenance files, `tests/FuzzCorpus`, a narrowly scoped campaign script, ordinary solution entries for neutral projects, approved friend declarations if needed, `.gitignore`, the existing CI workflow, an operational guide and this report/current audit/generated documentation. Production artifacts retain their current references; all new external packages remain testing-only and outside ordinary release restore/build. No SBOM-wide, signing, AOT, loading-policy or roadmap project is included.

The **no-new-dependency alternative** can implement all neutral adapters, owned corpus, deterministic replay/mutation, process supervision, minimization rules, permanent regressions and CI replay using current .NET/xUnit/Skia. That is useful security infrastructure, but supplies no coverage-driven input selection. Under the interpretation stated above it would leave the coverage-guided portion of POST-P2-002 open. It remains available if the user declines this toolchain.

## Actual execution and change ledger

This table answers the task's final-summary fields at the approval stop. Proposed values above are not actual run results.

| Requested evidence | Actual state |
|---|---|
| Branch, starting/final HEAD, initial tree (1-4) | Maintenance branch and 93865f639acf96f0b9598c97b04d49cea38e8260 as above; initially clean. |
| Inventory and 4,096 exercise (5-6) | Inventoried above; retained unchanged. |
| Targets, entry points, isolation, limits/rejections (7-17) | Zero implemented B9 targets. Twenty-five proposed IDs above. No B9 harness timeouts or hard memory limits actually enforced. Native/process memory gap explicit. |
| Corpus layout/counts/categories/provenance (18-28) | Proposed layout above; zero new B9 seeds for every target, zero generated/manual/external/private/unknown-license additions. Historical recipe plan is not a committed corpus. Existing ten PDF fixtures and 29 source cases remain unchanged. |
| Replay/reproduction/mutation (29-33) | Commands proposed only; no runnable B9 command, actual PRNG seed, iteration or mutation case. |
| Engine/campaign results (34-41) | No preexisting or newly approved engine. Tool versions proposed above. Campaign commands/duration/executions/crashes/timeouts/native exits: not run, not measured; do not report a clean campaign. |
| Artifacts/minimization/promotion/CI (42-47) | Design only. No artifacts/minimized failures/promotion/CI modification/scheduled campaign. Existing CI timeout remains 20 minutes. |
| Harness/replay/mutation/guided results (48-51) | Not implemented or run. |
| Defects/fixes/deferred defects (52-54) | No new reproducible defect established or fixed. Parser recursion and CMap aggregate expansion are isolation design concerns, not newly confirmed P0 findings. |
| Production/test/tool/corpus/CI changes (55-58) | Zero. |
| Documentation changes (59) | This report, current audit and normal DocGen outputs; exact final list recorded after validation below. |
| Dependencies/references/public/protected APIs (60-63) | Zero changes. |
| Tests baseline/additions (66-67) | B8 baseline 2,446 tests; zero permanent tests added or removed. |
| Closure/scope confirmations (78-85) | POST-P2-002 open pending dependency approval and actual implementation/validation. Other P2, measurement work and canonical font scope untouched; fifteen P0 and POST-P2-001 remain closed; no Git publication/history/branch operations. |

## Validation at the approval checkpoint

The full unchanged release gate **passed, exit 0**, using `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1`. No targeted B9 suite exists; the existing suite is baseline preservation evidence only. No skip flags, dependency changes or relaxed checks were used. Final evidence for requested summary fields 64-77:

| Check | Actual result |
|---|---|
| Release solution restore/build | Passed; zero build warnings/errors. |
| Existing complete test suite | **2,446 passed across 20 test projects; zero failed/skipped.** No added/removed tests; matches the committed B8 baseline. |
| Relevant full suites within that gate | Fonts 228; Fonts.Platform 98; Filters 119; Rendering 97; Rendering.Skia 25; Services 524; Syntax 304; Parsing 219; Security 92; Editing 133; Writing 153. All passed. |
| Integrated source size | Passed, 1,074 files; unchanged review notices for the 943-line graphics interpreter and 844-line sanitizer. No source files changed. |
| DocGen all/health/repair | 196 documents; zero validation errors/warnings/information; health 91.6%, A-; repair zero changes/files/diagnostics. |
| git diff --check | Passed before and in the gate; final documentation-only refresh checked again. |
| Harness self-tests/replay/mutation/guided campaign | Not implemented/run; no claim of fuzz acceptance from the existing suite. |
| Final scope | Five documentation files, listed below. Zero production/test/tool/corpus/CI/dependency/project-reference/API changes. |

Exact changed files:

- `docs/reviews/Post-BF-1054-Batch-B9-Bounded-Fuzz-and-Corpus-Completion-Report.md` (new approval-checkpoint report).
- `docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md` (open B9 disposition, version 1.19).
- `docs/Documentation-Index.md` (normal DocGen registration/version update).
- `docs/Documentation-Health.md` (normal DocGen count/health refresh).
- `docs/ValidationReport.md` (normal DocGen count/validation refresh).

Only result wording changed after the full gate; normal DocGen all/health/repair and whitespace checks were refreshed afterward. No new defect or unrelated change required correction. This checkpoint validates the documentation and preserves the baseline; it does not satisfy B9's implementation criteria. Stop here for approval of the testing-only toolchain before adding it or starting a campaign.

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-09-23 | Inventory existing adversarial infrastructure and propose bounded targets, owned corpus, isolation and a pinned coverage-guided toolchain; stop before dependency changes, leaving POST-P2-002 open. |
