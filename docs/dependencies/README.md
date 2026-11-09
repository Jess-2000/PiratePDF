# Dependency Inventory, Notices and SBOM Procedure

| Field | Value |
|---|---|
| **Document ID** | PP-DEP-001 |
| **Category** | Developer Documentation |
| **Version** | 1.3 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050, BF-1054 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Scope and outputs

PP-GOV-007 governs ongoing dependency/runtime and artifact currency. Check
publisher support/advisories, direct/transitive audits and native provenance
during maintenance, before release candidates and on relevant advisories.
Preserve historical inventories and relate serviced replacements to original
artifacts. Review changed archives, legal texts and transitive inputs before
refreshing current accountability evidence. Owner-approved exceptions require
explicit risk, permitted use, controls and a review/remediation condition.
[Currency policy](../05-Governance/Runtime-Dependency-and-Artifact-Currency.md).

Test-ArtifactCurrency.ps1 and artifact-security-baseline.json under
tools/dependencies provide local reusable artifact verification for B15.
Pass a reviewed manifest using regular PowerShell from C:\Development\PiratePDF.

```powershell
powershell.exe -NoProfile -File .\tools\dependencies\Test-ArtifactCurrency.ps1 -ManifestPath .\artifacts\fuzz\b14b-currency-20261108-01\current-artifacts.json
```

The manifest records source/build identity, exact output file hashes, compiler
assets, dated publisher metadata and complete project audits. Verification does
not fetch moving metadata or grant official distribution acceptance. Refresh
inputs before qualification and retain the command's diagnostics and exit code.

B14A services the SDK and implicit runtime/compiler packs to SDK 10.0.403 and
runtime/compiler 10.0.14. The existing B12 package inventory, notices and SBOMs
retain their reviewed NuGet dependency scope. Runtime packs and the SDK are
additional deployment/build inputs. Record their exact identities and actual
published runtime hashes in release provenance. Rebuild self-contained
artifacts after servicing and generate the final distribution SBOM from its
actual contents. B14A validation and acceptance are tracked separately.
[B14A report](../reviews/Post-BF-1054-Batch-B14A-Runtime-Toolchain-Security-Report.md).

This is repository dependency evidence, **not a desktop distribution SBOM**.
There is no desktop entry project. Windows 10/11 x64 is the sole official v1
target; retained Linux/macOS assets support future community compatibility.

| Artifact | Meaning |
|---|---|
| [resolved-dependencies.json](resolved-dependencies.json) | 47 project restore graphs, 21 unique NuGet package/version pairs, direct/transitive uses, production/development context, central versions, package sources/hashes, declared license/provenance and review issues |
| [native-assets.json](native-assets.json) | 20 restored native-runtime assets with origin package/commit, RID, size and SHA-256; includes three Windows PDBs, not just loadable libraries |
| [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) | Deduplicated package/version attribution index, reviewed upstream texts and tool-use decision |
| `licenses/*.txt` | 33 unique original legal texts from exact archives and immutable publisher sources, named by SHA-256; original six packaged texts preserved |
| [license-review.json](license-review.json) | Reviewed offline input: all 13 repository findings, 26 generator identities, source/hash/obligation records and 117 DLL-to-archive matches |
| [License-Provenance-Resolution.md](License-Provenance-Resolution.md) | Per-package and generator matrices, historical xUnit evidence and owner-confirmed tool-use terms |
| `evidence/*.txt` | 46 manifests/historical records, including the new Pkcs 8.0.2 manifest; named by SHA-256 with reviewed source URLs |
| [engine.cdx.xml](engine.cdx.xml) | CycloneDX 1.6 XML, four packages used by production engine projects |
| [solution.cdx.xml](solution.cdx.xml) | CycloneDX 1.6 XML, 18 solution-resolved packages, including test/development packages |
| [sharpfuzz.cdx.xml](sharpfuzz.cdx.xml) | CycloneDX 1.6 XML, seven packages from the separately scoped SharpFuzz project and project references |
| [generation-tool.json](generation-tool.json) | Original CycloneDX 6.3.1 vendor provenance and all three target graphs, retained as blocked historical evidence after the Pkcs advisories |
| [generation-tool-serviced.json](generation-tool-serviced.json) | Active PiratePDF source-serviced variant, exact upstream/source patch, SDK/runtime identity, reproducible runtime files and 24 package identities |
| [serviced-tool-review.json](serviced-tool-review.json) | Owner-approved rebuild and exact Pkcs 8.0.2 archive, manifest, legal and binary-origin evidence, referencing unchanged original component reviews |

The three SBOM package sets overlap; their union is exactly the inventory's
21 packages. Adding component counts would double-count shared Skia packages.
`src/` uses are production; `tests/`, `samples/` and `tools/` uses are
development contexts. A package may have both. Scope does not prove a file is
shipped in a future product. Native assets and test-only dependencies present
in restore graphs are not declared included in any desktop distribution.

The existing root tool manifest separately declares SharpFuzz.CommandLine
2.3.0; a declaration is not proof of installation or a restored tool graph.
The .NET SDK is pinned by global.json to 10.0.403 with latestPatch roll-forward.
CI selects .NET 10.0.403 and uses checkout@v7, setup-dotnet@v6 and
upload-artifact@v7. Those hosted tools and their internals are development
inputs, not NuGet runtime components. They are not covered by the engine SBOM.
Action tag pinning, operating-system packages, SDK/runtime packs and signing
are not silently represented as resolved application components.

## Native origin and Windows priority

SkiaSharp and its Linux, macOS and Win32 asset packages are all 4.150.7.
Their manifests identify Microsoft, MIT declarations and upstream release
commit `78140838b2661f7a458d8463b27f6d2400405fbe`. Native package notices contain
additional embedded third-party attribution and licenses; the package MIT
declaration alone does not resolve all those obligations. The included notices
must accompany review of the actual distributed native build.

Windows x64 assets in NativeAssets.Win32 are:

| Asset | SHA-256 |
|---|---|
| `runtimes/win-x64/native/libSkiaSharp.dll` | `832edf69408297a3ad77ab511273ef59937ba4445c1b65b8717d65215aa0364c` |
| `runtimes/win-x64/native/libSkiaSharp.pdb` | `646c664f0efb7e62bb19e07b26004d444d6680da5b0add26670f0316701afa5c` |

B14B's owner-approved servicing update changes four Skia package identities
and their native payloads while retaining the existing 21-package graph. All
four archives pass publisher/repository signature verification. Their seven
packaged legal texts match the reviewed 4.150.1 texts byte-for-byte.
The eight original B12 inventory/accountability files are preserved under
history/b12-20261031. Original legal texts and immutable evidence remain in
their existing locations. Completed reports retain their original results.
The active inventory, native hashes, notices and SBOMs describe the serviced
4.150.7 graph. [Preserved B12 inventory](history/b12-20261031/resolved-dependencies.json).

B11's static Windows import evidence lists ole32, USER32, FONTSUB, KERNEL32,
d3d12 and D3DCOMPILER_47 DLLs. These are system-loader dependencies, not bundled
NuGet libraries. No universal transitive-loader confinement or OS-component
qualification is claimed. The native inventory records package/archive origin,
not an independently rebuilt Skia component graph or desktop qualification.

## Regeneration

Use **regular Windows PowerShell 5.1** from the repository root (the same shell
is explicitly selected in CI for comparable JSON serialization). Restore the
existing solution and the separately scoped tool project; do not upgrade them.
The original CycloneDX 6.3.1 bundle is blocked from current execution because
all three targets embed vulnerable Pkcs 8.0.1. The owner approved rebuilding
the exact upstream source with Pkcs 8.0.2. The development identity is
PiratePDF.CycloneDX.Serviced 6.3.1-piratepdf.b14b.1. Use a new ignored output
directory for its checksum-pinned source build, secure runtime floor, two
reproducible publishes and dependency audit. Existing vendor bytes remain intact.

```powershell
dotnet restore .\PiratePDF.slnx
dotnet restore .\tools\PiratePDF.Fuzz.SharpFuzz\PiratePDF.Fuzz.SharpFuzz.csproj --locked-mode --configfile .\tools\dependencies\NuGet.Config
powershell.exe -NoProfile -File .\tools\dependencies\Build-ServicedSbomTool.ps1 -OutputDirectory .\artifacts\fuzz\sbom-serviced
powershell.exe -NoProfile -File .\tools\dependencies\Export-DependencyInventory.ps1 -OutputDirectory .\docs\dependencies
powershell.exe -NoProfile -File .\tools\dependencies\Export-Sbom.ps1 -CycloneDxPath .\artifacts\fuzz\sbom-serviced\publish-1\CycloneDX.exe -OutputDirectory .\docs\dependencies
powershell.exe -NoProfile -File .\tools\dependencies\Test-DependencyPipeline.ps1 -EvidenceDirectory .\artifacts\fuzz\b12-validation
git diff -- .\docs\dependencies
```

Stop on a nonzero exit. Do not substitute a partial output after restore,
metadata, hash, schema or coverage failure. The generator reads NuGet cache
locations through the installed .NET CLI; a restricted execution context can
block it even with `--disable-package-restore`. This is an environment failure,
not permission to suppress package errors. Default GitHub license lookup is
not enabled and no credentials are needed. Network access is required for
restore/install, not for the vendored schema validation.

The inventory verifies restore content hashes against NuGet install metadata,
archive SHA-512 against cached archive checksum files, and extracted manifests,
notices and native assets against their actual ZIP entries. NuGet restore
content hashes and archive hashes are separate evidence fields: they differ
in the observed packages and are not compared as interchangeable values.
These local checks do not establish certificate trust or independent upstream
authenticity. Never print user NuGet configuration or credentials.

SBOM generation uses the source-serviced derivative of the
[pinned upstream source](https://github.com/CycloneDX/cyclonedx-dotnet/tree/6938a02734b0c30e1a59976f5bd592f1ee8be1eb)
with restore disabled and the original CLI options. Export-Sbom validates
source/patch identity, binary origins, legal evidence and the reviewed main DLL
before execution. It rejects the original or unknown tool without running it.
Only generation timestamps are removed; serial numbers are disabled. Component
identity, scope, dependency graph, hashes and licenses are preserved. Repeated
local generation with the same restored packages produced byte-identical
artifacts. Re-signed package archives, SDK resolution, changed sources or
approved tool updates may produce meaningful differences that require review.
The generator-file inventory excludes optional XML documentation and PDBs,
so CI's existing NUGET_XMLDOC_MODE=skip does not create documentation-file
retention drift. The complete package archive hash is recorded separately.

The XML validator uses .NET XmlSchemaSet with DTDs and network resolution
disabled. Official CycloneDX 1.6 and SPDX schemas plus their Apache-2.0 license
are vendored in `tools/dependencies/schema/`, with versioned source URLs and
SHA-256 in `provenance.json`. Schema provenance hashes are checked before use.
Scoped `.gitattributes` disable line-ending conversion for schema resources,
generated evidence and vendor notice texts so checkout preserves hashed bytes.
The validator also checks package-set coverage, engine production coverage,
archive hashes, notice correspondence and private-path/credential-like values.
The negative tests preserve owned mutated fixtures in ignored evidence and
require rejection of schema, hash, notice, coverage and private-path mutations,
plus altered upstream evidence and mismatched review identity. Normal
regeneration consumes retained license-review.json and evidence offline.
It verifies hashes and exact version/archive correspondence; it does not fetch
moving license URLs or overwrite package declarations with reviewed licenses.

## Review and release limitations

All **13 repository findings are reconciled** with authoritative version-specific
evidence; see PP-DEP-002. Missing packaged text was not itself a licensing
violation. Historical xunit.abstractions 2.0.3 is reviewed against its original
license URL, official tag and pre-publication xUnit license history, not modern
terms. Original declarations and packaged-metadata findings remain available.

The original vendor review covers **26 unique identities** across three
25-identity graphs, including different Humanizer versions. All 117 historical
DLLs match archive entries. The active source-serviced variant has 24 package
identities plus its distinctly versioned source-built main assembly. Pkcs 8.0.2
has four byte-identical legal texts and new archive/manifest/binary evidence.
Three unchanged json-everything packages declare OSMFEULA.txt in addition
to MIT source licensing. On 2026-10-31 the owner confirmed non-revenue-generating
use and accepted those documented terms. Preserve that decision separately
from technical verification; reassess before revenue-generating use or changed
terms. The tool's top-level Apache declaration is not assigned to its bundle.
Full original legal texts and exact source/hash records are retained. This is
evidence and use-decision reconciliation, not a general legal-compliance opinion.

For each update, review the manifest source/commit, declared license and
included notices; verify package/native hashes; resolve missing evidence from
authoritative version-specific upstream sources; rerun generation and negative
checks; review the diff and regenerate documentation. Deduplicate by package
identity and content hash without deleting notices required by a real build.
Record the selected publisher/build when an actual desktop distribution exists.
Do not silently delete stale orphaned notice files or historical evidence;
review removal explicitly during an authorized update.

CI performs locked tool-project restore, pinned generator installation,
inventory/SBOM generation, schema/hash/negative checks and tracked-output drift
checks without repeating the release gate. It retains accountability artifacts.
Structural CI success alone is not legal-release approval. Reconcile notices
with the selected artifacts before distributing them.
Hosted CI execution remains unverified until this working tree is integrated.

B11 remains partial for actual application distribution/startup. Native H2b
remains deferred and disabled; B9 remains open. B12 does not add desktop,
installer, signing, dependency modernization or production APIs.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.3 | 2026-11-08 | Apply B14B permanent artifact currency policy and servicing/verification guidance. |
| 1.2 | 2026-11-08 | Record approved B14A SDK/runtime security servicing and separate validation/acceptance evidence. |
| 1.1 | 2026-10-31 | Reconcile all package and tool-bundle findings with retained historical evidence, offline validation, binary correspondence and owner-confirmed use terms. |
| 1.0 | 2026-10-31 | Establish resolved inventory, native provenance, preserved notices, approved pinned SBOM generation, offline validation and explicit licensing/tooling review limitations. |
