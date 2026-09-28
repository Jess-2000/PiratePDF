# Post-BF-1054 Batch B9 - Neutral Single-Case Runner Report

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-031 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-1054 |
| **Last Updated** | 2026-09-23 |
| **Next Review** | 2026-12-23 |

## Disposition

The single-case runner foundation is implemented and validated on Windows x64, with the file-admission portability limitation below. **Overall B9 is incomplete; POST-P2-002 remains open.** This slice supplements [PP-AUD-029](Post-BF-1054-Batch-B9-Bounded-Fuzz-and-Corpus-Completion-Report.md) and [PP-AUD-030](Post-BF-1054-Batch-B9-Neutral-Targets-Completion-Report.md), without editing those reports or the current audit.

Work continued on `maintenance/post-BF-1054-repository-review`, HEAD `93865f639acf96f0b9598c97b04d49cea38e8260`, preserving the existing uncommitted B9 work. Root AGENTS.md, PROJECT-CHARTER.md, both reports and the existing contract tests were inspected. No commit, push, merge, tag, branch switch, reset, clean or revert occurred.

## Command and protocol

```text
dotnet tools/PiratePDF.Fuzz.Runner/bin/Release/net10.0/PiratePDF.Fuzz.Runner.dll run --target filter-asciihex --input <local-case-file> --config v1
```

Exactly one each of `--target`, `--input` and `--config` is required; their order may vary. Configuration syntax follows PP-AUD-029: `v` followed by a positive decimal integer, without leading zeros. Target IDs resolve through the existing neutral TargetRegistry. The runner does not register or implement targets itself. All three current IDs are available: `filter-asciihex`, `filter-flate`, `font-sanitize`.

Unknown targets and mismatched configuration versions fail before filesystem access. Invalid arguments, unavailable/non-regular inputs and oversized inputs fail before target invocation. Reading allocates one payload buffer of the selected target's ceiling, plus a one-byte overflow probe. Length admission precedes allocation; the read loop never grows the buffer, checks cancellation and probes for data beyond the ceiling. Only the populated span reaches FuzzTarget.Run. No unbounded ReadAllBytes, filesystem discovery or directory enumeration is used by the runner.

The command prints one compact JSON record followed by a newline. Schema version 1 has exactly these ordered fields: `schemaVersion`, `target`, `configurationVersion`, `outcome`, `code`, `observation`, `exitCode`. Target metadata comes from validated registrations; observations come from bounded FuzzResult factories. The JSON is bounded by 512 UTF-8 bytes (excluding the newline). Unknown user-supplied IDs, input paths/bytes, exception messages/types/stacks, environment variables and document contents are not emitted. Results contain no timestamps or execution durations.

| Exit | Outcome | Meaning |
|---:|---|---|
| 0 | success | Target returned success. |
| 1 | rejected | Target returned its established expected rejection. |
| 2 | invalid | Usage, registry/configuration or input admission failed. |
| 3 | cancelled | Explicit caller cancellation; Ctrl+C requests the command token. |
| 4 | failure | Unexpected runner/target exception; not an accepted malformed input. |

File-access exception handling is confined to input admission/read. Target IOException, argument, arithmetic, index, null and other unexpected exceptions are failures, not newly accepted rejection classes. Requested OperationCanceledException must carry the command token; the established PdfFilterDecodeException/Cancelled representation also maps to cancellation when that token is requested. Spontaneous cancellation is failure. The adapters and their exception allowlists are unchanged.

Catchable unexpected failures produce only a fixed failure code, and the CLI exits. Actual stack exhaustion, native termination or severe resource exhaustion may prevent any managed result; this worker does not promise containment or recovery. Isolation classification remains target metadata. No child process or timeout supervision was implemented or exercised by `run`.

## File admission and limitations requiring follow-up

This slice admits ordinary **Windows local paths**. It rejects UNC/device namespaces, alternate data streams, reserved device names, directories, reparse points/links in the inspected path components, and non-seekable handles. It checks attributes again on the opened handle and uses read-only sharing. These checks are not a filesystem sandbox or an atomic defense against a malicious concurrent parent-directory replacement. File-size growth is still bounded by the read/probe ceiling.

**Non-Windows file execution fails closed with exit 2 / UnsupportedInputPlatform.** The .NET 10 public Unix attribute implementation does not expose regular-file versus FIFO classification, and ordinary FileStream open can block on a FIFO before a seekability check. See the primary [.NET file-attribute implementation](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/IO/FileStatus.Unix.cs) and [handle-open implementation](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/Microsoft/Win32/SafeHandles/SafeFileHandle.Unix.cs). No native calls, external helper or dependency was added to work around that limitation. Portable admission needs separate design/authorization before the proposed Linux replay integration; cross-platform execution is not claimed by this report. Tests explicitly assert the fail-closed result on non-Windows rather than treating it as successful target execution. This run's evidence is Windows only.

The pre-existing `smoke` command and ChildProcess scaffold are preserved; their output and compatibility behavior are not validated or extended by this slice. No SharpFuzz, instrumenter, native driver or fuzz campaign was executed. The manual tool provisioning was not modified. Replay/mutation, corpus/manifest creation, supervision, CI and other P2/roadmap work remain outside this slice.

## Exact files changed in this slice

Changes are relative to the already dirty working tree, not the committed baseline.

| File | Change |
|---|---|
| tools/PiratePDF.Fuzz.Runner/Program.cs | Dispatch neutral run/usage to the worker; connect Ctrl+C cancellation; preserve smoke scaffold. |
| tools/PiratePDF.Fuzz.Runner/RunCommand.cs | Add finite option parsing, registry/version validation, invocation and separate failure/cancellation mapping. |
| tools/PiratePDF.Fuzz.Runner/InputFile.cs | Add bounded local-file admission and reading. |
| tools/PiratePDF.Fuzz.Runner/RunResult.cs | Add bounded JSON result and explicit exit-code values. |
| tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj | Add test-only reference to the existing Runner project. |
| tests/PiratePDF.Fuzz.Tests/RunCommandTests.cs | Add 33 focused deterministic runner cases. |
| docs/reviews/Post-BF-1054-Batch-B9-Neutral-Runner-Completion-Report.md | Record this slice and evidence. |

Total: **7 files**. No production implementation, production API, target implementation, NuGet dependency, friend declaration, solution, CI, corpus, SharpFuzz or existing report/audit change. SHA-256 comparison confirmed that the other 28 pre-existing modified/untracked files were unchanged, including ChildProcess and all target files. No pre-existing file disappeared.

## Validation evidence

Environment: Windows x64, SDK 10.0.302, test host .NET 10.0.10.

| Check | Actual result |
|---|---|
| Runner Release build | Passed; zero warnings/errors. |
| Complete Fuzz.Tests Release suite | **78 passed, 0 failed, 0 skipped**: original 45 target/registry tests plus 33 new runner cases. |
| Direct CLI checks | Actual process exits 0 (owned ASCIIHex terminator), 1 (malformed ASCIIHex), 2 (configuration mismatch before missing-file access); one bounded JSON record each. Temporary success input was removed. |
| Compiler/analyzers | Build passed with current settings; no suppression added. |
| git diff --check | Passed; added/untracked slice files also checked separately for whitespace errors. |
| Full release gate | Not run, explicitly outside this slice. |

Commands:

```powershell
dotnet build tools/PiratePDF.Fuzz.Runner/PiratePDF.Fuzz.Runner.csproj --configuration Release --no-restore --verbosity quiet
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj --configuration Release --verbosity quiet
dotnet test tests/PiratePDF.Fuzz.Tests/PiratePDF.Fuzz.Tests.csproj --configuration Release --no-restore --verbosity quiet
git diff --check
```

The initial restore attempt was blocked by sandbox access to the existing user NuGet.Config; the authorized retry succeeded without dependency changes. The initial test run found one incorrect new assertion: a one-byte font is MalformedHeader, not UnsupportedContainer. The production source confirmed the contract; only the test expectation was corrected. Two additional contract cases were then added; the final complete run passed all 78 tests.

New coverage includes known successful decoding, rejection from all three existing targets, ordinal unknown IDs, configuration mismatch/syntax, invalid/duplicate/missing CLI options, unavailable/directory/device/stream paths, exclusive file locks, empty/exact-limit/limit+1 admission and invocation counts, non-default registration metadata, cancellation before/during invocation and filter cancellation, foreign/spontaneous cancellation, synthetic unexpected defects, repeat output across culture/path/option order, maximum metadata sizes, sensitive-output exclusion and explicit exit-code values. No external fuzz tools are spawned by these tests. Real native crashes, actual OOM/stack exhaustion, file-growth races, Ctrl+C delivery and timeout supervision were not exercised.

The historical 2,446-test release-gate baseline is not a fresh gate result for this work. No overall B9 acceptance or finding closure is claimed.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-23 | Record the bounded neutral single-case worker, 33 runner cases, Windows validation and explicit portability/supervision limits. |
