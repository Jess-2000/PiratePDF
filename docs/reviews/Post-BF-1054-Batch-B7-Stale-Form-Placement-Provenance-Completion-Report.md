# Post-BF-1054 Batch B7 - Stale Form Placement Provenance

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-026 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-21 |
| **Next Review** | 2026-12-21 |

## Scope and baseline

B7 addresses POST-P0-009 and the directly exposed owner, binding and lexical-context variants at the same Form placement clone boundary. Starting branch: `maintenance/post-BF-1054-repository-review`. Starting HEAD: **13e8fb72cadb9b9861f1814c3cbdf5215d11cad6**, Enforce external font embedding rights. B6 was committed and initial status/diff were clean. HEAD and branch remain unchanged. No unrelated user edits were found on the final scope refresh.

Root AGENTS.md, PROJECT-CHARTER.md, README, the current audit/B6 report, BF-1054B/N/O/P contracts, BF-1051D/E transactions, BF-1053A/K preservation/projection contracts, B1/B3/B5 reports, engineering standards and ADR-0009/0010 informed this implementation. The descriptor actually resides in Services, not Editing. The authoritative consolidated release gate is unchanged; Test-PiratePDFSourceFileSizes.ps1 remains intentionally absent.

**Disposition: POST-P0-009 is implemented and closed. The full unmodified release gate passed 2,378 tests across 20 projects, with zero build warnings/errors, integrated source-size validation and clean DocGen. All fifteen currently recorded P0 findings are now closed.** POST-P0-005/006/007/008/015 remain closed. P2 and measurement-required work remains; no P2 work is included.

## Reproductions before production edits

Six permanent tests failed independently against unchanged production: four source variants, changed binding, and wrong owner. A seventh lexical-context regression also failed before production correction.

The original fixture has page **3 0**, content **4 0**, Form **5 0**, direct Resources/XObject mapping **X1 -> 5 0 R**, and decoded content `/X1 Do\n/X1 Do`. The first descriptor records operand X1 and span **[0,6)**. Current content was then replaced without changing the descriptor or resource binding.

| Current content before call | Observed pre-fix projected content | Evidence |
|---|---|---|
| `0 0 m\n/X1 Do` | `/X2 Do/X1 Do` | Original audit corruption: move operation overwritten |
| `0 0 m ` | `/X2 Do` | Same-length unrelated replacement, valid bounds |
| `q\n/X1 Do\n/X1 Do` | `/X2 DoDo\n/X1 Do` | Shifted placement and identical surviving occurrence |
| `/X2 Do\n/X1 Do` | `/X2 Do\n/X1 Do` | Changed operand was silently authorized |

Each pre-fix call returned clone **6 0**, allocated name **X2**, and three projections (page, content, clone). The live object store still contained five objects because projection preparation is pure; applying those projections would publish the corruption. No pre-fix live mutation is claimed.

Changing X1 to another indirect Form while leaving source bytes unchanged also succeeded incorrectly. Supplying page 6 with a discovery source owned by page 3 also succeeded, even though its copied page dictionary merely happened to match. Finally, changing `q /X1 Do Q` to `( /X1 Do )` preserves the exact [2,8) bytes but changes them into string content. This demonstrates why a tiny span comparison alone cannot establish lexical context after arbitrary stream replacement.

## Provenance and compatibility

Before B7, the private descriptor constructor retained only page/content/Form identifiers, resource name and start/end offsets. Its public factory received caller-supplied canonical operations and a display Form, but no document, resource dictionary or source bytes. It did not compare the supplied page identifier to the discovery source's owner. There are no production callers of clone projection in this baseline; the public projector and descriptor are composed by callers, and the repository callers were tests.

B7 adds a **document-aware Create overload**. It validates the existing canonical operation/display-span mapping and source owner, then captures authoritative provenance internally. Retained state is document identity; page, physical content container/stream and resource-owner/container object identities; the resolved Form object; physical stream index; one private defensive copy of the exact decoded operation span; stream data revision; and the supported decoded representation category. Full object number and generation remain part of every indirect binding comparison.

The old three-argument factory and all existing public signatures remain available. It produces identity metadata, but has insufficient provenance to authorize mutation: clone projection now fails closed with InvalidOperationException and asks for rediscovery using the document-aware overload. Callers must migrate their discovery call to that overload; they do not manually populate new fields. This is an intentional behavioral compatibility restriction, not a claim that all old factory-produced descriptors still mutate successfully. The private constructor remains private, and no retained byte buffer is publicly exposed.

PdfStream gains an additive read-only **DataRevision** property. Successful ReplaceData advances an instance-local checked counter after preparing the owned replacement buffer. Constructor ownership and ToByteArray defensive snapshots are unchanged. Counter overflow fails before assignment rather than wrapping; allocation failure cannot publish a new revision or bytes. This property is not serialized into PDF dictionaries, is not a document-wide registry, and does not track dictionary changes or provide concurrency synchronization.

The counter is necessary to reject unchanged target bytes whose surrounding lexical context changed, without retaining another whole-stream copy or scanning the stream during each mutation. Any intervening ReplaceData requires fresh discovery, including replacement with identical bytes or restoration by undo. Replacing the stream instance also invalidates provenance. Changing the original Form's data in the same bound Form instance does not invalidate a page placement: projection intentionally clones the current Form dictionary and the caller's supplied replacement program. Replacing the Form object instance at the same identifier does invalidate provenance.

## Exact validation and supported boundaries

Validation uses the authoritative Document object store, the page's current Contents, and its effective Resources through the actual parent chain. It does not use the high-level page's detached content snapshot. Direct/indirect Resources and XObject dictionaries and inherited resources resolve under the existing copy-on-write policy; equivalent replacement dictionaries reject because identity cannot be proved unchanged. Name aliases are distinct placements even when they resolve to the same Form. Direct Form values remain outside the indirect-Form cloning contract.

The current page must be a Page, the selected physical Contents entry must reference the recorded stream identifier, and its stream index must still match. Contents arrays, including an indirect array container, are resolved by physical index, not flattened offsets. Reordering/rebinding the selected entry rejects. Direct content streams and nested Form-owned placement editing remain unsupported. The existing factory rejects FormXObject discovery sources; B7 does not add nested clone editing.

Bounds require **0 <= start < end <= decoded length** before narrowing or slicing. No allocation uses an unchecked caller-provided span length. Negative/reversed PdfContentSourceSpan construction retains its existing argument exceptions; zero/extreme/out-of-range/noncanonical placement spans reject with InvalidOperationException. Exact-end valid spans succeed.

At capture, PdfFormPlacementSyntax recognizes the exact selected name/Do pair: decoded name bytes must equal the expected resource operand, followed only by legal PDF whitespace/comments and the exact Do operator, with a valid following token boundary. Name escapes retain their source spelling until the selected operation is intentionally replaced. Prefix inspection is a discovery-time lexical exclusion check using existing string/comment scanning and graphics operator definitions. It rules out a forged operation inside a string, comment, array/dictionary or pending operand. It does not build a second general content parser, search for another matching placement, or relocate a target.

**Explicit compatibility boundary:** Services has no Parsing dependency, and this batch forbids adding one. Capture after inline-image BI data rejects with NotSupportedException rather than guessing binary EI boundaries. Unknown prefix operators outside the recognized graphics vocabulary also fail closed. B7 adds no inline-image parser or general content grammar. These restrictions are documented and the inline-image rejection has a permanent control.

At mutation, current document/owner/container/resource/Form identities and complete bindings are resolved again. The shared mutation codec decodes once under its existing policy and limits. The data revision, filtered/unfiltered category and exact private span bytes must still match. Since revision and supported decode semantics preserve the capture-time lexical context, no full-stream search or second parse is needed. The same decoded buffer is passed into redirection. The synchronous operation assumes no concurrent external mutation; no concurrency system or validity cache is introduced.

All validation completes before clone identifier reservation, clone encoding, resource-name allocation or projection construction. Replacement names still use PdfObjectSerializer's PDF-name escaping and the actual collision-free name returned by PdfPageResourceMutationProjector. Redirection copies exact prefix/suffix bytes and replaces only the selected operation. It does not reserialize the content program.

## Filters, failure contracts and preservation

Unfiltered and single-Flate page and Form streams remain supported through PdfContentStreamMutationCodec. Filter name/array spelling, DecodeParms and Predictor restrictions remain unchanged; no decoder, budget, encoder or dependency changed. Codec validation remains authoritative before revision mismatch is reported. Thus a hostile current Flate stream still produces its established decode failure rather than being decoded through another path. Truncated/malformed Flate and expansion-budget controls publish nothing. The projector and descriptor have no cancellation-token parameter; B7 adds none and does not claim a new cancellation boundary. Existing codec cancellation regressions remain in the gate.

| Case | Before B7 | After B7 |
|---|---|---|
| Valid captured placement | Three pure projections | Same successful projection semantics |
| Stale bytes with valid bounds | Could overwrite current content | InvalidOperationException |
| Changed binding / wrong owner | Could project wrong placement | InvalidOperationException |
| Invalid numeric bounds | Bounds failure at redirection; zero span tolerated | Predictable capture-time InvalidOperationException; negative/reversed source-span argument failures unchanged |
| Malformed selected operator | Bounds alone could authorize it | InvalidOperationException |
| Missing authoritative snapshot | Not represented | InvalidOperationException before clone work |
| Malformed/truncated Flate | InvalidDataException | Same codec exception, no publication |
| Decode resource ceiling | PdfFilterDecodeException with failure category | Same codec contract |
| Inline-image prefix | No authoritative source check | Explicit NotSupportedException at capture |
| Resource-name collision | Deterministic next free name | Same; X1/X2 occupied produces X3 |
| Cancellation | No token on placement APIs | Unchanged; shared codec semantics retained |

Changed resources, content bindings, owners, Form instances and same-number/different-generation bindings are covered. Failure asserts exact serialized graph bytes, original object references/count and the next allocator result. The allocator belongs to each write-plan projection call; stale failure neither reserves an identifier nor inserts an orphan clone/resource. Later encoding failures remain pure projection failures. Existing resource allocation policy is unchanged.

## Transactions, history and B5/B6 controls

The public clone API prepares write projections; it does not itself publish an Editing operation. Calling it inside an otherwise empty transaction leaves both graph and history empty. A failed call after a valid document-information edit leaves that earlier edit intact until normal disposal rollback or commit/undo/redo. The rejected call never enters history.

Four additional tests apply a successful clone through the **existing internal ApplyExistingTextResourceChange primitive**, already visible to Services.Tests. They separate the projected content bytes from page/clone object replacements and exercise unfiltered/filtered commit, rollback, undo and redo. Exact complete graph serialization restores on rollback/undo and reappears on redo, including original Form, clone, canonical resource name and page bytes. No friend assembly, new transaction implementation or public clone-editing API was added. This is evidence for the existing publication primitive, not a claim that the projector acquired an automatic public history route.

B5's actual boundary remains intact: style Apply rejects Form and shared sources and does not offer clone-on-write styling. Its permanent Form/shared-source rejection and preservation tests pass. Existing filtered/shared-Form mutation controls also pass. B6 rights production is untouched; 83 B6 Services permission cases were included in the combined focused run in addition to full-gate coverage. No canonical font feature or conformance expansion is included.

## Save/reopen and normal discovery

Eight permanent writer cases cover success/rejection x unfiltered/Flate x traditional/xref-stream output. Successful projection is applied using PdfProjectedFullRewritePlanner, then serialized through PdfFullRewriteWriter. They verify exact unselected bytes, filter dictionary preservation, original Form bytes, clone program and original/clone bindings. A rejected call serializes the unchanged authoritative graph, without inventing a failed-output document.

The supplemental harness references already built repository assemblies and adds no repository project/reference. Exact command:

```powershell
dotnet run --project 'C:\Users\Sufan\AppData\Local\Temp\PiratePDF-B7-Validation\B7Reopen.csproj' --configuration Release --no-restore
```

All **8 actual parser reopen combinations** pass. PdfCrossReferenceParser or PdfCrossReferenceStreamParser plus PdfIndirectObjectResolver reopen the output; PdfContentStreamParser verifies Do operands/counts. The selected operation resolves to X2/clone, the untouched one to X1/original. Exact encoded page/original/clone bytes and decoded clone program survive; Fl spelling and Predictor 1 survive. Rejected outputs retain current bytes and have no X2 resource.

Two more supplemental controls use actual content parsing, resource resolution, PdfNestedGraphicsProvider and PdfGraphicsInterpreter to produce the discovery source/display Form. Plain and escaped-name/comment representations both create valid descriptors with the document-aware factory and redirect only the selected occurrence. These controls use Form generation 7. Supplemental acceptance total: **10**, separate from the permanent gate test count. Initial harness compilation/setup corrections were harness-only; no production contract was loosened to pass them.

## Exact changed files

### Production - five files

| File | Change |
|---|---|
| src/BlackFlag.Engine.Document/Objects/PdfStream.cs | Add instance-local data revision without exposing backing bytes |
| src/BlackFlag.Engine.Services/Text/PdfFormPlacementDescriptor.cs | Document-aware capture overload, owner check, private provenance and stream index; legacy factory retained |
| src/BlackFlag.Engine.Services/Text/PdfFormPlacementProvenance.cs | New authoritative owner/binding/span snapshot and validation |
| src/BlackFlag.Engine.Services/Text/PdfFormPlacementSyntax.cs | New narrow selected-Do recognizer and discovery lexical-context guard |
| src/BlackFlag.Engine.Services/Text/PdfFormPlacementCloneProjector.cs | Validate before clone work and reuse the same decoded content snapshot |

### Permanent tests - six files

| File | Change |
|---|---|
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFormPlacementCloneProjectorTests.cs | Preserve four existing controls, update discovery helper and capture budget-test provenance before hostile replacement |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFormPlacementCloneProjectorTests.Provenance.cs | Initial stale/owner/binding/context failures and malformed/truncated Flate |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFormPlacementCloneProjectorTests.Resources.cs | Direct/indirect/inherited binding, generations, aliases, owner/container replacement and exact graph assertions |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFormPlacementCloneProjectorTests.Syntax.cs | Source variants, hostile spans, malformed/embedded targets, adjacent placements, stream array identity and supported boundaries |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFormPlacementCloneProjectorTests.Transactions.cs | Existing publication primitive rollback/replay, caller transaction preservation and snapshot ownership |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfFormPlacementCloneProjectorTests.Writing.cs | Eight real graph serialization controls and supplemental fixture entry point |

### Documentation - five files

- docs/reviews/Post-BF-1054-Batch-B7-Stale-Form-Placement-Provenance-Completion-Report.md
- docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md
- docs/Documentation-Index.md
- docs/Documentation-Health.md
- docs/ValidationReport.md

Verified final scope: **16 files: 5 production, 6 tests, 5 documentation**. Historical reports are unchanged.

## Validation

| Check | Result |
|---|---|
| Form-placement focused suite | 74 passed: 70 new cases plus 4 existing cases |
| Combined Form, filtered mutation, B5 preservation and B6 Services rights filter | 196 passed |
| Complete Services.Tests | 524 passed |
| Complete Editing.Tests | 133 passed |
| Complete Writing.Tests | 153 passed |
| Complete Parsing.Tests | 219 passed |
| Complete Document.Tests | 62 passed |
| Complete Rendering.Tests | 97 passed |
| Complete SDK.Adapters.Tests | 35 passed |
| Supplemental actual reopen / normal discovery | 8 / 2 passed |
| Release solution build | 0 warnings, 0 errors |
| git diff --check | Passed before acceptance gate |
| Full unmodified release gate | Passed, exit 0; 2,378 tests across 20 projects, zero failed/skipped |
| Integrated source-size check | Passed, 1,066 files; all changed source/test files below review range |
| DocGen generation/health/repair | 193 documents, 0 errors/warnings/information; health 91.6% A-; repair 0 changes/files/diagnostics |

Focused and affected runs used Release configuration, FullyQualifiedName filters where applicable, then complete project --no-build runs after a clean solution build. No test was weakened or removed. No source-size split was required: the new files have cohesive provenance and syntax responsibilities, and all changed source/test files remain below the decomposition review range.

The exact acceptance command was `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1`. The first sandboxed invocation passed source-size checks but could not read the user's NuGet.Config; the unchanged command was rerun with approved access and passed every stage. No gate stage was bypassed. The two existing 800-line review entries (graphics execution 943, sanitizer 844) were unchanged. After recording acceptance, documentation validation/health/repair and git diff --check were rechecked.

## Cost, necessary corrections and remaining work

The exact operation snapshot is one bounded private copy with no public exposure. Decode remains governed by existing byte/expansion limits. Discovery additionally decodes the selected stream and inspects its prefix once; mutation performs the already-required decode, scalar revision checks, an exact target-span comparison and resource-chain lookups. Prefix scanning never occurs during mutation and no target relocation/search or whole-document reparse was introduced. Resource resolution retains cycle protection and is bounded by the current object graph. The existing resource projector resolves again when building its copy-on-write projection; B7 does not restructure that subsystem.

Each PdfStream gains one counter. Descriptors retain owner/object references, so their lifetime can retain the associated document; callers should release obsolete descriptors. No global cache, pool, unsafe/native code, pinning, custom memory manager or cryptographic hash was added. All throughput/latency and aggregate retention claims remain **measurement required**; no speedup is claimed.

The directly exposed P0 corrections are source bytes, effective resource binding, wrong owner and unchanged span inside changed lexical context. Their reproductions, production files, tests and additive/behavioral API impact are recorded above. The data revision is the narrow cross-layer addition required to avoid a second retained whole-stream snapshot or repeated prefix scanning. No NuGet/native/runtime dependency, project reference or friend assembly changed.

POST-P2-001 through POST-P2-008, canonical font editing, global descriptor audits and broader Form architecture remain untouched. Broader stale-span fuzzing belongs to POST-P2-002; only deterministic tiny malformed cases were added. B1-B6 and ownership/F findings remain intact. No claim of zero remaining engineering/security work is made. No commit, push, merge, tag or branch switch was performed. Stop after B7 acceptance.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-21 | Record B7 stale Form provenance correction, explicit compatibility boundaries and validation evidence. |
