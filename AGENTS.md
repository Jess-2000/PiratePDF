# PiratePDF Codex Repository Instructions

## Purpose

This file is the persistent operating guide for Codex when working in the PiratePDF repository.
It is a map and guardrail, not a replacement for the repository's architecture, milestone, ADR,
security, validation, and engineering-standards documents.

When instructions conflict, follow the most specific applicable repository instruction and the
current user task. Do not infer permission to broaden scope.

## Project

PiratePDF is a professional, free and open-source desktop PDF editor built on the BlackFlag Engine.

Primary implementation:
- C# / .NET
- BlackFlag Engine for PDF engine capabilities
- PiratePDF SDK and Services for stable public/orchestration boundaries
- Avalonia for the desktop application UI

BlackFlag Engine must remain platform-agnostic. Platform-specific behavior belongs in application
or platform adapters.

## Sources of truth

Before making a substantial change, locate and read the relevant repository documents. In
particular, use:
- `PROJECT-CHARTER.md` when present.
- `README.md` and project-level README files.
- `docs/adr/` or the repository ADR location.
- `docs/milestones/` for completed and current BF work-item contracts.
- The PiratePDF v1 Code & Engineering Standards and Practices document or its repository copy.
- Security / Zero-Trust architecture documents.
- Validation/release documentation and `Invoke-PiratePDFReleaseGate.ps1`.

Historical milestone documents are implementation history. Do not rewrite historical statements
merely because later milestones superseded them. Reconcile current state in current documentation.

## Current repository checkpoint

The current baseline is post-BF-1054:
- BF-1054 Existing Text Editing & Typography is completed and merged to `develop`.
- Preservation-aware Writing, transactional Editing, SDK/Services text-editing exposure, and BF-1054 closure hardening are established.
- The next major feature workstream is canonical font editing architecture unless the user selects another approved roadmap item.

Do not reopen completed BF-1054 behavior during unrelated maintenance. Historical milestone documents remain historical records.

## Non-negotiable engineering priorities

Optimize in this order:
1. Correctness.
2. PDF/document preservation.
3. Security and privacy.
4. Behavioral compatibility.
5. Clarity and maintainability.
6. Measured or strongly evidenced performance.
7. Compactness.

Never trade a higher priority for a lower one without explicit approval.

Apply:
- K.I.S.S.
- D.R.Y.
- Y.A.G.N.I.
- Zero-trust handling of untrusted document/input data.
- Small, reviewable, evidence-based changes.

## Repository-wide optimization rule

Review every hand-authored C# file when explicitly asked to perform the repository audit.

Do **not** change every file.

Change a file only when there is a concrete, defensible improvement in at least one of:
- correctness;
- security/privacy;
- measurable or clearly evidenced performance/resource use;
- maintainability/readability;
- duplication;
- reliability/error handling;
- testability;
- compliance with established PiratePDF engineering standards.

Do not create churn for formatting, personal preference, novelty, or theoretical micro-optimization.

## Behavior-preservation constraints

Unless the current task explicitly authorizes a behavior change:
- Preserve observable behavior.
- Preserve public API behavior and signatures.
- Preserve serialization semantics.
- Preserve PDF object identity and generation semantics.
- Preserve xref and incremental-update semantics.
- Preserve original-byte/revision behavior where the Writing architecture requires it.
- Preserve Editing transaction, ownership, rollback, undo/redo, and replay semantics.
- Preserve plugin SDK boundaries, capability controls, isolation, and security policy.
- Preserve rendering/parsing behavior and malformed-document failure semantics.
- Preserve platform portability.
- Preserve exception behavior where callers/tests may depend on it.

If an apparent optimization could alter any of these, stop and report it as a candidate requiring
explicit approval rather than applying it.

## PDF-specific preservation rules

Treat these as preservation-sensitive:
- indirect object identifiers and generations;
- object-stream membership and compressed objects;
- xref tables, xref streams, `/Prev`, `startxref`, and revision boundaries;
- stream bytes, filters, decode parameters, and lengths;
- page-tree parentage, `/Kids`, `/Count`, inheritance, and object identity;
- resource dictionaries and inherited resources;
- font/resource identity, encoding, glyph mapping, and embedding permissions;
- annotations, metadata, document information, outlines, forms, and unknown dictionary entries;
- content-stream ordering and graphics-state semantics.

Do not normalize or rewrite unrelated valid PDF state merely because it is possible.

Malformed, cyclic, contradictory, hostile, or unsupported structures must fail predictably. Do not
replace bounded validation with optimistic assumptions.

## Code standards

### Naming and structure
- Use simple, descriptive names.
- Keep responsibilities cohesive.
- Keep public contracts intentional and small.
- Avoid unexplained abbreviations.
- Avoid generic utility dumping grounds.

### File size
- No hand-authored source file may reach 1,000 lines.
- A hand-authored file in the 600-800 line range requires explicit decomposition review.
- Split by cohesive responsibility, not arbitrary line count.
- Generated files must be clearly generated and reproducible.

### Control flow
- Prefer guard clauses over deep nesting.
- Prefer `switch` statements/expressions for discrete dispatch when clearer than long `if` chains.
- Prefer `for` loops when indexed/deterministic positional processing is clearer.
- Use recursion when it naturally models hierarchical PDF structures, with cycle/depth protections
  where malformed input could recurse indefinitely.
- Do not mechanically replace valid constructs merely to satisfy a stylistic preference.
- LINQ is acceptable when it improves clarity and does not create unjustified hot-path allocations.

### Comments and documentation
- Preserve useful comments.
- Comments should explain intent, invariants, preservation rules, algorithms, or non-obvious reasons.
- Do not narrate obvious syntax.
- Remove or update stale comments.
- Do not leave permanent TODO/FIXME markers without a concrete tracked follow-up.

### Exceptions and validation
- Validate inputs and preservation-critical invariants at ownership boundaries.
- Use specific exception types and actionable messages.
- Do not swallow exceptions without a documented recovery policy.
- Transactional operations must complete coherently or restore prior state.
- Caller-owned resources must not be disposed unexpectedly.

## Performance rules

Do not optimize by instinct alone.

Prefer improvements that remove:
- repeated parsing/decoding;
- repeated dictionary/list scans in demonstrably hot paths;
- unnecessary whole-document materialization;
- avoidable large allocations/copies;
- accidental quadratic behavior;
- repeated object resolution;
- unnecessary serialization/rewrite work;
- unbounded caches or recursion;
- disposable-resource leaks.

Use `Span<T>`, pooling, caching, streaming, specialized collections, or low-level tricks only when:
- the path plausibly matters;
- the change is demonstrably safe;
- ownership/lifetime is clear;
- readability remains acceptable;
- tests/benchmarks or strong complexity evidence support it.

Do not introduce unsafe code solely for speed without explicit approval.

## Dependencies

- Prefer the .NET Base Class Library and existing project capabilities.
- Do not add, remove, or upgrade NuGet/native/runtime dependencies during a cleanup/optimization
  audit without explicit approval.
- Do not introduce platform-specific dependencies into BlackFlag Engine.
- Preserve licensing/provenance rules.

## Testing

Every applied change must have validation proportional to risk.

Requirements:
- Existing tests must continue to pass.
- Add regression tests for corrected defects when deterministic reproduction is possible.
- Add/strengthen tests before risky refactors if behavior is insufficiently protected.
- Preservation-sensitive Writing/Parsing/Editing changes require appropriate integration or
  serialization/reopen coverage, not only intermediate-object assertions.
- Tests must remain deterministic and isolated.
- Do not weaken/delete tests to make a refactor pass.
- Do not replace meaningful assertions with looser assertions without explicit justification.

## Security

Treat PDFs, embedded content, plugin input, paths, URIs, external metadata, serialized data, and
future network/database input as untrusted.

Protect against applicable risks including:
- integer overflow;
- invalid offsets/lengths;
- excessive allocation;
- decompression bombs;
- recursion/cycles;
- path traversal;
- implicit execution;
- unsafe external process/network access;
- secret leakage;
- sensitive document-content logging.

Do not weaken validation, sanitization, capability checks, or least-privilege boundaries for speed.


### Additional security-hardening rules
- Treat hostile-file parsing and resource exhaustion as primary desktop threat surfaces. Bound lengths, offsets, object counts, recursion/depth, decompression, allocations, and aggregate work.
- Fuzz high-risk byte-oriented boundaries and add deterministic regression fixtures for reproducible crashes, hangs, corruption, or invariant violations.
- PDF JavaScript, launch actions, embedded executables/files, external commands, and network actions must not execute implicitly.
- Minimize sensitive document/credential data lifetime and copies. Do not claim guaranteed erasure of arbitrary managed memory; clear mutable secret buffers only where technically meaningful.
- Native/PInvoke boundaries must be narrow and validated. Do not rely on writable/current-directory native-library search paths; verify packaged DLL/dylib resolution.
- Track dependency purpose/version/license/provenance/security-update path and release SBOM/third-party notices where applicable.
- Native AOT/trimming/symbol stripping are deployment-hardening choices, not security or secrecy boundaries. Treat AOT/trimming warnings as engineering work and validate reflection, serialization, plugins, SkiaSharp/native assets, fonts/images, and platform adapters.
- Official Windows release artifacts should be Authenticode-signed; official macOS artifacts should be code-signed/notarized. Protect signing credentials, record hashes/provenance, and pursue reproducible/deterministic release builds where practical.

## Git and change discipline

For repository optimization/hardening work:
- Work only on the dedicated maintenance/optimization branch selected by the user.
- Do not create or switch branches unless explicitly instructed.
- Do not merge.
- Do not tag.
- Do not push unless explicitly instructed.
- Do not commit unless explicitly instructed.
- Do not modify unrelated files.
- Keep batches reviewable by subsystem.
- Check `git status` and `git diff` frequently.
- Never discard user changes.
- Never rewrite repository history.

## Audit classification

Classify findings as:
- P0: correctness, data integrity, security, or corruption risk.
- P1: significant/evidenced performance or resource issue.
- P2: maintainability, duplication, reliability, complexity, or testability issue.
- P3: stylistic/preference-only issue.

P3 findings should normally be left unchanged.

For each proposed change record:
- file/path;
- finding;
- classification;
- evidence;
- behavioral risk;
- proposed change;
- required validation.

## Audit workflow

When asked for the full repository optimization, use two distinct phases.

### Phase A - Audit only
1. Confirm branch and working-tree state.
2. Inventory all hand-authored `.cs` files.
3. Identify generated/vendor code and exclude it from hand refactoring.
4. Read architecture/ADR/milestone/security documents relevant to each subsystem.
5. Inspect every hand-authored C# file.
6. Build/test/analyze as useful for evidence.
7. Produce a prioritized audit report.
8. Do not edit production/test C# files during Phase A.

### Phase B - Approved implementation batches
Only begin after the user approves the audit or a specific batch.

For each batch:
1. State the exact files/findings being addressed.
2. Add/strengthen tests first when risk warrants it.
3. Make the smallest coherent changes.
4. Run targeted tests.
5. Run broader affected-project tests.
6. Run `git diff --check`.
7. Run the full PiratePDF release gate when the batch is intended for acceptance.
8. Summarize changes, evidence, tests, remaining risks, and deferred findings.

## Release gate

The authoritative repository completion check is:

```powershell
.\Invoke-PiratePDFReleaseGate.ps1
```

Do not alter, bypass, weaken, or suppress release-gate checks to make an optimization pass.

If the gate fails:
- diagnose the failure;
- fix the underlying issue if it belongs to the approved batch;
- do not paper over analyzer/test/DocGen failures;
- report unrelated pre-existing failures distinctly.

A batch is not complete until the required gate passes.

## Definition of done for an optimization batch

A batch is complete only when:
- scope stayed within approved findings;
- intended behavior is preserved unless an approved defect was corrected;
- code is simpler/safer/faster for a concrete reason;
- tests appropriate to risk pass;
- analyzers/build remain clean;
- documentation/comments are accurate;
- `git diff --check` passes;
- the required release gate passes;
- no unrelated churn remains;
- the final report identifies exactly what changed and why.

## Stop conditions

Stop and ask for approval rather than proceeding when:
- a public API must change;
- PDF semantics or preservation behavior may change;
- a dependency change is needed;
- a security policy/boundary would change;
- tests reveal ambiguous intended behavior;
- a broad architectural rewrite appears necessary;
- a performance change requires unsafe code or a major complexity tradeoff;
- a finding belongs to BF-1054 or another future feature rather than maintenance;
- the working tree contains unexpected user changes that overlap the intended work.

When in doubt, preserve behavior and report the candidate.
