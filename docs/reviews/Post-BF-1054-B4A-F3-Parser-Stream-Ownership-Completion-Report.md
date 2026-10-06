# Post-BF-1054 B4A-F3 Parser-Stream Ownership Completion Report

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-019 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1039, BF-1040, BF-1043, BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-12 |
| **Next Review** | 2026-12-12 |

## Baseline and authority

B4A-F3 addresses POST-P0-013 with the user's narrow expanded authorization for necessary directly exposed defects. Starting branch: **maintenance/post-BF-1054-repository-review**. Starting commit: **1dddd9a1f12f7e91fd3bb81537a5c820b20cf2db**, Consolidate release gate validation. The working tree and diff were clean. F2 was committed as 7c71424, Harden source revision ownership; the subsequent consolidation was also committed. No branch switch occurred.

Root AGENTS.md, PROJECT-CHARTER.md, README.md and the current gate were read. Applicable context includes the [main audit](Post-BF-1054-CSharp-Repository-Audit.md), [ownership characterization](Post-BF-1054-B4A-Ownership-Candidate-Characterization.md), [B4A](Post-BF-1054-Batch-B4A-Sanitized-Font-Ownership-Completion-Report.md), [F1](Post-BF-1054-B4A-F1-Descriptor-Ownership-Completion-Report.md), [F2](Post-BF-1054-B4A-F2-Source-Revision-Ownership-Completion-Report.md), and [pre-B4 expression review](Post-BF-1054-Pre-B4-Expression-and-Materialization-Review.md); BF-1039 raw stream ownership, BF-1040 filters, BF-1043 lazy resolution, BF-1051A/E ownership/history, BF-1053 original-revision/save preservation, and BF-1054 stream/filter/identity preservation contracts. Earlier historical reports remain unchanged.

Invoke-PiratePDFReleaseGate.ps1 is the single authoritative gate. Its integrated source-size stage contains the former standalone validation. Test-PiratePDFSourceFileSizes.ps1 is intentionally absent and was neither recreated nor used. The consolidated gate is not modified by F3.

## Characterized defect and corrected invariant

Objects.PdfStream already copied nonempty constructor input into a private readonly array; empty streams held a zero-length array. Data returned ReadOnlyMemory over that retained storage. Supported managed MemoryMarshal.TryGetArray and AsMemory calls recovered writable access. Changing a returned view could change later reads and byte-dependent equality/hash while leaving dictionary identity and byte count unchanged.

The two original Parsing characterization cases passed before edits: ASCIIHex 41> became 42>, changing decoded A to B; a real resolver's ObjStm bytes 2 0 42 became 2 0 43, leaving cached compressed object 42 inconsistent with a fresh parse of the same cached stream. A fresh resolver over the unchanged random-access source still returned 42. No on-disk corruption, PDF-only exploit, isolated-plugin escape or automatic parser-to-writer corruption was established.

Both inverted Parsing cases failed on the old production implementation. The new Objects ownership/key regression also failed; its empty-stream control passed. This establishes the regression tests detect the original alias rather than merely exercising a new accessor.

**Data now returns an independent defensive snapshot using data.ToArray().** Constructor storage and private byte-based equality/hash loops are unchanged. Nonempty results never share retained storage or another public result. Empty results have no mutable element and need no unique array identity. A caller may mutate its returned snapshot, including through an array segment, without changing the stream or another retained snapshot. No public snapshot is cached.

## Exact production changes and API decision

| File | Change |
|---|---|
| src/BlackFlag.Engine.Objects/PdfStream.cs | Defensive Data snapshot; precise snapshot XML documentation; additive scalar Length getter over private storage |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.XObjects.cs | Use actual stored Length for image encoded-size validation and error text |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.Fonts.cs | Use actual stored Length for embedded-font size validation and error text |
| src/BlackFlag.Engine.Parsing/Resources/PdfResourceResolver.ColorSpaces.cs | Use actual stored Length for ICC profile size validation and error text |
| src/BlackFlag.Engine.Rendering/Images/PdfImageDecoder.cs | Enforce the existing encoded-byte ceiling before allocating an XObject Data snapshot; preserve inline-image validation behavior |

All existing public signatures remain intact. **The sole additive public API is int PdfStream.Length.** This is necessary to keep cross-assembly resource checks allocation-free without exposing retained memory. Before F3, the only actual-byte-count API was Data.Length. With defensive Data, that would allocate an entire stream solely to validate its size, and rejected resources would allocate again while formatting the error. Dictionary /Length cannot substitute: this object deliberately does not synchronize that entry, and public construction can supply a different value. Tests retain /Length 0 or 99 while checking actual storage length independently.

An internal scalar getter alone cannot serve Parsing/Rendering because Objects has no existing friend relationship. A new friend declaration merely to access the scalar would unnecessarily expose the assembly's internals. A public borrowed span/memory or callback helper would broaden the ownership surface. The scalar property is the smallest safe additive contract; it transfers no memory, changes no existing signature or size ceiling, and does not normalize dictionary state. No friend assembly, dependency or project reference is added.

## Cross-assembly consumer review

The repository-wide production Data search identified nine direct consumer files before F3: six decode readers and three resource-size readers. Remaining production Data reads are exactly one each in the six decode paths below. Constructor and stream-type searches distinguished parser, Document, decoded buffers and BCL streams.

| Assembly / consumer | Classification | Decision and ownership |
|---|---|---|
| Objects.PdfStream constructor | Already-copying | Retain one ownership copy of nonempty input |
| Objects.PdfStream.Equals/GetHashCode | Trusted private consumer | Continue reading private arrays directly; no Data accessor/copy, new borrow API or changed equality definition |
| Any public Data caller or visitor receiving PdfStream | Public/untrusted | One independent snapshot per read |
| Parsing.ObjectStreams.PdfObjectStreamParser | Public callback boundary; single snapshot | One Data evaluation passed to injectable IPdfFilterPipeline; independently owned input must be retained |
| Parsing.CrossReferences.PdfCrossReferenceStreamParser | Public callback boundary; single snapshot | Same one-snapshot policy for injectable filters |
| Parsing.Rendering.PdfNestedGraphicsProvider | Public callback boundary; single snapshot | Form/Type3 content decode uses one snapshot; subsequent content parser sees decoded output |
| Rendering.Fonts.PdfFontStreamDecoder | Public callback boundary; single snapshot | Font/ToUnicode/encoding decode uses one snapshot; injectable pipeline receives no retained alias |
| Rendering.Images.PdfImageDecoder | Public callback boundary; single snapshot | Validate scalar length first, then reuse one encoded snapshot through prefix filters and caller-supplied codecs |
| Rendering.Functions.PdfFunctionEvaluator | Single snapshot | Locally constructed default pipeline reads one snapshot; no new cross-assembly friendship to remove that single copy |
| Parsing.Resources font/image/ICC resolution | Scalar metadata reader | Replace six Data.Length evaluations across three files with Length; no byte snapshot is needed |
| Parsing.Streams.PdfStreamParser and PdfObjectParser.IndirectObjects | Already-copying producer / unaffected envelope | Exact source read and constructor ownership copy remain; indirect identity/generation unchanged |
| Parsing.Loading.PdfIndirectObjectResolver | Unaffected retention/decode orchestration | Retains the same Lazy stream/object instances; no invalidation, observer, versioning or alternate cache introduced |
| Parsing.ObjectStreams.PdfCompressedObjects | Already-copying | Owns decoded bytes and copies each selected object slice into private random-access input; unchanged |
| Parsing security visitor and resource wrappers | Unaffected metadata/reference consumers | Security visits stream dictionaries, not Data; resolved font/image/Form/ICC wrappers retain stream references but public byte exposure is now isolated |
| Document / Editing / Services / Writing | Separate stream model / unaffected production | No production Objects.PdfStream.Data bridge found; document streams use their own explicit mutable/copy APIs |

**No internal borrowed-byte API was introduced.** Sharing ReadOnlySpan with an injectable decoder is unsafe: ordinary managed callback code can write through MemoryMarshal.GetReference. IPdfFilterPipeline implementations can also retain and mutate supplied ReadOnlyMemory. A regression explicitly supplies such a pipeline to the actual ObjStm parser; its callback and later retained-input mutation cannot change the parser stream. Even PdfFilterPipeline dispatches to registered IPdfFilterDecoder implementations. Isolation remains explicit at that existing extension boundary.

No cross-assembly operation evaluates Data repeatedly for length/slicing/decoding. Existing single evaluations remain unchanged; image decoding already names and reuses its encoded snapshot. There is no new storage cache, borrowed-memory lifetime, unsafe/native code, pinning, memory manager or pooling scheme.

## Allocation accounting

Let E denote encoded length and D decoded length. Counts below separate encoded ownership, decoded output and object-sized materialization.

| Boundary | Before | After |
|---|---|---|
| PdfStream constructor | One E-byte ownership copy, except empty | Unchanged |
| Public Data read | Zero-copy unsafe alias | One E-byte independent snapshot, except empty |
| Equals/hash | Private bytes, no Data copy | Unchanged |
| Resource size accepted/rejected | Zero copied bytes through unsafe length view | Zero copied bytes through scalar Length; avoids naive one/two E-byte snapshots |
| Oversized image decode | No encoded copy before size rejection | No encoded copy before rejection; avoids naive one E-byte snapshot |
| ASCIIHex pipeline input | No encoded isolation copy | One E-byte snapshot; decoder and bounded decoded-output allocations unchanged |
| Single Flate pipeline input and working input | Existing decoder-owned E-byte copy for ZLibStream | One Data snapshot plus the same decoder-owned E-byte copy: two total encoded copies excluding constructor |
| Unfiltered decode | One output copy, D=E | One encoded snapshot plus the existing unfiltered output copy |
| Each filtered output stage | Bounded accumulator/buffer plus independent output materialization | Unchanged; no new decoded-output copy |
| ObjStm PdfCompressedObjects construction | One D-byte owned decoded copy | Unchanged |
| Selected compressed object parse | One selected-object byte slice copy into ReadOnlyMemoryRandomAccessSource, then normal token/object allocations | Unchanged |
| Cached resolver hit for the same object | No stream decode/copy | Unchanged |
| Explicit test-only parser-to-Document construction | One Document stream ownership copy from public alias | One public snapshot plus existing Document ownership copy |

Normal uncached ObjStm resolution therefore adds **one encoded snapshot per Parse call**. The existing resolver may parse/decode the containing ObjStm again for a different uncached compressed object; that behavior predates F3. F3 does not add another decode, per-token copy or object-stream cache. It adds a constant one-snapshot cost to each existing operation, not an E-byte copy for each small length query. Filter output budgets and source parser bounds remain unchanged. Flate's own working copy is retained because its public decoder accepts caller-owned input and clears its independently owned working bytes; it cannot assume ownership of an incoming array.

The added public cost is O(E) per access. **Measurement required:** overall parser throughput, object-stream latency, large-document peak memory, GC behavior, cache effectiveness, optimal internal/buffer API design and percentage performance claims. No end-to-end speed or memory reduction is claimed. Allocation regressions use small one-MiB fixtures and current-thread allocation bounds to detect a full-size copy during rejection; they are not throughput benchmarks or total application memory measurements.

## Directly exposed resource correction

**F3-R1, P1 resource-regression prevention:** making Data defensive naively would allocate one whole encoded stream before image decode's existing size guard and one/two copies in resource resolution's size/error paths. This is directly caused by the ownership correction, matters for attacker-controlled stream sizes, and would defeat early resource rejection. The four consumer changes and scalar getter above are required now. They introduce no new rejection policy or numeric conversion. Source inspection establishes the allocation count; regression cases reject one-MiB data while allocating less than 128 KiB in each guarded operation.

Exact members are CreateImageXObject, CreateFont, CreateIccBasedColorSpace, and PdfImageDecoder.Decode. Size thresholds, comparison strictness, messages and exception classes remain intact. Length-only changes are in the expressly required primary consumer review; the image pre-snapshot guard is the narrow adjacent fix under expanded authorization. Both are recorded explicitly rather than represented as unrelated audit findings. No other expanded-scope correction was required.

## Tests and parser consistency

| Exact test file | Changes / cases |
|---|---|
| tests/BlackFlag.Engine.Objects.Tests/PdfStreamOwnershipTests.cs | New two cases: constructor/public/retained snapshot isolation, dictionary/hash-set keys, equality/hash, actual versus dictionary length, empty behavior |
| tests/BlackFlag.Engine.Parsing.Tests/Loading/PdfStreamOwnershipCharacterizationTests.cs | Invert existing two cases; ASCIIHex stays A; actual resolver/cached stream/fresh ObjStm interpretation all stay 42; original source, dictionary identity, N/First and compressed entry remain intact |
| tests/BlackFlag.Engine.Parsing.Tests/Loading/PdfStreamOwnershipIntegrationTests.cs | New eight cases: unfiltered/Flate ObjStm, hostile retained pipeline input, xref stream offsets/generation, malformed Flate, decoded-size limit, expansion limit and cancellation |
| tests/BlackFlag.Engine.Parsing.Tests/Resources/PdfResourceResolverTests.cs | Eleven new cases: exact/over-limit and one-MiB rejected font/image/ICC streams, plus valid/oversized image decode; metadata and resource identity retained |
| tests/BlackFlag.Engine.Writing.Tests/PdfSourceRevisionOwnershipCharacterizationTests.cs | Invert the parser-to-Document copy control; copies made both before and after attempted public snapshot mutation remain original; intentional Document.ReplaceData affects only that document stream |

**24 focused cases passed.** Actual ObjStm integration is checked into the existing Parsing project and uses PdfIndirectObjectResolver, PdfObjectStreamParser and PdfCompressedObjects. The cached stream and integer retain reference identity. Public snapshot bytes can locally become 2 0 43, but future Data reads, existing cached resolution, a fresh parse of the same stream, and a new resolver all yield original 42. Original random-access source bytes compare exactly. N=1, First=4, six encoded bytes, object number 2 and compressed generation-zero identity are preserved. Xref stream entry records, including offset 42 and generation 7, remain identical after public mutation.

Flate coverage retains filter-array/DecodeParms representation with Predictor 1 and a private parameter, exact encoded bytes and decoded content. Mutating independent decoded output also cannot change the parser stream. Equality/hash/key tests leave deliberately inconsistent dictionary Length untouched; this batch does not normalize PDF dictionaries.

## Exception compatibility and observed limitation

Constructor null validation, stream parser bounds, filter/read-parameter validation, resource limits and resolver failure handling remain unchanged. No catch/wrap/suppression was added. Malformed established fixture 01 02 03 04 05 still reports PdfFilterDecodeException/InvalidData; byte and expansion ceilings retain their exact failure classifications and messages before/after snapshot mutation. Pre-cancellation remains OperationCanceledException with the same token on the tested entry path. Existing complete project suites cover broader parser/filter/resolver rejection contracts.

An initial new malformed fixture consisting solely of byte 78 was accepted by the unchanged Flate pipeline instead of throwing. This occurred on its first decode, before any public-view mutation. The existing decoder delegates completion to ZLibStream, so this test assumption did not describe the current exception contract. The ownership regression uses the established malformed fixture above. This observation is not fixed or claimed fixed by F3; it does not permit retained Data mutation or cached/fresh divergence, and changing truncated-Flate acceptance is outside this ownership correction. No assertion about rejecting every truncated zlib prefix is made. This is recorded as **F3-OBS-01, truncated-Flate acceptance requiring separate characterization**.

Public snapshot allocation can fail under exhausted managed memory before downstream work; that unavoidable allocation possibility is disclosed. The new scalar length paths specifically avoid that added allocation before oversized-resource rejection. No domain exception contract is otherwise changed.

## Document streams, preservation and save/reopen applicability

Document.Objects.PdfStream is mutable by design: it copies constructor dictionary/data, exposes defensive ToByteArray, and replaces data through an explicitly copying ReplaceData. It is distinct from Objects.PdfStream. Writing serializes the Document model. Repository searches found no automatic production conversion from parser Data to Document/Editing/Services/Writing. Therefore there is no changed production save bridge requiring a new save/reopen fixture, and none was invented.

The existing explicit test-only bridge now proves both pre- and post-attempt Document copies serialize identical original bytes. Changing the latter Document stream deliberately leaves the parser and earlier copy untouched. F2 original-prefix behavior and existing Writing tests remain intact. Actual parser integration is persistent and required for ObjStm/xref, while additional save/reopen is **not applicable to the changed production path**. No supplemental executable, production bridge or test-project reference was added.

## Validation and release-gate disposition

Complete affected suites passed: **Objects 180, Parsing 216, Rendering 89, Writing 153**. Filters' complete **92 tests** also passed because its real decode paths are exercised by F3 integration. There were zero failures/skips and no final targeted-build warnings/errors. Security and Services production/read paths did not change; they are covered by the full gate rather than labeled independently affected merely because they transitively reference Objects.

Focused commands used `dotnet test tests/<project>/<project>.csproj --configuration Release --no-restore`, filtering PdfStreamOwnership, StreamResourceLimitsUseActualLength, ImageDecodePreservesSnapshot and the explicit Document control. Complete Objects/Parsing/Writing runs reused freshly built outputs with --no-build. Rendering/Filters used --no-restore. Missing test imports encountered during development were corrected without analyzer suppression.

The **full unmodified consolidated release gate passed**, exit code zero: **1,901 tests across 20 projects**, zero failed/skipped, **zero build warnings/errors**. Integrated source-size validation passed for **1,035 files**, with unchanged review notices for the 937-line graphics interpreter and 930-line sanitizer. Restore, build, tests, DocGen, repair preview, whitespace and status stages all executed normally. Command:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

DocGen validated **186 documents**, zero errors/warnings/information. Health was **91.6% (A-)**; repair preview proposed **zero changes/files/diagnostics**. POST-P0-013 was marked implemented in the main audit only after this successful gate. Final documentation generation, health, repair and whitespace checks follow the completion update.

**POST-P0-013 is closed for B4A-F3.** Public Data cannot mutate retained encoded state, and no directly exposed unresolved retained-byte alias or cached/fresh divergence remains in the modified path. The separate Flate acceptance observation is explicitly outside this claim.

## CI finding and final scope

**POST-P2-008 remains open.** The current .github/workflows/continuous-integration.yml invokes separate restore/build/test/DocGen commands and no authoritative gate or integrated source-size stage. Consolidation alone is not evidence of CI enforcement. The main audit's current wording is reconciled to the consolidated architecture; no CI implementation is part of F3 and historical source-size reports are not rewritten.

The exact final change set is **15 files**: five production and five test files listed above; this report; docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md; and normal DocGen updates to docs/Documentation-Index.md, docs/Documentation-Health.md and docs/ValidationReport.md. Every changed file was reviewed. No gate, dependency, project-reference, friend-assembly, history/cache algorithm, security ceiling or unrelated syntax change is included.

POST-P0-011, POST-P0-005/006/007/008/009 and the separate text/glyph/inline-image/hex/Core string views remain untouched. F1 and F2 production are unchanged. B4A-F4, B4B/B4C and roadmap work were not started. No commit, push, merge, tag or branch switch occurred. Stop after F3 validation and documentation; further work requires explicit approval.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-12 | Isolate parser-stream public bytes, preserve bounded resource checks, validate real parser/cache/filter behavior and record cross-assembly copying, API, CI and preservation limits. |
