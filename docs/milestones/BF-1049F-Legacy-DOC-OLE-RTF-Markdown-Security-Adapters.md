# BF-1049F - Legacy DOC/OLE + RTF/Markdown Security Adapters

| Field | Value |
|---|---|
| **Document ID** | BF-1049F |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-12 |
| **Next Review** | 2026-11-12 |

## Objective

Complete the Version 1.0 passive-conversion security-adapter set for legacy
Word Binary DOC, OLE Compound File Binary containers, RTF, and Markdown while
preserving the shared recursive inspection and resource-budget architecture.

## Implemented Scope

- Added bounded OLE Compound File Binary parsing for security inspection.
- Added legacy `.doc` root analysis.
- Detects VBA/WordBasic/macro-related OLE entries.
- Detects ActiveX-related OLE entries and embedded-object storage indicators.
- Scans bounded WordDocument/Table streams for DDE and external-reference data.
- Extracts recognizable payloads from native/package OLE streams for shared
  recursive classification.
- Added RTF root and container security analysis.
- Detects RTF object data, automatic OLE update/link controls, DDE fields, and
  external field-reference constructs.
- Decodes bounded RTF `objdata` and sends embedded payloads through the shared
  recursive classifier.
- Added Markdown root and container security analysis.
- Detects raw HTML active constructs, script URIs, and external link/resource
  references without dereferencing them.
- Added Markdown to normalized embedded-object classification and made RTF a
  recursively inspectable container.
- Added a built-in format-adapter factory used by DOC/OLE, RTF, Markdown,
  DOCX/DOCM, and ODT analyzers so nested supported document types share the
  same recursion engine and inspection budget.

## Integration Hardening

- OLE format-specific parsing now requires the actual CFB signature in addition
  to contextual classification, so synthetic or contextually classified OOXML
  macro/ActiveX parts retain their normalized findings without being
  misinterpreted as valid Compound File Binary containers.
- The malformed-OLE regression fixture carries the CFB signature but invalid
  structure, ensuring it exercises the OLE parser rather than only the shared
  classifier.
- RTF and OLE embedded-payload signature searches use ordered loops and reusable
  signature tables, preserving OLE -> PDF -> ZIP -> DOS priority while removing
  repeated conditional search chains.
- RTF hexadecimal object decoding uses a pre-sized buffer to avoid List growth
  and the subsequent collection-to-array copy.
- Cancellation is checked before full RTF text materialization.
- OLE DDE and external-reference scans use precomputed ASCII/UTF-16 patterns, avoiding repeated encoding allocations per inspected stream.

## Security Behavior

All analysis is passive. No VBA, WordBasic, DDE, ActiveX, OLE automation, raw
HTML behavior, script URI, external reference, or embedded payload is
executed, activated, or dereferenced.

Automatic clean scans remain silent. Findings are normalized observations for
the existing BlackFlag policy layer. Passive-conversion remediation intent and
Security Passport integration remain in BF-1049H and BF-1049I.

## Bounds and Failure Behavior

OLE parsing validates signature, byte order, sector geometry, FAT/DIFAT
chains, directory limits, stream sizes, chain cycles, and mini-stream bounds.
RTF and Markdown enforce configured document limits. Recursive embedded
payloads remain governed by the shared BF-1049 inspection budget.

Malformed container structure produces a deterministic inspection failure
rather than attempting to execute, repair, or reinterpret potentially active
content.

## Tests

Coverage includes clean RTF/Markdown, RTF DDE, RTF embedded executable
classification, Markdown external references, raw HTML active content, script
URIs, DOC format boundaries, invalid OLE rejection, and normalized
RTF/Markdown container classification.

## Provenance

BF-1049F follows ADR-0014. The implementation is independent and uses
PiratePDF contracts, platform primitives, and documented file-format
structures; it does not copy or derive source from external document-processing
repositories.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-12 | Added legacy DOC/OLE, RTF, and Markdown passive security adapters. |
