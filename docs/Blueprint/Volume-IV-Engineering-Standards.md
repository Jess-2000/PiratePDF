# PiratePDF Master Blueprint - Volume IV: Engineering Standards

| Field | Value |
|---|---|
| **Document ID** | PP-DEV-001 |
| **Category** | Developer Documentation |
| **Version** | 1.1 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-1025, BF-1026, BF-1030 |
| **Last Updated** | 2026-08-15 |
| **Next Review** | 2026-10-24 |

---
## Revision History

| Version | Date | Description |
|---|---|---|
| 1.1 | 2026-08-15 | Added source-file size and responsibility-based decomposition standards. |
| 1.0 | 2026-07-24 | Standardized document metadata and Blueprint structure. |

---

## Source File Size and Decomposition

PiratePDF hand-authored source files must remain small enough to review, reason about, and maintain safely. The following limits apply to production code, tests, tooling, samples, and hand-authored PowerShell scripts:

- **1,000 lines is a hard ceiling.** A hand-authored source file must not exceed this limit.
- **Approximately 800 lines is a mandatory review point.** Files reaching this size must be reviewed for a natural responsibility-based decomposition before additional behavior is added.
- **Approximately 500-800 lines is the preferred upper operating range for larger cohesive files.** Smaller cohesive files are fully acceptable and must not be inflated to meet a target size.
- Generated source may be exempt when manual decomposition would not improve maintainability.

Decomposition must follow domain, behavior, or responsibility boundaries. Arbitrary `Part1`, `Part2`, or line-range splits are not acceptable. Partial classes are appropriate when one logical type remains the correct public or architectural facade and the partial files represent cohesive responsibilities. Partial classes must not be used merely to conceal a type whose responsibilities should be separate types.

An organization-only split must preserve behavior and public API unless an architectural change is independently justified. Analyzer and compiler warnings remain milestone blockers, and the full release gate must pass after the refactor.

The repository release gate executes `Test-PiratePDFSourceFileSizes.ps1`. Files at or above 800 lines are reported for review; files above 1,000 lines fail the gate.
