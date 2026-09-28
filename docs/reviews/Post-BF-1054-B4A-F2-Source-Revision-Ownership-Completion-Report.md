# Post-BF-1054 B4A-F2 Source-Revision Ownership Completion Report

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-018 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1051, BF-1053, BF-1054 |
| **Last Updated** | 2026-09-10 |
| **Next Review** | 2026-12-10 |

## Scope and baseline

The user approved B4A-F2, POST-P0-014, with narrow expanded authorization for necessary directly exposed defects. Work started on `maintenance/post-BF-1054-repository-review` at **`558b420961193161eee4a126c873bf28f5a9f760`**, `Harden existing text descriptor ownership`. F1 was committed; the starting working tree and diff were clean. Branch, status and recent history were checked before production edits.

Governing context: root AGENTS.md, PROJECT-CHARTER.md and README.md; Blueprint architecture/engineering/security principles; memory architecture and ADR-0010 bounded materialization; BF-1051A ownership and BF-1051E history; BF-1053E/F/G original revision and incremental output, BF-1053AJ public save; BF-1054A/V preservation contracts; the [main audit](Post-BF-1054-CSharp-Repository-Audit.md), [ownership characterization](Post-BF-1054-B4A-Ownership-Candidate-Characterization.md), [B4A report](Post-BF-1054-Batch-B4A-Sanitized-Font-Ownership-Completion-Report.md), [F1 report](Post-BF-1054-B4A-F1-Descriptor-Ownership-Completion-Report.md) and [pre-B4 expression review](Post-BF-1054-Pre-B4-Expression-and-Materialization-Review.md). Historical documents remain unchanged.

## Original mechanism and evidence

PdfMemorySourceRevision already copied constructor input into a private readonly byte array and exposed immutable Length, CrossReferenceOffset and CrossReferenceMode metadata. CopyTo subsequently called `destination.Write(bytes)`, which selects the overridable synchronous ReadOnlySpan overload. The readonly span did not establish an ownership boundary.

The existing ordinary managed destination copies the callback span to its output, then assigns through `MemoryMarshal.GetReference(buffer)` while the span is valid. The first output can therefore remain correct even though retained source state changes. A later CopyTo returned different bytes and SHA-256; a later incremental save using the same revision started with X instead of the original percent sign. The counting wrapper forwarded that alias. Both traditional-table and xref-stream incremental paths reproduced the defect. This requires in-process callback access; no PDF-only exploit, isolated-plugin escape or legal post-return retention of a source span is claimed.

All five original characterization cases passed before changes. After inversion and boundary additions, the old production implementation failed twelve of fifteen cases; three controls passed. The failures included source mutation, both incremental prefixes and the newly required callback granularity/failure behavior. No existing characterization case was deleted. The parser-stream bridge control is deliberately unchanged because POST-P0-013 remains separate.

## Corrected ownership and chunk contract

Only **src/BlackFlag.Engine.Writing/PdfMemorySourceRevision.cs** changes in production. Constructor copying, input validation and metadata remain unchanged. CopyTo validates the destination as before, then allocates one ordinary **65,536-byte (64 KiB) array**. Each iteration copies source bytes into that independent array and invokes the destination's span overload with only the copied portion. The array is reused within that call and never shared between CopyTo calls or pooled. Retained revision storage is never passed to destination callbacks.

The fixed size bounds added source-isolation storage independently of hostile revision size and permits useful bulk writes without a whole-revision clone. It is an engineering bound, not a measured optimum. Even a one-byte valid revision allocates that fixed buffer; no size-dependent policy or public tuning parameter is introduced. There is no unsafe code, pinning, native memory, memory manager, borrowed-memory accessor, dependency or project-reference change.

For source length L greater than zero, callbacks occur in increasing source-offset order, each at most 65,536 bytes, with exactly `ceil(L / 65,536)` callbacks. An exact multiple has no extra empty callback. A final partial chunk exposes exactly its copied count; stale buffer tail bytes are never written. Empty revisions remain rejected by the constructor with the same ArgumentException. A destination may alter its own supplied temporary span or reject output, but cannot change future source copies.

Offset/count arithmetic is structurally bounded: offset starts at zero; while offset is less than the int-sized retained array length, remaining `bytes.Length - offset` is positive and cannot overflow. Count is the smaller of that remainder and the positive fixed buffer length. Consequently count is positive, offset plus count cannot exceed bytes.Length or int.MaxValue, and every iteration makes progress. Source slicing and destination slicing use that same count. Public Length remains long and the existing array-backed size contract is unchanged.

## Compatibility, exceptions and counting stream

IPdfOriginalSourceRevision, CopyTo(Stream), Length and cross-reference metadata signatures are unchanged. Null and nonwritable destination validation runs before allocation, preserving established exception types/messages/parameter names. No exception is caught, wrapped or suppressed. The new bounded allocation can fail before the first write under exhausted managed memory, as ordinary managed allocations can.

The intentional compatibility difference is write granularity: previously one write regardless of source size; now one or more ordered bounded writes. A destination that depends on one callback observes this difference. First/middle/final callback IOException instances propagate unchanged. Earlier chunks, and whatever the failing callback itself wrote, can remain in caller-owned output. CopyTo neither rolls back arbitrary destination state nor closes it. There is no new seek/flush requirement and no cancellation parameter or cancellation policy is introduced.

PdfCountingWriteStream remains unchanged: it forwards the independent temporary span, increments BytesWritten by the successful write's exact length using checked arithmetic, and does not close the destination. It cannot reacquire retained revision storage. A same-length temporary mutation leaves count correct and source intact. On a throwing callback, counting stops with the propagated exception; no incremental finalization follows. Checked-in complete-save equality and actual parser offsets verify normal counting across multiple chunks.

Reviewed adjacent paths: IPdfOriginalSourceRevision, PdfIncrementalWritePlan, PdfIncrementalWritePlanner, PdfIncrementalUpdateWriter, PdfCountingWriteStream, PdfEditingSaveService and PdfExistingTextTransactionWritingService. Services routes incremental saves through the same writer and retains the revision object; the sole production source-copy call is in PdfIncrementalUpdateWriter. Full rewriting does not consume a source revision. Plan identity, current document objects, change-set/transaction/history handling, projection, generations and SDK boundaries are unchanged.

## Allocation accounting and performance limits

| Boundary | Before | After |
|---|---|---|
| Constructor | One complete ownership copy | Same one copy |
| CopyTo before destination callback | No isolation copy; retained alias exposed | One fixed 65,536-byte array per call, reused within that call |
| Per chunk | Retained storage forwarded | Source copied into temporary; only temporary span forwarded |
| Successful whole copy | Destination consumes L bytes | Destination consumes same L bytes, plus L bytes of isolation copying |
| Incremental original prefix | Retained alias forwarded through counting stream | Same single bounded buffer through counting stream |
| Save result/document/serialization storage | Existing allocations | Unchanged |

Additional source-isolation peak storage is O(64 KiB), not O(L) as a whole-revision clone would require. Concurrent or reentrant calls each own their buffer; the per-call bound is not a global application memory ceiling. The BCL's array-only Stream fallback can additionally use its own temporary copy per chunk, as before; this is distinct from the one source-isolation allocation. No buffer is returned to a pool by this implementation. Managed memory lifetime remains CLR-controlled, with no guaranteed-erasure claim.

**Measurement required:** save throughput, large-document latency, callback-frequency effects, GC behavior, optimal chunk size and end-to-end memory profiles. This correction makes no throughput or total application peak-memory improvement claim over the old unsafe path.

## Exact tests and outcomes

| Test file | Change |
|---|---|
| tests/BlackFlag.Engine.Writing.Tests/PdfSourceRevisionOwnershipCharacterizationTests.cs | Invert span mutation and both incremental-save attacks; preserve array-fallback and separate parser bridge controls. Extend save fixtures to multiple chunks, nonzero object generation, exact bytes/hashes, repeated saves, metadata, Prev and startxref. Public fixture supports supplemental parser validation. Five cases. |
| tests/BlackFlag.Engine.Writing.Tests/PdfSourceRevisionCopyBoundaryTests.cs | New six length cases, three throwing-callback cases, and one empty/invalid-destination validation case. Ten cases. |

Boundary lengths are 1, 65,535, 65,536, 65,537, 196,608 and 196,609 bytes. Each performs three hostile/ordinary pairs, checks exact content and independently recomputed SHA-256, callback sizes/count, constructor-input isolation, metadata and nonseekable destination lifetime. The zero-length case confirms existing rejection. Failures at writes one, two and three use a 131,073-byte source, giving first/middle/final-one-byte coverage; each checks the same exception instance, exact partial output and intact subsequent source copy/hash/metadata. The existing array-only fallback mutates only during its synchronous callback and still cannot change source state.

Both incremental fixtures mutate every original-prefix chunk after first copying it into the hostile destination. They then reuse the exact same source revision and plan for two ordinary saves. All complete saves are identical; original prefixes compare byte-for-byte and by independent SHA-256. No original-prefix normalization or serialization occurs.

## Actual parser reopen

The existing Writing test project does not reference Parsing. As expressly authorized, a supplemental executable uses current built assemblies and the checked-in deterministic fixture without adding repository references. Run:

```powershell
dotnet run --project C:\Users\Sufan\AppData\Local\Temp\PiratePDF-PostBF1054-Audit-92e26a4\B4AF2Reopen.csproj --configuration Release --no-restore
```

Both modes passed; each reopens the original revision and the later ordinary incremental save, for **four parser opens**. Traditional output uses PdfCrossReferenceParser. Stream output bootstraps actual xref objects through PdfIndirectObjectResolver and PdfCrossReferenceStreamParser, with bounded, cycle-protected Prev traversal. Both use PdfObjectLocationTable and the actual resolver for revision/object content; existing filter limits are retained.

| Source/output mode | Original bytes | Original xref | Exact preserved-prefix SHA-256 |
|---|---:|---:|---|
| Traditional table | 131,298 | 131,152 | 95E56F34F6643306330AB4BE2A54DA012ECE4F53C725BC589C8CE795710249D7 |
| Cross-reference stream | 131,323 | 131,152 | E93276618D0DC5A6177041CACAF6E61001A9E265AD710FD572B185D4DF2524A2 |

Reopen verifies one versus two revisions, original/active xref offsets, Prev equal to 131,152, startxref pointing to the actual appended table or XRef stream, and unchanged root object 1 and multi-chunk object 2. Object **3 7 R** resolves to original in the source and updated in the incremental result. The output starts with the exact original PDF bytes and the hashes above remain identical after the hostile callback. The probe supplements persistent regressions; it is not a checked-in release-gate test project or a claim of complete rendering conformance.

## Validation and disposition

Focused tests: **15 passed**. Complete affected projects: **Writing 153 passed; Services 313 passed**, zero failed/skipped and no final build warnings/errors. Editing/SDK paths were inspected but not changed; Parsing production and project references were not changed. Their suites remain part of the full release gate.

Commands used `dotnet test tests/<project>/<project>.csproj --configuration Release --no-restore`, with `--filter FullyQualifiedName~PdfSourceRevision` for focused tests; the complete Writing run used the freshly built output with `--no-build`. A nullable test annotation warning encountered during the pre-fix run was corrected by explicitly enabling nullable context, without suppression. `git diff --check` passed before the gate.

The **full unmodified release gate passed**, exit code zero: **1,880 tests across 20 projects**, zero failed/skipped. Source-size validation passed for 1,034 files; the unchanged 937-line graphics interpreter and 930-line sanitizer review notices remain. Restore, build, tests, DocGen, repair preview and whitespace all passed. No analyzer/test suppression or release-gate edit was made. DocGen validated **185 documents**, with zero errors/warnings/information; health **91.6% (A-)**, and repair preview **zero changes/files/diagnostics**. Command:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

**POST-P0-014 is implemented and closed for B4A-F2.** The main audit disposition was updated only after the gate passed. Final documentation generation/validation, health, repair preview and whitespace checks also passed after recording completion.

## Final scope review

**Additional issues fixed under expanded authorization: none.** No directly exposed bypass or unresolved P0 was found in the modified source-revision path. Adjacent source-copy/counting/save orchestration needs no production change. Arbitrary third-party implementations of IPdfOriginalSourceRevision still bear their own immutable-source contract; this batch corrects the concrete PdfMemorySourceRevision implementation.

The exact final change set is **eight files**: the one production file and two test files above, this report, docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md, and normal DocGen updates to docs/Documentation-Index.md, docs/Documentation-Health.md and docs/ValidationReport.md. Every changed file was reviewed. Historical characterization/milestone reports and the release gate are unchanged.

POST-P0-011/013, POST-P0-005/006/007/008/009, related provenance views, B4A-F3/F4, B4B/B4C and roadmap features remain untouched. No public signature, dependency, reference, resource ceiling, security policy or unrelated syntax change is included. No commit, push, merge, tag or branch switch occurred. Stop after F2 documentation and validation; later work requires explicit approval.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-10 | Isolate retained source revisions from destination callbacks with bounded copying; record regression, exception, allocation and incremental parser preservation evidence. |
