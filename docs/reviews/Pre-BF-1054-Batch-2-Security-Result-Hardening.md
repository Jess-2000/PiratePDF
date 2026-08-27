# Pre-BF-1054 Batch 2 — Security Result Hardening

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-004 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049, BF-1054 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## Executive summary

Batch 2 addresses only `AUD-P0-003 — security-result collection immutability` from the pre-BF-1054 C# repository audit. Twelve security-result types copy constructor inputs into arrays but return those arrays through `IReadOnlyList<T>` properties. A caller can cast a returned value back to its runtime array type and replace elements, changing a security assessment, report, remediation plan, container result, format-analysis result, PDF analysis result, or SDK snapshot after construction.

The smallest coherent correction is to retain the current constructor snapshots and public `IReadOnlyList<T>` signatures while exposing one cached `ReadOnlyCollection<T>` per collection. This closes the concrete-cast mutation path without changing policy, analysis, ordering, reporting, remediation, parsing, SDK mapping, or public API signatures. No production or test implementation is authorized by this planning document.

## Verified baseline and sources of truth

Before this plan was written:

- Batch 1 was confirmed committed at `37f5830` (`Harden plugin package and collection boundaries`).
- The branch remained `maintenance/pre-BF-1054-repository-optimization`.
- `git status --short` was empty.

The plan is governed by `AGENTS.md`, the Phase A audit, the project charter, the BlackFlag Engine architecture and SDK specifications, the BF-1049A through BF-1049F, BF-1049H, and BF-1049I milestone contracts, the current implementations listed below, and their existing Security, Parsing, Services, SDK, and SDK-adapter tests.

The governing invariants are that document content remains untrusted and passive, format analyzers emit normalized observations, shared Security owns policy and reporting, remediation is deterministic, reports are explanatory rather than authorization tokens, and SDK results are immutable snapshots that do not expose parser implementation types.

## Exact affected production surface

The implementation is limited to these 12 production files, types, and 16 existing properties:

| Project | File and type | Properties |
|---|---|---|
| BlackFlag.Engine.Security | `src/BlackFlag.Engine.Security/Inspection/DocumentSecurityAssessment.cs` — `DocumentSecurityAssessment` | `Findings`, `Interactions` |
| BlackFlag.Engine.Security | `src/BlackFlag.Engine.Security/Reporting/DocumentSecurityReport.cs` — `DocumentSecurityReport` | `Findings`, `Interactions` |
| BlackFlag.Engine.Security | `src/BlackFlag.Engine.Security/Remediation/DocumentSecurityRemediationPlan.cs` — `DocumentSecurityRemediationPlan` | `Actions` |
| BlackFlag.Engine.Security | `src/BlackFlag.Engine.Security/Containers/DocumentContainerSecurityInspection.cs` — `DocumentContainerSecurityInspection` | `Observations`, `EmbeddedObjects` |
| BlackFlag.Engine.Security | `src/BlackFlag.Engine.Security/Containers/DocumentEmbeddedInspectionResult.cs` — `DocumentEmbeddedInspectionResult` | `Observations` |
| BlackFlag.Engine.Security | `src/BlackFlag.Engine.Security/Formats/Markdown/MarkdownSecurityAnalysisResult.cs` — `MarkdownSecurityAnalysisResult` | `Observations` |
| BlackFlag.Engine.Security | `src/BlackFlag.Engine.Security/Formats/Rtf/RtfSecurityAnalysisResult.cs` — `RtfSecurityAnalysisResult` | `Observations` |
| BlackFlag.Engine.Security | `src/BlackFlag.Engine.Security/Formats/Ole/LegacyWordBinarySecurityAnalysisResult.cs` — `LegacyWordBinarySecurityAnalysisResult` | `Observations` |
| BlackFlag.Engine.Security | `src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordSecurityAnalysisResult.cs` — `OoxmlWordSecurityAnalysisResult` | `Observations` |
| BlackFlag.Engine.Security | `src/BlackFlag.Engine.Security/Formats/OpenDocument/OpenDocumentTextSecurityAnalysisResult.cs` — `OpenDocumentTextSecurityAnalysisResult` | `Observations` |
| BlackFlag.Engine.Parsing | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalysisResult.cs` — `PdfSecurityAnalysisResult` | `Observations` |
| BlackFlag.Engine.SDK | `src/BlackFlag.Engine.SDK/Security/DocumentSecuritySdkResult.cs` — `DocumentSecuritySdkResult` | `Findings`, `Interactions` |

No other result, request, policy, parser, service, report-writer, or adapter type is included merely because it also contains a collection.

## Current mutability mechanism

Each affected constructor currently materializes its enumerable input with `ToArray()` or uses an empty array. Each public property is typed as `IReadOnlyList<T>` but returns that private array instance directly. `IReadOnlyList<T>` prevents mutation through its declared interface only; it does not make the runtime array immutable. A caller can cast the property value to `T[]` or `IList<T>` and replace an element.

This is security-significant rather than stylistic:

- `DocumentSecurityAssessment.IsBlocked` and `ShouldGenerateReport` are evaluated from the exposed findings array, so replacement can change a completed policy outcome.
- Report findings and interactions can be changed after the report is created.
- Remediation actions, container children, and normalized observations can be substituted after validation or analysis.
- The SDK contract explicitly promises immutable result snapshots, but its findings and interaction-name arrays are cast-mutable.

The constructors already prevent mutation through the caller's original input collection. The defect is the exposed concrete backing object, not failure to snapshot the input.

## Intended implementation

For every affected collection:

1. Keep the existing constructor parameter and public property signatures unchanged.
2. Preserve the existing single materialization of the input, including existing null handling, filtering, de-duplication, and order.
3. Wrap the resulting array once with `Array.AsReadOnly(...)` and store the resulting `ReadOnlyCollection<T>` in a private readonly field.
4. Return that cached field through the existing `IReadOnlyList<T>` property.
5. Do not allocate or copy on property access.
6. Where internal code currently uses array-only members such as `Length`, use the equivalent collection `Count` without changing the predicate or outcome.

Illustrative shape only:

```csharp
private readonly ReadOnlyCollection<T> items;

// Existing transformation and ordering remain authoritative.
items = Array.AsReadOnly(source.ToArray());

public IReadOnlyList<T> Items => items;
```

The implementation should add only the required `System.Collections.ObjectModel` imports. It must not introduce a shared helper, new abstraction, new package, per-access copy, custom collection, or deep-cloning system.

## Public API compatibility requirements

- All public types, constructors, parameter types, property names, and property return types remain unchanged.
- Every affected collection remains exposed as `IReadOnlyList<T>`.
- Enumeration order, indexing, count, and element identity remain unchanged.
- Repeated property access returns the same cached read-only view rather than a fresh allocation.
- Constructor input remains snapshotted exactly once; later mutation of caller-owned source collections remains unable to affect results.
- The concrete runtime backing type changes intentionally from an array to `ReadOnlyCollection<T>`. Casting an `IReadOnlyList<T>` to its undocumented array implementation is not a supported public contract.
- Existing immutable/value-like element contracts remain as they are. Batch 2 protects collection membership and ordering; it does not redesign or deep-clone element types.

## Security and behavioral semantics that must remain unchanged

- `DocumentSecurityAssessment` must preserve policy-selected findings and interactions, removal of `DocumentSecurityInteraction.None`, first-occurrence ordering under `Distinct()`, `ManualInspection`, `ShouldGenerateReport`, and `IsBlocked` behavior.
- Automatic clean inspections remain silent; manual inspections still produce a report even when clean.
- `DocumentSecurityReport` must preserve findings, filtered/de-duplicated interactions, manual-inspection state, and `GeneratedAtUtc` exactly.
- Reports remain ephemeral explanatory output and never become authorization tokens or automatic persistence.
- `DocumentSecurityRemediationPlan` must preserve ordered actions, `IsBlocked`, and `RequiresMutation`; no remediation category or intent mapping changes.
- Container and embedded-inspection results must preserve observation and child order, empty-result behavior, budget accounting, recursive traversal contracts, and cancellation behavior.
- PDF, OOXML, ODT, OLE/DOC, RTF, and Markdown analyzers must continue to emit the same normalized observations and must never execute, activate, or dereference document-provided behavior.
- `DocumentSecuritySdkResult` must preserve finding and interaction order, `IsBlocked`, `HasReport`, `ManualInspection`, and `ReportGeneratedAtUtc`, and must remain composed only of SDK-owned types.
- No security policy action, interaction decision, report-generation rule, resource bound, failure contract, or service/adapter mapping may change.

## Test-first regression plan

Tests must be added and observed failing against the current raw-array exposure before production files are edited.

### BlackFlag.Engine.Security tests

Add:

- `tests/BlackFlag.Engine.Security.Tests/ResultCollectionImmutabilityTests.cs`

Cover all ten affected `BlackFlag.Engine.Security` result types in one focused boundary test class:

- assessment findings and interactions;
- report findings and interactions;
- remediation actions;
- container observations and embedded objects;
- embedded-inspection observations;
- Markdown, RTF, legacy Word/OLE, OOXML, and OpenDocument observations.

For each exposed collection, assert that it is not an array, is read-only when viewed as `IList<T>`, rejects indexed replacement with `NotSupportedException`, preserves the original element/order after the attempt, and returns the same cached property instance. Use representative non-empty data so replacement is exercised rather than only add/remove behavior.

For `DocumentSecurityAssessment`, construct a blocking finding, attempt replacement with a non-blocking finding, and assert both the original finding and `IsBlocked` remain stable. Also retain explicit checks for interaction filtering/order and `ShouldGenerateReport`. For the report and remediation plan, assert downstream report/action semantics remain stable after rejected mutation.

### BlackFlag.Engine.Parsing tests

Extend:

- `tests/BlackFlag.Engine.Parsing.Tests/Security/PdfSecurityAnalyzerTests.cs`

Add a focused assertion that a non-empty `PdfSecurityAnalysisResult.Observations` collection rejects indexed replacement, remains unchanged, and is a cached view. Keep the existing analyzer fixture and observation semantics; do not add PDF parsing behavior to this batch.

### BlackFlag.Engine.SDK tests

Extend:

- `tests/BlackFlag.Engine.SDK.Tests/Security/DocumentSecuritySdkServiceTests.cs`

Strengthen the existing `ResultCopiesCollections` coverage so both `Findings` and `Interactions` reject indexed replacement and remain stable, while preserving the existing proof that caller-owned inputs are copied. Assert `IsBlocked`, report metadata, ordering, and cached property identity remain unchanged.

Existing Services and SDK-adapter tests remain validation coverage for policy-to-report and internal-to-SDK mapping. They should not require modification unless a test-only compilation adjustment is strictly necessary; such a need must be reported before expanding the planned file list.

## Behavioral and implementation risks

| Risk | Control |
|---|---|
| A policy result changes while replacing its backing representation. | Keep all policy predicates and transformations intact; add blocked/report-generation characterization assertions before production edits. |
| Interaction filtering, de-duplication, or ordering changes. | Wrap only after the current `Where(...).Distinct().ToArray()` pipeline and test representative ordered input. |
| Empty-result construction changes or produces null collections. | Preserve current empty-array behavior and cover empty collections through existing analyzer tests. |
| A per-access allocation is introduced. | Cache one `ReadOnlyCollection<T>` in each constructor and assert reference identity on repeated property access. |
| Enumeration or element identity changes. | Wrap the existing snapshot without sorting, projection, or deep copying. |
| An internal consumer relies on the concrete array type. | Compile all affected projects and run their complete tests; retain only the documented `IReadOnlyList<T>` contract. |
| Scope expands into policy, analyzer, or DTO redesign. | Limit production edits to the 12 listed files and collection backing representation only. |

## Implementation sequence after approval

1. Reconfirm the Batch 1 commit, branch, and clean working tree.
2. Add the three planned regression-test changes and run the focused tests to demonstrate the current cast-mutation defect.
3. Change only the 12 listed production types to cached `ReadOnlyCollection<T>` views.
4. Run the focused tests until they pass.
5. Inspect `git status --short` and the complete diff for scope and semantic preservation.
6. Run complete affected-project tests, complete solution tests, whitespace validation, and the release gate.
7. Stop and report results; do not commit or begin another batch.

## Validation sequence

Run in this order after implementation approval:

```powershell
dotnet test .\tests\BlackFlag.Engine.Security.Tests\BlackFlag.Engine.Security.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~ResultCollectionImmutabilityTests"

dotnet test .\tests\BlackFlag.Engine.Parsing.Tests\BlackFlag.Engine.Parsing.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PdfSecurityAnalyzerTests"

dotnet test .\tests\BlackFlag.Engine.SDK.Tests\BlackFlag.Engine.SDK.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DocumentSecuritySdkServiceTests"

dotnet test .\tests\BlackFlag.Engine.Security.Tests\BlackFlag.Engine.Security.Tests.csproj --configuration Release --no-restore

dotnet test .\tests\BlackFlag.Engine.Parsing.Tests\BlackFlag.Engine.Parsing.Tests.csproj --configuration Release --no-restore

dotnet test .\tests\BlackFlag.Engine.Services.Tests\BlackFlag.Engine.Services.Tests.csproj --configuration Release --no-restore

dotnet test .\tests\BlackFlag.Engine.SDK.Tests\BlackFlag.Engine.SDK.Tests.csproj --configuration Release --no-restore

dotnet test .\tests\BlackFlag.Engine.SDK.Adapters.Tests\BlackFlag.Engine.SDK.Adapters.Tests.csproj --configuration Release --no-restore

dotnet test .\PiratePDF.slnx --configuration Release --no-restore

git diff --check

powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

If a focused test filter matches no tests or a project path has changed, correct only the invocation and record the exact command used. Do not weaken or alter the release gate.

## Acceptance criteria

Batch 2 is complete only when:

1. All 16 listed collection properties reject mutation through concrete array, mutable-list, or `IList<T>` casts.
2. Each constructor continues to snapshot its input once and each property exposes one cached read-only view.
3. Assessment blocking and report-generation outcomes cannot be changed by callers after construction.
4. Report, remediation, container, format-analysis, PDF-analysis, and SDK-result membership and ordering remain stable after attempted mutation.
5. All public signatures and supported behavior remain unchanged.
6. All security-policy, passive-inspection, resource-bound, report, remediation, service, and SDK-mapping semantics remain unchanged.
7. Focused regression tests pass.
8. Complete Security, Parsing, Services, SDK, and SDK-adapter project tests pass.
9. Complete solution tests pass.
10. `git diff --check` passes.
11. The full PiratePDF release gate passes with analyzers clean.
12. The complete diff contains only the 12 approved production files, the three approved test-file changes, and the approved Batch 2 documentation output.

## Explicitly out of scope

- Any implementation before explicit approval of this plan.
- `AUD-P0-001` and `AUD-P0-002`, which were completed in Batch 1.
- Any P1, P2, or P3 audit finding.
- BF-1054 existing-text editing, typography, or other roadmap work.
- Security-policy actions, severity, interaction, report-generation, trust, authorization, or remediation semantics.
- Parser/analyzer detection logic, PDF object behavior, document preservation, serialization, or Writing behavior.
- Recursive-inspection budgets, decompression limits, cancellation, container parsing, or failure behavior.
- Redesign of the Security, Services, Parsing, SDK, or SDK-adapter architecture.
- Public API additions, removals, signature changes, or breaking versioning work.
- Deep immutability redesign of element types or unrelated collection-bearing DTOs.
- New collection abstractions, helper frameworks, dependencies, package updates, caching, pooling, or performance refactoring.
- Formatting, naming, comment, analyzer-suppression, generated-file, or unrelated cleanup.
- Commits, pushes, merges, tags, branch changes, or beginning Batch 3.

## Approval boundary

This document defines the Batch 2 implementation contract only. No production or test code may be changed until a human explicitly approves Batch 2 implementation. After approval and validation, work must stop again for human review before any later audit batch begins.

## Revision history

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Defined the test-first implementation and validation contract for security-result collection immutability. |
