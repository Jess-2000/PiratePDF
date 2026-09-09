# Post-BF-1054 Batch B3A - CMap Tokenizer Progress and Bounds

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-013 |
| **Category** | Engineering Review |
| **Version** | 1.1 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1047, BF-1054 |
| **Last Updated** | 2026-09-08 |
| **Next Review** | 2026-12-08 |

## Scope and status

The user approved **B3A only**, addressing newly recorded **POST-P0-010: CMap tokenizer non-progress/resource exhaustion** in the [post-BF-1054 audit](Post-BF-1054-CSharp-Repository-Audit.md). It was discovered during B3 validation, after the original Phase A inventory; existing findings are not renumbered. The starting tree was clean on `maintenance/post-BF-1054-repository-review` at `613be2a3ec0692c47071536237a9544200df087f`. Root `AGENTS.md`, governing repository context, ADR-0010, memory/resource architecture and relevant font/security milestone contracts were reviewed.

Implementation and validation are complete. The full unmodified release gate passed with **1,818 tests**, zero build warnings/errors and clean documentation/repair/whitespace checks. B4 and all other original findings remain untouched. No commit, push, merge, tag or branch switch was performed.

## Characterization before production changes

`Parse` decoded its input into an ASCII string and immediately materialized `Tokenize(source).ToArray()`. The tokenizer had three yield sites:

| Branch | Original behavior and progress |
|---|---|
| Whitespace | Incremented the source index, then continued; no yield |
| Comment starting at `%` | Advanced to CR/LF or EOF; no yield |
| `[` or `]` | Yielded the delimiter, then advanced on the next enumeration step |
| `<` | Searched for the first `>`, yielded the entire slice, then advanced; did not distinguish `<<` |
| Ordinary token | Scanned until whitespace or an array/angle delimiter, then yielded; at `>` the scan consumed nothing and yielded an empty token repeatedly |

Names and numbers/operators used the ordinary-token path. Comments were only recognized at a token start; adjacent `%` could become part of an ordinary token. Literal strings were not consumed atomically. `>>` had no consuming branch. Generated `/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def` therefore left the second closing angle bracket unconsumed. `ToArray()` could retain an indefinitely growing sequence of empty strings.

Both existing Services builders, `PdfCidToUnicodeCMapBuilder` and `PdfToUnicodeCMapBuilder`, emit this dictionary form. It belongs to the repository's intended CMap syntax and requires no builder correction. The Type0 builder emits two-byte CID mappings and UTF-16BE destinations, including supplementary scalars.

Before editing production code, three tests reflected the original private iterator and requested at most `source.Length + 1` tokens. Standalone `>`, `>>` and generated-style dictionary syntax each failed an empty-token assertion: **3 failed, 0 passed**. No test awaited memory exhaustion or materialized an unlimited reproduction.

## Exact production file changed

`src/BlackFlag.Engine.Rendering/Fonts/PdfCMapParser.cs` is the only production file changed.

- Add explicit dictionary, array and literal-string delimiter handling within the existing tokenizer.
- Keep one token yield site, preceded by a source-position progress check.
- Return token end positions through an internal iterator seam for deterministic tests; no public API is added or changed.
- Bound decoded input before ASCII allocation and token count before creating the next token or growing delimiter state.
- Retain the existing mapping interpreter and range-expansion implementation.

No builder, resource allocator, font sanitizer/subsetter, embedding policy, native/plugin loader, public save facade, dependency or project reference is changed.

## Progress invariant and delimiter behavior

Every tokenizer iteration advances the input position, ends, or throws. Whitespace (including NUL) advances by one; comments consume through their line or EOF. Every token branch updates the index before reaching the single yield site. That site rejects `index <= start` with `InvalidDataException`; empty tokens cannot escape. Iterator tests assert strictly increasing end offsets and a finite source-length-derived step ceiling.

| Input | New behavior |
|---|---|
| Standalone `>` or unmatched `>>` | Deterministic `InvalidDataException` before any token yield |
| Matched `<< ... >>` | Separate opening/closing tokens; both characters consumed; nested delimiter matching retained |
| `<0041>` and other supported hex strings | Single hex token; existing hex interpretation, odd-nibble padding and Unicode mapping preserved |
| Unterminated hex or a nested opening angle in a hex token | Deterministic `InvalidDataException` |
| Adjacent dictionary/hex delimiters | Consumed as distinct lexical tokens without requiring whitespace |
| Arrays | Separate tokens; unmatched or crossed dictionary/array delimiters reject |
| Literal strings | Consume nested and escaped parentheses atomically, so metadata delimiters/comments remain string content; unmatched strings reject |
| Names, numbers, operators | Remain ordinary tokens; names stop at the next lexical delimiter |
| Comments adjacent to tokens/delimiters | `%` starts a comment outside a string/hex token; advances through CR/LF or EOF |
| Whitespace and EOF | Consume whitespace; EOF rejects unfinished dictionary/array state |

The change is a bounded CMap lexical correction, not a general PDF/PostScript tokenizer or interpreter.

## Materialization and resource limits

Token materialization remains bounded and eager for the indexed mapping interpreter, producing an owned `string[]` from one ordered traversal of the bounded tokenizer. The interpreter uses lookahead and variable-length range arrays. Retaining that structure avoids an unnecessary parser rewrite. The later user collection-expression cleanup affects the separate byte-array copy in `AddBigEndian`, not token materialization; see the reconciliation record below.

Progress alone gives a finite input-proportional token count, but short-token inputs can still amplify object/reference overhead. A separate internal **1,000,000-token ceiling** limits token strings, array entries and delimiter-stack growth. The check runs before creating the next token or growing delimiter state. It is a CMap-local cap, not a second filter policy: no existing shared CMap token-count model exists. The ceiling provides over sevenfold headroom above the measured fewer-than-140,000 tokens produced by the existing builder for all 65,536 two-byte codes. A regression actually builds, tokenizes and parses that complete code space and checks every mapping. This is compatibility evidence, not a performance claim.

Before creating the ASCII string, public `Parse` also applies the existing `PdfFilterDecodeLimits.DefaultMaximumDecodedBytes` ceiling (**256 MiB**) to direct callers. This limits character scanning, long individual tokens and whitespace/comment-only input as well as the initial string copy. It does not weaken upstream filter or expansion limits. An internal overload permits small exact-bound tests without large hostile allocations.

Tests verify exact and over-limit byte/token behavior with tiny inputs. The iterator accepts exactly three tokens plus trailing whitespace/comment and rejects a fourth before yielding it. Parser tests accept exactly the byte/token budget needed for `/WMode 1`, then separately reject a one-byte-short budget and a one-token-short budget.

These ceilings bound this parser's lexical input and token materialization. They are **not whole-process memory guarantees** and do not add an aggregate mapped-range work/output budget. The existing one-million-entry per-range limit remains unchanged. The source string, token strings, array and mapping collections still allocate managed memory.

## Exception and interpretation compatibility

Public signatures remain unchanged. New delimiter, byte-limit, token-limit and non-progress failures use `InvalidDataException`, matching the existing parser's format/domain rejection family. Unterminated hex retains that exception type. Existing `ParseHex` conversions, range rules and supported mapping interpretation remain unchanged; this batch does not normalize every pre-existing malformed mapping/count exception. `PdfFontDecoder.ParseOptionalCMap` already catches `InvalidDataException` and `FormatException`; its existing diagnostic/fallback behavior is preserved.

Valid known generated CMaps and existing parser tests pass. New rejection behavior intentionally affects unmatched/crossed delimiters, incomplete lexical structures and inputs above the explicit limits. The previous API has no cancellation parameter; none is added, and upstream stream-decoder cancellation is unchanged.

## Exact test files changed or added

| File | Change |
|---|---|
| `tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfCMapTokenizerTests.cs` | New: bounded original-defect regression, delimiter/adjacency/comment/whitespace/string cases, finite progress corpus, repeated closers and exact/over-limit checks |
| `tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfCMapParserTests.cs` | Add dictionary-metadata compatibility across codespaces, writing mode, CID characters/ranges, Unicode characters/ranges and range arrays; existing assertions unchanged |
| `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfGeneratedCMapParserTests.cs` | New: actual Type0 and simple-font builders, supplementary Unicode and complete two-byte code-space budget compatibility |
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfExternalFontCollisionIntegrationTests.cs` | Strengthen the existing B3 edit/undo/redo/rollback/save fixture to parse the actual generated ToUnicode stream and resolve CID 1 to `A` |

The delimiter corpus enumerates all 1,331 three-character combinations of an eleven-character alphabet containing angle/array/string delimiters, comment/name markers, whitespace and an ordinary character. Each enumeration has a finite step ceiling and checks advancing offsets or deterministic rejection. A 4,096-character closing-delimiter input rejects on its first move. No memory-exhaustion, timing-dependent or system-font fixture is used.

## Parse, edit, save and reopen evidence

The earlier B3 supplemental reopen executable was copied to local scratch as `B3AReopen.csproj` and its saved-ToUnicode-byte assertion replaced with actual `PdfCMapParser.Parse` and CID-to-Unicode lookup. All **16 combinations** passed: direct, indirect, inherited and indirect-category ownership; full rewrite/incremental save; traditional/xref-stream serialization.

The executable reopens saved bytes through the repository xref parser/object resolver, parses content, resolves the actual `Tf` font and decodes that font's generated ToUnicode mapping to `A`. It retains B3 checks for original mappings/generations, shared owners, filter/decode-parameter representation, following text and incremental original-byte preservation. The previously failing generated CMap now parses successfully without changing the builder.

This is supplemental local validation using already-built assemblies, not a new project or permanent release-gate executable. Permanent Services regressions exercise the actual builders and generated CMap after editing and before saving. To rerun the local executable after building Release assemblies:

```powershell
dotnet run --project "$env:TEMP\PiratePDF-PostBF1054-Audit-92e26a4\B3AReopen.csproj" --configuration Release --no-restore
```

The separate public save-facade gap discovered in B3 remains untouched; save validation continues to use supported Writing APIs.

## Validation results

| Check | Result |
|---|---|
| Targeted Rendering CMap parser/tokenizer tests | 29 passed, 0 failed/skipped |
| Targeted Services generated-CMap and B3 integration tests | 27 passed, 0 failed/skipped |
| Complete Rendering project | 87 passed, 0 failed/skipped |
| Complete Services project | 284 passed, 0 failed/skipped |
| Supplemental generated-CMap save/reopen | 16 combinations passed |
| `git diff --check`, including new files | Passed |
| Full unmodified PiratePDF release gate | Passed: 1,818 tests across 20 projects, 0 failed/skipped, 0 build warnings/errors |

The gate inspected 1,026 source files and validated 180 documents with zero errors, warnings or information findings. Documentation health remains 91.6% (A-); repair preview proposes zero changes. The gate ran with the previously authorized access needed to read the user's NuGet.Config. No B3A-caused or unrelated release-gate failure occurred. After recording the gate result, the normal DocGen generation/validation, health and repair commands and whitespace checks were refreshed for the final report wording.

Test commands use `dotnet test <project> --configuration Release --no-restore --verbosity quiet`. The Rendering targeted filter is `FullyQualifiedName~PdfCMap`; Services uses `FullyQualifiedName~PdfGeneratedCMapParserTests|FullyQualifiedName~PdfExternalFontCollisionIntegrationTests`. The acceptance command is the unchanged `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1`.

## Remaining limitations and scope boundary

The parser remains the established supported-CMap subset, with no new predefined-CMap provider, `usecmap` evaluation or general PostScript execution. Aggregate mapping expansion/work and comprehensive malformed mapping-grammar validation are not added by this tokenizer batch. Existing hex conversion behavior remains; this is not a claim of complete PDF lexical conformance or visual rendering validation.

This report and the new finding/disposition in the current audit are the only hand-authored documentation changes. Historical milestones and the historical B3 report remain unchanged. DocGen refreshed `docs/Documentation-Index.md`, `docs/ValidationReport.md` and `docs/Documentation-Health.md` through its normal commands. `docs/Document-Changes.md` remains unchanged with zero proposed repairs.

**B4 and every other original audit finding remain untouched by B3A.** No additional batch is started.

## Post-B3A repository-wide reconciliation, 2026-09-08

The user requested review of every current uncommitted change after additional manual/IDE edits. The authoritative starting state remained `maintenance/post-BF-1054-repository-review` at `613be2a3ec0692c47071536237a9544200df087f`: **11 changed files**, comprising eight tracked modifications and three untracked B3A additions. Recent commits establish B1/B2/B3 as incorporated; B3A remains uncommitted. Attribution below uses the prior B3A implementation record together with the current full working-tree diff, rather than treating every difference from HEAD as a new user edit.

| Changed file | Origin and assessment |
|---|---|
| `Invoke-PiratePDFReleaseGate.ps1` | Later user cleanup: remove indentation from three continued `Join-Path` assignments. Formatting only; retained unchanged. Both versions parse successfully and their 439 ordered non-newline/non-EOF PowerShell tokens, including continuation tokens, match after CRLF/LF normalization. No checks, flags, commands or failure handling changed. |
| `src/BlackFlag.Engine.Rendering/Fonts/PdfCMapParser.cs` | Original approved B3A hardening plus later user replacement of the private `AddBigEndian` byte-array copy with a collection expression. Equivalent for all reachable helper inputs; retained unchanged. Documentation now distinguishes this copy from token materialization. |
| `tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfCMapTokenizerTests.cs` | Original B3A addition plus later user exception-name simplification using the existing `System.IO` import, tuple deconstruction in the end-offset loop and nested-loop indentation. Same exception type, offsets, assertions, iteration order and 1,331-case corpus; retained unchanged. |
| `tests/BlackFlag.Engine.Rendering.Tests/Fonts/PdfCMapParserTests.cs` | Original B3A compatibility test; no additional user semantic change identified. Retained unchanged. |
| `tests/BlackFlag.Engine.Services.Tests/Fonts/PdfGeneratedCMapParserTests.cs` | Original B3A builder/full-code-space regressions; no additional user change identified. Retained unchanged. |
| `tests/BlackFlag.Engine.Services.Tests/Text/PdfExternalFontCollisionIntegrationTests.cs` | Original B3A generated-CMap assertion in the B3 integration fixture; no additional user change identified. Retained unchanged. |
| `docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md` | Original B3A finding/disposition and historical inventory reconciliation; now supplemented with this review's disposition. |
| `docs/reviews/Post-BF-1054-Batch-B3A-CMap-Tokenizer-Completion-Report.md` | Original B3A report; now reconciled to the actual copy/materialization distinction and current validation. |
| `docs/Documentation-Index.md` | Generated B3A document registration/version data; refreshed only by DocGen. |
| `docs/Documentation-Health.md` | Generated B3A document counts and subsequent validation timestamp; refreshed only by DocGen. |
| `docs/ValidationReport.md` | Generated B3A document count and subsequent validation timestamp; refreshed only by DocGen. |

No other pre-existing local change was identified. Repository-wide searches covered affected types, the private helper's sole call site, parser consumers (`PdfFontDecoder` and `PdfSameFontTextEncoder`), tests, comments, audit/report wording, generated references and release-gate invocations. No public API, visibility, nullability, SDK/plugin boundary, transaction, Writing, dependency or project-reference change was introduced by the later edits. The source-size review thresholds are unaffected. No production, test or script correction was needed or made during reconciliation.

The collection expression is target-typed as `byte[]` and copies an already-materialized non-null byte array produced by `ParseHex`. It retains eager copying, order and independent ownership; it introduces neither deferred enumeration nor a second traversal of a lazy input. The unchanged carry loop operates only on that copy. A bounded local probe verified four independent-copy/carry cases, preserved input bytes, the same overflow `InvalidDataException`, and a public Unicode-range carry from U+00FF to U+0100. No allocation regression is apparent from the array-copy structure; no performance improvement or measured allocation result is claimed. Reflection-only invalid arguments to this private helper are not a public null-input contract.

The tokenizer's advancing yield guard, 256 MiB pre-string input check, one-million-token pre-yield/pre-stack-growth cap, balanced delimiter handling and finite enumeration remain intact. Existing tests still confirm dictionaries, closing angles, hex, arrays, strings, comments, names/operators, whitespace and malformed/crossed delimiters. Supported mapping behavior and domain/format failures remain unchanged by the user edits; the parser remains the supported-CMap subset.

Current reconciliation validation: targeted CMap tests **29 passed**; targeted generated-CMap/B3 integration **27 passed**; complete Rendering **87 passed**; complete Services **284 passed**; supplemental actual generated-CMap save/reopen **16 combinations passed**. The complete current release gate passed its unchanged 1,818-test suite and all build/analyzer, source-size, documentation, repair and whitespace checks. DocGen validated 180 documents with zero errors/warnings/information findings; health remains 91.6% (A-), with zero proposed repairs. The user's reported earlier gate pass is recorded as user-reported evidence, separate from these live results. Final result wording was followed by the normal DocGen and whitespace refresh.

Final file review and SHA-256 comparisons against the reconciliation starting state confirm every production/test file and the gate script remained byte-for-byte unchanged. Only this report, the current audit and normal generated documentation output were reconciled. The final working tree still contains the same eleven changed files; no additional file or implementation batch was introduced.

Historical B1/B2/B3 completion reports remain unchanged: their implementation and validation statements describe those earlier checkpoints. In particular, B3's old tokenizer reproduction remains historical evidence. The reconciliation preserves the user's release-gate script byte-for-byte; its indentation-only difference from HEAD is not a weakened or bypassed gate. The existing aggregate mapping-work and public save-facade limitations remain out of scope. No new batch, B4A/B4B/B4C work or canonical font editing was started.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-08 | Record POST-P0-010 characterization, progress-safe bounded tokenization, generated-CMap compatibility and validation. |
| 1.1 | 2026-09-08 | Reconcile all eleven uncommitted files after user/IDE cleanups; preserve equivalent code and gate edits, clarify eager materialization versus the byte-array copy, and refresh validation. |
