# ADR-0005 â€” Use One Codebase for Standard and Professional Editions

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-005 |
| **Category** | Architecture Decision Record |
| **Version** | 1.1 |
| **Status** | Superseded |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-8000 |
| **Last Updated** | 2026-08-09 |
| **Next Review** | 2026-11-09 |

---

**Status:** Superseded  
**Date:** July 22, 2026

## Context

This ADR records the original product plan in which PiratePDF Standard and Professional shared one codebase. Maintaining separate applications would have duplicated code, fixes, testing, installers, and documentation.

## Historical Decision

PiratePDF Standard and Professional would share one codebase and one BlackFlag
Engine.

Edition differences would be controlled above the engine through licensed
capabilities and feature registration.

## Superseded By

ADR-0013 — Use One Free and Open-Source PiratePDF Edition supersedes the
edition-based portion of this decision. The architectural benefit of one
codebase remains, but PiratePDF no longer has commercial editions or licensed
feature gates.

## Historical Consequences

- One primary maintenance path.
- Shared quality and scalability.
- Easier upgrades between editions.
- The engine remained independent from product-tier policy.
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized document metadata and governance structure. |
| 1.1 | 2026-08-09 | Marked the edition-based decision superseded by ADR-0013 while preserving its architectural history. |
