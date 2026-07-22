# ADR-0005 — Use One Codebase for Standard and Professional Editions

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
