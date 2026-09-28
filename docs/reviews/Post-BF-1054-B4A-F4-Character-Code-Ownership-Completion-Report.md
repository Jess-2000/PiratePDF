# Post-BF-1054 B4A-F4 Character-Code Ownership Completion Report

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-020 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1047, BF-1054 |
| **Last Updated** | 2026-09-13 |
| **Next Review** | 2026-12-13 |

## Scope and baseline

The user approved **B4A-F4 / POST-P0-011**. Work started with a clean working tree and empty diff on `maintenance/post-BF-1054-repository-review` at **`5dffbab5ea5d0220510fcac2c70cfd7c6eb17771`**, `Harden parser stream ownership`. Recent history confirms F3 was committed, following release-gate consolidation and F2. No unexpected edits were present.

Governing context: root AGENTS.md, PROJECT-CHARTER.md and README.md; Blueprint engineering and security architecture, memory architecture, ADR-0010 bounded materialization and BF-1054V immutability/closure contracts; the [main audit](Post-BF-1054-CSharp-Repository-Audit.md), [ownership characterization](Post-BF-1054-B4A-Ownership-Candidate-Characterization.md), [B4A](Post-BF-1054-Batch-B4A-Sanitized-Font-Ownership-Completion-Report.md), [F1](Post-BF-1054-B4A-F1-Descriptor-Ownership-Completion-Report.md), [F2](Post-BF-1054-B4A-F2-Source-Revision-Ownership-Completion-Report.md), [F3](Post-BF-1054-B4A-F3-Parser-Stream-Ownership-Completion-Report.md) and [pre-B4 expression review](Post-BF-1054-Pre-B4-Expression-and-Materialization-Review.md).

The consolidated Invoke-PiratePDFReleaseGate.ps1 was read and remains authoritative and unchanged. Test-PiratePDFSourceFileSizes.ps1 remains intentionally absent. Historical references to that former script do not supersede the current gate; historical reports were not rewritten.

## Original mechanism and reproduced scope

PdfCharacterCode already copied constructor input into a private readonly byte array. Bytes returned array-backed ReadOnlyMemory over that same retained array. MemoryMarshal.TryGetArray and MemoryMarshal.AsMemory allowed ordinary managed callers to mutate it. Equals compared current private bytes, GetHashCode recomputed their hash, and ToString formatted them in uppercase hexadecimal.

Both original characterization cases passed before production edits. The bounded original probe selected a changed hash without assuming collision-free HashCode behavior, then demonstrated changed equality/hash, failed Dictionary/HashSet lookups, and an equivalent duplicate dictionary key. The original CMap control showed fresh 41 still resolving to CID 7 and Unicode A after a public query became 42: internal CMap keys were not exposed or corrupted.

This finding concerns an in-process public value contract and caller-owned hash collections. F4 does not broaden it into a PDF-only exploit, renderer cache attack, or SDK/plugin isolation escape.

## Corrected ownership and exact production files

| Exact file | Change |
|---|---|
| src/BlackFlag.Engine.Rendering/Fonts/PdfCharacterCode.cs | Bytes returns bytes.ToArray(); XML documentation identifies an independent snapshot. Constructor, equality, hashing and display implementations are unchanged. |
| src/BlackFlag.Engine.Rendering/Fonts/PdfFontDecoder.cs | DecodeComposite captures one codeBytes snapshot and reuses it for length, big-endian CID fallback and PdfDecodedGlyph construction. |

Each nonempty public snapshot owns separate element storage. Callers can mutate their returned memory locally, but cannot change retained bytes, another snapshot or later reads. Constructor input remains independently copied. No hash cache, new key representation, CMap cloning, public/internal accessor or borrowed-memory API is introduced.

## Repository-wide consumer classification

Repository searches covered all PdfCharacterCode references, inferred ReadCode results, Bytes reads, equality/hash/collection uses, CMap construction and generated Unicode/CID mapping tests. Direct production users remain the four Rendering font files below, alongside the value implementation itself.

| Consumer | Classification and disposition |
|---|---|
| PdfCharacterCode.Bytes | Public/untrusted boundary: independent snapshot per access. |
| PdfCharacterCode.Equals/GetHashCode/ToString | Private storage inspection: unchanged; equality/hash never call Bytes. Uppercase hex display and byte-sequence equality remain exact. No ordering/comparison API exists. |
| PdfCMap | Private dictionary keys, public lookup and copied ReadCode result construction: unchanged. Internal constructor copies dictionary containers; parser creates keys privately, and public APIs do not enumerate keys. No production HashSet of these keys was found. |
| PdfCMapParser | Private construction of CID/Unicode character/range keys: unchanged. Existing decoded-byte/token/range bounds and materialization remain intact. |
| PdfFontDecoder.DecodeSimple | Constructs one-byte lookup codes and reads no Bytes property: unaffected. Existing glyph constructor copying remains. |
| PdfFontDecoder.DecodeComposite | One snapshot per operation/code; trusted local length/byte inspection reuses that snapshot. Previous two accesses for mapped CIDs or three for fallback become one. |
| PdfSameFontTextEncoder | Constructs one-byte ToUnicode reverse-lookup keys; does not read Bytes: unaffected. |
| Rendering and Services tests | Public snapshots, CMap lookup, generated CMaps and font integrations: validated without mechanical copy changes in unrelated fixtures. |

No internal byte borrow was needed. Rendering already grants friend access to Parsing, Rendering.Skia and three test assemblies; introducing another storage accessor would widen the review surface for tiny values. One ordinary local snapshot preserves a small implementation and never gives retained storage to callers or callbacks. No friend declarations changed.

## Equality, collections, CMap and decoding results

Constructor-array mutation leaves Bytes, equality, hash, display and dictionary membership intact. Mutating snapshot A leaves snapshot B and future C unchanged. Both TryGetArray and AsMemory are exercised. Equal independent instances retain identical hashes within the process; tests do not require a fixed cross-process hash or assume different values cannot collide.

Dictionary lookup succeeds for both the inserted instance and an independently constructed equivalent key. Adding an equivalent key throws the normal exact ArgumentException and leaves one entry. HashSet.Contains uses actual hashed lookup, succeeds for both instances, and Add returns false for the equivalent value. It is not replaced by an assertion that merely enumerates the set.

The CMap control retains original and fresh 41-to-CID-7/A mappings after snapshot mutation. Constructing a genuinely different 42 query from the caller's mutated copy still misses both mappings. A two-byte query 80AB similarly remains CID 300/Omega after attempted mutation to 80AC, which remains unmapped. CMap storage is unchanged; no new duplicate or missing mapping behavior appears.

Actual PdfFontDecoder coverage verifies simple one-byte Euro/A order; mixed one/two-byte A/Omega/A decoding with explicit CIDs 7/300/7, glyph IDs, source bytes, origins and advances; and Identity-H/Identity-V two-byte big-endian CID fallback. Repeated decoding retains identical results. Uppercase hexadecimal display (41 and 80AB) is stable. Empty codes remain invalid rather than acquiring a new valid representation.

## Exact tests and development evidence

| Exact file | Cases |
|---|---|
| tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfCharacterCodeOwnershipCharacterizationTests.cs | Invert both original cases; expand ownership/key test to one-byte and two-byte theory cases; add empty-input rejection. Four cases cover constructor/snapshot isolation, equality/hash/display, Dictionary/HashSet and the CMap control. |
| tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfCharacterCodeDecodingTests.cs | New six cases: simple decoding, variable-length mapping, two identity encodings, truncated code and out-of-codespace code. |

Against old production, the revised ownership tests produced three failures and one empty-input pass. New decoding tests produced one alias failure and five behavior-control passes. After the fix, **all 10 focused cases passed**. Test setup imports and Objects.PdfDictionary construction were corrected before validation. The HashSet test captures Contains results explicitly to test hash lookup while satisfying the collection-assertion analyzer; no suppression or weaker enumeration assertion was used.

## Compatibility and allocation accounting

Public signatures, constructors, CMap/font APIs and semantic equality/hash rules are unchanged. Shared Bytes storage identity/mutation is the intentionally corrected behavior. ToString remains Convert.ToHexString over private bytes. Empty input retains ArgumentException with parameter bytes and the same message; malformed/truncated character codes retain exact InvalidDataException text and source offset. No catch, wrap, validation-order change, cancellation check, disposal behavior or resource limit changed. Existing cancellation paths remain in place. The new public allocation can fail under exhausted managed memory before downstream decoding; no extra domain exception policy is introduced.

For a character code of length L, excluding object headers and unrelated filter/font allocations:

| Boundary | Before | After |
|---|---|---|
| Constructor | One L-byte defensive copy | Unchanged |
| Public Bytes access | Zero copies; unsafe alias | One L-byte snapshot |
| Equals / GetHashCode | Allocation-free direct private byte inspection | Unchanged; no public snapshot |
| ToString | Hex string allocation from private bytes | Unchanged; no byte snapshot |
| Composite glyph | ReadCode constructor copy plus glyph constructor copy; Bytes accesses alias | Same two copies plus one L-byte local snapshot |
| Naive defensive getter in unchanged composite consumer | Would add two snapshots for mapped CID, three for fallback | Local reuse limits the added cost to one snapshot in either path |
| Simple decoding, parser/CMap keys, same-font reverse lookup | Existing constructor/key costs | Unchanged; no added Bytes access |

No additional expanded-scope defect required a fix. Decoder snapshot reuse is the expressly requested prevention of directly introduced redundant copies. There is no cache, pooling, unsafe/native code, pinning, custom memory manager, new reference or dependency. **Measurement required:** end-to-end throughput, total/peak allocations, GC effects and any percentage performance claim. Character codes are normally short; no meaningful application speedup or memory reduction is claimed. Public construction retains its existing nonempty-span contract without a new maximum length.

## Validation

Focused command: dotnet test for BlackFlag.Engine.Rendering.Tests in Release with --no-restore and filter FullyQualifiedName~PdfCharacterCode. Complete Rendering: **97 passed**; Services: **313 passed**; Parsing: **216 passed**, all with zero failures/skips. Rendering was rerun with --no-build after its focused build; Services and Parsing used --no-restore.

Rendering is directly changed. Services directly references Rendering and contains generated CMap and descriptor/external-font CMap integration tests, so its entire suite ran. Parsing references Rendering for resources but has no direct character-code/CMap tests found by search; its entire suite also ran as downstream resource integration coverage. No save/write or transaction path changes; no new serialization/reopen fixture or project reference was needed. Existing downstream preservation/history tests remain in the complete suites and gate.

The **full unmodified consolidated release gate passed with exit code zero**: **1,909 tests across 20 projects**, zero failures/skips, and **zero build warnings/errors**. Integrated source-size validation passed for **1,036 files**, retaining the existing 937-line interpreter and 930-line sanitizer review notices. Restore, build, all tests, documentation generation/health/repair, whitespace and status stages ran normally. Command:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

DocGen validated **187 documents**, zero errors/warnings/information; health **91.6% (A-)**. Repair preview proposed **zero changes/files/diagnostics**. Git diff --check passed before and within the gate. POST-P0-011 was marked implemented in the audit only after gate success; final documentation generation/health/repair and whitespace validation were repeated after the completion update.

## Ownership follow-up closure and exclusions

| Finding | Batch | Status |
|---|---|---|
| POST-P0-012 | F1 descriptor ownership | Implemented and validated in F1 |
| POST-P0-014 | F2 source-revision ownership | Implemented and validated in F2 |
| POST-P0-013 | F3 parser-stream ownership | Implemented and validated in F3 |
| POST-P0-011 | F4 character-code ownership | Implemented and validated in F4 |

**POST-P0-011 is closed. All four characterized B4A ownership findings are now closed.** Unrelated provenance byte-view candidates are not included in that closure: PdfTextRunElement.EncodedBytes, PdfDecodedGlyph.SourceCode, PdfInlineImage.EncodedBytes, PdfHexString.Value and Core PdfString.Value are untouched.

POST-P0-005/006/007/008/009, POST-P2 findings (including POST-P2-008 CI parity), AUD-P1 measurement candidates and roadmap features remain untouched. **F3-OBS-01 — truncated-Flate acceptance requiring separate characterization** remains recorded without assigning severity; it was not investigated, characterized or changed in F4. Flate implementation/tests and all limits are unchanged.

The exact final change set is **nine files**: the two production files and two test files listed above; this report; docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md; and normal DocGen changes to docs/Documentation-Index.md, docs/Documentation-Health.md and docs/ValidationReport.md. Every changed file was reviewed. No public signatures, dependency/project references, friend assemblies, security limits, gate thresholds or unrelated expressions changed. No retained byte borrow crosses an untrusted boundary; equality/hash do not allocate through Bytes. No commit, push, merge, tag or branch switch occurred. Stop after this batch; no further maintenance work is authorized by its completion.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-13 | Isolate character-code snapshots, preserve stable value/key semantics, reuse one decoder snapshot and record ownership follow-up validation and scope. |
