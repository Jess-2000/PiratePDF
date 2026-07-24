# ADR-0004 — Keep Word Conversion Outside BlackFlag Engine

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-004 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-7000 |
| **Last Updated** | 2026-07-24 |
| **Next Review** | 2026-10-24 |

---

**Status:** Accepted  
**Date:** July 22, 2026

## Context

DOC and DOCX are flow-layout formats, while PDF is a fixed-layout format. Word conversion requires import, layout, pagination, and compatibility logic that is not part of the PDF engine.

## Decision

DOC and DOCX import will live in PiratePDF conversion modules outside the core BlackFlag PDF engine.

The importer may use BlackFlag page-composition and PDF-writing APIs without making BlackFlag depend on Word formats.

## Consequences

- PDF engine remains focused.
- Additional importers can be added independently.
- Word conversion can evolve without destabilizing PDF parsing and rendering.
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized document metadata and governance structure. |
