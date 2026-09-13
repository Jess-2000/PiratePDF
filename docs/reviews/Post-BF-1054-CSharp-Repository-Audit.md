# Post-BF-1054 C# Repository Engineering, Security and Optimization Audit

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-009 |
| **Category** | Engineering Review |
| **Version** | 1.10 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049, BF-1050, BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-13 |
| **Next Review** | 2026-12-06 |

## 1. Executive summary

**Implementation update, 2026-09-08:** B1 (POST-P0-001) and B2 (POST-P0-002) are implemented and validated; see the [B1 completion report](Post-BF-1054-Batch-B1-Completion-Report.md) and [B2 completion report](Post-BF-1054-Batch-B2-Completion-Report.md). The user subsequently approved B3 only (POST-P0-003); its completed implementation and validation are recorded in the [B3 completion report](Post-BF-1054-Batch-B3-Completion-Report.md). At that checkpoint, B4 and later batches were unapproved and untouched. The findings, inventory and baseline results below record the original Phase A assessment.

**Implementation update, 2026-09-09:** The user separately approved B4A only (POST-P0-004). Trusted sanitized-font ownership is now implemented and validated; see the [B4A completion report](Post-BF-1054-Batch-B4A-Sanitized-Font-Ownership-Completion-Report.md) for public byte/stream isolation, trusted internal consumption, copy accounting, downstream save/reopen results and separately reported analogous ownership candidates. The unmodified release gate passed all 1,841 tests. B4B/B4C and later batches remain unapproved and untouched.

Phase A is complete. The baseline passes the existing release gate, but that gate does not cover the defects demonstrated here. Recommend separately approved, narrow hardening batches before beginning canonical font editing. No production or test C# was changed during Phase A.

The original Phase A audit records **9 P0 findings**, **0 established P1 findings**, **2 carried-forward P1 candidates marked measurement required**, and **8 P2 findings**. P0 follows this repository's broad classification of correctness, preservation, and security risks; it is not a claim that all nine findings have critical exploit severity. Evidence is distinguished as bounded reproduction, source-established behavior, or an unverified deployment/test gap.

**Post-audit addition, 2026-09-08:** B3 validation discovered **POST-P0-010, CMap tokenizer non-progress/resource exhaustion**, bringing the recorded P0 total to ten. It was not part of the original Phase A inventory. The user separately approved B3A only; see the [B3A completion record](Post-BF-1054-Batch-B3A-CMap-Tokenizer-Completion-Report.md) for implementation, bounded reproductions, generated-CMap compatibility and validation. Existing finding identifiers and historical baseline evidence are retained.

The highest priority security issues are unbounded editing-path Flate expansion, intrinsic JPEG dimensions checked after native allocation, and mutation of a supposedly trusted sanitized font buffer. Other demonstrated defects affect resource naming, subset bounds, malformed-font failures, embedding permissions, and stale Form placement. Text decorations also lack the required transform and valid text-object placement.

The previous plugin/result hardening and Abstractions test improvements remain incorporated. The earlier structural deferral remains appropriate. No file should be split merely because it is large.

**Characterization update, 2026-09-09:** The separately authorized B4A follow-up confirms four additional in-process ownership defects, POST-P0-011 through POST-P0-014. The cumulative recorded P0 total is fourteen, including previously implemented findings; the original Phase A count and evidence remain historical. See the [ownership candidate characterization](Post-BF-1054-B4A-Ownership-Candidate-Characterization.md) for independent reproductions, negative controls, severity limits and proposed B4A-F1 through F4 batches. All 16 characterization cases and the full unmodified 1,857-test release gate passed. At that checkpoint none of these four findings was fixed or approved for implementation.

**Implementation update, 2026-09-10:** Separately approved B4A-F1 implements POST-P0-012. EncodedBytes now returns independent snapshots, with one snapshot reused by replacement/style/typography consumers. All 20 focused regressions, affected Editing/Services/SDK.Adapters suites, seven supplemental parser reopen cases and the unmodified 1,870-test release gate passed. See the [F1 completion report](Post-BF-1054-B4A-F1-Descriptor-Ownership-Completion-Report.md) for exact files, copy accounting and compatibility limits. No additional issue required an expanded-scope implementation. POST-P0-011/013/014 and other planned batches remain untouched.

**Implementation update, 2026-09-10:** Separately approved B4A-F2 implements POST-P0-014. CopyTo now isolates retained revision bytes with one fixed 64 KiB temporary buffer per call. All 15 focused cases, Writing 153/Services 313 affected-project tests, both incremental xref parser-reopen scenarios and the full unmodified 1,880-test release gate passed. See the [F2 completion report](Post-BF-1054-B4A-F2-Source-Revision-Ownership-Completion-Report.md) for allocation bounds, callback granularity, exception compatibility, exact prefix hashes and final scope. No adjacent production fix was necessary. POST-P0-011/013 and other planned batches remain untouched.

**Implementation update, 2026-09-12:** Separately approved B4A-F3 implements POST-P0-013. Parser-stream Data returns independent snapshots; an additive scalar Length getter preserves allocation-free resource-size checks, and image decoding validates encoded size before the snapshot. All 24 focused cases, complete affected suites, and the unmodified consolidated 1,901-test release gate passed, including its integrated source-size stage. See the [F3 completion report](Post-BF-1054-B4A-F3-Parser-Stream-Ownership-Completion-Report.md) for the cross-assembly review, exact copy costs, real ObjStm/cache/filter regressions and separately recorded truncated-Flate observation. No friend assembly or internal borrowed-memory API was added. POST-P2-008 remains open with wording reconciled to the consolidated gate; F4 and unrelated findings remain untouched.

**Implementation update, 2026-09-13:** Separately approved B4A-F4 implements POST-P0-011. Character-code Bytes returns independent snapshots; composite decoding reuses one snapshot per code. Constructor isolation, equality/hash, Dictionary/HashSet membership, uppercase display and CMap mappings remain stable. All 10 focused cases, Rendering 97/Services 313/Parsing 216 complete suites and the unmodified consolidated 1,909-test release gate passed, including integrated size validation and clean DocGen. See the [F4 completion report](Post-BF-1054-B4A-F4-Character-Code-Ownership-Completion-Report.md). All four characterized B4A ownership findings (POST-P0-012/F1, POST-P0-014/F2, POST-P0-013/F3 and POST-P0-011/F4) are now closed. Other byte-view candidates and findings remain separate and untouched; F3-OBS-01 was not investigated or assigned severity in F4.

## 2. Repository and baseline health

The unmodified authoritative release gate completed successfully:

| Check | Result |
|---|---|
| Source-size gate | Passed; 1,012 gated files: 1,010 C# plus 2 PowerShell scripts |
| Restore | Passed |
| Release build and analyzers | 0 warnings, 0 errors |
| Tests | 1,653 passed, 0 failed, 0 skipped across 20 test assemblies |
| DocGen validation | 175 documents; 0 errors, 0 warnings, 0 informational issues |
| Documentation health | 91.7%, A- |
| Documentation repair | 0 changes in 0 files |
| Whitespace | Passed |
| Working tree after baseline gate | Clean |

Test totals: Abstractions 3; Core 23; Objects 178; Fonts 21; Filters 92; SDK 26; Security 92; Rendering 50; Document 62; Rendering.Skia 1; IO 28; DocGen 62; Services 180; Fonts.Platform 3; Editing 133; Syntax 304; Writing 135; Plugins 31; SDK.Adapters 34; Parsing 195.

These results establish baseline health, not comprehensive malformed-input, native-platform, rendering-conformance, AOT, or signed-release validation.

## 3. Checkpoint, authority and branch

Before audit inspection, the following were verified and confirmed to the user:

- Branch: maintenance/post-BF-1054-repository-review.
- Working tree: clean.
- Baseline HEAD: 92e26a416091e521bdc3fc8e6964ed0678e1f588, matching local develop and the locally available origin/develop reference.
- BF-1054 merge: a7682f1; completion commit: 2200c47; tag: BF-1054-complete.
- Current root AGENTS.md was read.
- Phase A is audit-only; serious P0 findings require separate implementation authorization.

The named Downloads/PiratePDF-Codex-Repository-Audit-Prompt.md contains pre-BF-1054 checkpoint language. Its audit procedure was applied with the user's explicit post-BF-1054 checkpoint, output path, security requirements, and stop condition taking precedence. The supplied post-BF-1054 engineering/security audit document supplied the expanded reporting checklist. Historical instructions do not authorize implementation.

Governing context included root AGENTS.md, PROJECT-CHARTER.md, README.md, Blueprint architecture/engineering/security material, applicable ADRs, BF-1049 security and BF-1050 plugin contracts, BF-1051 transactions, BF-1053 preservation/save closure, and BF-1054 A-V contracts. Particular interaction contracts are BF-1054I trusted font programs, N resource ownership, O Form placement, P filtered mutation, Q external fonts, S/T styling and measurement, and U/V SDK integration and closure.

No branch was created or switched. No commit, merge, tag, push, dependency modification, or roadmap implementation was performed.

## 4. C# inventory and coverage

**1,019 tracked C# files are hand-authored and accounted for**: 710 production, 248 test/support, 60 tooling, and 1 sample. Nine production AssemblyInfo.cs files contain hand-authored friend-assembly declarations. Their exclusion by the source-size gate does not make them generated code.

There are 1,010 ordinary C# files plus those 9 assembly-metadata files, totaling 119,241 physical lines. No tracked generated/vendor C# was identified for hand refactoring. Build-generated obj/bin files are excluded; there were no additional untracked source files at the audit baseline.

Coverage uses the reconciliation explicitly permitted by the task:

- **142 files changed or added since af6faf5**, the prior completed structural assessment: current content/change review, relevant contracts, callers and tests.
- **877 unchanged files**: exact Git comparison to the prior completed review, previous-audit reconciliation, and the current cross-cutting security/architecture pass. Files revisited for new evidence, such as the JPEG adapter, are identified separately below.
- The original audit baseline af3c924 differs from current HEAD in 168 C# paths. Using af6faf5 avoids treating already completed maintenance changes as new work.
- All 9 assembly-metadata files were inspected; no blanket exclusion was applied.

The appendix provides one row per file, physical line count, and review basis. "Reconciled" is not a claim of a fresh line-by-line reread or dynamic execution of every unchanged file. It means previously reviewed content remains identical and its disposition was reconsidered against current requirements. Findings in shared paths are called out even where the implementation is unchanged.

## 5. Previous maintenance reconciliation

| Prior finding/work | Prior disposition | Current evidence | Updated-security effect and action |
|---|---|---|---|
| AUD-P0-001 plugin path containment | Fixed in Batch 1, PP-AUD-003 | Current PluginPackagePath uses full paths plus relative-path containment; plugin code unchanged since completed review | Keep fix. Native fallback and physical-path policy need new readiness coverage, POST-P2-004; do not redo lexical containment |
| AUD-P0-002 cast-mutable plugin collections | Fixed in Batch 1 | Manifest/catalog/activation/session snapshots and regression tests remain unchanged | No recurrence demonstrated; no further collection rewrite |
| AUD-P0-003 cast-mutable security results | Fixed in Batch 2, PP-AUD-005 | Security, Parsing and SDK result wrappers retain defensive snapshots and read-only collections | Keep established behavior. POST-P0-004 is a different byte-buffer ownership problem in new Fonts code |
| AUD-P2-003 Abstractions smoke test | Replaced by meaningful contract tests, Batch 3, PP-AUD-007 | Current three tests pass; no subsequent source change | Complete; do not recreate work |
| AUD-P2-001 graphics decomposition | Deferred by PP-AUD-008 | Interpreter execution file unchanged, 937 lines | Updated security requires targeted hostile-input/transform coverage, not automatic extraction |
| AUD-P2-002 OLE/OOXML/security-analysis decomposition | Deferred by PP-AUD-008 | All three affected implementations unchanged | Preserve deferral; characterize relevant format/budget behavior before any future extraction |
| AUD-P1-001 recovery allocation/scans | Measurement required | ReadSource still materializes bounded whole source; recovery scans remain | Still measurement required; no demonstrated latency improvement |
| AUD-P1-002 identifier ordering | Measurement required | PdfObjectStore.Identifiers still sorts; allocator enumerates it into a set | Still measurement required; do not cache across mutable object-store state without proof |
| Prior "generated AssemblyInfo" classification | Eight metadata files excluded | Nine current files contain hand-authored InternalsVisibleTo declarations | Correct inventory classification in this report; leave historical reports as history |

The current structural assessment supersedes the original audit's decomposition proposal. No blanket refactoring batch is recommended.

## 6. Findings summary

| ID | Priority | Finding | Evidence |
|---|---|---|---|
| POST-P0-001 | P0 | Editing Flate decoding has no output/expansion budget | Bounded reproduction and source |
| POST-P0-002 | P0 | JPEG intrinsic dimensions reach native allocation before limits | Source-established ordering; no large-allocation experiment |
| POST-P0-003 | P0 | Font projections report requested resource name after collision rename | Bounded reproduction |
| POST-P0-004 | P0 | Public sanitized-font memory exposes mutable trusted backing array | Bounded reproduction |
| POST-P0-005 | P0 | Subset root glyphs bypass configured retained-glyph limit | Bounded reproduction; generated-byte guard also late |
| POST-P0-006 | P0 | Malformed font numeric lengths escape the domain failure contract | Bounded reproduction; catalog impact source-traced |
| POST-P0-007 | P0 | Decorations ignore text transforms and emit paths inside text objects | Source plus existing expected-output tests and PDF specification |
| POST-P0-008 | P0 | Font embedding restrictions are reduced incorrectly | Version-aware bounded reproduction and OpenType specification |
| POST-P0-009 | P0 | Stale Form placement can overwrite unrelated current content | Bounded reproduction |
| POST-P0-010 | P0 | CMap tokenizer non-progress/resource exhaustion | Discovered during B3, after Phase A; source characterization and bounded iterator reproduction in B3A |
| POST-P0-011 | P0 | Public character-code alias invalidates equality/hash and caller keys | B4A follow-up reproduction; private CMap keys remain inaccessible |
| POST-P0-012 | P0 | Descriptor byte alias changes stale-source validation decisions | B4A follow-up reproduction in replacement/font/style; replay snapshots remain independent |
| POST-P0-013 | P0 | Parser stream alias permits cached-object/source divergence | B4A follow-up filter and actual ObjStm resolver reproduction |
| POST-P0-014 | P0 | Synchronous destination callback mutates retained original revision | B4A follow-up CopyTo and both incremental xref output modes |
| AUD-P1-001 / AUD-P1-002 | P1 candidates | Recovery materialization; repeated identifier sorting | Measurement required; carried forward, not established P1 benefits |
| POST-P2-001 | P2 | Font catalog build lacks cancellation propagation and aggregate policy | Source-established |
| POST-P2-002 | P2 | High-risk byte boundaries lack an operational fuzz/corpus workflow | Repository coverage gap |
| POST-P2-003 | P2 | BF-1054 integration assertions do not establish reopen/render conformance | Test-content inspection |
| POST-P2-004 | P2 | Packaged native resolution and fallback policy lack platform fixtures | Source and deployment-evidence gap |
| POST-P2-005 | P2 | Dependency provenance, notices and SBOM readiness incomplete | Repository-evidence gap |
| POST-P2-006 | P2 | Official release signing/provenance workflow not established | Repository-evidence gap |
| POST-P2-007 | P2 | AOT/trimming compatibility not demonstrated for dynamic/plugin/native paths | Source and deployment-evidence gap |
| POST-P2-008 | P2 | CI does not run the release gate's source-size check | Workflow comparison |

P3 is recorded as one aggregate category: formatting and preference-only churn. Individual instances were not counted and are not implementation proposals.

## 7. Detailed findings

### POST-P0-001: editing Flate expansion bypasses established bounds

**Location:** src/BlackFlag.Engine.Services/Text/PdfContentStreamMutationCodec.cs, DecodeFlate at line 200 and Copy at line 281. Related contract: src/BlackFlag.Engine.Filters/PdfFilterDecodeLimits.cs.

**Evidence:** the Services codec copies a ZLibStream to a growing MemoryStream until EOF. Cancellation is checked, but there is no decoded-byte limit, expansion-ratio limit, or aggregate allowance before writing. A synthetic 1 MiB zero payload compresses to 1,039 bytes. This codec accepts all 1,048,576 decoded bytes, ratio 1,009.2. The ordinary filter pipeline rejects the identical input with PdfFilterDecodeException under its default ratio limit of 200.

**Impact:** a separately callable mutation path can perform disproportionate allocations on untrusted compressed content. The usual rendering/filter path may reject this particular input earlier; the experiment does not prove that every hostile file reaches mutation through normal document opening. Services also accepts host-created interpreted snapshots, so earlier inspection is not a substitute for bounding this actual decode.

**Smallest change:** use the established decode-budget model at the copying boundary, checking before growth. Preserve the intentionally narrow unfiltered/single-Flate mutation policy, original filter/decode-parameter representation, predictor restrictions, and cancellation behavior. Review exception compatibility explicitly rather than changing it accidentally.

**Risk and validation:** medium, preservation-sensitive; a new rejection is an approved behavior change. Add exact-limit/limit-plus-one, expansion, cancellation, and malformed-stream regressions; exercise direct/positioned/style/Form callers, rollback and undo/redo, then serialize/reopen supported filtered streams. Fuzz the bounded decoder and malformed zlib headers. No benchmark is needed to establish the missing guard; tune any new aggregate defaults using measurement. **Batch B1; affects BF-1054P and its consumers.**

### POST-P0-002: native JPEG allocation precedes intrinsic-dimension limits

**B2 disposition, 2026-09-07:** implemented and validated through an internal bounded codec seam with `IPdfImageCodec` unchanged; the full unmodified release gate passed with 1,732 tests. See the [B2 completion record](Post-BF-1054-Batch-B2-Completion-Report.md) for preallocation proof, compatibility effects and platform limits. The following evidence records the original audit baseline.

**Location:** src/BlackFlag.Engine.Rendering/Images/PdfImageDecoder.cs, Decode around lines 92-109; src/BlackFlag.Engine.Rendering.Skia/Images/SkiaDctImageCodec.cs, Decode lines 23-46; src/BlackFlag.Engine.Rendering/Images/IPdfImageCodec.cs.

**Evidence:** the outer decoder checks PDF dictionary dimensions, calls the codec, then checks decoded dimensions. The Skia codec derives intrinsic JPEG dimensions, constructs SKBitmap, and calls GetPixels before that outer check. The codec contract receives no PdfImageDecodeLimits. Checked multiplication for the managed copy also occurs after bitmap allocation.

**Impact:** small declared PDF dimensions do not bound allocation driven by different JPEG header dimensions. Existing encoded-size limits do not make these dimensions agree. This is a source-established allocation ordering defect; no enormous JPEG was decoded and no native crash or arbitrary-code-execution claim is made.

**Smallest change:** validate intrinsic dimensions and checked pixel/byte sizes against the caller's limits before native allocation. Check allocation/pixel-pointer results before copying. Determine the smallest compatible bounded codec seam; any public interface change requires explicit approval.

**Risk and validation:** medium/high at the native boundary. Test a small dimension-mismatch image and an instrumented preallocation rejection path, checked overflow, truncated JPEG, cancellation where supported, and packaged Windows/macOS/Linux native assets. Do not use destructive memory-exhaustion tests. Fuzz JPEG handling only in a process with memory/time ceilings. Measurement is required for any speed claim. **Batch B2; predates BF-1054, relevant to hostile PDFs and native hardening.**

### POST-P0-003: allocated font resource name is discarded

**B3 disposition, 2026-09-08:** implemented and validated. The three font projectors now propagate the canonical resource mutation result's name. Collision, transaction and supported Writing save/reopen checks pass; the full unmodified release gate passed with 1,789 tests. See the [B3 completion record](Post-BF-1054-Batch-B3-Completion-Report.md) for exact scope, preservation evidence and the separate existing save-facade/CMap limitations discovered during validation. The following evidence records the original audit baseline.

**Locations:** Create return values in src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeResourceProjector.cs:119, PdfExternalTrueTypeSubsetResourceProjector.cs:144, and PdfExternalType0TrueTypeResourceProjector.cs:175. Consumer: src/BlackFlag.Engine.Services/Text/PdfExternalExistingTextFontService.cs, ChangeFont.

**Evidence:** PdfPageResourceMutationProjector correctly allocates an available name and returns ResourceName. All three font projectors instead return their original resourceName argument. In the probe, an existing FExt1 points to 99 0 R; the new Type0 font is 9 0 R under FExt2; the projection still reports FExt1. The external-text service uses the reported name for Tf after CID re-encoding.

**Impact:** subsequent text can select the pre-existing font with bytes encoded for the new one, producing incorrect glyphs or extraction. The resource dictionary itself preserves the old entry, so inspecting only that dictionary misses the defect.

**Smallest change:** return the canonical allocated name from each projection; keep the resource allocator authoritative.

**Risk and validation:** low code volume, medium preservation risk. Regress all three projectors with direct/indirect/inherited collisions; verify the Tf name resolves to the actual new font after serialization/reopen, old mappings are untouched, and content/resource undo/redo is atomic. No performance measurement needed; seed collision cases in the preservation corpus. **Batch B3; affects BF-1054J/K/M/N/Q.**

### POST-P0-004: sanitized-font trust can be invalidated through public memory

**Location:** src/BlackFlag.Engine.Fonts/Sanitization/PdfSanitizedExternalFontProgram.cs, Bytes line 25 and OpenRead line 31.

**Evidence:** Bytes returns an array-backed ReadOnlyMemory over the private trusted array. MemoryMarshal.TryGetArray obtains that array through a public BCL API. Modifying one byte changes Bytes/OpenRead content while Sha256 retains the old value. The existing sanitizer test checks a ToArray copy, not this alias.

**Impact:** the immutable trusted-program and hash/content invariants required by BF-1054I are false. Downstream consumers may process bytes different from those sanitized. This is an in-process ownership defect, not evidence that the SDK already exposes raw font bytes to remote plugins or that managed isolation is an OS sandbox.

**Smallest change:** stop exposing the mutable backing store publicly; preserve an efficient internal read path with explicit ownership, and provide defensive public snapshots or a non-exposing stream. Audit analogous provenance byte views during this narrow boundary review rather than mechanically copying every ReadOnlyMemory in the repository.

**Risk and validation:** medium; additional copies may affect large-font allocation and require measurement before performance claims. Add alias-mutation, retained-view, hash consistency, stream, subset and embed/reopen regressions. Ensure input snapshots and meaningful secret-buffer clearing remain intact. Fuzzing is supplementary to the deterministic ownership regression. **Batch B4; affects BF-1054I and all trusted-font consumers.**

**Disposition, 2026-09-09:** Implemented and validated in B4A. Bytes and OpenRead now expose independent defensive snapshots; assembly-internal inspectors borrow a private read-only span, subsetters retain one working copy, and Services projectors reuse one public snapshot per embedding operation. Characterization also reproduced trusted-buffer exposure through stream-copy destination callbacks. Ownership, hash, metadata/subset/embed and six actual parser-reopen combinations passed; the full unmodified release gate passed 1,841 tests. The [B4A completion report](Post-BF-1054-Batch-B4A-Sanitized-Font-Ownership-Completion-Report.md) records allocation tradeoffs marked measurement required and analogous provenance-view candidates requiring separate approval. POST-P0-005/006 were not implemented.

### POST-P0-005: subset limits do not count initial roots

**Locations:** src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeCompositeClosure.cs, Visit lines 104-108; PdfTrueTypeGlyphSubsetter.cs, selected roots and lines 136-141; PdfUnicodeTrueTypeGlyphSubsetter.cs, roots and lines 218-225.

**Evidence:** callers populate selectedGlyphs before closure traversal. Visit checks MaximumRetainedGlyphs only when Add returns true, so pre-existing roots bypass the count check. A valid font with glyph zero plus requested A succeeds with MaximumRetainedGlyphs=1. The Unicode path can gather far more roots than the default 16,384. Both subsetters check MaximumGeneratedBytes after building the complete SFNT; that guard limits accepted output, not peak allocation.

**Impact:** caller-configured resource limits are not the actual processing boundary. Sanitizer bounds still limit the source, so this is not a claim of infinitely large font data.

**Smallest change:** count all distinct retained glyphs, including glyph zero and initial roots, before processing; preserve dependency/cycle/depth checks. Preflight generated lengths before large output allocations if MaximumGeneratedBytes is intended as a resource guard.

**Risk and validation:** medium. Add simple-root exact/over-limit cases for both subsetters, duplicate roots, glyph zero, composite dependencies, cancellation and checked output-size boundaries. Resanitize output and verify glyph IDs/CID maps/metrics after embedding. Fuzz composite flags, cycles, truncation and root sets. Measure allocation before promising savings. **Batch B4, separable from buffer ownership; affects BF-1054K/L/M.**

### POST-P0-006: malformed font lengths escape predictable failure handling

**Locations:** src/BlackFlag.Engine.Fonts/Sanitization/PdfOpenTypeFontSanitizer.cs, ReadBounded:177 and ValidateCmapSubtable:515/520; src/BlackFlag.Engine.Fonts.Platform/PdfFileSystemExternalFontProvider.cs, BuildSnapshot.

**Evidence:** checked conversion of a CMap uint length occurs before range rejection. A checksummed format-12 fixture declaring uint.MaxValue throws OverflowException, not PdfExternalFontSanitizationException. Separately, MaximumInputBytes=int.MaxValue passes options validation but maximumBytes - total + 1 overflows and a tiny valid font throws ArgumentOutOfRangeException. These were reproduced without large inputs.

**Impact:** expected hostile-input rejection can escape the documented domain contract. The filesystem provider catches supported malformed/IO failures but not these exceptions; its Lazy snapshot can therefore fail the whole catalog and retain that failure. That catalog propagation is source-traced, not a demonstrated desktop UI crash.

**Smallest change:** validate unsigned ranges before narrowing, use overflow-safe remaining-byte arithmetic, and reject invalid option combinations at their boundary. Preserve domain exceptions; do not broadly swallow programming errors in the provider.

**Risk and validation:** low/medium. Add checksummed format-12/13/14 length extremes, short headers, configured int limits, and a directory containing one bad and one valid font. Verify deterministic omission, retry/cancellation policy and absence of partial publication. Add the tiny fixtures to the fuzz regression corpus. No benchmark required. **Batch B4; affects BF-1054I/R.**

### POST-P0-007: decoration output is not positioned in text space

**Locations:** src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleTokenEditor.cs, BuildSuffix and WriteDecoration; PdfExistingTextStyleService.cs, Apply/ResolveMeasuredDecoration; tests/BlackFlag.Engine.SDK.Adapters.Tests/Editing/PdfExistingTextSdkSessionTests.cs, InterfaceStyleUsesScriptColorAndDecorationMappings.

**Evidence:** WriteDecoration always emits a path beginning at x=0 with the supplied offset and advance. It does not consume TextMatrix/TextLineMatrix or translate to the target text origin. For text positioned with 1 0 0 1 100 200 Tm and offset -2, the emitted start remains 0 -2, rather than incorporating the target's translation. The SDK test's expected content also places m/l/S between BT and ET. PDF text objects admit text, general graphics-state, color and marked-content operations; path construction/painting belongs outside that object. Text placement depends on the text matrix and text state. See [Adobe PDF Reference 1.6, sections 5.3 and 5.3.1](https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.6.pdf).

**Impact:** decorations can appear at the wrong position/orientation, and generated content relies on invalid operator placement. Current identity-matrix byte-string assertions do not establish rendered correctness.

**Smallest change:** first characterize actual interpreted text/line matrices and subsequent text position. Emit decoration through a valid path context with correctly transformed geometry, preserving state, clipping, order and following text. Simply injecting ET/BT is insufficient because those operations reset/discard text matrices.

**Risk and validation:** high preservation sensitivity. Require translated/rotated/skewed/scaled text, non-default rise, explicit/automatic widths, script combinations, Tj/TJ/quote operators, clipping, following-line behavior, render and reopen tests. No visual render experiment was performed in Phase A; the missing transform and operator sequence are source-established. No speed claim; fuzz malformed spans only after geometry regressions. **Batch B5; affects BF-1054S/T/U.**

### POST-P0-008: external-font permission mapping loses restrictions

**Locations:** src/BlackFlag.Engine.Fonts/Embedding/PdfExternalTrueTypeInspector.cs, ReadEmbeddingRights:383; src/BlackFlag.Engine.Fonts/PdfSanitizedExternalFontMetadataInspector.cs, fsType mapping:245; src/BlackFlag.Engine.Fonts/PdfExternalFontEmbeddingRights.cs; the two subset projectors and external-text service.

**Evidence:** the inspector collapses accepted programs to Allowed; provider metadata maps 0x0008 to SubsetOnly and does not preserve the independent no-subsetting restriction. The public Type0 projector accepted a sanitized OS/2 **version 3**, fsType=0x0100 font for a subset. Version matters: older OS/2 versions assign these bits differently. OpenType defines 0x0100 as prohibiting subsetting where applicable, 0x0008 as editable embedding, and 0x0004 as preview/print permission. See [Microsoft OpenType OS/2 fsType specification](https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fstype).

**Impact:** a subset can violate the input program's restriction; preview/print and editable permissions are not represented faithfully for an editor. This is a concrete engineering-policy defect, not a legal opinion about any third-party font license.

**Smallest change:** centralize version-aware program permission interpretation and enforce the requested operation against both provider and program restrictions. A narrow fail-closed check can block forbidden subsets before broader public enum design. Do not infer that subset permission makes an otherwise restricted font usable; do not rewrite original fsType flags.

**Risk and validation:** medium/high because policy and possibly public contracts change; require explicit batch approval. Test OS/2 versions, no-subsetting, restricted/bitmap-only, preview/print versus editable, combinations, missing/unknown rights and provider/program disagreement. Existing synthetic fixtures use version-zero OS/2 and must not be mistaken for a version-three permission oracle. Retain them where testing unrelated behavior and add specification-correct permission fixtures. No benchmark needed; fuzz numeric/table handling, not legal interpretation. **Batch B6; affects BF-1054H/J/K/M/Q/R.**

### POST-P0-009: stale Form spans overwrite unrelated operations

**Locations:** src/BlackFlag.Engine.Services/Text/PdfFormPlacementCloneProjector.cs, Create/RedirectPlacement; PdfFormPlacementDescriptor.cs, Create.

**Evidence:** RedirectPlacement validates only offset bounds. A descriptor for the first /X1 Do span [0,6) is created from the existing test fixture. Replacing current page content with "0 0 m\n/X1 Do" and then cloning with that descriptor succeeds, producing "/X2 Do/X1 Do". The original move operation is overwritten. The fixture and probe use ordinary current public projection APIs after descriptor construction.

**Impact:** bounds-valid stale provenance can corrupt current drawing instructions. The descriptor's original source match does not prove that later bytes and resource bindings remain the same.

**Smallest change:** validate the current canonical Do operator, operand/resource identity, stream/page ownership and resource binding before constructing projections. Preserve valid source representation outside the selected span; reject stale content instead of searching for a replacement placement.

**Risk and validation:** medium/high preservation sensitivity. Add same-length replacement, shifted offsets, changed resource binding, wrong owner, direct/inherited resource and filtered-stream cases; require no partial object application, original-Form/other-placement preservation and save/reopen coverage. Include stale-span permutations in the regression corpus. No benchmark needed. **Batch B7; affects BF-1054O/P.**

### POST-P0-010: CMap tokenizer non-progress/resource exhaustion

**Origin:** discovered during B3 save/reopen validation, not part of the original Phase A inventory. This is a new finding; POST-P0-001 through POST-P0-009 retain their identifiers.

**Location:** `src/BlackFlag.Engine.Rendering/Fonts/PdfCMapParser.cs`, `Tokenize` and `Parse`.

**Evidence:** the old `<` path consumed through only the first closing `>` of a dictionary; the ordinary-token branch then excluded `>` without advancing or rejecting it, repeatedly yielding an empty token. `Parse` materialized the sequence with `ToArray()`. B3 observed process memory exhaustion on a generated ToUnicode CMap. B3A confirmed the defect safely before production changes: finite iterator tests for `>`, `>>` and generated-style dictionary syntax failed on empty tokens. No uncontrolled exhaustion reproduction was run in B3A.

**Impact:** a tiny malformed input or valid repository-generated dictionary syntax can cause non-termination and unbounded token materialization at a font/CMap byte boundary.

**Approved correction, B3A:** retain the mapping parser, distinguish dictionary/hex delimiters, consume or reject closing delimiters, guard the single yield site against non-progress, and bound decoded input/token materialization. The public signature and CMap builders remain unchanged.

**Risk and required validation:** medium compatibility sensitivity. Preserve all supported mapping operations and builder output; reject malformed lexical sequences with the existing domain/format exception family. Verify strict progress with finite enumeration, exact/over-budget tests, generated full CID-space compatibility, real edit/save/reopen CMap decoding, complete Rendering/affected Services tests and the full release gate. This closes lexical non-progress, not every possible CMap grammar or aggregate range-work issue.

**Disposition, 2026-09-08:** implemented and validated; targeted, complete Rendering/Services and 16 generated-CMap save/reopen combinations pass. The full unmodified release gate passed with 1,818 tests. See the [B3A completion record](Post-BF-1054-Batch-B3A-CMap-Tokenizer-Completion-Report.md) for exact limits, files and remaining limitations. B4 and all other original findings remain untouched.

**Post-B3A reconciliation, 2026-09-08:** all eleven current uncommitted files were reviewed across the repository. Later user edits comprise the independent byte-array copy in `AddBigEndian`, equivalent tokenizer-test simplifications/indentation and indentation-only changes to the release-gate script. They are retained; token materialization remains eager and bounded, and all B3A limits, progress, exception and mapping contracts remain intact. The [B3A reconciliation record](Post-BF-1054-Batch-B3A-CMap-Tokenizer-Completion-Report.md) distinguishes these edits from the original B3A work and generated output. Current targeted, affected-project and sixteen reopen checks pass; the complete current release gate also passed with the unchanged 1,818-test suite and clean documentation/repair checks. No production, test or script file was modified during reconciliation. No other finding or batch is implemented by this reconciliation.

### POST-P0-011: public character-code mutation invalidates key semantics

**Location:** src/BlackFlag.Engine.Rendering/Fonts/PdfCharacterCode.cs, Bytes/Equals/GetHashCode. Constructor input is copied, but public array-backed memory exposes the stored bytes. A bounded mutation changes equality/hash; Dictionary/HashSet lookup fails after insertion and equivalent duplicate dictionary keys can appear. Public CMap query mutation does not expose or corrupt private stored CMap keys. This is a confirmed public-value correctness defect, not a demonstrated parser-only CMap exploit.

**Original proposal and risk (historical):** B4A-F4, after the more consequential provenance/revision/stream work. Preserve public signatures with defensive Bytes exposure and review internal decoder length/byte access to avoid unnecessary copies. Retain constructor/equality/CMap controls and invert the alias/key-corruption characterization tests when implementing. Allocation impact is measurement required. Details and exact probes: [ownership characterization](Post-BF-1054-B4A-Ownership-Candidate-Characterization.md).

**F4 disposition, 2026-09-13:** implemented and validated after separate approval. Each Bytes access returns an independent snapshot; constructor copying and allocation-free private equality/hash inspection remain unchanged. Caller mutation cannot invalidate dictionary/set keys, alter display or affect private CMap mappings. Composite decoding reuses one snapshot without new public/internal APIs or friend access. All 10 focused cases, complete Rendering/Services/Parsing suites and the consolidated 1,909-test gate passed. See the [F4 completion record](Post-BF-1054-B4A-F4-Character-Code-Ownership-Completion-Report.md) for exact files, decoding/exception controls, added copy costs and closure of all four characterized ownership findings. Unrelated provenance views and F3-OBS-01 remain outside this closure.

### POST-P0-012: descriptor alias changes stale-source validation

**Location:** src/BlackFlag.Engine.Editing/Text/PdfExistingTextDescriptor.cs, EncodedBytes. Public memory exposes copied private provenance bytes. Mutating Hello to World can make a valid target fail. After an authoritative same-length Hello-to-World source change, forging the retained descriptor bytes to World allows font switching and styling to proceed despite stale UnicodeText Hello; font switching reintroduces Hello. Direct replacement still rejects the independent display-run mismatch. Rollback/undo/redo snapshots remain independent and replay correctly. SDK opaque-target registration invalidation was not bypassed.

**Original proposal and risk:** B4A-F1, first. Preserve the constructor copy and property signature; return defensive snapshots and reuse them locally when validation currently reads twice. Characterize typography/external/width-aware consumers alongside the existing direct/font/style and history cases; add save/reopen if orchestration changes. Publicly constructible descriptors remain validation inputs, not unforgeable capabilities. Copies add O(text-byte-length) work; realistic impact is measurement required. [Characterization evidence and limitations](Post-BF-1054-B4A-Ownership-Candidate-Characterization.md).

**Disposition, 2026-09-10:** Implemented and validated in B4A-F1. Public snapshot mutation cannot alter descriptor contents or stale-source decisions. The former font-switch World-to-stale-Hello overwrite remains rejected after attempted snapshot forgery; replacement, font, style, typography, external-font and width-aware controls pass with independent rollback/undo/redo. Public signatures, constructor/SDK/history contracts and resource limits remain unchanged. The [completion report](Post-BF-1054-B4A-F1-Descriptor-Ownership-Completion-Report.md) records 20 focused cases, seven actual parser reopen cases and the full unmodified 1,870-test gate with zero build warnings/errors, along with copy costs marked measurement required and an unrelated supplemental decoder limitation. No other ownership candidate was implemented.

### POST-P0-013: parser stream alias permits cache/source divergence

**Location:** src/BlackFlag.Engine.Objects/PdfStream.cs, Data/Equals/GetHashCode. Public memory exposes the private copied stream array. ASCIIHex input mutation changes decoded A to B. An actual cached ObjStm mutation changes freshly parsed compressed object 42 to 43 while the resolver's previously cached object and untouched source still yield 42. Length/dictionary identity stay intact; array resizing is unnecessary. Document.PdfStream has a separate copy/mutation contract; a copy made before parser mutation remains isolated. No automatic parser-stream-to-writer path was established.

**Original proposal and risk:** B4A-F3, third. Defensive public Data exposure with an explicit cross-assembly consumer/copy review; preserve lazy object identity, generations, filter policy and source bytes. Invert parser/cache mutation tests and cover filter/resource/serialization boundaries actually changed. This has the broadest potential allocation impact; large-stream peak cost is measurement required. Avoid broad friend access or a parallel stream model. [Evidence and scope](Post-BF-1054-B4A-Ownership-Candidate-Characterization.md).

**F3 disposition, 2026-09-12:** implemented and validated after separate approval. Public Data snapshots no longer alter retained bytes, equality/hash, ASCIIHex output or cached/fresh ObjStm interpretation. Every decode path retains one independent input snapshot; no borrow crosses injectable filter/codec callbacks. Scalar Length and pre-snapshot image validation prevent resource-check allocation amplification. Existing public signatures remain intact; Length is the sole additive API. The complete consolidated gate passed 1,901 tests and integrated size validation. See the [F3 completion record](Post-BF-1054-B4A-F3-Parser-Stream-Ownership-Completion-Report.md), including F3-R1 resource-regression prevention and F3-OBS-01 truncated-Flate acceptance requiring separate characterization. No automatic parser-to-Writing bridge was found or introduced.

### POST-P0-014: destination callback mutates preceding revision state

**Location:** src/BlackFlag.Engine.Writing/PdfMemorySourceRevision.cs, CopyTo; forwarding path PdfCountingWriteStream.Write(ReadOnlySpan). A custom managed destination copies the span then writes its first byte through MemoryMarshal.GetReference while the callback is active. A second CopyTo returns changed bytes and hash. Through actual incremental saving, the first destination retains the original prefix but a later ordinary save begins with X instead of `%`; both xref modes reproduce this. This confirms synchronous source mutation, not safe retention of a source span after return. The byte-array fallback exposes an independent temporary buffer.

**Original proposal and risk:** B4A-F2, second. Never pass stored revision bytes into caller callbacks; use an independently owned bounded copy buffer, with explicit review of callback granularity, failure timing and destination ownership. Keep signature, exact byte ordering, Prev/startxref, length and exception behavior. Test chunk edges, throwing/nonseekable destinations, hashes and both xref modes with reopen. Whole-revision cloning adds O(revision-size) peak memory and is not a required design. Throughput is measurement required. [Managed reproduction and limits](Post-BF-1054-B4A-Ownership-Candidate-Characterization.md).

**F2 disposition, 2026-09-10:** implemented and validated after separate user approval. One fixed 65,536-byte per-call array isolates all source-copy callbacks without changing public signatures or metadata. Empty-source rejection, exact exception propagation, chunk boundaries, repeated copies/saves, nonseekable destinations and both incremental modes pass. Actual parser reopen confirms exact original prefixes/hashes, Prev/startxref and original/updated object 3 7 R. The full unmodified release gate passed 1,880 tests. See the [F2 completion record](Post-BF-1054-B4A-F2-Source-Revision-Ownership-Completion-Report.md). No expanded-scope production fix was needed.

### POST-P2-001: font catalog cancellation and aggregate work policy

**Location:** src/BlackFlag.Engine.Fonts.Platform/PdfFileSystemExternalFontProvider.cs, GetFaces/BuildSnapshot/EnumerateCandidates.

GetFaces checks cancellation before entering Lazy.Value, but BuildSnapshot does not receive the token and calls Sanitize without one. Per-file sanitizer bounds do not bound the number of candidate files, retained metadata or aggregate catalog work. This can delay cancellation on a large configured directory; its practical latency and memory cost are **measurement required**, not an established P1 claim.

Smallest proposal: define cancelable snapshot construction and explicit aggregate candidate/retained-work policy without publishing partial snapshots or caching cancellation permanently. Risk: medium, including concurrency and retry semantics. Test cancellation between files, duplicate paths, IO failure, deterministic ordering and bounded traversal. Use a synthetic local directory corpus; no dependency needed. **Batch B8; affects BF-1054R; can follow B4.**

### POST-P2-002: operational fuzz coverage is incomplete

**Locations:** tests/BlackFlag.Engine.Security.Tests/Inspection/DocumentSecurityAdversarialHardeningTests.cs; tests/BlackFlag.Engine.Fonts.Tests/PdfOpenTypeFontSanitizerTests.cs; tests/BlackFlag.Engine.Services.Tests/Text/PdfContentStreamMutationCodecTests.cs; .github/workflows/continuous-integration.yml.

The repository has deterministic adversarial coverage, including 4,096 seeded byte-identification iterations, but no identified coverage-guided harness/continuous corpus workflow for the new font sanitizer/subsetters or mutation token codecs. Existing random identification tests do not exercise the full parser/native pipeline.

Smallest proposal: add bounded entry-point harnesses, owned minimized corpus and regression promotion rules; approve any external fuzz dependency separately. Risk: low production risk, meaningful CI/runtime isolation work. Validate reproducible seeds, expected rejection classes, process time/memory ceilings and corpus replay. No throughput benefit claimed. **Batch B9; new BF-1054 and unchanged parser/native boundaries; targets in section 14.**

### POST-P2-003: new feature tests under-prove preservation conformance

**Locations:** tests/BlackFlag.Engine.Services.Tests/Text/PdfExternalExistingTextFontServiceTests.cs, PdfFormPlacementCloneProjectorTests.cs, PdfExistingTextReplacementServiceTests.cs; tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalType0TrueTypeResourceProjectorTests.cs; tests/BlackFlag.Engine.SDK.Adapters.Tests/Editing/PdfExistingTextSdkSessionTests.cs.

External-font service tests cover shaping rejection and operation metadata rather than the complete ChangeFont workflow. Several "full rewrite" tests assert substrings; the incremental replacement fixture begins with a deliberately synthetic source revision, not a fully reopenable original PDF. These are useful narrow tests but cannot establish font/resource/geometry/preservation conformance. The P0 findings show specific consequences.

Smallest proposal: add a few complete parse/edit/save/reopen/render fixtures, targeted to the affected contracts. Risk: low production risk; avoid nondeterministic system fonts and pixel assertions without a defined tolerance. Characterize failed mutation/rollback before refactors. Validate traditional/xref-stream modes where supported, original prefix, object generations, unknown keys, Form reuse and SDK invalidation. No benchmark needed. **Tests distributed across B1-B7, then B10 for remaining integration gaps; affects BF-1054 closure.**

### POST-P2-004: native resolution policy lacks packaged-platform proof

**Locations:** src/BlackFlag.Engine.Plugins/Runtime/PluginLoadContext.cs, LoadUnmanagedDll:28; src/BlackFlag.Engine.Rendering.Skia/Images/SkiaDctImageCodec.cs; src/BlackFlag.Engine.Rendering.Skia/Fonts/SkiaFontRealizer.cs.

Resolved plugin paths receive lexical package containment. An unresolved native name returns zero, allowing runtime fallback; physical reparse/symlink containment is not established by the lexical helper. No malicious packaged-library reproduction was run. Prior Batch 1 expressly did not prove native/reparse sandboxing, so this is a new policy/fixture gap, not a regression in that fix.

Smallest proposal: specify allowed native dependencies and fallback behavior, then test actual packaged DLL/dylib resolution from a writable unrelated current directory and redirected paths. Do not blanket-block system libraries or redesign plugin isolation without approval. Risk: high compatibility if loader behavior changes. Test Windows/macOS packages plus applicable Linux assets and unload/dispose behavior. No speed claim; native fuzzing must be out of process. **Batch B11; not a BF-1054 regression.**

### POST-P2-005: dependency and SBOM provenance is incomplete

**Locations:** Directory.Packages.props, global.json, src/BlackFlag.Engine.Rendering.Skia/BlackFlag.Engine.Rendering.Skia.csproj, .github/workflows/continuous-integration.yml.

Central versions and deterministic build settings exist, but no tracked dependency/SBOM/third-party-notice inventory or package lock file was found. CI uses version-tagged actions, a floating runner/SDK selection and distribution-native packages. No current vulnerability-feed audit was performed; this finding does not assert a CVE in any installed package.

Smallest proposal: inventory direct/transitive/native purpose, versions, licenses, provenance and update ownership; select a reproducible SBOM/notice and restore verification process. Dependency additions/upgrades remain separately approved. Risk: low for documentation, medium for locked restore/release inputs. Validate against actual restore assets and packaged native files, hashes, notices and reproducibility evidence. Fuzzing not directly applicable. **Batch B12; release readiness, not BF-1054 behavior.**

### POST-P2-006: official release integrity is not demonstrated

**Locations:** .github/workflows/continuous-integration.yml, Directory.Build.props, Invoke-PiratePDFReleaseGate.ps1.

The tracked workflow builds/tests; no official Windows Authenticode or macOS signing/notarization/package-attestation workflow was identified. Deterministic compilation alone does not establish reproducible signed packages or signing-key protection. No shipped artifact was examined, so this is a readiness gap rather than a claim about existing public releases.

Smallest proposal: define a release-only pipeline and acceptance checklist for immutable inputs, protected signing identity, timestamp/notarization verification, hashes/provenance and credential-free build logs. Actual credential setup, publishing and signing need separate authorization. Risk: high operational sensitivity. Validate tampered/wrong-signature rejection and clean-machine install/native loading on each official platform. Fuzzing and performance are not applicable. **Batch B13, after B11/B12; not BF-1054 implementation.**

### POST-P2-007: AOT/trimming support is unproven

**Locations:** src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeHost.cs, Load/Activator.CreateInstance; PluginLoadContext.cs; Rendering.Skia project and platform-font adapters; project publish configuration.

No AOT/trimmed deployment matrix or warning baseline was identified. Dynamic assembly loading and reflection are concrete compatibility pressure points; Native AOT does not support arbitrary dynamic managed assembly loading. See [Microsoft Native AOT limitations](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).

Smallest proposal: explicitly declare supported deployment modes, then run representative publish experiments against serialization, reflection, plugin discovery, Skia/native assets and font/image adapters. Do not enable AOT globally or suppress warnings. Risk: medium/high if deployment behavior changes; fixes require preserving SDK capabilities and platform portability. Validate features on actual published artifacts, not only JIT unit tests. Measure startup/size separately; stripping/AOT is not a secrecy or security boundary. **Batch B14; BF-1054 font adapters are included.**

### POST-P2-008: CI omits integrated release-gate source-size validation

**Current locations:** .github/workflows/continuous-integration.yml; Invoke-PiratePDFReleaseGate.ps1.

**Current-state reconciliation, 2026-09-12:** committed maintenance consolidation moved source-file-size validation directly into the single authoritative Invoke-PiratePDFReleaseGate.ps1 and intentionally removed the standalone script. The current CI workflow still restores, builds, tests and runs documentation checks independently; it invokes neither the authoritative gate nor its integrated size validation. Consolidation therefore does not close POST-P2-008. A CI-green change can still fail the required local gate. Current files pass; size is not itself a defect. Historical validation records accurately describe their original checkpoints and remain unchanged.

Smallest current proposal: execute the authoritative consolidated release gate in CI, or otherwise share its integrated source-size validation without weakening thresholds, exclusions or other checks. Do not recreate the deleted standalone script. Risk: low; validate a controlled over-limit temporary fixture and ensure CI/local results agree without tracked source churn. No benchmark or fuzzing needed. **Open; Batch B15; not implemented by F3.**

## 8. BF-1054 regression and interaction review

| Contract | Current strengths | Finding or required interaction coverage |
|---|---|---|
| A/B identity/discovery | Owner/session, source span, operation index and run ordinal are explicit | Stale Form projection is separate from SDK target invalidation, POST-P0-009 |
| C/D direct and positioned replacement | Source-byte comparison, literal/hex preservation, TJ numeric elements retained | Codec resource bounds; complete reopen fixtures |
| E atomic find/replace | Descending source offsets, rollback, one history transaction have tests | Aggregate work/large-document performance remains measurement required |
| F/S typography, script/color | State restoration and quote normalization tested; invalid inputs reject before mutation | Decoration geometry/operator placement, POST-P0-007 |
| G existing resolved fonts | Existing resource selection and unmappable-text failure tested | No permission to broaden into canonical font editing |
| H-M external fonts | Sanitization gateway, subsetting/cycle checks, CID/ToUnicode construction established | POST-P0-003/004/005/006/008 |
| N resource ownership | Copy-on-write direct/indirect/inherited category tests, collision allocation and cycle rejection | Callers must propagate the allocated name |
| O/P Form/filter mutation | Shared-source intent remains explicit; supported filter representation retained | POST-P0-001/009 |
| Q external text/font change | Content/resource changes use established Editing operation | Missing full service lifecycle tests; collision/permission defects |
| R platform adapters | Platform paths stay outside core engine; best-effort malformed-file omission intended | Numeric failures, cancellation and aggregate catalog policy |
| T measurement | Widths, spacing and local compensation use renderer font data | Characterize transformed/script decoration and subsequent positions |
| U/V SDK closure | Public interface map regression exists; opaque IDs, snapshot invalidation and undo/redo tests pass | Preserve these fixes; no internal-type expansion into SDK |

No completed milestone was rewritten or reopened for implementation in Phase A.

## 9. File-size and decomposition review

No hand-authored source currently reaches 1,000 lines. The following require explicit review because they are at least 600 lines; this table records that review.

| File | Lines | Disposition and reasoning |
|---|---:|---|
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Execution.cs | 937 | Retain PP-AUD-008 deferral; cohesive order/state machine unchanged. Characterize ordering, text/clip/Form state before a future narrow extraction |
| src/BlackFlag.Engine.Fonts/Sanitization/PdfOpenTypeFontSanitizer.cs | 930 | New cohesive sanitizer; real bound/exception fixes are justified, a wholesale split is not. Consider a tested table-validation seam only within approved work if it improves isolation |
| src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordContainerSecurityAdapter.cs | 796 | Retain format-boundary deferral; ZIP/XML/path/shared budgets are coupled intentionally |
| src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.AnalysisState.cs | 763 | Retain traversal/order/identity/budget cohesion; no new independent consumer |
| src/BlackFlag.Engine.Security/Formats/Ole/OleCompoundFileSecurityAdapter.cs | 732 | CFB reader remains a possible future seam, after FAT/DIFAT/mini-stream/cycle characterization |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.cs | 707 | Discrete PDF function evaluator; no new defect or measured gain from decomposition established |
| tests/BlackFlag.Engine.Parsing.Tests/PdfCrossReferenceParserTests.cs | 764 | Cohesive contract suite; reconcile unchanged |
| tests/BlackFlag.Engine.Parsing.Tests/PdfCrossReferenceStreamParserTests.cs | 755 | Cohesive contract suite; reconcile unchanged |
| tests/BlackFlag.Engine.Fonts.Tests/PdfTrueTypeGlyphSubsetterCompositeTests.cs | 714 | Much of size is deterministic SFNT construction; strengthen missing limit cases without arbitrary split |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalType0TrueTypeResourceProjectorTests.cs | 686 | Fixture-heavy suite; permission/collision/reopen tests have greater value than reducing lines |
| tests/BlackFlag.Engine.Parsing.Tests/PdfObjectParserTests.cs | 661 | Cohesive contract suite; reconcile unchanged |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextReplacementServiceTests.cs | 625 | Preservation fixture size justified; strengthen actual reopen coverage |
| tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFileSystemExternalFontProviderTests.cs | 620 | Three behavioral tests plus font builder; add failed-catalog/cancellation cases before factoring fixtures |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalTrueTypeSubsetResourceProjectorTests.cs | 619 | Fixture-heavy suite; no mandatory decomposition |

Repeated synthetic SFNT builders are a candidate for a small shared test fixture only when new tests need it and byte-level control remains clear. No generic test framework or numbered partial-file split is proposed.

## 10. Performance candidates: measurement required

| Candidate and exact path | Existing evidence | Required measurement before change | Preservation risk |
|---|---|---|---|
| AUD-P1-001: src/BlackFlag.Engine.Parsing/Recovery/PdfCrossReferenceRecoveryService.cs, ReadSource/ScanObjects/ScanTrailers | Bounded whole-source allocation and multiple scan phases remain | Representative clean/damaged/encrypted documents, bytes scanned, peak/allocated bytes, recovery duration, cancellation responsiveness | High: newest-wins locations, recovered encryption authorization and diagnostics must match |
| AUD-P1-002: src/BlackFlag.Engine.Document/Objects/PdfObjectStore.cs, Identifiers; src/BlackFlag.Engine.Writing/PdfWriteObjectAllocator.cs | Sorting occurs on enumeration, including allocator set construction | Instrument enumeration count and allocation across full/incremental/projection saves at realistic object counts | Medium/high: deterministic ordering, generation identity, reservation state and cache invalidation |
| BF-1054 repeated decode/encoding/provenance scans | Services repeatedly materializes decoded/encoded arrays and some reverse font mappings | Measure complete user workflows, cold/warm fonts, multi-match streams and history retention; establish which copies are ownership requirements | High: avoid stale snapshots/caches and original-byte changes |
| Catalog enumeration/caching, POST-P2-001 | Cold build traverses all candidates; GetFaces sorts a snapshot | Number of faces/files, cold cancellation, retained metadata bytes, warm-call allocation | Medium: deterministic snapshots and invalidation |

No benchmark was run and no percentage improvement is claimed. Missing security limits are not justified as speculative speed optimizations. Retain BCL/current abstractions until measurements identify a real bottleneck.

For AUD-P1-001, the smallest candidate after measurement is bounded/windowed scanning only if it preserves the same candidate ordering, recovery diagnostics, encryption restrictions and cancellation. Require damaged/encrypted/multiple-revision characterization and a recovery corpus before changing it. For AUD-P1-002, first determine whether allocator-only enumeration can avoid unnecessary ordering without changing the public ordered Identifiers contract; any cache must account for Add/Set/remove mutations. Require object reservation/generation tests and full/incremental save/reopen coverage. Keep these as separate, explicitly approved measurement-led batches. Neither implementation changed during BF-1054; new font/resource projections add consumers, not proof of a performance problem.

## 11. Test gaps and characterization prerequisites

Tests must precede risky changes. Required additions are attached to each finding; the highest value cases are:

1. Complete parse/edit/save/reopen fixtures for font resource collisions and Form placement, with untouched unknown keys and object generations asserted.
2. Rendering/interpretation of decoration under non-identity transforms, text rise, clipping, quote/TJ operations and following text.
3. Resource-bound boundary values, cancellation and rollback before buffer growth; initial subset roots and generated size preflight.
4. A malformed font alongside a valid font, version-aware fsType checks and trusted-memory alias attempts.
5. Native dimension preflight and packaged library-resolution tests on actual platform artifacts.
6. Existing-text resource-change undo/redo through the complete service, not just operation metadata.

Do not loosen the existing precise byte assertions. Add semantic assertions alongside them and correct an expected byte sequence only as part of an explicitly approved defect fix. Existing tests that assert only a subtype marker or non-empty subset are not redundant enough to delete during this audit.

## 12. Security review

Passive handling remains the intended design: security analysis classifies active content rather than executing it. The current authored-source scan found no implicit PDF JavaScript engine, external process launch, HTTP client, BinaryFormatter use, or authored unsafe/PInvoke declaration. That negative scan is limited to tracked source; SkiaSharp and plugin code/native assets remain distinct runtime trust surfaces.

Existing protections include security policy separation, collection immutability hardening, bounded embedded-container work, parser/filter limits, transactional rejection/rollback, and meaningful key-buffer clearing. The new actionable issues are the exact missing/late guards and ownership failures in POST-P0-001/002/004/005/006. Permission interpretation, current resource identity and stale provenance are also correctness/security-policy concerns.

No exploitability, absence-of-vulnerabilities, or package-CVE assurance is inferred from a green build. No mitigation, capability check or preservation rule was weakened.

## 13. Hostile-input and resource-bound review

| Boundary | Current protections | Remaining work |
|---|---|---|
| Syntax, objects, xref/revision recovery | Existing offset/length validation, malformed-input tests, bounded recovery scan and source size | Reconcile unchanged; exercise overflow, cycles, contradictory revisions and cancellation in corpus |
| Core filter pipeline | Default 256 MiB decoded bytes, ratio 200, maximum 8 filters | Preserve limits; mutation codec must not bypass equivalent accounting |
| Embedded-container inspection | Defaults include depth 16, 10,000 objects, 2 GiB aggregate expansion, 512 MiB per object, ratio 1,000, 100,000 observations, 5,000,000 work units | Values are policy ceilings, not demonstrated interactive-memory targets; measure combined live allocations rather than arbitrarily tightening them |
| Image decode | PDF dimensions, encoded bytes, decoded pixel/dimension limits | Intrinsic JPEG preallocation gap, POST-P0-002 |
| External font sanitizer | 64 MiB input, 32 MiB single table, 128 tables, 65,535 glyphs, 256 CMap subtables, 4,096 name records | Overflow-safe rejection and immutable trusted output |
| TrueType subsets | Composite depth 32, 256 components/glyph, configured roots/output bounds | Initial-root count bypass and late output guard |
| Resource inheritance/Form use | Cycle-aware resource ownership and explicit shared-source intent | Current-byte/resource validation for stale Form placement |
| Font catalog | Per-file sanitizer and deterministic metadata snapshot | No demonstrated aggregate cancellation/work policy |

These limits come from current source, not benchmark-derived recommended desktop budgets. A shared limit value need not be forced across unlike subsystems, but each actual allocation/recursive/work boundary must enforce its own intended limit.

## 14. Fuzzing targets and corpus

| Priority target | Entry files | Seeds and oracle |
|---|---|---|
| Font sanitize/subset | Fonts/Sanitization/PdfOpenTypeFontSanitizer.cs; Fonts/Embedding/PdfTrueTypeCompositeClosure.cs and both subsetters | Minimal checksummed fonts; offsets/lengths, format 4/12/14 CMaps, short loca/glyf, composite cycles/depth/flags, huge declared counts. Expected domain rejection or stable sanitized result; no hang/unbounded growth |
| Filter/mutation | Filters/PdfFilterPipeline.cs; Services/Text/PdfContentStreamMutationCodec.cs | Tiny high-ratio zlib, truncated streams, predictor/filter dictionaries, exact budget edges; bounded rejection and no mutation |
| Text token/provenance | Services/Text/PdfTextStringTokenEditor.cs, PdfTextArrayTokenEditor.cs, PdfCompoundTextOperatorNormalizer.cs, PdfFormPlacementCloneProjector.cs | Literal/hex escapes, numeric TJ tokens, invalid spans, changed same-length operations; unaffected bytes/state and stale rejection |
| PDF syntax/xref/object stream | Syntax and Parsing entry points; Parsing/Recovery/PdfCrossReferenceRecoveryService.cs | Existing malformed fixtures, cycles, generations, Prev chains, conflicting offsets; predictable exception and preservation oracle |
| Containers/actions | Security/Formats/Ole, Ooxml, Odt, Rtf; Parsing/Security/PdfSecurityAnalyzer.AnalysisState.cs | Mini-stream/FAT loops, ZIP relationships, XML depth, oversized embedded objects, action dictionaries; aggregate budget and passive-only handling |
| Native images/fonts | Rendering.Skia/Images/SkiaDctImageCodec.cs and Fonts/SkiaFontRealizer.cs | Small malformed headers/fonts, packaged native versions; process-isolated memory/time ceiling and minimized crash inputs |

Paths in this table are relative to src/BlackFlag.Engine.* project roots as named. Reuse existing deterministic fixtures; record provenance/licenses for external corpus files. Keep private user documents out of the corpus. New reproducible failures become small deterministic regression cases. A seed loop is useful but is not coverage-guided fuzzing. No fuzz campaign ran during Phase A.

## 15. Sensitive data and memory lifetime

PdfStandardSecurityAuthenticator and its AES/legacy helpers clear mutable derived password/key buffers in relevant finally paths. PdfStandardSecuritySession clears its retained file key on Dispose. Preserve caller ownership and existing disposal behavior.

Document plaintext also exists intentionally in parsed objects, decoded streams, Unicode descriptors/SDK strings, and undo/redo snapshots. Their lifetime is tied to document/session/history ownership. No guaranteed erasure of arbitrary managed strings or copies is claimed. Clearing a caller-owned input or history needed for rollback would break the contract.

The mutation codec uses in-memory buffers rather than adding plaintext temporary document files. The audit's temporary probe contains only synthetic fixtures. Native rendering can create additional pixel/font copies; measure and map their ownership before introducing pooling. Review diagnostic sinks for document text, paths and credentials as each sink is added; the reviewed paths do not establish a global application logging guarantee.

POST-P0-004 is an integrity/lifetime ownership defect, not an instruction to zero arbitrary font/document memory.

## 16. Native, PInvoke and library loading

Native work is concentrated in Rendering.Skia and plugin loading. Core engine projects have no new platform-specific PInvoke implementation; filesystem font adapters remain in Fonts.Platform. Managed declarations do not describe all native behavior inside dependencies.

The JPEG allocation ordering requires B2. Native package resolution requires B11: validate where the actual packaged library was loaded from, behavior with an unrelated writable current directory, permitted system dependencies and architecture mismatch. Test missing/wrong native assets and predictable failure rather than falling back silently to a weaker policy.

Package containment is lexical; it is not proof of physical symlink/reparse containment. An AssemblyLoadContext is not an OS sandbox. Do not claim native safety from AOT or symbol stripping.

## 17. Plugin and SDK isolation

The isolated SDK project retains SDK-owned contracts. The text adapter uses opaque targets, host-created source registration and invalidation after mutation/history changes. Existing interface-map and assembly-boundary tests pass; the earlier explicit-interface stub bug was already corrected.

Plugin policy/capability authorization, activation plans and read-only collection snapshots remain intact. The reference plugin and test plugin were accounted for. Loader/native fallback and reparse policy remain separately identified in POST-P2-004.

No evidence warrants broadening SDK contracts with Document, Rendering, Editing, Services, source offsets or raw external font buffers. Loading arbitrary managed/native plugin code in process cannot establish containment against hostile code by type boundaries alone. A future stronger process-isolation design requires an approved architecture workstream, not a cleanup refactor.

## 18. Dependency, SBOM and supply-chain readiness

Current centrally declared packages:

| Dependency | Version | Current role |
|---|---|---|
| SkiaSharp | 4.150.1 | Rendering adapter and native binding |
| SkiaSharp.NativeAssets.Linux | 4.150.1 | Linux native runtime assets |
| Microsoft.NET.Test.Sdk | 17.14.1 | Test infrastructure |
| xunit | 2.9.3 | Test framework |
| xunit.runner.visualstudio | 3.1.4 | Test discovery/runner |
| coverlet.collector | 6.0.4 | Test coverage collector |

global.json requests SDK 10.0.302 with latestPatch; the local audit used 10.0.302. Directory.Build.props enables deterministic builds and strict analyzer/build behavior.

This table is not an SBOM. It omits the resolved transitive graph, native component provenance and license texts. No tracked lockfile, LICENSE/NOTICE-named inventory, or release SBOM artifact was found in the targeted tracked-file search. This does not establish that every external package lacks upstream license metadata.

B12 should record package purpose/license/update owner, restore-source policy, resolved asset hashes and notices. Review action pinning and floating CI platform inputs without introducing unauthorized dependency changes. No online vulnerability feed or license adjudication was performed.

## 19. Native AOT and trimming

Ordinary Release compilation is clean; no Native AOT or trimmed publish was performed. This audit therefore reports no measured startup/size benefit and no AOT warning-free claim.

Dynamic plugin assembly loading/reflection needs a declared supported deployment model before AOT can be accepted. Serialization, discovered plugin constructors, SkiaSharp assets, native fonts/images and platform adapters require tests using published output. Do not use broad reflection roots or warning suppression as a substitute for preserving required behavior. B14 records feasibility and explicit incompatibilities; it does not silently remove plugins.

## 20. Release integrity, signing and provenance

The repository's local gate and CI are engineering checks. They do not alone prove signed installer integrity, protected signing credentials, notarization, immutable release inputs or reproducible packages.

Before official distribution, B13 should make Windows Authenticode and macOS code-signing/notarization verification concrete, record hashes and provenance, separate build/publish privileges and keep credentials outside source/logs. Signing timestamps and package metadata can affect byte-for-byte reproducibility; document which artifacts are reproducible rather than claiming determinism for the entire signed release.

Phase A neither signed nor published anything.

## 21. PDF preservation review

Keep the established Writing authority for object allocation/projections and save modes. No evidence supports simplifying identity/generation semantics, xref tables/streams, Prev/startxref chains, original revision bytes, unknown entries, resource inheritance, stream filters or page-tree ownership.

The current defects are localized departures from those contracts: wrong returned resource names, stale placement spans, and decoration geometry/context. Correcting them must preserve unrelated resource entries, original Form instances, other placements, annotations/metadata, content order and text position.

Reopen validation is mandatory for preservation-sensitive batches. Incremental tests must use a genuinely reopenable source revision when asserting end-to-end correctness, preserve its original prefix, and validate the appended xref/object references. Full rewrite must retain the document version and supported semantic state. No normalization of unrelated valid PDF objects is authorized.

## 22. Architecture and cross-file pass

Dependency direction remains coherent: SDK contracts are isolated; adapters translate; Services orchestrates Rendering/Fonts/Editing/Writing; platform font locations and Skia native implementation stay outside the platform-neutral engine layers.

Concrete cross-file mismatches are:

- Established filter limits versus Services' unbounded equivalent transformation.
- Canonical resource allocation versus discarded returned names in three font builders.
- Trusted immutable program claims versus a mutable public backing buffer.
- Different embedding-permission interpretations in two font inspectors.
- Correct original provenance construction versus missing revalidation at later Form projection.
- Stateful text interpretation versus decoration emission that omits that state.
- Local gate requirements versus independently maintained CI steps.

These support using existing contracts consistently, not adding a second font model, generic orchestration framework, or speculative cache. Similar font-table readers and test builders are maintenance seams to evaluate during real defect fixes; no independent broad DRY batch is justified.

## 23. Proposed implementation batches

**This table records the Phase A proposals and the later B3A addition; the report itself grants no approval.** The user subsequently approved B1, B2, B3 and B3A as separate batches; see the [B1 completion record](Post-BF-1054-Batch-B1-Completion-Report.md), [B2 completion record](Post-BF-1054-Batch-B2-Completion-Report.md), [B3 completion record](Post-BF-1054-Batch-B3-Completion-Report.md) and [B3A completion record](Post-BF-1054-Batch-B3A-CMap-Tokenizer-Completion-Report.md). Explicit user approval remains required for every other batch, including serious P0 issues. Accept each implementation batch only after targeted tests, affected-project tests, diff checks and the full unmodified release gate.

| Order | Batch | Exact finding scope and principal files | Preconditions and acceptance |
|---|---|---|---|
| 1 | B1 bounded mutation decode | POST-P0-001; Services/Text/PdfContentStreamMutationCodec.cs and its codec/service tests | Characterize supported filters, exception/cancellation and rollback; preserve serialization/reopen |
| 2 | B2 native JPEG preallocation | POST-P0-002; Rendering/Images/PdfImageDecoder.cs, IPdfImageCodec.cs only if approved, Skia/Images/SkiaDctImageCodec.cs, decoder/native tests | Approve compatible limit seam; prove rejection before allocation; platform native checks |
| 3 | B3 resource-name correctness | POST-P0-003; the three Services/Fonts projectors and their tests; external-text integration test | All collision/resource ownership variants and save/reopen |
| 3A | B3A CMap tokenizer (added after Phase A; separately approved) | POST-P0-010; Rendering/Fonts/PdfCMapParser.cs and directly relevant Rendering/Services font tests | Bounded progress/materialization proof, delimiter compatibility, generated-CMap parse/save/reopen |
| 4 | B4 font trust and bounds | POST-P0-004/005/006; Sanitization program/sanitizer, CompositeClosure, both subsetters/SfntBuilder as required, focused Fonts/Platform tests | Prefer three independently reviewable sub-batches; characterize exception/ownership before patching |
| 5 | B6 font permissions | POST-P0-008; both font inspectors, embedding policy, subset projectors, permission tests | Explicit policy/API approval; version-aware fixtures and program/provider disagreement |
| 6 | B7 current Form provenance | POST-P0-009; FormPlacementCloneProjector/Descriptor and Form/resource tests | Current-token/binding checks, no partial projections, other placements unchanged |
| 7 | B5 decoration semantics | POST-P0-007; StyleService/StyleTokenEditor and Services/SDK rendering integration tests | Geometry/state characterization first; any wider preservation semantics need explicit approval |
| 8 | B8 font catalog responsiveness | POST-P2-001; Fonts.Platform provider and tests | Complete B4 failure-contract work; define cancellation/retry and aggregate policy |
| 9 | B9 bounded fuzz/corpus | POST-P2-002; bounded harness/test/corpus/CI files selected in approved proposal | Tool/dependency approval if required; process ceilings and deterministic replay |
| 10 | B10 remaining conformance tests | POST-P2-003; named Services/SDK tests and deterministic fixtures | Complement B1-B7, do not duplicate their regressions |
| 11 | B11 native loading readiness | POST-P2-004; PluginLoadContext, native adapter/package fixtures and policy docs | Approve fallback/reparse policy before behavior changes |
| 12 | B12 dependency provenance | POST-P2-005; central package/restore configuration only as separately approved, inventory/notices/SBOM docs | No implicit upgrade; validate actual resolved and packaged assets |
| 13 | B14 AOT/trim feasibility | POST-P2-007; controlled publish fixtures and deployment documentation | Declare plugin/deployment support and evaluate warnings without suppression |
| 14 | B13 official release readiness | POST-P2-006; release documentation/workflow after B11/B12/B14 decisions | Separate credential/sign/publish authorization, clean-machine artifact validation |
| 15 | B15 gate parity | POST-P2-008; CI and existing source-size invocation | Reuse authoritative checks without weakening them; may be approved independently |
| Later | Measurements only | AUD-P1-001/002 and section 10 | Gather baseline before proposing optimization; no bundled speculative production changes |

Recommended first approval: **B1**, because it closes a demonstrated resource-bound omission in the common mutation codec while preserving its established filter policy. B3 is a small independent correctness batch. B2 is urgent but requires careful codec/native contract review. Keep preservation-sensitive fixes separate from structural decomposition and deployment policy.

## 24. Rejected churn

Rejected: repository-wide formatting; mechanical LINQ/loop replacement; caching without a proven hot path and invalidation policy; pooling without clear ownership; forced splitting of 600-999-line files; generated-code hand edits; public API renaming; removing strict tests; normalizing unrelated PDF state; new dependencies for cosmetic cleanup; and generic frameworks to hide a few shared validation steps.

The user's request to clean up the temporary diagnostic Program.cs was handled in that temporary probe only. It was formatted and rerun; no repository Program.cs was edited.

## 25. Deferred roadmap work

Canonical font editing architecture, paragraph/line reflow, cross-object layout, complex-script shaping, expanded Type3/composite editing, broader symmetric filter encoders, Form-specific style/external-font integration, application UI and stronger plugin process isolation remain separate workstreams. Existing fail-closed behavior in unsupported cases must remain.

The reported defects affect implemented contracts and do not authorize any of these features. A fix that cannot be kept within its approved maintenance batch must return for explicit scope approval.

## 26. Validation commands and reproduction record

Commands ran from C:\Development\PiratePDF unless a temporary project path is shown. Read-only discovery used git ls-files/log/diff/show, rg and Get-Content; failed discovery paths were corrected and were not counted as successful checks.

~~~powershell
git branch --show-current
git status --porcelain=v1
git rev-parse HEAD develop origin/develop
git tag --list '*1054*'
git ls-files '*.cs'
git diff --name-only af3c924 HEAD -- '*.cs'
git diff --name-only af6faf5 HEAD -- '*.cs'
git diff --check
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
dotnet run --project 'C:\Users\Sufan\AppData\Local\Temp\PiratePDF-PostBF1054-Audit-92e26a4\AuditProbe.csproj' --configuration Release --no-restore
dotnet run --project 'C:\Users\Sufan\AppData\Local\Temp\PiratePDF-PostBF1054-Audit-92e26a4\AuditProbe.csproj' --configuration Release --no-restore -- --validate-docs
git diff --no-index --check -- NUL docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md
~~~

The probe references already built Release assemblies and existing synthetic test-fixture builders. It does not alter repository projects/tests or add package dependencies. Reflection is used to reach internal codec and fixture helpers; the font-memory alias itself uses public MemoryMarshal without reflection/unsafe code.

Observed output:

~~~text
FONT_ALIAS: backing array exposed=True; stored hash matches current bytes=False
FONT_COLLISION: reported=FExt1; FExt1=99 0 R; FExt2=9 0 R; newFont=9 0
FONT_RIGHTS: accepted Type0 subset of OS/2 version 3, fsType=0x0100; font=9 0
GLYPH_LIMIT: accepted requested glyph plus glyph zero with MaximumRetainedGlyphs=1; CID count=1
MAX_INPUT: ArgumentOutOfRangeException
CMAP_LENGTH: OverflowException
EDIT_FLATE: encoded=1039; decoded=1048576; ratio=1009.2
CORE_FLATE: PdfFilterDecodeException
STALE_FORM: /X2 Do/X1 Do
~~~

Reproduction recipes, independent of retaining the temporary project:

- Alias: sanitize TestFontFactory.CreateMinimalTrueType; TryGetArray on Bytes; mutate one byte; recompute SHA256 and compare to stored Sha256.
- Collision: use PdfExternalType0TrueTypeResourceProjectorTests.CreateContext; set page Font/FExt1 to an existing reference; call Create with preferred FExt1; compare returned ResourceName and projected resource bindings.
- Permission: take the Type0 synthetic font tables, set a 96-byte OS/2 table to version 3 and fsType 0x0100, rebuild SFNT checksums with TestFontFactory.BuildSfnt, sanitize, then request a Type0 subset.
- Glyph limit: request A from the same valid Type0 fixture with MaximumRetainedGlyphs=1; glyph zero and A are both retained.
- Numeric errors: set MaximumInputBytes=int.MaxValue on a tiny valid font; separately change a CMap subtable to format 12 with uint.MaxValue declared length and rebuild checksums before sanitizing.
- Expansion: compress 1,048,576 zero bytes with ZLibStream; compare mutation decode against core PdfFilterPipeline with default limits.
- Stale Form: construct the existing fixture's first [0,6) /X1 Do descriptor; replace current content with 0 0 m followed by newline and /X1 Do; call the clone projector and inspect projected page content.

The entire baseline gate ran before adding this report. The temporary helper then loaded the built DocGen DocumentationLibraryLoader and DocumentationValidator directly, without invoking report/index writers: the 176-document library including this report has 0 errors, 0 warnings and 0 informational issues. This preserves the report-only repository scope. Final inventory/path/whitespace checks passed; the only working-tree addition is this report. The report does not claim another implementation acceptance gate. Generated documentation indexes should be refreshed as part of any later approved documentation acceptance/commit batch.

## 27. Environment and limitations

Windows/PowerShell workspace; .NET SDK 10.0.302. A temporary probe's initial restore encountered restricted access to the user NuGet configuration; the approved retry succeeded, and subsequent no-restore runs completed. pwsh was unavailable, so the authoritative gate used powershell.exe. Temporary helper formatting was verified by rerunning its bounded observations.

No production/test C#, project, dependency or generated source was edited. Temporary synthetic diagnostic C# exists outside the repository. No dangerous native memory-exhaustion payload, extended fuzz campaign, benchmark, macOS/Linux runtime execution, AOT/trim publish, signed installer, live vulnerability-feed audit or real private document corpus was exercised.

Source searches and prior-review reconciliation are not formal verification. Suspected performance gains are measurement required. Native exploitability, current deployed artifact signing and complete downstream parser immunity remain unproven. Local origin/develop is a checked local reference; no remote fetch was required or performed.

## 28. Final coverage and Phase A stop

Every current tracked hand-authored C# file is accounted for by the review/reconciliation method in section 4 and the appendix: **1,019 of 1,019**, with **142 current changed-file reviews and 877 unchanged-file reconciliations**. No source file was selected for change because of size alone.

Phase A ends with this report and the proposed batches. **Phase B has not begun and requires explicit user approval.**

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-06 | Post-BF-1054 audit, prior-work reconciliation, bounded reproductions, updated security/deployment review and proposed approval-gated batches. |
| 1.1 | 2026-09-07 | Record separately approved B2 implementation disposition and completion evidence; retain the historical audit baseline and all other findings. |
| 1.2 | 2026-09-08 | Record separately approved B3 disposition and link its validation evidence and limitations; retain the historical audit baseline and other findings. |
| 1.3 | 2026-09-08 | Add POST-P0-010 discovered during B3 after Phase A; record separately approved B3A and its validation without renumbering original findings. |
| 1.4 | 2026-09-08 | Record repository-wide reconciliation of post-B3A user changes and current validation; preserve prior batch history and all deferred findings. |
| 1.5 | 2026-09-09 | Record B4A-only completion of POST-P0-004, ownership/stream regressions, downstream reopen and full release-gate validation; link separately reported analogous candidates while preserving historical findings and deferred batches. |
| 1.6 | 2026-09-09 | Record characterization-only POST-P0-011 through POST-P0-014 with bounded reproductions, negative controls and proposed independent batches; implement no ownership fix. |
| 1.7 | 2026-09-10 | Record B4A-F1 completion of POST-P0-012, independent descriptor snapshots, consumer reuse, ownership/history/SDK/save validation and full release-gate success; retain deferred findings and historical characterization. |
| 1.8 | 2026-09-10 | Record B4A-F2 completion of POST-P0-014, bounded source-copy isolation, callback/exception compatibility, exact incremental-prefix and parser-reopen validation, and full release-gate success. |
| 1.9 | 2026-09-12 | Record B4A-F3 completion of POST-P0-013, parser-stream snapshots and bounded resource checks, real cache/filter regressions and consolidated release-gate success; reconcile POST-P2-008 wording while keeping it open. |
| 1.10 | 2026-09-13 | Record B4A-F4 completion of POST-P0-011, stable character-code ownership/key semantics, decoder snapshot reuse and full validation; close the four characterized B4A ownership follow-ups while retaining unrelated findings and F3-OBS-01. |

## Appendix A. Per-file coverage ledger

C = current review of changed/added content since af6faf5, including applicable prior unchanged portions. R = previously reviewed; unchanged by exact Git comparison; reconciled against current context. R+ = unchanged, with specific current reinspection for a finding or measurement candidate. M = hand-authored assembly metadata, inspected in this audit. A row is coverage, not an assertion that every method was dynamically tested.


| Path | Lines | Basis |
|---|---:|---|
| samples/BlackFlag.Engine.ReferencePlugin/ReferencePlugin.cs | 38 | R |
| src/BlackFlag.Engine.Abstractions/IO/IRandomAccessSource.cs | 47 | R |
| src/BlackFlag.Engine.Core/Objects/PdfBoolean.cs | 44 | R |
| src/BlackFlag.Engine.Core/Objects/PdfInteger.cs | 17 | R |
| src/BlackFlag.Engine.Core/Objects/PdfName.cs | 30 | R |
| src/BlackFlag.Engine.Core/Objects/PdfNull.cs | 25 | R |
| src/BlackFlag.Engine.Core/Objects/PdfObject.cs | 12 | R |
| src/BlackFlag.Engine.Core/Objects/PdfObjectKind.cs | 22 | R |
| src/BlackFlag.Engine.Core/Objects/PdfReal.cs | 20 | R |
| src/BlackFlag.Engine.Core/Objects/PdfString.cs | 41 | R |
| src/BlackFlag.Engine.Core/Objects/PdfStringKind.cs | 17 | R |
| src/BlackFlag.Engine.Core/Primitives/ByteRange.cs | 103 | R |
| src/BlackFlag.Engine.Document/Annotations/PdfAnnotation.cs | 100 | R |
| src/BlackFlag.Engine.Document/Annotations/PdfAnnotationOptions.cs | 18 | R |
| src/BlackFlag.Engine.Document/Annotations/PdfAnnotationRectangle.cs | 40 | R |
| src/BlackFlag.Engine.Document/Annotations/PdfAnnotationSubtype.cs | 32 | R |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecoration.cs | 80 | R |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecorationAlignment.cs | 14 | R |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecorationKind.cs | 11 | R |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecorationRegion.cs | 11 | R |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecorationTarget.cs | 69 | R |
| src/BlackFlag.Engine.Document/Decorations/PdfPageDecorations.cs | 47 | R |
| src/BlackFlag.Engine.Document/Decorations/PdfPageNumberFormat.cs | 14 | R |
| src/BlackFlag.Engine.Document/Decorations/PdfPageNumberingOptions.cs | 39 | R |
| src/BlackFlag.Engine.Document/Decorations/PdfPageSelectionParity.cs | 14 | R |
| src/BlackFlag.Engine.Document/Forms/PdfAcroForm.cs | 52 | R |
| src/BlackFlag.Engine.Document/Forms/PdfFormField.cs | 139 | R |
| src/BlackFlag.Engine.Document/Forms/PdfFormFieldOptions.cs | 15 | R |
| src/BlackFlag.Engine.Document/Forms/PdfFormFieldType.cs | 14 | R |
| src/BlackFlag.Engine.Document/Metadata/PdfDocumentInformation.cs | 99 | R |
| src/BlackFlag.Engine.Document/Metadata/PdfDocumentInformationField.cs | 16 | R |
| src/BlackFlag.Engine.Document/Model/IPdfDictionaryBackedModel.cs | 12 | R |
| src/BlackFlag.Engine.Document/Model/PdfCatalog.cs | 77 | R |
| src/BlackFlag.Engine.Document/Model/PdfDocument.cs | 91 | R |
| src/BlackFlag.Engine.Document/Model/PdfObjectBinding.cs | 38 | R |
| src/BlackFlag.Engine.Document/Model/PdfTrailer.cs | 116 | R |
| src/BlackFlag.Engine.Document/Model/PdfVersion.cs | 95 | R |
| src/BlackFlag.Engine.Document/Objects/PdfArray.cs | 118 | R |
| src/BlackFlag.Engine.Document/Objects/PdfDictionary.cs | 190 | R |
| src/BlackFlag.Engine.Document/Objects/PdfIndirectReference.cs | 99 | R |
| src/BlackFlag.Engine.Document/Objects/PdfNames.cs | 78 | R |
| src/BlackFlag.Engine.Document/Objects/PdfObject.cs | 17 | R |
| src/BlackFlag.Engine.Document/Objects/PdfObjectIdentifier.cs | 44 | R |
| src/BlackFlag.Engine.Document/Objects/PdfObjectKind.cs | 18 | R |
| src/BlackFlag.Engine.Document/Objects/PdfObjectStore.cs | 101 | R+ |
| src/BlackFlag.Engine.Document/Objects/PdfStream.cs | 61 | R |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfBoolean.cs | 55 | R |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfInteger.cs | 56 | R |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfName.cs | 99 | R |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfNull.cs | 23 | R |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfReal.cs | 65 | R |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfString.cs | 107 | R |
| src/BlackFlag.Engine.Document/Objects/Primitives/PdfStringFormat.cs | 10 | R |
| src/BlackFlag.Engine.Document/Outlines/PdfOutline.cs | 52 | R |
| src/BlackFlag.Engine.Document/Outlines/PdfOutlineDestination.cs | 4 | R |
| src/BlackFlag.Engine.Document/Outlines/PdfOutlineItem.cs | 83 | R |
| src/BlackFlag.Engine.Document/Outlines/PdfOutlineNavigationAction.cs | 25 | R |
| src/BlackFlag.Engine.Document/Outlines/PdfOutlineNavigationActionType.cs | 11 | R |
| src/BlackFlag.Engine.Document/Pages/PdfEffectivePage.cs | 33 | R |
| src/BlackFlag.Engine.Document/Pages/PdfEffectivePageCollection.cs | 29 | R |
| src/BlackFlag.Engine.Document/Pages/PdfEffectiveValue.cs | 10 | R |
| src/BlackFlag.Engine.Document/Pages/PdfPage.cs | 531 | R |
| src/BlackFlag.Engine.Document/Pages/PdfPageBox.cs | 35 | R |
| src/BlackFlag.Engine.Document/Pages/PdfPageBoxKind.cs | 11 | R |
| src/BlackFlag.Engine.Document/Pages/PdfPageGeometry.cs | 58 | R |
| src/BlackFlag.Engine.Document/Pages/PdfPageRotation.cs | 12 | R |
| src/BlackFlag.Engine.Document/Pages/PdfPageTree.cs | 209 | R |
| src/BlackFlag.Engine.Document/Pages/PdfPageValueOrigin.cs | 12 | R |
| src/BlackFlag.Engine.Document/Properties/AssemblyInfo.cs | 16 | M |
| src/BlackFlag.Engine.Editing/Annotations/PdfAnnotationMutationKind.cs | 12 | R |
| src/BlackFlag.Engine.Editing/Annotations/PdfAnnotationSnapshot.cs | 10 | R |
| src/BlackFlag.Engine.Editing/Annotations/PdfEditableAnnotation.cs | 41 | R |
| src/BlackFlag.Engine.Editing/Annotations/PdfEditableAnnotationCollection.cs | 100 | R |
| src/BlackFlag.Engine.Editing/Decorations/PdfEditablePageDecoration.cs | 30 | R |
| src/BlackFlag.Engine.Editing/Decorations/PdfEditablePageDecorationCollection.cs | 97 | R |
| src/BlackFlag.Engine.Editing/Decorations/PdfPageDecorationMutationKind.cs | 13 | R |
| src/BlackFlag.Engine.Editing/Decorations/PdfPageDecorationSnapshot.cs | 12 | R |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Annotations.cs | 222 | R |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Decorations.cs | 248 | R |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Forms.cs | 402 | R |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Metadata.cs | 94 | R |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Outlines.cs | 302 | R |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Pages.cs | 521 | R |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Text.cs | 267 | C |
| src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.cs | 149 | R |
| src/BlackFlag.Engine.Editing/Documents/PdfEditingPreservationMode.cs | 14 | R |
| src/BlackFlag.Engine.Editing/Documents/PdfEditingSession.cs | 431 | R |
| src/BlackFlag.Engine.Editing/Documents/PdfEditingSessionOptions.cs | 22 | R |
| src/BlackFlag.Engine.Editing/Forms/PdfEditableFormField.cs | 35 | R |
| src/BlackFlag.Engine.Editing/Forms/PdfEditableFormFieldCollection.cs | 135 | R |
| src/BlackFlag.Engine.Editing/Forms/PdfFormFieldMutationKind.cs | 19 | R |
| src/BlackFlag.Engine.Editing/Forms/PdfFormFieldSnapshot.cs | 14 | R |
| src/BlackFlag.Engine.Editing/Geometry/PdfPageGeometryMutationKind.cs | 9 | R |
| src/BlackFlag.Engine.Editing/Geometry/PdfPageResizeAnchor.cs | 8 | R |
| src/BlackFlag.Engine.Editing/History/PdfEditHistory.cs | 105 | R |
| src/BlackFlag.Engine.Editing/Metadata/PdfDocumentInformationMutationKind.cs | 8 | R |
| src/BlackFlag.Engine.Editing/Operations/IPdfEditOperation.cs | 19 | R |
| src/BlackFlag.Engine.Editing/Operations/IPdfStructuralEditOperation.cs | 21 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfAnnotationEditOperation.cs | 41 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfContentEditOperation.cs | 37 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfContentMutationKind.cs | 9 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfDocumentInformationEditOperation.cs | 41 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfEditScope.cs | 12 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfExistingTextEditOperation.cs | 49 | C |
| src/BlackFlag.Engine.Editing/Operations/PdfExistingTextResourceChangeOperation.cs | 56 | C |
| src/BlackFlag.Engine.Editing/Operations/PdfFormFieldEditOperation.cs | 44 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfOutlineEditOperation.cs | 37 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfPageDecorationEditOperation.cs | 34 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfPageEditOperation.cs | 75 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfPageGeometryEditOperation.cs | 38 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfPageMutationKind.cs | 13 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfResourceEditOperation.cs | 45 | R |
| src/BlackFlag.Engine.Editing/Operations/PdfResourceMutationKind.cs | 10 | R |
| src/BlackFlag.Engine.Editing/Outlines/PdfEditableOutlineItem.cs | 32 | R |
| src/BlackFlag.Engine.Editing/Outlines/PdfEditableOutlineItemCollection.cs | 134 | R |
| src/BlackFlag.Engine.Editing/Outlines/PdfOutlineItemSnapshot.cs | 12 | R |
| src/BlackFlag.Engine.Editing/Outlines/PdfOutlineMutationKind.cs | 15 | R |
| src/BlackFlag.Engine.Editing/Ownership/PdfEditingOwnershipException.cs | 13 | R |
| src/BlackFlag.Engine.Editing/Ownership/PdfEditingOwnershipToken.cs | 7 | R |
| src/BlackFlag.Engine.Editing/Pages/PdfEditablePage.cs | 195 | R |
| src/BlackFlag.Engine.Editing/Pages/PdfEditablePageCollection.cs | 304 | R |
| src/BlackFlag.Engine.Editing/Pages/PdfEditablePageOrigin.cs | 19 | R |
| src/BlackFlag.Engine.Editing/Preservation/PdfEditingPreservationContract.cs | 89 | R |
| src/BlackFlag.Engine.Editing/Properties/AssemblyInfo.cs | 7 | M |
| src/BlackFlag.Engine.Editing/Structural/PdfStructuralValueOrigin.cs | 19 | R |
| src/BlackFlag.Engine.Editing/Structural/PdfStructuralValueState.cs | 141 | R |
| src/BlackFlag.Engine.Editing/Structural/PdfStructureKind.cs | 26 | R |
| src/BlackFlag.Engine.Editing/Text/PdfExistingTextDescriptor.cs | 106 | C |
| src/BlackFlag.Engine.Editing/Text/PdfExistingTextEditability.cs | 38 | C |
| src/BlackFlag.Engine.Editing/Text/PdfExistingTextIdentity.cs | 102 | C |
| src/BlackFlag.Engine.Editing/Text/PdfExistingTextSourceKind.cs | 14 | C |
| src/BlackFlag.Engine.Editing/Text/PdfExistingTextTypography.cs | 56 | C |
| src/BlackFlag.Engine.Editing/Transactions/PdfEditChange.cs | 31 | R |
| src/BlackFlag.Engine.Editing/Transactions/PdfEditChangeSet.cs | 59 | R |
| src/BlackFlag.Engine.Editing/Transactions/PdfEditTransaction.cs | 103 | R |
| src/BlackFlag.Engine.Editing/Transactions/PdfEditTransactionState.cs | 11 | R |
| src/BlackFlag.Engine.Editing/Validation/IPdfEditingValidator.cs | 15 | R |
| src/BlackFlag.Engine.Editing/Validation/PdfEditingConsistencyValidator.cs | 144 | R |
| src/BlackFlag.Engine.Editing/Validation/PdfEditingValidationException.cs | 13 | R |
| src/BlackFlag.Engine.Filters/IPdfFilterDecodeBudget.cs | 33 | R |
| src/BlackFlag.Engine.Filters/IPdfFilterDecoder.cs | 31 | R |
| src/BlackFlag.Engine.Filters/IPdfFilterPipeline.cs | 28 | R |
| src/BlackFlag.Engine.Filters/IPdfFilterRegistry.cs | 30 | R |
| src/BlackFlag.Engine.Filters/PdfAscii85FilterDecoder.cs | 251 | R |
| src/BlackFlag.Engine.Filters/PdfAsciiHexFilterDecoder.cs | 143 | R |
| src/BlackFlag.Engine.Filters/PdfFilterByteClassifier.cs | 18 | R |
| src/BlackFlag.Engine.Filters/PdfFilterDecodeBudget.cs | 116 | R |
| src/BlackFlag.Engine.Filters/PdfFilterDecodeContext.cs | 45 | R |
| src/BlackFlag.Engine.Filters/PdfFilterDecodeException.cs | 43 | R |
| src/BlackFlag.Engine.Filters/PdfFilterDecodeFailure.cs | 42 | R |
| src/BlackFlag.Engine.Filters/PdfFilterDecodeLimits.cs | 94 | R+ |
| src/BlackFlag.Engine.Filters/PdfFilterPipeline.cs | 181 | R |
| src/BlackFlag.Engine.Filters/PdfFilterRegistrations.cs | 26 | R |
| src/BlackFlag.Engine.Filters/PdfFilterRegistry.cs | 80 | R |
| src/BlackFlag.Engine.Filters/PdfFlateDecodeParameters.cs | 172 | R |
| src/BlackFlag.Engine.Filters/PdfFlateFilterDecoder.cs | 156 | R |
| src/BlackFlag.Engine.Filters/PdfLzwBitReader.cs | 56 | R |
| src/BlackFlag.Engine.Filters/PdfLzwDecodeParameters.cs | 48 | R |
| src/BlackFlag.Engine.Filters/PdfLzwFilterDecoder.cs | 254 | R |
| src/BlackFlag.Engine.Filters/PdfPredictorDecoder.cs | 445 | R |
| src/BlackFlag.Engine.Filters/PdfRunLengthFilterDecoder.cs | 118 | R |
| src/BlackFlag.Engine.Filters/PooledByteAccumulator.cs | 177 | R |
| src/BlackFlag.Engine.Filters/PooledPdfFilterBufferWriter.cs | 161 | R |
| src/BlackFlag.Engine.Filters/Properties/AssemblyInfo.cs | 3 | M |
| src/BlackFlag.Engine.Fonts.Platform/MacOS/PdfMacOSExternalFontProvider.cs | 55 | C |
| src/BlackFlag.Engine.Fonts.Platform/PdfFileSystemExternalFontProvider.cs | 301 | C |
| src/BlackFlag.Engine.Fonts.Platform/PdfPlatformExternalFontProviders.cs | 18 | C |
| src/BlackFlag.Engine.Fonts.Platform/Windows/PdfWindowsExternalFontProvider.cs | 62 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfExternalTrueTypeInspector.cs | 498 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfExternalTrueTypeMetrics.cs | 40 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeBinary.cs | 191 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeCmap.cs | 432 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeCompositeClosure.cs | 311 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeGlyphMetricsReader.cs | 120 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeGlyphSubsetter.cs | 193 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeSfntBuilder.cs | 200 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeSubsetOptions.cs | 52 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeSubsetResult.cs | 20 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfUnicodeTrueTypeGlyphSubsetter.cs | 246 | C |
| src/BlackFlag.Engine.Fonts/Embedding/PdfUnicodeTrueTypeSubsetResult.cs | 73 | C |
| src/BlackFlag.Engine.Fonts/IPdfExternalFontProvider.cs | 29 | C |
| src/BlackFlag.Engine.Fonts/PdfExternalFontCatalog.cs | 223 | C |
| src/BlackFlag.Engine.Fonts/PdfExternalFontEmbeddingPolicy.cs | 38 | C |
| src/BlackFlag.Engine.Fonts/PdfExternalFontEmbeddingRights.cs | 10 | C |
| src/BlackFlag.Engine.Fonts/PdfExternalFontFace.cs | 76 | C |
| src/BlackFlag.Engine.Fonts/PdfExternalFontMatch.cs | 43 | C |
| src/BlackFlag.Engine.Fonts/PdfExternalFontMatchRequest.cs | 42 | C |
| src/BlackFlag.Engine.Fonts/PdfExternalFontSlant.cs | 9 | C |
| src/BlackFlag.Engine.Fonts/PdfExternalFontTechnology.cs | 11 | C |
| src/BlackFlag.Engine.Fonts/PdfSanitizedExternalFontMetadata.cs | 43 | C |
| src/BlackFlag.Engine.Fonts/PdfSanitizedExternalFontMetadataInspector.cs | 413 | C |
| src/BlackFlag.Engine.Fonts/Sanitization/IPdfExternalFontSanitizer.cs | 13 | C |
| src/BlackFlag.Engine.Fonts/Sanitization/PdfExternalFontSanitizationException.cs | 15 | C |
| src/BlackFlag.Engine.Fonts/Sanitization/PdfExternalFontSanitizationFailure.cs | 18 | C |
| src/BlackFlag.Engine.Fonts/Sanitization/PdfExternalFontSanitizationOptions.cs | 30 | C |
| src/BlackFlag.Engine.Fonts/Sanitization/PdfOpenTypeFontSanitizer.cs | 930 | C |
| src/BlackFlag.Engine.Fonts/Sanitization/PdfSanitizedExternalFontProgram.cs | 32 | C |
| src/BlackFlag.Engine.IO/RandomAccess/BufferedRandomAccessCursor.cs | 376 | R |
| src/BlackFlag.Engine.IO/RandomAccess/IRandomAccessCursor.cs | 58 | R |
| src/BlackFlag.Engine.IO/RandomAccess/RandomAccessCursor.cs | 208 | R |
| src/BlackFlag.Engine.IO/RandomAccess/RandomAccessCursorExtensions.cs | 59 | R |
| src/BlackFlag.Engine.IO/RandomAccess/RandomAccessDefaults.cs | 12 | R |
| src/BlackFlag.Engine.IO/Sources/FileRandomAccessSource.cs | 147 | R |
| src/BlackFlag.Engine.Objects/GlobalSuppressions.BF-POM-0005.cs | 8 | R |
| src/BlackFlag.Engine.Objects/PdfArray.cs | 168 | R |
| src/BlackFlag.Engine.Objects/PdfBoolean.cs | 54 | R |
| src/BlackFlag.Engine.Objects/PdfDictionary.cs | 259 | R |
| src/BlackFlag.Engine.Objects/PdfHexString.cs | 67 | R |
| src/BlackFlag.Engine.Objects/PdfIndirectObject.cs | 124 | R |
| src/BlackFlag.Engine.Objects/PdfIndirectReference.cs | 118 | R |
| src/BlackFlag.Engine.Objects/PdfInteger.cs | 54 | R |
| src/BlackFlag.Engine.Objects/PdfLiteralString.cs | 61 | R |
| src/BlackFlag.Engine.Objects/PdfName.cs | 59 | R |
| src/BlackFlag.Engine.Objects/PdfNull.cs | 46 | R |
| src/BlackFlag.Engine.Objects/PdfObject.cs | 24 | R |
| src/BlackFlag.Engine.Objects/PdfObjectType.cs | 40 | R |
| src/BlackFlag.Engine.Objects/PdfObjectVisitor.cs | 136 | R |
| src/BlackFlag.Engine.Objects/PdfReal.cs | 54 | R |
| src/BlackFlag.Engine.Objects/PdfStream.cs | 109 | R |
| src/BlackFlag.Engine.Parsing/ContentStreams/MemoryRandomAccessSource.cs | 30 | R |
| src/BlackFlag.Engine.Parsing/ContentStreams/PdfContentParseException.cs | 28 | R |
| src/BlackFlag.Engine.Parsing/ContentStreams/PdfContentParseFailure.cs | 14 | R |
| src/BlackFlag.Engine.Parsing/ContentStreams/PdfContentParseOptions.cs | 45 | R |
| src/BlackFlag.Engine.Parsing/ContentStreams/PdfContentStreamParser.cs | 385 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReference.cs | 89 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceChain.cs | 90 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceChainBuilder.cs | 37 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceEntry.cs | 52 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceParser.Bytes.cs | 216 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceParser.StartXRef.cs | 149 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceParser.Table.cs | 220 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceParser.Trailer.cs | 42 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceParser.cs | 119 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceSection.cs | 72 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceStreamEntry.cs | 209 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceStreamEntryKind.cs | 23 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceStreamParser.cs | 573 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceStreamSection.cs | 68 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfCrossReferenceTable.cs | 89 | R |
| src/BlackFlag.Engine.Parsing/CrossReferences/PdfObjectId.cs | 56 | R |
| src/BlackFlag.Engine.Parsing/Documents/PdfEffectivePageTreeLoader.cs | 273 | R |
| src/BlackFlag.Engine.Parsing/Documents/PdfPageTreeLoadException.cs | 15 | R |
| src/BlackFlag.Engine.Parsing/Documents/PdfPageTreeLoadOptions.cs | 18 | R |
| src/BlackFlag.Engine.Parsing/Loading/IPdfIndirectObjectResolver.cs | 16 | R |
| src/BlackFlag.Engine.Parsing/Loading/PdfIndirectObjectResolver.cs | 250 | R |
| src/BlackFlag.Engine.Parsing/Loading/PdfObjectLocation.cs | 112 | R |
| src/BlackFlag.Engine.Parsing/Loading/PdfObjectLocationKind.cs | 16 | R |
| src/BlackFlag.Engine.Parsing/Loading/PdfObjectLocationTable.cs | 69 | R |
| src/BlackFlag.Engine.Parsing/Loading/PdfObjectResolutionException.cs | 43 | R |
| src/BlackFlag.Engine.Parsing/Loading/PdfObjectResolutionFailure.cs | 15 | R |
| src/BlackFlag.Engine.Parsing/ObjectStreams/PdfCompressedObjectEntry.cs | 48 | R |
| src/BlackFlag.Engine.Parsing/ObjectStreams/PdfCompressedObjects.cs | 246 | R |
| src/BlackFlag.Engine.Parsing/ObjectStreams/PdfObjectStreamParser.cs | 423 | R |
| src/BlackFlag.Engine.Parsing/ObjectStreams/ReadOnlyMemoryRandomAccessSource.cs | 78 | R |
| src/BlackFlag.Engine.Parsing/ParsingAssemblyMarker.cs | 7 | R |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Arrays.cs | 39 | R |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Atoms.cs | 65 | R |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Dictionaries.cs | 52 | R |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Helpers.cs | 122 | R |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.IndirectObjects.cs | 80 | R |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.IndirectReferences.cs | 71 | R |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Numeric.cs | 63 | R |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.Strings.cs | 233 | R |
| src/BlackFlag.Engine.Parsing/PdfObjectParser.cs | 139 | R |
| src/BlackFlag.Engine.Parsing/Properties/AssemblyInfo.cs | 4 | M |
| src/BlackFlag.Engine.Parsing/Recovery/PdfCrossReferenceRecoveryResult.cs | 57 | R |
| src/BlackFlag.Engine.Parsing/Recovery/PdfCrossReferenceRecoveryService.cs | 546 | R+ |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveredEncryptionState.cs | 15 | R |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveredObjectCandidate.cs | 12 | R |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveryConfidence.cs | 15 | R |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveryDiagnostic.cs | 11 | R |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveryLimits.cs | 30 | R |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveryMode.cs | 15 | R |
| src/BlackFlag.Engine.Parsing/Recovery/PdfRecoveryOptions.cs | 22 | R |
| src/BlackFlag.Engine.Parsing/Rendering/PdfNestedGraphicsProvider.cs | 377 | R |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolutionException.cs | 34 | R |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolutionFailure.cs | 34 | R |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolutionOptions.cs | 35 | R |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.ColorSpaces.cs | 177 | R |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.Fonts.cs | 168 | R |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.GraphicsResources.cs | 74 | R |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.Helpers.cs | 47 | R |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.Values.cs | 195 | R |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.XObjects.cs | 126 | R |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.cs | 226 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfEncryptionSecurityAssessment.cs | 26 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfEncryptionSecurityHandler.cs | 232 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfEncryptionSecurityState.cs | 14 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfPasswordAuthenticationKind.cs | 14 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalysisOptions.cs | 22 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalysisResult.cs | 22 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.AnalysisState.cs | 763 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.cs | 93 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticationResult.cs | 27 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.Aes256.cs | 208 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.Configuration.cs | 215 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.Legacy.cs | 301 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityAuthenticator.cs | 188 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityCryptography.cs | 208 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecurityPermissions.cs | 44 | R |
| src/BlackFlag.Engine.Parsing/Security/PdfStandardSecuritySession.cs | 182 | R |
| src/BlackFlag.Engine.Parsing/Streams/PdfStreamParser.Data.cs | 75 | R |
| src/BlackFlag.Engine.Parsing/Streams/PdfStreamParser.Header.cs | 44 | R |
| src/BlackFlag.Engine.Parsing/Streams/PdfStreamParser.Terminator.cs | 172 | R |
| src/BlackFlag.Engine.Parsing/Streams/PdfStreamParser.cs | 93 | R |
| src/BlackFlag.Engine.Plugins/Activation/PluginActivationIssue.cs | 3 | R |
| src/BlackFlag.Engine.Plugins/Activation/PluginActivationIssueKind.cs | 12 | R |
| src/BlackFlag.Engine.Plugins/Activation/PluginActivationPlan.cs | 22 | R |
| src/BlackFlag.Engine.Plugins/Activation/PluginActivationPlanner.cs | 123 | R |
| src/BlackFlag.Engine.Plugins/Activation/PluginActivationPlanningOptions.cs | 15 | R |
| src/BlackFlag.Engine.Plugins/Capabilities/PluginCapabilityAccess.cs | 29 | R |
| src/BlackFlag.Engine.Plugins/Capabilities/PluginCapabilityRegistry.cs | 31 | R |
| src/BlackFlag.Engine.Plugins/Discovery/PluginCatalog.cs | 25 | R |
| src/BlackFlag.Engine.Plugins/Discovery/PluginDescriptor.cs | 6 | R |
| src/BlackFlag.Engine.Plugins/Discovery/PluginDiscoveryIssue.cs | 4 | R |
| src/BlackFlag.Engine.Plugins/Discovery/PluginDiscoveryIssueKind.cs | 11 | R |
| src/BlackFlag.Engine.Plugins/Discovery/PluginDiscoveryOptions.cs | 27 | R |
| src/BlackFlag.Engine.Plugins/Discovery/PluginDiscoveryService.cs | 105 | R |
| src/BlackFlag.Engine.Plugins/Extensions/PluginExtensionRegistry.cs | 61 | R |
| src/BlackFlag.Engine.Plugins/Extensions/PluginExtensionSecurityRegistry.cs | 19 | R |
| src/BlackFlag.Engine.Plugins/Extensions/PluginExtensionTransaction.cs | 32 | R |
| src/BlackFlag.Engine.Plugins/Runtime/PluginEntryPoint.cs | 24 | R |
| src/BlackFlag.Engine.Plugins/Runtime/PluginLoadContext.cs | 66 | R+ |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeEnvironment.cs | 19 | R |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeHandle.cs | 16 | R |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeHost.cs | 101 | R+ |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeIssue.cs | 4 | R |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeIssueKind.cs | 16 | R |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeOptions.cs | 12 | R |
| src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeSession.cs | 44 | R |
| src/BlackFlag.Engine.Plugins/Security/PluginSecurityAuthorization.cs | 14 | R |
| src/BlackFlag.Engine.Plugins/Security/PluginSecurityPolicyDeniedException.cs | 6 | R |
| src/BlackFlag.Engine.Rendering.Skia/Fonts/SkiaFontRealizer.cs | 81 | R |
| src/BlackFlag.Engine.Rendering.Skia/Images/SkiaDctImageCodec.cs | 63 | R+ |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Helpers.cs | 92 | R |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Images.cs | 104 | R |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Paths.cs | 158 | R |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Shadings.cs | 199 | R |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.Text.cs | 307 | R |
| src/BlackFlag.Engine.Rendering.Skia/SkiaPdfPageRenderer.cs | 214 | R |
| src/BlackFlag.Engine.Rendering/Color/IPdfColorConverter.cs | 8 | R |
| src/BlackFlag.Engine.Rendering/Color/PdfBlendMode.cs | 3 | R |
| src/BlackFlag.Engine.Rendering/Color/PdfBlendModeParser.cs | 17 | R |
| src/BlackFlag.Engine.Rendering/Color/PdfColorConverter.cs | 80 | R |
| src/BlackFlag.Engine.Rendering/Color/PdfColorTarget.cs | 7 | R |
| src/BlackFlag.Engine.Rendering/Color/PdfSrgbColor.cs | 7 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfClipKind.cs | 11 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfClipState.cs | 68 | C |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfColorValue.cs | 40 | C |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDashPattern.cs | 28 | C |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayBounds.cs | 54 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayForm.cs | 34 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayImage.cs | 60 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayItem.cs | 33 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayList.cs | 28 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayMarkedContentPoint.cs | 34 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayPath.cs | 37 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayShading.cs | 23 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayTextRun.cs | 107 | C |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfFillRule.cs | 11 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfGraphicsStateSnapshot.cs | 100 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfLineCapStyle.cs | 14 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfLineJoinStyle.cs | 14 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfMarkedContentContext.cs | 38 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfMatrix.cs | 64 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfPathGeometry.cs | 68 | C |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfPathSegment.cs | 30 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfPathSegmentKind.cs | 17 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfPoint.cs | 18 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfTextRunElement.cs | 44 | R |
| src/BlackFlag.Engine.Rendering/DisplayList/PdfTextRunElementKind.cs | 11 | R |
| src/BlackFlag.Engine.Rendering/Fonts/PdfCMap.cs | 85 | C |
| src/BlackFlag.Engine.Rendering/Fonts/PdfCMapParser.cs | 283 | R |
| src/BlackFlag.Engine.Rendering/Fonts/PdfCharacterCode.cs | 45 | R |
| src/BlackFlag.Engine.Rendering/Fonts/PdfCodeSpaceRange.cs | 48 | R |
| src/BlackFlag.Engine.Rendering/Fonts/PdfDecodedGlyph.cs | 47 | R |
| src/BlackFlag.Engine.Rendering/Fonts/PdfDecodedTextRun.cs | 36 | C |
| src/BlackFlag.Engine.Rendering/Fonts/PdfFontDecodeDiagnostic.cs | 4 | R |
| src/BlackFlag.Engine.Rendering/Fonts/PdfFontDecoder.cs | 445 | R |
| src/BlackFlag.Engine.Rendering/Fonts/PdfFontStreamDecoder.cs | 106 | R |
| src/BlackFlag.Engine.Rendering/Fonts/PdfGlyphNameUnicodeMap.cs | 123 | R |
| src/BlackFlag.Engine.Rendering/Fonts/PdfSameFontTextEncoder.cs | 310 | C |
| src/BlackFlag.Engine.Rendering/Fonts/PdfWinAnsiEncoding.cs | 46 | R |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluationLimits.cs | 34 | R |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.CalculatorMachine.cs | 378 | R |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.CalculatorParser.cs | 157 | R |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.CalculatorTokens.cs | 60 | R |
| src/BlackFlag.Engine.Rendering/Functions/PdfFunctionEvaluator.cs | 707 | R |
| src/BlackFlag.Engine.Rendering/Graphics/PdfContentSourceSpan.cs | 29 | R |
| src/BlackFlag.Engine.Rendering/Graphics/PdfGraphicsOperation.cs | 38 | R |
| src/BlackFlag.Engine.Rendering/Graphics/PdfGraphicsOperator.cs | 94 | R |
| src/BlackFlag.Engine.Rendering/Graphics/PdfInlineImage.cs | 23 | R |
| src/BlackFlag.Engine.Rendering/Images/IPdfImageCodec.cs | 16 | R+ |
| src/BlackFlag.Engine.Rendering/Images/PdfDecodedImage.cs | 35 | R |
| src/BlackFlag.Engine.Rendering/Images/PdfImageCompatibilityConverter.cs | 126 | R |
| src/BlackFlag.Engine.Rendering/Images/PdfImageCompatibilityDecision.cs | 11 | R |
| src/BlackFlag.Engine.Rendering/Images/PdfImageCompatibilityTarget.cs | 16 | R |
| src/BlackFlag.Engine.Rendering/Images/PdfImageConversionPolicy.cs | 11 | R |
| src/BlackFlag.Engine.Rendering/Images/PdfImageDecodeDiagnostic.cs | 4 | R |
| src/BlackFlag.Engine.Rendering/Images/PdfImageDecodeLimits.cs | 25 | R |
| src/BlackFlag.Engine.Rendering/Images/PdfImageDecodeResult.cs | 6 | R |
| src/BlackFlag.Engine.Rendering/Images/PdfImageDecoder.cs | 483 | R+ |
| src/BlackFlag.Engine.Rendering/Images/PdfImageFormatRecognizer.cs | 76 | R |
| src/BlackFlag.Engine.Rendering/Images/PdfImageSourceFormat.cs | 19 | R |
| src/BlackFlag.Engine.Rendering/Images/PdfImageSourceTraits.cs | 24 | R |
| src/BlackFlag.Engine.Rendering/Interpretation/IPdfFormOperationProvider.cs | 16 | R |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpretationException.cs | 25 | R |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpretationFailure.cs | 32 | R |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpretationOptions.cs | 40 | R |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Execution.cs | 937 | R |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.GraphicsState.cs | 243 | R |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Operands.cs | 249 | R |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Paths.cs | 126 | R |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Text.cs | 329 | R |
| src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.cs | 47 | R |
| src/BlackFlag.Engine.Rendering/Nested/IPdfNestedGraphicsProvider.cs | 23 | R |
| src/BlackFlag.Engine.Rendering/Nested/PdfTilingPatternCell.cs | 14 | R |
| src/BlackFlag.Engine.Rendering/Nested/PdfType3GlyphProgram.cs | 10 | R |
| src/BlackFlag.Engine.Rendering/Patterns/PdfPatternTilePlan.cs | 10 | R |
| src/BlackFlag.Engine.Rendering/Patterns/PdfPatternTilePlanner.cs | 139 | R |
| src/BlackFlag.Engine.Rendering/Properties/AssemblyInfo.cs | 6 | M |
| src/BlackFlag.Engine.Rendering/Raster/IPdfPageRenderer.cs | 13 | R |
| src/BlackFlag.Engine.Rendering/Raster/PdfPixelFormat.cs | 8 | R |
| src/BlackFlag.Engine.Rendering/Raster/PdfRasterSurface.cs | 50 | R |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderDiagnostic.cs | 7 | R |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderLimits.cs | 61 | R |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderMode.cs | 11 | R |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderQuality.cs | 14 | R |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderRegion.cs | 30 | R |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderRequest.cs | 40 | R |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderResult.cs | 24 | R |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderWorkBudget.cs | 39 | R |
| src/BlackFlag.Engine.Rendering/Raster/PdfRenderingDiagnosticCodes.cs | 15 | R |
| src/BlackFlag.Engine.Rendering/RenderingAssemblyMarker.cs | 7 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfColorSpaceKind.cs | 43 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfFontEmbeddingRights.cs | 19 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfFontSlant.cs | 16 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfFontTechnology.cs | 28 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfFontVariationAxis.cs | 51 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedColorSpace.cs | 46 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedExtGState.cs | 55 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedFont.cs | 114 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedFormXObject.cs | 42 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedImageXObject.cs | 66 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedPattern.cs | 21 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedProperties.cs | 20 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedResource.cs | 33 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedShading.cs | 31 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResolvedXObject.cs | 21 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResourceIdentity.cs | 66 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResourceKind.cs | 28 | R |
| src/BlackFlag.Engine.Rendering/Resources/PdfResourceScope.cs | 93 | R |
| src/BlackFlag.Engine.Rendering/Shadings/PdfGradientStop.cs | 7 | R |
| src/BlackFlag.Engine.Rendering/Shadings/PdfShadingDecoder.cs | 150 | R |
| src/BlackFlag.Engine.Rendering/Shadings/PdfShadingDescriptor.cs | 71 | C |
| src/BlackFlag.Engine.SDK.Adapters/Editing/PdfEditingSdkSession.Text.cs | 496 | C |
| src/BlackFlag.Engine.SDK.Adapters/Editing/PdfEditingSdkSession.cs | 553 | C |
| src/BlackFlag.Engine.SDK.Adapters/Security/DocumentSecuritySdkService.cs | 76 | R |
| src/BlackFlag.Engine.SDK/Editing/IPdfEditingSdkSession.cs | 89 | R |
| src/BlackFlag.Engine.SDK/Editing/IPdfExistingTextSdkSession.cs | 48 | C |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkChangeSet.cs | 8 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkDocumentInformationField.cs | 14 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkOperation.cs | 10 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPage.cs | 14 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPageBox.cs | 8 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPageBoxKind.cs | 11 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPageBoxState.cs | 6 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPageResizeAnchor.cs | 8 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPageRotation.cs | 12 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkPreservationMode.cs | 13 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkStructuralValueOrigin.cs | 10 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfEditingSdkStructureSummary.cs | 12 | R |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkEditability.cs | 15 | C |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkResults.cs | 64 | C |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkSourceKind.cs | 8 | C |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkStyle.cs | 131 | C |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkTarget.cs | 51 | C |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkTypography.cs | 10 | C |
| src/BlackFlag.Engine.SDK/Editing/PdfExistingTextSdkTypographyUpdate.cs | 12 | C |
| src/BlackFlag.Engine.SDK/Plugins/Capabilities/IPluginCapability.cs | 4 | R |
| src/BlackFlag.Engine.SDK/Plugins/Capabilities/IPluginCapabilityAccess.cs | 7 | R |
| src/BlackFlag.Engine.SDK/Plugins/Capabilities/PluginCapabilityKey.cs | 14 | R |
| src/BlackFlag.Engine.SDK/Plugins/Extensions/IPluginExtension.cs | 4 | R |
| src/BlackFlag.Engine.SDK/Plugins/Extensions/IPluginExtensionRegistration.cs | 7 | R |
| src/BlackFlag.Engine.SDK/Plugins/Extensions/PluginExtensionPoint.cs | 14 | R |
| src/BlackFlag.Engine.SDK/Plugins/IBlackFlagPlugin.cs | 8 | R |
| src/BlackFlag.Engine.SDK/Plugins/PluginCapability.cs | 17 | R |
| src/BlackFlag.Engine.SDK/Plugins/PluginCompatibility.cs | 13 | R |
| src/BlackFlag.Engine.SDK/Plugins/PluginDependency.cs | 27 | R |
| src/BlackFlag.Engine.SDK/Plugins/PluginId.cs | 33 | R |
| src/BlackFlag.Engine.SDK/Plugins/PluginInitializationContext.cs | 20 | R |
| src/BlackFlag.Engine.SDK/Plugins/PluginManifest.cs | 67 | R |
| src/BlackFlag.Engine.SDK/Plugins/PluginManifestException.cs | 8 | R |
| src/BlackFlag.Engine.SDK/Plugins/PluginManifestLimits.cs | 19 | R |
| src/BlackFlag.Engine.SDK/Plugins/PluginManifestReader.cs | 100 | R |
| src/BlackFlag.Engine.SDK/Plugins/PluginVersionComparison.cs | 24 | R |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkFinding.cs | 10 | R |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkObservation.cs | 35 | R |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkObservationCategory.cs | 16 | R |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkOperation.cs | 10 | R |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkResult.cs | 48 | R |
| src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkSourceLocation.cs | 10 | R |
| src/BlackFlag.Engine.SDK/Security/IDocumentSecuritySdkService.cs | 11 | R |
| src/BlackFlag.Engine.Security/Containers/DocumentContainerEntry.cs | 28 | R |
| src/BlackFlag.Engine.Security/Containers/DocumentContainerSecurityInspection.cs | 28 | R |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedInspectionEngine.cs | 252 | R |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedInspectionResult.cs | 18 | R |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedObject.cs | 28 | R |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedObjectClassification.cs | 26 | R |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedObjectClassifier.cs | 382 | R |
| src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedObjectKind.cs | 17 | R |
| src/BlackFlag.Engine.Security/Containers/IDocumentContainerAdapter.cs | 14 | R |
| src/BlackFlag.Engine.Security/Containers/IDocumentContainerSecurityAdapter.cs | 17 | R |
| src/BlackFlag.Engine.Security/Containers/IDocumentEmbeddedObjectClassifier.cs | 7 | R |
| src/BlackFlag.Engine.Security/Findings/DocumentSecurityFinding.cs | 13 | R |
| src/BlackFlag.Engine.Security/Formats/DocumentSecurityFormatAdapterFactory.cs | 29 | R |
| src/BlackFlag.Engine.Security/Formats/Markdown/MarkdownContainerSecurityAdapter.cs | 220 | R |
| src/BlackFlag.Engine.Security/Formats/Markdown/MarkdownSecurityAnalysisOptions.cs | 12 | R |
| src/BlackFlag.Engine.Security/Formats/Markdown/MarkdownSecurityAnalysisResult.cs | 19 | R |
| src/BlackFlag.Engine.Security/Formats/Markdown/MarkdownSecurityAnalyzer.cs | 53 | R |
| src/BlackFlag.Engine.Security/Formats/Ole/LegacyWordBinarySecurityAnalysisResult.cs | 19 | R |
| src/BlackFlag.Engine.Security/Formats/Ole/LegacyWordBinarySecurityAnalyzer.cs | 55 | R |
| src/BlackFlag.Engine.Security/Formats/Ole/OleCompoundFileSecurityAdapter.cs | 732 | R |
| src/BlackFlag.Engine.Security/Formats/Ole/OleCompoundFileSecurityAnalysisOptions.cs | 20 | R |
| src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordContainerSecurityAdapter.cs | 796 | R |
| src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordSecurityAnalysisOptions.cs | 19 | R |
| src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordSecurityAnalysisResult.cs | 19 | R |
| src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordSecurityAnalyzer.cs | 80 | R |
| src/BlackFlag.Engine.Security/Formats/OpenDocument/OpenDocumentTextContainerSecurityAdapter.cs | 433 | R |
| src/BlackFlag.Engine.Security/Formats/OpenDocument/OpenDocumentTextSecurityAnalysisOptions.cs | 17 | R |
| src/BlackFlag.Engine.Security/Formats/OpenDocument/OpenDocumentTextSecurityAnalysisResult.cs | 19 | R |
| src/BlackFlag.Engine.Security/Formats/OpenDocument/OpenDocumentTextSecurityAnalyzer.cs | 58 | R |
| src/BlackFlag.Engine.Security/Formats/Rtf/RtfContainerSecurityAdapter.cs | 317 | R |
| src/BlackFlag.Engine.Security/Formats/Rtf/RtfSecurityAnalysisOptions.cs | 17 | R |
| src/BlackFlag.Engine.Security/Formats/Rtf/RtfSecurityAnalysisResult.cs | 18 | R |
| src/BlackFlag.Engine.Security/Formats/Rtf/RtfSecurityAnalyzer.cs | 51 | R |
| src/BlackFlag.Engine.Security/Inspection/DocumentSecurityAssessment.cs | 54 | R |
| src/BlackFlag.Engine.Security/Inspection/DocumentSecurityAssessmentEngine.cs | 78 | R |
| src/BlackFlag.Engine.Security/Inspection/DocumentSecurityInspectionBudget.cs | 200 | R |
| src/BlackFlag.Engine.Security/Inspection/DocumentSecurityInspectionLimits.cs | 41 | R |
| src/BlackFlag.Engine.Security/Model/DocumentSecurityObservation.cs | 58 | R |
| src/BlackFlag.Engine.Security/Model/DocumentSecurityObservationCategory.cs | 39 | R |
| src/BlackFlag.Engine.Security/Model/DocumentSecuritySourceLocation.cs | 21 | R |
| src/BlackFlag.Engine.Security/Plugins/BlackFlagPluginSecurityPolicy.cs | 31 | R |
| src/BlackFlag.Engine.Security/Plugins/IPluginSecurityPolicy.cs | 7 | R |
| src/BlackFlag.Engine.Security/Plugins/PluginSecurityDecision.cs | 8 | R |
| src/BlackFlag.Engine.Security/Plugins/PluginSecurityGrant.cs | 17 | R |
| src/BlackFlag.Engine.Security/Plugins/PluginSecurityOperation.cs | 8 | R |
| src/BlackFlag.Engine.Security/Plugins/PluginSecurityRequest.cs | 21 | R |
| src/BlackFlag.Engine.Security/Plugins/PluginSecuritySensitivity.cs | 8 | R |
| src/BlackFlag.Engine.Security/Policy/BlackFlagDocumentSecurityPolicy.cs | 155 | R |
| src/BlackFlag.Engine.Security/Policy/DocumentSecurityInteraction.cs | 24 | R |
| src/BlackFlag.Engine.Security/Policy/DocumentSecurityOperation.cs | 18 | R |
| src/BlackFlag.Engine.Security/Policy/DocumentSecurityPolicyAction.cs | 21 | R |
| src/BlackFlag.Engine.Security/Policy/DocumentSecurityPolicyDecision.cs | 17 | R |
| src/BlackFlag.Engine.Security/Policy/DocumentSecuritySeverity.cs | 18 | R |
| src/BlackFlag.Engine.Security/Policy/IDocumentSecurityPolicy.cs | 15 | R |
| src/BlackFlag.Engine.Security/Remediation/DocumentSecurityRemediationAction.cs | 8 | R |
| src/BlackFlag.Engine.Security/Remediation/DocumentSecurityRemediationIntent.cs | 24 | R |
| src/BlackFlag.Engine.Security/Remediation/DocumentSecurityRemediationPlan.cs | 30 | R |
| src/BlackFlag.Engine.Security/Remediation/DocumentSecurityRemediationPlanner.cs | 90 | R |
| src/BlackFlag.Engine.Security/Remediation/PassiveConversionSecurityResult.cs | 15 | R |
| src/BlackFlag.Engine.Security/Remediation/PassiveConversionSecurityService.cs | 50 | R |
| src/BlackFlag.Engine.Security/Reporting/DocumentSecurityReport.cs | 59 | R |
| src/BlackFlag.Engine.Security/Reporting/DocumentSecurityReportFactory.cs | 30 | R |
| src/BlackFlag.Engine.Security/Reporting/DocumentSecurityTextReportWriter.cs | 111 | R |
| src/BlackFlag.Engine.Services/Editing/PdfAnnotationCommonStateWritingService.cs | 213 | R |
| src/BlackFlag.Engine.Services/Editing/PdfAnnotationStructuralWritingService.cs | 297 | R |
| src/BlackFlag.Engine.Services/Editing/PdfAnnotationTransactionWritingService.cs | 354 | R |
| src/BlackFlag.Engine.Services/Editing/PdfEditingSaveService.cs | 243 | C |
| src/BlackFlag.Engine.Services/Editing/PdfEditingService.cs | 28 | R |
| src/BlackFlag.Engine.Services/Editing/PdfNestedPageTransactionWritingService.cs | 363 | R |
| src/BlackFlag.Engine.Services/Editing/PdfNestedPageTreeOperationReplay.cs | 294 | R |
| src/BlackFlag.Engine.Services/Editing/PdfNestedPageTreeStructuralWritingService.cs | 312 | R |
| src/BlackFlag.Engine.Services/Editing/PdfPageGeometryWritingService.cs | 130 | R |
| src/BlackFlag.Engine.Services/Editing/PdfPageRotationWritingService.cs | 129 | R |
| src/BlackFlag.Engine.Services/Editing/PdfPageStateWritingService.cs | 156 | R |
| src/BlackFlag.Engine.Services/Editing/PdfPageStructuralWritingService.cs | 218 | R |
| src/BlackFlag.Engine.Services/Editing/PdfPageTransactionWritingService.cs | 304 | R |
| src/BlackFlag.Engine.Services/Fonts/PdfCidToUnicodeCMapBuilder.cs | 105 | C |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalFontEmbeddingWritingService.cs | 208 | C |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalFontResourceProjection.cs | 37 | C |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeResourceProjector.cs | 249 | C |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeSubsetResourceProjector.cs | 306 | C |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalType0TrueTypeResourceProjection.cs | 47 | C |
| src/BlackFlag.Engine.Services/Fonts/PdfExternalType0TrueTypeResourceProjector.cs | 411 | C |
| src/BlackFlag.Engine.Services/Fonts/PdfToUnicodeCMapBuilder.cs | 98 | C |
| src/BlackFlag.Engine.Services/Pages/PdfPageService.cs | 101 | R |
| src/BlackFlag.Engine.Services/Properties/AssemblyInfo.cs | 3 | M |
| src/BlackFlag.Engine.Services/Resources/PdfPageResourceMutationProjection.cs | 24 | C |
| src/BlackFlag.Engine.Services/Resources/PdfPageResourceMutationProjector.cs | 357 | C |
| src/BlackFlag.Engine.Services/Resources/PdfResourceMutationOwnership.cs | 10 | C |
| src/BlackFlag.Engine.Services/Security/DocumentSecurityService.cs | 55 | R |
| src/BlackFlag.Engine.Services/Security/DocumentSecurityServiceResult.cs | 12 | R |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustDecision.cs | 12 | R |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustEvaluation.cs | 12 | R |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustEvaluator.cs | 148 | R |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustEvidence.cs | 10 | R |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustRecord.cs | 17 | R |
| src/BlackFlag.Engine.Services/Security/Trust/DocumentTrustRecordFactory.cs | 29 | R |
| src/BlackFlag.Engine.Services/Text/PdfCompoundTextOperatorNormalizer.cs | 142 | C |
| src/BlackFlag.Engine.Services/Text/PdfContentStreamMutationCodec.cs | 323 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingFontCandidate.cs | 40 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingFontSelectionService.cs | 80 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextColor.cs | 73 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextDecoration.cs | 96 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextDecorationKind.cs | 7 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextDiscoveryService.cs | 341 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextDiscoverySource.cs | 187 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextEditingService.cs | 89 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextFindReplaceResult.cs | 38 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextFindReplaceService.cs | 565 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextFontService.cs | 325 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextReplacementService.cs | 266 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextScriptStyle.cs | 8 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextSearchMatch.cs | 54 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextSearchScope.cs | 28 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleService.cs | 253 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleTokenEditor.cs | 334 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleUpdate.cs | 67 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextTransactionWritingService.cs | 133 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextTypographyService.cs | 251 | C |
| src/BlackFlag.Engine.Services/Text/PdfExistingTextTypographyUpdate.cs | 60 | C |
| src/BlackFlag.Engine.Services/Text/PdfExternalExistingTextFontService.cs | 338 | C |
| src/BlackFlag.Engine.Services/Text/PdfExternalTextShapingGuard.cs | 66 | C |
| src/BlackFlag.Engine.Services/Text/PdfFontSwitchTokenEditor.cs | 147 | C |
| src/BlackFlag.Engine.Services/Text/PdfFormPlacementCloneProjection.cs | 22 | C |
| src/BlackFlag.Engine.Services/Text/PdfFormPlacementCloneProjector.cs | 181 | C |
| src/BlackFlag.Engine.Services/Text/PdfFormPlacementDescriptor.cs | 99 | C |
| src/BlackFlag.Engine.Services/Text/PdfPositionedTextReplacementService.cs | 303 | C |
| src/BlackFlag.Engine.Services/Text/PdfSharedFormEditMode.cs | 14 | C |
| src/BlackFlag.Engine.Services/Text/PdfSharedFormExistingTextMutationService.cs | 95 | C |
| src/BlackFlag.Engine.Services/Text/PdfTextAdvanceCompensationTokenEditor.cs | 168 | C |
| src/BlackFlag.Engine.Services/Text/PdfTextAdvanceFit.cs | 12 | C |
| src/BlackFlag.Engine.Services/Text/PdfTextAdvanceMeasurement.cs | 26 | C |
| src/BlackFlag.Engine.Services/Text/PdfTextAdvanceMeasurementService.cs | 163 | C |
| src/BlackFlag.Engine.Services/Text/PdfTextArrayTokenEditor.cs | 297 | C |
| src/BlackFlag.Engine.Services/Text/PdfTextStateTokenEditor.cs | 243 | C |
| src/BlackFlag.Engine.Services/Text/PdfTextStringTokenCodec.cs | 339 | C |
| src/BlackFlag.Engine.Services/Text/PdfTextStringTokenEditor.cs | 179 | C |
| src/BlackFlag.Engine.Services/Text/PdfWidthAwareTextReplacementService.cs | 246 | C |
| src/BlackFlag.Engine.Syntax/Lexing/IPdfLexer.cs | 20 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfCharacterClassifier.cs | 191 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Delimiters.cs | 65 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Helpers.cs | 49 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Hexadecimal.cs | 37 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.HexadecimalStrings.cs | 101 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Keywords.cs | 274 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.LiteralStrings.cs | 148 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Names.cs | 91 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Numbers.cs | 142 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Position.cs | 103 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.Trivia.cs | 44 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexer.cs | 114 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexerException.cs | 73 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfLexerOptions.cs | 55 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfSourcePosition.cs | 77 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfSourceSpan.cs | 73 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfToken.cs | 73 | R |
| src/BlackFlag.Engine.Syntax/Lexing/PdfTokenKind.cs | 122 | R |
| src/BlackFlag.Engine.Syntax/Parsing/PdfTokenReader.cs | 337 | R |
| src/BlackFlag.Engine.Syntax/Parsing/PdfUnexpectedTokenException.cs | 196 | R |
| src/BlackFlag.Engine.Syntax/Properties/AssemblyInfo.cs | 3 | M |
| src/BlackFlag.Engine.Writing/IPdfOriginalSourceRevision.cs | 17 | R |
| src/BlackFlag.Engine.Writing/PdfAnnotationCommonStateFullRewritePlanner.cs | 42 | R |
| src/BlackFlag.Engine.Writing/PdfAnnotationCommonStateIncrementalWritePlanner.cs | 46 | R |
| src/BlackFlag.Engine.Writing/PdfAnnotationFullRewritePlanner.cs | 35 | R |
| src/BlackFlag.Engine.Writing/PdfAnnotationIncrementalWritePlanner.cs | 38 | R |
| src/BlackFlag.Engine.Writing/PdfAnnotationWriteProjector.cs | 403 | R |
| src/BlackFlag.Engine.Writing/PdfCountingWriteStream.cs | 64 | R |
| src/BlackFlag.Engine.Writing/PdfCrossReferenceStreamWriter.cs | 166 | R |
| src/BlackFlag.Engine.Writing/PdfCrossReferenceWriteMode.cs | 14 | R |
| src/BlackFlag.Engine.Writing/PdfDocumentInformationFullRewritePlanner.cs | 87 | R |
| src/BlackFlag.Engine.Writing/PdfDocumentInformationIncrementalWritePlanner.cs | 86 | R |
| src/BlackFlag.Engine.Writing/PdfDocumentInformationWriteProjector.cs | 194 | R |
| src/BlackFlag.Engine.Writing/PdfFullRewriteWriter.cs | 211 | R |
| src/BlackFlag.Engine.Writing/PdfIncrementalCrossReferenceStreamWriter.cs | 201 | R |
| src/BlackFlag.Engine.Writing/PdfIncrementalTraditionalCrossReferenceWriter.cs | 109 | R |
| src/BlackFlag.Engine.Writing/PdfIncrementalUpdateWriter.cs | 222 | R |
| src/BlackFlag.Engine.Writing/PdfIncrementalWritePlan.cs | 39 | R |
| src/BlackFlag.Engine.Writing/PdfIncrementalWritePlanner.cs | 84 | R |
| src/BlackFlag.Engine.Writing/PdfIndirectObjectSerializer.cs | 60 | R |
| src/BlackFlag.Engine.Writing/PdfMemorySourceRevision.cs | 61 | R |
| src/BlackFlag.Engine.Writing/PdfNestedPageTreeCrossNodeProjector.cs | 489 | R |
| src/BlackFlag.Engine.Writing/PdfNestedPageTreeStructuralProjector.cs | 336 | R |
| src/BlackFlag.Engine.Writing/PdfNestedPageTreeTopologyAnalyzer.cs | 266 | R |
| src/BlackFlag.Engine.Writing/PdfNestedPageTreeTopologyProjector.cs | 261 | R |
| src/BlackFlag.Engine.Writing/PdfNestedPageTreeWriteProjector.cs | 110 | R |
| src/BlackFlag.Engine.Writing/PdfNewAnnotationFullRewritePlanner.cs | 41 | R |
| src/BlackFlag.Engine.Writing/PdfNewAnnotationIncrementalWritePlanner.cs | 44 | R |
| src/BlackFlag.Engine.Writing/PdfNewAnnotationProjectionBatch.cs | 116 | R |
| src/BlackFlag.Engine.Writing/PdfNewPageFullRewritePlanner.cs | 36 | R |
| src/BlackFlag.Engine.Writing/PdfNewPageIncrementalWritePlanner.cs | 39 | R |
| src/BlackFlag.Engine.Writing/PdfNewPageProjectionBatch.cs | 107 | R |
| src/BlackFlag.Engine.Writing/PdfObjectSerializer.cs | 399 | R |
| src/BlackFlag.Engine.Writing/PdfObjectWriteProjection.cs | 17 | R |
| src/BlackFlag.Engine.Writing/PdfObjectWriteProjectionSet.cs | 74 | R |
| src/BlackFlag.Engine.Writing/PdfPageGeometryWriteProjector.cs | 149 | R |
| src/BlackFlag.Engine.Writing/PdfPageTreeHierarchySnapshot.cs | 296 | R |
| src/BlackFlag.Engine.Writing/PdfPageTreeWriteProjector.cs | 137 | R |
| src/BlackFlag.Engine.Writing/PdfProjectedFullRewritePlanner.cs | 108 | R |
| src/BlackFlag.Engine.Writing/PdfProjectedIncrementalWritePlanner.cs | 97 | R |
| src/BlackFlag.Engine.Writing/PdfTraditionalCrossReferenceWriter.cs | 82 | R |
| src/BlackFlag.Engine.Writing/PdfWriteMode.cs | 11 | R |
| src/BlackFlag.Engine.Writing/PdfWriteObjectAllocator.cs | 51 | R+ |
| src/BlackFlag.Engine.Writing/PdfWriteOptions.cs | 57 | R |
| src/BlackFlag.Engine.Writing/PdfWritePlan.cs | 77 | R |
| src/BlackFlag.Engine.Writing/PdfWritePlanner.cs | 87 | R |
| src/BlackFlag.Engine.Writing/PdfWritingPreservationContract.cs | 32 | R |
| src/BlackFlag.Engine.Writing/Properties/AssemblyInfo.cs | 4 | M |
| src/BlackFlag.Engine.Writing/WritingAssemblyMarker.cs | 7 | R |
| tests/BlackFlag.Engine.Abstractions.Tests/IRandomAccessSourceContractTests.cs | 61 | R |
| tests/BlackFlag.Engine.Core.Tests/Objects/PdfPrimitiveObjectTests.cs | 131 | R |
| tests/BlackFlag.Engine.Core.Tests/Primitives/ByteRangeTests.cs | 100 | R |
| tests/BlackFlag.Engine.Document.Tests/Annotations/PdfAnnotationModelTests.cs | 51 | R |
| tests/BlackFlag.Engine.Document.Tests/Decorations/PdfPageDecorationTests.cs | 64 | R |
| tests/BlackFlag.Engine.Document.Tests/Forms/PdfAcroFormTests.cs | 44 | R |
| tests/BlackFlag.Engine.Document.Tests/GlobalUsings.cs | 5 | R |
| tests/BlackFlag.Engine.Document.Tests/Model/PdfBackingDictionaryTests.cs | 229 | R |
| tests/BlackFlag.Engine.Document.Tests/Model/PdfDocumentInformationTests.cs | 26 | R |
| tests/BlackFlag.Engine.Document.Tests/Model/PdfDocumentModelTests.cs | 192 | R |
| tests/BlackFlag.Engine.Document.Tests/Objects/PdfCompositeObjectTests.cs | 216 | R |
| tests/BlackFlag.Engine.Document.Tests/Objects/PdfObjectIdentifierTests.cs | 48 | R |
| tests/BlackFlag.Engine.Document.Tests/Objects/Primitives/PdfPrimitiveObjectTests.cs | 224 | R |
| tests/BlackFlag.Engine.Document.Tests/Outlines/PdfOutlineModelTests.cs | 43 | R |
| tests/BlackFlag.Engine.Document.Tests/Pages/PdfPageGeometryStateTests.cs | 41 | R |
| tests/BlackFlag.Engine.Editing.Tests/Annotations/PdfAnnotationEditingTests.cs | 203 | R |
| tests/BlackFlag.Engine.Editing.Tests/Content/PdfContentResourceMutationTests.cs | 188 | R |
| tests/BlackFlag.Engine.Editing.Tests/Decorations/PdfPageDecorationEditingTests.cs | 191 | R |
| tests/BlackFlag.Engine.Editing.Tests/Documents/PdfEditableDocumentTests.cs | 75 | R |
| tests/BlackFlag.Engine.Editing.Tests/Documents/PdfEditingSessionTests.cs | 56 | R |
| tests/BlackFlag.Engine.Editing.Tests/Forms/PdfFormFieldEditingTests.cs | 234 | R |
| tests/BlackFlag.Engine.Editing.Tests/Geometry/PdfPageGeometryEditingTests.cs | 139 | R |
| tests/BlackFlag.Engine.Editing.Tests/GlobalUsings.cs | 5 | R |
| tests/BlackFlag.Engine.Editing.Tests/Hardening/PdfCrossStructureConsistencyTests.cs | 138 | R |
| tests/BlackFlag.Engine.Editing.Tests/Hardening/PdfEditingHardeningTests.cs | 362 | R |
| tests/BlackFlag.Engine.Editing.Tests/Hardening/PdfStructuralEditingAdversarialTests.cs | 198 | R |
| tests/BlackFlag.Engine.Editing.Tests/Hardening/PdfStructuralEditingScaleTests.cs | 64 | R |
| tests/BlackFlag.Engine.Editing.Tests/History/PdfEditHistoryTests.cs | 245 | R |
| tests/BlackFlag.Engine.Editing.Tests/Metadata/PdfDocumentInformationEditingTests.cs | 149 | R |
| tests/BlackFlag.Engine.Editing.Tests/Outlines/PdfOutlineEditingTests.cs | 198 | R |
| tests/BlackFlag.Engine.Editing.Tests/Ownership/PdfEditingOwnershipTests.cs | 22 | R |
| tests/BlackFlag.Engine.Editing.Tests/Pages/PdfEditablePageCollectionTests.cs | 48 | R |
| tests/BlackFlag.Engine.Editing.Tests/Pages/PdfPageMutationTests.cs | 215 | R |
| tests/BlackFlag.Engine.Editing.Tests/Preservation/PdfEditingPreservationContractTests.cs | 212 | R |
| tests/BlackFlag.Engine.Editing.Tests/Structural/PdfStructuralEditOperationContractTests.cs | 92 | R |
| tests/BlackFlag.Engine.Editing.Tests/Structural/PdfStructuralValueStateTests.cs | 72 | R |
| tests/BlackFlag.Engine.Editing.Tests/Text/PdfExistingTextContractTests.cs | 182 | C |
| tests/BlackFlag.Engine.Editing.Tests/Transactions/PdfEditTransactionTests.cs | 168 | R |
| tests/BlackFlag.Engine.Editing.Tests/Validation/PdfEditingValidationTests.cs | 281 | R |
| tests/BlackFlag.Engine.Filters.Tests/Corpus/PdfBinaryFilterCorpus.cs | 65 | R |
| tests/BlackFlag.Engine.Filters.Tests/Corpus/PdfTextFilterCorpus.cs | 131 | R |
| tests/BlackFlag.Engine.Filters.Tests/GlobalUsings.cs | 7 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfAscii85FilterDecoderTests.cs | 113 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfAsciiHexFilterDecoderTests.cs | 97 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterDecodeBudgetTests.cs | 111 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterDecodeContextTests.cs | 56 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterDecodeLimitsTests.cs | 60 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterIntegrationTests.cs | 266 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterLargeInputRegressionTests.cs | 169 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterPipelineTests.cs | 288 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfFilterRegistryTests.cs | 61 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfFlateFilterDecoderTests.cs | 353 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfFlateTestData.cs | 25 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfLzwFilterDecoderTests.cs | 243 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfLzwTestEncoder.cs | 102 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfPredictorDecoderTests.cs | 114 | R |
| tests/BlackFlag.Engine.Filters.Tests/PdfRunLengthFilterDecoderTests.cs | 97 | R |
| tests/BlackFlag.Engine.Filters.Tests/TestFilterDecoder.cs | 48 | R |
| tests/BlackFlag.Engine.Fonts.Platform.Tests/GlobalUsings.cs | 1 | C |
| tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFileSystemExternalFontProviderTests.cs | 620 | C |
| tests/BlackFlag.Engine.Fonts.Tests/PdfExternalFontCatalogTests.cs | 347 | C |
| tests/BlackFlag.Engine.Fonts.Tests/PdfOpenTypeFontSanitizerTests.cs | 284 | C |
| tests/BlackFlag.Engine.Fonts.Tests/PdfTrueTypeGlyphSubsetterCompositeTests.cs | 714 | C |
| tests/BlackFlag.Engine.Fonts.Tests/TestFontFactory.cs | 234 | C |
| tests/BlackFlag.Engine.IO.Tests/RandomAccess/BufferedRandomAccessCursorTests.cs | 116 | R |
| tests/BlackFlag.Engine.IO.Tests/RandomAccess/RandomAccessCursorTests.cs | 270 | R |
| tests/BlackFlag.Engine.IO.Tests/Sources/FileRandomAccessSourceTests.cs | 324 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfArrayTests.cs | 318 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfBooleanTests.cs | 49 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfDictionaryTests.cs | 467 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfHexStringTests.cs | 79 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfIndirectObjectTests.cs | 209 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfIndirectReferenceTests.cs | 190 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfIntegerTests.cs | 50 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfLiteralStringTests.cs | 57 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfNameTests.cs | 61 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfNullTests.cs | 54 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfObjectTests.cs | 94 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfObjectVisitorTests.cs | 68 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfPrimitiveVisitorTests.cs | 73 | R |
| tests/BlackFlag.Engine.Objects.Tests/PdfRealTests.cs | 50 | R |
| tests/BlackFlag.Engine.Parsing.Tests/ContentStreams/PdfContentStreamParserTests.cs | 114 | R |
| tests/BlackFlag.Engine.Parsing.Tests/Documents/PdfEffectivePageTreeLoaderTests.cs | 72 | R |
| tests/BlackFlag.Engine.Parsing.Tests/GlobalUsings.cs | 6 | R |
| tests/BlackFlag.Engine.Parsing.Tests/Loading/PdfIndirectObjectResolverTests.cs | 113 | R |
| tests/BlackFlag.Engine.Parsing.Tests/Loading/PdfObjectLocationTableTests.cs | 52 | R |
| tests/BlackFlag.Engine.Parsing.Tests/PdfCrossReferenceParserTests.cs | 764 | R |
| tests/BlackFlag.Engine.Parsing.Tests/PdfCrossReferenceStreamParserTests.cs | 755 | R |
| tests/BlackFlag.Engine.Parsing.Tests/PdfObjectParserIndirectTests.cs | 540 | R |
| tests/BlackFlag.Engine.Parsing.Tests/PdfObjectParserTests.cs | 661 | R |
| tests/BlackFlag.Engine.Parsing.Tests/PdfObjectStreamParserTests.cs | 373 | R |
| tests/BlackFlag.Engine.Parsing.Tests/PdfStreamParserTests.cs | 434 | R |
| tests/BlackFlag.Engine.Parsing.Tests/Recovery/PdfCrossReferenceRecoveryServiceTests.cs | 195 | R |
| tests/BlackFlag.Engine.Parsing.Tests/Resources/PdfResourceResolverTests.cs | 284 | R |
| tests/BlackFlag.Engine.Parsing.Tests/Security/PdfEncryptionSecurityHandlerTests.cs | 159 | R |
| tests/BlackFlag.Engine.Parsing.Tests/Security/PdfSecurityAnalyzerTests.cs | 298 | R |
| tests/BlackFlag.Engine.Parsing.Tests/Security/PdfStandardSecurityAuthenticatorTests.cs | 331 | R |
| tests/BlackFlag.Engine.Parsing.Tests/TestRandomAccessSource.cs | 85 | R |
| tests/BlackFlag.Engine.Plugins.TestPlugin/SamplePlugin.cs | 31 | R |
| tests/BlackFlag.Engine.Plugins.Tests/Activation/PluginActivationPlannerTests.cs | 99 | R |
| tests/BlackFlag.Engine.Plugins.Tests/Architecture/PluginAssemblyDependencyTests.cs | 22 | R |
| tests/BlackFlag.Engine.Plugins.Tests/Discovery/PluginDiscoveryServiceTests.cs | 109 | R |
| tests/BlackFlag.Engine.Plugins.Tests/Extensions/PluginExtensionRegistryHardeningTests.cs | 138 | R |
| tests/BlackFlag.Engine.Plugins.Tests/GlobalUsings.cs | 1 | R |
| tests/BlackFlag.Engine.Plugins.Tests/Reference/ReferencePluginContractTests.cs | 55 | R |
| tests/BlackFlag.Engine.Plugins.Tests/Runtime/PluginRuntimeHostTests.cs | 289 | R |
| tests/BlackFlag.Engine.Rendering.Skia.Tests/GlobalUsings.cs | 1 | R |
| tests/BlackFlag.Engine.Rendering.Skia.Tests/SkiaPdfPageRendererTests.cs | 49 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Color/PdfColorConverterTests.cs | 9 | R |
| tests/BlackFlag.Engine.Rendering.Tests/DisplayList/PdfClipStateTests.cs | 101 | C |
| tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfCMapParserTests.cs | 69 | C |
| tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfGlyphNameUnicodeMapTests.cs | 22 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfWinAnsiEncodingTests.cs | 16 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Functions/PdfFunctionEvaluatorBf1048GTests.cs | 69 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Functions/PdfFunctionEvaluatorTests.cs | 61 | R |
| tests/BlackFlag.Engine.Rendering.Tests/GlobalUsings.cs | 5 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Images/PdfImageCompatibilityTests.cs | 77 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Images/PdfImageDecoderTests.cs | 138 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Interpretation/PdfGraphicsInterpreterTests.cs | 321 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Patterns/PdfPatternTilePlannerTests.cs | 53 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Raster/PdfRenderLimitsBf1048FTests.cs | 19 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Raster/PdfRenderRequestTests.cs | 36 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Raster/PdfRenderWorkBudgetTests.cs | 33 | R |
| tests/BlackFlag.Engine.Rendering.Tests/Shadings/PdfShadingDecoderTests.cs | 50 | R |
| tests/BlackFlag.Engine.SDK.Adapters.Tests/Editing/PdfEditingSdkHardeningTests.cs | 90 | R |
| tests/BlackFlag.Engine.SDK.Adapters.Tests/Editing/PdfEditingSdkSessionTests.cs | 164 | R |
| tests/BlackFlag.Engine.SDK.Adapters.Tests/Editing/PdfExistingTextSdkSessionTests.cs | 422 | C |
| tests/BlackFlag.Engine.SDK.Adapters.Tests/GlobalUsings.cs | 1 | R |
| tests/BlackFlag.Engine.SDK.Adapters.Tests/Security/DocumentSecuritySdkServiceTests.cs | 33 | R |
| tests/BlackFlag.Engine.SDK.Tests/Editing/PdfEditingSdkContractTests.cs | 75 | R |
| tests/BlackFlag.Engine.SDK.Tests/Editing/PdfExistingTextSdkContractTests.cs | 43 | C |
| tests/BlackFlag.Engine.SDK.Tests/GlobalUsings.cs | 4 | R |
| tests/BlackFlag.Engine.SDK.Tests/Plugins/PluginCapabilityAccessContractTests.cs | 30 | R |
| tests/BlackFlag.Engine.SDK.Tests/Plugins/PluginDependencyManifestTests.cs | 54 | R |
| tests/BlackFlag.Engine.SDK.Tests/Plugins/PluginManifestConstructionTests.cs | 69 | R |
| tests/BlackFlag.Engine.SDK.Tests/Plugins/PluginManifestReaderTests.cs | 48 | R |
| tests/BlackFlag.Engine.SDK.Tests/Plugins/PluginSdkIsolationTests.cs | 69 | R |
| tests/BlackFlag.Engine.SDK.Tests/Security/DocumentSecuritySdkServiceTests.cs | 70 | R |
| tests/BlackFlag.Engine.Security.Tests/Containers/DocumentEmbeddedInspectionEngineTests.cs | 212 | R |
| tests/BlackFlag.Engine.Security.Tests/Containers/DocumentEmbeddedObjectClassifierTests.cs | 110 | R |
| tests/BlackFlag.Engine.Security.Tests/Formats/Markdown/MarkdownSecurityAnalyzerTests.cs | 64 | R |
| tests/BlackFlag.Engine.Security.Tests/Formats/Ole/LegacyWordBinarySecurityAnalyzerTests.cs | 148 | R |
| tests/BlackFlag.Engine.Security.Tests/Formats/Ooxml/OoxmlWordSecurityAnalyzerTests.cs | 382 | R |
| tests/BlackFlag.Engine.Security.Tests/Formats/OpenDocument/OpenDocumentTextSecurityAnalyzerTests.cs | 185 | R |
| tests/BlackFlag.Engine.Security.Tests/Formats/Rtf/RtfSecurityAnalyzerTests.cs | 60 | R |
| tests/BlackFlag.Engine.Security.Tests/GlobalUsings.cs | 1 | R |
| tests/BlackFlag.Engine.Security.Tests/Inspection/DocumentSecurityAdversarialHardeningTests.cs | 149 | R |
| tests/BlackFlag.Engine.Security.Tests/Inspection/DocumentSecurityAssessmentEngineTests.cs | 90 | R |
| tests/BlackFlag.Engine.Security.Tests/Inspection/DocumentSecurityInspectionBudgetTests.cs | 78 | R |
| tests/BlackFlag.Engine.Security.Tests/Model/DocumentSecurityObservationTests.cs | 49 | R |
| tests/BlackFlag.Engine.Security.Tests/Plugins/BlackFlagPluginSecurityPolicyTests.cs | 31 | R |
| tests/BlackFlag.Engine.Security.Tests/Policy/BlackFlagDocumentSecurityPolicyTests.cs | 118 | R |
| tests/BlackFlag.Engine.Security.Tests/Remediation/DocumentSecurityRemediationPlannerTests.cs | 102 | R |
| tests/BlackFlag.Engine.Security.Tests/Remediation/PassiveConversionSecurityServiceTests.cs | 72 | R |
| tests/BlackFlag.Engine.Security.Tests/Reporting/DocumentSecurityReportFactoryTests.cs | 74 | R |
| tests/BlackFlag.Engine.Security.Tests/Reporting/DocumentSecurityTextReportWriterTests.cs | 63 | R |
| tests/BlackFlag.Engine.Security.Tests/ResultCollectionImmutabilityTests.cs | 198 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfAnnotationCommonStateWritingServiceTests.cs | 235 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfAnnotationStructuralWritingServiceTests.cs | 388 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfAnnotationTransactionWritingServiceTests.cs | 390 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfEditingSaveConformanceTests.cs | 587 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfEditingSaveServiceTests.cs | 281 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfEditingServiceTests.cs | 38 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfNestedPageTransactionWritingServiceTests.cs | 592 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfNestedPageTreeStructuralWritingServiceTests.cs | 408 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfNestedPageTreeTopologyManagementTests.cs | 392 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfPageGeometryWritingServiceTests.cs | 194 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfPageRotationWritingServiceTests.cs | 195 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfPageStateWritingServiceTests.cs | 322 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfPageStructuralWritingServiceTests.cs | 357 | R |
| tests/BlackFlag.Engine.Services.Tests/Editing/PdfPageTransactionWritingServiceTests.cs | 473 | R |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalTrueTypeResourceProjectorTests.cs | 586 | C |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalTrueTypeSubsetResourceProjectorTests.cs | 619 | C |
| tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalType0TrueTypeResourceProjectorTests.cs | 686 | C |
| tests/BlackFlag.Engine.Services.Tests/GlobalUsings.cs | 5 | R |
| tests/BlackFlag.Engine.Services.Tests/Pages/PdfPageServiceTests.cs | 141 | R |
| tests/BlackFlag.Engine.Services.Tests/Resources/PdfPageResourceMutationProjectorTests.cs | 318 | C |
| tests/BlackFlag.Engine.Services.Tests/Security/DocumentSecurityServiceTests.cs | 48 | R |
| tests/BlackFlag.Engine.Services.Tests/Security/Trust/DocumentTrustEvaluatorTests.cs | 260 | R |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfCompoundTextOperatorNormalizerTests.cs | 72 | C |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfContentStreamMutationCodecTests.cs | 254 | C |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextDiscoveryServiceTests.cs | 460 | C |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextFindReplaceServiceTests.cs | 553 | C |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextFontServiceTests.cs | 492 | C |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextReplacementServiceTests.cs | 625 | C |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextStyleServiceTests.cs | 69 | C |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextTypographyServiceTests.cs | 335 | C |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfExternalExistingTextFontServiceTests.cs | 105 | C |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFormPlacementCloneProjectorTests.cs | 255 | C |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfPositionedTextReplacementServiceTests.cs | 395 | C |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfTextAdvanceMeasurementServiceTests.cs | 164 | C |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/LexerTestFactory.cs | 26 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfCharacterClassifierTests.cs | 193 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerCoreTests.cs | 360 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerExceptionTests.cs | 100 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerHexadecimalStringTests.cs | 473 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerKeywordTests.cs | 482 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerLiteralStringTests.cs | 495 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerNameTests.cs | 279 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerNumberTests.cs | 434 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfLexerOptionsTests.cs | 44 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfSourcePositionTests.cs | 77 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfSourceSpanTests.cs | 91 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/PdfTokenTests.cs | 92 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Lexing/TestRandomAccessCursor.cs | 144 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Parsing/PdfTokenReaderTests.cs | 302 | R |
| tests/BlackFlag.Engine.Syntax.Tests/Parsing/PdfUnexpectedTokenExceptionTests.cs | 173 | R |
| tests/BlackFlag.Engine.Writing.Tests/GlobalUsings.cs | 5 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfAnnotationCommonStateWritePlannerTests.cs | 216 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfAnnotationWriteProjectorTests.cs | 219 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfCrossReferenceStreamWriterTests.cs | 186 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfDocumentInformationFullRewritePlannerTests.cs | 188 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfDocumentInformationIncrementalWritePlannerTests.cs | 143 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfDocumentInformationWriteProjectorTests.cs | 181 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfFullRewriteWriterTests.cs | 239 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfIncrementalUpdateWriterTests.cs | 297 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfIncrementalWritePlannerTests.cs | 131 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfIndirectObjectSerializerTests.cs | 40 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfNestedPageTreeCrossNodeProjectorTests.cs | 420 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfNestedPageTreeStructuralProjectorTests.cs | 380 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfNestedPageTreeWriteProjectorTests.cs | 363 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfNewAnnotationWritePlannerTests.cs | 247 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfNewPageWritePlannerTests.cs | 228 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfObjectSerializerTests.cs | 119 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfPageGeometryWriteProjectorTests.cs | 136 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfPageTreeWriteProjectorTests.cs | 154 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfProjectedFullRewritePlannerTests.cs | 136 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfProjectedIncrementalWritePlannerTests.cs | 161 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfTargetedAnnotationWritePlannerTests.cs | 215 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfWriteObjectAllocatorTests.cs | 66 | R |
| tests/BlackFlag.Engine.Writing.Tests/PdfWritePlannerTests.cs | 145 | R |
| tests/BlackFlag.Engine.Writing.Tests/WritingAssemblyTests.cs | 19 | R |
| tests/PiratePDF.DocGen.Tests/Changes/DocumentChangeApplierTests.cs | 120 | R |
| tests/PiratePDF.DocGen.Tests/Changes/DocumentChangeSetTests.cs | 128 | R |
| tests/PiratePDF.DocGen.Tests/Commands/CommandDispatcherTests.cs | 140 | R |
| tests/PiratePDF.DocGen.Tests/GlobalUsings.cs | 6 | R |
| tests/PiratePDF.DocGen.Tests/Health/DocumentationHealthCalculatorTests.cs | 141 | R |
| tests/PiratePDF.DocGen.Tests/Library/DocumentationLibraryTests.cs | 158 | R |
| tests/PiratePDF.DocGen.Tests/Parsing/MarkdownMetadataParserTests.cs | 160 | R |
| tests/PiratePDF.DocGen.Tests/Repair/BrokenDocumentReferenceRepairRuleTests.cs | 179 | R |
| tests/PiratePDF.DocGen.Tests/Repair/DocumentationRepairPlannerTests.cs | 222 | R |
| tests/PiratePDF.DocGen.Tests/Repair/MissingNextReviewRepairRuleTests.cs | 85 | R |
| tests/PiratePDF.DocGen.Tests/Repair/MissingRevisionHistoryRepairRuleTests.cs | 164 | R |
| tests/PiratePDF.DocGen.Tests/TestInfrastructure/DocumentationTestFactory.cs | 86 | R |
| tests/PiratePDF.DocGen.Tests/TestInfrastructure/TemporaryDirectory.cs | 92 | R |
| tests/PiratePDF.DocGen.Tests/Validation/DocumentationContractIntegrationTests.cs | 70 | R |
| tests/PiratePDF.DocGen.Tests/Validation/DocumentationValidatorTests.cs | 200 | R |
| tests/PiratePDF.DocGen.Tests/Validation/MarkdownStructureValidatorTests.cs | 169 | R |
| tools/PiratePDF.DocGen/Changes/Application/DocumentChangeApplicationResult.cs | 6 | R |
| tools/PiratePDF.DocGen/Changes/Application/DocumentChangeApplier.cs | 216 | R |
| tools/PiratePDF.DocGen/Changes/DocumentChange.cs | 172 | R |
| tools/PiratePDF.DocGen/Changes/DocumentChangeSet.cs | 133 | R |
| tools/PiratePDF.DocGen/Changes/DocumentChangeType.cs | 8 | R |
| tools/PiratePDF.DocGen/Changes/Reporting/DocumentChangeReportWriter.cs | 239 | R |
| tools/PiratePDF.DocGen/Commands/AllCommand.cs | 96 | R |
| tools/PiratePDF.DocGen/Commands/CommandContext.cs | 68 | R |
| tools/PiratePDF.DocGen/Commands/CommandDispatcher.cs | 137 | R |
| tools/PiratePDF.DocGen/Commands/DoctorCommand.cs | 375 | R |
| tools/PiratePDF.DocGen/Commands/HealthCommand.cs | 157 | R |
| tools/PiratePDF.DocGen/Commands/IDocGenCommand.cs | 12 | R |
| tools/PiratePDF.DocGen/Commands/IndexCommand.cs | 323 | R |
| tools/PiratePDF.DocGen/Commands/RepairCommand.cs | 380 | R |
| tools/PiratePDF.DocGen/Commands/ValidateCommand.cs | 149 | R |
| tools/PiratePDF.DocGen/Generation/TrackedGeneratedFileWriter.cs | 94 | R |
| tools/PiratePDF.DocGen/Generation/ValidationReportWriter.cs | 230 | R |
| tools/PiratePDF.DocGen/Health/DocumentationHealthCalculator.cs | 170 | R |
| tools/PiratePDF.DocGen/Health/DocumentationHealthSnapshot.cs | 20 | R |
| tools/PiratePDF.DocGen/Infrastructure/DocumentationLibraryLoader.cs | 63 | R |
| tools/PiratePDF.DocGen/Infrastructure/RepositoryLocator.cs | 35 | R |
| tools/PiratePDF.DocGen/Library/Coverage/DocumentationCoverage.cs | 120 | R |
| tools/PiratePDF.DocGen/Library/Coverage/DocumentationCoverageSnapshot.cs | 52 | R |
| tools/PiratePDF.DocGen/Library/DocumentationDocument.cs | 148 | R |
| tools/PiratePDF.DocGen/Library/DocumentationLibrary.cs | 67 | R |
| tools/PiratePDF.DocGen/Library/DocumentationSection.cs | 13 | R |
| tools/PiratePDF.DocGen/Library/Queries/DocumentationQueries.cs | 121 | R |
| tools/PiratePDF.DocGen/Library/Relationships/DocumentationRelationships.cs | 126 | R |
| tools/PiratePDF.DocGen/Library/RevisionHistoryEntry.cs | 6 | R |
| tools/PiratePDF.DocGen/Library/Statistics/DocumentationStatistics.cs | 104 | R |
| tools/PiratePDF.DocGen/Models/DocumentMetadata.cs | 13 | R |
| tools/PiratePDF.DocGen/Models/DocumentStatuses.cs | 72 | R |
| tools/PiratePDF.DocGen/Models/DocumentationContract.cs | 22 | R |
| tools/PiratePDF.DocGen/Parsing/MarkdownDocumentParser.cs | 430 | R |
| tools/PiratePDF.DocGen/Parsing/MarkdownMetadataParser.cs | 343 | R |
| tools/PiratePDF.DocGen/Program.cs | 51 | R |
| tools/PiratePDF.DocGen/Properties/AssemblyInfo.cs | 3 | M |
| tools/PiratePDF.DocGen/Repair/Diagnostics/RepairDiagnostic.cs | 33 | R |
| tools/PiratePDF.DocGen/Repair/Diagnostics/RepairDiagnosticSeverity.cs | 8 | R |
| tools/PiratePDF.DocGen/Repair/DocumentationRepairPlan.cs | 58 | R |
| tools/PiratePDF.DocGen/Repair/DocumentationRepairPlanner.cs | 64 | R |
| tools/PiratePDF.DocGen/Repair/Metadata/MarkdownMetadataLocator.cs | 248 | R |
| tools/PiratePDF.DocGen/Repair/Metadata/MarkdownMetadataValueSpan.cs | 10 | R |
| tools/PiratePDF.DocGen/Repair/References/DocumentationRedirectMap.cs | 34 | R |
| tools/PiratePDF.DocGen/Repair/Rules/BrokenDocumentReferenceRepairRule.cs | 196 | R |
| tools/PiratePDF.DocGen/Repair/Rules/CanonicalStatusRepairRule.cs | 76 | R |
| tools/PiratePDF.DocGen/Repair/Rules/DocumentChangeBuilder.cs | 0 | R |
| tools/PiratePDF.DocGen/Repair/Rules/IDocumentRepairRule.cs | 14 | R |
| tools/PiratePDF.DocGen/Repair/Rules/IsoDateRepairRule.cs | 125 | R |
| tools/PiratePDF.DocGen/Repair/Rules/MissingNextReviewRepairRule.cs | 81 | R |
| tools/PiratePDF.DocGen/Repair/Rules/MissingRevisionHistoryRepairRule.cs | 146 | R |
| tools/PiratePDF.DocGen/Repair/Rules/MissingTrailingNewlineRepairRule.cs | 70 | R |
| tools/PiratePDF.DocGen/Repair/Rules/RepairRuleContext.cs | 28 | R |
| tools/PiratePDF.DocGen/Repair/Rules/RepairRuleRegistry.cs | 19 | R |
| tools/PiratePDF.DocGen/Reporting/DocumentationHealthReportWriter.cs | 362 | R |
| tools/PiratePDF.DocGen/Validation/DocumentationValidationResult.cs | 32 | R |
| tools/PiratePDF.DocGen/Validation/DocumentationValidator.cs | 335 | R |
| tools/PiratePDF.DocGen/Validation/MarkdownStructureValidator.cs | 188 | R |
| tools/PiratePDF.DocGen/Validation/ValidationIssue.cs | 7 | R |
| tools/PiratePDF.DocGen/Validation/ValidationSeverity.cs | 8 | R |
