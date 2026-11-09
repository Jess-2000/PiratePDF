# PiratePDF Project Charter

| Field | Value |
|---|---|
| **Document ID** | PP-CHARTER-001 |
| **Category** | Project Charter |
| **Version** | 1.2 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-2000, BF-8000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

---

## Purpose

This charter defines PiratePDF’s mission, product direction, engineering commitments, open-source product philosophy, and measures of success.

It serves as a stable reference for deciding what PiratePDF should become and how the project should evolve.

## Mission

PiratePDF exists to make working with PDF documents simpler, faster, more transparent, and more trustworthy.

The project aims to provide a feature-rich document editor that is approachable for everyday users while also supporting specialized professional workflows.

## Product Vision

PiratePDF shall become a complete document workspace that helps users:

- Create PDFs.
- Open and edit PDFs.
- Convert documents to PDF.
- Inspect document structure and health.
- Repair damaged or malformed PDFs.
- Optimize document size and performance.
- Prepare documents for printing.
- Learn useful document-production skills.
- Extend the application through plugins.

PiratePDF should feel like a breath of fresh air compared with software that is expensive, difficult to understand, or unnecessarily restrictive.

## Core Product Principles

### Save Time

Features should reduce repetitive work, unnecessary applications, and avoidable manual steps.

### Reduce Frustration

Common workflows should be clear, predictable, and easy to discover.

### Increase Confidence

PiratePDF should explain what it found, why it matters, and what the user can do about it.

### Respect the Document

The application shall preserve document integrity, avoid unnecessary rewrites, validate changes, and clearly communicate destructive operations.

### Performance Is a Feature

PiratePDF and the BlackFlag Engine shall be designed for responsive interaction and large documents, including files containing thousands of pages.

### Professional Without Being Intimidating

New users should be able to begin productive work quickly. Experienced users should have access to increasingly advanced capabilities without overwhelming everyone else.

### Design From Experience

Real-world document and print-production workflows shall inform the product roadmap.

Ideas should be validated against practical user needs rather than copied from competitor feature lists.

### Documentation Is Part of the Product

The source code, architecture, specifications, roadmap, and developer guidance shall be maintained as first-class project assets.

## Product and Open-Source Philosophy

PiratePDF is one complete product.

There is no commercial edition split. Functionality intended for advanced
production, prepress, high-volume, enterprise, or specialized workflows is
part of the same PiratePDF application as everyday document editing
functionality.

PiratePDF shall be 100% free and open source. The project shall not introduce
paid feature tiers, premium-only capabilities, entitlement checks,
subscription-gated functionality, or deliberately reduced editions.

The exact open-source license is a separate legal and governance decision. The
license must be selected explicitly and recorded before a release is
represented as licensed under that specific license.

The complete product capability set may include:

- PDF creation and editing.
- Word-document conversion.
- Page resizing.
- Templates.
- Personal and organizational color palettes.
- Document Inspector.
- Document Doctor.
- PDF optimization.
- Document comparison.
- Plugin management and general-purpose plugin support.
- FusionPro integration where technically and legally possible.
- Advanced preflight.
- PDF/X and PDF/A workflows.
- Advanced color management.
- Ink-coverage analysis.
- Separation previews.
- Batch processing.
- Production reports.
- Organizational templates and palettes.
- Managed plugin deployment.
- Workflow automation.

Advanced capabilities shall be exposed according to workflow relevance and
user needs, not according to a commercial product tier.

## Plugin Strategy

PiratePDF Version 1.0 shall include a plugin architecture.

The plugin system shall:

- Be available to every PiratePDF user.
- Keep optional integrations outside the core application.
- Support version and compatibility checks.
- Allow plugins to contribute commands and workflows through controlled extension points.
- Protect the stability of the host application where practical.
- Support an official FusionPro plugin, subject to available integration and third-party licensing options.

Developer Mode is intentionally deferred until the plugin ecosystem is mature enough to justify it.

## Engineering Commitments

PiratePDF shall be developed using production-quality engineering practices.

Each merged milestone should include, where applicable:

- Maintainable implementation.
- Automated tests.
- Documentation updates.
- Clean build output.
- Passing continuous integration.
- Clear traceability through BF work-item identifiers.
- A focused pull request.
- A demo or sample when it provides meaningful validation.

The project shall favor clean, understandable architecture without introducing unnecessary abstraction.

## Code Readability

Source files should be organized for future readers.

Methods should generally follow the natural execution flow from high-level entry points to lower-level implementation details.

Long files should use simple, consistent section markers where they improve navigation.

Comments should explain intent, constraints, or non-obvious decisions. Comments should not merely restate readable code.

Architectural and file-purpose explanations belong primarily in the documentation library rather than in large source-code summaries.

## Success Criteria

PiratePDF will be considered successful when users can complete everyday document workflows without needing multiple PDF applications.

Success includes:

- Users choosing PiratePDF because they prefer it.
- Reliable handling of real-world documents.
- High performance on large PDFs.
- Clear and trustworthy editing behavior.
- Strong document inspection and repair capabilities.
- A complete capability set for everyday and specialized production workflows.
- Free and open-source availability without paid feature gating.
- A clean codebase that contributors can understand.
- Documentation that remains synchronized with the implementation.

## Intentionally Deferred Capabilities

The following ideas are valuable but are not required for Version 1.0:

- Project Workspaces.
- Workflow Marketplace.
- Developer Mode.
- Cloud synchronization.
- Live collaboration.
- Mobile companion applications.
- AI-assisted content analysis.

These ideas remain candidates for later releases and shall not distract from completing the Version 1.0 foundation.

## Related Documents

Current platform, form-event and release requirements are recorded in the
governing Project Charter and linked policies. The native data-merge plan
belongs to separately scoped future feature work, with CSV mapping, a 25,000
record job ceiling and complete-letter output boundaries. Optional FusionPro
interoperability has its own integration/licensing scope.
[Governing charter](../PROJECT-CHARTER.md),
[Application specification](Blueprint/Volume-III-PiratePDF-Application-Specification.md),
[Document form policy](05-Governance/Document-Form-JavaScript-Policy.md),
[Windows release contract](releases/Windows-Release-Signing-and-Provenance.md).

- PP-VSN-000 — PiratePDF Master Blueprint
- PP-VSN-001 — Vision and Product Strategy
- PP-ARCH-001 — BlackFlag Engine Architecture
- PP-ROAD-000 — Development Roadmap
- PP-GOV-002 — Documentation Standards

## Related BF Work Items

- BF-1000
- BF-2000
- BF-8000

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.2 | 2026-11-08 | Reconcile B13 policy, current guidance, authority references and release/maintenance sequencing. |
| 1.0 | 2026-07-24 | Standardized the project mission, product principles, and edition philosophy. |
| 1.1 | 2026-08-09 | Replaced the planned product-tier split with one complete free and open-source PiratePDF product. |
