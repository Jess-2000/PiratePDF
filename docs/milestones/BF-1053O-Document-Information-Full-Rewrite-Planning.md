# BF-1053O — Document Information Full-Rewrite Planning

| Field | Value |
|---|---|
| **Document ID** | BF-1053O |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1053 |
| **Last Updated** | 2026-08-22 |
| **Next Review** | 2026-11-22 |

## Objective

Complete preservation-aware document-information write orchestration for full-rewrite output, including creation of a previously absent `/Info` dictionary.

## Scope

`PdfDocumentInformationFullRewritePlanner` coordinates the existing document-information projector with the projection-aware full-rewrite planner introduced by BF-1053M.

When `/Info` already exists, the planner projects only the explicitly targeted metadata fields onto that dictionary and refreshes the full-rewrite plan.

When `/Info` is absent and at least one targeted logical field has a value, the planner reserves a new generation-zero indirect object, creates the information-dictionary projection, assigns the trailer `/Info` reference, and refreshes the full-rewrite plan so the new object participates in serialization.

## Preservation

Existing information dictionaries continue to use the BF-1053I projection rules: untargeted entries are preserved, targeted null values remove their keys, explicit empty values remain explicit, ASCII values use literal strings, and non-ASCII values use UTF-16BE hexadecimal strings.

A missing information dictionary is not created when every targeted logical value is absent.

## Transaction Safety

The planner validates full-rewrite mode before mutating the trailer. For newly created `/Info` objects, a downstream projected-planning failure restores the trailer to its previous absent state while BF-1053M restores projected object-store mutations.

## Output Coverage

Tests cover both traditional cross-reference-table and cross-reference-stream full rewrites. The serialized output must contain the newly allocated information object and a matching `/Info` reference in the final trailer representation.

## Deferred Work

Page-tree structural projection, annotations, AcroForm structures, outlines, page decorations, content-stream rewriting, deletion/free entries, encrypted writing, and public Services/SDK orchestration remain deferred.

## Acceptance Criteria

1. Existing `/Info` dictionaries are reused during full rewrite.
2. Missing `/Info` dictionaries are allocated only when targeted metadata contains a value.
3. Newly allocated information objects are included in the refreshed full-rewrite plan.
4. The final traditional trailer carries the correct `/Info` reference.
5. Cross-reference-stream output carries the correct `/Info` reference.
6. Incremental base plans are rejected without mutating the trailer or object store.
7. Existing metadata projection preservation rules remain unchanged.
8. No external dependency or CI prerequisite is added.
9. The full PiratePDF release gate passes.

## Preservation and Dependency Review

No external dependency, NuGet package, native runtime dependency, platform prerequisite, or CI installation step is added.

## Provenance

Implementation is independently written from PiratePDF contracts, specifications, ADRs, and project architecture in accordance with ADR-0014.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-22 | Added full-rewrite document-information planning and missing `/Info` creation. |
