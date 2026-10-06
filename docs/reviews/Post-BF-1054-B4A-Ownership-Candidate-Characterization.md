# Post-BF-1054 B4A Ownership Candidate Characterization

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-016 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-09 |
| **Next Review** | 2026-12-09 |

## Scope and baseline

Characterization only, following the committed [B4A ownership correction](Post-BF-1054-Batch-B4A-Sanitized-Font-Ownership-Completion-Report.md). The initial working tree and diff were clean on `maintenance/post-BF-1054-repository-review`, commit **`8c0c872042fee8d2fc758b9c5dde3dfe84153cfc`**, `Harden sanitized font ownership boundaries`. Recent history was checked. No production source, dependency, project reference, resource ceiling, API signature or ownership implementation changed. No commit, push, merge, tag or branch switch occurred.

Governing context: root AGENTS.md, PROJECT-CHARTER.md and README.md; Blueprint Volumes II and IV; Architecture/MEMORY-ARCHITECTURE.md; BF-1051A ownership and BF-1051E history contracts; BF-1053E original-source revision and incremental planning; BF-1054A identity/provenance, BF-1054C direct replacement and BF-1054V closure contracts; the [current audit](Post-BF-1054-CSharp-Repository-Audit.md), B4A completion report and [pre-B4 expression/materialization review](Post-BF-1054-Pre-B4-Expression-and-Materialization-Review.md). Historical milestone statements remain historical records.

The new tests deliberately characterize **current unfixed behavior**, alongside controls that remain safe. Passing means the observations reproduced; it does not mean these ownership boundaries are secure. A future approved implementation must invert the defect assertions to enforce the intended invariant, while retaining the controls. No reflection, unsafe code, native mutation, unbounded enumeration or memory-exhaustion probe was used.

## Dispositions and priority

P0 uses the repository definition covering correctness, integrity and preservation defects. It does not mean remote exploitability or critical severity. Every attack requires in-process managed code with access to an engine object or destination callback; no PDF-only exploit or isolated-plugin escape was demonstrated.

| Candidate | Previous candidate ID | New finding | Disposition | Demonstrated impact | Proposed order/batch |
|---|---|---|---|---|---|
| A: PdfCharacterCode.Bytes | B4A-AN-04 | POST-P0-011 | **Confirmed P0**, narrower practical impact | Public value equality/hash and caller dictionary/set invariants break; private CMap entries remain intact | 4, B4A-F4 character-code ownership |
| B: PdfExistingTextDescriptor.EncodedBytes | B4A-AN-01 | POST-P0-012 | **Confirmed P0** | Valid targets fail; stale direct font/style targets can pass after byte forgery; font switching can overwrite current text with stale Unicode | 1, B4A-F1 descriptor ownership |
| C: Objects.PdfStream.Data | B4A-AN-03 | POST-P0-013 | **Confirmed P0** | Shared cached parser stream mutates; newly decoded compressed object disagrees with cached object and source | 3, B4A-F3 parser-stream ownership |
| D: PdfMemorySourceRevision.CopyTo | B4A-AN-02 | POST-P0-014 | **Confirmed P0**, synchronous callback mechanism | Destination changes the stored revision during CopyTo; a later incremental save has a changed original prefix | 2, B4A-F2 source-revision ownership |

These batch identifiers are proposals only. Existing POST-P0 identifiers are not renumbered. No new P1 or P2 finding is established. Related views below remain candidates requiring their own scope/approval.

## Candidate A - immutable character-code keys

**Exact boundary:** `src/BlackFlag.Engine.Rendering/Fonts/PdfCharacterCode.cs`, constructor, Bytes, Equals, GetHashCode. Constructor takes a span and copies it into private readonly byte-array storage. Mutating constructor input does not change the code. Bytes returns ReadOnlyMemory over that same private array. TryGetArray and AsMemory recover writable storage through public BCL APIs. Equals compares current bytes; GetHashCode recomputes a byte-derived hash; ToString formats the current bytes. The readonly field protects the reference, not its contents.

**Bounded reproduction:** construct code `41`, insert the same object into a Dictionary and HashSet, recover Bytes and change one byte. The test tries at most 256 byte values to find a different hash, avoiding dependence on a particular process-randomized HashCode value or a presumed absence of collisions. The original input copy remains isolated. After mutation, the hash changes, equality against fresh `41` becomes false, equality against a fresh copy of the mutated bytes becomes true, and lookup by either the stored object or the equivalent new object fails. Adding the equivalent new key succeeds: dictionary Count becomes two while enumerated keys compare equal. This is an actual violation of stable value/key semantics.

**Production reachability:** all direct production users are Fonts/PdfCMap.cs, PdfCMapParser.cs, PdfFontDecoder.cs and PdfSameFontTextEncoder.cs in Rendering. PdfCMap owns CID and Unicode dictionaries keyed by PdfCharacterCode; its internal constructor copies dictionary containers, not key objects. The parser creates those keys internally. Public CMap APIs expose lookup and ReadCode, not stored keys. ReadCode constructs a new code from a copied candidate span. The font decoder reads code length/bytes, CID and Unicode mappings, and creates a copied PdfDecodedGlyph. The same-font encoder constructs lookup codes during reverse mapping.

**Negative control:** parse a CMap mapping `41` to CID 7 and Unicode A; mutate a public ReadCode result to `42`. Lookup through that changed query fails, but fresh `41` still returns CID 7/A. Public query mutation does **not** corrupt the CMap's private dictionary keys. No production HashSet or publicly obtainable stored CMap key was found. The dictionary/set corruption reproduction concerns the public value contract and caller-owned collections, not a demonstrated renderer cache attack.

**Smallest proposed fix:** retain constructor copying; return an independent Bytes snapshot. Audit PdfFontDecoder's repeated Bytes reads so a fix need not copy solely to read length. A narrowly scoped internal read path or scalar length accessor can preserve internal use without exposing storage. Caching the hash alone is insufficient because equality/ToString and caller-visible bytes would still change. Keep all public signatures. The only intended compatibility change is eliminating shared alias mutation. No PDF code ordering, glyph mapping or CMap limit changes are needed.

**Implementation validation:** invert alias/equality/hash/dictionary/set assertions; retain constructor isolation and the CMap negative control; run font decoding/reverse encoding and rendering tests. Public exposure adds an O(code-length) copy per access; constructor copying stays one. Avoid mechanical whole-CMap key cloning. Hot-path allocation/throughput impact is **measurement required**.

## Candidate B - descriptor provenance

**Exact boundary:** `src/BlackFlag.Engine.Editing/Text/PdfExistingTextDescriptor.cs`, EncodedBytes. Constructor copies its ReadOnlyMemory input into private readonly byte-array storage. The property documents a defensive immutable view but returns the private array-backed memory. Mutating public bytes changes EncodedBytes while Identity, UnicodeText, typography and font metadata remain unchanged. Identity does not hash these bytes; services use them as expected source text during validation.

**Bounded fixture:** the existing synthetic filtered-content fixture contains `(Hello) Tj`, a stable five-byte token and unchanged operation offsets. It uses the established single-Flate policy. An F2 alias to the same resolved deterministic font is registered in both the fixture resource dictionary and discovery source, keeping this test focused on source ownership rather than font-selection differences.

| Probe | Observation |
|---|---|
| Mutate original constructor input | Descriptor stays Hello; constructor ownership works |
| Recover descriptor array and change H to J | EncodedBytes becomes Jello; UnicodeText remains Hello; identity and authoritative content do not change |
| Mutate PdfDisplayTextRun.EncodedBytes result | Later run reads remain Hello; this accessor concatenates into a new array |
| Valid target, replacement/font/style | Each operation succeeds and rolls back to original content |
| Change descriptor Hello to World while source stays Hello | All three operations throw InvalidOperationException without changing content |
| Change authoritative token Hello to World, keep old descriptor/source | All three reject stale provenance without mutation |
| Then change old descriptor bytes to World | Direct replacement still rejects the independent discovered-run mismatch; direct font switching and styling proceed |
| Font switch after forged-current descriptor | Uses stale UnicodeText Hello to re-encode; the authoritative World token becomes Hello under F2 |
| Style after forged-current descriptor | Styles World even though the descriptor still reports UnicodeText Hello |
| Rollback and later undo/redo after successful forged-target operations | Restore the actual pre-operation World bytes and the actual changed bytes correctly, even after further descriptor corruption |

**Mechanism:** direct replacement compares descriptor bytes with the independently concatenated display run before invoking PdfTextStringTokenEditor. Direct font switching, styling and typography use descriptor bytes for current-token validation without that same independent byte comparison; font switching encodes descriptor.UnicodeText. Width-aware and external-font paths also pass descriptor bytes to the token editor. These latter paths are source-traced, not separately claimed as dynamic reproductions. Positioned/composite and shared-Form paths instead validate element segments from the display run; those related element aliases need separate characterization. Session/page/owner/span validation was not removed or bypassed by this probe.

**History and SDK limits:** PdfEditableDocument.Text.cs captures independent previous/replacement stream arrays in history callbacks; PdfExistingTextEditOperation retains identity and lengths, not a descriptor or its byte array. The probes found an incorrect acceptance decision, not corrupted replay storage. Services discovery/search matches expose descriptors and reuse them during find/replace. SDK adapters privately retain descriptors in target registrations, expose opaque target IDs/string metadata, and invalidate registrations after relevant mutations/history operations. This is not a demonstrated SDK-only alias access or target-registration bypass. Moreover, descriptors have public constructors: they are validation inputs, not unforgeable capabilities. Fixing the public alias cannot guarantee that a caller deliberately fabricating a new descriptor is harmless; a wider current-source-validation redesign would need separate approval.

**Smallest proposed fix:** retain constructor copy, return an independent EncodedBytes snapshot; reuse a public snapshot locally where a consumer currently evaluates it twice for validation. Do not expose a new cross-assembly mutable buffer or add friend access to Services. Keep signatures, identity and domain exception contracts. The fix should make mutation of a retained descriptor view unable to change a discovered descriptor; it should not be advertised as replacing session or current-source validation.

**Implementation validation:** convert all three valid/stale path controls into ownership regressions, proving attempted public mutation leaves the original descriptor and stale decision intact. Retain actual content assertions and rollback/undo/redo, add typography/external/width-aware representative coverage and save/reopen for any changed mutation orchestration. Public access adds O(encoded-text-length) copying; two validation accesses should not require two copies. Larger text-run costs are **measurement required**. No resource, font-rights or transaction policy should change.

## Candidate C - parser stream and cache consistency

**Exact boundary:** `src/BlackFlag.Engine.Objects/PdfStream.cs`, Data, Equals and GetHashCode. Constructor copies nonempty input; empty input shares a zero-length array with no writable element. Dictionary is the Objects-layer immutable dictionary. Data exposes the copied private array as ReadOnlyMemory; equality and hash depend on its current contents. PdfStream deliberately does not synchronize `/Length`, according to its remarks. Same-length byte mutation changes content while leaving dictionary, type, reference identity and length unchanged; no fabricated length mismatch is necessary to demonstrate the defect.

**Decode probe:** construct a three-byte ASCIIHex stream `41>` with `/Length 3` and `/Filter /ASCIIHexDecode`. Mutating constructor input does nothing. Decode returns A. Recover Data's array and change it to `42>`: later Data reads and decode return the changed content/B, equality with the original snapshot is false, and Length remains three. The earlier decoded output stays A. Filter output is an independent snapshot; the defect is the input stream alias, not shared decoder output.

**Real parser/cache probe:** parse an indirect ObjStm containing `2 0 42`, resolve object 2 to integer 42, then change the publicly exposed stream's last digit to 3. The resolver returns the same mutated stream object from its cache. Cached object 2 remains 42; freshly parsing that cached stream returns 43. A new resolver over the same untouched random-access source returns 42. Exact original source bytes remain unchanged. This deterministically demonstrates shared parsed-object state and cache/source divergence, without claiming a persisted on-disk modification.

**Production consumers:** PdfStreamParser creates the owned snapshot; PdfObjectParser.IndirectObjects and PdfIndirectObjectResolver publish/retain it, including Lazy caching and compressed-object resolution. Direct encoded-byte consumers include Parsing CrossReferences/PdfCrossReferenceStreamParser, ObjectStreams/PdfObjectStreamParser, Rendering/PdfNestedGraphicsProvider and Resources/PdfResourceResolver.Fonts and .XObjects; Rendering Fonts/PdfFontStreamDecoder, Images/PdfImageDecoder and Functions/PdfFunctionEvaluator. Resource wrappers preserve references to stream objects for font programs, ToUnicode, image/Form XObjects and color-space profiles. PdfFontDecoder interprets encoding streams through PdfFontStreamDecoder. Shading/resource/security visitors inspect dictionaries/types; this does not by itself prove they consume changed bytes. Filter and resource limits remain in effect; alias mutation cannot resize the array.

**Writing distinction and control:** Writing/PdfObjectSerializer consumes `BlackFlag.Engine.Document.Objects.PdfObject`, including its separate mutable-by-design Document.PdfStream. No automatic Objects.PdfStream-to-Writing projection was found in the inspected current path. The explicit bridge control constructs Document.PdfStream from parsed.Data before mutation: its defensive copy and serialized output remain original. Constructing another Document stream after mutation serializes the changed bytes. This proves the ownership boundary's timing, not an automatic loader-to-save exploit. Incremental original-prefix preservation is a separate source-revision contract (candidate D).

**Smallest proposed fix:** preserve Data's public signature with defensive snapshots, then review all length-only and decode consumers to avoid redundant full-stream copies. The class spans Objects/Parsing/Rendering assemblies: an assembly-internal accessor alone cannot safely supply all consumers. Do not add broad friend access, public borrowed memory or a new stream model without separate design review. Keep dictionary/reference identity, generation, filters, encoded bytes and lazy resolver semantics intact. Hash caching alone would not repair mutable equality or cache divergence.

**Implementation validation:** invert stream/cache divergence cases; retain decoder output, input/source and Document-model isolation; cover unfiltered and Flate/object/xref streams, resource decoding, malformed/filter-limit exceptions and serialization/reopen wherever a production bridge is changed. Public Data access adds O(encoded-stream-length) copying and could amplify existing length checks and decode copies. Peak allocations and realistic stream workloads are **measurement required**. This is the broadest consumer review among the four proposals.

## Candidate D - original revision callback ownership

**Exact boundary:** `src/BlackFlag.Engine.Writing/PdfMemorySourceRevision.cs`, CopyTo. Constructor validates offset/mode and copies the source span into a private readonly array. Length and CrossReferenceOffset stay fixed. `destination.Write(bytes)` resolves to the synchronous ReadOnlySpan overload. The class has no WriteAsync/CopyToAsync path and exposes neither a public byte memory value nor a persistent reader.

**Ordinary managed reproduction:** a custom destination overrides `Write(ReadOnlySpan<byte>)`, first copies the bytes normally with MemoryStream.Write, then assigns `MemoryMarshal.GetReference(buffer) = (byte)'X'` while the nonempty span is valid. This public BCL method returns a writable managed reference even for ReadOnlySpan. No unsafe/reflection/native API, out-of-range reference, escaped stack reference or post-lifetime write is used. The first destination contains the original content, but a second CopyTo returns Xource revision instead of source revision and its independently recomputed SHA-256 differs. The revision itself stores no hash; tests recompute hashes externally.

**Retained-alias distinction:** a span cannot be kept in an ordinary heap field. The confirmed attack mutates during the synchronous callback; later source copies remain corrupted. It is not a demonstrated retained array obtained directly from CopyTo. A destination implementing only the byte-array Write overload receives Stream's fallback temporary copy. Mutating that temporary array during the callback leaves the revision intact. The persistent test deliberately does not write to that fallback array after return because the BCL may already have returned it to a pool; such a write could corrupt unrelated memory owned by a subsequent renter. An initial isolated control attempted a post-return mutation and observed no source change; that observation is not relied upon as a valid lifetime contract. No supported safe retention of the original source array was established.

**Actual incremental save:** produce a small full-rewrite PDF, capture its real startxref and source bytes, then save an incremental revision to the custom destination. PdfIncrementalUpdateWriter routes the source through PdfCountingWriteStream, whose span overload forwards the borrowed span directly. The first save has the original prefix because the callback copies before modifying. A second normal save using the same source revision starts with X instead of `%`, and the original-prefix hash changes; the remaining prefix bytes, source length and CrossReferenceOffset are unchanged. Both traditional-table and cross-reference-stream incremental output reproduce this. The byte-count check cannot detect same-length corruption. A hostile destination can already corrupt its own output; the additional defect is modifying retained revision state used by later trusted saves.

**Consumers:** PdfIncrementalUpdateWriter is the only production invocation of SourceRevision.CopyTo found. IPdfOriginalSourceRevision defines immutability; incremental plans/planners and Services save/annotation/page/font/text orchestration pass the source revision through that writer. PdfCountingWriteStream preserves the callback exposure. No asynchronous callback or native consumer is required.

**Smallest proposed fix:** copy into an independent fixed-size temporary buffer before invoking caller code; never pass a span over the stored revision to a destination. A whole-revision copy would be simpler but adds O(revision-size) peak memory per save; a bounded ordinary-array chunk buffer avoids that peak. No pooling/custom memory manager is needed. Preserve synchronous ordering, destination ownership, exception propagation, exact bytes, counts and cancellation behavior (CopyTo currently has no cancellation parameter). Chunking can change callback granularity and failure timing, so explicitly review Stream compatibility before accepting that choice. The existing public signature can remain.

**Implementation validation:** invert the span-callback and later-save corruption assertions; retain array-fallback/input-copy controls; exercise repeated copies, chunk boundaries, nonseekable/writable/throwing destinations and both xref modes, original-prefix hashes, Prev/startxref and reopen. Allocation beyond the deterministic proposed O(chunk-size) bound and save throughput are **measurement required**. This is in-process integrity hardening, not protection against arbitrary native memory corruption or an OS sandbox.

## Related provenance views

| Exact file/member | Classification | Evidence and limit |
|---|---|---|
| `src/BlackFlag.Engine.Rendering/DisplayList/PdfTextRunElement.cs`, EncodedBytes | **Trusted/provenance candidate** | Constructor copies into private array but public memory aliases it; ordered text/Unicode relationship and positioned current-source checks consume it. Separate multi-element characterization needed. |
| `src/BlackFlag.Engine.Rendering/Fonts/PdfDecodedGlyph.cs`, SourceCode | **Trusted/provenance candidate** | Copied private array exposed while CID/glyph/Unicode/position fields remain fixed. No history/cache corruption claimed without further testing. |
| `src/BlackFlag.Engine.Rendering/Graphics/PdfInlineImage.cs`, EncodedBytes | **Trusted/provenance candidate** | Explicit immutable-image contract, copied private array and public alias; image decoder consumes the bytes. Separate image-decode/invariant characterization required. |
| `src/BlackFlag.Engine.Objects/PdfHexString.cs`, Value | **Trusted/provenance candidate** | Explicit immutable sequence, constructor copy, array-backed exposure and byte-derived equality/hash. Parser/operand use needs its own regression scope. |
| `src/BlackFlag.Engine.Core/Objects/PdfString.cs`, Value | **Requires separate characterization** | Private array is copied then exposed, and ToString changes with contents. A record's synthesized array-field equality is not the byte-wise equality of Objects.PdfHexString; do not transfer that hash claim automatically. |
| `src/BlackFlag.Engine.Rendering/DisplayList/PdfDisplayTextRun.cs`, EncodedBytes | **No mutable alias to stored run bytes** | A new concatenation array is returned on each access; public mutation control passed. Underlying element aliases are a separate issue. |
| `src/BlackFlag.Engine.Document/Objects/PdfStream.cs`, ToByteArray/ReplaceData | **Mutable-by-design with defensive copies** | Explicit mutation API; both input and output snapshots copied. Serialization control passed. |

None of the five requested related views is an internal-only safe borrow. These observations do not authorize a repository-wide immutable-memory rewrite or claim all views have equal security importance.

## Exact probes and inspected paths

Four new test files, **16 deterministic cases**; no existing test or production file was changed:

| Added file | Cases |
|---|---:|
| `tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfCharacterCodeOwnershipCharacterizationTests.cs` | 2: public key mutation and CMap negative control |
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfDescriptorOwnershipCharacterizationTests.cs` | 7: constructor/metadata control; three valid-target failures; three stale-target/history paths |
| `tests/BlackFlag.Engine.Parsing.Tests/Loading/PdfStreamOwnershipCharacterizationTests.cs` | 2: constructor/filter/equality and actual cached ObjStm divergence |
| `tests/BlackFlag.Engine.Writing.Tests/PdfSourceRevisionOwnershipCharacterizationTests.cs` | 5: explicit Document copy control; array fallback; synchronous source mutation; two incremental xref modes |

The exact primary and related paths appear above. Additional focused source inspections and production-consumer searches covered the following paths (member/call-site review, not a claim of a new whole-file repository audit):

- `src/BlackFlag.Engine.Rendering/Fonts/PdfCMap.cs`, `PdfCMapParser.cs`, `PdfFontDecoder.cs`, `PdfSameFontTextEncoder.cs`, `PdfFontStreamDecoder.cs`.
- `src/BlackFlag.Engine.Services/Text/PdfExistingTextDiscoveryService.cs`, `PdfExistingTextDiscoverySource.cs`, `PdfExistingTextReplacementService.cs`, `PdfExistingTextFontService.cs`, `PdfExistingTextStyleService.cs`, `PdfExistingTextTypographyService.cs`, `PdfExternalExistingTextFontService.cs`, `PdfWidthAwareTextReplacementService.cs`, `PdfPositionedTextReplacementService.cs`, `PdfSharedFormExistingTextMutationService.cs`, `PdfExistingTextFindReplaceService.cs`, `PdfExistingTextSearchMatch.cs`, `PdfExistingTextEditingService.cs`, `PdfTextStringTokenEditor.cs`, `PdfTextStateTokenEditor.cs`, `PdfExistingTextStyleTokenEditor.cs`, `PdfExistingTextStyleUpdate.cs`.
- `src/BlackFlag.Engine.Editing/Documents/PdfEditableDocument.Text.cs`, `src/BlackFlag.Engine.Editing/Operations/PdfExistingTextEditOperation.cs`, `src/BlackFlag.Engine.Editing/Transactions/PdfEditTransaction.cs`, `src/BlackFlag.Engine.SDK.Adapters/Editing/PdfEditingSdkSession.Text.cs`.
- `src/BlackFlag.Engine.Parsing/Loading/PdfIndirectObjectResolver.cs`, `ObjectStreams/PdfObjectStreamParser.cs`, `CrossReferences/PdfCrossReferenceStreamParser.cs`, `PdfObjectParser.IndirectObjects.cs`, `Streams/PdfStreamParser.cs`, `Streams/PdfStreamParser.Data.cs`, `Resources/PdfResourceResolver.Fonts.cs`, `Resources/PdfResourceResolver.XObjects.cs`, `Resources/PdfResourceResolver.Values.cs`, `Resources/PdfResourceResolver.ColorSpaces.cs`, `Rendering/PdfNestedGraphicsProvider.cs`, `Security/PdfSecurityAnalyzer.AnalysisState.cs`.
- `src/BlackFlag.Engine.Rendering/Images/PdfImageDecoder.cs`, `Functions/PdfFunctionEvaluator.cs`, `Shadings/PdfShadingDecoder.cs`, `Resources/PdfResolvedFont.cs`, `Resources/PdfResolvedXObject.cs`, `Resources/PdfResolvedImageXObject.cs`, `Resources/PdfResolvedFormXObject.cs`, `Resources/PdfResolvedColorSpace.cs`; `src/BlackFlag.Engine.Filters/PdfFilterPipeline.cs`.
- `src/BlackFlag.Engine.Writing/IPdfOriginalSourceRevision.cs`, `PdfIncrementalUpdateWriter.cs`, `PdfCountingWriteStream.cs`, `PdfIncrementalWritePlanner.cs`, `PdfObjectSerializer.cs`, `PdfWritePlanner.cs`; `src/BlackFlag.Engine.Services/Editing/PdfEditingSaveService.cs`; `src/BlackFlag.Engine.Document/Objects/PdfDictionary.cs`, `Pages/PdfPage.cs`.
- Fixture/contract tests: `tests/BlackFlag.Engine.Services.Tests/Text/PdfFilteredTextTestDocument.cs`, `PdfExistingTextReplacementServiceTests.cs`, `PdfExistingTextFontServiceTests.cs`, `PdfExistingTextStyleServiceTests.cs`; `tests/BlackFlag.Engine.Parsing.Tests/Loading/PdfIndirectObjectResolverTests.cs`, `tests/BlackFlag.Engine.Parsing.Tests/TestRandomAccessSource.cs`; `tests/BlackFlag.Engine.Writing.Tests/PdfIncrementalUpdateWriterTests.cs`. Parsing test project references were read, not edited.

Bare filenames share the preceding directory; paths beginning with a subsystem directory are relative to the named project root. Full-repository searches for candidate types, member accesses, source-revision call sites and layer-specific stream consumers were reconciled with these focused inspections. Passthrough service/planner references do not imply every caller owns or accesses the backing bytes.

## Validation and scope closure

Targeted Release tests passed: Rendering **2**, Services **7**, Parsing **2**, Writing **5**. Complete affected projects passed: Rendering **89**, Services **300**, Parsing **197**, Writing **143**; zero failed/skipped. Targeted commands used `dotnet test tests/<project>/<project>.csproj --configuration Release --no-restore --filter FullyQualifiedName~OwnershipCharacterizationTests`. Complete affected suites used the same built Release assemblies with `--no-build --no-restore` and no filter.

One initial xUnit analyzer warning proposed replacing a HashSet.Contains assertion with collection enumeration. The test now names the actual hashed lookup result explicitly, preserving the invariant being tested without suppressing the analyzer. The final targeted Rendering build had no warning. No production behavior or existing assertion was changed to make characterization pass.

The **full unmodified release gate passed**, exit code zero: source-size validation (1,032 files), restore, build, **1,857 tests across 20 projects** with zero failed/skipped, documentation, repair preview and whitespace validation. DocGen validated **183 documents**, with zero errors/warnings/information; health **91.6% (A-)**. Repair preview proposed **zero changes**. The two existing source-size review notices (937-line graphics interpreter and 930-line sanitizer) are unchanged. Command:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

The exact nine changed paths are the four new tests listed above; this report; `docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md`; and normal DocGen outputs `docs/Documentation-Index.md`, `docs/Documentation-Health.md`, `docs/ValidationReport.md`. Final `git diff --check` and new-file whitespace checks passed. No finding is marked fixed. POST-P0-005/006, B4B/B4C, B5/B6, canonical font editing and other audit implementations remain untouched. Implementation requires separate approval; this pass stops after characterization/documentation/validation.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-09 | Independently characterize four B4A ownership candidates, positive/negative controls, proposed fix boundaries and validation; implement no ownership fix. |
