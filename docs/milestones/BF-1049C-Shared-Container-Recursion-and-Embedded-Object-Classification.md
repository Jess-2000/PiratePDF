# BF-1049C - Shared Container Recursion and Embedded Object Classification

| Field | Value |
|---|---|
| **Document ID** | BF-1049C |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-12 |
| **Next Review** | 2026-11-12 |

## Objective

Provide one format-independent recursion and embedded-object classification
foundation for all BlackFlag document-security analyzers.

## Implemented Scope

- Added passive embedded-payload contracts in `BlackFlag.Engine.Security`.
- Added signature-first embedded-object classification.
- Added normalized recognition for PDF, ZIP, OOXML, OpenDocument, OLE compound
  documents, RTF, executable/script content, and common image payloads.
- Added safe container-adapter contracts for future format-specific expansion.
- Added recursive inspection orchestration.
- Reused one `DocumentSecurityInspectionBudget` across the root document and
  every recursively inspected child.
- Enforced shared container-depth, embedded-object, expanded-byte,
  single-object, compression-ratio, observation, and work-unit limits.
- Added cancellation throughout recursive traversal.
- Added normalized observations for every classified embedded payload.
- Added explicit executable/script observations without execution.
- Added tests for signature precedence, OOXML hints, OLE, executable/script
  classification, shared recursion budgets, depth limits, and compression
  ratio enforcement.

## Security Boundary

BF-1049C classifies bytes and coordinates passive recursion. It never executes
or activates embedded content. Declared file names and media types are hints;
recognized byte signatures take precedence when they conflict.

Actual ZIP/OOXML/ODF/OLE extraction remains in format-specific adapters so
parsers can extract facts while BlackFlag Security owns the shared recursion,
budget, normalization, and policy boundary.

## Roadmap Integration

BF-1049D through BF-1049F will build their document-format analyzers and
container adapters on this foundation rather than implementing independent
recursion systems.

This preserves the Version 1 requirement that macros, scripts, event handlers,
ActiveX/OLE automation, DDE behavior, embedded executables, automatic actions,
external refresh behavior, and document-triggered network communication are
never executed merely to inspect or convert a document.

## Provenance

BF-1049C follows ADR-0014 and was independently implemented against PiratePDF
contracts and file-format signatures.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-12 | Added shared recursive embedded-object classification and bounded container-inspection foundation. |
