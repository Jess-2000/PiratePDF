# Pre-BF-1054 Batch 3 — Abstractions Test Quality

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-006 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-2000, BF-1054 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## Executive summary

Batch 3 addresses only `AUD-P2-003 — Abstractions tests contain only a tautological smoke test` from the Phase A audit.

`BlackFlag.Engine.Abstractions` contains one production contract, `IRandomAccessSource`. Its test project contains only `SmokeTests.TestRunnerDiscoversTests`, whose sole assertion is `Assert.True(true)`. The smallest meaningful correction is test-only: replace that tautology with focused contract-shape and dependency-boundary tests for `IRandomAccessSource` and the Abstractions assembly.

No production-code change is required or authorized. The implementation must stop for renewed approval if the proposed tests reveal that a production or project-reference change is necessary.

## Verified baseline and sources of truth

Before this plan was written:

- Batch 2 was confirmed committed at `7d28c57` (`Harden security result collection boundaries`).
- The branch was `maintenance/pre-BF-1054-repository-optimization`.
- The working tree was clean.

The complete hand-authored scope was re-read:

- `src/BlackFlag.Engine.Abstractions/IO/IRandomAccessSource.cs`
- `src/BlackFlag.Engine.Abstractions/BlackFlag.Engine.Abstractions.csproj`
- `tests/BlackFlag.Engine.Abstractions.Tests/SmokeTests.cs`
- `tests/BlackFlag.Engine.Abstractions.Tests/BlackFlag.Engine.Abstractions.Tests.csproj`

The applicable contracts are `AGENTS.md`, the Phase A audit, `docs/Technical-Specifications/BlackFlag-Engine/Abstractions.md`, the Abstractions and dependency-direction sections of `docs/Blueprint/Volume-II-BlackFlag-Engine-Architecture.md`, and the current repository implementation. Existing SDK and Plugins architecture tests establish assembly-metadata inspection as a repository-supported dependency-boundary testing pattern.

## Exact existing tautological test

File:

- `tests/BlackFlag.Engine.Abstractions.Tests/SmokeTests.cs`

Test:

- `SmokeTests.TestRunnerDiscoversTests`

Current body:

```csharp
[Fact]
public void TestRunnerDiscoversTests()
{
    Assert.True(true);
}
```

The test always passes once xUnit invokes it. It proves only that the test runner discovered one test assembly and provides no regression signal for the sole Abstractions interface, its API shape, its ownership contract, its large-file offsets, its async/cancellation surface, or the assembly dependency boundary. A breaking change to `IRandomAccessSource` would not affect this test unless it independently caused compilation elsewhere.

## Stable contracts suitable for testing

### `IRandomAccessSource` public contract

The Abstractions specification and current interface agree on these stable requirements:

- the type is a public interface;
- it is position-independent and therefore takes an absolute offset on each read;
- `Length` is a get-only `long`, preserving files larger than 2 GB;
- synchronous reads return `int` and accept `(long offset, Span<byte> destination)`;
- asynchronous reads return `ValueTask<int>` and accept `(long offset, Memory<byte> destination, CancellationToken cancellationToken = default)`;
- the cancellation token is optional with its default value;
- ownership is explicit through both `IDisposable` and `IAsyncDisposable`.

These are appropriate contract-test targets because they describe the cross-module API itself and are independently stated in the technical specification. They do not prescribe how an I/O implementation reads files.

### Abstractions dependency boundary

The architecture places Abstractions immediately above Core and below I/O. Abstractions may depend on Core but must not acquire compiled dependencies on higher BlackFlag layers, PiratePDF application/UI assemblies, or Avalonia.

An assembly-metadata test should enumerate `typeof(IRandomAccessSource).Assembly.GetReferencedAssemblies()`, filter references beginning with `BlackFlag.Engine.`, and require every such reference to be the approved `BlackFlag.Engine.Core` layer. The assertion must allow the compiler to omit an unused Core metadata reference; it must not require Core to appear merely because the project file currently declares that project reference.

This protects dependency direction without parsing a repository-relative `.csproj`, depending on build-output `.deps.json`, or hard-coding framework assembly references that may change with the SDK.

## Smallest meaningful replacement

Replace the existing smoke-test file with one clearly named test file:

- remove `tests/BlackFlag.Engine.Abstractions.Tests/SmokeTests.cs`;
- add `tests/BlackFlag.Engine.Abstractions.Tests/IRandomAccessSourceContractTests.cs`.

The replacement class should contain three focused tests:

1. `ExposesPositionIndependentReadContract`
   - assert `IRandomAccessSource` is an interface;
   - assert `Length` exists, is `long`, is readable, and has no setter;
   - assert `Read(long, Span<byte>)` exists and returns `int`;
   - assert `ReadAsync(long, Memory<byte>, CancellationToken)` exists and returns `ValueTask<int>`;
   - assert the cancellation-token parameter is optional and has the default value.
2. `RequiresSynchronousAndAsynchronousDisposal`
   - assert the interface is assignable to both `IDisposable` and `IAsyncDisposable`.
3. `AssemblyReferencesOnlyApprovedBlackFlagLayer`
   - inspect compiled assembly references;
   - filter only `BlackFlag.Engine.*` names;
   - assert every result is `BlackFlag.Engine.Core`.

Use exact reflection lookups with the intended parameter types so a change from 64-bit to 32-bit offsets, `Span<byte>`/`Memory<byte>` drift, removal of cancellation, return-type drift, a writable `Length`, or disposal-contract removal causes a focused failure.

Do not create a fake `IRandomAccessSource` solely to assert behavior implemented by that fake. Concrete read, EOF, offset-validation, cancellation, concurrency, and disposal behavior belongs to `BlackFlag.Engine.IO` and is already exercised by its own test project; duplicating those tests here would not validate Abstractions.

## Production-code requirement

No production-code change is required.

The current `IRandomAccessSource` already matches its specification, and the current Abstractions project follows the documented dependency direction. Batch 3 corrects a false-green test signal; it does not correct runtime behavior.

If the replacement tests cannot pass without modifying `IRandomAccessSource`, its project file, or another production project, implementation must stop and report the conflict rather than expanding this batch.

## Public and API compatibility constraints

- Do not modify `IRandomAccessSource`.
- Do not add, remove, rename, or reorder public members or parameters.
- Do not change `long` offsets/length, `Span<byte>`, `Memory<byte>`, `ValueTask<int>`, cancellation, or disposal contracts.
- Do not modify `BlackFlag.Engine.Abstractions.csproj` or any project reference.
- Do not change any concrete `IRandomAccessSource` implementation.
- Do not add a public test-support abstraction or production helper.
- Do not add, remove, or update dependencies.

Only test class/file names change; they are not public product API.

## Test and validation sequence

Because `AUD-P2-003` is a test-quality finding rather than a production defect, no intentionally failing red-phase test is required. The evidence is removal of the unconditional tautology and replacement with assertions that are sensitive to real contract and architecture regressions.

After implementation approval, run:

```powershell
dotnet test .\tests\BlackFlag.Engine.Abstractions.Tests\BlackFlag.Engine.Abstractions.Tests.csproj --configuration Release --no-restore

dotnet test .\PiratePDF.slnx --configuration Release --no-restore

git diff --check

powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

Before the full gate, inspect `git status --short` and the complete diff. The only code diff should be the replacement Abstractions test file. As with prior review documents, gate-generated documentation summaries should be checked and excluded from Batch 3 churn after successful validation.

## Behavioral risks and controls

| Risk | Control |
|---|---|
| Reflection assertions overfit incidental compiler metadata. | Assert only specification-backed public members and BlackFlag dependency names; do not assert metadata order, tokens, framework references, or unused-reference emission. |
| A test double creates a false behavior guarantee. | Do not add one; test the interface shape and compiled dependency boundary directly. |
| The architecture test accidentally requires an unused Core assembly reference. | Permit zero or more filtered references, but reject every BlackFlag reference other than Core. |
| Intentional future API evolution becomes harder. | This is expected for a stable cross-module contract; an intentional breaking change must update its specification and tests together. |
| Scope expands into I/O implementation behavior. | Keep concrete read semantics in `BlackFlag.Engine.IO.Tests`; do not touch that project. |

## Acceptance criteria

Batch 3 is complete only when:

1. `SmokeTests.TestRunnerDiscoversTests` and `Assert.True(true)` no longer exist.
2. The replacement tests protect the documented 64-bit length/offset, sync read, async read, cancellation, and dual-disposal surface of `IRandomAccessSource`.
3. The replacement architecture test rejects compiled dependencies on any BlackFlag layer other than Core without requiring an unused Core reference to be emitted.
4. No fake implementation is tested as though it established behavior for all implementations.
5. No production or project file changes are made.
6. No public API or supported behavior changes.
7. The complete Abstractions test project passes with meaningful contract tests replacing the single tautology.
8. Complete solution tests pass.
9. `git diff --check` passes.
10. The full PiratePDF release gate passes with analyzers clean.
11. The final diff contains only the Abstractions test replacement and approved Batch 3 documentation output.

## Explicitly out of scope

- Any implementation before explicit approval of this plan.
- Production changes to `IRandomAccessSource` or any implementation.
- Changes to `BlackFlag.Engine.Abstractions.csproj` or test-project dependencies.
- Behavioral retesting of `FileRandomAccessSource`, parsing memory sources, cursors, or other concrete implementations.
- A reusable implementation-conformance suite or abstract test framework.
- `AUD-P2-001` graphics-interpreter decomposition.
- `AUD-P2-002` security-parser decomposition.
- `AUD-P1-001`, `AUD-P1-002`, or any other performance candidate.
- Any other P0, P1, P2, or P3 finding.
- BF-1054 or another roadmap feature.
- Dependencies, formatting, generated code, documentation modernization, or unrelated cleanup.
- Changes to any other test project.
- Commits, pushes, merges, tags, branch changes, or beginning Batch 4.

## Approval boundary

This document defines the Batch 3 implementation contract only. No production or test code may be changed until a human explicitly approves Batch 3 implementation. If implementation would require production or project-file changes, work must stop for additional direction.

## Revision history

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Defined the test-only correction and validation contract for Abstractions test quality. |
