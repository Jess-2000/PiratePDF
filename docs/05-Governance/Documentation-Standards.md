# PiratePDF Documentation Standards

| Field | Value |
|---|---|
| **Document ID** | PP-GOV-002 |
| **Category** | Governance |
| **Version** | 1.1 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1026; BF-1053 |
| **Last Updated** | 2026-08-16 |
| **Next Review** | 2026-11-16 |

---

## Purpose

This document defines the required structure, metadata, naming conventions, status values, and maintenance rules for PiratePDF and BlackFlag Engine documentation.

Markdown files in the repository are the editable source of truth. Generated Word, PDF, HTML, and in-application documentation are publications derived from the Markdown sources.

## Document Categories

| Prefix | Category | Example |
|---|---|---|
| PP-VSN | Vision | PP-VSN-001 |
| PP-ARCH | Architecture | PP-ARCH-001 |
| PP-ROAD | Roadmap | PP-ROAD-000 |
| PP-SPEC | Technical Specification | PP-SPEC-001 |
| PP-DEV | Developer Documentation | PP-DEV-001 |
| PP-GOV | Governance | PP-GOV-002 |
| PP-ADR | Architecture Decision Record | PP-ADR-001 |
| PP-USER | User Documentation | PP-USER-001 |
| PP-REL | Release Documentation | PP-REL-001 |
| PP-TEST | Testing Documentation | PP-TEST-001 |
| PP-PERF | Performance Documentation | PP-PERF-001 |

## Required Metadata

Every authoritative Markdown document must include:

- Document ID
- Category
- Version
- Status
- Owner
- Related BF Work Items
- Last Updated
- Next Review

Dates use `YYYY-MM-DD`. For newly authored documents, the default `Next Review` date is three calendar months after `Last Updated`. DocGen may repair a missing `Next Review` from a valid `Last Updated` using that deterministic cadence.

## Standard Header

```markdown
# Document Title

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-001 |
| **Category** | Technical Specification |
| **Version** | 1.0 |
| **Status** | Draft |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-2200 |
| **Last Updated** | 2026-08-16 |
| **Next Review** | 2026-11-16 |
```

## Revision History Requirement

Every authoritative document must contain a `## Revision History` section. Validation and repair use the same requirement: a missing section is a validation error and may also receive a repair proposal.

This alignment prevents a document from passing validation while later failing the release gate solely because repair discovers a separately encoded structural requirement.

## Canonical Tooling Contract

DocGen parsing, validation, and repair share canonical metadata names and the Revision History heading through one code-level documentation contract.

When a mandatory canonical rule is introduced, its parser or validator behavior, safe repair behavior, template, and regression tests must be updated together.

A canonical newly authored milestone must satisfy both conditions:

1. Documentation validation reports no issues.
2. Documentation repair proposes no changes.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-23 | Established documentation standards. |
| 1.1 | 2026-08-16 | Aligned validation, repair, template, and deterministic review-date policy. |
