# Post-BF-1054 Batch B8 - Font Catalog Responsiveness

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-028 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-23 |
| **Next Review** | 2026-12-23 |

## Scope and baseline

B8 addresses POST-P2-001 only: cancellation, retry, aggregate discovery bounds and complete snapshot publication in the existing filesystem font provider. **POST-P2-001 is implemented and closed after the full unmodified release gate passed 2,446 tests across 20 projects with zero failures/skips and zero build warnings/errors.** All fifteen P0 findings remain closed. POST-P2-002 through POST-P2-008 remain untouched, AUD-P1-001/002 remain measurement-only, and canonical font editing has not started.

Branch: `maintenance/post-BF-1054-repository-review`. Starting and final uncommitted HEAD: **f9c0ccfb29c80fcb27ff0d1dc84b53e1d0c01570**, Remove unused imports and dead implementation code. Initial working tree and diff were clean. B7 and the repository cleanup are committed. No commit, push, merge, tag or branch switch occurred during B8.

Governing context includes root AGENTS.md, charter/README, current audit and cleanup report, B4A/B4B/B4C and B6 completion reports, BF-1054H/I/Q/R external-font contracts, engineering/security guidance, ADR-0010 bounded materialization, and release validation. Historical reports remain historical. The authoritative Invoke-PiratePDFReleaseGate.ps1 is unchanged; Test-PiratePDFSourceFileSizes.ps1 remains intentionally absent.

## Actual request path and pre-fix evidence

PdfExternalFontCatalog.Match calls its GetFaces, which forwards the caller token to each IPdfExternalFontProvider.GetFaces. The Windows/macOS adapters inherit PdfFileSystemExternalFontProvider. Previously that provider checked cancellation once, then entered a shared Lazy with ExecutionAndPublication. Its parameterless BuildSnapshot traversed configured directories, selected distinct TTF/OTF paths, opened a FileStream, called IPdfExternalFontSanitizer.Sanitize without the token, inspected trusted metadata, and built local entries. Lazy published entries only after BuildSnapshot returned. GetFaces then sorted/materialized faces on every call.

PdfSanitizedExternalFontMetadataInspector has no cancellation parameter. SupportsRune and OpenFontProgram resolve an entry from the same snapshot; those APIs have no cancellation parameter. Services receives a selected provider/face and opens the program for its separately bounded sanitizer/embedding workflow; it does not add a hidden discovery path. The existing SDK resource-name editing path is not a platform catalog API. No Services/SDK or shared Fonts contract changes were needed.

| Characterization | Pre-fix evidence |
|---|---|
| Already canceled cold request | Existing regression passes: original token propagated, zero sanitizer calls, subsequent call succeeds. |
| During enumeration, before/after sanitization, between files, after final font before publication | Five new deterministic callback cases fail because no OperationCanceledException is thrown. The caller token never reaches build/sanitizer work. |
| Sanitizer itself throws cancellation | First call throws; a later call rethrows the cached exception instead of retrying. New permanent retry regression fails. |
| Partial publication | Source has local entries and one Lazy publication; an escaping failure cannot publish a partial dictionary. Mid-build caller cancellation was ignored, so it could publish a complete but canceled request's result. No pre-fix partial dictionary exposure is claimed. |
| Malformed/valid encounter order | Existing seven ordered-directory cases pass, including bad-before-valid, valid-before-bad, unsupported mapping and duplicates. |
| Warm access | New warm control passes: a pre-canceled request throws without destroying the completed snapshot; later access does not resanitize. |
| Concurrent successful callers | New barrier control passes: one cold build, complete equivalent face results. |
| Aggregate candidates/bytes/traversal | Source-established absence: no aggregate counters/options; every distinct candidate is attempted subject only to each sanitizer invocation's limits. This is not a latency or memory benchmark. |

Before production edits, the complete platform run was **38 cases: 32 passed, six failed as expected**. All 30 existing tests passed. Eight new cases used existing sanitizer/protected-selection seams, tiny generated fonts and events; no sleep synchronization or broad filesystem abstraction was introduced.

A directly exposed traversal gap was also characterized during implementation: bounding Directory.EnumerateFiles results misses immediate subdirectories filtered internally by the BCL. The three-case subdirectory matrix initially passed two controls and failed limit+1. Counting immediate filesystem entries fixes this same aggregate-policy gap without recursion or a new path-security policy.

## Cancellation, publication and concurrency contract

- GetFaces remains synchronous and checks its caller token before either cold construction or warm retrieval. A pre-canceled warm request still throws; the completed snapshot remains valid.
- Checks occur before traversal, per directory, before and after iterator advancement, before candidate admission/opening, after opening and before sanitization, before/after underlying reads, after sanitization/before metadata inspection, after inspection, between files including omitted failures, before/after final sorting/materialization, and immediately before publication.
- The original token is forwarded to Sanitize. Metadata inspection has checks on either side and remains bounded by existing per-font constraints. Sorting has checks on either side and is bounded by admitted candidates. No unrelated API gained cancellation.
- A monitor serializes cold builds as Lazy previously did. The caller that acquires the build lock owns that build's token. Other callers wait with their own cancellation checks using a 50 ms lock-attempt interval; that interval is not a work budget or a promised response time.
- A canceled waiter does not cancel the owner. A canceled owner publishes nothing and releases the lock; an uncanceled waiter can then build afresh. Recursive same-thread initialization still fails with InvalidOperationException instead of recursing.
- Entries and the sorted face array remain private until successful construction and the final token check. One Volatile.Write publishes the complete snapshot. ReadOnlyCollection protects returned lists; immutable faces/metadata retain their established ownership. No partial face list is returned on cancellation, aggregate exhaustion or unexpected failure.
- Cancellation and aggregate exhaustion are not cached. A later explicit call gets a fresh budget and rebuilds from current files. Identical over-limit state fails again; reducing source files or constructing a provider with explicit larger finite limits permits success. There is no automatic retry loop.
- Other unexpected exceptions retain Lazy's failure-caching behavior through ExceptionDispatchInfo. This preserves the existing programming-error/OOM regression contract and original exception identity. Narrow per-file omission remains separate.
- Successful snapshots still live for the provider lifetime, with no refresh/invalidation facility. Changes to source files after success do not rebuild metadata. OpenFontProgram retains its existing caller-owned stream and source-change semantics.
- No task, background thread, cancellation registration or caller-owned CancellationTokenSource is introduced or retained. Tests use tasks/events only to exercise concurrent callers. File streams and enumerators are disposed on all exits.

Cancellation is cooperative: an in-progress synchronous OS operation, bounded metadata operation or sort is not forcibly interrupted. Cancellation racing after the final check may coincide with successful publication; already completed state is not retroactively canceled. Host-provided sanitizer callbacks must honor their existing synchronous contract; B8 is not isolation against arbitrary host code.

## Aggregate policy and exact boundaries

PdfExternalFontCatalogLimits is immutable through init-only properties and validated at provider construction. All limits must be positive; zero/negative values throw ArgumentOutOfRangeException before directory enumeration. There is no unlimited sentinel. Existing constructors remain and use defaults; additive explicit-limits overloads preserve existing source and binary call signatures without null-overload ambiguity.

| Limit | Default and unit | Accounting and enforcement | Arithmetic and boundary |
|---|---|---|---|
| MaximumSearchDirectories | 64 supplied directory values | Constructor counts every value, including blanks/duplicates, before path normalization or list/set insertion. Normalized paths still deduplicate using the existing OS comparer and retain input order. | int; reject before increment when count equals maximum. N values allowed, N+1 rejected before retaining it. A constructor failure publishes no provider; retry uses a new construction. |
| MaximumDirectoryEntries | 65,536 immediate filesystem entries | Cold build counts each yielded entry, including non-font files, subdirectories and repeated entries from overlapping discovery. Check precedes directory filtering/path work. Missing configured directories consume only the supplied-directory budget. | int; pre-increment equality check. Exact count succeeds, next yielded entry throws before candidate work. |
| MaximumCandidates | 4,096 distinct supported normalized paths | Count before seen-set growth/opening. Malformed, unsupported-mapping, empty and unreadable supported candidates consume a unit. Non-font extensions consume only the entry budget. Repeated normalized candidate paths are not admitted twice. | int; pre-increment equality check. At most N candidates/entries retained; N+1 is not opened/sanitized. |
| MaximumSourceBytes | 1,073,741,824 bytes (1 GiB) | Preflight opened stream Length against remaining budget before sanitizer materialization. A read wrapper charges actual logical source reads across candidates, including malformed files, EOF-probe work and rereads after seeking. Unreadable files consume candidate work but no successful read bytes. | long; check count against maximum-minus-consumed before addition. Exact EOF is accepted. At exhaustion, at most one additional undelivered byte distinguishes EOF from excess; it throws before downstream processing. Failure remains sticky for the build. |

These are conservative safety/readiness ceilings, not measured optimal values. Standard adapter configurations have only a few directories; 64 permits explicit custom search sets while bounding constructor materialization. 65,536 bounds enumeration independently of font admission. 4,096 permits a substantial installed font catalog while bounding entries and final sorting. The 1 GiB read ceiling complements, rather than replaces, each file's 64 MiB sanitizer ceiling. A read-only local Windows metadata observation found **507 supported files, 218,258,770 total bytes, maximum 21,894,056 bytes**. This supports headroom over one realistic installation only; no platform-wide compatibility, speed or memory guarantee is inferred. Applications with larger intentional collections must choose explicit finite limits or narrower search scope.

The provider supports standalone TTF/OTF only and creates at most one face per admitted file; TTC/WOFF/WOFF2 and unsupported mapping paths retain their existing rejection/omission behavior. Thus retained faces are bounded by MaximumCandidates; an independent face option would duplicate this invariant. Metadata/cmap retention is further bounded by the existing per-font input/table structure and aggregate source-read ceiling. These are structural work bounds, not an exact peak-managed-memory budget.

The source-byte counter measures bytes returned by the underlying stream, not filesystem read-ahead, disk traffic or canonical output bytes. FileStream's existing fixed buffer and sanitizer's bounded buffer/copies remain. File length is only an early rejection hint, not authoritative evidence of later content. Actual reads, including custom sanitizer rereads, cannot bypass the counter; a sticky failure also prevents a custom sanitizer from translating exhaustion into an omittable malformed exception. Controlled underreported-length streams prove the read boundary without modifying large files or depending on races.

long.MaxValue is handled without wrapping: subtraction precedes addition, EOF request narrowing is conditional, and a synthetic budget test consumes maximum-minus-one, exact maximum and one-over without large allocation. Configured int maxima likewise cannot overflow because admission rejects before the next increment. One iterator lookahead is necessary to distinguish exactly N entries from N+1. Arbitrary host enumerable MoveNext calls and OS I/O have no CPU-time guarantee.

Enumeration remains lazy and top-directory-only. Immediate subdirectories now consume enumeration units but are not traversed or treated as candidates. Successful face sorting is unchanged: family and face name OrdinalIgnoreCase, then FaceId Ordinal. FaceId remains SHA-256 of the normalized path. Physical hardlinks/symlink aliases are not coalesced by inode; distinct normalized paths count separately. No reparse resolution, recursive search or native-library policy work was added.

## Exceptions and compatibility

Budget exhaustion throws the new **PdfExternalFontCatalogLimitException : InvalidOperationException**, exposing LimitName and Limit with an actionable path-free message. It is distinct from OperationCanceledException and individual font failures, and is excluded from failure caching. Constructor directory exhaustion uses the same exception; malformed limit values use existing BCL argument exceptions.

The per-candidate filter is unchanged: IOException, UnauthorizedAccessException, PdfExternalFontSanitizationException, InvalidDataException and NotSupportedException remain omittable. Cancellation, aggregate exhaustion, programming errors and synthetic OutOfMemoryException are not omitted. Post-catch cancellation/budget checks prevent either condition from becoming a silent omission. Missing-after-enumeration, disappearing directory, exclusive sharing, injected access denial and IO failures still permit a later valid candidate; real Windows exclusive reopen checks verify disposal.

Directory creation/enumerable-acquisition IO/access failures keep the existing skip filter. Exceptions thrown later by the lazy enumerator's GetEnumerator/MoveNext remain escaping failures, as before, and retain unexpected-failure caching. B8 does not broaden filesystem exception recovery without evidence. Path-normalization catches remain ArgumentException, NotSupportedException and PathTooLongException. Cancellation precedence is established at checks; simultaneous independent failures are not given a new global ordering contract.

| Compatibility question | Answer |
|---|---|
| Public API changed? | Yes, additive limits/exception types and three-argument adapter constructors only. Existing constructors/methods/constants remain. |
| Protected API changed? | Yes, additive four-argument base constructor. Existing constructor and IsSupportedFontFile override remain. |
| Interface changed? | No. GetFaces already had the token; no shared Fonts contract changes. |
| Matching/order or successful metadata changed? | No. Equivalent complete results and ordered identities are tested. |
| B6 embedding rights changed? | No. Existing installable/restricted/preview-print/editable, no-subsetting and bitmap-only tests still pass. |
| B4 ownership/sanitizer/subset bounds changed? | No. Trusted byte ownership and all per-font validation/exception policies are untouched. |
| Malformed/IO policy changed? | No filter changes; cancellation and aggregate aborts are intentionally distinct build outcomes. |
| Dependency or project reference changed? | No. A Platform-only test friend assembly allows safe synthetic counter/read tests; no Fonts trusted-buffer access is exposed. |
| Concrete sanitizer input stream changed? | Yes, a bounded readable/seekable Stream wrapper replaces direct FileStream delivery. IPdfExternalFontSanitizer promises Stream, not FileStream. Custom implementations must not downcast to FileStream or retain build-owned input. OpenFontProgram's public FileStream behavior is unchanged. |

Intentional behavioral differences are mid-build cancellation/retry and rejection of over-budget configurations/builds. These may expose collections previously processed without aggregate bounds. Unsupported arbitrary concrete-stream assumptions are a compatibility concern for custom sanitizers; they cannot be retained while guaranteeing aggregate actual-read accounting. There are no removed/renamed public or protected signatures, and no interface/SDK redesign.

Existing sanitizer defaults remain input 64 MiB, single table 32 MiB, tables 128, glyphs 65,535, CMap subtables 256, name records 4,096. Subset defaults remain retained glyphs 16,384 and generated bytes 64 MiB. B8 does not duplicate or weaken those checks or any rights, transaction, serialization or preservation boundary.

## Work and allocation implications

Each provider retains four scalar limits, a monitor, snapshot/failure references and a build flag. Each build adds two int counters, one long counter and optional failure state. A lightweight stream wrapper surrounds each existing file stream; it introduces no full-font copy. Directory configuration remains eagerly owned but is bounded before collection growth. Lazy entry enumeration remains; counting subdirectories adds a bounded Directory.Exists check per admitted immediate entry.

The previous per-call sort/materialization moves into the complete publication boundary; the snapshot now also retains a bounded array of face references. Each GetFaces still returns a fresh read-only wrapper. Retained metadata is otherwise unchanged. This makes publication include sorting and materialization, but is not a throughput or allocation-optimization claim. Source bytes/canonical font ownership and sanitizer copies are unchanged. Canceled work structurally stops at a checkpoint, but cold/warm latency, peak memory, GC cost and default tuning remain **measurement required**. No pools, background workers, telemetry, raw bytes or private paths were added to diagnostics.

## Permanent tests and validation

The platform suite grows from **30 to 98 tests: 68 additions, no removals**. ExistingFailureFilterRemainsNarrow changes only its cancellation retry-count assertion from one invocation to two; all exception identity/type and other cache assertions remain. No assertions were weakened.

| File | Added cases and purpose |
|---|---|
| PdfFileSystemExternalFontProviderTests.Responsiveness.cs | 8: five cancellation boundaries, sanitizer cancellation retry, warm cancellation and shared successful cold construction. |
| PdfFileSystemExternalFontProviderTests.AggregateLimits.cs | 41: below/exact/over candidates, entries including subdirectories and malformed candidates, source sums, directory configuration, malformed/non-font/duplicate handling, zero/tiny input, validation, cancellation interaction, repeat/recovery and macOS adapter propagation. |
| PdfFileSystemExternalFontProviderTests.BuildFailures.cs | 13: caller-token ownership, owner retry, IO/disappearance/sharing/access filters, malformed/cancellation, sticky aggregate failure, actual catalog matching and reentrancy. |
| PdfFontCatalogBuildBudgetTests.cs | 6: synthetic long overflow, underreported lengths, EOF, rereads/array reads, and cancellation during underlying I/O. |

Atomicity tests change source identities/add valid files after failure, then compare the retry's full ordered metadata against an independent control. Exclusive FileShare.None opens prove files are released after success, malformed omission, cancellation and aggregate failure. Concurrency uses callbacks/events with bounded safety timeouts, never sleeps as the synchronization mechanism. Fixtures are tiny synthetic fonts in real temporary filesystem directories; no installed font is a correctness dependency.

| Check | Result |
|---|---|
| Focused cancellation/read-bound/subdirectory regressions | 17 passed, 0 failed/skipped |
| Complete Fonts.Platform Release suite | 98 passed, 0 failed/skipped |
| Complete Fonts Release suite (additional B4/B6 assurance) | 228 passed, 0 failed/skipped |
| Compiler/Roslyn unused-import checks in both affected projects | dotnet format style with IDE0005, verify-no-changes and no-restore passed |
| Supplemental acceptance | Permanent real-filesystem controls above pass; local system observation is metadata-only and not acceptance-dependent. No separate benchmark or reopen harness is necessary for this discovery-only boundary. |
| Release solution build and full unchanged gate | Passed, zero warnings/errors; every gate stage passed |
| Full test count reconciliation | 2,378 + 68 = **2,446**, across 20 projects; zero failures/skips |
| Other complete gate suites relevant to compatibility | Services 524, SDK.Adapters 35, SDK 26, Editing 133, Writing 153, Parsing 219; all passed |
| Integrated source-size | Passed: 1,074 inspected files, no hard-limit violation; existing 943/844-line review notices unchanged |
| DocGen all/health/repair | 195 documents; zero errors/warnings/information; 91.6%, A-; repair zero changes/files/diagnostics |
| git diff --check | Passed before and in the gate, and after final documentation updates |

Commands: dotnet test on each named project with Release and no-restore; the focused run uses no-build and filters cancellation, counter/read and subdirectory regressions. Full acceptance command: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1`, without skip flags or script changes. Services/SDK contracts are unchanged; their full suites remain included in the gate.

The first gate passed 2,443 tests. Final coverage review added three malformed-candidate directory-entry boundary cases, with no production change; the complete platform suite and full unchanged gate were rerun and passed at 2,446. No production/test edits followed that final gate. Completion-only documentation updates were followed by clean DocGen all/health/repair and whitespace checks.

## Exact file inventory

Eight production files:

- `src/BlackFlag.Engine.Fonts.Platform/PdfFileSystemExternalFontProvider.cs`
- `src/BlackFlag.Engine.Fonts.Platform/PdfExternalFontCatalogLimits.cs` (new)
- `src/BlackFlag.Engine.Fonts.Platform/PdfExternalFontCatalogLimitException.cs` (new)
- `src/BlackFlag.Engine.Fonts.Platform/PdfFontCatalogBuildBudget.cs` (new)
- `src/BlackFlag.Engine.Fonts.Platform/PdfFontCatalogReadStream.cs` (new)
- `src/BlackFlag.Engine.Fonts.Platform/Properties/AssemblyInfo.cs` (new)
- `src/BlackFlag.Engine.Fonts.Platform/Windows/PdfWindowsExternalFontProvider.cs`
- `src/BlackFlag.Engine.Fonts.Platform/MacOS/PdfMacOSExternalFontProvider.cs`

Five test files:

- `tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFileSystemExternalFontProviderTests.Responsiveness.cs` (new)
- `tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFileSystemExternalFontProviderTests.AggregateLimits.cs` (new)
- `tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFileSystemExternalFontProviderTests.BuildFailures.cs` (new)
- `tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFontCatalogBuildBudgetTests.cs` (new)
- `tests/BlackFlag.Engine.Fonts.Platform.Tests/PdfFileSystemExternalFontProviderTests.NumericFailures.cs`

Documentation scope: this report, `docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md`, and normal generated `docs/Documentation-Index.md`, `docs/Documentation-Health.md`, `docs/ValidationReport.md`. **Final changed-file count: 18 (eight production, five test, five documentation).** No historical report, release script, dependency or project file is changed. The generated repair report has no content diff.

No touched production or new test file approaches the 600-line decomposition review range. The preexisting large shared test font-builder file is reused without modification. The gate's 1,074-file count excludes assembly metadata by its existing policy; this is not a new whole-repository audit inventory.

## Remaining limitations and stop boundary

Bounds apply per filesystem provider build, not to arbitrary third-party providers or an arbitrary host-supplied number of providers. The portable catalog/matching architecture is unchanged. No hard deadline or process isolation is claimed for synchronous OS operations/custom host code. macOS adapter configuration is exercised with synthetic directories on Windows; packaged macOS enumeration/native readiness is not established. Lifetime cache invalidation and source-file change semantics are unchanged.

POST-P2-002 through POST-P2-008, AUD-P1-001/002 measurement work and canonical font editing remain untouched. All fifteen P0 closures remain intact. B8 stops after acceptance for user review, without starting another batch or performing any Git publication action.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-23 | Characterize and correct POST-P2-001 cancellation/retry and aggregate discovery policy; record compatibility and validation. |
