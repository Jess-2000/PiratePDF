# Post-BF-1054 F3-OBS-01 Truncated-Flate Characterization

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-021 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1054 |
| **Last Updated** | 2026-09-13 |
| **Next Review** | 2026-12-13 |

## Scope, baseline and disposition

The user authorized characterization first and the smallest necessary correction of F3-OBS-01. The starting branch was `maintenance/post-BF-1054-repository-review`, with a clean working tree and empty diff at **55e59e9ff51a8bdf7313130a4a1132205501a177**, Harden character code ownership. F4 was committed after F3 and release-gate consolidation. Root AGENTS.md, charter, README, current audit, F3/F4 reports, B1 and relevant B3A progress principles, BF-1040 and BF-1054P contracts and the current gate were read. The consolidated Invoke-PiratePDFReleaseGate.ps1 remains authoritative and unchanged; the former standalone source-size script remains intentionally absent.

**Classification D: confirmed correctness defect, POST-P0-015 (P0).** This follows the repository's correctness/data-integrity classification: an actual object-stream parse silently produced integer 4 from the incomplete encoding of 42. No resource-limit bypass, execution exploit, plugin escape or critical exploit severity is claimed. The ordinary filter violates an explicit incomplete-zlib rejection contract. Production correction was necessary under the user's authorization. The full unmodified release gate passed; **POST-P0-015 is implemented and F3-OBS-01 is closed**.

The original observation remains accurate: ordinary Filters accepted a single 78 byte with zero output before any ownership mutation; 01 02 03 04 05 still failed. F3 and F4 ownership are independent and remain closed. POST-P0-005 is not started.

## Contract, specification and runtime evidence

**Repository policy:** [BF-1040](../milestones/BF-1040-Stream-Filter-Framework-and-FlateDecode.md) explicitly requires the zlib wrapper, rejects raw DEFLATE, and classifies malformed or incomplete zlib as InvalidData. [BF-1054P](../milestones/BF-1054P-Filtered-Content-Stream-Mutation-and-Safe-Re-encoding.md) preserves single-Flate decoded-space mutation and dictionary representation; [B1](Post-BF-1054-Batch-B1-Completion-Report.md) already added complete-member checking and valid empty-member encoding. This is enforcement of established policy, not a new generic strictness preference. Historical [F3](Post-BF-1054-B4A-F3-Parser-Stream-Ownership-Completion-Report.md) and [F4](Post-BF-1054-B4A-F4-Character-Code-Ownership-Completion-Report.md) reports remain unchanged.

**PDF/zlib:** ISO 32000 clause 7.4.4 defines Flate by RFC 1950/1951; see the [Adobe-hosted PDF 2.0 specification, 7.4.4.1](https://developer.adobe.com/document-services/docs/assets/5b15559b96303194340b99820d3a70fa/PDF_ISO_32000-2.pdf). [RFC 1950 sections 2.2/2.3](https://www.rfc-editor.org/rfc/rfc1950.html) define the wrapper/trailer, require header/checksum checking, and distinguish bytes following ADLER32 from the member itself. Missing trailers cannot establish successful checksum verification. Rejecting incomplete members follows the repository contract; requiring consumption of every supplied byte does not follow from that contract.

**Runtime:** inspected [.NET v10.0.10 DeflateStream ReadCore and InflatorIsFinished](https://raw.githubusercontent.com/dotnet/runtime/v10.0.10/src/libraries/System.IO.Compression/src/System/IO/Compression/DeflateZLib/DeflateStream.cs). ReadCore can return zero when the source ends while inflation remains unfinished; strict validation is behind a process switch that defaults off. Finished zlib members stop requesting source input. Public ZLibStream has no exposed finished/checksum-success property. Neither Read returning zero nor BaseStream.Position proves completion. F4's runtime version is not presumed: this probe recorded **SDK 10.0.302, Microsoft.NETCore.App 10.0.10, MSBuild 18.6.11, Windows 10.0.26200, win-x64**. No switch was changed.

These runtime controls are observations for this installation, not incidental behaviors frozen in unit assertions. Other runtime versions, native compression builds and operating systems may differ; macOS/Linux were not executed. The repository regression contract is independent of the default permissiveness switch, while the narrow input wrapper relies on the reviewed synchronous read-loop behavior, as B1 already does.

## Baseline and corrected comparison matrix

A temporary console probe referenced the existing Services project and used its existing test friend identity to compare the internal mutation codec without a repository API/reference/friend change. It used no reflection, unsafe memory or new package. Files are under the temporary PiratePDF-F3OBS01-55e59e9 directory: Program.cs, Probe.csproj, baseline.jsonl and corrected.jsonl. A sandbox NuGet-config read failure required an approved rerun; fixture setup errors were corrected before obtaining evidence. These temporary files are not acceptance artifacts; the matrix and permanent regressions below preserve the evidence.

**21 identical byte fixtures, two BCL output-buffer sizes (1 and 64), 42 rows per run.** All inflater constructions succeeded. Each control performed at most 32 reads, recorded returned bytes, then attempted one further read. Source Position equalled input byte length in every row, including trailing bytes, because these tiny inputs fit the BCL input buffer. That is read-ahead, not proof that the inflater logically consumed trailing bytes. No compression or memory-exhaustion bomb was used.

The main generated fixture S is `7801010600F9FF414243444546057E0196`: ZLibStream NoCompression of ABCDEF, a final stored block with six payload bytes and ADLER32. Empty member is the complete canonical `789C030000000001`. Prefixes cut header/body/trailer bytes explicitly; no-final-block clears BFINAL on S and removes the trailer. Second-member appends a valid zlib encoding of GH.

Table notation: read sequences list successful returned counts, followed by E when InvalidDataException interrupted a read; 0 denotes EOF. Output is the bytes returned to the caller, in hex, for buffer sizes 1 / 64. All non-error EOF cases returned 0 again; all E cases threw InvalidDataException on the extra read. BCL exception text was the generic runtime message about an unsupported compression method, even for a bad checksum. Filters F means PdfFilterDecodeException/InvalidData; Editing E means exact InvalidDataException with its established editable-zlib message. OK followed by hex is successful output; bare OK is empty output. Editing is identical before/after in all 42 rows.

| Fixture | Input hex | BCL reads, size 1 / 64 | BCL output, size 1 / 64 | Total bytes, size 1 / 64 | Filters before | Editing both | Filters after |
|---|---|---|---|---|---|---|---|
| empty |  | 0 / 0 |  /  | 0 / 0 | F | E | F |
| 78 | 78 | 0 / 0 |  /  | 0 / 0 | OK  | E | F |
| 789C | 789C | 0 / 0 |  /  | 0 / 0 | OK  | E | F |
| 7801 | 7801 | 0 / 0 |  /  | 0 / 0 | OK  | E | F |
| 78DA | 78DA | 0 / 0 |  /  | 0 / 0 | OK  | E | F |
| complete | 7801010600F9FF414243444546057E0196 | 1,1,1,1,1,1,0 / 6,0 | 414243444546 / 414243444546 | 6 / 6 | OK 414243444546 | OK 414243444546 | OK 414243444546 |
| complete-empty | 789C030000000001 | 0 / 0 |  /  | 0 / 0 | OK  | OK  | OK  |
| trailer-minus1 | 7801010600F9FF414243444546057E01 | 1,1,1,1,1,1,0 / 6,0 | 414243444546 / 414243444546 | 6 / 6 | OK 414243444546 | E | F |
| trailer-minus2 | 7801010600F9FF414243444546057E | 1,1,1,1,1,1,0 / 6,0 | 414243444546 / 414243444546 | 6 / 6 | OK 414243444546 | E | F |
| trailer-minus3 | 7801010600F9FF41424344454605 | 1,1,1,1,1,1,0 / 6,0 | 414243444546 / 414243444546 | 6 / 6 | OK 414243444546 | E | F |
| no-trailer | 7801010600F9FF414243444546 | 1,1,1,1,1,1,0 / 6,0 | 414243444546 / 414243444546 | 6 / 6 | OK 414243444546 | E | F |
| body-partial | 7801010600F9FF4142 | 1,1,0 / 2,0 | 4142 / 4142 | 2 / 2 | OK 4142 | E | F |
| body-header | 7801010600 | 0 / 0 |  /  | 0 / 0 | OK  | E | F |
| no-final-block | 7801000600F9FF414243444546 | 1,1,1,1,1,1,0 / 6,0 | 414243444546 / 414243444546 | 6 / 6 | OK 414243444546 | E | F |
| trailing1 | 7801010600F9FF414243444546057E0196FF | 1,1,1,1,1,1,0 / 6,0 | 414243444546 / 414243444546 | 6 / 6 | OK 414243444546 | OK 414243444546 | OK 414243444546 |
| trailing3 | 7801010600F9FF414243444546057E0196FFFFFF | 1,1,1,1,1,1,0 / 6,0 | 414243444546 / 414243444546 | 6 / 6 | OK 414243444546 | OK 414243444546 | OK 414243444546 |
| second-member | 7801010600F9FF414243444546057E01967801010200FDFF474800D80090 | 1,1,1,1,1,1,0 / 6,0 | 414243444546 / 414243444546 | 6 / 6 | OK 414243444546 | OK 414243444546 | OK 414243444546 |
| invalid-header | 0000 | E / E |  /  | 0 / 0 | F | E | F |
| bad-checksum | 7801010600F9FF414243444546057E0169 | 1,1,1,1,1,E / E | 4142434445 /  | 5 / 0 | F | E | F |
| established-invalid | 0102030405 | E / E |  /  | 0 / 0 | F | E | F |
| raw-deflate | 010600F9FF414243444546 | E / E |  /  | 0 / 0 | F | E | F |

The one-byte 78 observation is reproduced exactly. Header-only/body-header cases returned zero; body-partial returned AB; missing one to four trailer bytes and a missing final block returned apparently complete ABCDEF. All were accepted by baseline Filters, but rejected by B1. A bad checksum produced five bytes ABCDE before delayed BCL failure with a one-byte buffer, but threw on the first 64-byte read. Neither baseline nor corrected pipeline published a successful result for that checksum error. Invalid headers, the established malformed fixture and raw DEFLATE rejected on the first read. Corrected Filters rejects every malformed/incomplete matrix case, while all complete and trailing controls preserve exact output.

## Completeness, trailing data and predictors

A missing or partial ADLER32 bypassed completion/verification in baseline Filters; a complete incorrect ADLER32 was already detected. The cleared-BFINAL fixture shows the issue is not solely a missing checksum: the inflater can return all available payload without encountering a final block. The correction does not parse deflate or compute a second checksum; it requires ZLibStream to finish without requesting unavailable input, leaving zlib responsible for wrapper, final-block and checksum validation.

One garbage byte, three garbage bytes and a second complete member are ignored after the first complete member. Both APIs already behaved this way, and B1 has an explicit trailing-data regression. There is no caller use of compressed-input consumption counts in these decode APIs. A strict Position == Length check would neither prove completion nor safely define a new trailing-data policy. No trailing rejection or concatenated-member decoding was added.

Minimal PNG Sub predictor input is 01 0A 0A 0A 0A, Columns 4, Colors 1, eight bits. Valid compression produces 0A 14 1E 28. Before correction, removing one or four trailer bytes still allowed predictor output; removing seven bytes left an incomplete prefixed row, so predictor validation masked compression truncation. The corrected decoder rejects all three at the compression boundary before transforming any predictor output. Complete zlib containing an incomplete predictor row retains its exact existing PNG-row exception. Predictor output remains staged behind a bounded intermediate accumulator; no predictor code changed. Editing intentionally rejects active predictors and continues to allow only Predictor 1, so an active-predictor output comparison with mutation would be inappropriate.

## Actual production paths and consumer impact

| Path | Input, ownership and behavior |
|---|---|
| Filters/PdfFlateFilterDecoder.Decompress | ReadOnlySpan input, one owned encoded array copy, synchronous ZLibStream. Common boundary for Predictor 1 and all supported predictors. The pipeline owns pooled output; no successful return follows decode failure. |
| Services/Text/PdfContentStreamMutationCodec.DecodeFlate | Owned byte[] copied from Document.PdfStream; existing private complete-member input wrapper, shared decode budget and bounded output. Already rejects all matrix truncations with InvalidDataException; no production change. EncodeFlate is the only other direct ZLibStream use and performs compression only. |
| Parsing/Rendering/PdfNestedGraphicsProvider | Decodes Form, pattern and Type3 CharProc streams through the pipeline before content parsing/interpreting. The content parser accepts complete syntax prefixes but cannot verify compressed completeness. New filter failure precedes operation publication. Top-level PdfContentStreamParser accepts already decoded memory; no alternate inflater exists there. |
| Rendering/Fonts/PdfFontStreamDecoder, PdfCMapParser and PdfFontDecoder | Font/CMap bytes use the ordinary pipeline. Real CMap control formerly published A with a missing trailer. Code-space/token validation cannot detect a missing compression trailer. Optional-CMap catches in font decode/reverse encoding catch syntax InvalidDataException/FormatException, not the filter IOException subclass; new filter failures propagate like existing corrupt-checksum failures. |
| Parsing/ObjectStreams/PdfObjectStreamParser | Real stored-block prefix of 2 0 42 yields 2 0 4; /N 1 and /First 4 still validate and GetByIndex(0) formerly returned integer 4. After correction, Parse throws before constructing PdfCompressedObjects. Complete input still produces 42, with dictionary and encoded bytes preserved. |
| Parsing/Loading/PdfIndirectObjectResolver | LoadCompressed uses that parser; successful values feed Lazy object caching. Resolve removes the failed object's cache entry on exception. PdfFilterDecodeException is an IOException and propagates past the narrower LoadCompressed syntax catch, as existing filter failures do. No cache implementation changed. The dynamic test exercises the actual object parser; cache consequences are source-traced. |
| Parsing/CrossReferences/PdfCrossReferenceStreamParser | Real missing-trailer input formerly passed exact decoded-size validation and published the same offset/generation records as valid input. New failure precedes sections; no xref table, offset, generation or /Prev semantics changed. Short decoded records still have their own structural rejection. |
| Rendering/Images/PdfImageDecoder | Pipeline decoding precedes raw-sample conversion; insufficient samples have a later size failure, but sufficient bytes with a missing trailer formerly passed the compression boundary. Encoded size and image dimension guards remain intact. Font programs similarly have later format checks; those are not a substitute for complete Flate. |
| Rendering/Functions/PdfFunctionEvaluator | Stream-backed function data uses the ordinary pipeline before sample/calculator processing. No independent inflater, cache or function policy change. |
| Parsing/Security/PdfSecurityAnalyzer.AnalysisState | Direct stream visits inspect dictionaries, not decoded stream bodies. Reference visits use the resolver, so compressed referenced objects can depend on ObjStm decoding. No active-content execution, inspection completeness guarantee or demonstrated security bypass is inferred from this correction. |
| Editing/history/Writing | B1 rejects incomplete compression before token mutation/history recording; disposal of a failed surrounding transaction retains the established rollback contract. No new save/mutation path changed. Existing Services tests cover filtered preservation, history and saves; no new save/reopen fixture is needed for this Filters-only correction. |

Direct IPdfFilterDecoder.Decode writes to a caller-supplied IBufferWriter. It is a streaming API, not an atomic-result API: a caller can see provisional bytes before a later failure, as with checksum failures before this batch. A regression verifies ABCDEF is provisionally written before truncation failure; that caller must discard failed output. The high-level pipeline disposes its internal output on failure and returns nothing. B1 also returns nothing on failure. Adding whole-output buffering solely to make the low-level writer atomic would be a separate behavioral/resource change, unnecessary for the owned pipeline contract.

## Smallest production correction and compatibility

Only **src/BlackFlag.Engine.Filters/PdfFlateFilterDecoder.cs** changes in production. Replace its MemoryStream with a private CompleteFlateInputStream over the same owned array. Its synchronous array/span Read overrides call the base read and throw InvalidDataException if a nonempty request reaches EOF. Zero-length requests are not treated as truncation. The existing catch maps that exception to **PdfFilterDecodeException / InvalidData**, retaining the established message and inner exception. No broad catch or low-level EndOfStreamException leaks.

The helper follows B1's existing invariant. B1's private helper belongs to the higher Services assembly; Filters cannot reuse it without inverting dependencies or exposing a new shared API/friend contract. One small local helper at each of the two existing decode boundaries is the narrow compatible choice. Nothing is duplicated across parser/rendering consumers, and there is no parallel inflater or process-wide compression switch.

Accepted malformed inputs intentionally become failures. This may reject damaged PDFs that previously appeared to decode, even when all expected payload bytes were available but the trailer was missing. There is no repair-mode policy in the selected filter contract authorizing silent acceptance. Valid zlib, complete empty streams, trailing data, filter/decode-parameter representation and supported predictors are preserved. Empty encoded input keeps its distinct existing filter message; malformed header/checksum failures keep their existing exception family/message. Predictor-row validation now runs only after successful compression completion, intentionally changing which malformed-input diagnostic wins when both layers are incomplete. No public signature, dependency, reference, friend declaration or security limit changes.

## Resource and allocation accounting

No new resource bypass was demonstrated: the issue is false decode success. PdfFilterDecodeBudget and PooledPdfFilterBufferWriter continue enforcing decoded bytes and expansion before output growth. Predictive decompression also bounds its intermediate accumulator. Filter count bounds chain length; budgets are per stage, not a document-wide aggregate quota. Existing aggregate-work scope was not expanded or weakened.

Tiny 128-byte payload cases verify exact decoded-byte/expansion limits, limit-plus-one and the adjacent smaller ratio for complete and truncated input. Over-budget output fails with the original limit classification before it can be accepted or later reach EOF. Pipeline pre-cancellation retains OperationCanceledException and its token; direct decoder budget cancellation remains PdfFilterDecodeException/Cancelled with that token as inner OperationCanceledException. A deterministic writer cancels after the first output chunk, verifying cancellation wins before the next input read for both complete and incomplete streams. B1 pre-cancellation and bounded-copy tests remain unchanged and pass.

Before/after successful decode allocation shape: one encoded-data copy, one stream object, the same zlib state/buffers, same rented work buffer, and the same bounded output/predictor copies. The private stream subclass adds no fields or byte copy, no second decode and no checksum scan. It adds a read-result branch; failure adds ordinary exception objects and avoids the pipeline's final successful-output copy. Owned encoded data and pooled work buffers are still cleared in the existing finally block. No guaranteed managed-memory erasure claim is made. **Measurement required:** throughput, GC, peak memory and percentage effects on representative documents. No performance gain is claimed.

## Exact test files and validation

| Exact file | Changes |
|---|---|
| tests/BlackFlag.Engine.Filters.Tests/PdfFlateCompletenessTests.cs | New 27 cases: headers/malformed/raw input; every proper prefix under three compression levels; complete empty member and all its prefixes; final block/checksum; trailing/second-member controls; predictor validation; exact/over byte and ratio limits; pre/mid cancellation; direct provisional-output failure. |
| tests/BlackFlag.Engine.Parsing.Tests/Loading/PdfFlateCompletenessIntegrationTests.cs | New three cases: actual shortened ObjStm integer, xref trailer and CMap trailer. Characterization initially asserted baseline acceptance, then was inverted while retaining valid controls and source/dictionary isolation. |
| tests/BlackFlag.Engine.Services.Tests/Text/PdfContentStreamMutationCodecLimitTests.cs | Six added cases: four short headers, missing final block, and concatenated first-member behavior, comparing actual ordinary and B1 APIs and their distinct exception contracts. Existing 26 B1 cases retained. |

Before production edits, the 21-fixture matrix reproduced; all three unsafe parser controls passed. Inverted parser tests then failed 3/3. The initial 25 filter contract cases had 13 failures and 12 passes after correcting one test's assumed PNG-message wording to the actual established text. No production contract was changed to satisfy that assertion. Two deterministic cancellation cases were added afterward. Permanent tests assert PiratePDF's intended contract, not incidental BCL partial-read timing/messages.

**62 focused cases passed**: Filters 27, Parsing 3, Services codec 32. **36 cases are new overall.** Complete affected suites: **Filters 119, Parsing 219, Services 319**, zero failures/skips. Complete Rendering **97** also passed for image/font/function downstream integration and F4 ownership. Focused builds used dotnet test --configuration Release --no-restore; complete suites reused those fresh outputs with --no-build, while Rendering built with --no-restore. The final empty-member prefix strengthening passed its focused rerun; the full gate subsequently passed all tests together.

The **full unmodified consolidated release gate passed with exit code zero: 1,945 tests across 20 projects, zero failed/skipped, zero build warnings/errors**. Integrated source-size validation passed **1,038 files**, retaining the existing 937-line graphics interpreter and 930-line sanitizer review notices. Restore, build, tests, documentation, health, repair preview, whitespace and status stages all ran. DocGen validated **188 documents** with zero errors/warnings/information; health **91.6% (A-)**; repair preview **zero changes/files/diagnostics**. The command was powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1. POST-P0-015 was marked implemented only after that gate passed. Final DocGen generation/health/repair and whitespace checks were repeated after recording completion.

## Final scope

Exact final change set: **nine files** — one production file, the three test files above, this report, docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md, and normal DocGen updates to docs/Documentation-Index.md, docs/Documentation-Health.md and docs/ValidationReport.md. Every changed file was reviewed after generation. Git diff --check and separate whitespace checks for the new files passed. No additional directly exposed issue required correction. No production mutation/predictor/parser/cache/security code was changed.

F3 parser-stream and F4 character-code ownership remain closed; their implementations are untouched. Other ownership findings/candidates, POST-P0-005 and all unrelated P0/P2/measurement/roadmap work remain untouched. No commit, push, merge, tag or branch switch occurred. Stop after this investigation and correction; POST-P0-005 requires separate approval.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-13 | Characterize BCL/filter/editing truncation, establish POST-P0-015 correctness impact, enforce ordinary complete-member validation and record compatibility, bounds and validation. |
