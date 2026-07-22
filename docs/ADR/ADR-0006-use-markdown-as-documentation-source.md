# ADR-0006 — Use Markdown as Documentation Source of Truth

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
