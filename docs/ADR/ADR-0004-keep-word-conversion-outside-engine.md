# ADR-0004 — Keep Word Conversion Outside BlackFlag Engine

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
