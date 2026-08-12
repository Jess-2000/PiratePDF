# BF-1049D - OOXML DOCX/DOCM Security Analysis

| Field | Value |
|---|---|
| **Document ID** | BF-1049D |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-12 |
| **Next Review** | 2026-11-12 |

## Objective

Add passive, bounded security inspection for Word OOXML DOCX and DOCM
documents while preserving the shared BlackFlag recursion, policy, and
resource-budget architecture.

## Implemented Scope

- Added `OoxmlWordSecurityAnalyzer` for DOCX/DOCM package inspection.
- Added a shared-container security adapter that extracts OOXML security facts
  while delegating recursive embedded-object traversal to the BF-1049C engine.
- Detects VBA project parts, VBA relationships, and VBA content-type
  declarations.
- Detects ActiveX parts and relationships.
- Detects OLE-object and embedded-package relationships.
- Detects external OOXML relationships without following or dereferencing
  external targets.
- Detects DDE and DDEAUTO Word field instructions without executing them.
- Supplies `word/embeddings`, VBA project payloads, and ActiveX binaries to the
  shared embedded-object classifier and recursive inspection pipeline.
- Supports recursively embedded DOCX/DOCM payloads through the same shared
  inspection engine and budget.
- Rejects suspicious package traversal paths as structural observations.
- Enforces archive-entry count, XML-part size, shared expanded-byte,
  embedded-object, compression-ratio, recursion-depth, observation, and work
  limits.
- Checks cancellation throughout package and XML inspection.

## BF-1049C Shared-Recursion Refinement

BF-1049D extends the BF-1049C container contract with a
format-specific security-adapter result. A security adapter returns normalized
observations and recursively inspectable children; the shared engine remains
the only component that coordinates recursive traversal.

`DocumentContainerEntry` can mark a child as already budget-accounted. This
allows a ZIP-based adapter to reserve embedded-object and expanded-byte budget
before decompression, avoiding decompression-bomb allocation while also
preventing the recursive engine from charging the same bytes twice.

The shared engine also exposes root-container inspection so the root DOCX/DOCM
is not incorrectly reported as embedded content.

## Security Behavior

DOCX/DOCM inspection is passive. It never executes VBA, DDE fields, ActiveX,
OLE automation, external relationships, or embedded payloads. External
relationship targets are treated as inert strings.

A clean automatic inspection produces no security observations and therefore
does not force a Security Passport. Findings remain subject to the shared
BlackFlag security policy and later BF-1049H/BF-1049I remediation/reporting
integration.

## Tests

Coverage includes:

- clean DOCX inspection;
- DOCM VBA project detection;
- external relationships;
- DDE/DDEAUTO fields;
- ActiveX content;
- embedded executable classification;
- recursively embedded DOCM macro discovery;
- shared decompression-ratio enforcement;
- cancellation;
- root-container observation behavior; and
- pre-accounted child budget behavior.

## Provenance

BF-1049D follows ADR-0014. The implementation is independent and uses only
PiratePDF contracts plus platform ZIP/XML facilities; it does not copy or
derive source from external document-processing repositories.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-12 | Added bounded DOCX/DOCM security analysis on the shared recursive container foundation. |
