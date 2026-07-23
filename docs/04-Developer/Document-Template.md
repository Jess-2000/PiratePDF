@'
# Document Title

| Field | Value |
|---|---|
| **Document ID** | PP-CATEGORY-000 |
| **Category** | Replace Category |
| **Version** | 0.1 |
| **Status** | Draft |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-XXXX |
| **Last Updated** | YYYY-MM-DD |
| **Next Review** | YYYY-MM-DD |

---

## Purpose

Explain why this document exists.

## Scope

Define what this document covers and what it does not cover.

## Requirements or Decisions

Describe the authoritative requirements, behavior, or decisions.

## Risks and Limitations

Document known risks, tradeoffs, and limitations.

## Acceptance Criteria

- [ ] Criterion one
- [ ] Criterion two
- [ ] Tests defined
- [ ] Documentation reviewed

## Related Documents

- PP-XXXX-000 — Related document

## Related BF Work Items

- BF-XXXX

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1 | YYYY-MM-DD | Initial draft |
'@ | Set-Content `
    -Path .\docs\04-Developer\Document-Template.md `
    -Encoding utf8