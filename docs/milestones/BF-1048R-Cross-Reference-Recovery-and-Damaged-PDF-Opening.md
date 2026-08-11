
# BF-1048R - Cross-Reference Recovery and Damaged-PDF Opening

| Field | Value |
|---|---|
| **Document ID** | BF-1048R |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1042, BF-1044, BF-1048 |
| **Last Updated** | 2026-08-10 |
| **Next Review** | 2026-11-10 |

## Objective

Provide bounded, non-destructive structural recovery for PDFs whose normal
cross-reference information is unusable while preserving a hard security
boundary around encrypted content.

## Implemented Scope

- Recovery modes and bounded recovery limits.
- Parser-validated physical indirect-object scanning.
- Synthetic newest-wins direct-object location tables.
- Recovery trailer discovery outside parser-valid indirect-object extents.
- Catalog validation and unique-catalog inference when a trustworthy trailer
  is unavailable.
- Duplicate physical-object diagnostics.
- Recovery confidence and diagnostics.
- Structural encryption assessment.
- `AuthenticationRequired` handoff when structurally complete Standard
  security metadata is present.
- `Damaged` classification when encryption metadata is missing or malformed.
- A hard rule that damaged encryption is never reconstructed, bypassed, or
  silently ignored.
- Deterministic PiratePDF-owned recovery/security fixture PDFs and SHA-256
  fixture manifest.
- Regression coverage that rejects a false object header embedded inside a
  parser-valid stream.

## Security Invariant

Document recovery may increase PiratePDF's ability to locate valid content,
but it must never increase the caller's authorization to access protected
content.

BF-1048R does not implement a PDF cryptographic security handler. When
structurally valid password-security metadata is encountered, the recovery
layer reports `AuthenticationRequired`. The future document-opening/security
layer owns password prompting, password validation, decryption, and permission
interpretation. Damaged encryption remains a terminal security condition.

## Non-Destructive Policy

Recovery constructs an in-memory synthetic object index. It does not patch the
source bytes. A future explicit repair/save operation must use the normal PDF
writer to produce fresh cross-reference structures.

## Fixture Provenance

All BF-1048R fixtures were generated specifically for PiratePDF. No external
PDF repository test corpus or implementation source was used.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-10 | Added bounded structural xref recovery, encryption-safety classification, diagnostics, and deterministic recovery/security fixtures. |
