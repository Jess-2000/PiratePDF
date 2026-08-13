
# BF-1049B - PDF Active and Embedded Content Analysis

| Field | Value |
|---|---|
| **Document ID** | BF-1049B |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-11 |
| **Next Review** | 2026-11-11 |

## Objective

Add parser-driven PDF security discovery that reuses the BF-1049A normalized
security model, policy engine, reporting rules, and shared inspection budget.

BF-1049B inventories security-relevant PDF structures. It never executes
JavaScript, actions, attachments, rich media, or other document-provided
behavior.

## Implemented Scope

- Added `PdfSecurityAnalyzer` in the Parsing layer.
- Added bounded PDF object-graph traversal with cancellation.
- Reused the shared `DocumentSecurityInspectionBudget`.
- Added cycle-safe indirect-object traversal.
- Added unresolved-reference structural observations.
- Added PDF action classification.
- Added JavaScript discovery.
- Added automatic `/OpenAction` and `/AA` discovery.
- Added `/Launch` action discovery.
- Added URI and remote-file action discovery.
- Added `SubmitForm` and `ImportData` external-reference discovery.
- Added interactive rendition/movie/sound action discovery.
- Added embedded-files name-tree discovery.
- Added file-specification and `/EF` discovery.
- Added AcroForm and XFA discovery.
- Added FileAttachment, RichMedia, Screen, Movie, Sound, and Widget annotation
  discovery.
- Added normalized source locations for PDF object/report context.
- Added regression tests proving that ordinary findings flow through BF-1049A
  as silent report-only findings.
- Added regression coverage proving that clean PDF inspection produces no
  security report.

## Silent Behavior

BF-1049B does not introduce PDF-specific popups.

JavaScript, actions, attachments, embedded files, external references,
interactive forms, and related observations flow through the BF-1049A policy.
For ordinary opening, these findings are recorded in the security report
without interrupting the user.

If the analyzer discovers nothing security-relevant, the automatic inspection
produces no report.

## Security Boundary

The analyzer examines parsed PDF object structure only. It does not:

- execute JavaScript;
- invoke actions;
- launch external applications;
- submit form data;
- open embedded files;
- render or activate rich media;
- follow external network references.

Embedded payload byte inspection and recursive format classification are
reserved for BF-1049C, which will reuse the same shared inspection budget.

## Reuse

BF-1049B intentionally reuses:

- immutable BlackFlag PDF objects;
- `IPdfIndirectObjectResolver`;
- BF-1049A normalized observations;
- BF-1049A shared inspection limits/budget;
- BF-1049A policy and assessment engine;
- BF-1049A conditional report-generation behavior.

No parallel PDF-specific security policy or reporting model is introduced.

## Provenance

BF-1049B follows ADR-0014. The implementation was independently developed
against PiratePDF contracts and PDF semantics and does not copy source from
other PDF repositories.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-11 | Added non-executing PDF active-content, action, attachment, external-reference, form, annotation, and embedded-file discovery through the shared BlackFlag security pipeline. |
