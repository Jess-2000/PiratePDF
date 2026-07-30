# Documentation Template RC1

| Field | Value |
|---|---|
| Document ID | BF-DOC-1001 |
| Category | Developer Documentation |
| Version | 0.1.0 |
| Status | In Development |
| Owner | PiratePDF Project |
| Related BF Work Items | BF-1001 |
| Last Updated | 2026-07-30 |
| Next Review | 2026-08-30 |

## Usage

Copy the template below when creating a governed PiratePDF or BlackFlag Engine document.
Replace every angle-bracket value before committing. The metadata table must be the first
content after the level-one title so PiratePDF.DocGen can parse it.

## Governed Document Template

```markdown
# <Document Title>

| Field | Value |
|---|---|
| Document ID | BF-DOC-<unique-number> |
| Category | <approved category> |
| Version | <semantic version> |
| Status | <approved status> |
| Owner | PiratePDF Project |
| Related BF Work Items | BF-<four-or-more digits> |
| Last Updated | YYYY-MM-DD |
| Next Review | YYYY-MM-DD |

## Purpose

<Why this document exists.>

## Scope

<What is included and excluded.>

## Requirements

<Normative requirements.>

## Acceptance Criteria

<Conditions required for approval or release.>

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1.0 | YYYY-MM-DD | Initial draft. |

## Related Documents

- <Document title or "None">

## Related BF Work Items

- BF-<number> — <description>
```

## Metadata Rules

- Place the metadata table immediately after the title and one blank line.
- Use the exact `Field` and `Value` column headings.
- Keep one metadata value per row.
- Use a unique document ID.
- Use ISO `YYYY-MM-DD` dates.
- Use BF work-item identifiers containing at least four digits.
- Run DocGen repair preview and validation before opening a pull request.

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1.0 | 2026-07-30 | Established the validator-compatible governed document template. |

## Related Documents

- PDF Object Model RC1

## Related BF Work Items

- BF-1001 — Documentation Template RC1
