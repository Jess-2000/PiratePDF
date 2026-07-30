# ADR-0005 â€” Use One Codebase for Standard and Professional Editions

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-005 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-8000 |
| **Last Updated** | 2026-07-24 |
| **Next Review** | 2026-10-24 |

---

**Status:** Accepted  
**Date:** July 22, 2026

## Context

Maintaining separate applications would duplicate code, fixes, testing, installers, and documentation.

## Decision

PiratePDF Standard and Professional will share one codebase and one BlackFlag Engine.

Edition differences will be controlled above the engine through licensed capabilities and feature registration.

## Consequences

- One primary maintenance path.
- Shared quality and scalability.
- Easier upgrades between editions.
- The engine must remain edition-agnostic.
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized document metadata and governance structure. |
