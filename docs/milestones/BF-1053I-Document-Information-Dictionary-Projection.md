# BF-1053I — Document Information Dictionary Projection

| Field | Value |
|---|---|
| **Document ID** | BF-1053I |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-16 |
| **Next Review** | 2026-11-15 |

## Objective

Implement the first structure-specific preservation-aware projection on top of the BF-1053H indirect-object projection boundary: targeted updates to an existing PDF document-information dictionary.

## Projection Behavior

`PdfDocumentInformationWriteProjector` accepts the authoritative `PdfDocument` plus an explicit set of `PdfDocumentInformationField` targets. It copies the existing indirect `/Info` dictionary and changes only those requested fields.

Untargeted dictionary entries are retained as their existing `PdfObject` values, including unsupported, vendor-specific, and application-private entries. The source dictionary is not mutated during projection.

A targeted logical field that is present becomes a PDF text string. ASCII text remains a literal string; non-ASCII text is emitted as a hexadecimal UTF-16BE string with a byte-order mark. A targeted logical field that is absent removes only the corresponding information-dictionary key. Explicit empty strings remain distinct from absence.

`PdfDocumentInformationField.ModificationDate` maps to the PDF `/ModDate` key.

## Allocation Boundary

This slice intentionally requires an existing indirect information dictionary. Creating a brand-new `/Info` indirect object is deferred because BF-1053H currently permits projected allocation at `PdfWritePlan.NextObjectNumber`, while BF-1053G also reserves that same fixed object number for a transient incremental cross-reference stream.

BF-1053I therefore fails explicitly instead of risking an object-number collision. A later allocation slice will make new projected objects reservation-aware.

## Dependency Review

Production dependencies are unchanged. `BlackFlag.Engine.Writing` remains independent from `BlackFlag.Engine.Editing`.

The Writing test project adds a project-only reference to `BlackFlag.Engine.Editing` so tests can exercise the public editing API that populates authoritative logical metadata. No NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Deferred Work

This slice does not create a missing `/Info` object, project page geometry, annotations, forms, outlines, or page decorations, add deletion/free-entry support, or implement encrypted writing.

## Acceptance Criteria

1. Only explicitly targeted document-information fields are projected.
2. Untargeted and unknown `/Info` dictionary entries are preserved.
3. The original dictionary is not mutated during projection.
4. Targeted absent fields are removed without clearing other metadata.
5. `/ModDate` is mapped correctly.
6. Non-ASCII text has a lossless UTF-16BE PDF string representation.
7. Missing/non-dictionary information objects fail explicitly.
8. Writing gains no production dependency on Editing.
9. No external dependency or CI prerequisite is added.
10. The full PiratePDF release gate passes.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-16 | Added preservation-aware document-information dictionary projection. |
