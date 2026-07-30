# ADR-0002 â€” Keep BlackFlag Engine Independent of Avalonia

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-002 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-2000, BF-8000 |
| **Last Updated** | 2026-07-24 |
| **Next Review** | 2026-10-24 |

---

**Status:** Accepted  
**Date:** July 22, 2026

## Context

BlackFlag Engine may later support desktop, command-line, server, testing, and other applications.

## Decision

No BlackFlag Engine project may reference Avalonia or PiratePDF UI assemblies.

The dependency direction is:

BlackFlag Engine â†’ SDK â†’ PiratePDF application and UI.

## Consequences

- Engine remains reusable.
- UI can evolve without contaminating engine internals.
- Testing is easier.
- Future non-GUI applications remain possible.
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized document metadata and governance structure. |
