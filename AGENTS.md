# PiratePDF Codex Repository Instructions

## Purpose

This file is the persistent operating guide for Codex when working in
the PiratePDF repository. It is a map and guardrail, not a replacement
for the repository's architecture, milestone, ADR, security, validation,
and engineering-standards documents.

When instructions conflict, follow the most specific applicable
repository instruction and the current user task. Do not infer
permission to broaden scope.

## Project

PiratePDF is a professional, free and open-source desktop PDF editor
built on the BlackFlag Engine.

Primary implementation: - C# / .NET - BlackFlag Engine for PDF engine
capabilities - PiratePDF SDK and Services for stable
public/orchestration boundaries - Avalonia for the desktop application
UI

BlackFlag Engine must remain platform-agnostic. Platform-specific
behavior belongs in application or platform adapters.

## Sources of truth

Before making a substantial change, locate and read the relevant
repository documents. In particular, use: - `PROJECT-CHARTER.md` when
present. - `README.md` and project-level README files. - `docs/adr/` or
the repository ADR location. - `docs/milestones/` for completed and
current BF work-item contracts. - The PiratePDF v1 Code & Engineering
Standards and Practices document or its repository copy. - Security /
Zero-Trust architecture documents. - Validation/release documentation
and `Invoke-PiratePDFReleaseGate.ps1`. -
`docs/handoffs/PiratePDF-Maintenance-Handoff-Post-BF-1054-Part1.md` for
the accepted post-BF-1054 Maintenance Part 1 continuation state. Read
this handoff before beginning a new post-BF-1054 maintenance batch.

Historical milestone documents are implementation history. Do not
rewrite historical statements merely because later milestones superseded
them. Reconcile current state in current documentation.

## Current repository checkpoint

The accepted Post-BF-1054 Maintenance Part 1 baseline is: - Integrated
checkpoint commit: `095ad01`
(`Merge pull request #21 from Jess-2000/develop`). - `main` and
`origin/main` were at `095ad01` when the handoff was established. -
Continued maintenance branch:
`maintenance/post-BF-1054-repository-review`. - The maintenance branch
was fast-forwarded locally to `095ad01`; at that point its remote still
referenced the earlier Part 1 checkpoint `e27d386`. - Working tree at
handoff: clean. - Recovery snapshot:
`PiratePDF-BF-Post-1054-Maintenance-Part1-Snapshot.zip`. - Corrected
cross-platform CI passed, and CI on `main` after the GitHub pull-request
merge was green. - PP-AUD-038 CI Portability Remediation is accepted. -
B9 / POST-P2-002 remains open because the substantive bounded
coverage-guided campaign has not begun. - BF-1054 Existing Text Editing
& Typography remains completed and integrated. - Preservation-aware
Writing, transactional Editing, SDK/Services text-editing exposure, and
BF-1054 closure hardening are established. - Canonical font editing
remains the next major feature workstream after maintenance unless the
user selects another approved roadmap item.

For continuation details, read
`docs/handoffs/PiratePDF-Maintenance-Handoff-Post-BF-1054-Part1.md`.

Do not reopen completed BF-1054 behavior during unrelated maintenance.
Historical milestone documents remain historical records.

## Non-negotiable engineering priorities

Optimize in this order: 1. Correctness. 2. PDF/document preservation. 3.
Security and privacy. 4. Behavioral compatibility. 5. Clarity and
maintainability. 6. Measured or strongly evidenced performance. 7.
Compactness.

Never trade a higher priority for a lower one without explicit approval.

Apply: - K.I.S.S. - D.R.Y. - Y.A.G.N.I. - Zero-trust handling of
untrusted document/input data. - Small, reviewable, evidence-based
changes.

## Repository-wide optimization rule

Review every hand-authored C# file when explicitly asked to perform the
repository audit.

Do **not** change every file.

When modifying a hand-authored C# file for an approved task, leave the
touched area no dirtier than you found it. Remove imports or private
implementation members that become conclusively unused as a direct
result of the approved change, provided removal is local,
behavior-preserving, and satisfies the source-hygiene rules below. Do
not expand the task into an unrelated repository-wide cleanup.

Change a file only when there is a concrete, defensible improvement in
at least one of: - correctness; - security/privacy; - measurable or
clearly evidenced performance/resource use; -
maintainability/readability; - duplication; - reliability/error
handling; - testability; - compliance with established PiratePDF
engineering standards.

Do not create churn for formatting, personal preference, novelty, or
theoretical micro-optimization.

## Behavior-preservation constraints

Unless the current task explicitly authorizes a behavior change: -
Preserve observable behavior. - Preserve public API behavior and
signatures. - Preserve serialization semantics. - Preserve PDF object
identity and generation semantics. - Preserve xref and
incremental-update semantics. - Preserve original-byte/revision behavior
where the Writing architecture requires it. - Preserve Editing
transaction, ownership, rollback, undo/redo, and replay semantics. -
Preserve plugin SDK boundaries, capability controls, isolation, and
security policy. - Preserve rendering/parsing behavior and
malformed-document failure semantics. - Preserve platform portability. -
Preserve exception behavior where callers/tests may depend on it.

If an apparent optimization could alter any of these, stop and report it
as a candidate requiring explicit approval rather than applying it.

## PDF-specific preservation rules

Treat these as preservation-sensitive: - indirect object identifiers and
generations; - object-stream membership and compressed objects; - xref
tables, xref streams, `/Prev`, `startxref`, and revision boundaries; -
stream bytes, filters, decode parameters, and lengths; - page-tree
parentage, `/Kids`, `/Count`, inheritance, and object identity; -
resource dictionaries and inherited resources; - font/resource identity,
encoding, glyph mapping, and embedding permissions; - annotations,
metadata, document information, outlines, forms, and unknown dictionary
entries; - content-stream ordering and graphics-state semantics.

Do not normalize or rewrite unrelated valid PDF state merely because it
is possible.

Malformed, cyclic, contradictory, hostile, or unsupported structures
must fail predictably. Do not replace bounded validation with optimistic
assumptions.

## Code standards

### Naming and structure

-   Use simple, descriptive names.
-   Keep responsibilities cohesive.
-   Keep public contracts intentional and small.
-   Avoid unexplained abbreviations.
-   Avoid generic utility dumping grounds.

### File size

-   No hand-authored source file may reach 1,000 lines.
-   A hand-authored file in the 600-800 line range requires explicit
    decomposition review.
-   Split by cohesive responsibility, not arbitrary line count.
-   Generated files must be clearly generated and reproducible.

### Control flow

-   Prefer guard clauses over deep nesting.
-   Prefer `switch` statements/expressions for discrete dispatch when
    clearer than long `if` chains.
-   Prefer `for` loops when indexed/deterministic positional processing
    is clearer.
-   Use recursion when it naturally models hierarchical PDF structures,
    with cycle/depth protections where malformed input could recurse
    indefinitely.
-   Do not mechanically replace valid constructs merely to satisfy a
    stylistic preference.
-   LINQ is acceptable when it improves clarity and does not create
    unjustified hot-path allocations.

### Expressions, materialization, and deconstruction

-   Do not mechanically prefer newer or shorter syntax. Choose the form
    that makes ownership, evaluation, allocation, resource bounds, and
    domain intent clearest.
-   Do not mechanically replace `ToArray()` with collection expressions
    or collection expressions with `ToArray()`.
-   Prefer collection expressions for straightforward fixed construction
    and known/materialized collection copies when independent ownership
    is clear.
-   Prefer or retain `ToArray()` when it more clearly establishes an
    intentional eager materialization boundary, especially for
    lazy/stateful enumerables, LINQ pipelines, parser/tokenizer
    pipelines, security-bounded enumeration, or sequences whose
    evaluation/exception behavior matters.
-   Neither `ToArray()` nor collection-expression spread is inherently
    faster or more secure. For performance-sensitive choices, use
    measurement or strong complexity/allocation evidence rather than
    syntax preference.
-   Ensure attacker-controlled enumeration is bounded and makes progress
    before relying on eager materialization. Syntax must not obscure
    resource ceilings or mutation/ownership boundaries.
-   Use deconstruction when the extracted values are meaningfully
    related and the resulting names remain clear. Prefer named variables
    or member access when deconstruction introduces unused values,
    obscures domain meaning/type/ownership/lifetime, or makes
    security-sensitive code harder to audit.
-   Keep descriptive intermediate variables when they capture state,
    provenance, numeric width, resource budgets, offsets, ownership,
    lifetime, or another meaningful invariant. Inline them only when
    evaluation timing, side effects, exception behavior, and readability
    remain clearly equivalent.
-   Use `var` when the type is evident and unimportant to understanding
    the operation. Prefer an explicit type when numeric
    width/signedness, interface versus implementation, ownership,
    lifetime, or another semantic distinction matters.
-   Before simplifying an expression, preserve evaluation count,
    enumeration count, ordering, side effects, exception timing,
    cancellation, disposal, null behavior, deferred/eager execution,
    mutation, and aliasing.
-   Add a concise comment when a non-obvious materialization or
    expression choice protects an important ownership, evaluation,
    preservation, security, or resource-bound invariant. Explain why the
    form is intentional rather than narrating its syntax.

### Source hygiene, unused code, and dead-code removal

Keep hand-authored source free of genuinely unused imports and
implementation code, but treat removal as a correctness and
compatibility decision rather than a cosmetic cleanup.

-   Remove unused ordinary `using` directives when compiler/Roslyn
    analysis establishes that they are unnecessary.
-   Evaluate `global using`, alias, and `using static` directives at the
    appropriate project/semantic scope rather than by same-file textual
    search.
-   Account for extension-method resolution, aliases, conditional
    compilation, generated-code interaction, and supported
    target/platform configurations before removing an import.
-   Do not reorganize, fully qualify, or otherwise rewrite live code
    merely to eliminate a `using`.

For apparently unused members, absence of an ordinary C# call site is
not sufficient evidence that the member is dead.

Before removing a type, constructor, method, property, field, event,
delegate, operator, overload, local function, parameter, or other
member, account for applicable use through:

-   interfaces and explicit/implicit interface implementation;
-   overrides, virtual dispatch, inheritance, and protected extension
    points;
-   public or protected API consumption outside the repository;
-   delegates, events, callbacks, method groups, and expression trees;
-   reflection, `Activator`, attributes, name-based discovery, and
    runtime registration;
-   plugin discovery, activation, manifests, factories, and lifecycle
    contracts;
-   serialization/deserialization and generated-code contracts;
-   P/Invoke, native callbacks, function pointers, and platform
    adapters;
-   test/fixture/data-source discovery;
-   conditional compilation and supported target frameworks/platforms;
-   partial types and declarations in other files;
-   tooling, scripts, samples, documentation, and repository conventions
    that invoke symbols indirectly.

Apply increasingly conservative removal standards as accessibility
increases:

-   Private implementation members may be removed when repository-wide
    semantic analysis establishes that they are genuinely unreachable
    and unnecessary.
-   Internal members require cross-project, test, tooling, and
    `InternalsVisibleTo` analysis before removal.
-   Public and protected members are API/extension surface. Do not
    remove them merely because the repository contains no caller. Retain
    them unless an explicitly approved API change establishes that
    removal is compatibility-safe.
-   Required interface implementations and overrides are live contract
    members even when no direct call site exists.

When usage is dynamic, framework-driven, platform-specific, generated,
externally consumable, or otherwise uncertain, retain the member. False
retention is preferable to false deletion.

Additional cleanup rules:

-   Inspect every partial declaration before classifying a member as
    dead.
-   Do not remove constructors solely because there is no direct `new`
    expression; account for reflection, factories, serializers, plugins,
    generic activation, and frameworks.
-   Do not remove setters/properties solely because there is no direct
    assignment/read; account for serialization, binding, reflection,
    interfaces, and generated code.
-   Do not remove event handlers or callbacks solely because there is no
    conventional invocation expression.
-   Do not remove `Dispose`, `DisposeAsync`, finalizers, equality/hash
    members, `ToString`, operators, conversions, entry points, lifecycle
    hooks, or throw-only/no-op contract implementations based only on
    direct-reference counts.
-   Do not remove public constants, public enum members, public
    exception types, or public overloads as routine dead-code cleanup.
-   Do not renumber enums or alter persisted/serialized numeric values
    during cleanup.
-   Treat struct-layout, interop, serialization, and native-facing
    fields as contract-sensitive.
-   Treat `CancellationToken` parameters as contract-sensitive; do not
    remove them merely because the current implementation does not
    consume them.
-   Remove private/internal unused parameters or dead overloads only
    when all controlled callers and delegate/signature contracts remain
    equivalent.
-   Remove unused local functions and newly orphaned private helpers
    only after applying the same semantic/evaluation analysis.
-   A test is not dead merely because another test appears similar.
    Preserve unique behavioral and regression coverage.
-   Production members referenced only by tests are not automatically
    dead; determine whether the tests are validating an intentional
    production contract.
-   Tool, sample, plugin, and platform entry points may be externally
    invoked and must not be classified by ordinary application call
    graphs alone.
-   Documented architectural seams should normally be retained even when
    the current implementation has no caller. Undocumented private
    implementation with no callers or contract is a stronger removal
    candidate.
-   Clearly obsolete commented-out implementation may be removed when
    version control preserves its history, but preserve rationale,
    invariants, specification notes, TODOs with tracked follow-up, and
    explanatory comments that remain relevant.
-   Remove comments/XML documentation that solely describe a deleted
    member; do not use dead-code cleanup as a general documentation
    rewrite.
-   If deletion leaves an otherwise unnecessary private/internal type or
    source file empty, reassess it under the same dead-code rules rather
    than deleting it mechanically.

Dead-code cleanup must not be combined opportunistically with:

-   API redesign;
-   renaming;
-   formatting churn;
-   syntax modernization;
-   LINQ/control-flow rewrites;
-   visibility changes to make a member removable;
-   inlining of live helpers merely to reduce member count;
-   performance refactoring;
-   security-policy changes;
-   dependency/project-reference changes;
-   feature implementation.

For repository-wide cleanup work, characterize candidates before
deleting them. Classify each meaningful candidate as one of:

-   remove --- proven dead;
-   retain --- public/protected API;
-   retain --- interface/override;
-   retain --- reflection/dynamic;
-   retain --- plugin/framework;
-   retain --- serialization/native;
-   retain --- conditional target;
-   retain --- documented architecture;
-   retain --- uncertain;
-   investigate further.

A non-local member should be removed only when the evidence establishes
that removal preserves supported behavior and compatibility. If analysis
is inconclusive, keep the code and report the candidate rather than
guessing.

Perform cleanup in small coherent groups. Build and run affected tests
after meaningful deletion groups. If deleting supposedly dead code
causes a build, analyzer, test, DocGen, API-surface, platform, or
runtime-validation failure, treat that as evidence that the code may be
live; investigate the assumption rather than weakening validation.

After member cleanup, perform a final compiler/analyzer-backed
unused-import pass so imports orphaned by legitimate deletions are
removed without creating unrelated churn.

Unless the current task explicitly authorizes otherwise, source-hygiene
cleanup must preserve observable behavior and public/protected API
surface.

### Comments and documentation

-   Preserve useful comments.
-   Comments should explain intent, invariants, preservation rules,
    algorithms, or non-obvious reasons.
-   Do not narrate obvious syntax.
-   Remove or update stale comments.
-   Do not leave permanent TODO/FIXME markers without a concrete tracked
    follow-up.

### Exceptions and validation

-   Validate inputs and preservation-critical invariants at ownership
    boundaries.
-   Use specific exception types and actionable messages.
-   Do not swallow exceptions without a documented recovery policy.
-   Transactional operations must complete coherently or restore prior
    state.
-   Caller-owned resources must not be disposed unexpectedly.

## Performance rules

Do not optimize by instinct alone.

Prefer improvements that remove: - repeated parsing/decoding; - repeated
dictionary/list scans in demonstrably hot paths; - unnecessary
whole-document materialization; - avoidable large allocations/copies; -
accidental quadratic behavior; - repeated object resolution; -
unnecessary serialization/rewrite work; - unbounded caches or
recursion; - disposable-resource leaks.

Use `Span<T>`, pooling, caching, streaming, specialized collections, or
low-level tricks only when: - the path plausibly matters; - the change
is demonstrably safe; - ownership/lifetime is clear; - readability
remains acceptable; - tests/benchmarks or strong complexity evidence
support it.

Do not introduce unsafe code solely for speed without explicit approval.

## Dependencies

-   Prefer the .NET Base Class Library and existing project
    capabilities.
-   Do not add, remove, or upgrade NuGet/native/runtime dependencies
    during a cleanup/optimization audit without explicit approval.
-   When the current task explicitly authorizes dependency remediation
    and evidence directly implicates an existing dependency/toolchain
    component, a current stable upstream update of that same component
    may be used after reviewing provenance, release notes, licensing,
    transitive changes, and compatibility. Do not turn that
    authorization into an unrelated dependency sweep or architecture
    replacement.
-   Do not introduce platform-specific dependencies into BlackFlag
    Engine.
-   Preserve licensing/provenance rules.

## Testing

Every applied change must have validation proportional to risk.

Requirements: - Existing tests must continue to pass. - Add regression
tests for corrected defects when deterministic reproduction is
possible. - Add/strengthen tests before risky refactors if behavior is
insufficiently protected. - Preservation-sensitive
Writing/Parsing/Editing changes require appropriate integration or
serialization/reopen coverage, not only intermediate-object
assertions. - Tests must remain deterministic and isolated. - Do not
weaken/delete tests to make a refactor pass. - Treat build/test failures
after dead-code removal as evidence that the removed code may have been
live; investigate and restore it when the removal cannot be proven safe
rather than changing callers/tests merely to preserve the deletion. -
Repository dead-code cleanup must not reduce test discovery or coverage
silently. Reconcile and explain any changed test count. - When cleanup
could affect externally visible members, compare the public/protected
API surface before and after where practical; a passing in-repository
test suite alone does not prove external API compatibility. - Do not
replace meaningful assertions with looser assertions without explicit
justification.

## Security

Treat PDFs, embedded content, plugin input, paths, URIs, external
metadata, serialized data, and future network/database input as
untrusted.

Protect against applicable risks including: - integer overflow; -
invalid offsets/lengths; - excessive allocation; - decompression
bombs; - recursion/cycles; - path traversal; - implicit execution; -
unsafe external process/network access; - secret leakage; - sensitive
document-content logging.

Do not weaken validation, sanitization, capability checks, or
least-privilege boundaries for speed.

### Additional security-hardening rules

-   Treat hostile-file parsing and resource exhaustion as primary
    desktop threat surfaces. Bound lengths, offsets, object counts,
    recursion/depth, decompression, allocations, and aggregate work.
-   Fuzz high-risk byte-oriented boundaries and add deterministic
    regression fixtures for reproducible crashes, hangs, corruption, or
    invariant violations.
-   PDF JavaScript, launch actions, embedded executables/files, external
    commands, and network actions must not execute implicitly.
-   Minimize sensitive document/credential data lifetime and copies. Do
    not claim guaranteed erasure of arbitrary managed memory; clear
    mutable secret buffers only where technically meaningful.
-   Native/PInvoke boundaries must be narrow and validated. Do not rely
    on writable/current-directory native-library search paths; verify
    packaged DLL/dylib resolution.
-   Track dependency purpose/version/license/provenance/security-update
    path and release SBOM/third-party notices where applicable.
-   Native AOT/trimming/symbol stripping are deployment-hardening
    choices, not security or secrecy boundaries. Treat AOT/trimming
    warnings as engineering work and validate reflection, serialization,
    plugins, SkiaSharp/native assets, fonts/images, and platform
    adapters.
-   Official Windows release artifacts should be Authenticode-signed;
    official macOS artifacts should be code-signed/notarized. Protect
    signing credentials, record hashes/provenance, and pursue
    reproducible/deterministic release builds where practical.

## Manual commands and elevated execution

Codex should perform ordinary repository commands itself when they can
be run safely in the available environment. Do not ask the user to
manually run commands merely for convenience.

When a required validation, setup, diagnostic, or repository command
cannot be completed because it needs permissions or an execution context
Codex does not have, stop at the appropriate boundary and tell the user
exactly what must be run manually.

In particular:

-   Explicitly identify when a command must be run from an
    **Administrator PowerShell** or an **Administrator Command Prompt**.
-   If elevation is not required but the command must still be run
    manually, state whether **regular PowerShell** or **regular Command
    Prompt** is appropriate.
-   Prefer PowerShell commands for this repository unless the required
    tool specifically needs Command Prompt or another shell.
-   Provide the exact working directory and complete copy/paste-ready
    command(s), including required arguments.
-   State briefly why manual execution or elevation is required and what
    result/output Codex needs from the user afterward.
-   Never tell the user to weaken execution policy, security controls,
    filesystem protections, certificate protections, or OS policy merely
    to make a command pass unless the current task explicitly requires
    such a policy change and the user approves it.
-   Never claim a privileged/manual validation passed when Codex could
    not execute it. Record it as blocked or pending until the user
    reports the actual result.
-   After the user supplies the result, incorporate that evidence into
    the current validation/report rather than rerunning or redesigning
    unrelated work.
-   If a non-elevated alternative provides equivalent validation without
    weakening the contract, prefer it.
-   Do not request elevation preemptively. Ask for Administrator
    execution only when the actual command or fixture demonstrably
    requires it.

Example: if a Windows test must create a symbolic-link fixture and the
current process receives a privilege error before the validator runs,
preserve the test and report the blocker. Ask the user to rerun the
exact focused test from Administrator PowerShell, followed by the
required broader test suite if the focused test passes. Do not skip or
weaken the test.

## Git and change discipline

For repository optimization/hardening work: - Work only on the dedicated
maintenance/optimization branch selected by the user. - Do not create or
switch branches unless explicitly instructed. - Do not merge. - Do not
tag. - Do not push unless explicitly instructed. - Do not commit unless
explicitly instructed. - Do not modify unrelated files. - Keep batches
reviewable by subsystem. - Evidence-driven corrective iteration within
an approved batch is allowed. A validation slice may require multiple
corrective iterations when each change is directly supported by the
observed failure; do not stop merely because a necessary correction
touches production code. - Check `git status` and `git diff`
frequently. - Never discard user changes. - Never rewrite repository
history.

## Audit classification

Classify findings as: - P0: correctness, data integrity, security, or
corruption risk. - P1: significant/evidenced performance or resource
issue. - P2: maintainability, duplication, reliability, complexity, or
testability issue. - P3: stylistic/preference-only issue.

P3 findings should normally be left unchanged.

For each proposed change record: - file/path; - finding; -
classification; - evidence; - behavioral risk; - proposed change; -
required validation.

## Audit workflow

When asked for the full repository optimization, use two distinct
phases.

### Phase A - Audit only

1.  Confirm branch and working-tree state.
2.  Inventory all hand-authored `.cs` files.
3.  Identify generated/vendor code and exclude it from hand refactoring.
4.  Read architecture/ADR/milestone/security documents relevant to each
    subsystem.
5.  Inspect every hand-authored C# file.
6.  Build/test/analyze as useful for evidence.
7.  Produce a prioritized audit report.
8.  Do not edit production/test C# files during Phase A.

### Phase B - Approved implementation batches

Only begin after the user approves the audit or a specific batch.

For each batch: 1. State the exact files/findings being addressed. 2.
Add/strengthen tests first when risk warrants it. 3. Make the smallest
coherent changes. 4. Run targeted tests. 5. Run broader affected-project
tests. 6. Run `git diff --check`. 7. Run the full PiratePDF release gate
when the batch is intended for acceptance. 8. Summarize changes,
evidence, tests, remaining risks, and deferred findings.

## Release gate

The authoritative repository completion check is:

``` powershell
.\Invoke-PiratePDFReleaseGate.ps1
```

Do not alter, bypass, weaken, or suppress release-gate checks to make an
optimization pass.

If the gate fails: - diagnose the failure; - fix the underlying issue if
it belongs to the approved batch; - do not paper over
analyzer/test/DocGen failures; - report unrelated pre-existing failures
distinctly.

A batch is not complete until the required gate passes.

## Definition of done for an optimization batch

A batch is complete only when: - scope stayed within approved
findings; - intended behavior is preserved unless an approved defect was
corrected; - code is simpler/safer/faster for a concrete reason; - tests
appropriate to risk pass; - analyzers/build remain clean; -
documentation/comments are accurate; - `git diff --check` passes; - the
required release gate passes; - no unrelated churn remains; - the final
report identifies exactly what changed and why.

For a source-hygiene/dead-code batch, completion additionally requires:

-   genuinely unused imports introduced or exposed by the batch are
    removed;
-   every removed non-local member has defensible evidence that it was
    dead;
-   uncertain dynamic/API/framework members were retained rather than
    guessed away;
-   public/protected API surface is unchanged unless explicitly
    approved;
-   no test-count decrease remains unexplained;
-   no cleanup-only formatting, modernization, or unrelated refactoring
    remains in the diff.

## Stop conditions

Stop and ask for approval rather than proceeding when: - a public API
must change; - PDF semantics or preservation behavior may change; - a
dependency change is needed; - a security policy/boundary would
change; - tests reveal ambiguous intended behavior; - a broad
architectural rewrite appears necessary; - a performance change requires
unsafe code or a major complexity tradeoff; - a finding belongs to
BF-1054 or another future feature rather than maintenance; - the working
tree contains unexpected user changes that overlap the intended work.

When in doubt, preserve behavior and report the candidate.
