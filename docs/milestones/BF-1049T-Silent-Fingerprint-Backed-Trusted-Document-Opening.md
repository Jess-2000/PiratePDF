
# BF-1049T — Silent Fingerprint-Backed Trusted-Document Opening

| Field | Value |
|---|---|
| **Document ID** | BF-1049T |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Provide the trusted-document opening fast path without weakening the rule that
every document is untrusted input until it has been safely analyzed.

## Trust Model

The fast path is backed by application-owned local trust evidence:

- SHA-256 of the complete document byte sequence;
- the security-policy version under which validation succeeded;
- validation timestamp;
- optional writer identity.

Document metadata such as PDF `/Producer` or PiratePDF XMP is only a provenance
hint. Metadata alone never creates trust.

Opening a document never creates trust. `DocumentTrustRecordFactory` may be
used only after a complete security inspection succeeds. Persistence remains an
application/platform concern outside `BlackFlag.Engine.Security`.

## Opening Rules

A trusted fast path is permitted only when the current complete SHA-256
fingerprint and security-policy version match the local trust record. If both
the record and current evidence contain a writer identity, it must also match.

Missing, stale, changed, or mismatched evidence silently selects full security
inspection. Successful fast-path validation is also silent. No badge, dialog,
toast, report, or interaction is created merely because trust validation
succeeded or failed.

## Security Properties

- SHA-256 is computed over the complete byte sequence.
- Hashing never executes document behavior.
- Stream position is restored after evaluation.
- Cancellation is checked before and during hashing.
- Metadata cannot elevate an untrusted document.
- A policy-version change invalidates the fast path.
- A byte change invalidates the fast path.
- A writer mismatch invalidates the fast path when both identities are known.
- The service contains no persistence, network, process execution, or UI code.

## Optimization Review

Hashing uses a pooled 64 KiB buffer and `IncrementalHash`, avoiding whole-file
materialization and per-chunk allocation. Comparison is constant-work with
respect to trust metadata size. The stream is traversed exactly once. No report
or finding objects are allocated for the trust decision.

The implementation deliberately performs a full SHA-256 pass rather than
trusting file size or timestamp as authoritative evidence. Correctness and
security take priority over avoiding the hash pass.

## Provenance

Implementation is independently written from PiratePDF project contracts,
architecture, and ADR-0014. No external repository implementation source was
used.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added silent fingerprint-backed trusted-document opening. |
