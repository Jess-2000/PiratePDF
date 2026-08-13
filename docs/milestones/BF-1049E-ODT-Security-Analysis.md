# BF-1049E - ODT Security Analysis

| Field | Value |
|---|---|
| **Document ID** | BF-1049E |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049 |
| **Last Updated** | 2026-08-12 |
| **Next Review** | 2026-11-12 |

## Objective

Add passive, bounded security inspection for OpenDocument Text (`.odt`)
documents while reusing the shared BF-1049C/BF-1049D recursive container and
resource-budget architecture.

## Implemented Scope

- Added `OpenDocumentTextSecurityAnalyzer`.
- Added an ODT security adapter over the shared container-security contract.
- Detects embedded Basic/Scripts macro or script content.
- Detects script/event-listener active content in XML parts.
- Detects absolute external `xlink:href` references without dereferencing them.
- Sends binary payloads under embedded object locations through the shared
  embedded-object classifier and recursive inspection engine.
- Excludes ordinary `ObjectReplacements` preview graphics from embedded-object
  security reporting.
- Rejects suspicious package traversal paths as structural observations.
- Enforces archive-entry count, XML-part size, shared expanded-byte,
  embedded-object, compression-ratio, recursion-depth, observation, and work
  limits.
- Checks cancellation throughout package and XML inspection.
- Restricts the root analyzer to ODT input; ODS/ODP remain outside BF-1049E.

## Security Behavior

Inspection is passive. Scripts, macros, event handlers, external references,
and embedded payloads are never executed or dereferenced.

A clean automatic inspection produces no observations. Findings flow into the
existing shared BlackFlag policy/reporting architecture; user-facing
sanitization and Security Passport integration remain in later BF-1049 slices.

## Tests

Coverage includes clean ODT, embedded Basic macro content, external
references, script event listeners, embedded executable classification,
compression-ratio enforcement, ODT format boundaries, and cancellation.

## Provenance

BF-1049E follows ADR-0014. The implementation is independent and uses the
project's existing contracts plus platform ZIP/XML facilities; no external
document-processing repository source is copied or derived.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-12 | Added bounded ODT security analysis on the shared recursive security foundation. |
