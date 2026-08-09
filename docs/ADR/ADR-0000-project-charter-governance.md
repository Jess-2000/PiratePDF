# ADR-0000 — Project Charter Governance

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-0000 |
| **Category** | Architecture Decision Record |
| **Version** | 1.1 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-1039, BF-8000 |
| **Last Updated** | 2026-08-09 |
| **Next Review** | 2026-11-09 |

---

**Status:** Accepted
**Date:** July 31, 2026
**Related Milestone:** BF-1039

## Context

BlackFlag Engine and PiratePDF now span parsing, editing, production,
documentation, and security concerns. Technical decisions require a stable
source of project-level values so individual milestones do not redefine the
project's purpose or protections independently.

## Decision

`PROJECT-CHARTER.md` is the governing statement of the project's mission,
values, product promises, security principles, engineering standards, and
definition of success.

Architectural decisions, milestone specifications, implementation choices, and
product behavior should be consistent with the Charter.

When a proposal conflicts with the Charter, the proposal must be revised or
the Charter must be amended explicitly through review. The conflict must not
be resolved silently in implementation.

## Alternatives Considered

### Allow each milestone to define its own principles

Rejected because it would permit inconsistent security, quality, and product-model
policies.

### Treat the Charter as nonbinding project prose

Rejected because a document without decision authority would not guide
difficult tradeoffs.

### Encode all values only in ADRs

Rejected because ADRs record individual decisions and are not an effective
single statement of mission and project identity.

## Consequences

- Future ADRs can reference one stable governing document.
- Security and product-model protections remain consistent.
- Contributors have a clear North Star for difficult decisions.
- Charter amendments require deliberate review.
- Technical convenience cannot silently override adopted principles.

## Future Work

The Charter may be versioned when the project's mission or governing
principles genuinely evolve. Historical ADRs remain unchanged.

## Related Documents

- `PROJECT-CHARTER.md`
- `docs/Project-Charter.md`
- `docs/ADR/ADR-Index.md`

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-31 | Established the Project Charter as the governing project document. |
| 1.1 | 2026-08-09 | Updated governance terminology for the single-edition free/open-source product model. |
