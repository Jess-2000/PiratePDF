# BF-1049H — Remediation and Passive Conversion Security

| Field | Value |
|---|---|
| **Document ID** | BF-1049H |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Turn shared security-policy findings into a deterministic remediation contract
for sanitization and passive document conversion without placing
format-specific mutation logic in the shared security layer.

## Architecture

`DocumentSecurityRemediationPlanner` maps sanitizing findings to intent:

- active, script, automatic, executable, and suspicious relationship behavior:
  `RemoveActiveContent`;
- embedded payloads: `RemoveEmbeddedContent`;
- external references: `DisableExternalReference`;
- blocking or encryption conditions: `RejectOperation`.

Report-only findings do not manufacture mutation work.

`PassiveConversionSecurityService` evaluates normalized observations through
the existing assessment engine and creates the remediation contract converters
must enforce before producing output.

## Security Invariants

Source behavior is never executed. Active behavior and embedded payloads are
omitted or neutralized rather than transferred. External behavior is disabled
without dereferencing it. Password-required and damaged-encryption states are
not treated as sanitizable plaintext. The original source remains unchanged.
Automatic clean conversion remains silent; actual sanitization remains
reportable through the existing interaction/report path.

## Optimization Review

Planning is one ordered pass over existing findings. The action list is
pre-sized. Category mapping is switch-based. Report-only findings allocate no
remediation action. Existing budgets and cancellation remain authoritative.
No duplicate observation scan, network access, external process, reflection,
or source mutation is introduced.

## Provenance

Implementation is independent and follows ADR-0014, ADR-0011, and the approved
PiratePDF passive-conversion architecture.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added remediation planning and passive-conversion security boundary. |
