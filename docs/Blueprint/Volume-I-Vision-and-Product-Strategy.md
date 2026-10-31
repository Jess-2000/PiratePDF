# PiratePDF Master Blueprint - Volume I: Vision and Product Strategy

| Field | Value |
|---|---|
| **Document ID** | PP-VSN-001 |
| **Category** | Vision |
| **Version** | 1.2 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-8000 |
| **Last Updated** | 2026-10-31 |
| **Next Review** | 2027-01-31 |

---

## Volume I â€” Vision and Product Strategy

**Document ID:** PP-BP-VOL1  
**Version:** 1.2
**Status:** Approved  
**Last Updated:** October 31, 2026
**Owners:** PiratePDF Project

---

## 1. Purpose

This volume defines the product vision, strategic direction, product model, user groups, core principles, committed capabilities, and long-term success criteria for PiratePDF and the BlackFlag Engine.

This document is the primary source of truth for product-level decisions. Technical implementation details belong in the BlackFlag Engine Technical Specification and subsystem specifications.

---

## 2. Product Summary

PiratePDF is a desktop PDF viewing, editing, inspection, conversion, and production platform powered by BlackFlag Engine.

PiratePDF is intended to provide:

- Reliable PDF viewing and editing.
- Object-level text, image, vector, and color manipulation.
- Strong performance with very large documents, including files containing 6,000 pages or more.
- Direct editing workflows that remain understandable to nontechnical users.
- Professional color-management and production capabilities.
- Minimal third-party licensing friction.
- A modular plugin and SDK architecture.
- One complete free and open-source product with the full capability set available to every user.
- Clear, searchable, version-controlled engineering documentation.

---

## 3. Mission

PiratePDF exists to make complex PDF documents understandable, editable, and dependable without hiding important document details from the user.

The project will prioritize:

1. Document integrity.
2. User control.
3. Performance.
4. Transparency.
5. Accessibility.
6. Extensibility.
7. Long-term maintainability.
8. Minimal licensing friction.
9. Clear documentation.
10. Professional reliability.

---

## 4. Product Vision

PiratePDF should become a credible desktop alternative to established professional PDF editors while developing its own identity.

Its defining strengths should be:

### 4.1 Transparent document inspection

Users should be able to understand what is inside a PDF through tools such as:

- Document Inspector.
- Color Information.
- Color Inspector.
- Color Usage Explorer.
- Document Health.
- Document Statistics.
- Font information.
- Image information.
- Output-intent information.
- Security and metadata inspection.

### 4.2 Object-level editing

Users should be able to select and edit:

- Text.
- Images.
- Vector objects.
- Annotations.
- Colors.
- Page content.
- Repeated text or image instances.

### 4.3 Large-document reliability

PiratePDF must support very large PDFs.

The architecture must support:

- Documents containing 6,000 pages or more.
- Lazy loading.
- Virtualized page controls.
- Bounded caches.
- Background indexing.
- Progressive search.
- Transactional editing.
- Incremental saving.
- Crash recovery.
- Memory-bounded batch operations.

### 4.4 Professional color capabilities

PiratePDF should make sophisticated color workflows understandable and accessible.

Core capabilities should include:

- RGB profile selection.
- CMYK profile selection.
- ICC profile selection.
- Grayscale conversion.
- Monotone colorization.
- Interactive color mapping.
- Color inspection within images.
- Replace-color workflows.
- Conversion previews.

Advanced production capabilities include:

- Color separation preview.
- Advanced production color reports.
- DeviceN and spot-color workflows.
- Advanced output-intent management.
- Batch color conversion.

### 4.5 Conversion to PDF

The user-facing conversion workflow should use clear language:

> **File â†’ Convert to PDFâ€¦**

Initial supported source formats:

- Microsoft Word `.docx`.
- Legacy Microsoft Word `.doc`.

Converted files should:

- Open immediately as editable PDFs.
- Leave the original Word file unchanged.
- Preserve text, images, tables, styles, and emoji where supported.
- Produce a conversion report.
- Warn users when content is substituted, approximated, flattened, or skipped.

---

## 5. Product Model and Open-Source Policy

PiratePDF has one complete edition with one codebase and one BlackFlag Engine.

All implemented functionality is part of PiratePDF. Advanced production,
prepress, batch, automation, integration, reporting, and organizational
capabilities are not reserved for a paid or premium tier.

The product shall be free and open source. The project shall not use
commercial feature tiers, subscription-gated functionality, entitlement
checks, or deliberately reduced product variants.

The exact open-source license is a separate explicit governance decision and
is not selected by this Blueprint revision.

### 5.1 Complete capability set

The single PiratePDF product is intended for individuals, businesses,
educational users, designers, office users, commercial print operators,
prepress professionals, production departments, and high-volume document
workflows.

Committed capabilities include:

- Open and view PDFs.
- Edit supported PDF content.
- Manage pages.
- Select text, images, and objects.
- Delete selected content.
- Undo and redo.
- Search and replace.
- Replace matching image instances.
- Annotations and markup.
- Document Inspector.
- Color Information.
- Color Inspector.
- Color Usage Explorer.
- RGB, CMYK, and ICC profile selection.
- Grayscale, monotone, and mapped color workflows.
- Rulers and guidelines.
- DOC and DOCX conversion to editable PDF.
- Emoji preservation and editing support.
- Plugin support.
- Support for PDFs containing 6,000 pages or more.
- FusionPro workflow integration where technically and legally possible.
- Batch PDF processing and document conversion.
- Batch text and image replacement.
- Watched folders and saved production presets.
- Color separation preview.
- Advanced production color tools and output-intent management.
- Production reports.
- Resume support for interrupted batch jobs.
- Workflow automation.
- Deployment and administrative capabilities where justified.

### 5.2 Product-access policy

PiratePDF shall not artificially restrict implemented functionality through:

- Page limits.
- Reduced engine quality.
- Premium feature gates.
- Subscription tiers.
- Commercial entitlement checks.
- Separate reduced-capability builds.

UI complexity may be managed through progressive disclosure, workspace
organization, presets, and context-sensitive controls, but those usability
choices must not become hidden commercial tiers.

## 6. User Groups

PiratePDF serves both everyday document users and specialized production
workflows through the same product.

### 6.1 General document users

Typical users may include:

- Office workers.
- Educators and students.
- Freelancers.
- Designers.
- Small businesses.
- Home users.
- Users who need occasional print preparation.
- Users converting Word documents to PDFs.

### 6.2 Production and specialized users

Typical production users may include:

- Commercial print operators.
- Variable-data production teams.
- FusionPro users.
- Prepress professionals.
- High-volume document-processing teams.
- Production departments.
- Organizations with repeatable batch workflows.

These groups use the same PiratePDF product. Interface organization may expose
different workflows without changing feature entitlement.

## 7. Core User Experience

PiratePDF should be approachable for new users while still exposing detailed information for professionals.

### 7.1 Editing model

Users should edit directly within the document.

Text editing should use an editing overlay that behaves like an invisible text box while preserving the appearance of direct page editing.

Users should be able to:

- Correct punctuation and spelling.
- Change fonts and font size.
- Change text color.
- Apply bold, italic, and underline where supported.
- Change alignment.
- Adjust paragraph and line spacing.
- Add, remove, or modify list formatting.
- Replace images.
- Resize and reposition images and objects.
- Change object colors.
- Undo any major editing operation.

### 7.2 Properties Sidebar

**Shortcut:** `Ctrl+E`

The Properties Sidebar should provide context-sensitive editing controls for the selected content.

Sections may include:

- Text.
- Paragraph.
- Image.
- Object.
- Position and dimensions.
- Color.
- Metadata.
- Accessibility.

### 7.3 Document Inspector

**Shortcut:** `Ctrl+Shift+E`

The Document Inspector should expose:

- Pages.
- Fonts.
- Embedded images.
- Bookmarks.
- Metadata.
- Security.
- Color spaces.
- ICC profiles.
- Output intents.
- Layers.
- PDF version.
- File size.
- Linearization status.
- Incremental-save information.

### 7.4 Rulers and guidelines

**Shortcut:** `Ctrl+R`

When enabled:

- A horizontal ruler appears above the editing area.
- A vertical ruler appears on the left.
- Users can drag horizontal or vertical guidelines from either ruler.
- A guideline created on one page applies to that page and all subsequent pages by default.
- Guidelines are editing aids only.
- Guidelines do not print or export into the PDF.
- Guidelines are visible only while rulers are enabled.
- Guidelines support undo and redo.
- Visible pages render applicable guidelines without creating guide objects for every page.

### 7.5 Search and replace

PiratePDF should support:

- Find text.
- Replace current match.
- Replace selected matches.
- Replace all matches on the current page.
- Replace all matches in a page range.
- Replace all matches in the document.
- Match-case and whole-word options.
- Regular expressions when appropriate.
- Preview and exclusion of selected matches.
- One-step undo for the entire replace operation.

Image replacement should support:

- Shared image-object replacement.
- Exact decoded-pixel matches.
- Optional visually similar image detection.
- Current page, page range, or entire-document scope.
- Preview and exclusion.
- One-step undo.

---

## 8. Keyboard Shortcuts

### 8.1 Default shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+C` | Copy |
| `Ctrl+V` | Paste |
| `Ctrl+X` | Cut |
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save As |
| `Ctrl+Z` | Undo |
| `Ctrl+Shift+Z` | Redo |
| `Ctrl+P` | Print |
| `Ctrl+O` | Open |
| `Ctrl+N` | New |
| `Ctrl+F` | Find |
| `Ctrl+H` | Find and Replace |
| `Delete` | Delete selected content |
| `Ctrl+A` | Select all applicable objects on the current page |
| `Ctrl+Shift+A` | Deselect all |
| Arrow keys | Nudge selected content |
| `Ctrl+R` | Toggle rulers |
| `Ctrl+E` | Toggle Properties Sidebar |
| `Ctrl+Shift+E` | Open Document Inspector |
| `Ctrl++` | Zoom in |
| `Ctrl+-` | Zoom out |
| `Ctrl+0` | Fit page |
| `Ctrl+1` | Actual size |
| `Ctrl+Mouse Wheel` | Smooth zoom |

Shortcuts should be configurable in a later release without changing their default behavior.

---

## 9. Conversion Profiles

The **Convert to PDF** workflow should support profiles that configure output behavior.

Initial profiles:

- Office Printing.
- Commercial Printing.
- Web PDF.
- Archival PDF/A.
- Press Ready CMYK.
- High Quality Images.
- Small File Size.
- Custom.

Profiles may control:

- PDF version.
- Font embedding.
- Image compression.
- Image downsampling.
- RGB or CMYK destination profile.
- Output intent.
- Metadata.
- Accessibility options.
- Transparency handling.
- Unsupported-object flattening.
- Conversion reporting.

PiratePDF should support core profiles, saved custom profiles, unlimited saved presets where practical, batch profile application, watched-folder profile assignment, production reports, and FusionPro handoff where integration is available.

---

## 10. Unicode and Emoji Strategy

PiratePDF must treat emoji as Unicode grapheme or emoji clusters rather than isolated code points.

Required behavior:

- Preserve variation selectors.
- Preserve zero-width-joiner sequences.
- Preserve skin-tone modifiers.
- Preserve flags and regional indicator sequences.
- Move the caret across one visible emoji as one unit.
- Delete one visible emoji as one unit.
- Preserve searchable Unicode text.
- Use font fallback when the selected font lacks an emoji.
- Convert color-font glyphs to PDF vector or raster appearances where necessary.
- Avoid silent missing-glyph substitution.

Emoji support is part of the single PiratePDF capability set.

The DOC and DOCX conversion pipeline should use the same text-shaping and emoji-handling services as native PDF editing.

---

## 11. System Requirements Strategy

Product requirements should distinguish minimum, recommended, and professional workstation configurations.

### 11.1 Minimum target

- Windows 10/11 x64 (sole official v1 platform).
- Modern 64-bit quad-core CPU.
- 16 GB RAM.
- Integrated graphics.
- SSD with sufficient free space.
- Display meeting the minimum supported resolution.

### 11.2 Recommended target

- Intel Core i5, AMD Ryzen 5, or comparable newer processor.
- 32 GB RAM.
- 1 TB SSD.
- Modern integrated or dedicated graphics.

### 11.3 Large-document workstation target

- Intel Core i7, AMD Ryzen 7, or comparable newer processor.
- 64 GB RAM where high-volume work requires it.
- Fast NVMe SSD.
- Multi-core CPU.
- Dedicated GPU optional unless later profiling demonstrates a material benefit.

The application should still use bounded memory and must not assume that all users have 32 or 64 GB of RAM.

---

## 12. Platform Strategy

### 12.1 Primary platform

Windows 10/11 x64 is the sole official v1 target under the owner's B11-D1
and B11-D3 decisions, recorded in [PP-GOV-005](../05-Governance/Platform-Support-and-Native-Lifetime.md).
Actual application distribution and startup remain unqualified because the
desktop entry project has not yet been implemented.

### 12.2 Cross-platform path

PiratePDF uses Avalonia for the desktop interface and .NET/C# for the primary implementation.

Linux and macOS support is deferred to future community-led work and is not
a v1 release-qualification blocker. The architecture should preserve a path to:

- macOS.
- Linux evaluation.
- Command-line and server tools.
- Future applications built on BlackFlag Engine.

Platform-specific code must remain isolated behind interfaces.

---

## 13. Technology Strategy

### 13.1 Primary language

C# is the primary language for:

- BlackFlag Engine.
- PiratePDF SDK.
- PiratePDF application.
- Plugin host.
- Plugins.
- Tests.
- Developer tools.

### 13.2 Native-performance path

Rust is the preferred secondary language for narrowly isolated, proven performance bottlenecks.

Native modules should expose a stable C ABI and be called from .NET through controlled interop.

C++ should be reserved for:

- Required vendor SDKs.
- Existing C++ integrations.
- Platform APIs that cannot reasonably be accessed another way.

Native code must not be introduced without profiling or a clear compatibility requirement.

---

## 14. Documentation Strategy

Markdown files in the repository are the editable source of truth.

Generated formats may include:

- Searchable PDF.
- HTML documentation.
- In-application help.
- API reference.
- Release notes.

Generated documents must not become the editable master.

Documentation must include:

- Revision history.
- Stable document identifiers.
- Status.
- Cross-references.
- Requirement identifiers where useful.
- Architecture Decision Records.
- Testing references.
- Open issues and deferred decisions.

---

## 15. Product Development Lifecycle

Major features should follow this lifecycle:

1. Idea.
2. Product blueprint.
3. Technical specification.
4. Architecture review.
5. Implementation.
6. Automated testing.
7. Manual validation.
8. Documentation update.
9. Release review.
10. Release.

A feature is not complete when only the code exists.

---

## 16. Product Success Criteria

PiratePDF will be considered successful when it can:

- Reliably open and render complex PDFs.
- Edit supported text, images, vectors, pages, colors, and metadata.
- Handle PDFs containing 6,000 pages or more.
- Preserve document integrity through editing and saving.
- Provide reliable undo, redo, autosave, and recovery.
- Convert DOC and DOCX files into editable PDFs.
- Preserve Unicode and emoji behavior.
- Provide meaningful color inspection and conversion.
- Provide one complete free and open-source product from one codebase.
- Provide production-ready workflows to every user who needs them.
- Maintain clear and searchable documentation.
- Offer stable SDK and plugin extension points.
- Remain maintainable over many years.

---

## 17. Non-Goals for Initial Release

The following are not committed to the initial release:

- Full native Word-document editing and saving back to DOC or DOCX.
- Complete replacement of Enfocus PitStop Pro.
- Real-time cloud collaboration.
- Mobile applications.
- Web application.
- AI functionality in the core V1 product.
- Full legacy Office-object emulation.
- Every PDF specification feature on day one.

Deferred features must remain clearly labeled and must not quietly enter an active milestone.

---

## 18. Revision History

| Version | Date | Summary |
|---|---|---|
| 1.0 | July 22, 2026 | Initial authoritative Markdown edition. |
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized document metadata and Blueprint structure. |
| 1.1 | 2026-08-09 | Replaced the planned commercial-tier strategy with one complete free and open-source PiratePDF product. |
| 1.2 | 2026-10-31 | Apply owner-approved Windows 10/11 x64-only v1 scope; defer Linux/macOS to community-led support while preserving the platform-neutral architecture and identifying the missing desktop qualification target. |
