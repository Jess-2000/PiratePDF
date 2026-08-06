# Architecture Decision Record Index

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-INDEX-001 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-1039, BF-8000 |
| **Last Updated** | 2026-07-31 |
| **Next Review** | 2026-10-31 |

---

## Purpose

This index is the authoritative inventory of Architecture Decision Records for
PiratePDF and BlackFlag Engine.

## Decision Records

| ADR | Title | Status | Related Milestone |
|---|---|---|---|
| [ADR-0000](ADR-0000-project-charter-governance.md) | Project Charter Governance | Accepted | BF-1039 |
| [ADR-0001](ADR-0001-use-csharp-as-primary-language.md) | Use C# as the Primary Language | Accepted | BF-1000 |
| [ADR-0002](ADR-0002-keep-blackflag-independent-of-avalonia.md) | Keep BlackFlag Engine Independent of Avalonia | Accepted | BF-1000 |
| [ADR-0003](ADR-0003-use-random-access-file-io.md) | Use Random-Access File I/O | Accepted | BF-1000 |
| [ADR-0004](ADR-0004-keep-word-conversion-outside-engine.md) | Keep Word Conversion Outside BlackFlag Engine | Accepted | BF-1000 |
| [ADR-0005](ADR-0005-use-one-codebase-for-editions.md) | Use One Codebase for Standard and Professional Editions | Accepted | BF-1000 |
| [ADR-0006](ADR-0006-use-markdown-as-documentation-source.md) | Use Markdown as the Documentation Source of Truth | Accepted | BF-1026 |
| [ADR-0007](ADR-0007-separate-stream-parsing-from-object-parsing.md) | Separate Stream Parsing from Object Parsing | Accepted | BF-1039 |
| [ADR-0008](ADR-0008-defer-indirect-stream-length-resolution.md) | Defer Indirect Stream Length Resolution | Accepted | BF-1039 |
| [ADR-0009](ADR-0009-preserve-raw-stream-bytes.md) | Preserve Raw Stream Bytes Before Filter Decoding | Accepted | BF-1039 |

## Lifecycle

ADR lifecycle values are:

- Proposed
- Accepted
- Superseded
- Deprecated
- Rejected

The governed metadata status uses the corresponding approved documentation
status where required by DocGen.

ADRs remain in the repository when superseded so the architectural history is
preserved.

## Numbering

ADR numbers are unique, sequential, and never reused.

ADR-0000 establishes Charter governance. Technical decisions begin with
ADR-0001.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-31 | Created the authoritative ADR index through ADR-0009. |
