# Runtime, Dependency and Artifact Currency

| Field | Value |
|---|---|
| **Document ID** | PP-GOV-007 |
| **Category** | Governance |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054, BF-1056, BF-1058, BF-8000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Authority and applicability

The owner's B14B assignment establishes this permanent policy. The project
owner owns baseline approval, exceptions and release acceptance. Maintainers
collect publisher evidence, evaluate compatibility, rebuild affected artifacts
and retain verification results. Release maintainers enforce qualification
against the reviewed baseline before distribution.

This policy applies to BlackFlag Engine, PiratePDF, SDK, Services, plugins,
developer utilities, CI outputs, native dependencies and future Windows desktop
distributions. PP-GOV-005 retains Windows 10/11 x64 as the official v1 target.
It complements PP-DEP-001/002's legal/provenance controls and PP-REL-001's signing
contract. PP-GOV-004's native H2b deferral and PP-GOV-006's document-script policy
retain their independent security requirements.

## Currency and review cadence

Check upstream supported security-servicing releases during every scoped
maintenance batch, before every release candidate and on relevant security
advisories. Record review date, publisher URL, affected/fixed ranges, platform
applicability and the selected compatible baseline. Recheck changed publisher
metadata before qualification. A clean package audit alone cannot establish
native-library support or absence of unreported vulnerabilities.

Promptly assess applicable advisories. Block affected supported artifacts from
release qualification until remediation or an explicit approved exception.
Use the latest verified compatible supported servicing inputs. Review breaking
changes, transitive graphs, licensing, native ABI/behavior and compatibility
before adoption. Freshness does not authorize unvalidated major upgrades.

## Runtime and dependency obligations

- Track the supported .NET SDK, shared runtime, runtime packs and compiler
  inputs separately. Preserve the selected target framework unless separately
  approved. Retain warnings-as-errors, deterministic settings and NuGet auditing.
- Rebuild and redeploy affected self-contained packages after embedded runtime
  security fixes. Verify actual packaged runtime binaries and their hashes
  against the reviewed pack. Updating a machine's shared runtime cannot service
  those embedded bytes.
- Framework-dependent packages must declare and validate their minimum secure
  runtime and compatible roll-forward behavior. Installed-runtime owners must
  service the applicable architecture/runtime and restart consuming processes.
  A repository-local SDK services only executions that actually select it.
- Audit direct and transitive NuGet dependencies at the review cadence. Include
  separately scoped projects and development tools. Apply compatible supported
  fixes and review changed legal/provenance obligations before distribution.
- Verify native publisher/package origin, versions, hashes, advisory and support
  status, RID/architecture and actual loading/fallback behavior. Retain required
  notices. Unknown or inaccessible native provenance blocks qualification.
  Loaded modules may remain process-resident under PP-GOV-005. Replacing loaded
  native modules requires process restart and revalidation.

## Artifact lifecycle and replacement

Classify accessible artifacts as current development, qualified distribution,
historical evidence or unsupported/blocked. Record location, purpose, source
commit and working-tree inputs, SDK/runtime/compiler and NuGet/native identities,
deployment mode, verification status and replacement/blocking disposition.
Maintain original-to-replacement relationships when rebuilding.

Use isolated output/intermediate directories. Preserve historical, signed and
published artifacts. Old bin/obj outputs and copied packages do not inherit a
new baseline merely because source configuration changed. Preserve evidence
while explicitly excluding outdated/unknown outputs from current distribution.
Inaccessible external artifacts remain unresolved and blocked from any claimed
qualification until their actual bytes and provenance are available.

## Verification and release enforcement

The reviewed machine-readable baseline and Test-ArtifactCurrency.ps1 provide
reusable checks for SDK selection, runtime/compiler inputs, runtimeconfig
minimums, embedded runtime and native hashes, audit results and complete artifact
file provenance. Inputs are trusted maintainer-created evidence. File hashes
detect drift relative to that evidence. Publisher trust, legal review,
compatibility, signing and security qualification remain separate evidence.
Do not execute untrusted artifacts merely to inventory them.

Release qualification must fail when selected tooling, packs, compilers,
dependencies, manifests or artifact bytes fail the approved baseline, required
evidence is missing, audits fail, or a required security check is unknown.
Successful currency checks establish only their stated scope. Official release
acceptance additionally requires applicable regression/full-gate success,
final-artifact SBOM/notices/provenance verification, signing qualification and
actual installation/startup/native-loading checks under PP-REL-001 and B11.

B14B supplies local reusable verification and its evidence. B15 owns hosted-CI
integration and parity. Until integrated and directly observed, hosted enforcement
remains an explicit qualification gap. Existing blocking CI/gate controls remain
in force. Development-only outputs cannot be represented as an official release.

## Source-serviced development tools

B14B's owner-approved SBOM tool source rebuild is a separately identified
development variant, with upstream commit/archive checksum, the sole Pkcs
8.0.2 dependency pin, preserved legal notices, secure .NET 10 runtime floor,
reproducible runtime bytes and audited dependency origins. The original
CycloneDX 6.3.1 bundle remains blocked because all its targets contain the
affected Pkcs 8.0.1 component. It receives no vulnerability exception. Existing
CI SBOM provisioning uses the source-serviced producer. Maintainers must review
upstream releases/advisories at the normal cadence, maintain the pinned recipe
and reassess this variant when an audited compatible publisher replacement
becomes available. Generic hosted artifact-currency integration remains B15 work.

## Exceptions

Only the owner may approve an exception. Record the exact component/artifact,
affected versions, reason, known risk, permitted use, distribution restrictions,
compensating controls where applicable, approval evidence and a defined review
date or remediation trigger. An exception cannot silently waive another mandatory
security or legal acceptance requirement. On expiration or trigger, reassess
before use and block qualification until resolved. Unknown scope or absent
approval supplies no exception. Preserve historical exception decisions with
their original evidence and date.

## Related guidance

- [Dependency inventory and SBOM procedure](../dependencies/README.md)
- [License and provenance guidance](../dependencies/License-Provenance-Resolution.md)
- [Deployment compatibility](../04-Developer/Deployment-Compatibility.md)
- [Release signing and provenance](../releases/Windows-Release-Signing-and-Provenance.md)
- [Maintenance handoff](../handoffs/PiratePDF-Maintenance-Handoff-Post-BF-1054-Part1.md)

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-11-08 | Establish owner-authorized runtime, dependency and artifact currency policy under B14B. |
