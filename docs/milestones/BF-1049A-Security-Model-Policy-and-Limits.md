# BF-1049A - Security Model, Policy, and Limits

| Field | Value |
|---|---|
| **Document ID** | BF-1049A |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-11 |
| **Next Review** | 2026-11-11 |

## Objective

Establish one shared BlackFlag security architecture that PDF, OOXML,
OpenDocument, legacy Office, RTF, Markdown, and future supported document
formats can reuse without duplicating policy, reporting, recursion limits, or
user-interaction rules.

## Implemented Scope

- Added the dedicated `BlackFlag.Engine.Security` project.
- Added a format-independent security-observation model.
- Separated discovered observations from policy-derived findings.
- Added a common BlackFlag security policy.
- Added operation-specific policy for ordinary opening, manual inspection,
  passive conversion, and explicit sanitization.
- Added the silent-by-default user-interaction model.
- Limited automatic user interruptions to password requirements, damaged
  encryption, structural recovery/repair, and sanitization.
- Added conditional report generation.
- Clean automatic inspections produce no report.
- Manual security inspections produce a report even when clean.
- Added shared recursive-container, expansion, compression-ratio,
  observation-count, and work-unit limits.
- Added a shared inspection budget intended to span the root document and all
  recursively inspected embedded content.
- Added format-independent remediation intent.
- Added regression tests for policy decisions, silent inspection, report
  generation, sanitization interaction, damaged encryption, structural
  recovery notification, and bounded inspection work.

## Architectural Rule

Format-specific analyzers discover security-relevant structures and normalize
them into `DocumentSecurityObservation` instances. Shared BlackFlag security
services own policy, reporting decisions, user-interaction decisions, shared
inspection limits, and remediation intent whenever those concepts are
format-independent.

Analyzers must not execute macros, scripts, actions, embedded objects, or other
document-provided behavior in order to inspect them.

## Silent Security Behavior

Automatic inspection is intentionally quiet.

JavaScript, document actions, attachments, macros, embedded scripts and
objects, external references, and similar non-blocking discoveries are
recorded in a security report without interrupting the user.

No security report is generated for a clean automatic inspection.

The only automatic user-interaction categories established by BF-1049A are:

- password entry;
- damaged encryption;
- recovered or repairable structural damage;
- sanitization that removes or neutralizes content.

## Relationship to Passive Conversion

BF-1049A implements the shared policy foundation required by
`PASSIVE-DOCUMENT-CONVERSION.md`. Passive conversion never transfers
executable behavior. When policy selects sanitization, the format-specific
converter remains responsible for applying the requested remediation while
the security layer remains responsible for the normalized finding and
sanitization notification requirement.

## Provenance

BF-1049A follows ADR-0014. The implementation was independently created for
PiratePDF and does not copy security-engine implementations from external
repositories.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-11 | Added the shared BlackFlag security project, observation/finding model, policy engine, silent interaction rules, conditional reports, inspection limits, work budget, remediation intent, and tests. |
