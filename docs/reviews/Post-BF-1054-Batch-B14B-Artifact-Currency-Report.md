# B14B Package Rebuild and Artifact Currency

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-046 |
| **Category** | Engineering Review |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054, BF-1056, BF-1058, BF-8000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Acceptance state

**B14B COMPLETE — FINAL SOURCE-SERVICED CHECKPOINT ACCEPTED.**

Eight current development groups are rebuilt and verified. Outdated/unknown
outputs are explicitly blocked from distribution. The permanent policy,
reusable verification, security audits and applicable regressions pass. The
owner reported that the full gate passed through the local-SDK launcher on
November 8. That elevated result is owner-reported and separate from the
agent's retained five privilege failures. The subsequent owner-approved SBOM
tool source rebuild resolves its newly discovered package advisories. The final
agent gate again has only the five link-privilege failures. The owner separately
reported that the full release gate passed for this final source-serviced
checkpoint. B14A remains complete. B15 follows B14B, then final B16 and BF-1055 under
their own scopes.

## Authority, scope and provenance

The owner's B14B assignment authorizes accessible artifact inventory, isolated
servicing rebuilds, permanent currency policy, reusable verification and current
documentation reconciliation. The owner additionally selected inclusion of the
Skia patch review/upgrade and its licensing/SBOM/compatibility validation.
After the tool-bundle audit exposed Pkcs 8.0.1, the owner approved rebuilding
the exact CycloneDX source with Pkcs 8.0.2.

The entry branch is maintenance/post-BF-1054-repository-review at
67f1e622024cb50e8be210d244e4ede2955f6f3c with retained B13/B14/B14A working-tree
changes. Entry status and file hashes are retained under ignored evidence
artifacts/fuzz/b14b-currency-20261108-01. Exact source/build inputs and current
artifact file hashes are recorded separately. Changes remain scoped to
servicing configuration/lock entries, accountability outputs, verifier scripts,
governance and current documentation. Production C# and public APIs are retained.

## Secure baseline and native patch review

Fresh Microsoft metadata on November 8 still identifies SDK 10.0.403 and runtime
10.0.14 as latest .NET 10 servicing inputs. B14A's SDK ZIP checksum is reverified
against its retained publisher provenance. net10.0, global.json, runtime
selection, CI SDK setup, deterministic builds and warnings/audit policies remain.
The narrow AOT restore uses ILCompiler 10.0.14 with auditing enabled.
[Microsoft release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json),
[Runtime advisory](https://github.com/dotnet/runtime/security/advisories/GHSA-p3jp-9v83-76w9).

The publisher's documentation lists 4.150.x as a supported stable branch and
4.150.5 as its latest documented patch. NuGet's authoritative version/catalog
metadata additionally lists stable 4.150.7, published October 31. The existing
4.150 branch is retained and serviced to its latest verified patch. Other
release lines and prereleases require their own compatibility decision.
[Publisher support overview](https://mono.github.io/SkiaSharp/docs/releases/index.html),
[Published patch](https://www.nuget.org/packages/SkiaSharp/4.150.7).

All four exact packages pass Microsoft author and NuGet repository signature
verification. Manifests identify immutable source commit
78140838b2661f7a458d8463b27f6d2400405fbe, which is verified through the publisher's
GitHub API. The tag release endpoint returns 404 and is retained as such. The
published package and immutable source evidence establish this review's origin.
The source comparison covers 24 commits and 236 files, including native Skia
M150 servicing and Ganesh upload checks, build infrastructure and RAW/DNG
removal. It contains no binding-directory changes. PiratePDF's sole SKCodec
creation boundary is the documented JPEG/DCT codec. RAW/DNG are outside that
supported codec contract. Applicable image/render and public-boundary tests
remain required. [Immutable source](https://github.com/mono/SkiaSharp/commit/78140838b2661f7a458d8463b27f6d2400405fbe).

| Input | Before | Serviced selection |
|---|---|---|
| SDK | B14A 10.0.403 | 10.0.403, explicitly selected local executable |
| Runtime/compiler | B14A 10.0.14 | 10.0.14 |
| SkiaSharp | 4.150.1 | 4.150.7 |
| SkiaSharp.NativeAssets.Linux/macOS/Win32 | 4.150.1 | 4.150.7 |
| Unique repository NuGet identities | 21 | 21, four version replacements |
| Windows x64 Skia SHA-256 | C8770C219E0D3CD9BB119FAD46F2D00AE1855B11315816E86E909E3332826212 | 832EDF69408297A3AD77AB511273EF59937BA4445C1B65B8717D65215AA0364C |

The two central Skia pins and their affected SharpFuzz lock entries change.
The lock also records the rebuilt utility's win-x64 runtime asset graph.
Locked restore succeeds. The four packages retain MIT declarations and their
seven packaged legal texts are byte-identical to previously reviewed texts.
No unrelated package identity changes. Regeneration validates 47 project graphs,
21 package identities and 20 native assets, with three CycloneDX SBOMs and
updated notices/archive/native hashes. Eight original B12 accountability records
are preserved byte-for-byte under docs/dependencies/history/b12-20261031.
Existing legal texts, immutable evidence and completed reports remain intact.

## Source-serviced SBOM tool security remediation

The final developer-tool scan finds two advisories for the Pkcs 8.0.1 DLL in
all three targets of the latest publisher tool, CycloneDX 6.3.1. Microsoft
identifies CPU-exhaustion issues in SignedCms.CheckSignature (high severity,
all platforms) and EnvelopedCms.Decode (moderate severity, Windows). Both are
patched in Pkcs 8.0.2. Updating the shared .NET runtime leaves this bundled
package unchanged. Original findings and the November 8 NuGet feed snapshots
remain retained. [Microsoft advisory CVE-2026-84008](https://github.com/advisories/GHSA-846f-f764-76mx),
[Microsoft advisory CVE-2026-70162](https://github.com/advisories/GHSA-2fmv-rv2c-59x7).

The owner explicitly approves a source rebuild. Build-ServicedSbomTool.ps1
downloads the checksum-pinned archive for upstream commit 6938a02734b0c30e1a59976f5bd592f1ee8be1eb,
adds the sole Pkcs 8.0.2 central transitive pin with a modification notice,
and builds net10.0/Windows x64 using SDK 10.0.403, runtime floor 10.0.14 and
LatestPatch roll-forward. The identity is PiratePDF.CycloneDX.Serviced
6.3.1-piratepdf.b14b.1. Original Apache LICENSE/NOTICE and a servicing notice
accompany the development output. The original vendor bundle is retained and
blocked from current execution.

Two isolated publishes produce identical 41 runtime DLL/JSON/executable files.
All 24 package identities match the original net10 graph with the sole approved
Pkcs version replacement. The restored Pkcs archive, active Windows DLL and
source-built main assembly are pinned and validated. Source package audit and
independent comparison against retained NuGet advisory ranges both report zero
current findings. Pkcs 8.0.2's four legal texts match original reviewed bytes.
Other package versions and the exact owner-reviewed json-everything terms remain.

Upstream .NET 10 tests discover 358 cases, with 356 passes, zero failures and
two existing skips. The skipped GitLicense_FromVersionTag and multi-framework
functional case retain their upstream attributes. An initial run used a shared
artifacts layout incompatible with a fixture's four-parent relative path. It
records one failure. The unchanged tests pass in their expected bin layout
inside the isolated source checkout. Docker E2E is outside this validation scope.

Export-Sbom refuses original/unknown generators before execution and verifies
the source-serviced producer, legal evidence and binary origins. Current SBOMs
are regenerated with the new identity. Existing CI provisioning builds this
pinned variant and retains its blocking audit/provenance checks. This narrowly
repairs the known tool dependency. Generic hosted currency-gate parity remains
B15 work. The maintenance obligation is the pinned recipe, reproducibility and
dependency review until an audited compatible publisher replacement is adopted.

## Artifact inventory and replacement matrix

[B14B artifact inventory](B14B-Artifact-Inventory.json) records all 217 accessible
runtime configurations discovered at entry, 172 unmatched/native executables,
37 package containers, 47 local project build caches, machine/global and
source/deferred/unavailable scopes, plus eight current rebuild groups. These
records include overlapping component/group scopes. Entries carry actual framework/dependency
identities and adjacent Windows native hashes where available. Missing source,
SDK or compiler provenance is explicitly unknown. No artifact is executed merely
to discover its identity. Prior local build outputs and historical/test packages
are blocked from current distribution. A new source pin cannot service old bytes.

| Artifact group | Servicing action and classification |
|---|---|
| DocGen framework-dependent/self-contained | Fresh isolated B14B rebuilds, development only |
| Engine integration workload framework-dependent/self-contained | Fresh isolated B14B rebuilds using copied existing B10/security/plugin fixtures, development only |
| Fuzz runner and managed SharpFuzz utility | Fresh isolated framework-dependent rebuilds, development only; native H2b remains disabled |
| Source-serviced SBOM producer | Distinct net10.0 development variant with Pkcs 8.0.2, reproducible bytes and source/package provenance; original vendor bundle blocked |
| Engine, SDK, Services, reference plugin and solution fixtures | Fresh isolated serviced solution build; compile/test artifacts only |
| Original B14 .NET 10.0.10 outputs | Historical evidence, preserved and excluded from current distribution |
| B14A accepted outputs and negative fixtures | Historical accepted evidence, preserved with original Skia 4.150.1 identities |
| Other local bin/obj/copied test outputs | Unsupported for distribution until rebuilt and verified; unknown provenance remains unknown |
| Station-only/native H2b tooling | Qualification/production execution remains deferred and blocked under PP-GOV-004 |
| Official desktop, installer and signed packages | Actual artifacts absent from this checkpoint; qualification remains open |
| Externally retained/published artifacts | No external package set supplied; remediation/qualification remains unresolved |

The current-artifacts.json evidence manifest lists every file/hash in eight
current development groups, their project/build identity, source-input snapshot,
SDK executable, compiler assets, publisher metadata, audit scopes and applicable
SBOM/notices/provenance digests. Existing Publish-Modes.ps1 commands use separate
output/intermediate roots. Rebuild and verification logs are retained beside it.

The first published SharpFuzz utility refuses startup with FileNotFoundException
for SharpFuzz. SDK 10.0.403's _ComputePackageReferencePublish target excludes
PrivateAssets=all references unless their Publish metadata is explicit. The
existing private reference now sets Publish=true. This preserves the tooling
dependency boundary while including its three runtime assemblies in the
44-file publish. The rebuilt artifact passes registry-direct with shared-memory
coverage uninitialized, as expected for the managed diagnostic. The runner's
no-argument invocation produces its existing Usage record and exit 2. No native
driver or production worker is activated. Initial failure and passing replacement
logs are retained separately.

## Permanent policy and reusable verification

[PP-GOV-007](../05-Governance/Runtime-Dependency-and-Artifact-Currency.md) establishes
project-wide ownership, servicing cadence, runtime/package/native obligations,
historical/current classification, release blocking and owner exceptions with
defined review/remediation conditions. It applies to engine, SDK, Services,
plugins, tools, CI and future Windows distributions. No exception is adopted
for retaining the outdated Skia patch.

Test-ArtifactCurrency.ps1 uses the reviewed artifact-security-baseline.json and
trusted maintainer-created manifests. It verifies actual SDK selection and
global/central versions, publisher metadata consistency, source/evidence hashes,
all-project vulnerability-audit coverage, compiler identities/audit settings,
runtimeconfig requirements, embedded runtime binaries, native identities and
complete output file coverage. Relative path containment, duplicate records
and reparse-point evidence are checked. Failures produce nonzero exits and
actionable diagnostics. This integrity tool does not sandbox concurrent file
replacement or authenticate an attacker-supplied manifest.

Test-ArtifactCurrencyVerification.ps1 supplies a positive verification and
fourteen expected-failure mutations covering SDK/source/compiler/audit evidence,
hash/coverage/duplicate/path errors, insecure runtime floors and runtime/native
binary identity. B15 owns later hosted-CI integration and observed parity.
Current local tooling does not establish hosted enforcement or official signing.

## Validation and limitations

Both serviced engine modes pass 35 conformance/security/plugin cases and Windows
font enumeration. Module inspection records the actual selected runtime and
packaged Skia bytes despite an owned invalid current-directory decoy. Both
modes enumerate 497 faces. All 374 checked runtime-pack binaries in the two
self-contained outputs match reviewed 10.0.14 package bytes. The missing-native
fixture exits 1 with DllNotFoundException. Published utility checks pass after
the packaging repair described above.

Currency verification passes all eight groups and 48 audited project scopes. Its
positive and fourteen negative scenarios pass final checkpoint validation. The existing dependency pipeline
passes structural/legal/provenance checks and all nine negative mutations.
Three audit scopes cover 45 solution projects plus the separately scoped
SharpFuzz and test plugin projects, with zero reported vulnerable packages.
The locked utility restore and narrow compiler audit both pass.

The unchanged authoritative gate builds with zero warnings/errors. Its final source-serviced agent
run discovers 3,428 tests across 21 test projects, with 3,423 passes, five failures
and zero skips. One console line interleaves two project summaries, so totals
use every summary match rather than one match per line. The five failures are
the required symbolic-link privilege fixtures.

- CorpusManifestTests.SeedLinkEscapeIsRejected
- FindingStorageTests.ArtifactLinkEscapeIsRejected
- H2bStationAclApplicabilityTests.ReparseSubstitutionIsRejectedBeforeOwnerAdmission
- CampaignTicketAdmissionTests.TicketDirectoryLinkIsRejectedAndForeignBytesPreserved
- CampaignRunnerTests.CampaignWorkspaceLinkIsRejected

Before the source-serviced tool change, the owner reported “The full release gate passed” after running
Invoke-PatchedReleaseGate.ps1 from Administrator PowerShell. The launcher binds
dotnet to the verified local SDK and checks 10.0.403. The agent did not observe
an elevated execution log. After the source rebuild, the owner again reported
“The full release gate passed” for the final source-serviced checkpoint using
the same launcher from Administrator PowerShell. This final result is also
owner-reported. The gate remains byte-identical with SHA-256
7D555BF893C2C2485969C67E2D95E00FE6CE119000B1E0688BFD7094BCFFFE38.

Independent DocGen validation covers 230 documents with zero findings. Repair
preview reports zero changes/files/diagnostics. Whitespace checks pass. Entry
preservation covers 1,730 files with zero unexpected modifications, including
unchanged B12/B13/B14/B14A reports, B13 inventory and B14 measurements. All eight
preserved B12 inventory copies match their original hashes. Initial restricted-
network/tool-context failures and the first refused utility publish are retained
alongside the permitted passing replacements.

Official Windows 10/11 installation/startup, signed distribution and installer
servicing remain B11/PP-REL-001 acceptance gaps. Linux/macOS support is deferred
to community qualification. External packages, other machine installations and
hosted CI have their own unresolved execution/servicing scope. The machine-wide
SDK remains 10.0.302 and shared runtimes include 6.0.32/33, 8.0.22, 9.0.20 and
10.0.9/10.0.10. These cannot satisfy the selected project SDK/runtime floors.
Owner-managed machine servicing remains outstanding. Native H2b stays
disabled/unqualified and B9 remains open. AOT/trimming dispositions remain as
accepted under B14. Historical runtime/native outputs remain historical.

## Documentation reconciliation

The [B14B documentation inventory](B14B-Documentation-Reconciliation.json) records each reviewed current
document, its retained/modified disposition, rationale and before/after hashes.
Current README/instructions, audit/handoff, roadmap/desktop plans, deployment,
release, dependency/legal and governance guidance are reconciled to
B14A complete, B14B complete, B15, B16 and BF-1055. Generated indexes/health and
validation reports follow DocGen. Completed B12/B13/B14/B14A reports and
historical inventories retain their original content and attribution.

Final Windows PowerShell regeneration reproduces all current accountability
files byte-for-byte. A fresh build using the final source-servicing recipe also
reproduces the selected tool's runtime files and provenance exactly. The final
preservation check covers all 1,730 entry files with zero unexpected changes
and verifies all eight historical B12 copies. The accessible artifact inventory
contains 487 records with overlapping runtime, executable, container and
aggregate scopes.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-11-08 | Record B14B supported artifact servicing, permanent currency policy, reusable verification and historical preservation. |
