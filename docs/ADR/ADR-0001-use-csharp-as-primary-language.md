# ADR-0001 — Use C# as the Primary Language

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-001 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000 |
| **Last Updated** | 2026-07-24 |
| **Next Review** | 2026-10-24 |

---

**Status:** Accepted  
**Date:** July 22, 2026

## Context

PiratePDF requires a language suitable for a cross-platform desktop application, a custom PDF engine, extensive tooling, testing, plugins, and integration with Avalonia.

## Decision

Use C# and modern .NET as the primary implementation platform.

Rust may be introduced only for isolated, measured performance bottlenecks. C++ may be used when required by external SDKs or platform integrations.

## Consequences

- Shared language across engine, application, tests, and plugins.
- Strong Avalonia compatibility.
- Managed-memory safety for most of the codebase.
- Native interop remains available when justified.
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-24 | Standardized document metadata and governance structure. |
