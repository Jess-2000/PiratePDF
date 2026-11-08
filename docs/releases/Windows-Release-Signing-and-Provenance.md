# Windows Release Signing and Provenance

| Field | Value |
|---|---|
| 1.1 | 2026-11-08 | Record scoped B14 deployment evidence, metadata/plugin incompatibilities, toolchain security servicing and future desktop/packaging implications. |
| **Document ID** | PP-REL-001 |
| **Category** | Release Documentation |
| **Version** | 1.3 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054, BF-1056, BF-1058, BF-8000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Current implementation and release scope

Windows 10/11 x64 is the official v1 platform under PP-GOV-005.
Production certificate provisioning and an official signing/publishing pipeline
remain deferred. The current checkout contains engine libraries, developer
tools and tests. A desktop executable and final packaging format await their
future milestones. Existing CI builds/tests and B12 inventory/SBOM validation
provide development evidence.

This document defines the approved release acceptance contract. Signing,
timestamping, credential-provider integration and final-artifact verification
need separately scoped implementation and direct qualification.
BF-1056 through BF-1058 retain provisional numbering pending registry review.

## Development and official release identity

Ordinary development builds may be unsigned and must be labeled development
artifacts. Any deliberately test-signed fixture must identify its test publisher
and validation scope. Test identities require isolated, explicitly approved
setup and cannot be represented as the official publisher identity.

An official release must identify the approved publisher, reviewed source
commit, build execution and final immutable artifact set. Windows application
executables, installer/bootstrapper files and first-party binaries selected by
the final packaging policy require Authenticode qualification. Record existing
third-party signatures and notices with their publisher provenance. Any
first-party signing exception requires a documented release decision.

## Source-to-artifact provenance record

PP-GOV-007 supplies the permanent runtime/dependency and artifact currency
policy. Official release qualification must stop when SDK/runtime/compiler,
NuGet/native inputs or artifact provenance fail its reviewed security baseline.
Rebuild and redeploy affected self-contained packages, validate framework-
dependent minimum secure runtimes and installed-runtime servicing, and retain
old packages as explicitly historical or blocked. Refresh final SBOM/notices,
hashes, regression and signing evidence after servicing. B14B's local verifier
is supplied for later B15 hosted integration, whose enforcement remains open.
[Artifact currency policy](../05-Governance/Runtime-Dependency-and-Artifact-Currency.md).

For each future official release, retain a versioned, reviewable provenance
record containing these fields.

| Record group | Required evidence |
|---|---|
| Source identity | Repository URL, full commit SHA, approved ref/release version, checkout cleanliness and any explicitly approved source patch hashes |
| Build identity | Workflow file revision, run/job identifier and URL, UTC build time, builder/runner identity and Windows image, SDK/runtime/tool versions, build/publish commands, target RID and configuration |
| Build inputs | Restore sources, resolved package identities/hashes, approved tool versions, native asset provenance, relevant configuration hashes and dependency review version |
| Artifact identity | Relative file name, product/version, role, size and SHA-256 for each final artifact and package |
| Signing identity | Approved publisher, public signer certificate identity/thumbprint, verification result, timestamp authority/evidence and verification tool/version |
| Qualification | Full gate result with its source/build identity, signature and tampering checks, clean Windows 10/11 x64 installation/startup evidence and named release approver |
| Accountability | Exact packaged SBOM and notices with SHA-256, provenance record identifier and retained verification logs |

Compute SHA-256 after signing and after producing the final installer/archive.
Signing changes file bytes. Track pre-sign and post-sign digests separately if
both are retained. Bind the final manifest to the approved authenticated
release channel and immutable build identity. A digest comparison establishes
byte integrity relative to that recorded manifest.

The existing engine, solution and SharpFuzz SBOMs remain scoped B12 evidence.
The actual desktop package requires a distribution-specific inventory and SBOM
that matches the files selected for that release, including native assets and
applicable third-party notices. The package's provenance must reference both
the exact accountability records and its final file hashes.

## Credential isolation

Provisioning requires an explicit owner decision on publisher identity,
certificate/key custody, authorized signing service or hardware/provider,
rotation/revocation, access approvers and cost. Prefer a protected signing
boundary with narrowly scoped release access and auditable operations.

Keep private keys, PFX files, passwords, tokens and credential exports outside
the repository, ordinary build outputs, caches, logs and uploaded artifacts.
Public certificate identifiers may appear in provenance. Untrusted pull-request
code must never receive signing credentials or execute inside a privileged
signing context. Admission must bind the approved source/build and exact
artifact digests before signing, followed by independent verification.
Avoid commands that print secrets or expose them through command arguments.

Production credentials, certificate creation, trust-store changes, signing
service accounts and publication require their own concrete authorization.
Existing content-read-only CI remains a development verification workflow.

## Signature verification and failure handling

The future release path must verify every intended signature using Windows
Authenticode policy, require the approved publisher identity and check the
timestamp evidence and certificate chain under the selected trust policy.
Use SHA-256 file and timestamp digest algorithms in the selected signing flow.
Retain the exact verifier output and its exit code.

With an explicitly provisioned Windows SDK SignTool, the verification form is

```powershell
signtool.exe verify /pa /all /tw /v '.\staging\PiratePDF.exe'
```

The filename is a future artifact placeholder. `/pa` selects Authenticode
verification, `/all` checks every signature, and `/tw` warns about a missing
timestamp. The release wrapper must fail on verification warnings as well as
errors and verify the expected signer separately. The PowerShell
Get-AuthenticodeSignature command can expose signer and timestamp certificates
for additional recorded checks. The Windows trust configuration and chosen
revocation behavior must be recorded, including offline/unknown results.
These command semantics follow Microsoft's documentation.
[SignTool reference](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool),
[Get-AuthenticodeSignature reference](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/get-authenticodesignature).

Reject unsigned files, unexpected publishers, changed bytes, missing or invalid
timestamp evidence, invalid chains and failed/unknown required checks.
Retain failed artifacts and diagnostic evidence in restricted staging under
the approved retention policy. Publication must stop before artifacts become
official. Retrying requires correcting the cause and regenerating/verifying
the affected artifact and manifest coherently.

Qualification must include owned tampered-byte, missing-signature,
wrong-publisher and timestamp-failure fixtures, plus final clean-machine
installation/startup and native loading. A passing developer gate supplies
build/test evidence for its checkpoint. Official signing acceptance requires
the final signed distribution and its own verification results.

## Dependencies and continuation

B14's scoped deployment experiments retain the framework-dependent untrimmed
developer baseline. Self-contained packaging requires an explicit runtime
servicing/rebuild policy and actual application qualification. Trimming fails
current metadata/plugin analysis, while Native AOT conflicts with runtime-loaded
managed plugins. Its original .NET 10.0.10 compiler restore was security-audit
blocked. B14A selects SDK 10.0.403 and runtime/compiler 10.0.14 and records its
separate completed validation and owner-reported elevated full-gate acceptance.
The retained agent run has five link-privilege failures. Retain exact runtime/toolchain versions and
refresh final artifact hashes/SBOM/provenance after servicing. A shared-runtime
update requires process restart, while self-contained artifacts require rebuild
and redeployment. Record actual embedded runtime files and pack identities.
[B14A security remediation](../reviews/Post-BF-1054-Batch-B14A-Runtime-Toolchain-Security-Report.md).
[Deployment compatibility](../04-Developer/Deployment-Compatibility.md).

B11 remains partial pending actual desktop distribution/startup qualification.
The provisional BF-1056 foundation and BF-1058 packaging/installer workstreams
must supply the executable, packaging inputs and clean Windows qualification.
V1 Release Preparation applies this contract to the selected final artifacts.
B14's deployment evaluation is complete. B14A security servicing precedes
B15 gate parity, with
B16 final maintenance before BF-1055. Linux/macOS remain deferred
community-oriented targets under PP-GOV-005.

## Related guidance

- [Platform and native lifetime](../05-Governance/Platform-Support-and-Native-Lifetime.md)
- [B12 dependency accountability](../dependencies/README.md)
- [Desktop sequence](../02-Roadmaps/Windows-Desktop-Development-Sequence.md)
- [Current maintenance handoff](../handoffs/PiratePDF-Maintenance-Handoff-Post-BF-1054-Part1.md)

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.3 | 2026-11-08 | Apply B14B permanent artifact currency policy and servicing/verification guidance. |
| 1.2 | 2026-11-08 | Record approved B14A SDK/runtime security servicing and separate validation/acceptance evidence. |
| 1.0 | 2026-11-08 | Define development/official signing identity, source-to-artifact provenance, final SHA-256 records, credential isolation and future qualification with deferred production provisioning. |
