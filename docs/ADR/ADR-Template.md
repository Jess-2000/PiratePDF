@'
# ADR-XXXX — Decision Title

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-XXXX |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Proposed |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-XXXX |
| **Last Updated** | YYYY-MM-DD |
| **Next Review** | YYYY-MM-DD |

---

## Context

Describe the problem, constraints, and forces affecting the decision.

## Decision

State the chosen solution clearly.

## Alternatives Considered

### Alternative 1

Describe the alternative and why it was not selected.

### Alternative 2

Describe the alternative and why it was not selected.

## Consequences

### Positive

- Benefit one
- Benefit two

### Negative

- Tradeoff one
- Tradeoff two

## Implementation Notes

Describe important implementation requirements.

## Validation

Explain how the decision will be tested or reviewed.

## Related Documents

- PP-ARCH-000 — Related architecture document

## Related BF Work Items

- BF-XXXX

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | YYYY-MM-DD | Initial decision |
'@ | Set-Content `
    -Path .\docs\ADR\ADR-Template.md `
    -Encoding utf8