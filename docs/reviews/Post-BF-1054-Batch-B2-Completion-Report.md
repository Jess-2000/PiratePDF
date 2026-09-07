# Post-BF-1054 Batch B2 - JPEG Preallocation Validation

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-011 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1048, BF-1054 |
| **Last Updated** | 2026-09-07 |
| **Next Review** | 2026-12-07 |

## Scope and status

The user approved **B2 only: POST-P0-002, native JPEG intrinsic dimensions reaching pixel allocation before established limits** from the [post-BF-1054 audit](Post-BF-1054-CSharp-Repository-Audit.md). B1 was already incorporated in the clean starting tree. Branch: `maintenance/post-BF-1054-repository-review`; starting commit: `b7666d55b83ca725bcfac1c3b596453304913fba`.

B2 is implemented and validated. The full unmodified release gate passed with 1,732 tests and zero build warnings or errors. No B3 or later finding, public API, dependency, project reference, native package version, loader policy or historical milestone is changed. No commit, push, merge, tag or branch switch was performed.

## Characterization and smallest compatible seam

Before production changes, the call chain was reviewed from `SkiaPdfPageRenderer.RenderImage` through `PdfImageDecoder.Decode` to `SkiaDctImageCodec.Decode`. The renderer supplies `MaximumSurfacePixels` through the established `PdfImageDecodeLimits`. The neutral decoder checks encoded bytes and dictionary dimensions, decodes prefix filters, invokes `IPdfImageCodec`, then checks returned dimensions. The original Skia codec reads intrinsic dimensions from `SKCodec.Info`, allocates an `SKBitmap` and decodes pixels before the outer check; checked managed-copy arithmetic also ran after native allocation.

The original `IPdfImageCodec` has no per-call limit parameter. **It remains unchanged.** A small internal `IBoundedPdfImageCodec` capability carries the existing limits to the repository Skia implementation. The decoder selects it when present and retains the original entry point and final dimension check for legacy codec implementations. No second public codec interface, replacement JPEG header parser, mutable ambient state or independent limit model is introduced.

Dimension and encoded-length policy remain in the neutral decoder, exposed internally to the repository Skia adapter through the existing friend-assembly convention. Skia's explicit bounded implementation and original public Decode method share one implementation; direct public calls use existing default limits. The parameterless public constructor and all original public signatures remain available. Another friend declaration exposes only Skia internals to its test assembly, allowing a per-instance bitmap allocation hook without a public testing API.

This is a compiler visibility arrangement for repository assemblies, not a plugin isolation or native-loader security mechanism. Third-party implementations of the original public codec contract retain their previous behavior and remain responsible for their own allocation safeguards.

## Exact production files changed or added

| File | Change |
|---|---|
| `src/BlackFlag.Engine.Rendering/Images/PdfImageDecoder.cs` | Select the internal bounded codec capability; share existing policy checks; validate positive dimensions, checked RGBA size and array representability before codec/native allocation |
| `src/BlackFlag.Engine.Rendering/Images/IBoundedPdfImageCodec.cs` | New internal per-call limit seam using `PdfImageDecodeLimits` |
| `src/BlackFlag.Engine.Rendering/Properties/AssemblyInfo.cs` | Grant the repository Skia adapter internal access to the seam and shared checks |
| `src/BlackFlag.Engine.Rendering.Skia/Images/SkiaDctImageCodec.cs` | Validate intrinsic size before pixel allocation; check allocation and pointer/capacity results before native decoding or copying; internal allocation hook |
| `src/BlackFlag.Engine.Rendering.Skia/Properties/AssemblyInfo.cs` | New friend declaration for focused Skia tests |

`IPdfImageCodec.cs`, `PdfImageDecodeLimits.cs`, package/project files and release-gate scripts are unchanged.

## Preallocation enforcement

The bounded path performs these steps in order:

1. Preserve the outer encoded-size and PDF dictionary validation. Validate the actual codec payload against the same encoded-byte limit before copying it into `SKData`.
2. Create `SKCodec` to inspect intrinsic dimensions. Header inspection can allocate native metadata; it does not allocate the destination bitmap's pixel buffer.
3. Apply the shared positive-width/height, maximum-dimension and decoded-pixel checks to those intrinsic dimensions.
4. Check the int-sized pixel product and four-byte RGBA product, then the managed array ceiling. These checks are required by the existing `PdfDecodedImage` and copy representation. Establish the RGBA row size before constructing/allocating the bitmap.
5. Check cancellation, then request pixels with `TryAllocPixels`. Its false result is converted to a null allocation result after disposing the bitmap handle.
6. Reject a null bitmap, null pixel pointer, insufficient native buffer length, wrong dimensions or wrong row stride before calling native `GetPixels` or `Marshal.Copy`.
7. Preserve the existing successful/incomplete-input decode result handling and RGBA packing, with cancellation checks before managed copying and in the row loop.

The existing defaults remain **100,000 maximum dimension, 100,000,000 decoded pixels and 256 MiB encoded bytes**. No configurable threshold is reduced or increased. Representability checks reject sizes the current RGBA representation cannot hold, even when a caller configures larger pixel limits.

The [SkiaSharp bitmap API](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skbitmap?view=skiasharp) documents the allocation and pixel access operations; the installed 4.150.1 API XML was also checked for the exact overloads used. Implementation follows repository policy rather than copying an external decoder implementation.

## Exception and compatibility effects

| Condition | Result |
|---|---|
| Configured width/height or pixel limit exceeded | Existing `InvalidDataException` type and limit messages; intrinsic failures now occur before pixel allocation |
| Nonpositive intrinsic dimensions | Predictable `InvalidDataException` from codec/header or shared dimension validation |
| Pixel or decoded-byte multiplication overflow | `InvalidDataException` with the original `OverflowException` as its inner exception, before pixel allocation |
| Managed array ceiling exceeded | `InvalidDataException` before native or managed pixel allocation |
| Native allocation returns failure or an unusable pixel buffer | `InvalidDataException` before native output or managed copying |
| Malformed/truncated JPEG header | Existing `InvalidDataException` behavior and unsupported-JPEG message retained |
| Native `Success` or `IncompleteInput` | Existing acceptance retained; other codec result failures retain their existing message/type |
| Cancellation | Original-token `OperationCanceledException`; no conversion into an image diagnostic or budget exception |
| Declared/intrinsic dimension mismatch within limits | Existing acceptance retained; decoded image dimensions remain the intrinsic dimensions |

Overflow previously could surface after native allocation as `OverflowException`; converting it to `InvalidDataException` is a deliberate correction so existing strict/tolerant image failure handling works predictably. Tolerant rendering reports `IMAGE_DECODE_FAILED`; strict rendering propagates `InvalidDataException`. Native false/null results now receive the same controlled failure category instead of reaching a null-pointer operation or a less-specific allocation error.

Direct Skia calls now enforce the existing default encoded/intrinsic limits. A prefix-filter result passed to Skia is also checked against the caller's encoded-byte ceiling; the original encoded-input check and prefix filter budgets remain in place. This closes the unchecked adapter entry without a new policy model.

Characterization confirmed that removing the tiny fixture's JPEG end marker returns an accepted image with transparent pixels on this package/runtime; B2 preserves that result. It does not introduce strict completeness requirements or change decoder color/format support. PDF dictionaries and encoded source bytes are not rewritten.

## Exact tests added

| File | Coverage |
|---|---|
| `tests/BlackFlag.Engine.Rendering.Tests/Images/PdfImageDecodeLimitTests.cs` | 11 cases: declared pixel/byte overflow, width/height and invalid-dimension rejection, encoded exact/over limit, legacy codec compatibility/postcheck, exact limit-object/token forwarding |
| `tests/BlackFlag.Engine.Rendering.Skia.Tests/Images/SkiaDctImageCodecTests.cs` | 7 cases: exact small JPEG pixels, within-limit mismatch, four truncated headers, characterized incomplete-input acceptance; current native module evidence in test output |
| `tests/BlackFlag.Engine.Rendering.Skia.Tests/Images/SkiaDctImageCodecLimitTests.cs` | 14 cases: intrinsic width/height exact/over limits, pixel limit, checked pixel/byte overflow, zero dimensions, direct defaults, allocation failure/null/undersized pixels, malformed input and cancellation |
| `tests/BlackFlag.Engine.Rendering.Skia.Tests/Images/SkiaJpegRenderingTests.cs` | 3 integration cases: tolerant failure diagnostic, strict failure and valid JPEG at the exact pixel limit |
| `tests/BlackFlag.Engine.Rendering.Skia.Tests/Images/SkiaJpegTestData.cs` | Tiny generated JPEG/display-item fixture and controlled SOF-header mutation; no hostile-size source bitmap |

The pre-change neutral regression run demonstrated **2 failures and 8 passes**: unrepresentable pixel and RGBA byte sizes reached the legacy codec. Seven native characterization cases then passed against the original codec after correcting test-only imports and the initially assumed incomplete-image pixels to match observed behavior. No existing assertion was weakened.

Intrinsic overflow fixtures patch only a tiny JPEG header to 50,000 by 50,000 pixels or 32,768 by 16,384 pixels. The allocation hook throws immediately if reached. Tests assert an `InvalidDataException` containing `OverflowException`, proving the checked validation was reached while no pixel allocation was requested. Similar hooks establish ordering for dimension/pixel limits and malformed input. Null and one-pixel bitmap replacements simulate unusable allocations safely. No destructive allocation, timing-based cancellation, machine-memory-dependent test or exception swallowing is used.

## Platform and native validation

Executed on **Microsoft Windows 10.0.26200, process architecture X64, .NET 10.0.10** using the repository's existing **SkiaSharp 4.150.1** assets. Successful JPEG encode/decode and renderer tests exercised the native boundary.

The successful test process reported the loaded module:

```text
C:\Development\PiratePDF\tests\BlackFlag.Engine.Rendering.Skia.Tests\bin\Release\net10.0\runtimes\win-x64\native\libSkiaSharp.DLL
SHA256 C8770C219E0D3CD9BB119FAD46F2D00AE1855B11315816E86E909E3332826212
```

The hash matches the installed `skiasharp.nativeassets.win32/4.150.1/runtimes/win-x64/native/libSkiaSharp.dll`. This verifies the applicable packaged native asset loaded in this test environment. It does not verify a signed installer, search-path resistance, clean-machine deployment or Windows ARM64/x86 behavior. macOS and Linux were not executed; no equivalence is claimed.

## Validation results

| Check | Result |
|---|---|
| Targeted new Rendering regressions | 11 passed, 0 failed, 0 skipped |
| Targeted Skia image regressions/integration | 24 passed, 0 failed, 0 skipped |
| Complete Rendering project | 61 passed, 0 failed, 0 skipped |
| Complete Rendering.Skia project | 25 passed, 0 failed, 0 skipped |
| Current Windows x64 native asset | Loaded from packaged runtime path; hash matched installed package |
| `git diff --check` | Passed; new untracked files checked separately as well |
| Full unmodified release gate | Passed: 1,732 tests, 0 failed, 0 skipped across 20 test assemblies |
| Release build and analyzers | 0 warnings, 0 errors |
| Source-file size gate | Passed; 1,021 files inspected; existing large-file review entries unchanged |
| DocGen validation | 178 documents; 0 errors, 0 warnings, 0 informational issues |
| Documentation health | 91.6%, A- |
| Documentation repair preview | 0 proposed changes in 0 files; 0 diagnostics |

The first full gate passed its build and all tests, then rejected the new report's `BF-1048D` metadata reference. The metadata requires the parent work item `BF-1048`; this report was corrected and the entire unmodified gate rerun successfully. No production/test adjustment, unrelated fix or gate suppression was needed. After recording the final results, documentation generation/validation, health, repair preview and whitespace checks were refreshed using the already-built DocGen application.

Commands run from the repository root:

```powershell
dotnet test tests/BlackFlag.Engine.Rendering.Tests/BlackFlag.Engine.Rendering.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~PdfImageDecodeLimitTests --verbosity quiet
dotnet test tests/BlackFlag.Engine.Rendering.Skia.Tests/BlackFlag.Engine.Rendering.Skia.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Images --logger 'console;verbosity=detailed' --verbosity quiet
dotnet test tests/BlackFlag.Engine.Rendering.Tests/BlackFlag.Engine.Rendering.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet test tests/BlackFlag.Engine.Rendering.Skia.Tests/BlackFlag.Engine.Rendering.Skia.Tests.csproj --configuration Release --no-restore --verbosity quiet
git diff --check
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

## Documentation and remaining limitations

Exact documentation files added or updated are `docs/reviews/Post-BF-1054-Batch-B2-Completion-Report.md`, `docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md`, `docs/Documentation-Index.md`, `docs/ValidationReport.md` and `docs/Documentation-Health.md`. The last three are generated by DocGen. The repair preview remained unchanged with zero proposed repairs. Historical milestone documents remain unchanged.

B2 addresses POST-P0-002's destination pixel allocation ordering and checked-copy boundary. It does **not** fix all native-image security risks. `SKData`/`SKCodec` metadata parsing still crosses the native boundary; native decoding is synchronous and cannot be interrupted mid-call through this API. Pixel limits do not constitute an aggregate process-memory or decode-time budget, and several bounded native/managed RGBA buffers can coexist. Real machine exhaustion is not simulated. General renderer surface allocations, native-loader policy, other formats and legacy third-party codec allocation behavior are outside this batch.

POST-P0-003 through POST-P0-009, all eight POST-P2 findings and both measurement-required P1 candidates remain untouched. B1's accepted implementation is retained. B3 and every later batch require separate explicit approval; no canonical font feature, plugin/native-loader redesign, dependency/SBOM change, AOT/trimming mitigation or release signing work was begun.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-07 | Record approved B2 characterization, internal bounded codec seam, preallocation checks, exception review, regression evidence and platform validation. |
