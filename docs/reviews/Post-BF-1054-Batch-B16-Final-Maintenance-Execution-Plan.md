# Post-BF-1054 Batch B16 — Final Maintenance Execution Plan

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-047 |
| **Category** | Engineering Review |
| **Version** | 1.1 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1055 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Purpose and baseline

B16 is the final planned maintenance batch after POST-P2-008 / B15 and before BF-1055. The hosted maintenance-branch CI run [42536472888](https://github.com/Jess-2000/PiratePDF/actions/runs/42536472888) passed on commit `d031cfaaf30dfcf50a0d2ed4a57854fd3295fd37`. Treat this as a hosted CI checkpoint, not a measured managed-fuzzer performance baseline or a claim that all remaining release/desktop qualifications are complete.

The authoritative scope is [PP-ROAD-003](../02-Roadmaps/Windows-Desktop-Development-Sequence.md), [the Future Roadmap](../Blueprint/Volume-VI-Future-Roadmap.md), [PP-GOV-004](../05-Governance/H2b-Deferral-and-Maintenance-Sequencing.md) and the active [handoff](../handoffs/PiratePDF-Maintenance-Handoff-Post-BF-1054-Part1.md).

## Workstream 1 — Semantic cleanup

1. Inventory projects and existing analyzers; identify unreachable, redundant, obsolete and duplicated implementations with concrete call-site evidence.
2. Check each candidate against public APIs, binary/serialized compatibility, reflection, dependency injection, plugin discovery, XAML, generated code, native interop and platform entry points.
3. Classify candidates as **remove**, **retain intentionally**, or **defer for evidence**. No deletion is authorized solely by absent static references.
4. Make narrow changes with focused regression tests, then run the unchanged authoritative gate. Preserve historical documents, dependency inventories, SBOMs, corpus seeds and source-size policies.

## Workstream 2 — Measured managed-fuzzer optimization

1. Capture reproducible baseline measurements for allocations, mutation throughput, replay latency, process startup/supervision, repeated parsing, corpus copies and finding/artifact retention. Record hardware, OS, SDK/runtime, build configuration, seed corpus, case count, warmup, repetitions and elapsed/allocated metrics.
2. Select only bottlenecks supported by measurements. Compare before/after using the same workload and report noise and regressions.
3. Preserve exact deterministic output and replay, immutable seed ownership, mutation and finding semantics, process/resource ceilings, cancellation, failure classification and retained evidence.
4. Reject changes that weaken security, bypass supervision, silently reduce cases, alter coverage or conceal failures. No native H2b execution or hostile-input fallback.

## Acceptance checklist

- [x] Candidate inventory with semantic reachability and preservation decisions.
- [x] Focused regression tests for each source change.
- [x] Reproducible managed-fuzzer baseline with multiple measurements.
- [x] Before/after performance evidence for every claimed optimization.
- [x] Deterministic replay, corpus, finding, cancellation and security invariants verified.
- [x] Windows authoritative release gate and hosted CI pass on the final source commit.
- [x] Optional Linux compatibility signal reviewed without claiming official Linux support.
- [x] Documentation validation, dependency/artifact currency and repository preservation checks pass.
- [x] Completion report distinguishes demonstrated acceptance from deferred B9, H2b, B11 desktop and official signing/distribution requirements.

## Boundaries and sequencing

Implementation and current verification are recorded in the
[B16 completion report](Post-BF-1054-Batch-B16-Final-Maintenance-Completion-Report.md).
The measured change reuses immutable filter configuration and removes three
compiler-proven redundant test imports. The owner reports full-gate success.
Both hosted jobs pass on final source a5c057c at run 42562113984. Formal B16
acceptance is recommended on the recorded evidence.

B16 does not close B9 / POST-P2-002, activate or qualify native H2b, qualify the absent Windows desktop distribution, provision signing credentials, or start BF-1055. The final source changes and acceptance evidence must be reviewed before B16 is marked complete.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.1 | 2026-11-08 | Record implemented cleanup/optimization, reproducible evidence and passing final-source qualification supporting formal acceptance. |
| 1.0 | 2026-11-08 | Establish B16 scoped execution and measurable acceptance plan after hosted B15 CI success. |
