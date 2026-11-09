# Post-BF-1054 Batch B12 - Dependency Provenance and SBOM

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-042 |
| **Category** | Engineering Review |
| **Version** | 1.2 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050, BF-1054 |
| **Last Updated** | 2026-10-31 |
| **Next Review** | 2027-01-31 |

## Disposition

**B12 COMPLETE — DEPENDENCY PROVENANCE AND SBOM VERIFIED**

All 13 package findings and the generator bundle's licensing/provenance evidence
are now reconciled. The owner confirmed the full remediation release gate passed
after the five agent-observed symbolic-link privilege failures. Structural validation
and the owner's use decision are not a general legal-compliance opinion.
POST-P2-005 is closed for this repository checkpoint. No dependency version was upgraded or replaced.
There is no desktop entry project, so no application graph or distribution
SBOM is invented. Windows 10/11 x64 remains the sole official v1 target.

## Licensing and provenance remediation — 2026-10-31

The follow-up assignment authorizes focused evidence remediation. Its entry
branch/HEAD match the baseline below; full status and hashes of every preexisting
modified/untracked file are retained in
`artifacts/fuzz/b12-licensing-20261031-01/entry-status.txt`, `entry-hashes.json`,
`branch.txt` and `head.txt`. Prior B9/B10/B11 evidence and original SBOMs remain.

[PP-DEP-002](../dependencies/License-Provenance-Resolution.md) supplies the full
**13-row repository resolution matrix** and **26-row tool-component matrix**.
[license-review.json](../dependencies/license-review.json) retains exact
archive/source identities, commit/URL references, SHA-512 archive hashes,
SHA-256 legal/manifest evidence, per-component obligations and usage decisions.
All 13 findings are individually reconciled with authoritative historical
evidence. Missing packaged standalone text was not itself a license violation.

The official xunit.abstractions 2.0.3 tag and original archive license URL are
cross-checked with pre-publication xUnit license history, establishing historical
Apache-2.0 attribution without applying today's license retrospectively. The
tag's frozen nuspec still says 2.0.1; that limitation is explicitly retained,
not represented as reproduction of the 2.0.3 binary build. SharpFuzz.Common's
MIT declaration is supported by the official v2.2.0 annotated tag and license.

The tool graphs contain 25 identities each and **26 unique identities** in
their union, including CycloneDX itself: Humanizer.Core 2.14.1 is shipped for
net8, 3.0.1 for net9/net10. All **117 DLLs**, including culture resources,
match original archive entries; dependent DLLs match a dependency archive in
their target graph, not only the enclosing tool archive. All 123 runtime
DLL/config hashes and the pinned tool archive hash remain recorded.

Json.More.Net 3.0.0, JsonPointer.Net 7.0.0 and JsonSchema.Net 9.1.1 declare
OSMFEULA.txt. Their complete binary-use agreements and MIT source license text
are preserved separately. After disclosure, the owner explicitly confirmed:
**Owner confirms non-revenue-generating use and accepts the documented terms**
on 2026-10-31. No blanket Apache/MIT-only classification, legal opinion, payment
or revenue-generating-use approval is inferred. Renew review if use generates
revenue, terms change or a different tool/build is selected. No licensing or
provenance decision remains unresolved for the current approved use.

The repository now preserves **33 unique legal texts** (including all six
original packaged texts) and **45 manifest/historical evidence files**.
Notices link original bytes and sources. Existing package declarations remain
unchanged; original missing-metadata findings are retained separately from
their reviewed resolution. No production dependency, native asset, runtime
behavior, public API, H2b policy or package version changed.

Regeneration consumes reviewed evidence offline, verifies hashes and exact
archive correspondence, and retains the original three SBOM scopes. CI now
retains supporting evidence files with its existing accountability upload;
no full-gate duplication or weakened controls were introduced. Hosted CI has
not been executed against this uncommitted tree.

### Remediation validation

All commands ran from `C:\Development\PiratePDF` in regular PowerShell,
with permitted-context retries only where cache/network access required them.
Full commands/logs and retained negative fixtures are in the remediation
evidence directory; PP-DEP-001 contains copy/paste regeneration commands.

- Original archives were downloaded as evidence, never installed as dependencies.
  Publisher texts were retrieved at manifest-declared commits or official
  historical tags; no current-branch substitution was used.
- `Export-DependencyInventory.ps1 -OutputDirectory docs/dependencies`: exit 0,
  47 projects/21 package identities, zero unresolved package-review findings.
- Pinned `Export-Sbom.ps1`: initial sandbox cache lookup failed; permitted retry
  generated all outputs. The new validator initially exposed a satellite-resource
  target lookup defect; corrected to use the enclosing target graph. Final full
  generation exits 0, schema-valid solution/engine/SharpFuzz counts **18/4/7**.
- `Test-DependencyPipeline.ps1`: exit 0; original **five negative mutations**
  still reject with exit 1, plus **two new** evidence/identity mutations reject.
- Repeat inventory and full pinned generation: **86 evidence files, zero byte
  differences**, `repeat-before.json` and `repeat-result.json`.
- All three original SBOMs and `native-assets.json` are **byte-identical to the
  remediation entry baseline**, `sbom-native-preservation.json`.
- New unchanged full-gate run: sandbox restore denied cache/config access,
  exit 1. Permitted retry builds with **zero warnings/errors**, then reports
  **3,428 tests: 3,423 passed, five failed, zero skipped**, exit 1. Same five
  symbolic-link fixture-creation failures listed below; documentation stage
  is not reached. All affected suites pass, including Plugins 48, Skia 25,
  DocGen 88 and Services 553. Logs: `release-gate-sandbox.log` and
  `release-gate-permitted.log`, with exact exit files.
- The owner's earlier October 31 full-gate success remains separate user-reported
  evidence for the preceding B12 checkpoint. The first follow-up command was
  pasted as one line with Set-Location and failed before launching the gate;
  a corrected single-line gate command was supplied. The owner subsequently
  confirmed **the full release gate passed** for the remediation checkpoint.
  This is user-reported evidence, not an agent-observed successful run; no new
  test count or warning count was supplied. The current gate result is no longer
  pending. Original failed runs remain retained and are not relabeled passed.
- Final documentation: **222 documents**, zero errors/warnings/information;
  health **91.4%, A-**, zero warnings/errors; repair preview **zero changes,
  files or diagnostics**. `git diff --check` passes with existing checkout
  line-ending notices. PowerShell parsing and final evidence/preservation
  checks are retained in the remediation evidence directory.
- Preservation comparison covers **187 preexisting modified/untracked files**:
  **zero unexpected changes**, unchanged HEAD and authoritative release-gate
  SHA-256 (`preservation-final.json`). Original three SBOMs, native inventory,
  all six original legal texts, B9 corpus and B10/B11 implementation are preserved.

All seven acceptance conditions are satisfied: 13 findings individually
reconciled; tool identities/terms/provenance reviewed; notices traceable;
schema/hash/negative/reproducibility checks pass; existing security/CI controls
preserved; no unresolved current-use decision remains; documentation and
preservation checks pass. B12 completion does not qualify a desktop distribution,
accept revenue-generating use under different facts, close B11/B9, enable H2b
or authorize implementation of B13-B16 or BF-1055-BF-1058.

The initial implementation evidence below is retained as history. Its original
13-package/tool-license gaps are superseded by this remediation and PP-DEP-002;
its agent failures and owner confirmations retain their original scope.

## Baseline and preservation

- Branch: `maintenance/post-BF-1054-repository-review`.
- HEAD: `f51824dbc031b58497a38a6292ee11568b9ffeda`.
- Entry tree: 144 status entries, intentionally dirty; SHA-256 inventory covers
  160 preexisting modified/untracked files.
- Evidence: `artifacts/fuzz/b12-provenance-20261031-01/`, including entry status,
  HEAD/hashes, tool/generation/restore logs, TRXs and retained negative fixtures.
- Read current root instructions, charter, PP-GOV-004/005, product/engineering
  standards, roadmap, handoff, repository audit, PP-AUD-041, package/tool/CI
  configuration and the unchanged authoritative release gate.
- Preserve B9 code/corpus/manifests/findings/reports, all 15 immutable seeds,
  B10/B11 source and evidence, existing solution changes and unrelated work.
  No reset, clean, stash, branch switch, staging, commit, push, merge, tag or
  history rewrite was performed.

The release gate remains byte-identical, SHA-256
`7D555BF893C2C2485969C67E2D95E00FE6CE119000B1E0688BFD7094BCFFFE38`.
No production C#, public API, runtime dependency, security policy, H2b gate,
installer/signing infrastructure or BF-1055-BF-1058 implementation changed.

## Dependency inventory and provenance

[PP-DEP-001](../dependencies/README.md) contains the procedure and complete
output mapping. `resolved-dependencies.json` inventories 47 current projects
and 21 unique resolved NuGet package/version pairs. Four packages are used by
production `src/` projects: SkiaSharp plus Linux, macOS and Win32 NativeAssets,
all 4.150.1. Other uses are development contexts, including tests and the
separately scoped SharpFuzz project. Packages can have both usage contexts.
Direct/transitive usage and resolved package edges are recorded per framework.

Central declarations are captured from both root Directory.Packages.props and
the SharpFuzz-specific central file. The existing SharpFuzz 2.3.0 lockfile and
SharpFuzz.CommandLine 2.3.0 tool manifest are preserved. Tool declaration is
distinguished from proof of installation. SDK 10.0.302/latestPatch and hosted
CI action/tool inputs are documented separately from runtime NuGet components.

For each repository package, available metadata includes source, ID/version,
declared license/legacy URL, project/repository URL and commit, declared authors/
copyright, restore hash, archive SHA-512 and manifest SHA-256. Sources are public
NuGet metadata; credential-bearing/private URLs are refused. Missing metadata
is explicit, not silently approved. Missing restore/cache/manifest files fail
generation rather than producing a partial success artifact.

Restore hashes are checked against installed NuGet metadata. Raw package
archives are checked against cached archive checksums; extracted manifests,
notices and native files are checked against actual archive entries. Restore
content hashes and archive hashes are retained separately, not conflated.
Local metadata/hashes do not prove certificate trust or independent upstream
authenticity, and no vulnerability-feed audit is claimed.

## Native provenance and notices

`native-assets.json` records 20 restored native-runtime assets across Skia's
Linux/macOS/Windows packages, including three Windows PDBs. Each record includes
origin package/source/commit, declared package license, RID, size, SHA-256 and
an explicit false desktop-distribution qualification flag. The Windows x64 DLL
is `libSkiaSharp.dll`, SHA-256
`c8770c219e0d3cd9bb119fad46f2d00ae1855b11315816e86e909e3332826212`;
its PDB is separately identified as debug symbols.

The packages declare Microsoft, MIT and release commit
`c3e4f4c20e1f23ab74d31a8838a5bd6dc55365f2`. Actual bundled third-party material
is represented by the publishers' full notice files; individual embedded
library/build provenance is not inferred from the package MIT declaration.
Windows system imports from B11 are recorded separately, not presented as
bundled NuGet components or universally confined native imports.

`THIRD-PARTY-NOTICES.txt` deduplicates package/version attributions and links
**six unique original license/notice texts**, copied byte-for-byte and named
by SHA-256. Multiple consuming projects do not duplicate notices. The source
package paths and hashes remain available for review. No license text,
copyright holder or approval status was fabricated.

### Initial notice/provenance review findings — resolved by remediation above

The initial implementation review register identified:

- coverlet.collector, Microsoft.NET.Test.Sdk, Microsoft.TestPlatform.ObjectModel,
  SharpFuzz and SharpFuzz.Common: declared licenses but no included standalone
  attribution/license text.
- xunit, xunit.analyzers, xunit.assert, xunit.core,
  xunit.extensibility.core, xunit.extensibility.execution and
  xunit.runner.visualstudio: Apache-2.0 declarations but no packaged standalone
  notice/license text.
- xunit.abstractions 2.0.3: no declared license expression/file or repository
  URL; only a legacy moving license URL. Version-specific evidence needs review.
- CycloneDX generator: Apache-2.0 declaration but no standalone packaged license
  text; 25-component bundled graphs for each net8/net9/net10 tool target lack
  complete per-library license/provenance evidence. These are visible in
  `generation-tool.json`, not silently counted as production packages.

Those findings distinguished absent package-supplied text from a claim that
upstream text does not exist. Obtain authoritative version-specific evidence,
retain it with hashes and review distribution obligations before shipping
affected components. Generic/current-branch license text is not retrospectively
assigned to old packages. Review native bundled notices against the actual
future distributed build. No definitive legal-compliance conclusion is made.

## Approved SBOM approach

The owner explicitly approved **CycloneDX .NET tool 6.3.1** as a pinned
development/CI tool. It was installed only under ignored evidence using a
NuGet-only configuration, with roll-forward allowed for the existing .NET 10
runtime. Existing root tool declarations were not replaced. No production
package reference was added.

The selected [official tool](https://www.nuget.org/packages/CycloneDX/6.3.1)
supports current solution/project inputs and explicit format/scope options.
Existing dotnet package-list output lacks the requested SBOM/license evidence;
a custom SBOM writer or extra schema-validation dependency was unnecessary.
The chosen validator uses .NET's built-in XML schema facilities.

| SBOM | Scope | Packages |
|---|---|---:|
| `engine.cdx.xml` | Production engine NuGet set, obtained through the existing Skia project graph | 4 |
| `solution.cdx.xml` | Current solution, including development/test dependencies | 18 |
| `sharpfuzz.cdx.xml` | Existing separate SharpFuzz project and project-reference graph | 7 |

The union is **21**, not the sum of overlapping counts. All outputs validate
against CycloneDX 1.6 using vendored versioned official BOM/SPDX XSDs; source
URLs, hashes and the schemas' Apache-2.0 license are retained. DTDs and network
schema resolution are disabled. The schema license is not mislabeled as a
generator-package notice. Only volatile generation timestamps are removed and
serial numbers disabled; package/scope/license/hash/graph evidence is preserved.

Repeated full generation produced **zero byte differences** across generated
inventory, notice, native, tool and SBOM artifacts under identical restored
inputs. `generation-before.json` and `repeat-differences.json` preserve this
check. The final runtime-file inventory also repeated identically across all
**13 generated files** (`final-generation-before.json`, `final-repeat-result.json`).
Publisher re-signing, changed package sources, SDK resolution or an
approved tool update may produce explainable differences requiring review.

## Tooling, CI and tests

New scripts under `tools/dependencies/`:

- `Export-DependencyInventory.ps1`: restore/native/notice evidence, source URL
  filtering, archive correspondence and deduplicated notices.
- `Export-Sbom.ps1`: pinned generator, three scoped SBOMs, timestamp normalization
  and generator package/bundled runtime DLL/config inventory (optional XML/PDB
  retention differences do not affect that file set).
- `Test-DependencyArtifacts.ps1`: offline schema, package union/production set,
  hashes, notice correspondence and private-path/credential-like value checks.
- `Test-DependencyPipeline.ps1`: positive validation plus five owned negative
  mutations; failure evidence retained under the caller's ignored evidence path.
- NuGet-only tooling configuration and vendored schema provenance/license.

Scoped `.gitattributes` preserve schema, generated artifact and vendor-notice
bytes across Git checkouts; automatic CRLF conversion must not invalidate
their recorded hashes. No global Git configuration was changed.

The existing blocking Windows job now performs locked SharpFuzz restore,
exact-version tool installation, generation/validation, negative checks and
tracked-output drift detection. It retains the accountability artifacts.
It does not repeat the entire gate, weaken any test or add GitHub license
lookups/credentials. Existing analyzers, all Windows security tests, source-size
gate, documentation and cleanliness controls remain. Linux stays optional.
Hosted CI results remain unverified until integrated; local checks do not
invent a green remote run.

## Validation evidence

Commands run from `C:\Development\PiratePDF`, regular PowerShell unless the
sandbox permission retry is explicitly noted. See PP-DEP-001 for complete
reproduction commands.

- Inventory generation: exit 0, 47 projects/21 packages; native and notice bytes
  correspond to archive entries; no private absolute paths exported.
- Approved tool installation/generation initially hit sandbox NuGet-config/cache
  access restrictions. Permitted-context retries succeeded. No package-error
  fallback or credential/config disclosure was introduced.
- Three XML SBOMs: schema-valid; package-set union, production coverage, archive
  hashes and copied notices match. Five negative mutations rejected with exit 1:
  invalid schema, altered hash, altered notice, missing package and private path.
- Repeated generation: zero differences.
- Existing plugin/native suite: **48 passed, zero failures/skips**, `plugins.trx`.
- Existing Skia suite: **25 passed, zero failures/skips**, `skia.trx`.
- Existing DocGen suite: **88 passed, zero failures/skips**, `docgen.trx`.
- Existing separate tool locked restore: exit 0 after permitted-context retry;
  no lockfile or dependency upgrade, `locked-restore-permitted.log`.
- Documentation `all` after report integration: **221 documents, zero errors,
  warnings or information findings**. The new CI dependency PowerShell block
  parses locally. Hosted CI is unverified; the tracked-output drift check is
  not claimed passing against this intentionally dirty, uncommitted tree.
- Final documentation health: **91.4%, A-**, zero errors/warnings; repair
  preview: **zero changes, files or diagnostics**. Final schema/hash validation
  passed after confirming scoped Git attributes disable text conversion.
  `git diff --check` passed; Git emitted existing checkout line-ending notices.
- Preservation comparison: **zero unexpected changes** among the 160
  preexisting files; HEAD and the authoritative release gate hash are unchanged
  (`preservation-final.json`).
- Gate sandbox attempt: source sizes passed; restore failed reading the user
  NuGet configuration (exit 1, `release-gate-sandbox.log`/`.exit`).
- Gate permitted-context retry: source sizes, restore and build passed with
  **zero build warnings/errors**. Tests: **3,428 total, 3,423 passed, five failed,
  zero skipped**, exit 1 (`release-gate-permitted.log`/`.exit`). The gate stopped
  before documentation. These are new agent-observed results, separate from
  earlier owner-reported successful B11 gates.

The five failures were Windows symbolic-link fixture-creation privilege errors
before the validators executed:

1. `CorpusManifestTests.SeedLinkEscapeIsRejected`.
2. `FindingStorageTests.ArtifactLinkEscapeIsRejected`.
3. `H2bStationAclApplicabilityTests.ReparseSubstitutionIsRejectedBeforeOwnerAdmission`.
4. `CampaignTicketAdmissionTests.TicketDirectoryLinkIsRejectedAndForeignBytesPreserved`.
5. `CampaignRunnerTests.CampaignWorkspaceLinkIsRejected`.

No failure was skipped, weakened or marked passed in the agent run. The owner
subsequently confirmed **the full release gate passed** on 2026-10-31. This is
user-reported B12 validation, distinct from the retained agent-observed failure
and earlier B11 confirmations. No additional test count, warning count or
execution-context detail was supplied. The current full-gate validation is no
longer pending at that initial checkpoint; licensing/provenance gaps still
prevented closure then and were resolved by the subsequent remediation above.

For reproduction, the previously requested command from Administrator
PowerShell was:

```powershell
Set-Location -LiteralPath 'C:\Development\PiratePDF'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

Retain the full output and result when available. This confirmation does not
resolve missing licensing evidence or enable native H2b, and is not represented
as an agent-observed successful run.

The unchanged gate command is:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

## Planning reconciliation and continuation

The [desktop roadmap](../02-Roadmaps/Windows-Desktop-Development-Sequence.md)
records provisional BF-1056 foundation, BF-1057 editing integration and BF-1058
packaging/installer, followed by V1 Release Preparation. Their identifiers were
not assigned in the inspected milestone files/index and remain provisional
until registry reconciliation. Existing BF-8000 is not renumbered. B16 stays
the final maintenance batch after POST-P2-008 and before BF-1055, preserving its
semantic cleanup and measured managed-fuzzer scope and all original safeguards.

B11's engine-level evidence remains valid; actual application distribution/
startup acceptance remains outstanding for reconciliation during BF-1058.
B12 neither closes B11 nor implements a desktop/installer. B9 remains open,
native H2b stays disabled/deferred, and B13-B16 are not implemented. The
license/provenance review is reconciled and the owner confirmed the remediation
gate passed. B12 is complete; later batches still require their task scope.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.2 | 2026-10-31 | Complete B12 after reconciling all 13 package findings and 26 tool identities, original legal evidence, 117 DLL origin matches, owner-confirmed use terms and separately attributed remediation gate success. |
| 1.1 | 2026-10-31 | Record owner-confirmed B12 full release-gate success separately from the retained agent privilege failures; keep license/provenance remediation and partial disposition. |
| 1.0 | 2026-10-31 | Establish B12 inventory/notices, approved pinned SBOM generation, schema/hash/negative validation, CI and desktop planning reconciliation; retain explicit licensing/provenance remediation and historical evidence. |
