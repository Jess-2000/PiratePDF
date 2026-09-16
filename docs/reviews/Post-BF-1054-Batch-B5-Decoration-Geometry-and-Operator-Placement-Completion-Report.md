# Post-BF-1054 Batch B5 - Decoration Geometry and Operator Placement

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-024 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-15 |
| **Next Review** | 2026-12-15 |

## Scope and baseline

B5 addresses POST-P0-007 only, including the user's expressly authorized, directly exposed corrections necessary for valid decoration geometry and state preservation. Branch: `maintenance/post-BF-1054-repository-review`. Starting commit: **147587f6fe3ea5ec2c7fc9ba4ce4702971eef2d6**, Harden malformed font numeric validation. B4C was committed and the starting working tree and diff were clean. POST-P0-005, POST-P0-006 and POST-P0-015 remain closed.

Root AGENTS.md, charter, README, engineering/security guidance, the current audit and B4C report, BF-1054E/F/G/S/T/U/V contracts, relevant BF-1053 preservation contracts, ADR-0009/0010/0012 and the consolidated release gate informed this change. Historical reports remain unchanged. The consolidated gate is authoritative; Test-PiratePDFSourceFileSizes.ps1 remains intentionally absent.

**Disposition: POST-P0-007 is implemented and closed. The full unmodified release gate passed 2,098 tests across 20 projects with zero build warnings/errors, integrated source-size validation and clean DocGen.**

## Separate baseline reproductions

Both initial defect regressions failed before production edits. With two 500-unit glyphs at 12 points, translated text and explicit width 12/offset -2, the exact decoded output was:

```text
BT
/F1 12 Tf
1 0 0 1 100 200 Tm
(AB) Tj
q
0 G
1 w
0 -2 m
12 -2 l
S
Q

ET
```

**Geometry defect:** the painted endpoints remained (0,-2) and (12,-2), instead of (100,198) and (112,198). WriteDecoration ignored the target text matrix and effective text state.

**Operator-placement defect:** the second fixture included following text. Its exact sequence was the same through Q, followed by a blank line, `(CD) Tj`, and `ET`. Thus m/l/S executed between the original BT and ET. q/Q alone cannot create a legal path context. The original SDK expected string encoded this invalid ordering.

The corrected translated insertion closes text before painting, uses `q`, `1 0 0 1 100 200 cm`, the local path, and `Q`. It then starts BT, restores the line matrix with Tm, and restores horizontal advance with a number-only TJ before the untouched following text. This is tested through interpreted endpoints, not only output strings.

## Geometry and text-state strategy

The authoritative geometry types are PdfMatrix/PdfPoint; the authoritative glyph/spacing measurement is PdfTextAdvanceMeasurementService. The run's GraphicsState.TextMatrix is local to its current CTM; the display run's TextMatrix already combines placement. B5 emits the local text matrix as cm under the existing CTM, isolated by q/Q. With the repository's row-vector multiplication, the result is **local text matrix.Multiply(current CTM)**. Local translations and newly concatenated cm matrices multiply on the left.

The local start is `(0, effective rise + offset)` and end is `(width, effective rise + offset)`. Font size participates in measurement and default offset/thickness; it is not multiplied into already measured endpoints again. Measurement includes character spacing, word spacing, Tz and TJ adjustments. Automatic width retains signed horizontal advance, including negative Tz. Explicit positive widths remain literal local widths. Automatic zero advance remains unsupported; measurable whitespace remains supported. No vertical-writing feature is added.

Script styling is measured using its effective size/rise before projection. Default underline offset remains -0.12 times effective font size; strikeout remains 0.30 times that size. Default thickness remains max(0.5, 0.05 times absolute effective font size). Explicit thickness remains unchanged. Thickness is a scalar in the same local coordinate system as the decoration path; cm subjects the stroke to the affine transform, including nonuniform scaling and skew. No new device-space or perpendicular-width typography policy is introduced.

BT resets both text matrices. Tm restores the original line matrix. A numeric-only TJ, temporarily using the same font resource at size 1 and Tz 100, restores the current horizontal position without moving the line origin; original size and Tz are then restored. This avoids dividing by zero/tiny source sizes. Existing script/color suffix restoration retains other text state. Td, TD, T*, quotes and later Tj/TJ therefore operate from the expected line and current position. The target run's effective advance is measured once; explicit decoration width also needs that measurement to restore subsequent text correctly.

This follows the text-object/matrix and properly nested marked-content rules in [Adobe PDF Reference 1.6, sections 5.3 and 10.5](https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.6.pdf). Paths execute outside text objects; text matrices are not part of the graphics state saved by q/Q.

## Graphics, tags and supported boundaries

q/Q contains the decoration CTM, stroke color and line width. Existing line cap/join/dash and clip are inherited for painting and preserved afterwards. Decoration stroke uses effective styled fill color, including the existing grayscale/RGB/CMYK representations. The existing update's stroke color still applies to styled text; it does not redefine decoration color policy. No dedicated decoration-color API is added.

For a tag begun outside BT, the local split retains its scope directly. For BMC/BDC begun inside BT, simply inserting ET would create crossing structural scopes. The narrow marked-content helper inserts ET before each affected original opening/closing marker and BT plus matrix restoration after it. Original marker bytes and property dictionaries, including MCID, occur exactly once. Only affected boundaries are changed; no duplicate tagging identifiers or whole-stream canonicalization occurs. A positioning replay over existing canonical operations obtains boundary matrices; it does not reparse arbitrary bytes or guess advances from string length.

Existing graphics clipping and Tr 0/1/2/3 are covered. A text object containing any clipping text run (Tr 4-7) rejects decoration with NotSupportedException before mutation: splitting its accumulated clipping union into successive intersections would change meaning. No existing decoration/text-clipping contract authorizes that change. A pending unfinished graphics path also rejects, since q/Q does not save the current path. Complete enclosing BT/ET provenance in the target physical stream is required; text objects spanning physical content streams reject instead of partially restructuring multiple streams.

**Forms:** Apply already requires unshared PageContent and rejects Form sources, shared or unshared. B5 preserves that boundary and tests both failures with unchanged bytes/object count/history. It does not clone Forms, embed page placement into Form bytes, alter Form matrix composition, or claim successful multi-placement Form styling. Replacement support for Forms does not imply style-service support. POST-P0-009 is not addressed.

## Directly exposed corrections under expanded authorization

| Reproduction and root cause | Narrow correction and exact production location | Preservation/API/allocation impact and evidence |
|---|---|---|
| Rotated Tm (0,1,-1,0,100,200), AB advance 12: following origin was (112,200), expected (100,212). Advance multiplied on the wrong side. | `src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Text.cs`: advance.Multiply(text matrix). | Restores local-axis advance for following text and inserted TJ. No API change or added allocation. Dedicated failing-then-passing regression. |
| Same rotated Tm, Td(0,-20): origin was (100,180), expected (120,200). Line translation multiplied on the wrong side. | `src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.GraphicsState.cs`: translation.Multiply(line matrix). | Correct following-line provenance; no API/allocation change. Dedicated regression and reopen following Td/TD/T*/quotes. |
| Outer cm(2,0,0,3,10,20), local translation(100,200), point(0,-2): interpreted (110,214), expected (210,614). | `src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Execution.cs`: new matrix.Multiply(CTM). | Required for decoration cm under a nonidentity CTM. No API/allocation change; independent numeric oracle. |
| q, AB, Q, CD under rotated Tm rewound CD to (100,200), expected (100,212). Q restored text matrices as graphics state. | Same Execution.cs: retain current text/line matrices while restoring saved graphics state. | Preserves PDF text-object state across decoration q/Q and source q/Q; one existing snapshot restoration remains. Dedicated regression. |
| Finite coordinate 2^63 threw OverflowException because double(long.MaxValue) rounds to 2^63. | `src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleTokenEditor.cs`: exclusive upper bound for integer formatting; otherwise existing PdfReal serializer. | Valid finite coordinates serialize without invalid exponent tokens/incidental integer overflow. No public/API policy change. Boundary regression reparses the point. |
| Automatic subscript width used pre-edit 30 instead of effective 22.5; rise/color also came from pre-style state. | `PdfExistingTextStyleService.cs` and `PdfExistingTextStyleTokenEditor.Decoration.cs` in the same Services/Text directory: measure effective state and use effective rise/fill. | Intentional B5 correction; SDK expectations now assert corrected red stroke and script offsets/width. One transient run snapshot for established measurement; no new resource. |
| Negative Tz produced width +23 when the text advanced -23. | Same service/decoration partial: keep signed automatic advance. | Corrects direction; explicit positive widths retain their meaning. Deterministic negative-Tz regression and reopened control. |

The user made three parenthesis-only edits in PdfGraphicsInterpreter.Text.cs during this batch and explicitly confirmed ownership. Those edits are preserved, separate from the B5 multiplication correction. No other user work was discarded.

## Numeric, exception and resource compatibility

Finite source components do not guarantee finite products. Effective rise, advance, offsets, thickness, transformed endpoints, transformed thickness components and continuation operands are checked before publication. Nonfinite/unrestorable geometry fails with NotSupportedException. No broad catch masks failures. Stale source/BT/ET or incomplete marked provenance fails with InvalidOperationException. Existing invalid API argument checks remain. Unsupported Forms/shared sources keep their established NotSupportedException.

Decode and encode still use PdfContentStreamMutationCodec. B1 decoded-byte/expansion budgets, supported unfiltered/single-Flate policy, filter aliases/array representation, DecodeParms and predictor restrictions remain unchanged. Truncated Flate keeps InvalidDataException at the mutation boundary; expansion rejection keeps PdfFilterDecodeException; cancellation preserves the original token. Resource identities, font names, encodings, stream generation and untouched bytes remain intact. Full stale-placement hardening remains POST-P0-009.

Projection and validation precede ReplaceExistingTextStream. Tests prove rejection creates no partial mutation/history; transaction disposal rolls back an earlier successful decoration if a later projection fails. Filtered/unfiltered undo and redo restore exact previous/current bytes and dictionary state. No resource or Form clone is created.

The implementation adds fixed-size affine work, one target measurement, canonical operation/display-run validation scans and output bytes needed for valid state restoration. Nested tags add a run lookup, one positioning replay and one final whole-stream copy for boundary insertions. Each intervening run is measured once in that replay. There is no new parser pass, full-stream normalization, cache, pooling or retained document snapshot. Scans observe cancellation. Throughput, GC and real-world memory improvements are **measurement required**; none is claimed. No public production API, dependency, project reference, friend assembly, unsafe/native code, pinning or custom memory manager was added.

## Permanent regression evidence

52 new Services test cases pass; the two existing SDK script/style mapping cases have stricter corrected expectations. Existing tests were not removed or weakened.

| Coverage | Result/evidence |
|---|---|
| Identity, translation, uniform/nonuniform scale, 90-degree/arbitrary rotation, skew, combined affine and singular zero basis | Nine deterministic cases under an outer CTM; independently computed numeric endpoint oracles, six-decimal tolerance. Translation gives (100,198) to (112,198). |
| Rise and Tz | Zero/positive/negative rise; 50/100/150/-100 percent Tz with character/word spacing; rise and scaling applied once. |
| Effective script | Super/subscript size, rise, measured width, color and default thickness; no stale pre-style measurement. |
| Tj/TJ/quotes | Widths 12, 13.44 (TJ adjustment -120), 12 and 16; quote baseline movement to y=180 retained. Existing normalizer remains authoritative. |
| Width/decoration policy | Underline and strikeout defaults, explicit 42 at Tz50, whitespace, automatic zero rejection, finite 2^63 formatting. |
| Syntax and graphics | Repository lexer tracks BT/ET and tag nesting; m/l/S/path operations inside text fail. Numeric graphics operations are reinterpreted through the existing interpreter. Stroke/color/clip/cap/join controls pass. |
| Tags | Nested BMC and BDC with MCID 7 remain properly nested and unduplicated. |
| Failure controls | Nonfinite geometry, pending path, clipping text, stale target/boundary, unsupported Forms, canceled token, truncated and high-expansion Flate reject without publication. |
| Transactions | Rollback after a later failure; filtered/unfiltered exact-byte undo/redo; real service save through committed changes. |
| Non-decoration | Exact serialized script/color control unchanged; complete existing Services and SDK font/typography suites pass. No new combined font-change API. |

The small syntax assertion adapter uses the repository lexer and only handles numeric graphics operations needed by these fixtures. It is not a second general PDF parser. Full parser acceptance is described separately below.

## Save, reopen and reinterpret acceptance

**38 combinations passed:** 19 scenarios each with traditional xref and xref streams. Scenarios: translated, rotated, skewed, TJ, single quote, double quote, following text, filtered, marked, nested marked, script, two different decorated runs, underline plus strikeout on one run, typography, existing clip, BDC, marked script, reversed Tz, and dash/cap/join state.

The supplemental acceptance harness at `C:\Users\Sufan\AppData\Local\Temp\PiratePDF-B5-Validation\B5Reopen.cs` references already built repository DLLs. It adds no repository dependency/project reference and is not part of the permanent release-gate test count. Run with:

```powershell
dotnet run --project C:\Users\Sufan\AppData\Local\Temp\PiratePDF-B5-Validation\B5Reopen.csproj --configuration Release --no-restore
```

Each case uses the real style transaction and PdfEditingSaveService, then PdfCrossReferenceParser or PdfCrossReferenceStreamParser plus PdfIndirectObjectResolver, PdfContentStreamParser and PdfGraphicsInterpreter. Reopen verifies content identity 4 2, resource F1/font object 5 and matching Helvetica/MissingWidth 500. The interpreter uses the same known fixture font metrics; this is not a platform font-loading test. Filtered reopen verifies the /Fl array and DecodeParms array/Predictor 1/private parameter 42, then explicitly invokes the existing Flate decoder (the generic registry uses its canonical FlateDecode name).

The oracle is the identical effective style without decoration, supplemented by independent permanent affine endpoint tests. All nonempty text runs are compared for bytes, current/line matrices, font size, Tz, rise, character/word spacing, leading, Tr, fill/stroke, marked scope and clip depth. Decoration endpoints, tag/depth and width are checked; following graphics retain geometry, stroke, line width and dash. Parser-level nesting rejects decoration m/l/S inside BT/ET.

Both decorations are applied through fresh parsed provenance. Reapplying a decoration places the newer strikeout immediately after the target before the older underline, deterministically. Two-run coverage uses different matrices, colors, rise and widths. No raster/screenshot claim is made; deterministic display-list geometry is the rendering evidence. Form acceptance is rejection-only because the style API excludes Forms.

## Validation

| Check | Result |
|---|---|
| Focused Services decoration tests | 52 passed, 0 failed/skipped |
| Focused SDK style mapping | 2 passed |
| Complete BlackFlag.Engine.Services.Tests | 371 passed |
| Complete BlackFlag.Engine.SDK.Adapters.Tests | 34 passed |
| Complete BlackFlag.Engine.Rendering.Tests | 97 passed |
| Complete BlackFlag.Engine.Editing.Tests | 133 passed |
| Complete BlackFlag.Engine.Parsing.Tests | 219 passed |
| Complete BlackFlag.Engine.Writing.Tests | 153 passed |
| Release solution build before gate | Passed, 0 warnings/errors |
| Supplemental save/reopen/reinterpret | 38 passed |
| git diff --check before gate | Passed |
| Full unmodified release gate | Passed: 2,098 tests across 20 projects; 0 failures/skips; build 0 warnings/errors |
| Integrated source-size validation | Passed: 1,052 files; Execution.cs 943 lines, existing sanitizer 844; none at the 1,000-line ceiling |
| DocGen all/health/repair | Passed: 191 documents; 0 errors/warnings/information; health 91.6%, A-; repair 0 changes/files/diagnostics |

Complete suites used `dotnet test tests/<project>/<project>.csproj --configuration Release --no-build`. The acceptance gate is exactly `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1`, without skip flags or edits.

Source-size review: Execution.cs was already above the decomposition-review threshold. Its two small state/matrix corrections are cohesive with existing dispatch and remain below the 1,000-line hard ceiling. Broad interpreter decomposition is expressly excluded. New helpers separate decoration emission, enclosing-context validation and marked-boundary positioning; no unrelated extraction was performed.

## Exact change inventory

Eight production files:

- `src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Execution.cs`
- `src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.GraphicsState.cs`
- `src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Text.cs` (also contains the three confirmed user edits)
- `src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleService.cs`
- `src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleTokenEditor.cs`
- `src/BlackFlag.Engine.Services/Text/PdfExistingTextStyleTokenEditor.Decoration.cs` (new)
- `src/BlackFlag.Engine.Services/Text/PdfExistingTextDecorationContext.cs` (new)
- `src/BlackFlag.Engine.Services/Text/PdfExistingTextDecorationMarkedContent.cs` (new)

Six test files:

- `tests/BlackFlag.Engine.SDK.Adapters.Tests/Editing/PdfExistingTextSdkSessionTests.cs` (updated)
- `tests/BlackFlag.Engine.Services.Tests/Text/PdfDecorationTestContext.cs` (new)
- `tests/BlackFlag.Engine.Services.Tests/Text/PdfDecorationSyntaxAssertions.cs` (new)
- `tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextDecorationGeometryTests.cs` (new)
- `tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextDecorationGeometryTests.Transforms.cs` (new)
- `tests/BlackFlag.Engine.Services.Tests/Text/PdfExistingTextDecorationPreservationTests.cs` (new)

Five documentation files:

- `docs/reviews/Post-BF-1054-Batch-B5-Decoration-Geometry-and-Operator-Placement-Completion-Report.md` (new)
- `docs/reviews/Post-BF-1054-CSharp-Repository-Audit.md`
- `docs/Documentation-Index.md` (generated)
- `docs/Documentation-Health.md` (generated)
- `docs/ValidationReport.md` (generated)

**Final changed-file count: 19**, comprising eight production, six test and five documentation files. The repair report has no content diff. Every changed file was reviewed; final git diff --check passes. Only acceptance documentation was updated after the passing gate, followed by another clean DocGen generation/health/repair validation.

## Remaining findings and stop boundary

POST-P0-008, POST-P0-009 and POST-P2-001 were not started. POST-P2-002/003, canonical font editing, interpreter decomposition and other roadmap work remain untouched. POST-P0-005/006/015 and the ownership batches remain closed. The release gate and dependency/project files are unchanged. No commit, push, merge, tag or branch switch occurred. This batch stops after final acceptance; any subsequent batch requires explicit user approval.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-15 | Record B5 geometry/operator corrections, direct state defects and preservation validation. |
