# Documentation Validation Rules

| Field | Value |
|---|---|
| **Document ID** | PP-GOV-003 |
| **Category** | Governance |
| **Version** | 1.1 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1028 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

---

# Purpose

This document defines the validation rules enforced by PiratePDF.DocGen.

These rules ensure that PiratePDF's documentation remains complete, consistent, searchable, and maintainable throughout the lifetime of the project.

---

# Scope

These validation rules apply to all authoritative Markdown documents contained within the `docs` directory.

Generated documents and document templates are excluded unless explicitly stated otherwise.

---

# Required Metadata

Every authoritative document shall contain the following metadata table.

| Field | Required |
|---|:---:|
| Document ID | ✓ |
| Category | ✓ |
| Version | ✓ |
| Status | ✓ |
| Owner | ✓ |
| Related BF Work Items | ✓ |
| Last Updated | ✓ |
| Next Review | ✓ |

---

# Document IDs

Document IDs shall:

- begin with the `PP-` prefix
- remain unique
- never be reused
- remain stable after publication

Duplicate IDs are treated as validation errors.

---

# Status Values

The following status values are recognized.

- Proposed
- Draft
- Review
- Approved
- In Development
- Implemented
- Tested
- Released
- Deferred
- Deprecated
- Superseded
- Rejected

Unknown status values are validation errors.

---

# Related BF Work Items

Every document shall reference one or more BlackFlag milestones.

Example:

```
BF-1028
```

Multiple work items may be separated by commas.

Example:

```
BF-1026, BF-1027, BF-1028
```

---

# Date Format

Dates shall use ISO-8601.

Example:

```
2026-07-24
```

Any other format is considered invalid.

---

# Validation Severity

## Error

An error represents a violation of a required documentation rule.

Errors cause DocGen to return a non-zero exit code.

---

## Warning

Warnings indicate documentation that should be reviewed but is still considered valid.

---

## Information

Informational messages provide useful context but do not require action.

---

# Generated Files

The following files are generated automatically and shall not be edited manually.

- Documentation-Index.md
- ValidationReport.md
- Documentation-Health.md
- Document-Changes.md

---

# Excluded Files

The following files are intentionally excluded from validation.

- Document-Template.md
- ADR-Template.md

Additional exclusions may be added as the documentation system evolves.

---

# Future Validation Rules

Future versions of DocGen are expected to validate:

- required document sections
- broken internal links
- architecture references
- duplicate work item references
- source code documentation coverage

---

# Related Documents

- PP-GOV-002 — Documentation Standards

---

# Revision History

| Version | Date | Description |
|---|---|---|
| 1.1 | 2026-11-08 | Reconcile generated-output catalog and implemented revision-history validation with actual DocGen behavior. |
| 1.0 | 2026-07-24 | Initial document. |

