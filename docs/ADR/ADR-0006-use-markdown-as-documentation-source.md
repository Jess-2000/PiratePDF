# ADR-0006 — Use Markdown as Documentation Source of Truth

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-006 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1026, BF-1027, BF-1028 |
| **Last Updated** | 2026-07-24 |
| **Next Review** | 2026-10-24 |

---

**Status:** Accepted  
**Date:** July 22, 2026

## Context

Word and PDF files are useful publications but are difficult to diff, review, merge, and maintain as authoritative sources.

## Decision

Repository Markdown files are the editable documentation source of truth.

PDF and HTML documentation are generated outputs.

## Consequences

- Documentation can be version controlled and reviewed.
- Searchable PDF editions can be regenerated.
- Documentation changes can be tied to code commits.
- Generated documents must not be edited directly.
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized document metadata and governance structure. |
