# B13 Signing, Provenance and Documentation Reconciliation

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-043 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049, BF-1050, BF-1054, BF-1055, BF-8000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Disposition and acceptance recommendation

**B13 DOCUMENTATION RECONCILIATION COMPLETE — ACCEPTANCE RECOMMENDED**

The revised assignment's documentation scope is complete. The owner confirmed
the full B13 release gate passed after the five agent-observed symbolic-link
fixture privilege failures. Documentation and preservation checks pass.
Recommend accepting the revised documentation-reconciliation deliverables.
POST-P2-006 retains official signed-distribution qualification as future work.
Production certificates and the application/installer remain dependencies of
the separately scoped executable, packaging and V1 Release Preparation work.

## Baseline and authority

- Branch `maintenance/post-BF-1054-repository-review`.
- HEAD `67f1e622024cb50e8be210d244e4ede2955f6f3c`.
- Entry working tree clean. The entry SHA-256 catalog covers 1,721 repository
  files. Ignored Markdown evidence is separately inventoried and hash-bound.
- Evidence directory `artifacts/fuzz/b13-reconciliation-20261108-01/` retains
  status, branch/HEAD, entry hashes, Markdown outlines/topic review, link
  findings and validation logs.
- The assignment's stale B12-remediation statement conflicted with committed
  PP-AUD-042 v1.2 and the owner's successful remediation gate confirmation.
  The owner explicitly directed **preserve B12 completion and document the
  assignment discrepancy**. B12 remains complete and its evidence is retained.
- Root AGENTS.md, the governing Project Charter, PP-GOV-004/005, current
  handoff/audit, B12 evidence, Blueprint, ADRs, specifications, CI and the
  authoritative gate supplied repository authority.

## Complete Markdown inventory and review method

The machine-readable inventory records every entry/final Markdown path,
classification, original/final SHA-256, line count, reviewed topic matches,
action and reason. It includes root guidance and the nested reference-plugin
README as well as the documentation library.
[Complete inventory](B13-Markdown-Inventory.json).

Discovery used rg with hidden/ignored files included, excluding `.git`, build
outputs (`bin`/`obj`) and node_modules. This found **276 Markdown files** at
entry, including 44 files under ignored artifacts. Vendor publications are
classified separately. The whole content of every discovered file was read
for the audit's automated content/link scans. Contextual review examined
current guidance, metadata/authority, applicable policy paragraphs, historical
milestone contracts and matched status/implementation statements. This was a
policy-focused documentation audit. Existing C# sources were inspected where
needed to check implementation claims.

The final inventory contains **279 files**, comprising **52 current guidance**,
**211 historical evidence**, **11 generated documentation** and **five unrelated
material** entries. It records 22 updated current documents and three additions.
Generated results have their own regeneration actions. The remaining current
guidance and protected evidence have explicit retain actions and reasons.

Classification separates current guidance, historical evidence, generated
documentation and unrelated material. Original RC1 packages and the living
BF-1039/BF-1040 narrative carry historical implementation context. Accepted
ADR principles remain current guidance while their dated future-work examples
retain their original milestone scope. The inventory identifies templates and
upstream legal/tool publications separately from authored project guidance.

Generated indexes, validation/health reports and repair previews are regenerated
through DocGen. Historical milestone/release/review files, qualification reports,
copied snapshots and vendor evidence retain their original bytes. Current
documentation indexes and the active handoff explain subsequent authority.

## Updated files and reasons

| Area | Updated authoritative sources | Reason |
|---|---|---|
| Root authority | AGENTS.md, PROJECT-CHARTER.md, README.md | Reconcile current maintenance order, Windows release requirements and canonical form-event policy |
| Form policy | PP-GOV-006 | Centralize permitted local behavior, prohibited effects, Safe Mode, uniform enforcement and actual implementation status |
| Release contract | PP-REL-001 | Define development/official identity, source/build traceability, final SHA-256, accountability records, credential isolation and verification failure handling |
| Product architecture | Blueprint Volumes I, II and III | Replace obsolete blanket script exclusion, retain engine state ownership and record planned native data-merge/composition requirements |
| SDK and plugins | SDK specification and plugin developer contract | Route document effects through the same policy and existing ownership/security boundaries |
| Document model | Document specification | Reconcile authoritative PdfDocument and transactional Editing/Writing with current source |
| Conversion | Passive conversion architecture | Clarify the separate passive-import and admitted local form-event scopes |
| Roadmap | Roadmap index, Future Roadmap, desktop sequence and PP-GOV-004 | Reconcile B12 completion, current B13, B14/B15/final B16 and provisional desktop milestones |
| Entry points | docs/README.md, Blueprint index, specification index and product charter | Repair absent roadmap/governance links and obsolete duplicate governance references |
| ADR catalog | ADR-Index.md | Catalog existing ADR-0014 and link current authority while retaining dated ADRs |
| Validation guidance | PP-GOV-003 | Reflect existing revision-history enforcement and generated-output catalog |
| Continuation | Active repository audit and maintenance handoff | Record current policies, owner-resolved B12 discrepancy and remaining release dependencies |

The inventory supplies the exact path/action for every reviewed file.
Primary document IDs are preserved. Blueprint legacy volume identifiers remain
labeled as aliases. Modified maintained documents receive a version/history
entry dated November 8, 2026. Duplicate identical history blocks in the current
library entry page were collapsed while retaining the original distinct record.
PP-GOV-004's misplaced 1.1 history row was moved from its evidence table to
the revision-history table. PP-GOV-002's hash-bound policy bytes are preserved.

## Relevant files reviewed and retained

The inventory's retain actions cover all original completed milestones,
release summaries and B1-B12 reports. In particular, BF-1048B's old
Windows/macOS v1 statement is historical. PP-GOV-005 supplies current
Windows-only v1 authority. BF-1049 passive discovery and BF-1052 structural
form contracts retain their original implementation scope under PP-GOV-006.
BF-1051H FusionPro preservation remains distinct from native composition.

Current memory/resource architecture, low-level Abstractions/Core/IO/Syntax
specifications, source-hygiene/QA standards, passive-conversion ADR, plugin
reference README, dependency instructions and reviewed license matrix were
examined for applicable contradictions. Their retained requirements remain
consistent with the reconciled policies. PP-GOV-005 already describes the
approved platform/native-lifetime decision accurately. The B12 JSON/XML,
notices, legal evidence and approved CycloneDX version retain their bytes.

## Security reconciliation

PP-GOV-006 preserves supported local button actions, calculations, formatting,
validation, selection controls, dependent field visibility/updates and local
autofill from existing values, conditional on qualified sandbox/policy support.
It prohibits document-open/close/unattended script execution, network/remote
retrieval, external transmission, arbitrary filesystem/process/code access,
dynamic modules, sandbox escape and privilege/consent bypass in every mode.

User interaction grants event intent within the allowed local effects.
Document-script APIs cannot silently acquire authority. Safe Mode disables
scripts while retaining supported non-scripted controls. Importing, rendering,
saving and inspection remain passive. Trusted user-initiated application
operations, such as CSV import, require their own application authorization.

The engine, SDK, Services, plugins and desktop all reference this single policy.
Future field effects must use authoritative PdfDocument and transactional Editing,
with rollback/history and preservation-aware Writing. Runtime selection and
sandbox qualification remain separately scoped implementation requirements.

## Architecture, roadmap and data composition

Current guidance preserves completed BF-1054 and separately upcoming BF-1055.
The desktop consumes SDK/Services and retains engine-backed state. Windows
10/11 x64 is the sole official v1 target, with Linux/macOS deferred to community
support. B9 remains open, native H2b deferred/disabled/unqualified, B10 complete,
B11 partial pending actual desktop distribution, and B12 complete.

B13 precedes B14, B15 and final maintenance B16 before BF-1055. BF-1056 through
BF-1058 remain provisional pending registry reconciliation. The original Phase A
proposal table in the active audit retains its dated ordering, with current
authority supplied by the November 8 reconciliation.

The application specification records planned native CSV mapping/local ingest,
programmable templates/conditional content, independently paginated variable
letters and a maximum **25,000 records per job**. Whole letters remain atomic
across output PDF boundaries. Automatic multi-PDF batching, bounded memory,
hybrid disk-backed spooling, cancellation/recovery and verified outputs are
requirements. Customer data remains local by default. Optional FusionPro
interoperability retains a separate integration/licensing scope. These advanced
capabilities require feature-milestone scope before a v1 delivery commitment.

## Implementation claims checked against source

| Claim | Repository evidence | Reconciled statement |
|---|---|---|
| Authoritative document | `src/BlackFlag.Engine.Document/Model/PdfDocument.cs` | Owns catalog/trailer/object store, pages, metadata, AcroForm, outline and decorations |
| Transactional Editing | `src/BlackFlag.Engine.Editing/Documents/PdfEditingSession.cs` and transaction/history code | One active ownership-bound session coordinates validated mutations and replay |
| Preservation-aware save | `src/BlackFlag.Engine.Services/Editing/PdfEditingSaveService.cs` and Writing project | Existing save/projection machinery owns output semantics |
| Passive PDF scripts | `src/BlackFlag.Engine.Parsing/Security/PdfSecurityAnalyzer.cs` | Inventories JavaScript/actions without execution |
| Policy assessment | `src/BlackFlag.Engine.Security/Policy/BlackFlagDocumentSecurityPolicy.cs` | Operation-specific assessment/reporting and remediation decisions |
| SDK/Services | DocumentSecurityService and DocumentSecuritySdkService | Evaluate/map observations and results without adding a script runtime |
| Forms | PdfDocument.AcroForm ownership and BF-1052E source/contract | Structural field state exists, with runtime/event and appearance work requiring separate qualification |
| Build identity | `Directory.Build.props`, `global.json` and current CI workflow | Deterministic compilation and developer build/test/accountability steps exist |
| Official signing | Repository signing/workflow search | Production SignTool/credential/publishing implementation and qualified signed release remain deferred |
| Desktop and merge | Project inventory and subsystem source search | Developer tools/test executables exist, while the desktop entry project and native merge engine await their feature scopes |

## Conflicts and references

| Finding | Resolution |
|---|---|
| Assignment retains B12 outstanding items after accepted remediation | Owner explicitly chose to preserve committed B12 completion |
| Blanket script exclusion in current engine blueprint | Superseded by PP-GOV-006 and explicit runtime implementation status |
| Root/roadmap/handoff continuation still points to B10/B11 | Current guidance now identifies B13 and the retained later sequence |
| Historical Windows/macOS v1 and native qualification wording | Retained dated bytes, with current authority linked from maintained guidance |
| Missing development-roadmap and project-governance guide files | Four live links redirected to the actual roadmap index and governing charter |
| Duplicate PP-GOV-002 reference for an absent separate governance guide | Current catalogs now reference the actual documentation standards and governing charter |
| Existing ADR-0014 absent from catalog | Added its existing identity to the maintained ADR index |
| Planned signing described alongside current developer hardening | Release contract explicitly identifies implemented inputs and future signing/provisioning/qualification |

Local link/heading-anchor checks include all discovered Markdown, with fenced
examples excluded. Remote URLs were classified as external references. The
Microsoft signing command references were checked against official documentation.
Historical copied indexes contain **422 unresolved local links** across three
ignored snapshot files. They are retained with exact path/target records in
the link findings. Two copied generated indexes account for 214 and 207, and
the copied B11 v1.2 report accounts for one. Those relative links originally
resolved from their source directories. Current source/library links are
checked separately after the new B13 report is present.

Older ADRs also contain repository-root textual path examples using lowercase
`docs/architecture/`. The actual directory is `docs/Architecture/`. Their dated
text remains, with current catalog links using the actual path. No new missing
governance or milestone file was invented to satisfy a reference.

## Validation

Commands ran from `C:\Development\PiratePDF` in regular PowerShell unless a
permitted-context retry is stated. The evidence directory retains exact output.

| Command/check | Exit | Result |
|---|---|---|
| `Review-Markdown.ps1` | 0 | Entry inventory and complete-file topic scans across 276 files |
| `Export-MarkdownInventory.ps1` | 0 | 279 final file classifications, hashes, actions and reasons |
| `Check-MarkdownLinks.ps1` | 0 | Zero live source/library local-link or heading-anchor findings, 422 preserved snapshot findings |
| `dotnet run --project tools/PiratePDF.DocGen/PiratePDF.DocGen.csproj --configuration Release --no-build -- all` | 0 | 225 documents, zero errors/warnings/information |
| Same DocGen invocation with `health` | 0 | 91.4%, A-, zero errors/warnings |
| Same DocGen invocation with `repair` | 0 | Zero changes/files/diagnostics, no repairs applied |
| `tools/dependencies/Test-DependencyArtifacts.ps1 -ArtifactDirectory docs/dependencies` | 0 | Three schema-valid SBOMs, 21-package union, 39 reviewed identities and 117 DLL origins, zero review findings |
| `git diff --check` | 0 | No whitespace findings after correcting a touched Markdown hard-break line |
| `Test-Preservation.ps1` | 0 | 1,721 repository files compared, 220 protected Markdown files plus three dated RC1/narrative files checked, zero unexpected changes |
| `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1`, sandbox | 1 | Integrated source-size validation passes, restore fails in restricted context |
| Same unchanged gate, permitted-context retry | 1 | Restore/build pass with zero warnings/errors, 3,428 tests across 21 projects with 3,423 passed, five failed and zero skipped |

The temporary audit scripts above are retained under the ignored B13 evidence
directory. They produce the review/link/hash evidence and add no runtime tooling
or mandatory CI step. Current authored documents were reconciled manually using
those scans and the cited source checks.

The five gate failures occurred during Windows symbolic-link fixture creation
before the tested validators could run. Exact retained cases are

1. FindingStorageTests.ArtifactLinkEscapeIsRejected.
2. H2bStationAclApplicabilityTests.ReparseSubstitutionIsRejectedBeforeOwnerAdmission.
3. CorpusManifestTests.SeedLinkEscapeIsRejected.
4. CampaignTicketAdmissionTests.TicketDirectoryLinkIsRejectedAndForeignBytesPreserved.
5. CampaignRunnerTests.CampaignWorkspaceLinkIsRejected.

The gate stops before documentation. Separate DocGen validation passes as
recorded above. Its DocGen 88, Plugins 48, Skia 25 and Services 553 tests pass
within the new gate. Logs and exit files are `release-gate-sandbox.log`/`.exit`
and `release-gate-permitted.log`/`.exit`. The authoritative gate SHA-256 remains
`7D555BF893C2C2485969C67E2D95E00FE6CE119000B1E0688BFD7094BCFFFE38`.

The owner ran the requested single-line gate command and confirmed **the full
release gate passed** on 2026-11-08. This is user-reported B13 evidence, separate
from the agent's retained failed attempts. No additional test count or warning
count was supplied. Prior October 31 owner successes retain their historical
B11/B12 scope. Hosted CI has not been executed for this uncommitted B13 change.

Final acceptance combines the per-file review/classification, reconciled current
authority, explicitly recorded historical references, passing documentation and
preservation checks, and owner-confirmed full-gate success. The revised B13
documentation acceptance criteria are satisfied. POST-P2-006's actual official
signed-distribution acceptance remains partial pending the dependencies below.

## Remaining dependencies

Official signing qualification requires the actual Windows application and
installer, owner-approved production credential provisioning, isolated signing
implementation, final provenance/SBOM/notices and signature/tampering/failure
tests against those exact artifacts. The provisional desktop/packaging milestones
and V1 Release Preparation retain this work. B11 requires real startup/native
qualification on Windows 10/11 x64.

The form policy requires qualified runtime/event/Safe Mode implementation in a
future scope. Native merge/composition requires its own feature milestone.
The revised assignment defines documentation acceptance independently of those
future implementation dependencies. Acceptance of the reconciled documentation
can therefore be recommended after validation while official release integrity
under POST-P2-006 remains partial.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-11-08 | Record B13 Markdown audit, canonical form policy, Windows release provenance requirements, owner-preserved B12 completion and validation/acceptance boundaries. |
