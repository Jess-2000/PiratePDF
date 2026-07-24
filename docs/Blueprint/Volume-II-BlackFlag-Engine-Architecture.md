# PiratePDF Master Blueprint - Volume II: BlackFlag Engine Architecture

| Field | Value |
|---|---|
| **Document ID** | PP-ARCH-001 |
| **Category** | Architecture |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-2000 |
| **Last Updated** | 2026-07-24 |
| **Next Review** | 2026-10-24 |

---

## Volume II — BlackFlag Engine Architecture

**Document ID:** PP-BP-VOL2  
**Version:** 1.0  
**Status:** Approved  
**Last Updated:** July 22, 2026

---

## 1. Purpose

This volume defines the approved architecture for BlackFlag Engine, the PDF technology platform shared by PiratePDF Standard and PiratePDF Professional.

BlackFlag Engine must remain independent of Avalonia, PiratePDF UI code, edition licensing, and Word-document import logic.

---

## 2. Architectural Goals

BlackFlag Engine must be:

- UI-independent.
- Cross-platform.
- Modular.
- Testable.
- Suitable for very large PDFs.
- Safe when processing untrusted files.
- Efficient with memory and file I/O.
- Extensible through stable SDK contracts.
- Capable of incremental growth toward professional rendering and editing.

---

## 3. Initial Project Structure

The first implementation phase contains:

- `BlackFlag.Engine.Core`
- `BlackFlag.Engine.Abstractions`
- `BlackFlag.Engine.IO`
- `BlackFlag.Engine.Syntax`
- `BlackFlag.Engine.Document`
- `BlackFlag.Engine.SDK`

Future modules may include:

- `BlackFlag.Engine.Rendering`
- `BlackFlag.Engine.Fonts`
- `BlackFlag.Engine.Images`
- `BlackFlag.Engine.Color`
- `BlackFlag.Engine.Editing`
- `BlackFlag.Engine.Search`
- `BlackFlag.Engine.Security`
- `BlackFlag.Engine.Writer`
- `BlackFlag.Engine.Plugins`

Projects should be added when implementation work begins rather than created as long-term empty placeholders.

---

## 4. Dependency Direction

The intended initial dependency direction is:

```text
BlackFlag.Engine.Core
        ↑
BlackFlag.Engine.Abstractions
        ↑
BlackFlag.Engine.IO
        ↑
BlackFlag.Engine.Syntax
        ↑
BlackFlag.Engine.Document
        ↑
BlackFlag.Engine.SDK
```

Additional direct references are allowed only where justified and documented.

The following are prohibited:

- Circular project references.
- References from BlackFlag projects to PiratePDF projects.
- References from BlackFlag projects to Avalonia.
- References from low-level projects to higher-level document or UI projects.

---

## 5. Core Responsibilities

### 5.1 BlackFlag.Engine.Core

Provides fundamental types and infrastructure that do not depend on PDF parsing or UI code.

Examples:

- Byte ranges.
- PDF object identifiers.
- Rectangles.
- Matrices.
- Version values.
- Diagnostics.
- Error and result types.

### 5.2 BlackFlag.Engine.Abstractions

Provides contracts used across engine modules.

Examples:

- Random-access data sources.
- Document-session contracts.
- Rendering contracts.
- Editing contracts.
- Search contracts.
- Color-service contracts.

Abstractions should remain small and stable. They must not become a dumping ground for unrelated interfaces.

### 5.3 BlackFlag.Engine.IO

Provides concrete binary-source and storage implementations.

Responsibilities include:

- Random-access file reads.
- Asynchronous reads.
- 64-bit file offsets.
- Large-file support.
- Temporary storage.
- Buffer pooling.
- Future memory-mapped sources where justified.

### 5.4 BlackFlag.Engine.Syntax

Interprets PDF lexical and syntactic structures.

Responsibilities include:

- Tokenization.
- Numbers.
- Names.
- Strings.
- Arrays.
- Dictionaries.
- Indirect references.
- Streams.
- Cross-reference tables.
- Cross-reference streams.
- Basic stream filters.

### 5.5 BlackFlag.Engine.Document

Builds higher-level PDF concepts from syntax objects.

Responsibilities include:

- Document catalog.
- Page tree.
- Page references.
- Resource inheritance.
- Page boxes.
- Metadata.
- Bookmarks.
- Annotations.
- Content-stream ownership.

### 5.6 BlackFlag.Engine.SDK

Exposes stable application-facing APIs.

PiratePDF should consume the SDK rather than directly depending on parser internals.

---

## 6. Large-Document Architecture

BlackFlag Engine must support PDFs containing at least 6,000 pages.

The engine must not:

- Read the complete file into one byte array.
- Parse every page eagerly.
- Decode every image at open time.
- Render every thumbnail at open time.
- Create a complete editable model for every page immediately.

The engine should:

- Open the file through a random-access source.
- Read the header, trailer, and cross-reference structures.
- Build a lightweight page index.
- Parse pages on demand.
- Cache only bounded working sets.
- Cancel obsolete work.
- release parsed and rendered resources when no longer needed.

---

## 7. Document Session Model

A document session should coordinate:

- Source file access.
- Parsed-object cache.
- Page index.
- Edit journal.
- Undo and redo.
- Render cache.
- Background tasks.
- Save state.
- Recovery state.

The session should not expose mutable parser internals directly to the UI.

---

## 8. Threading and Scheduling

The engine should support background work with explicit priorities.

Suggested priorities:

- Interactive.
- Visible.
- Prefetch.
- Background.

Examples:

- Visible page rendering is high priority.
- Nearby-page prefetch is medium priority.
- Whole-document analysis is low priority.

Long-running operations must support cancellation.

Shared random-access reads must not depend on one mutable stream position.

---

## 9. Memory Management

The engine should use bounded caches for:

- Parsed objects.
- Rendered tiles.
- Thumbnails.
- Decoded images.
- Fonts.
- Content streams.
- Search indexes.

Cache budgets should be measured by estimated memory cost, not only object count.

Large temporary edit or undo data may use disk-backed storage.

---

## 10. Editing Architecture

Editing should use commands and transactions.

A major edit should:

- Validate the target.
- Record the previous state.
- Apply the change.
- Invalidate affected caches.
- Create one user-visible undo entry.
- Remain recoverable until saved.

Replace-all operations may contain thousands of internal changes while appearing as one undo action.

---

## 11. Save Architecture

BlackFlag Engine should support:

### Incremental save

Append changed and new PDF objects.

Use when:

- Changes are compatible with incremental update.
- Fast saving is preferred.
- Secure removal is not required.

### Full rewrite

Create a complete new PDF.

Use when:

- Sanitization or secure removal is required.
- Major structural changes occur.
- Whole-document conversion occurs.
- Optimization is requested.

Full saves should write to a temporary file, validate key structures, and replace the destination atomically where possible.

---

## 12. Rendering Architecture

The rendering pipeline will eventually include:

```text
Random-access source
→ Syntax parser
→ Document model
→ Content interpreter
→ Graphics state
→ Fonts and images
→ Raster/vector surface
→ Render cache
```

Rendering should support:

- Tiled or page-based work.
- Cancellation.
- Zoom-specific caching.
- Rotation.
- Color-profile-aware output.
- Transparent editing overlays above rendered page content.

---

## 13. Security Principles

Every PDF is untrusted input.

The engine must:

- Validate offsets and lengths.
- Limit recursive structures.
- Guard against integer overflow.
- Bound decompression.
- Avoid uncontrolled allocation.
- Reject invalid ownership transitions.
- Isolate native code.
- Fuzz high-risk parsers when practical.

Embedded JavaScript is not part of the initial product scope.

---

## 14. SDK Principles

The SDK should expose:

- Stable interfaces.
- Immutable data where practical.
- Cancellation tokens.
- Structured diagnostics.
- Versioned capabilities.
- No direct parser implementation types.

Breaking SDK changes require versioning and migration notes.

---

## 15. Architecture Acceptance Criteria

This architecture is correctly implemented when:

- All current engine projects build without Avalonia.
- `BlackFlag.Engine.IO` can read independent offsets concurrently.
- Large-file offsets use 64-bit values.
- The parser can operate without loading an entire file.
- PiratePDF can consume engine capabilities through SDK boundaries.
- Unit tests cover public behavior.
- Dependency rules remain acyclic.
- Documentation and ADRs match the implemented structure.

---

## 16. Revision History

| Version | Date | Summary |
|---|---|---|
| 1.0 | July 22, 2026 | Initial authoritative Markdown architecture. |
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized document metadata and Blueprint structure. |
