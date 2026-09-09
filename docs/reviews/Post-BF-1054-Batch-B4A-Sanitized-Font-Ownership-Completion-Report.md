# Post-BF-1054 Batch B4A - Trusted Sanitized-Font Ownership

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-015 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053, BF-1054 |
| **Last Updated** | 2026-09-09 |
| **Next Review** | 2026-12-09 |

## Scope and baseline

The user authorized **B4A only**, implementing **POST-P0-004** from the [current audit](Post-BF-1054-CSharp-Repository-Audit.md). The starting tree was clean on `maintenance/post-BF-1054-repository-review` at **`998e427e8d8fe0cdec094afaeb9f341f1aab90f3`**. The updated root AGENTS.md and expression/materialization guidance were read alongside the charter, README, engineering/security architecture, memory architecture, ADR-0010, trusted-font BF-1054I and embedding/preservation contracts, B3/B3A context and the [pre-B4 review](Post-BF-1054-Pre-B4-Expression-and-Materialization-Review.md).

This is an in-process integrity/ownership fix. It is not process isolation, memory secrecy, guaranteed erasure or protection against arbitrary reflection/unsafe/native memory corruption. B4B/B4C, B5/B6, canonical font editing and other findings remain untouched. No public signature, dependency, project reference, resource limit, permission policy, transaction or serialization algorithm changes. No commit, push, merge, tag or branch switch was performed.

## Root cause and pre-fix characterization

`PdfSanitizedExternalFontProgram` already copied its internal constructor's byte-array input using `ToArray()`, copied/sorted table-tag input at the sanitizer boundary, and computed uppercase SHA-256 once from its private copied array. The sanitizer is the only production constructor caller: bounded input is validated and canonically reserialized before construction.

The defect was subsequent exposure. `Bytes` returned array-backed `ReadOnlyMemory<byte>` over that trusted private array. `MemoryMarshal.TryGetArray` could recover and mutate it; `MemoryMarshal.AsMemory` could likewise obtain a writable view. Recorded SHA-256 did not change when content changed. The constructor's original defensive copy did not protect these later aliases.

`OpenRead()` returned `new MemoryStream(bytes, writable: false)`. This blocked writes and ordinary buffer retrieval but was still insufficient: `CopyTo`, `CopyToAsync`, and concrete `MemoryStream.WriteTo` handed the source buffer to caller-supplied destination methods. A destination could retain and later mutate that array. A previously opened independent reader then observed corrupted trusted data.

Before production edits, seven focused cases produced **four failures and three passes**. Public byte mutation and all three stream-copy attacks failed; input ownership, ordinary position/disposal/read-only behavior and pre-cancellation passed. These tests assert content/hash and isolation, not a mandated stream implementation type. A missing xUnit import was corrected before running characterization. An unused read-result analyzer warning was resolved by using an exact-read cancellation test, without suppression.

## Ownership model and compatibility

The constructor remains unchanged: one defensive byte-array copy and one hash computation after ownership is established. Private trusted `byte[]` storage never leaves the object through a public view.

- **Bytes:** each call returns an independent defensive snapshot. A public result may still be array-backed and mutable through BCL APIs, but its mutation cannot change later reads, existing independent readers, SHA-256 or downstream consumers. Returning a fresh snapshot, rather than caching a shared public snapshot, protects retained-view behavior.
- **OpenRead:** each reader is a non-writable, seekable MemoryStream over its own defensive snapshot. Positions and disposal are independent. The existing concrete stream type and buffer-visibility/write/seek semantics remain. A hostile destination may affect its own reader's copy, but never the trusted program or another reader.
- **TrustedBytes:** an assembly-internal `ReadOnlySpan<byte>` borrows the private array for trusted synchronous consumers. Fonts has no friend-assembly exposure for this path. Neither inspectors nor subsetters pass it to public callbacks or return it through a public result. This is a narrow assembly contract, not a claim that ReadOnlySpan itself resists arbitrary low-level mutation.
- **Inspectors:** metadata and TrueType inspectors borrow the internal span. Their public outputs contain parsed values, read-only copied metrics and glyph-mapping delegates that own copied cmap data; no trusted full-font array escapes.
- **Subsetters:** retain their existing independent full-font working array by copying the internal span once. This is necessary for their existing captured table-rebuild processing; no root/closure/output-bound algorithm was changed.
- **Services projectors:** use one public `ReadOnlyMemory<byte>` snapshot for both `/Length1` and `PdfStream` construction. The document stream still makes its own copy. Services gains no friend access or trusted-buffer API.

Public signatures, SHA-256 formatting and sanitizer/subsetter/embed exception contracts are unchanged. The intentionally corrected observable behavior is alias identity/mutation: successive Bytes results no longer share storage, and streams no longer share trusted backing bytes. Public snapshot allocation can increase memory pressure; no new validation/rejection branch was added for valid fonts. Cancellation, embedding rights, deterministic font output and preservation behavior remain as before.

## Exact production changes

| File | Change |
|---|---|
| `src/BlackFlag.Engine.Fonts/Sanitization/PdfSanitizedExternalFontProgram.cs` | Defensive Bytes/OpenRead snapshots; internal borrowed read-only span; concise ownership comments and public snapshot documentation |
| `src/BlackFlag.Engine.Fonts/PdfSanitizedExternalFontMetadataInspector.cs` | Borrow TrustedBytes instead of the public snapshot |
| `src/BlackFlag.Engine.Fonts/Embedding/PdfExternalTrueTypeInspector.cs` | Borrow TrustedBytes instead of the public snapshot |
| `src/BlackFlag.Engine.Fonts/Embedding/PdfTrueTypeGlyphSubsetter.cs` | Retain one working copy from TrustedBytes |
| `src/BlackFlag.Engine.Fonts/Embedding/PdfUnicodeTrueTypeGlyphSubsetter.cs` | Retain one working copy from TrustedBytes |
| `src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeResourceProjector.cs` | Reuse one defensive snapshot for length and font-stream construction |
| `src/BlackFlag.Engine.Services/Fonts/PdfExternalTrueTypeSubsetResourceProjector.cs` | Reuse one defensive subset snapshot for length and font-stream construction |
| `src/BlackFlag.Engine.Services/Fonts/PdfExternalType0TrueTypeResourceProjector.cs` | Reuse one defensive Unicode subset snapshot for length and font-stream construction |

All eight production paths directly establish or preserve efficient use of the same trusted-font invariant. Subsetter changes are one-line source-access changes, not B4B. Sanitizer validation and font rights were not edited. Existing `ToArray` copies were retained for their concrete copying contracts; no syntax modernization was applied. Explicit `fontBytes` locals name the owned public snapshot and prevent duplicate property evaluation.

## Allocation review

Counts below are full-program copies at the identified boundary, excluding table-sized copies, PDF serialization buffers, caller copies, stream-growth reallocations and object headers.

| Boundary | Before | After |
|---|---|---|
| Constructor from canonical sanitizer output | 1 | 1 |
| Each public Bytes access | 0, unsafe alias | 1, independent snapshot |
| Each OpenRead call | 0, shared trusted buffer | 1, independent reader buffer |
| Metadata inspection | 0 full-font copies | 0 full-font copies |
| TrueType metric inspection | 0 full-font copies | 0 full-font copies |
| Each subsetter's initial working input | 1 | 1; avoids a second public-snapshot copy |
| Each full/subset/Type0 embedding stream boundary | 1 Document.PdfStream copy | 2: public snapshot plus Document.PdfStream copy |

Sanitizer bounded-read materialization, canonical SFNT allocation, constructor copy and hash computation are unchanged. Subsetter table copies, glyph/cmap rebuilding, output construction and resanitization remain unchanged. Save buffers and reopened parser/sanitizer ownership are unchanged. The three projectors avoid copying an entire font merely to read Bytes.Length, then copying it again for Bytes.Span.

This design preserves zero-copy trusted inspection without a new program abstraction, custom stream, memory manager, pooling, pinning, unsafe/native storage or dependency. Public isolation costs an O(n) copy per exposure. **Measurement required:** peak memory, repeated public access and large-font end-to-end latency; no benchmark or throughput improvement is claimed. A public stream wrapper that eliminates its defensive copy would require a larger, separately justified audit of all read/copy/callback paths. Ownership is not weakened to avoid that cost.

## Tests and downstream evidence

| File | Coverage |
|---|---|
| `tests/BlackFlag.Engine.Fonts.Tests/PdfSanitizedExternalFontProgramOwnershipTests.cs` (new) | Seven cases: caller-input mutation after sanitization; retained public array/writable-memory attacks; independent positions/seek/EOF/disposal; read-only writes and buffer retrieval; synchronous/asynchronous copy and WriteTo destination attacks; cancellation; independent SHA-256 recomputation |
| `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfSanitizedFontOwnershipIntegrationTests.cs` (new) | Six combinations: full, simple subset and Type0 Unicode embedding, each with table/stream xrefs; retained public memory mutation before and after consumers; stable metadata, rune coverage, metrics, rights, deterministic subset bytes, `/Length1`, hash, retained reader and resource collision identity; save fixture exposed to supplemental reopen executable |
| `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfExternalType0TrueTypeResourceProjectorTests.cs` | Existing deterministic SFNT builder accepts an optional name table; defaults unchanged. Supplies a valid checksummed family-name fixture without system fonts or a duplicate SFNT builder |

The existing font fixture had an empty name table suitable for its writer tests; initial metadata integration runs rejected it before the mutation attempt. The optional name table corrected the test fixture rather than weakening the metadata inspector. The existing large test file was reviewed for cohesion: this two-line fixture extension preserves its font-builder responsibility without growing it or requiring an unrelated decomposition.

Supplemental **actual parser reopen: six combinations passed**. The temporary `B4AReopen.csproj` uses existing built repository assemblies and the checked-in Services fixture, following the B3/B3A cross-project validation approach without changing repository project references. It parses saved table/stream xrefs, resolves page resource `FExt2` and its font/descendant/descriptor/FontFile2 chain, verifies the old `FExt1` identity/generation, checks exact pre-mutation expected font bytes and Length1, parses Type0 ToUnicode, and resanitizes the reopened font to identical bytes/SHA-256.

| Output | Font bytes | SHA-256, identical for both xref representations |
|---|---:|---|
| Full | 1,300 | `C364857A62DEC149E4175CD5E43375383D3E3D13E450129D938F448077D37D40` |
| Simple subset | 620 | `DB3C18D7CDA8BB5A3A565CD5E9EC57428DE22E718DEF1BCD2DE0690E64F99B52` |
| Unicode subset | 900 | `ED2781F72F70F637D51CE459155383EB5DB7219078F87EC9686B66F11158A0AD` |

Existing targeted external-font integration and complete Services tests retain rollback, cancellation, undo/redo, collision and incremental-save coverage. Fonts.Platform production was unchanged, but its complete synthetic-provider suite was run because metadata inspection uses the changed ownership path. SDK adapter production was unchanged; its full project is covered by the release gate.

## Narrow analogous-view audit - separate approval required

The full-repository search covered trusted-program construction, Bytes/OpenRead consumers, MemoryMarshal use and array-backed provenance views. There are no production OpenRead callers and no production construction outside the sanitizer. Metadata inspection, TrueType inspection, both subsetters and the three projectors account for direct trusted-program byte consumers; platform discovery and external-text services call these boundaries indirectly.

The following **static ownership candidates were not fixed or counted as completed audit findings**. They need independent behavioral/preservation characterization and approval. Their scope is broader than the font trust object even where the array-exposure pattern looks similar.

| Candidate | Evidence and possible impact | Required next validation |
|---|---|---|
| B4A-AN-01, P0 candidate: `src/BlackFlag.Engine.Editing/Text/PdfExistingTextDescriptor.cs`, `EncodedBytes` | Claims immutable provenance, copies constructor input, then exposes the private array as ReadOnlyMemory. Caller mutation can change the descriptor's byte snapshot. | Characterize current-source comparison, stale-target rejection, history and replay before choosing a fix |
| B4A-AN-02, P0 candidate: `src/BlackFlag.Engine.Writing/PdfMemorySourceRevision.cs`, `CopyTo` | Claims an immutable preceding revision but passes a borrowed span to a caller-owned Stream.Write override. A read-only span is not itself an integrity boundary against adversarial low-level managed ref access. | Deterministic destination-mutation probe and original-prefix/incremental-save preservation coverage |
| B4A-AN-03, P0 candidate: `src/BlackFlag.Engine.Objects/PdfStream.cs`, `Data` | Claims an immutable parser stream snapshot but returns its private array as ReadOnlyMemory. | Alias probe plus parsed-stream/cache and preservation tests; distinguish from the mutable Document.PdfStream model |
| B4A-AN-04, P0 candidate: `src/BlackFlag.Engine.Rendering/Fonts/PdfCharacterCode.cs`, `Bytes` | Claims immutable codes, exposes a private array, and computes equality/hash from those bytes. Mutation may invalidate dictionary-key invariants. | Alias mutation after CMap insertion and code lookup/equality tests |
| Related provenance views: Rendering `PdfTextRunElement.EncodedBytes`, `PdfDecodedGlyph.SourceCode`, `PdfInlineImage.EncodedBytes`; Objects `PdfHexString.Value`; Core `PdfString.Value` | Private arrays exposed through read-only memory; relationship to trusted/provenance consumers varies. | Assess each ownership contract and caller graph; do not mechanically copy every ReadOnlyMemory property |

Document-model PdfStream/PdfString defensive copy APIs do not expose the same array alias. Mutable image/surface buffers and caller-owned embedded-content inputs were not automatically equated with sanitizer trust. No general immutable-memory refactor or secret-buffer clearing was performed. The existing governing ownership guidance is sufficient; no additional general policy change is proposed in B4A.

## Validation and final disposition

Release targeted ownership tests: **7 passed**. Targeted Services font/external-font integration: **81 passed**, including the six new cases. Complete affected projects: **Fonts 28**, **Fonts.Platform 3**, **Services 293** passed, with no failed/skipped tests. Supplemental parser reopen: **6 passed**. `git diff --check` passed.

Commands used `dotnet test tests/<project>/<project>.csproj --configuration Release --no-restore`; targeted filters were `FullyQualifiedName~PdfSanitizedExternalFontProgramOwnershipTests` and `FullyQualifiedName~.Fonts.|FullyQualifiedName~PdfExternalFontCollisionIntegrationTests`. The supplemental executable ran with `dotnet run --project <scratch>/B4AReopen.csproj --configuration Release --no-restore`.

The full unmodified release gate **passed**: **1,841 tests across 20 projects**, no failed/skipped tests, and zero build warnings/errors. Source-size validation passed for 1,028 files; the existing 937-line graphics interpreter and 930-line sanitizer review notices remain unchanged. Restore, build, tests, documentation, repair preview and whitespace checks all passed. DocGen validated **182 documents** with zero errors/warnings/information; health was **91.6% (A-)**, and repair preview proposed **zero changes**.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

Documentation changes are this completion report and `docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md` (POST-P0-004 disposition recorded after the gate passed). Normal DocGen refresh updates `docs/Documentation-Index.md`, `docs/Documentation-Health.md` and `docs/ValidationReport.md`. Together with the eight production and three test paths listed above, these are the exact sixteen changed files. Historical milestone documents and the release-gate script are unchanged.

Remaining limitations are the measured-cost work identified above, the separately reported analogous candidates and existing unimplemented audit findings. Supplemental cross-project reopen is an external validation executable, not a new persistent project dependency or release-gate check. B4A does not establish a process sandbox or protection against code that bypasses managed accessibility. B4B/B4C and later batches require explicit approval.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-09 | B4A ownership correction, public alias/stream characterization, trusted consumer review and subset/embed/reopen validation. |
