# BF-1049K - PDF Standard Security Handler Authentication and Decryption

| Field | Value |
|---|---|
| **Document ID** | BF-1049K |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Complete the BF-1049 PDF password boundary with independent Standard security
handler authentication, file-key recovery, permissions validation, and
authenticated string/stream decryption.

## Implemented Scope

- Authenticates Standard security-handler user and owner credentials.
- Supports legacy revisions 2, 3, and 4 file-key/password algorithms.
- Supports AES-256 revisions 5 and 6 authentication and file-key recovery.
- Implements the revision-6 iterative hash algorithm with deterministic bounds
  defined by the PDF algorithm and cancellation checks per round.
- Validates revision-5/6 encrypted `Perms` before exposing an authenticated
  session.
- Exposes typed effective permissions and owner override semantics.
- Supports RC4 object decryption, AES-128 object decryption, AES-256
  string/stream decryption, and Identity crypt filters.
- Resolves revision-4 `StmF`/`StrF` crypt-filter selection without executing or
  loading external handlers.
- Keeps file-key material inside a disposable authenticated session and clears
  sensitive temporary arrays where practical.

## Security Boundary

No decryption session is returned for a failed password, malformed dictionary,
invalid key material, unsupported crypt filter, or inconsistent AES-256
permissions block. Damaged encryption remains terminal under BF-1049G and
BF-1048R; this implementation does not guess keys, recover damaged encrypted
content, invoke external security handlers, or bypass permissions.

Password UI and persisted credentials remain application concerns. The engine
accepts a credential for one explicit authentication attempt.

## Optimization Review

- Uses span-based hashing and fixed-size stack buffers on object-key paths.
- Avoids whole-document materialization; decryption operates on caller-provided
  encrypted string or stream bytes.
- Revision-6 hashing rents its repeated working buffer from `ArrayPool<byte>`
  and clears it before return.
- Uses one authenticated file-key copy per session and derives legacy object
  keys only when an object is decrypted.
- Uses fixed-time comparison for password validation hashes.
- Uses direct dictionary lookups and switch expressions for version/cipher
  selection.
- Clears password bytes, derived hashes, object keys, and the session file key
  when their lifetimes end.

## Tests

Coverage includes revision-2 user authentication, owner authentication, invalid
password rejection, RC4 object decryption, revision-5 AES-256 authentication and
stream decryption, encrypted permissions validation, and disposed-session
rejection. The existing BF-1049G tests continue to cover damaged and unsupported
pre-authentication states. The complete release gate remains the authoritative
integration validation.

## Provenance

BF-1049K follows ADR-0014. The implementation was independently written against
the ISO 32000 Standard security-handler algorithms and PiratePDF's existing
contracts. No external PDF repository implementation source was copied or
adapted.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added Standard security-handler authentication and authenticated decryption. |
