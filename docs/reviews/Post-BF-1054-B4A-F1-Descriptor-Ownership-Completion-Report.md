# Post-BF-1054 B4A-F1 - Existing-Text Descriptor Ownership

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-017 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-10 |
| **Next Review** | 2026-12-10 |

## Scope and baseline

The user authorized **B4A-F1 / POST-P0-012**, with additional corrections permitted only when directly exposed and necessary for descriptor ownership safety/completeness. The starting tree and diff were clean on `maintenance/post-BF-1054-repository-review` at **`379ce77955e4098f579fa785a1bf9f743aaf0137`**. The preceding B4A ownership characterization was already committed. Branch, status, recent history and diff were checked before implementation.

Root AGENTS.md, PROJECT-CHARTER.md, README.md, the [main audit](Post-BF-1054-CSharp-Repository-Audit.md), [ownership characterization](Post-BF-1054-B4A-Ownership-Candidate-Characterization.md), [B4A completion](Post-BF-1054-Batch-B4A-Sanitized-Font-Ownership-Completion-Report.md), [pre-B4 expression/materialization review](Post-BF-1054-Pre-B4-Expression-and-Materialization-Review.md), BF-1051 ownership/history and BF-1054 identity/direct-replacement/closure contracts governed this change. Historical milestone and characterization reports remain dated evidence of their respective checkpoints.

No branch switch, commit, push, merge or tag occurred. No dependency, project reference, friend access, public API signature, resource ceiling or policy changed. No unrelated expression modernization, new descriptor model, unsafe code, pooling, native storage or custom memory manager was introduced.

## Root cause and corrected invariant

The descriptor constructor already copied incoming ReadOnlyMemory into a private byte array. Its reference was readonly, but `EncodedBytes` returned array-backed ReadOnlyMemory over that same private storage. Public BCL APIs could mutate it after construction without updating UnicodeText, identity, typography or font metadata. Some consumers then treated the forged bytes as current-source provenance.

The original seven characterization cases passed on the starting implementation. The eleven revised cases all failed against the old public alias before production changes, including new typography and width-aware cases. Constructor copying and independent history storage were retained; the defect was later public exposure.

The constructor is unchanged. **Every EncodedBytes read now returns an independent snapshot**, using the array's explicit ToArray copying contract. Returned memory may still expose a writable array through TryGetArray/AsMemory, but that array belongs only to that returned snapshot. Mutation cannot change the descriptor, another previously returned snapshot, or subsequent reads. The public XML comment states that ownership contract. There is no public or internal borrowed-storage accessor for this descriptor.

Public construction remains available; descriptors are validation inputs rather than unforgeable capabilities. This fix prevents mutation of an already constructed descriptor through its returned memory. It does not redefine how a caller may construct a deliberately misleading new descriptor or establish a new provenance framework.

## Exact production changes

| File | Change and reason |
|---|---|
| `src/BlackFlag.Engine.Editing/Text/PdfExistingTextDescriptor.cs` | Return an independent EncodedBytes snapshot; update its public ownership documentation; retain constructor storage and all metadata |
| `src/BlackFlag.Engine.Services/Text/PdfExistingTextReplacementService.cs` | Read one descriptor snapshot for both independent display-run comparison and authoritative current-token validation; retain original source-run-before-descriptor evaluation order |
| `src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleService.cs` | Reuse one snapshot for expected/replacement bytes in the direct-target validation call |
| `src/BlackFlag.Engine.Services/Text/PdfExistingTextTypographyService.cs` | Reuse one snapshot for the equivalent direct-target validation call |

The other three direct byte consumers, PdfExistingTextFontService, PdfExternalExistingTextFontService and PdfWidthAwareTextReplacementService, already read the property once in their direct-target path and need no production change. Discovery constructs independently owned descriptors from run snapshots. Search retains descriptors and forwards them to the existing services. Positioned/composite paths consume display-run element segments instead; no descriptor storage is shared with those element arrays.

## Snapshot and resource accounting

Counts are copies of the descriptor's complete encoded text, excluding unrelated PDF stream buffers, token decoding/encoding and public caller copies.

| Boundary | Before | After |
|---|---|---|
| Descriptor constructor | 1 input copy | 1 input copy |
| Each public EncodedBytes read | 0 copies; private alias | 1 independent copy |
| Successful direct replacement | 2 property accesses, 0 descriptor copies | 1 property access/copy reused for both comparisons |
| Direct styling validation | 2 property accesses, 0 descriptor copies | 1 property access/copy |
| Direct typography validation | 2 property accesses, 0 descriptor copies | 1 property access/copy |
| Direct existing-font, external-font or width-aware mutation | 1 property access, 0 descriptor copies | 1 property access/copy |
| Discovery | Run concatenation plus constructor copy | Unchanged |
| History stream snapshots, encode/decode and save buffers | Existing independent copies | Unchanged |

### Duplicate-copy prevention in the primary consumer scope

Without the three Services adjustments, changing the property alone would introduce two O(n) copies in each of these operations. Source inspection proves the repeated property evaluations and their common logical source. Capturing one local removes one redundant full-text copy per affected operation relative to that naive implementation. No benchmark is needed to establish this allocation count; no throughput improvement over the old unsafe implementation is claimed. These adjustments are expressly required by F1's primary consumer scope, not silently added P0 findings.

The snapshots are local to a single call, not cached across operations. The direct replacement sourceBytes local also preserves the existing display-run materialization before descriptor access; it adds no source-run copy. Validation still uses the original independent display run and current authoritative token. No enumeration, token codec, cancellation branch, mutation callback or history algorithm was replaced.

Public isolation costs O(encoded-text-length) time and storage per exposure. **Measurement required:** large-run allocation pressure, repeated external property access, end-to-end latency/throughput and whole-workflow memory. No global cache/pool or visibility shortcut is justified by this change. Existing validation helpers' whole-stream temporary work is unchanged and is not attributed to the new descriptor snapshot.

## Exact tests and observed behavior

| File | Change |
|---|---|
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfDescriptorOwnershipCharacterizationTests.cs` | Convert the existing seven vulnerable-behavior cases into ownership regressions and add four typography/width-aware cases, for eleven cases total. The historical filename remains; comments/test names now describe the corrected invariant. |
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfDescriptorOwnershipIntegrationTests.cs` | Add nine cases: external-font ownership/history/save, stale rejection, cancellation, plus six filtered save cases for replacement/style/typography. Public fixture methods support supplemental parser reopen without new project references. |

All tests use deterministic synthetic content/fonts. No installed fonts or reflection/unsafe mutation is used. The constructor test supplies the same immutable typography object explicitly and checks its preservation alongside identity/font/Unicode/editability metadata. TryGetArray need not fail; when it succeeds, mutation must affect only the retained result. A second retained snapshot and later descriptor reads remain original after array and writable-memory attempts.

| Workflow | Valid target after attempted snapshot mutation | Stale target after matching a retained snapshot to new source |
|---|---|---|
| Replacement | Writes Other, matching the unmodified-input control | Rejects World source against unchanged Hello descriptor |
| Existing-font switch | Changes to F2 and keeps Hello, matching control | Rejects; the earlier World-to-stale-Hello overwrite no longer occurs |
| Style | Applies scoped red fill to Hello, matching control | Rejects without changing World |
| Typography | Applies font size 18 to Hello, matching control | Rejects without changing World |
| Width-aware replacement | Measures and writes Other with preserveAdvance=false, matching control; existing compensation coverage remains in the complete suite | Rejects without changing World |
| External-font switch | Embeds under allocated FExt2, preserves old FExt1, maps CID 1 to A and retains unrelated objects | Rejects authoritative B against unchanged A descriptor; object set/resources/content stay unchanged |

For direct/font/style/typography/width tests, successful operations are rolled back, then committed and replayed. Repeated mutation of the retained public result cannot change rollback, undo or redo bytes. Stale tests compare the exact InvalidOperationException message before/after the public mutation attempt and confirm no content change, including within a transaction. The external-font tests additionally compare complete object snapshots after rollback/undo/redo or failure and verify the history stack is unchanged on rejection. Pre-cancelled external-font mutation retains the exact OperationCanceledException token and original graph.

### Save and actual parser reopen

Six checked-in cases exercise replacement, styling and typography with traditional-table full rewrite and cross-reference-stream incremental output. They use the public Editing save facade and preserve the original incremental prefix byte-for-byte. The external-font case saves a full rewrite after resource rollback/undo/redo validation. This uses Writing directly because resource-change operations are outside the existing Editing save facade's supported change-set family; that pre-existing boundary was not broadened.

The supplemental `B4AF1Reopen.csproj` executable in the existing temporary audit workspace reuses those public fixtures and current built repository assemblies. **All seven actual parser reopen cases passed**. Checks include bounded xref/Prev traversal, one versus two revisions, content object `4 2 R`, original filter-array `/Fl` and decode-parameter representation, predictor 1, private parameter 42, private stream entry 23, encoded Length, exact decoded content and the untouched following Next text. The external case resolves old/new font references and decodes the saved CID through ToUnicode to A.

The temporary executable is supplemental validation, not a persistent release-gate test project. Reproduce with `dotnet run --project <scratch>/B4AF1Reopen.csproj --configuration Release --no-restore`; its checked-in input fixtures are listed above.

### Supplemental decoder limitation observed

The first reopen harness attempted to use PdfFontStreamDecoder directly for the page-content fixture's abbreviated `/Fl` filter. Its generic registry rejected that alias with PdfFilterDecodeException. The corrected harness first verifies the saved `/Fl` representation, then uses the existing bounded pipeline with canonical FlateDecode and the saved parameters, as the earlier content-mutation reopen probes do. All seven cases then passed. This is a pre-existing generic font-decoder alias compatibility limitation, not caused by descriptor ownership and not required to close POST-P0-012. It remains outside F1; the result does not claim every rendering consumer accepts every preserved abbreviated filter representation. No filter registration or security limit was altered.

## Compatibility, SDK and expanded-authorization review

Public signatures, constructor validation, identity/owner/page/span/session checks, Unicode/font metadata, current-source validation, serialization, transactions and exception contracts remain unchanged. The intentional observable change is memory alias identity: successive public byte results are independent, so mutating one no longer changes descriptor-dependent decisions. As with other defensive-copy APIs, allocating the public snapshot can fail under exhausted managed memory; no new domain rejection or policy limit was added.

Direct replacement retains its extra independent display-run comparison. Font/style/typography keep their established validation paths. No broad exception wrapping or suppression was added. Source-run materialization order is retained; style/typography only reuse a property snapshot around immutable identity reads. Existing-text history retains separate previous/replacement stream arrays rather than a descriptor byte alias.

SDK adapter production is unchanged. Inspection confirmed that registrations privately retain descriptors while targets expose opaque IDs and string/typography metadata, not raw bytes. The complete **34-test SDK.Adapters project passed**, including existing source registration, hidden provenance/interface boundaries, mutation invalidation, undo/redo invalidation, same-source re-registration and public workflow tests. Editing's complete 133 tests and Services' complete 313 tests also passed.

**Additional directly exposed issues fixed under expanded authorization: none.** The reviewed adjacent mutable views do not alias the descriptor's constructor-owned bytes and are not needed to enforce this returned-snapshot invariant. Existing public descriptor construction is explicitly retained by the task. No demonstrated unresolved issue in the modified path can mutate this owned snapshot through an EncodedBytes result. Separate provenance/stream/key findings must not be described as fixed by F1.

## Validation and final disposition

Focused ownership regressions: **20 passed**. Complete directly affected projects: **Editing 133**, **Services 313**, **SDK.Adapters 34**, all with zero failed/skipped tests. Targeted/affected builds reported no warnings or errors. Supplemental actual parser reopen: **7 passed**.

Commands used `dotnet test tests/<project>/<project>.csproj --configuration Release --no-restore`; the focused filter was `FullyQualifiedName~PdfDescriptorOwnership`. Services' full run reused the freshly built assemblies with `--no-build`. No tests or analyzers were weakened or suppressed.

The **full unmodified release gate passed**, exit code zero: **1,870 tests across 20 projects**, zero failed/skipped tests, and **zero build warnings/errors**. Source-size validation passed for 1,033 files; the existing 937-line graphics interpreter and 930-line sanitizer review notices remain unchanged. Restore, build, tests, documentation, repair preview and whitespace checks all passed. DocGen validated **184 documents** with zero errors/warnings/information; health was **91.6% (A-)** and repair preview proposed **zero changes**. Command:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

Documentation changes are this report, the main audit disposition recorded after successful validation, and normal DocGen refreshes of `docs/Documentation-Index.md`, `docs/Documentation-Health.md` and `docs/ValidationReport.md`. Together with four production and two test files above, the exact final change set is eleven files. Final `git diff --check` and new-file whitespace validation passed. Historical milestone and characterization reports and the release-gate script remain unchanged.

POST-P0-011/013/014, POST-P0-005/006/007/008/009, the related Rendering/Objects/Core provenance views, B4B/B4C, later maintenance batches and canonical font editing remain untouched. Stop after F1 validation and documentation; further implementation requires explicit approval.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-10 | Correct descriptor public ownership, prevent duplicate consumer snapshots, invert characterization regressions, validate external-font/history/save/SDK behavior and document scope/compatibility. |
