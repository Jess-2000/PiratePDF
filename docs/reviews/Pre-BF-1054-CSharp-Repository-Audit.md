# Pre-BF-1054 C# Repository Engineering Audit

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-001 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053, BF-1054 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## 1. Executive summary

This Phase A audit reviewed every tracked, hand-authored C# file before BF-1054. The review covered 881 hand-authored files (95,223 physical lines) and separately classified 8 generated `AssemblyInfo.cs` files. No vendored or external C# source was found. No production or test C# file was changed.

The repository baseline is healthy: the authoritative Release gate passed with zero compiler/analyzer warnings, all 1,474 tests passed, documentation validation reported no errors or warnings, documentation repair preview proposed no changes, and whitespace validation passed.

The audit found 3 P0 issues, 2 P1 optimization candidates requiring measurement, and 3 P2 issues. The P0 findings are concentrated at security-sensitive public collection and plugin package boundaries. They do not require public signature changes. No PDF serialization, xref, object-identity, page-tree, stream-byte, transaction, rollback, or preservation defect was established by this audit.

Of the 881 hand-authored files, 855 had no material finding. “No material finding” means the file was inventoried and reviewed against its subsystem contracts and did not require a P0/P1/P2 change or a separately tracked optimization candidate. P3-only observations are included in this count because they do not justify churn.

## 2. Repository and baseline health

Audit checkpoint:

- Branch: `maintenance/pre-BF-1054-repository-optimization`.
- HEAD: `af3c924` (`Add Codex repository engineering instructions`).
- `develop`: `5c0c15c`, the BF-1053 merge commit.
- BF-1053 completion tag: `0210aa7` (`BF-1053-complete`).
- Working tree before the audit: clean.
- BF-1054 implementation: not present and not included in this audit.

Baseline gate result:

| Gate step | Result | Evidence |
|---|---|---|
| Source-file size | PASS | 883 C#/PowerShell files inspected by the repository script; one 937-line review candidate; no file over 1,000 lines. |
| Restore | PASS | All projects up to date. |
| Release build/analyzers | PASS | 0 warnings, 0 errors. |
| Tests | PASS | 1,474 passed, 0 failed, 0 skipped across 18 test assemblies. |
| Documentation generation/validation | PASS | 145 documents; 0 errors, 0 warnings. |
| Documentation health | PASS | 91.7%, grade A-. |
| Repair preview | PASS | 0 changes across 0 files. |
| `git diff --check` | PASS | No whitespace errors. |

The first sandboxed gate attempt stopped at restore because the sandbox could not read `%AppData%\NuGet\NuGet.Config`. The same unmodified gate then passed outside that restricted filesystem boundary. This was an audit-environment access failure, not a repository failure.

## 3. C# inventory and coverage

Classification rules followed `Test-PiratePDFSourceFileSizes.ps1`: `.g.cs`, `.generated.cs`, `.designer.cs`, and `AssemblyInfo.cs` are generated. All eight generated files are `Properties/AssemblyInfo.cs`. No other generated markers, vendored directories, or external-source headers were found.

Tests are hand-authored source and were reviewed, but are classified separately from production. `tests/BlackFlag.Engine.Plugins.TestPlugin/Plugin.cs` is an executable test fixture. `samples/BlackFlag.Engine.ReferencePlugin/Plugin.cs` is a reference sample and contract fixture. Neither is production engine code.

| Project/subsystem | Hand-authored reviewed | Generated | Material-finding files | No-change reviewed files |
|---|---:|---:|---:|---:|
| samples/BlackFlag.Engine.ReferencePlugin | 1 | 0 | 0 | 1 |
| src/BlackFlag.Engine.Abstractions | 1 | 0 | 0 | 1 |
| src/BlackFlag.Engine.Core | 10 | 0 | 0 | 10 |
| src/BlackFlag.Engine.Document | 56 | 1 | 1 | 55 |
| src/BlackFlag.Engine.Editing | 61 | 1 | 0 | 61 |
| src/BlackFlag.Engine.Filters | 24 | 1 | 0 | 24 |
| src/BlackFlag.Engine.IO | 6 | 0 | 0 | 6 |
| src/BlackFlag.Engine.Objects | 16 | 0 | 0 | 16 |
| src/BlackFlag.Engine.Parsing | 85 | 1 | 3 | 82 |
| src/BlackFlag.Engine.Plugins | 27 | 0 | 5 | 22 |
| src/BlackFlag.Engine.Rendering | 113 | 1 | 1 | 112 |
| src/BlackFlag.Engine.Rendering.Skia | 8 | 0 | 0 | 8 |
| src/BlackFlag.Engine.SDK | 37 | 0 | 2 | 35 |
| src/BlackFlag.Engine.SDK.Adapters | 2 | 0 | 0 | 2 |
| src/BlackFlag.Engine.Security | 63 | 0 | 12 | 51 |
| src/BlackFlag.Engine.Services | 22 | 0 | 0 | 22 |
| src/BlackFlag.Engine.Syntax | 21 | 1 | 0 | 21 |
| src/BlackFlag.Engine.Writing | 47 | 1 | 1 | 46 |
| tests/BlackFlag.Engine.Abstractions.Tests | 1 | 0 | 1 | 0 |
| tests/BlackFlag.Engine.Core.Tests | 2 | 0 | 0 | 2 |
| tests/BlackFlag.Engine.Document.Tests | 12 | 0 | 0 | 12 |
| tests/BlackFlag.Engine.Editing.Tests | 23 | 0 | 0 | 23 |
| tests/BlackFlag.Engine.Filters.Tests | 19 | 0 | 0 | 19 |
| tests/BlackFlag.Engine.IO.Tests | 3 | 0 | 0 | 3 |
| tests/BlackFlag.Engine.Objects.Tests | 14 | 0 | 0 | 14 |
| tests/BlackFlag.Engine.Parsing.Tests | 17 | 0 | 0 | 17 |
| tests/BlackFlag.Engine.Plugins.TestPlugin | 1 | 0 | 0 | 1 |
| tests/BlackFlag.Engine.Plugins.Tests | 7 | 0 | 0 | 7 |
| tests/BlackFlag.Engine.Rendering.Skia.Tests | 2 | 0 | 0 | 2 |
| tests/BlackFlag.Engine.Rendering.Tests | 16 | 0 | 0 | 16 |
| tests/BlackFlag.Engine.SDK.Adapters.Tests | 4 | 0 | 0 | 4 |
| tests/BlackFlag.Engine.SDK.Tests | 8 | 0 | 0 | 8 |
| tests/BlackFlag.Engine.Security.Tests | 18 | 0 | 0 | 18 |
| tests/BlackFlag.Engine.Services.Tests | 18 | 0 | 0 | 18 |
| tests/BlackFlag.Engine.Syntax.Tests | 16 | 0 | 0 | 16 |
| tests/BlackFlag.Engine.Writing.Tests | 25 | 0 | 0 | 25 |
| tests/PiratePDF.DocGen.Tests | 16 | 0 | 0 | 16 |
| tools/PiratePDF.DocGen | 59 | 1 | 0 | 59 |
| **Total** | **881** | **8** | **26** | **855** |

Coverage was tracked by enumerating every Git-tracked `.cs` file, reading the complete hand-authored set, correlating source with adjacent tests and applicable architecture/milestone contracts, and assigning each file to generated/fixture, material-finding, or no-material-finding disposition. The 26 material-finding files are the union of all paths cited by the findings below; overlapping findings count a file once.

## 4. Findings summary

| Priority | Count | Summary |
|---|---:|---|
| P0 | 3 | Plugin path containment and cast-mutable security/plugin collection boundaries. |
| P1 | 2 | Measurement-required memory and repeated-ordering optimization candidates. |
| P2 | 3 | Oversized cohesive methods/types and a placeholder-only test project. |
| P3 | Aggregate only | Formatting, compact one-line members, repeated test `GlobalUsings.cs`, and preference-only control-flow alternatives. |

Terminology:

- A **candidate optimization** is potentially useful but requires measurement, stronger evidence, or more preservation/risk analysis. It is not approved for implementation.
- A **recommended optimization** is sufficiently evidenced, worthwhile, and low-risk to be a realistic Phase B batch.

This audit identifies two candidate performance optimizations (`AUD-P1-001`, `AUD-P1-002`). It does not recommend either until its measurement gate is met. The P0 hardening fixes and P2 decomposition/test changes are recommended engineering changes, but they are not represented as measured runtime performance gains.

## 5. Detailed findings

### AUD-P0-001 — Platform-incorrect plugin package containment

| Field | Detail |
|---|---|
| Priority | P0 — security/platform correctness |
| Project/subsystem | BlackFlag.Engine.Plugins / managed loading |
| Location | `src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeHost.cs`, `TryActivateAsync`; `src/BlackFlag.Engine.Plugins/Runtime/PluginLoadContext.cs`, `ValidateResolvedPath` |
| Finding | Package containment is checked with `full.StartsWith(packageRoot, StringComparison.OrdinalIgnoreCase)` on every platform. On a case-sensitive filesystem, a sibling whose path differs only by case can satisfy this test even though it is outside the configured package directory. |
| Evidence | Both entry-assembly and managed/unmanaged dependency paths use the unconditional case-insensitive prefix check. The architecture is cross-platform and BF-1050D requires resolved paths to remain inside the package. Existing runtime tests do not exercise case-sensitive sibling paths. |
| Why it matters | It weakens the declared dependency-resolution boundary and can load code outside the selected package on case-sensitive platforms. |
| Change risk | Medium. Path rules are security-sensitive and must preserve valid Windows/macOS packages and native dependency resolution. |
| Smallest recommended fix | Centralize a package-containment predicate using `Path.GetRelativePath`; reject rooted results, `..`, and paths beginning with `..` plus a separator. Use it in both host entry resolution and load-context dependency resolution. Do not broaden filesystem access or claim symlink isolation. |
| Required validation | Windows tests for valid child/similar-prefix sibling/traversal; case-sensitive-platform tests for case-different siblings; managed and unmanaged resolution tests; plugin project tests; full release gate. |
| Batch | Batch 1 with `AUD-P0-002`; keep `AUD-P0-003` separate by security-result subsystem. |

### AUD-P0-002 — Cast-mutable plugin contracts and lifecycle state

| Field | Detail |
|---|---|
| Priority | P0 — security boundary and lifecycle integrity |
| Project/subsystem | BlackFlag.Engine.SDK / BlackFlag.Engine.Plugins |
| Location | `src/BlackFlag.Engine.SDK/Plugins/PluginManifest.cs`; `src/BlackFlag.Engine.Plugins/Discovery/PluginCatalog.cs`; `src/BlackFlag.Engine.Plugins/Activation/PluginActivationPlan.cs`; `src/BlackFlag.Engine.Plugins/Runtime/PluginRuntimeSession.cs` |
| Finding | Public properties are typed as `IReadOnlyList<T>` but return arrays or mutable `List<T>` instances directly. Callers can cast to the concrete backing type and replace descriptors/capabilities or remove active handles. |
| Evidence | The manifest, catalog, and plan claim validated/immutable results yet expose array instances. `PluginRuntimeSession.ActivePlugins` exposes the exact list used by reverse-order deactivation, extension removal, and unload; clearing it before `DisposeAsync` skips that cleanup. BF-1051J already demonstrates the repository's intended defense: `Array.AsReadOnly` prevents mutation through a publicly castable backing array. |
| Why it matters | Mutation can invalidate dependency/capability decisions after validation and can bypass supported lifecycle cleanup. |
| Change risk | Low to medium. Public signatures remain unchanged; concrete runtime types observed only through unsupported casts will change. |
| Smallest recommended fix | Store/read through `ReadOnlyCollection<T>` wrappers. Keep private mutable lists where lifecycle mutation is required, but expose a stable `AsReadOnly()` view. Do not clone on every property access. |
| Required validation | Regression tests attempt array/list casts and mutation; verify plan/catalog/manifest stability, reverse cleanup, extension removal, and idempotent disposal; SDK/plugins tests; full release gate. |
| Batch | Batch 1 with `AUD-P0-001`. |

### AUD-P0-003 — Cast-mutable security observations and findings

| Field | Detail |
|---|---|
| Priority | P0 — security decision/report integrity |
| Project/subsystem | Security, Parsing security, SDK security |
| Location | `DocumentSecurityAssessment`, `DocumentSecurityReport`, `DocumentSecurityRemediationPlan`, `DocumentContainerSecurityInspection`, `DocumentEmbeddedInspectionResult`, the Markdown/RTF/OLE/OOXML/ODT analysis-result types, `PdfSecurityAnalysisResult`, and `DocumentSecuritySdkResult` |
| Finding | These types copy inputs into arrays but expose those arrays directly as `IReadOnlyList<T>`. A consumer can cast the property back to its array and replace elements. |
| Evidence | `DocumentSecurityAssessment.IsBlocked` is recomputed from the exposed backing `findings` array, so replacing a blocking finding changes the policy result after construction. Other affected objects can have observations, remediation actions, embedded children, reports, or SDK-visible results altered after validation. Existing tests do not attempt backing-array casts. |
| Why it matters | Security analysis and reporting objects are expected to be stable evidence. Publicly mutable backing arrays permit post-validation tampering and inconsistent downstream decisions. |
| Change risk | Low. Wrap existing construction snapshots once; public interfaces and element order remain unchanged. |
| Smallest recommended fix | Replace publicly exposed array backing with `ReadOnlyCollection<T>` (or retain arrays privately and expose cached `Array.AsReadOnly` views). Apply consistently to security-result boundaries, then audit remaining value/result DTOs under the same rule as a bounded follow-up. |
| Required validation | Mutation-attempt regression tests at assessment, report, container, parser-result, remediation, and SDK boundaries; verify `IsBlocked` and report output remain stable; affected tests; full release gate. |
| Batch | Batch 2, after plugin boundary hardening. |

### AUD-P1-001 — Whole-source recovery allocation

| Field | Detail |
|---|---|
| Priority | P1 candidate optimization — measurement required |
| Project/subsystem | BlackFlag.Engine.Parsing / damaged-xref recovery |
| Location | `src/BlackFlag.Engine.Parsing/Recovery/PdfCrossReferenceRecoveryService.cs`, `ReadSource` and scan methods |
| Finding | Recovery allocates one contiguous byte array for the complete source, with a default allowed size of 512 MiB, then performs multiple scans over it. |
| Evidence | `Recover` permits `MaximumRecoveryScanBytes` up to the configured default; `ReadSource` allocates `new byte[(int)source.Length]`; object and trailer discovery then traverse the materialized bytes. This is bounded and matches the current milestone contract, but has clear large-object and peak-memory cost. |
| Why it matters | Damaged/untrusted large files can create substantial contiguous allocation pressure in a recovery path. |
| Change risk | High. Chunking must preserve token boundaries, stream extents, newest-wins candidates, trailer inference, cancellation, encryption classification, and deterministic offsets. |
| Smallest candidate approach | First benchmark representative 64/128/256/512 MiB damaged inputs and record peak working set, allocation, and time. Only if justified, design a bounded windowed scanner with explicit overlap/state rather than replacing parser validation. |
| Required validation | Benchmark/measurement harness; adversarial cross-boundary tokens/streams; recovery corpus; parsing/security suites; full release gate. |
| Batch | Not eligible for Phase B until measurement and design review are approved. |

### AUD-P1-002 — Repeated sorting of object identifiers

| Field | Detail |
|---|---|
| Priority | P1 candidate optimization — measurement required |
| Project/subsystem | BlackFlag.Engine.Document / Writing |
| Location | `src/BlackFlag.Engine.Document/Objects/PdfObjectStore.cs`, `Identifiers`; `src/BlackFlag.Engine.Writing/PdfWriteObjectAllocator.cs` and Writing call sites |
| Finding | Every enumeration of `PdfObjectStore.Identifiers` performs `OrderBy(...).ThenBy(...)`. Writing enumerates this property during planning, allocation, projection validation, full-rewrite planning, incremental serialization, and trailer-size calculation. |
| Evidence | The property constructs a new ordered iterator each time; at least six Writing paths consume it. Several consumers need deterministic order, while allocator membership and maximum-object calculations do not. |
| Why it matters | For large indirect-object stores, repeated O(n log n) ordering and temporary allocations may be avoidable. No benchmark currently establishes user-visible impact. |
| Change risk | Medium. Ordering is preservation-sensitive and the plan/document mutation boundary is subtle; using a stale identifier snapshot could create allocation collisions after projections. |
| Smallest candidate approach | Benchmark large stores and trace enumerations per save. If material, reuse a proven-current plan snapshot where semantics permit or add an internal non-ordering traversal for membership/max operations while retaining the public deterministic property. |
| Required validation | Object-count benchmarks, allocation traces, plan/projection mutation tests, full/incremental serialization and reopen tests, Writing tests, full release gate. |
| Batch | Not eligible until measurement confirms significance and snapshot semantics are specified. |

### AUD-P2-001 — 926-line graphics interpretation method

| Field | Detail |
|---|---|
| Priority | P2 — maintainability/testability |
| Project/subsystem | BlackFlag.Engine.Rendering / graphics interpretation |
| Location | `src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Execution.cs`, `InterpretCore` |
| Finding | The 937-line file contains a single approximately 926-line method combining operator dispatch, graphics/text state mutation, path painting, marked content, clipping, XObjects, recursion, and local helper implementations. |
| Evidence | It is the only file above the 800-line review threshold and is close to the 1,000-line hard limit. The single method prevents responsibility-based partial-file decomposition even though the owning type is already partial. |
| Why it matters | Future rendering work has a high merge/conflict and regression-review surface; local functions capture broad mutable state, making isolated characterization difficult. |
| Change risk | Medium to high because operator order and graphics-state semantics are preservation-sensitive for rendering. |
| Smallest recommended fix | Add characterization tests first, then extract cohesive operator families/state helpers while retaining the `PdfGraphicsInterpreter` facade and exact dispatch/order. Avoid arbitrary numbered partials. |
| Required validation | Operator-family tests, nested Form/clip/text-state tests, Rendering and Skia tests, image/render comparison where deterministic, full release gate. |
| Batch | Batch 4 by itself. |

### AUD-P2-002 — Large security/parser types need responsibility-based decomposition

| Field | Detail |
|---|---|
| Priority | P2 — maintainability and reviewability |
| Project/subsystem | Security container inspection and Parsing security |
| Location | `OoxmlWordContainerSecurityAdapter.cs` (796 lines), `OleCompoundFileSecurityAdapter.cs` (732 lines), `PdfSecurityAnalyzer.AnalysisState.cs` (763 lines) |
| Finding | Each file is in the mandatory 600–800 decomposition-review band. OOXML combines archive traversal, XML/relationship/content-type/field inspection, path normalization, and entry reading. OLE combines policy inspection with a 425-line compound-file reader. PDF analysis state combines traversal with all action/annotation/file classification. |
| Evidence | Method/responsibility inventory shows separable parsing/transport and classification responsibilities; the OLE nested reader alone occupies most of its file. |
| Why it matters | Security review becomes harder when low-level bounded parsing and policy classification change in the same large unit. |
| Change risk | Medium. Limits, diagnostic order, logical paths, and malformed-input failure behavior must remain exact. |
| Smallest recommended fix | Split the OLE reader into an internal bounded-reader type; split OOXML helpers by archive/XML classification responsibility; split PDF analysis-state classification into cohesive partial files only if the state object remains the correct owner. Preserve all public APIs and behavior. |
| Required validation | Characterization of observation order/messages and hostile limits; affected Security/Parsing suites; full release gate. |
| Batch | Batch 5, one subsystem/file family at a time. |

### AUD-P2-003 — Abstractions tests contain only a tautological smoke test

| Field | Detail |
|---|---|
| Priority | P2 — testability/reliability |
| Project/subsystem | BlackFlag.Engine.Abstractions.Tests |
| Location | `tests/BlackFlag.Engine.Abstractions.Tests/SmokeTests.cs`, `TestRunnerDiscoversTests` |
| Finding | The project's only test is `Assert.True(true)`, so it validates test discovery but no `IRandomAccessSource` contract or architecture rule. |
| Evidence | The project contains one hand-authored C# file and the release gate reports one passing test. |
| Why it matters | The test creates a green signal without protecting the abstraction's sync/async, ownership, or dependency boundary. |
| Change risk | Low. Production behavior is untouched. |
| Smallest recommended fix | Replace the tautology with an architecture/contract test that verifies the assembly exposes the intended interface and has only approved project references; add shared behavioral tests only when a concrete implementation-neutral contract can be asserted. |
| Required validation | Abstractions test project and full release gate. |
| Batch | Batch 3 with other test-only hardening, if approved. |

## 6. File-size and decomposition review

| Band | Files | Decision |
|---|---|---|
| 1,000+ | None | No violation. |
| 800–999 | `PdfGraphicsInterpreter.Execution.cs` — 937 | Recommended decomposition; `AUD-P2-001`. |
| 600–799 production | `OoxmlWordContainerSecurityAdapter.cs` — 796; `PdfSecurityAnalyzer.AnalysisState.cs` — 763; `OleCompoundFileSecurityAdapter.cs` — 732; `PdfFunctionEvaluator.cs` — 707 | First three are recommended for responsibility-based decomposition under `AUD-P2-002`. `PdfFunctionEvaluator` is large but already separated into calculator parser/machine/token partials and its remaining type-specific dispatch is cohesive; no change recommended now. |
| 600–799 tests | `PdfCrossReferenceParserTests.cs` — 764; `PdfCrossReferenceStreamParserTests.cs` — 755; `PdfObjectParserTests.cs` — 661 | Large but cohesive specification-style test suites. Splitting would add navigation churn without changing production risk; no change recommended. |

## 7. Performance candidates requiring measurement

### Candidate optimizations

- `AUD-P1-001`: whole-source recovery allocation and repeated scans.
- `AUD-P1-002`: repeated identifier ordering in Writing.
- `PdfEditableFormField.WidgetIds` materializes on each access, but only one test use was found and no hot-path evidence exists. Retain as an unclassified measurement note, not a finding.

### Recommended optimizations

No runtime performance optimization is recommended without measurement. The recommended Phase B work consists of security/correctness hardening and maintainability/test improvements. `AUD-P2-001` and `AUD-P2-002` are recommended engineering optimizations in the broad sense of reducing review complexity, but no speed or allocation claim is made for them.

## 8. Test gaps and characterization requirements

- Add adversarial package-containment tests for case-sensitive and similar-prefix paths (`AUD-P0-001`).
- Add backing-array/list mutation tests across plugin and security result boundaries (`AUD-P0-002`, `AUD-P0-003`).
- Characterize graphics operator ordering, captured state, clipping, marked content, and nested Forms before decomposition (`AUD-P2-001`).
- Characterize observation order, diagnostics, logical paths, limits, and malformed-container failure semantics before security parser decomposition (`AUD-P2-002`).
- Replace the Abstractions tautology with a meaningful contract/architecture test (`AUD-P2-003`).
- If either P1 candidate advances, add benchmarks before production changes and keep preservation/reopen tests as acceptance gates.

Existing preservation coverage is strong at the public save boundary, including full rewrite, incremental update, xref tables/streams, nested page-tree mutation, annotation persistence, original-byte prefix preservation, and untargeted-object retention. No test was weakened or removed.

## 9. Security and preservation review

The security review found the package-path and cast-mutable result boundaries described by the P0 findings. Bounded filter decoding, container recursion/expansion controls, parser depth/cycle controls, passive-document rules, capability authorization, transactional extension registration, and exception containment were otherwise consistently represented in reviewed code and tests.

The PDF preservation review traced object identity/generation, raw stream ownership, xref and incremental revision paths, `/Prev` and trailer construction, page-tree projection, annotations, metadata, transaction rollback, undo/redo, and public save orchestration. No evidence justified changing these semantics. The two Writing performance candidates must not substitute stale state for the current projection namespace or reorder serialized identifiers without explicit proof.

No secret logging, automatic document-supplied execution, network access, unsafe code, unbounded cache, dependency-direction violation, or platform-specific dependency in BlackFlag Engine was found.

## 10. Recommended implementation batches

1. **Plugin boundary hardening:** `AUD-P0-001` and `AUD-P0-002`; add adversarial tests first, centralize containment, wrap public collections, run Plugins/SDK tests and the full gate.
2. **Security result immutability:** `AUD-P0-003`; apply cached read-only views consistently, add mutation tests, run Security/Parsing/SDK/Services tests and the full gate.
3. **Abstractions test correction:** `AUD-P2-003`; replace the tautology with meaningful architecture/contract coverage.
4. **Graphics interpreter decomposition:** `AUD-P2-001`; characterization first, one behavior-preserving rendering batch.
5. **Security parser decomposition:** `AUD-P2-002`; split one file family per batch after characterization.
6. **Measured candidates only:** consider `AUD-P1-001` or `AUD-P1-002` only after separate benchmark evidence and explicit approval.

## 11. Findings rejected as unjustified churn

- Mechanical conversion of `foreach`, LINQ, guard clauses, or switch forms where current control flow is clear.
- Bulk expansion of compact one-line plugin members solely for style.
- Consolidation of identical test `GlobalUsings.cs` files; SDK-style project conventions already make them cheap and obvious.
- Replacement of bounded pooled/filter code with additional `Span<T>`, unsafe code, or new pooling layers without measurement.
- Consolidating format-security adapters into a new framework; their format-specific limits and parsing behavior are clearer when independent.
- Recombining the legacy `BlackFlag.Engine.Core.Objects` and current document/object model during this maintenance audit; no safe behavioral migration was established.
- Splitting large parser tests merely to reduce line counts.

## 12. Deferred BF-1054 and later-roadmap items

No existing-text-editing, typography, font replacement, glyph mapping/edit persistence, text-layout UI, or related BF-1054 feature was implemented or proposed as maintenance. Encrypted PDF Writing, detached-object/free-xref policy, page-tree rebalancing, mixed page/annotation save domains, annotation appearance generation, Widget/AcroForm coordination, and application save UX remain later roadmap concerns under the BF-1053 closure record.

## 13. Validation commands used

```powershell
git status --short --branch
git branch --show-current
git log -5 --oneline --decorate
git diff --stat

# Initial sandboxed attempt; source-size check passed, restore was blocked by
# denied access to the user NuGet.Config.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1

# Same unmodified command with access to the user NuGet configuration; PASS.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1

git ls-files '*.cs'
rg --files -g '*.cs' -g '!**/bin/**' -g '!**/obj/**'
rg -n -g '*.cs' '<targeted correctness, security, lifetime, allocation, and test patterns>' src tests tools samples
git diff --check
git status --short
```

The repository source-size script was also inspected directly to ensure the inventory used the same generated-file classification and line-limit rules.

## 14. Final coverage statement

Every one of the 881 Git-tracked, hand-authored C# files in `src`, `tests`, `tools`, and `samples` was inventoried and reviewed. All 8 generated C# files were separately classified and excluded from hand refactoring. No vendored/external C# source was present. The review resulted in 26 unique material-finding files and 855 no-change reviewed files.

Phase A made no C# implementation changes. Phase B must not begin until a specific batch is explicitly approved.

## Revision history

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Completed the pre-BF-1054 full C# repository engineering audit. |
