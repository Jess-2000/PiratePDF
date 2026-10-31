# B9 Phase A Completion-2: Final Integration

Document ID: Draft - allocation pending. Version 0.1. Status: implementation in progress.

## Durable entry checkpoint

Branch and HEAD verified: `maintenance/post-BF-1054-repository-review`, `f51824dbc031b58497a38a6292ee11568b9ffeda`. Existing dirty work is intentional. New evidence directory: `artifacts/fuzz/phase-a-completion-2-final-20261010-01`; entry status/hashes preserve previous source/reports/evidence. Prior permitted-context focused-admin-final.trx inspected: 269 executed/passed, zero failed/skipped. Historical acceptance does not validate new code. Root instructions, charter, accepted handoff, PP-AUD-039 and existing checkpoint reports govern this continuation. Previous reports remain historical and will not be overwritten.

## Dependency order and contracts

1. Authenticated single-connection broker: parent independently mirrors deterministic mutation/work/pool/attempt policy, reads only the fixed incoming slot, compares exact candidate bytes, owns all gated worker launches/handles, and authenticates bounded responses. Requests carry sequence, target/case/attempt/content identity; no arbitrary path or executable. Parent-authoritative per-target clocks/counters and whole invocation cancellation prevent child counters enlarging limits.
2. Dedicated one-use admitted campaign child requests workers from that broker. Parent owns 75 seconds including startup/publication/exit, separate at-most-five-second concurrent root/worker/capture cleanup, and a declared separate five-second receipt-publication allowance. Public dispatch stays unchanged until real broker ownership/cleanup tests pass. Child cooperative clock remains defense in depth.
3. Nonce-authenticated terminal handoff and bounded immutable snapshot verification: first/start identity, each deterministic case and parent-observed worker result, terminal/summary equality, inventory/count/caps, child exit/status agreement and exact cleanup. Child snapshot remains provisional. Parent timeout wins after snapshot commit; no child bytes are rewritten.
4. Separate staged atomic parent receipt, no nonce/key/ticket in public data, committed/pending distinction and forensic preservation. Success requires all verification/ownership checks. Timings and scheduling limits are explicit; namespace rename is not disk durability or OS sandboxing.

Trusted same-user workspace/binaries remain assumptions. Observed Windows links/reparse/device/redirects are refused; privileged concurrent substitution and arbitrary hostile descendants are not contained. No hard total RSS or unconditional OS wall-clock claim, dependencies, production/SDK/CI/corpus changes, release gate or substantive coverage-guided campaign.

## Checkpoints and validation

Pending implementation. This report is saved before lengthy implementation/testing. B9 remains OPEN; Completion-3 and substantive campaigns remain separate.
