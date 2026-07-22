# ADR-0002 — Keep BlackFlag Engine Independent of Avalonia

**Status:** Accepted  
**Date:** July 22, 2026

## Context

BlackFlag Engine may later support desktop, command-line, server, testing, and other applications.

## Decision

No BlackFlag Engine project may reference Avalonia or PiratePDF UI assemblies.

The dependency direction is:

BlackFlag Engine → SDK → PiratePDF application and UI.

## Consequences

- Engine remains reusable.
- UI can evolve without contaminating engine internals.
- Testing is easier.
- Future non-GUI applications remain possible.
