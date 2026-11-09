# PiratePDF Master Blueprint - Volume III: PiratePDF Application Specification

| Field | Value |
|---|---|
| **Document ID** | PP-SPEC-001 |
| **Category** | Technical Specification |
| **Version** | 1.1 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-8000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

---
## Current architecture and delivery scope

PiratePDF uses Avalonia through the SDK and Services boundaries. PdfDocument
owns authoritative document state, transactional Editing owns mutations and
history, and preservation-aware Writing owns serialization. Desktop view models
hold presentation and session state without duplicating the engine object graph.
BlackFlag Engine remains platform-agnostic.

BF-1054 is complete. BF-1055 canonical font editing follows the approved
maintenance sequence. The provisional BF-1056 foundation, BF-1057 editing
integration and BF-1058 packaging milestones require registry reconciliation.
The current checkout has no desktop entry project. Windows 10/11 x64 is the
official v1 target, with Linux/macOS deferred to community-oriented support.
[Desktop sequence](../02-Roadmaps/Windows-Desktop-Development-Sequence.md).

## Form controls and document scripts

The desktop must preserve supported non-scripted form controls and enforce
the canonical local form-event policy across engine, SDK and Services.
Script runtime/event dispatch and Safe Mode need separately scoped implementation
and security qualification. The approved policy defines allowable behavior
without establishing a new v1 implementation commitment.
[Document form policy](../05-Governance/Document-Form-JavaScript-Policy.md).

## Planned native data merge and composition

Native data merge is planned advanced functionality requiring a separately
approved feature milestone and acceptance evidence. Its presence in the product
architecture supplies planning constraints. The v1 milestone scope determines
delivery commitments.

- Map CSV fields and ingest explicitly selected local data through trusted
  application operations.
- Support programmable templates, conditional content and variable-length,
  independently paginated letters under the approved security policy.
- Admit at most **25,000 records per generation job** and reject oversized jobs
  before generation. Resource ceilings may reject smaller jobs when necessary.
- Preserve **whole-letter atomicity across output PDF boundaries**. Rotate to
  another output PDF between complete letters. A single letter that exceeds a
  configured output/resource bound must fail coherently before publication.
- Automatically batch into multiple PDFs while preserving input-record order
  and complete-letter boundaries.
- Bound memory and aggregate work, using controlled hybrid disk-backed spooling
  where the working set requires it.
- Support safe cancellation and recovery with verifiable checkpoints. Verify
  every completed output before publication and distinguish verified completed
  outputs from incomplete staging when a job stops.
- Keep imported customer data local by default. External transmission requires
  a separately authorized trusted application workflow and never follows from
  template or document-script execution.

Native merge/composition is independent of optional FusionPro interoperability.
Existing FusionPro preservation contracts retain their structural/serialization
scope. Optional integration remains subject to available interfaces and licensing.
Neither preservation support nor this plan establishes a working merge engine.

## Windows distribution dependencies

The future executable and packaging workstreams must supply the real application
for B11 startup/native qualification and the official signing acceptance checks.
Source/build identity, final SHA-256 hashes, SBOM/notices and isolated credential
requirements follow the release contract.
[Windows signing and provenance](../releases/Windows-Release-Signing-and-Provenance.md).

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.1 | 2026-11-08 | Record current engine-backed desktop architecture, form policy, planned native data-merge limits and release dependencies. Replace obsolete blanket JavaScript exclusion with the canonical bounded local-form policy and actual implementation status. |
| 1.0 | 2026-07-24 | Standardized document metadata and Blueprint structure. |
