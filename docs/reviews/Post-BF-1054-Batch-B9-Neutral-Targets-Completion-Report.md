# Post-BF-1054 Batch B9 - Neutral Target Registry Completion Report

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-030 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1054 |
| **Last Updated** | 2026-09-23 |
| **Next Review** | 2026-12-23 |

## Completion status and scope

**The narrowly authorized neutral target-project slice is complete and build-validated. B9 as a whole is not complete; POST-P2-002 remains open.** This report records the subsequent instruction to work only in `tools/PiratePDF.Fuzz.Targets`, followed by the request to generate this report. It supplements the design in [PP-AUD-029](Post-BF-1054-Batch-B9-Bounded-Fuzz-and-Corpus-Completion-Report.md); it does not claim completion of that report's corpus, runner, native isolation or guided-campaign acceptance criteria.

Branch: `maintenance/post-BF-1054-repository-review`. Starting and ending HEAD: **93865f639acf96f0b9598c97b04d49cea38e8260**. The tree already contained uncommitted B9 scaffolding, documentation, local tool manifests and manually provisioned tooling. Work continued from that state. Nothing was reset, cleaned, reverted or recreated as a batch.

Root AGENTS.md, PROJECT-CHARTER.md and PP-AUD-029 were read before implementation. Only six C# files in the target project changed during this slice. Its project file and existing project references were preserved. No production API, production implementation, dependency or friend-assembly declaration changed. The existing Runner, SharpFuzz, corpus, CI and audit/report files were left untouched by the implementation slice. This report is a separately requested documentation addition.

## Implemented targets

All three registrations have configuration version **1** and isolation classification **SupervisedManagedWorker**. That classification describes the required execution context; the neutral library does not launch a process or enforce a wall-clock deadline or process-memory quota.

| Stable target ID | Production entry point | Input ceiling | Fixed production-policy configuration |
|---|---|---:|---|
| `filter-asciihex` | PdfFilterPipeline with PdfAsciiHexFilterDecoder | 16 KiB | Maximum decoded bytes 32 KiB; expansion ratio 32; filter-count ceiling 4; buffer size 1 KiB. Exactly one selected filter, no decode parameters. |
| `filter-flate` | PdfFilterPipeline with PdfFlateFilterDecoder | 16 KiB | Same limits; exactly one Flate filter, no active predictor configuration. |
| `font-sanitize` | PdfOpenTypeFontSanitizer.Sanitize | 64 KiB | Maximum input and single-table bytes 64 KiB; 32 tables; 256 glyphs; 16 CMap subtables; 64 name records. |

The two existing filter IDs were retained. `filter-asciihex` is the initial text-filter target; this slice does not claim the full proposed `filter-text-binary` family, predictor matrix, chains or mutation codec. Font subsetting and other proposed target families remain outside this slice.

Adapters call the real production entry points and existing resource-limit types. No parser, decoder or sanitizer algorithm was copied. The input ceiling is checked before invoking the adapter or copying its input. Font input is copied into a read-only, adapter-owned MemoryStream and disposed after sanitization. Successful font results use the sanitizer's existing SHA-256 property, avoiding trusted-buffer access and an additional output-font copy.

## Registry and invocation contracts

- Registrations contain immutable ID, positive configuration version, positive maximum input size, validated isolation classification and a private byte-input delegate.
- IDs contain at most 64 lowercase ASCII letters, digits or hyphens. Invalid metadata fails at construction.
- Registry construction admits at most 256 entries, owns a separate array and lookup dictionary, rejects null entries and duplicate IDs before publication, and exposes a read-only list sorted with StringComparer.Ordinal.
- Lookup uses StringComparer.Ordinal and rejects unknown IDs with ArgumentException. Null IDs retain argument-validation failure. Diagnostics do not interpolate supplied input or identifiers.
- Existing `TargetRegistry.All`, `TargetRegistry.Get` and `FuzzTarget.Run(input)` call shapes remain available. A separately constructed registry supports the same ordinal lookup through Find.
- Run checks admission, checks cancellation, invokes the private adapter with the caller token and checks cancellation again before returning. A null result is an unexpected failure. The delegate is not exposed as a way to bypass Run's guard.

Configuration version 1 denotes the fixed limits and target behavior above. Input bytes cannot supply replacement resource policies. No filesystem discovery, clock-dependent configuration, random generation or retained document state occurs in these adapters. Registry ordering and adapter configuration are deterministic by construction; runtime repeatability assertions were not executed in this slice.

## Bounded results and exception compatibility

FuzzResult is immutable and can be created only through validated factories. Outcome is the fixed string `success` or `rejected`. Successful observations are uppercase, 64-character SHA-256 digests. Rejection observations contain at most 64 ASCII letters, digits, hyphens or underscores. Registered adapters supply fixed production failure-enum names, not exception messages. No raw document bytes, arbitrary metadata, stack traces or paths are retained in results.

The filter adapters accept only PdfFilterDecodeException with InvalidData, DecodedByteLimitExceeded or ExpansionRatioExceeded. Cancelled, Unknown, unsupported-filter and unexpected filter-count failures are not silently accepted. Cancellation retains the production boundary's exception behavior; no broad IOException, ArgumentException or InvalidOperationException catch was introduced.

The font adapter accepts only PdfExternalFontSanitizationException with InputTooLarge, UnsupportedContainer, MalformedHeader, TooManyTables, DuplicateTable, InvalidTableRange, OverlappingTables, InvalidChecksum, MissingRequiredTable, MalformedRequiredTable or ResourceLimitExceeded. UnreadableInput is deliberately excluded because the adapter supplies its own readable MemoryStream. Cancellation and unexpected exceptions escape. OOM, stack exhaustion, arithmetic/index failures and hangs are not reclassified as malformed-input rejection.

Successful filter output is hashed after the pipeline returns. The library does not claim transactional guarantees for lower-level streaming writers, native crash containment, or process isolation merely because an isolation enum is present.

## Exact implementation files

Changes are relative to the existing uncommitted target-project scaffolding, not to an otherwise clean repository.

| File under tools/PiratePDF.Fuzz.Targets | Disposition | Change |
|---|---|---|
| FilterTargets.cs | Modified | Retain both adapters; name the input ceiling, forward cancellation and document fixed v1 decode policy. |
| FontTargets.cs | Added | Public sanitizer adapter with explicit finite limits, narrow rejection mapping and existing output digest. |
| FuzzIsolation.cs | Added | Define supervised managed-worker and dedicated-process classifications without implementing supervision. |
| FuzzResult.cs | Modified | Replace unrestricted result construction with immutable, bounded success/rejection factories. |
| FuzzTarget.cs | Modified | Validate immutable metadata, add version/isolation, retain input admission and hide the delegate behind cancellation-aware Run. |
| TargetRegistry.cs | Modified | Add owned ordinal registration/lookup, duplicate rejection, bounded registry construction and font registration. |

Total: **six implementation files: four modified, two added**. All remain below 60 lines at this checkpoint. No project-reference correction or access exception was needed.

## Validation evidence

| Check | Result |
|---|---|
| Release target-project build | Passed, zero warnings and zero errors. |
| Restore | Existing restored assets were sufficient; the successful build used no-restore. No package installation or dependency change was needed for this slice. |
| Compiler/analyzers | Passed with the repository's existing build settings; no suppression added. |
| Static contract review | Reviewed metadata validation, ordinal lookup/order, duplicate rejection, private guarded invocation, cancellation propagation, finite existing production limits and bounded result factories. |
| Focused tests | Not added or run. No appropriate B9 test project existed; the explicit task prohibited creating one and selected build/static validation in that case. |
| git diff --check | Passed. New untracked target sources separately checked for trailing whitespace and conflict markers; none found. |
| Scope preservation | SHA-256 comparison verified all 15 existing changed/untracked files outside the target project were unchanged during implementation. Ignored tool binaries were not modified. |
| Full release gate | Not run for this slice, as explicitly requested. |

Exact build command:

```powershell
dotnet build tools/PiratePDF.Fuzz.Targets/PiratePDF.Fuzz.Targets.csproj --configuration Release --no-restore --verbosity quiet
```

The earlier B9 approval checkpoint's 2,446-test gate result remains historical baseline evidence. This slice added or removed zero tests and does not present that earlier gate as fresh validation of these new contracts. Runtime boundary, cancellation, duplicate-ID and deterministic-output regressions remain for the separately authorized B9 test-project work.

## Remaining B9 work and stop boundary

No access requirement is blocked: all implemented adapters use available public production APIs. No friend declaration is necessary.

This slice does not establish SharpFuzz/.NET/native-driver compatibility or useful guided feedback. It performed no instrumentation, native-driver execution, campaign, corpus creation, replay/mutation implementation, CI integration or documentation completion for the overall batch. Existing scaffolding in those areas was preserved, without a completion claim. No assertion is made here about a successful prior smoke test.

The remaining PP-AUD-029 work includes the other approved targets, owned corpus and manifest, deterministic reproduction/replay/mutation, process supervision and its tests, toolchain compatibility evidence, bounded guided execution, failure minimization/promotion, CI replay, and eventual focused/affected/full-gate validation. POST-P2-002 therefore **cannot be closed by this slice**.

POST-P2-003 and later P2 items, canonical font editing and measurement-only P1 work were not started. No commit, push, merge, tag or branch switch occurred. Stop after this slice and its requested report for review.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-23 | Record the completed neutral target registry/contracts and three managed targets, scoped build/static validation, unchanged surrounding B9 work and remaining overall acceptance requirements. |
