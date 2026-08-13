# BF-1049G - PDF Password / Security-Handler Integration

| Field | Value |
|---|---|
| **Document ID** | BF-1049G |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-12 |
| **Next Review** | 2026-11-12 |

## Objective

Connect PDF trailer encryption metadata and BF-1048R recovery state to the
shared BlackFlag security model through one deterministic pre-authentication
security-handler boundary.

## Implemented Scope

- Added typed PDF encryption states: unencrypted, password required, and damaged.
- Added a PDF Standard security-handler metadata inspector.
- Supports direct and indirect trailer `/Encrypt` dictionaries.
- Validates `/Filter /Standard`, `/R`, `/O`, `/U`, `/P`, and optional `/V`
  before authentication is allowed to proceed.
- Normalizes valid protected documents to the existing `PasswordRequired`
  encryption observation consumed by `BlackFlagDocumentSecurityPolicy`.
- Normalizes malformed, unresolved, unsupported, or damaged encryption metadata
  to the existing terminal `DamagedEncryption` observation.
- Integrates BF-1048R `AuthenticationRequired` and `Damaged` states without
  reconstructing or bypassing encryption.

## Security Boundary

This slice establishes the safe pre-authentication boundary. Protected content
remains blocked whenever the result is `PasswordRequired` or `Damaged`.

BF-1049G does not expose plaintext and does not reinterpret damaged encryption
as an unencrypted document. Password prompting/UI is outside the engine. The
cryptographic decryption pipeline must consume this boundary and may only expose
protected content after successful authentication by a supported Standard
security-handler implementation.

## Optimization Review

- Uses static `PdfName` keys to avoid repeated key construction.
- Uses direct dictionary lookups and switch-based direct/indirect resolution.
- Produces no observation allocation for unencrypted documents.
- Does not enumerate whole dictionaries when fixed-key validation is sufficient.
- Cancellation is checked before resolver access.
- No network, external process, reflection, or speculative recovery is used.

## Tests

Coverage includes unencrypted trailers, direct and indirect Standard encryption
dictionaries, unresolved encryption references, unsupported filters, terminal
damaged-recovery state, and recovered authentication-required state.

## Provenance

BF-1049G follows ADR-0014. The implementation is independently written against
PiratePDF contracts and documented PDF encryption dictionary semantics. No
external PDF repository implementation source was copied or adapted.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-12 | Added typed PDF pre-authentication/security-handler integration. |
