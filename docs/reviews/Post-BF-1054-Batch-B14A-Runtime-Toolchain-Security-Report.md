# B14A Runtime and Toolchain Security Remediation

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-045 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Acceptance state

**B14A COMPLETE — B14-SEC-001 REMEDIATED FOR THE SELECTED INPUTS.**

Publisher-verified patched inputs, reconciled repository/CI selections,
successful affected publishes, auditing and regressions, plus the owner's
November 8 confirmation that the full gate passed through the corrected
patched-SDK launcher satisfy this batch's acceptance. The elevated success is
owner-reported. The retained agent gate has five privilege failures. B15 is
next under its own scope. B14's deployment evaluation remains complete under
its original .NET 10.0.10 environment.

## Security basis and selection

Microsoft's CVE-2026-111497 describes an out-of-bounds write in Brotli decoding
on affected platforms, including Windows. Affected .NET 10 compiler/runtime
versions are at least 10.0.0 and below 10.0.14. Servicing is required for affected
runtime inputs even though B14 did not establish a reachable PiratePDF exploit.
Framework-dependent deployments require patched shared runtimes and process
restart. Embedded self-contained/AOT runtime bytes require rebuild and
redeployment. [Publisher advisory](https://github.com/dotnet/runtime/security/advisories/GHSA-p3jp-9v83-76w9).

Official .NET 10 release metadata retrieved on November 8 identifies the latest
release as 10.0.14, dated October 25, and latest SDK as 10.0.403. These are the
selected stable .NET 10 inputs. The target framework remains net10.0.
[Release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json),
[Publisher release](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.14/10.0.14.md).

## Repository and machine inputs

The entry branch is maintenance/post-BF-1054-repository-review at
67f1e622024cb50e8be210d244e4ede2955f6f3c. Existing B13/B14 changes are retained.
Ignored evidence is under artifacts/fuzz/b14a-security-20261108-01.
The entry status and SHA-256 records identify preexisting changes.

| Input | Before | B14A selection |
|---|---|---|
| global.json SDK | 10.0.302, latestPatch | 10.0.403, latestPatch |
| CI setup-dotnet, both jobs | 10.0.x | 10.0.403 |
| Host/shared runtime used for validation | 10.0.10 | 10.0.14 |
| Ordinary net10.0 runtime framework | SDK-derived default | 10.0.14 unless explicitly supplied |
| AOT compiler restore | 10.0.10, NU1903 | 10.0.14 |

Directory.Build.targets applies the ordinary runtime selection after project
and publish-profile evaluation. This also raises framework-dependent generated
runtimeconfig minimums. The station-only provisioning profile retains its
explicit exact-runtime approval requirement and disabled roll-forward. Its
runtime property remains empty without an explicit selection. Qualification
and activation of deferred H2b remain outside this task.

The official Windows x64 SDK ZIP is extracted only into ignored local evidence.
Its SHA-512 matches the publisher metadata exactly.

```text
7A114C318FA7F1D9822991B534B2814D0E78386CEDA816B371F5141ACEFE741D87CCB606EDE06CADAC8F9AC5070E32B708A67BBD1619FF5E5A7858A27874EF48
```

sdk-provenance.json records URL, hash and verification time. dotnet-info-permitted.txt
records SDK 10.0.403, MSBuild 18.9.15 and runtime 10.0.14. Machine-wide .NET
installations remain the owner's separately serviced inputs.

## Validation evidence

The B14 DocGen executable and retained EngineWorkload are republished into
separate B14A framework-dependent and self-contained Windows x64 outputs.
The latter exercises 29 existing B10 conformance cases and six existing
security/plugin checks, including parse/render, text and embedded font edits,
save/reopen, ownership, rollback/undo/redo and capability denial. A separate
workload enumerates fonts through the existing Windows provider. Test fixture
dependencies remain development inputs.

The narrow DocGen AOT restore succeeds with auditing enabled and resolves
Microsoft.DotNet.ILCompiler/10.0.14 and
runtime.win-x64.Microsoft.DotNet.ILCompiler/10.0.14. aot-restore-permitted.log
and its exit record retain the result. The initial restricted-network restore
failure is retained separately. This probe establishes compiler-input servicing.
The B14 rejection of AOT for the dynamic managed-plugin host still applies.

Both completed deployment modes pass all 35 conformance/security/plugin cases
and Windows font enumeration of 497 faces. Loaded module inspection identifies
runtime 10.0.14 from the selected local SDK for framework-dependent execution
and from the publish directory for self-contained execution. Both modes load
their own packaged Skia DLL despite an owned invalid current-directory decoy.
The missing-native self-contained fixture exits 1 with DllNotFoundException.
Both published DocGen modes validate 228 documents with zero findings.

The runtime pack's 187 native/managed binaries in each self-contained output
match their 10.0.14 package bytes, covering 374 binary comparisons. The pack's
two framework-level JSON manifests are replaced by application-specific
manifests during normal publishing and are recorded as absent from both outputs.
The runtime pack's NuGet content hash is
Py+9fO4oGQwPrM2SDTOCZP1CburyfXradGLj49WFXFA1/awcgTOtY9Ao01P2mYTyEMY32W2WmQyftR3KlwJFfA==.

| Runtime/native binary | SHA-256 |
|---|---|
| coreclr.dll, both modes | 1BC65B8D45474B19ADF3CA85CE881087AFF599FFC72B2C1A323E878227757B65 |
| System.IO.Compression.Native.dll, both self-contained outputs | CE43BB776A6DF4ABE190D0E77096046475709F8E8B0C823E10CB7B39099C32A4 |
| libSkiaSharp.DLL, both modes | C8770C219E0D3CD9BB119FAD46F2D00AE1855B11315816E86E909E3332826212 |

The solution audit covers 45 projects. Separate audits cover the test plugin
and locked SharpFuzz project, reconciling all 47 repository projects. All audits
exit 0 with zero reported vulnerable packages. The locked SharpFuzz restore
succeeds. Package audit output and runtime/compiler identities are retained
separately so runtime servicing remains visible beyond ordinary package graphs.

The initial permitted gate builds with zero warnings/errors and records
3,420/3,428 passing tests, eight failures and zero skips. Five failures require
symbolic-link privileges. Three child-argument cases reach their existing
deadlines. A subsequent isolated permitted run passes all 19 argument tests.
The sequential gate rerun records five link-privilege failures and one fixture
process-lifetime failure. Its attached Process object can lose access to exit
information after teardown. The fixture now acquires its SafeHandle before
releasing the root. All original exit-code, orphaned-pipe, failed-cleanup and
unverified-execution assertions remain. All 44 process-supervisor/argument
regressions then pass with zero failures/skips. The post-repair unchanged gate
builds with zero warnings/errors and records 3,423/3,428 passes, five failures
and zero skips across 21 test projects. Its remaining failures are the same
five symbolic-link privilege requirements.

- CorpusManifestTests.SeedLinkEscapeIsRejected
- FindingStorageTests.ArtifactLinkEscapeIsRejected
- H2bStationAclApplicabilityTests.ReparseSubstitutionIsRejectedBeforeOwnerAdmission
- CampaignTicketAdmissionTests.TicketDirectoryLinkIsRejectedAndForeignBytesPreserved
- CampaignRunnerTests.CampaignWorkspaceLinkIsRejected

release-gate-final.log retains this failed agent run. The owner subsequently
reported that the full Administrator PowerShell gate passed using the corrected
local-SDK launcher. That success is separately attributed and has no agent-
observed elevated log. Prior B14 owner-reported gate success remains separate.
The owner's first elevated attempt selected the global SDK 10.0.302 and failed
restore because global.json requires 10.0.403. It supplies no patched-toolchain
gate success. The ignored Invoke-PatchedReleaseGate.ps1 launcher binds dotnet
directly to the verified local executable and verifies 10.0.403 before invoking
the unchanged gate. Its equivalent command resolution was checked separately.
The owner then reported “The full release gate passed” for this corrected run.
Documentation independently validates 228 documents with zero findings and
91.4% health (A-). Repair preview reports zero changes/files/diagnostics.
Entry preservation checks cover 32 preexisting changed/untracked files and
find zero unexpected modifications. Both historical B14 artifacts match their
entry hashes. The unchanged gate SHA-256 is
7D555BF893C2C2485969C67E2D95E00FE6CE119000B1E0688BFD7094BCFFFE38.

Patched-toolchain regeneration of all 86 existing dependency inventory/SBOM/
notice/evidence outputs is byte-identical to the reviewed B12 outputs. The
positive artifact validation and all seven schema/hash/notice/coverage/private-
path/upstream-evidence/review-identity negative scenarios pass unchanged.
The initial network-restricted SBOM generation failure is retained separately
from the permitted successful run. No B12 package evidence requires replacement.

## Files and provenance scope

Build changes are global.json, Directory.Build.targets and both SDK selections
in .github/workflows/continuous-integration.yml. Current guidance changes are
AGENTS.md, README.md, PP-DEV-003, PP-REL-001, PP-DEP-001, PP-AUD-009 and the
maintenance handoff and Windows desktop sequence, together with this report
and generated documentation indexes/health outputs. The directly implicated
NativeProcessSupervisorTests fixture retains its query handle before releasing
the root. Production source, public APIs, dependency versions and the
release-gate script are preserved.

The B14 report and measurement JSON remain historical .NET 10.0.10 evidence.
B12's reviewed package inventory and legal texts retain their original scope.
Future artifact SBOM/provenance must identify the SDK/runtime packs and actual
embedded runtime hashes in addition to application NuGet dependencies.

Actual desktop distribution/startup, official signing and installer servicing
remain future acceptance requirements. B11 and the signing qualification remain
partial. B9/H2b remain deferred, disabled and unqualified.
CI SDK configuration was reconciled locally. A new hosted CI execution is
outside the observed validation evidence. Previously installed global runtimes
and retained historical B14 outputs still carry their original versions and
require owner servicing or replacement before affected deployment/use.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-11-08 | Record B14A patched SDK/runtime selection, verified local provenance and security remediation validation. |
