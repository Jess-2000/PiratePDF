# Passive Document Conversion Architecture

| Field | Value |
|---|---|
| **Document ID** | PP-ARCH-CONV-001 |
| **Category** | Architecture |
| **Version** | 1.1 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1040, BF-7000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Principle

> PiratePDF converts document content, never executable behavior.

## Version 1.0 Source Formats

- Microsoft Word Binary Document (`.doc`)
- Microsoft Word Macro-Enabled Document (`.docm`)
- Microsoft Word Open XML Document (`.docx`)
- OpenDocument Text (`.odt`)
- Rich Text Format (`.rtf`)
- Markdown (`.md`)

## Behavior Removal

Conversion never transfers macros, scripts, event handlers, ActiveX controls,
OLE automation, DDE behavior, embedded executables, automatic actions,
external refresh behavior, or document-triggered network communication.

Executable behavior and embedded objects are omitted rather than activated or
transferred. The original source remains unchanged.

The local PDF form-event policy governs future supported interactive behavior
after explicit event admission. Conversion remains passive and never transfers
source macros or scripts into the output. Imported data cannot invoke trusted
application operations to obtain external privileges.
[Document form policy](../05-Governance/Document-Form-JavaScript-Policy.md).

## Content Preservation

The converter preserves passive content where supported, including text,
styles, layout, tables, images, vector graphics, equations, headers, footers,
footnotes, and endnotes.

## Inspection and Reporting

Automatic inspection is silent. When no potentially harmful content is found,
no report is generated. When content is removed, an ephemeral Security
Passport is available for review and optional export. A manual security scan
always produces a report.

## File Selection

Task-specific file pickers show only formats supported for that operation.
There is no “show unsupported files” mode. A central Document Format Registry
is the single source of truth for filters, drag-and-drop acceptance,
inspection, conversion, and future format capabilities.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.1 | 2026-11-08 | Clarify passive conversion scope alongside the local PDF form-event policy. |
| 1.0 | 2026-08-06 | Established passive conversion and the version 1.0 format scope. |
