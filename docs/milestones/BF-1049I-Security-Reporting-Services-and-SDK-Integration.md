
# BF-1049I — Security Reporting, Services, and SDK Integration

| Field | Value |
|---|---|
| **Document ID** | BF-1049I |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Expose the completed shared BlackFlag security assessment through conditional
human-readable reporting, an application service boundary, and a stable SDK
surface without leaking parser implementation types or adding UI concerns.

## Reporting

`DocumentSecurityReport` now carries the assessment context required by a
Security Passport: findings, required interactions, manual/automatic origin,
and generation time.

`DocumentSecurityTextReportWriter` provides deterministic human-readable export.
It writes only to a caller-provided `TextWriter`; the security layer never
persists reports automatically.

Clean automatic evaluations still produce no report. Manual inspections always
produce a report, including when clean.

## Application Service

`BlackFlag.Engine.Services.Security.DocumentSecurityService` is the application
orchestration boundary for already-normalized observations. It runs the shared
assessment engine and applies the existing conditional report factory exactly
once.

The service owns no format parsing, UI prompting, network access, persistence,
or document mutation.

## SDK

`DocumentSecuritySdkService` exposes immutable SDK-facing result snapshots. It
maps findings to stable descriptive values and contains no PDF parser
implementation types. The SDK exposes policy outcome, report presence,
interactions, finding location, and report timestamp while keeping UI decisions
in the host application.

## Optimization Review

- Assessment is performed once per service evaluation.
- Report creation reuses the assessment and does not rescan observations.
- SDK mapping preallocates exact-size arrays for findings and interactions.
- Text export iterates by index and performs no sorting or intermediate
  formatting collection.
- Clean automatic evaluation avoids report allocation.
- Existing inspection limits and cancellation remain authoritative.
- No reflection, external process, network access, automatic persistence, or
  source document mutation is introduced.

## Security Invariants

The SDK and service do not weaken policy. Password-required, damaged-encryption,
structural-recovery, sanitization, and other interaction decisions remain
defined by the shared policy layer. Reports are explanatory outputs, not
authorization tokens.

## Provenance

Implementation is independently written from PiratePDF contracts,
specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added reporting context/export, application security service, and SDK integration. |
