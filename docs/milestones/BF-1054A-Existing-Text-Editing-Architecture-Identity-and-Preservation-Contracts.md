# BF-1054A — Existing Text Editing Architecture, Identity, and Preservation Contracts

| Field | Value |
|---|---|
| **Document ID** | BF-1054A |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1047, BF-1051, BF-1052, BF-1053, BF-1054 |
| **Last Updated** | 2026-08-27 |
| **Next Review** | 2026-11-26 |

## 1. Objective

Define the canonical architecture for editing **existing PDF text** before any production text-replacement implementation begins.

BF-1054A establishes stable editable-text identity, mapping from rendered/display-list text back to source PDF content, authoritative subsystem boundaries, preservation rules, transaction semantics, supported/unsupported edit classes, and characterization-test gates for Rendering semantics consumed by editing.

BF-1054A does **not** implement general-purpose text replacement, reflow, font substitution, or content-stream rewriting.


## 2. Governing Architecture

```text
PDF syntax/object graph
        ↓
Parsing / resource resolution
        ↓
Rendering interpretation
        ↓
Immutable display-list text runs + provenance
        ↓
Existing-text selection / editable-text descriptors
        ↓
Editing transactions
        ↓
Text mutation planning
        ↓
Content/resource projections
        ↓
BF-1053 preservation-aware Writing
        ↓
Serialized PDF
```

Authority rules:

- `PdfDocument` remains authoritative document state.
- Rendering describes interpreted visual/text state; it is not an editable document model.
- Editing owns logical mutation, transaction identity, undo/redo, validation, and ownership.
- Services orchestrates supported edit workflows across subsystem boundaries.
- Writing serializes final authoritative state through BF-1053 preservation-aware planning/projection.


## 3. Core Design Principle

Existing-text editing must operate on **provenance-backed editable targets**, not visual text alone.

The engine distinguishes:

1. visual selection identity — what the user selected;
2. source identity — which PDF content object/operation produced it;
3. editable logical identity — the stable target used by Editing;
4. serialization target — the exact content/resource objects that must change.

A glyph or run may be visually selectable yet not safely editable in place.


## 4. Canonical Existing-Text Identity

Introduce a canonical internal/editing identity concept for existing text.

Conceptual shape:

```text
PdfExistingTextIdentity
    PageId
    PageObjectIdentifier?
    ContentOwnerIdentifier
    ContentStreamIdentifier?
    OperationIndex
    SourceSpan
    RunOrdinal
```

The exact API names may change during implementation, but the identity must:

- remain stable for the editing-session lifetime unless the source content is structurally replaced;
- be unique among editable targets in the session;
- retain both displayed-page identity and source-content owner;
- map to a deterministic source operation/run location;
- exclude screen coordinates, zoom, viewport state, and selection rectangle;
- avoid relying on display-list collection indices when unrelated rendering changes could shift them.

### Source spans

Source spans are preservation-sensitive provenance, but they are not globally unique. A span is meaningful only together with its owning content source/object.

### Form XObjects

For text rendered from a Form XObject, the model must distinguish the page where text is displayed from the Form/content object where text is defined.

If one Form is reused, editing its source may affect every placement. BF-1054 must never silently treat reused Form text as page-local.


## 5. Editable Text Descriptor

BF-1054A should establish a read-only descriptor for discovered/selectable existing text.

Conceptual fields include:

- Identity
- Unicode text
- Encoded bytes
- Font identity
- Font resource name
- Font size
- Text matrix
- Current transformation matrix
- Rendering mode
- Character spacing
- Word spacing
- Horizontal scaling
- Text rise
- Fill/stroke color state
- Source span
- Content owner
- Editability state/reason

Not every field must be public in v1. The internal model must retain enough provenance to validate and later serialize an edit safely.


## 6. Display-List Integration

BF-1047 established display text runs as future editing input. BF-1054A formalizes:

> Display-list text data may be used to discover and present editable targets only when it maps deterministically back to authoritative source content and resource context.

The editable-text layer must not copy the display list into a second authoritative text document model.

### Characterization-before-change rule

When BF-1054 first depends on or changes a Rendering/text interpretation semantic, add focused characterization tests **before** modifying that semantic.

Priority areas:

- `Td`, `TD`, `Tm`, `T*`, `'`, and `"` text-matrix transitions;
- mixed `Tj` / `TJ` advance and spacing;
- text clipping modes 4–7 applied at `ET`;
- `q` / `Q` restoration of text, CTM, color, alpha, and clip state;
- nested Form matrix/resource/marked-content behavior;
- display-item ordering;
- malformed/unbalanced text or graphics state.

Do not refactor unrelated graphics-operator families merely because BF-1054 consumes text runs.


## 7. Text Source Classification

Potential source classes:

### Direct content-stream text

`Tj`, `'`, `"`, and string elements inside `TJ` are potential direct-edit candidates when source mapping, font encoding, and resource ownership are unambiguous.

### Composite `TJ` arrays

A `TJ` array can mix text strings with numeric positioning adjustments. BF-1054 must preserve or intentionally recompute positioning; it must not blindly collapse a positioned array into a `Tj`.

### Form XObject text

Shared source requires explicit shared-impact handling or rejection. A uniquely owned Form may become directly editable in a later slice.

### Type 3 / unusual fonts

Visually selectable text may remain read-only when encoding or glyph behavior cannot be mapped safely.

### Paths/images

Text-shaped paths and images are not existing editable text. OCR/vector reconstruction belongs to separate workflows.


## 8. Editability Classification

Every discovered target must expose an explicit conceptual state such as:

- DirectlyEditable
- EditableWithResourceChange
- EditableWithSharedSourceImpact
- ReadOnlyAmbiguous
- ReadOnlyUnsupportedEncoding
- ReadOnlyUnsupportedFont
- ReadOnlyNonText
- ReadOnlyMalformedSource

Exact enum names may evolve. Unsupported edits must never be silently approximated.


## 9. Preservation Contract

For a direct text edit:

- preserve unrelated content operations and their order;
- preserve unrelated operands;
- preserve unrelated stream dictionary entries;
- preserve unrelated resources;
- preserve indirect-object identity where possible;
- preserve filters/stream representation unless a supported rewrite requires an equivalent transformation;
- preserve page-tree and unrelated document structures;
- preserve unknown/unsupported surrounding operators whenever possible.

Prefer mutation of the smallest authoritative content source containing the edited text.

Do **not** rebuild an entire page content stream from the display list. The display list is interpreted state, not a lossless serialization source.


## 10. Content-Stream Mutation Boundary

Later BF-1054 slices should mutate text through a dedicated content-stream mutation layer:

```text
Original content stream
    ↓
Parsed canonical operations + source spans
    ↓
Target operation identified by provenance-backed identity
    ↓
Validated logical mutation
    ↓
Minimal operation replacement
    ↓
Serialized replacement content stream
```

This layer belongs below Services/UI and above Writing serialization. UI code must never rewrite PDF content streams directly.


## 11. Transaction Semantics

All existing-text changes occur through Editing transactions.

A future text edit operation must retain enough information to:

- validate session ownership;
- identify the source text target;
- describe intended logical mutation;
- undo to prior state;
- redo deterministically;
- project final content/resource changes into Writing.

Future operation families may include replace/delete text; font/size/color changes; character/word spacing; horizontal scaling; text rise; and explicitly supported decorations/styles.

BF-1054A defines contracts only.


## 12. Stale Identity Policy

An edit must fail predictably if its source identity is stale.

Examples:

- the owning content stream was replaced;
- the source operation no longer exists;
- a prior edit invalidated the mapping;
- a shared Form was structurally replaced;
- a resource identity changed incompatibly.

Do not fall back to matching by displayed text or coordinates.

Fuzzy remapping is not an implicit behavior.


## 13. Find / Replace Architecture

Find/replace must use canonical text discovery and editable identity, not raw PDF string search.

A match may span multiple provenance-backed fragments. Replacement is allowed only when all fragments can be transformed through a supported mutation strategy.

Cross-run/cross-operation replacement is explicit and tested; it is not automatically equivalent to substring replacement in one string operand.


## 14. Font Boundary

BF-1054A establishes:

- existing text retains resolved font identity and resource provenance;
- same-font replacement requires encoding/glyph validation;
- font changes require a dedicated font/resource mutation contract;
- missing glyphs must never be silently replaced with wrong glyph codes;
- embedding permissions must be respected;
- substitution must be explicit and deterministic.

A direct same-font replacement is only eligible when the replacement text can be encoded safely in the existing font/resource context and does not require unsupported reflow.


## 15. Layout and Reflow Policy

Initial existing-text editing distinguishes:

- **operand replacement** — change text within the existing positioning model;
- **local adjustment** — bounded spacing/position updates where explicitly supported;
- **reflow** — redistributing text across lines, columns, pages, or unrelated objects.

General reflow is outside the initial direct-edit path unless a later BF-1054 slice explicitly designs it.

If replacement cannot fit the supported positioning model, return an explicit unsupported/overflow result rather than silently moving unrelated content.


## 16. Typography Semantics

Model PDF reality rather than word-processor flags.

Examples:

- font size → `Tf`
- character spacing → `Tc`
- word spacing → `Tw`
- horizontal scaling → `Tz`
- text rise → `Ts`
- fill/stroke colors → graphics/color state
- bold/italic → usually font face/resource selection
- underline → typically additional drawing/content
- superscript/subscript → typically size/rise/position changes

Later APIs must express what can actually be preserved and serialized.


## 17. Resource Mutation Rules

Any edit that changes resource usage must:

- resolve the owning resource scope;
- distinguish page-local and Form-local resources;
- preserve inheritance correctly;
- avoid resource-name collisions;
- allocate new objects through existing Writing allocation contracts;
- preserve unrelated resource entries;
- update only the resource owner that actually requires the change.


## 18. Security Boundary

Existing-text editing must not:

- execute document actions;
- dereference unsafe external targets;
- activate embedded content;
- disable security observations;
- bypass inspection policy;
- trust malformed content/resource structures.

An editable document remains untrusted input.


## 19. Public API Boundary

Do not expose raw parser/operator types as the long-term SDK contract.

Public concepts should be stable editing concepts such as:

- text selections/results;
- editable text descriptors;
- editability status/reasons;
- text edit commands/transactions;
- typography values.

Parser-specific implementation types remain internal unless already established as SDK contracts.


## 20. Suggested BF-1054 Slice Sequence

- **BF-1054A** — Architecture, Identity, and Preservation Contracts
- **BF-1054B** — Existing Text Discovery and Provenance Mapping
- **BF-1054C** — Same-Font Direct Text Replacement
- **BF-1054D** — Composite `TJ` Editing and Position Preservation
- **BF-1054E** — Find/Replace Transaction Integration
- **BF-1054F** — Typography State Editing
- **BF-1054G** — Canonical Font Selection / Resource Mutation Integration
- **BF-1054H** — Decorations and Derived Styles
- **BF-1054I** — Shared Form / Resource Scope Handling
- **BF-1054J** — Public Services/SDK Text Editing Orchestration
- **BF-1054K** — End-to-End Existing-Text Conformance and Hardening

The exact number of slices may contract if adjacent work proves simple. Do not create a slice merely to preserve lettering.


## 21. BF-1054A Implementation Scope

The first implementation slice is limited to contracts and validation infrastructure.

Recommended production additions:

- canonical existing-text identity type;
- explicit editability classification;
- read-only existing-text descriptor;
- internal provenance validation where required;
- ownership/session association only if the established Editing pattern requires it.

Recommended tests:

- identity equality/stability;
- same source span in different content owners is not the same identity;
- viewport/zoom/location do not participate in identity;
- displayed page and Form/source owner remain distinct;
- editability reason is explicit;
- SDK/internal boundaries do not leak parser implementation types;
- characterization tests only for Rendering semantics BF-1054A actually consumes.

BF-1054A must not rewrite content streams.


## 22. Acceptance Criteria

BF-1054A is complete when:

1. Existing text has a canonical provenance-backed identity contract.
2. Displayed page identity and source content owner are modeled separately.
3. Source spans are scoped to their owner.
4. Shared Form text cannot be silently treated as page-local.
5. Editable-text descriptors are read-only descriptions of authoritative interpreted state.
6. Editability classification explains unsupported conditions.
7. Rendering remains immutable and non-authoritative.
8. Editing remains the owner of future logical mutation.
9. BF-1053 Writing remains the serialization path for committed edits.
10. No general reflow is implied.
11. Font/resource mutation is explicitly deferred to dedicated contracts.
12. Unsupported/stale identities fail explicitly rather than using fuzzy matching.
13. Newly relied-upon Rendering semantics have characterization coverage.
14. No parser/operator implementation types are exposed publicly solely for BF-1054.
15. The full PiratePDF release gate passes.


## 23. Out of Scope for BF-1054A

- content-stream rewriting;
- text replacement/deletion;
- find/replace implementation;
- font changing/embedding/substitution;
- general reflow;
- paragraph/column/page layout;
- OCR;
- text extraction redesign;
- unrelated graphics-interpreter decomposition;
- Writing redesign;
- security redesign;
- plugin changes;
- application UI.


## 24. Engineering Standards Applied

- **K.I.S.S.** — establish the minimum durable contracts before mutation.
- **D.R.Y.** — reuse display-list, Editing, resource-resolution, and Writing foundations.
- **Y.A.G.N.I.** — do not implement reflow/font architecture before the direct-edit boundary proves it necessary.
- **Preservation first** — provenance and minimal mutation are mandatory.
- **Zero trust** — ambiguous/malformed source structures remain untrusted.
- **Characterize before semantic change** — add focused Rendering tests at the exact boundary consumed.
- **No cosmetic structural refactor** — do not decompose unrelated graphics/security code solely for line count.


## 25. Closure Decision

After BF-1054A passes the release gate and is committed, BF-1054B should begin from a fresh post-A snapshot and implement **existing-text discovery and provenance mapping**, not mutation.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-27 | Defined existing-text identity, provenance, editability, preservation, transaction, font/resource, and Rendering characterization contracts for BF-1054. |
