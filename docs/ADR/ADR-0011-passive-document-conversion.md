# ADR-0011 — Convert Documents as Passive Content

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-011 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-7000 |
| **Last Updated** | 2026-08-06 |
| **Next Review** | 2026-11-06 |

---

**Status:** Accepted
**Date:** August 6, 2026
**Related Milestone:** BF-1040

## Context

Source documents may contain macros, scripts, active controls, embedded
objects, automatic actions, and external-update behavior. Conversion must not
execute or transfer that behavior into the resulting PDF.

## Decision

PiratePDF converts passive information and omits executable behavior. Version
1.0 conversion supports DOC, DOCM, DOCX, ODT, RTF, and Markdown.

Macros, scripts, ActiveX, OLE behavior, embedded objects, automatic actions,
and document-triggered network activity are never executed or transferred.

## Consequences

- Conversion is deterministic and safer by default.
- Some source features are intentionally omitted.
- The original source remains unchanged.
- Findings are reported only when intervention occurs or a manual scan is
  requested.

## Related Documents

- `docs/architecture/PASSIVE-DOCUMENT-CONVERSION.md`
- `PROJECT-CHARTER.md`

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-06 | Adopted passive conversion and the version 1.0 source-format scope. |
