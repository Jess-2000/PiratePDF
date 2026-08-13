
# BF-1049J — Adversarial, Fuzz, and Integration Hardening

| Field | Value |
|---|---|
| **Document ID** | BF-1049J |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-13 |
| **Next Review** | 2026-11-13 |

## Objective

Close BF-1049 with adversarial boundary tests and hardening of the shared
inspection budget used across recursive document-security inspection.

## Hardening

`DocumentSecurityInspectionBudget` now performs bounded counter consumption with
compare/exchange loops. Limit checks use subtraction before addition, preventing
integer wraparound when a deliberately extreme configured limit approaches
`Int64.MaxValue`.

Embedded-object accounting is transactional with respect to expanded-byte
accounting. If the expanded-byte charge is rejected, the object-count charge is
rolled back before the exception leaves the budget.

These changes preserve the existing public API and normal-limit behavior.

## Adversarial Coverage

The BF-1049J tests verify:

- expanded-byte accounting cannot wrap at the signed 64-bit boundary;
- work-unit accounting cannot wrap at the signed 64-bit boundary;
- a rejected expanded-byte charge does not leak an embedded-object count;
- exact configured boundaries succeed once and reject subsequent work;
- 4,096 deterministic pseudo-random byte sequences can be classified without
  parser execution or classifier failure;
- truncated DOS/PE-looking prefixes remain passive rather than being promoted
  to executable content without a complete PE signature.

The deterministic pseudo-random corpus is intentionally generated in-process.
It requires no external corpus, network access, repository-derived source, or
nondeterministic seed.

## Integration Invariants

BF-1049J does not introduce a second security pipeline. Existing format
analyzers, recursive inspection, policy assessment, remediation, reporting,
services, SDK integration, PDF encryption boundary, and trusted-document
opening continue to use the same shared contracts established in BF-1049A
through BF-1049I and BF-1049T.

The repository release gate remains the authoritative integration gate for the
complete milestone.

## Optimization Review

The previous `Interlocked.Add` plus rollback pattern is replaced by a
compare/exchange loop that avoids overflow and avoids mutating a counter on
ordinary limit rejection. Under uncontended inspection the loop normally
completes in one compare/exchange. No locks, heap allocations, reflection,
process execution, or network access are added to budget accounting.

The fuzz-style test reuses one 257-byte buffer for all 4,096 iterations and
uses a fixed seed, keeping test allocation and runtime bounded and
deterministic.

## Security Invariants

- Untrusted input remains passive.
- Limits fail closed with `InvalidDataException`.
- Counter overflow cannot convert exhausted budget into apparent capacity.
- Rejected embedded-byte accounting does not leave a stale object charge.
- Classification does not execute embedded payloads.
- No cryptographic recovery of damaged encryption is introduced.
- No automatic persistence or trust elevation is introduced.

## Provenance

Implementation is independently written from PiratePDF project contracts,
architecture, and ADR-0014. No external repository implementation source was
used.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-13 | Added adversarial budget, deterministic fuzz-style, and integration hardening. |
