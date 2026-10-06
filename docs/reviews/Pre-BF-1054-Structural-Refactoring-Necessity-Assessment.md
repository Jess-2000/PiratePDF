# Pre-BF-1054 Structural-Refactoring Necessity Assessment

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-008 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1047, BF-1049, BF-1054 |
| **Last Updated** | 2026-08-26 |
| **Next Review** | 2026-11-26 |

## Executive summary

This assessment re-evaluates two Phase A maintainability findings without assuming that file size requires a refactor:

- `AUD-P2-001 — graphics-interpreter decomposition`;
- `AUD-P2-002 — security/parser type decomposition`.

The 600–800-line range and the 800-line review point are decomposition-review triggers, not automatic decomposition requirements. Applying K.I.S.S., Y.A.G.N.I., current feature pressure, regression coverage, and semantic risk leads to this result:

| Finding/type | Classification |
|---|---|
| `PdfGraphicsInterpreter.InterpretCore` | **DEFER UNTIL NATURAL FEATURE PRESSURE** |
| `OoxmlWordContainerSecurityAdapter` | **DEFER UNTIL NATURAL FEATURE PRESSURE** |
| `OleCompoundFileSecurityAdapter` | **DEFER UNTIL NATURAL FEATURE PRESSURE** |
| `PdfSecurityAnalyzer.AnalysisState` | **DEFER UNTIL NATURAL FEATURE PRESSURE** |

The files impose real review and navigation costs, and the OLE adapter contains the clearest separable responsibility. However, no current correctness, preservation, security, or reliability defect is caused by their physical structure. Three proposed decompositions would mostly redistribute private code without improving independent testability; the fourth would move a security-sensitive binary reader across a boundary before its hostile-input behavior is adequately characterized.

The recommended pre-BF-1054 action is therefore **option 5: defer both findings and proceed to BF-1054**. Graphics characterization should be added when BF-1054 first changes or depends on the relevant text/display-list semantics. Security characterization should precede any later decomposition in its owning security work, not delay BF-1054 now.

## Baseline and scope

Before assessment:

- Batch 3 was committed at `fdcf7c6` (`Strengthen abstractions contract tests`).
- The branch was `maintenance/pre-BF-1054-repository-optimization`.
- The working tree was clean.

No production code, test code, project file, dependency, or generated file was modified during this assessment. The only intended output is this document.

Sources of truth included:

- `AGENTS.md` and the Phase A audit;
- the four affected source files and their partial/type siblings;
- the complete directly relevant Rendering, Security, and Parsing tests;
- the approved engineering standards;
- BF-1045, BF-1046, BF-1047, BF-1049B, BF-1049D, and BF-1049F milestone contracts;
- ADR-0011 passive-document conversion and the documented BlackFlag architecture.

No detailed BF-1054 milestone specification exists in the repository at this checkpoint. BF-1054 interaction statements below are therefore bounded inferences from `AGENTS.md` and BF-1047's explicit existing-text-editing readiness contract.

## Governing decomposition rule

The approved engineering standard states:

- 1,000 lines is a hard ceiling;
- approximately 800 lines is a mandatory decomposition review point;
- approximately 500–800 lines is an acceptable upper range for larger cohesive files;
- splits must follow a domain or responsibility boundary;
- partial files must not merely hide excessive responsibility.

Accordingly, line count establishes the need for this review. It does not establish that any code should move. A safe recommendation must identify a concrete improvement in cohesion, testability, reliability, or maintainability that outweighs the risk of changing ordering-sensitive graphics or hostile-input security code.

## AUD-P2-001 — graphics-interpreter decomposition

### Current structure and line count

| Item | Evidence |
|---|---|
| File | `src/BlackFlag.Engine.Rendering/Interpretation/PdfGraphicsInterpreter.Execution.cs` |
| Current length | 937 lines |
| Primary method | `PdfGraphicsInterpreter.InterpretCore`, lines 11–936 |
| Dispatch size | 77 `case` labels, including grouped operator aliases |
| Local helpers | Nine: path painting/end/clip, text run/array, XObject/Form, marked content, display-item addition |
| Existing decomposition | The owning type is already partial, with separate operands, paths, text, graphics-state, facade, and execution files. |

`InterpretCore` owns one semantic operation: ordered interpretation of canonical PDF graphics operations into an immutable display list. Within that state machine it coordinates:

- graphics-state save/restore and current transformation;
- line/dash/rendering-intent/ExtGState mutation;
- path construction, painting, pending clipping, and clip depth;
- text-object lifecycle, text state, text matrices, text runs, TJ adjustments, and text clipping;
- fill/stroke color spaces and components;
- image, Form XObject, and shading items;
- Form recursion/depth, inherited state, bounds, resources, and active-form cleanup;
- marked-content nesting and marked-content points;
- per-operation exception normalization and end-of-stream balance validation;
- display-item, path, state, Form, clip, and marked-content limits.

### Mixed responsibility or cohesive domain complexity

The method contains multiple operator families, but they participate in one order-dependent interpreter state. The shared locals are not accidental global state: `state`, `stateStack`, `path`, `pendingClip`, `pendingTextClipRuns`, `inTextObject`, `markedContent`, `items`, resource scope, and active Form identities are the actual PDF graphics-machine state.

This is primarily cohesive domain complexity rather than unrelated responsibility. The existing partial files already separate pure operand parsing, path primitives, text calculations, and graphics-state helpers. What remains in `Execution.cs` is orchestration whose sequencing is itself the behavior.

### Concrete maintainability defects

Material costs do exist:

- The file is only 63 lines below the hard ceiling, so new execution behavior cannot safely accumulate in it.
- A single method creates a large merge and review surface for future operator work.
- Operator-family navigation is slower than in a smaller dispatcher.
- Nine local functions capture broad interpreter state, making individual handlers inaccessible to direct unit tests.
- Reviewers must reason across distant dispatch cases and local helpers when a state transition spans operator families.

No concrete duplication, dead branch, stale abstraction, or inconsistent error contract was found in this file. The central switch makes source order and exception wrapping explicit, and the existing helper partials avoid repeating operand/state algorithms.

### Coupling and testability

State sharing is high but semantically necessary. For example:

- a `W`/`W*` operator defers clipping until a later path-ending operator;
- text rendering modes accumulate clipping runs until `ET`;
- `q`/`Q` governs subsequent path, text, color, XObject, and clip state;
- Form interpretation inherits current state, adds a bounding-box clip, changes resource scope, and participates in recursion tracking;
- marked-content ancestry must be attached to items from every operator family.

Moving cases into static helpers would require numerous `ref` parameters. Moving them into an execution-context object would relocate the same shared mutable state and introduce a new internal abstraction. Partial methods would divide navigation but would not make behavior independently testable because correctness depends on operation sequences and final display-list output.

Decomposition can improve review locality when new operator behavior is actively changing, but it does not create meaningful unit seams by itself. End-to-end operation-sequence tests remain the correct regression level.

### Existing coverage

`PdfGraphicsInterpreterTests.cs` is 321 lines with nine tests. It currently covers:

- transformed path construction;
- `q`/`Q` restoration of line width;
- basic `Tj` text-run creation;
- ordered `TJ` text and adjustments;
- image XObject placement;
- inline-image placement;
- pending path clipping;
- marked-content attachment;
- deterministic recursive-Form rejection.

Rendering and Skia suites also protect downstream display-list consumption, but only this nine-test suite directly exercises `InterpretCore`.

Important sequence-level semantics are not directly characterized:

- `Td`, `TD`, `Tm`, `T*`, single-quote, and double-quote text-matrix/spacing transitions;
- text rendering modes 4–7 and clip application at `ET`;
- nested Forms combining caller state, Form matrix, bounding-box clip, nested resources, and marked content;
- graphics-state restoration across CTM, colors, alpha/ExtGState, clips, and text state;
- exact failure behavior for unbalanced graphics state, unterminated text/marked content, malformed operands, and unknown operators;
- display-item ordering across mixed path/text/image/Form/marked-content sequences;
- empty-path painting and pending-clip reset behavior.

### Decomposition benefit and estimated scope

A safe structural split would likely require:

- one private execution-state/context type;
- one dispatcher/orchestration file;
- cohesive handler files for general/path state, text, resources/Forms, and marked content;
- adjustments to existing helper ownership.

Estimated scope is five to seven production files or partial files plus characterization-test changes. Merely moving the switch into numbered partial files is explicitly rejected because it would only redistribute code.

The likely benefit is improved navigation, merge isolation, and operator-family review. The likely testability improvement is small unless handlers become independently callable, which would enlarge the internal API and obscure whole-sequence semantics.

### Semantic and regression risk

Risk is medium to high because a mechanical extraction can alter:

- operation order and when state snapshots are captured;
- `q`/`Q` value semantics and stack balance;
- when pending path/text clips become active or are cleared;
- text matrix advancement and TJ adjustment order;
- item source spans and marked-content ancestry;
- Form matrix multiplication order, inherited resources, clip ancestry, recursion detection, and `finally` cleanup;
- limit-consumption points and exception classification;
- behavior after a no-op, ignored, or malformed operation.

These are rendering and future editing semantics, not formatting details.

### BF-1054 interaction

BF-1047 explicitly states that display text runs retain font identity, encoded bytes, text state, spacing adjustments, source spans, and transforms for future existing-text editing. BF-1054 is therefore likely to consume `PdfDisplayTextRun` output and may expose gaps in text positioning or source mapping.

It is not yet established that BF-1054 must modify `InterpretCore`. Editing can consume the existing immutable display list and modify content through later Editing/Writing boundaries. With no BF-1054 implementation contract in the repository, restructuring the interpreter preemptively would be speculative.

If BF-1054 requires changes to text-run construction or matrix advancement, that is the natural pressure point to extract only the affected text-execution responsibility, backed by feature characterization. Refactoring the whole interpreter beforehand would increase the pre-feature regression surface and create avoidable merge churn.

### Recommendation

**DEFER UNTIL NATURAL FEATURE PRESSURE.**

The mandatory decomposition review is satisfied by this assessment. The current file is large and costly to navigate, but it is cohesive, has no demonstrated structural defect affecting behavior, and would require a broad state-holder refactor to split safely. Do not add further unrelated responsibilities to `Execution.cs`; re-open decomposition when BF-1054 or later graphics work must materially change an operator family or when the file would otherwise approach the 1,000-line hard limit.

## AUD-P2-002 — security/parser type decomposition

### `OoxmlWordContainerSecurityAdapter`

#### Current structure and responsibilities

| Item | Evidence |
|---|---|
| File | `src/BlackFlag.Engine.Security/Formats/Ooxml/OoxmlWordContainerSecurityAdapter.cs` |
| Current length | 796 lines |
| Public entry points | `CanInspect`, `Inspect` |
| Private operations | Archive iteration, bounded entry reads, XML parsing, relationship/content-type/field classification, part/path/media-type classification, observation de-duplication |

The adapter:

- identifies Word OOXML packages;
- opens and iterates the ZIP package;
- enforces archive-entry, compressed/expanded-size, compression-ratio, shared-budget, work, and cancellation limits;
- recognizes suspicious paths, VBA, ActiveX, and embedded payload parts;
- reads embedded children with pre-decompression budget reservation;
- reads bounded XML parts;
- classifies relationships, content types, and DDE/DDEAUTO field instructions;
- creates stable logical paths and de-duplicated normalized observations.

#### Cohesion, defects, duplication, and coupling

There are distinct archive-transport and XML-classification concerns, but both are format-specific parts of one passive OOXML adapter. The `Inspect` loop intentionally determines observation and child order, budget-consumption order, and which entries stop further classification through `continue` statements.

Some ZIP/XML/path mechanics resemble `OpenDocumentTextContainerSecurityAdapter`: entry reading, length validation, compression checks, XML reader settings, read-only streams, path normalization, and logical-path composition. This is real duplication. It is not automatically a safe common abstraction because OOXML and ODT have different part recognition, XML limits, budget timing, suspicious-path inputs, and embedded-child rules. The Phase A audit already rejected consolidating format adapters into a new framework.

Concrete maintainability costs are file navigation and the proximity of low-level reads to higher-level classification. No evidence showed inconsistent duplicate behavior causing a defect, unbounded input, hidden external side effect, or difficult-to-reproduce state mutation.

The current private static XML classifiers are already individually cohesive. Extracting them to separate internal types would improve physical organization but not testability unless they were exposed internally or tested through new seams. End-to-end package tests would still be needed to prove ordering, paths, budgets, and recursive-child behavior.

#### Existing coverage

The 382-line OOXML test suite contains ten tests covering clean DOCX, VBA, external relationships, DDE, ActiveX, embedded executable classification, nested DOCM recursion, compression-ratio enforcement, format rejection, and cancellation. Shared embedded-inspection tests cover root-container and pre-accounted-child integration described by BF-1049D.

Coverage is strong at the public analyzer boundary but does not fully characterize exact observation order/de-duplication, suspicious path reporting, archive-entry/XML-size limits, truncated declared entry behavior, logical-path construction, or simultaneous relationship/content-type/part detections.

#### Decomposition benefit, scope, and risk

A meaningful split would likely require three or four production files/types:

- the adapter/orchestrator;
- bounded OOXML package-entry reading;
- relationship/content-type XML classification;
- Word-field classification and/or part rules.

This could improve review locality, but independent testability would remain limited without increasing internal surface. Risks include changing entry/observation order, de-duplication keys, `continue` behavior, budget charge timing, decompression-before-validation behavior, logical paths, XML hardening, cancellation points, and recursive-child accounting.

BF-1054 existing PDF text editing has no identified dependency on OOXML passive security inspection.

#### Recommendation

**DEFER UNTIL NATURAL FEATURE PRESSURE.**

The adapter is at the review threshold but remains a cohesive format boundary. Split only when new OOXML security behavior makes a specific classifier or package reader change independently. Do not create a cross-format archive framework solely to reduce length.

### `OleCompoundFileSecurityAdapter`

#### Current structure and responsibilities

| Item | Evidence |
|---|---|
| File | `src/BlackFlag.Engine.Security/Formats/Ole/OleCompoundFileSecurityAdapter.cs` |
| Current length | 732 lines |
| Adapter/classification portion | Approximately lines 1–306 |
| Nested compound-file model/reader | Approximately lines 307–731; about 425 lines |

The outer adapter owns passive security policy facts: macro/ActiveX/embedded-entry classification, bounded Word stream scanning for DDE/external references, embedded-payload signature precedence, observation paths/de-duplication, child creation, shared-budget work, and cancellation.

The nested `CompoundFileReader` owns a separate low-level binary responsibility: CFB signature/geometry validation, DIFAT/FAT construction, directory parsing, regular-sector chains, mini-FAT/mini-stream chains, cycle/marker/bounds validation, stream materialization, and declared-length enforcement.

#### Cohesion, defects, duplication, and coupling

This is the clearest genuinely mixed-responsibility file in the assessment. A bounded CFB reader is conceptually independent from security classification, and moving it to a dedicated internal type would materially improve audit locality.

The coupling is nevertheless concrete:

- the reader uses adapter-owned limits and private entry types;
- stream materialization policy is chosen by the outer classifier;
- entry order becomes observation order;
- cancellation is checked at adapter iteration and stream-read entry, not throughout every internal chain loop;
- malformed-input messages and exact failure points are observable through the public analyzer.

No duplicated CFB reader elsewhere in the repository was found, and no current defect was identified. Extraction would improve cohesion but not automatically improve tests because the reader is private and current coverage reaches it only through the legacy Word analyzer.

#### Existing coverage

The direct legacy Word/OLE test suite is 148 lines with three tests:

- non-DOC filename rejection;
- invalid signed compound-file rejection;
- macro storage detection in one valid synthetic CFB fixture.

BF-1049F documents more behavior than these direct tests characterize: FAT/DIFAT cycles, directory limits, regular and mini streams, stream limits, DDE/external scans, payload signature priority, malformed sector references, and cancellation. Broader shared classifier/adversarial tests do not directly exercise most `CompoundFileReader` branches.

This coverage is adequate for the current happy path and a coarse malformed boundary, but inadequate as a safety net for moving the reader.

#### Decomposition benefit, scope, and risk

A natural future split is smaller than the other candidates: two or three production files/types for the adapter, internal compound-file reader, and its entry/enum model. That would improve cohesion and make a deliberate internal test seam possible.

Risks include changing sector arithmetic, FAT/DIFAT traversal order, cycle detection, mini-stream selection, truncation behavior, allocation limits, entry order, cancellation timing, exception messages/types, and payload bytes supplied to recursive classification. Because input is untrusted, even an organization-only change must not weaken a validation check or move allocation before validation.

BF-1054 has no identified dependency on legacy OLE parsing or passive-conversion security.

#### Recommendation

**DEFER UNTIL NATURAL FEATURE PRESSURE.**

Of the assessed types, this has the strongest eventual decomposition case. It still should not be moved before BF-1054 solely for size: no defect or current feature pressure exists, and the reader's hostile-input coverage is not yet sufficient for a low-risk extraction. Re-open when OLE behavior changes, when direct parser reuse is concretely required, or when a dedicated security-hardening batch first adds the missing characterization.

### `PdfSecurityAnalyzer.AnalysisState`

#### Current structure and responsibilities

| Item | Evidence |
|---|---|
| File | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.AnalysisState.cs` |
| Current length | 763 lines |
| Type | Private nested `AnalysisState` inside partial `PdfSecurityAnalyzer` |
| Shared state | Resolver, limits/options, cancellation, visited object IDs, observation keys/list, resolved-object count |

The type owns:

- bounded recursive traversal of dictionaries, arrays, streams, and indirect references;
- cycle prevention, resolution limits, depth/work budgets, and unresolved-reference observations;
- propagation of semantic visit context such as open/additional actions and name trees;
- action, file-specification, annotation, AcroForm, XFA, and presence classification;
- automatic-action derivation;
- observation construction, source locations, property dictionaries, de-duplication, order, and budget charging;
- PDF name/string helper interpretation.

#### Cohesion, defects, duplication, and coupling

Traversal and classification are separable concepts, but this analyzer deliberately classifies while walking so logical paths, indirect-object identity, visit context, observation order, de-duplication, and work/observation budgets share one source of truth.

The action switch and annotation classifier are sizeable and navigationally separate from traversal. That is a maintainability cost. No repeated object traversal, duplicate policy engine, parser-type leak, or contradictory helper implementation was found.

A partial-file split of the private nested class would reduce physical file length but would not reduce responsibility or improve testability. Extracting a classifier object would require a context and observation sink carrying paths, source IDs, visit context, de-duplication, and budget semantics. That new abstraction would be justified only if another consumer or independently changing classifier family appears.

#### Existing coverage

The 298-line direct test suite contains eight tests covering:

- immutable analysis-result observations;
- automatic JavaScript and silent policy integration;
- embedded-file name trees and file specifications;
- external URI and Launch actions;
- AcroForm, XFA, and interactive annotations;
- cyclic indirect references;
- unresolved references;
- clean-catalog/no-report behavior.

Coverage spans the principal public behaviors but does not fully characterize exact observation ordering/de-duplication, all action and annotation variants, JavaScript name trees, source-object/logical-path propagation through nested indirect structures, resolved-object/depth/work/observation limits, cancellation timing, or stream-dictionary traversal.

#### Decomposition benefit, scope, and risk

A responsibility-based decomposition would likely require three or four production files or nested types:

- traversal/context propagation;
- action and automatic-action classification;
- file/annotation/presence classification;
- observation construction/de-duplication.

The main benefit would be review/navigation locality. Testability would not improve unless private behavior became internal or tests continued through the same public analyzer, in which case code was only redistributed.

Risks include traversal/observation order, cycle handling, depth and object counts, budget-consumption points, duplicate suppression, visit-context propagation, source-object identity, logical paths, policy categories/messages/properties, and malformed-input behavior.

BF-1054 is expected to edit existing PDF text but has no identified need to change active-content security classification. The analyzer may remain part of document-opening security, but that runtime relationship does not make its internal structure a BF-1054 prerequisite.

#### Recommendation

**DEFER UNTIL NATURAL FEATURE PRESSURE.**

The type is large but cohesive around one stateful graph walk. Split only when a classifier family changes independently or a concrete test/reuse boundary emerges. A partial-file-only split is not recommended.

## Cross-file architecture assessment

No dependency-direction violation or public API leakage was found in these types. Existing architecture boundaries remain appropriate:

- Parsing produces canonical graphics operations; Rendering interprets them; Editing/Writing own future mutation and persistence.
- Format security adapters extract facts and children; the shared Security engine owns recursion budgets and policy.
- PDF Parsing security inspects immutable object structure and emits normalized Security observations without implementing policy.

The assessed files do not duplicate a responsibility that already has an obvious reusable owner. OOXML/ODT archive similarities should not be promoted into a framework without a proven common invariant. OLE's private reader is a future extraction candidate, not a currently duplicated service. Graphics and PDF security state are intentionally centralized to preserve ordering.

## Characterization tests even if production refactoring is deferred

### Graphics interpreter

Additional characterization is worthwhile because BF-1054 is likely to consume text-run state even if it does not modify the interpreter. The highest-value tests are:

- text matrix and line-matrix transitions across `Td`, `TD`, `Tm`, `T*`, `'`, and `"`;
- text spacing/advance across mixed `Tj`/`TJ` runs;
- text clipping modes applied at `ET` and inherited by later items;
- nested Form matrix, inherited graphics state, bounding clip, resource scope, marked-content ancestry, and output order;
- `q`/`Q` restoration across more than line width;
- deterministic end-state and malformed-operation failures.

These should be added when BF-1054 first depends on or changes the corresponding semantics, so the tests characterize the exact feature boundary before implementation. A standalone pre-BF-1054 test batch is not required merely to justify a deferred whole-interpreter refactor.

### OOXML security

Additional tests would be worthwhile before any adapter split or new OOXML security feature:

- exact observation/child ordering and duplicate suppression when the same VBA/ActiveX fact appears through part, relationship, and content-type evidence;
- suspicious normalized path behavior;
- archive-entry and XML-part size limits;
- logical paths and pre-accounted child budget state;
- truncated/mismatched entry failure behavior.

Existing public-boundary coverage is sufficient to defer both production and test changes until that pressure exists.

### OLE security

Additional characterization is strongly worthwhile before extracting `CompoundFileReader`:

- regular FAT, DIFAT, mini-FAT, and mini-stream success paths;
- cycles, invalid markers, invalid sectors, truncated chains, and declared-length mismatch;
- directory and stream limits;
- ASCII/UTF-16 DDE and external-reference detection;
- embedded-payload signature priority and exact child bytes/path;
- cancellation and exception type/message stability where contractually relevant.

These tests have independent hostile-input value, but no evidence makes them a BF-1054 prerequisite. Schedule them with future OLE/security hardening or immediately before reader extraction.

### PDF security analysis

Additional tests would be worthwhile before classifier/traversal decomposition:

- exact observation ordering and de-duplication;
- all supported action and annotation families;
- JavaScript name-tree context;
- nested indirect source identity and logical paths;
- object/depth/work/observation limits and cancellation;
- stream-dictionary traversal.

These belong with future PDF security work, not BF-1054 text editing.

## Decision matrix

| Candidate | Concrete structural benefit | Current defect caused by structure | BF-1054 pressure | Regression risk | Decision |
|---|---|---|---|---|---|
| Graphics interpreter | Better operator-family navigation and merge locality | Review surface; near hard ceiling, but no behavioral defect | Possible consumer/change point, not established | Medium–high | Defer until a specific operator family changes |
| OOXML adapter | Separates archive transport from XML classifiers | Navigation and some cross-format duplication only | None identified | Medium | Defer |
| OLE adapter | Clean separation of binary reader and policy classification | Mixed responsibility and weak reader test seam, but no known defect | None identified | Medium–high for hostile input | Defer; strongest future candidate |
| PDF security state | Better traversal/classifier navigation | Large private state owner, no known defect | None identified | Medium | Defer |

## Rejected pre-BF-1054 approaches

- Splitting files solely to move below a line threshold.
- Numbered partial files or partial files that leave the same monolithic responsibility hidden.
- A generic graphics-operator framework around a single state machine.
- Static graphics handlers with broad `ref` parameter lists.
- A new cross-format ZIP/XML security framework based only on OOXML/ODT similarity.
- Making private security helpers internal solely so extracted code can have direct tests.
- Extracting the OLE reader before characterizing hostile chain and mini-stream behavior.
- Separating PDF traversal from classification through a new event/visitor layer without a second concrete consumer.
- Combining either structural finding with a P1 performance candidate or BF-1054 feature implementation.

## Recommended pre-BF-1054 action

**5. Defer both findings and proceed to BF-1054.**

Rationale:

1. The engineering-standard review obligation has been met; line count alone does not authorize decomposition.
2. None of the four structures is causing a demonstrated correctness, preservation, security, reliability, or public-contract defect.
3. Graphics and PDF security are stateful ordered machines whose broad extraction would mostly relocate coupling.
4. OOXML's prospective split offers modest navigation benefit and no current independent seam.
5. OLE has a real future extraction boundary, but it is unrelated to BF-1054 and lacks the characterization needed for safe movement.
6. Preemptive decomposition would place higher-priority graphics/security semantics at risk for maintainability benefit that is not currently demanded.
7. BF-1054 should add focused characterization before changing any graphics/text interpretation semantic it actually touches; it need not refactor unrelated operator families first.

This recommendation is not a permanent rejection. Re-open `AUD-P2-001` when a concrete graphics operator/text-state change would otherwise expand the 937-line execution file or create active merge contention. Re-open each `AUD-P2-002` candidate independently when its format/parser behavior changes, after adding the specified characterization tests.

## Validation and final status

This assessment used read-only source, test, contract, and Git inspection. No build or test run was necessary because no executable file changed and current Batch 3 acceptance is already committed.

Validation commands:

```powershell
git status --short --branch
git log -3 --oneline --decorate
rg -n -C 10 "AUD-P2-001|AUD-P2-002" docs/reviews/Pre-BF-1054-CSharp-Repository-Audit.md
rg --files <affected source and test directories>
rg -n <affected symbols and test references>
git diff --check
git status --short
```

The final working-tree diff must contain only this assessment document.

## Revision history

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-26 | Assessed whether AUD-P2-001 and AUD-P2-002 require implementation before BF-1054. |
