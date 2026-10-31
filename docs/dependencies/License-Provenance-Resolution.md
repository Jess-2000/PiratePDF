# B12 License and Provenance Resolution Matrix

| Field | Value |
|---|---|
| **Document ID** | PP-DEP-002 |
| **Category** | Developer Documentation |
| **Version** | 1.0 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-10-31 |
| **Next Review** | 2027-01-31 |

## Evidence and distribution scope

[license-review.json](license-review.json) is the reviewed input register. It
retains exact source URLs, immutable commits, archive SHA-512, original package
manifest SHA-256, legal-text SHA-256, verification methods and obligations for
39 identities: all 13 repository findings and 26 unique generator identities.
Files are original bytes under `licenses/` and `evidence/`, named by SHA-256.
The original six packaged texts remain; there are now 33 unique legal texts
and 45 manifest/historical evidence files. Absence of packaged text was an
evidence gap, not a finding that a license was violated. No software was modified.

Repository findings are development/test-only. Assemblies may appear in local
test/tool build output; that does not make them production engine dependencies.
The generator and its dependencies are redistributed inside the installed tool
package, used locally/in CI, and are not included in an engine or desktop release.
Newtonsoft.Json also appears in repository test resolution; its tool row does
not erase that usage. The four existing Skia production packages and 20 native
assets retain their original evidence and hashes. No desktop distribution exists.
Future shipping selection, license/NOTICE delivery and native bundled notices
must be reconciled against that actual artifact. SDK/OS/hosted-action internals
are outside these NuGet graph scopes, as documented in PP-DEP-001.

## All 13 repository findings

Every row is reconciled as verified licensing evidence for the exact restored
archive. Package-declared metadata is preserved separately from reviewed
upstream evidence; xunit.abstractions metadata is not silently rewritten.

| Package/version | Reviewed license | Version-specific source | Legal texts |
|---|---|---|---|
| coverlet.collector/6.0.4 | MIT | [90b21079d43c](https://github.com/coverlet-coverage/coverlet/tree/90b21079d43cffae3a18f264e00962b9c8a1d57a) | [text](licenses/17ace2f0bc2bdf6692db688a5e8f48b4d5a665ad35c9e2a6b8ea74d1e5797ac4.txt), [text](licenses/5411d507bc9242b6acad10772a8f5a3ae217f494c419a206a3b6d849c6ccc1c0.txt) |
| Microsoft.NET.Test.Sdk/17.14.1 | MIT | [490850ae3fdc](https://github.com/microsoft/vstest/tree/490850ae3fdc1b470e3804ceab4f6a41cf89ae51) | [text](licenses/193b6793dacd7f80823027ef308b7ae688ad14b436cf3e3747b298ca446a5dbe.txt), [text](licenses/4d0502953fdb7784a9ca9e1f7e93da5db076de7c52456d6a2a666327a1a9981d.txt) |
| Microsoft.TestPlatform.ObjectModel/17.14.1 | MIT | [490850ae3fdc](https://github.com/microsoft/vstest/tree/490850ae3fdc1b470e3804ceab4f6a41cf89ae51) | [text](licenses/193b6793dacd7f80823027ef308b7ae688ad14b436cf3e3747b298ca446a5dbe.txt), [text](licenses/4d0502953fdb7784a9ca9e1f7e93da5db076de7c52456d6a2a666327a1a9981d.txt) |
| SharpFuzz.Common/2.2.0 | MIT | [28c353b41a1f](https://github.com/Metalnem/sharpfuzz/tree/28c353b41a1ff60039bf78293dbd5edd9d7c3014) | [text](licenses/e0ca5bd780510a0511a1dd4328eac0eb7decd8f8f6a48950fc6956b666bcfce4.txt) |
| SharpFuzz/2.3.0 | MIT | [7dde242459c5](https://github.com/Metalnem/sharpfuzz/tree/7dde242459c5459cb988fb8ace4397a9485d720f) | [text](licenses/e0ca5bd780510a0511a1dd4328eac0eb7decd8f8f6a48950fc6956b666bcfce4.txt) |
| xunit.abstractions/2.0.3 | Apache-2.0 | [489a2b3949a9](https://github.com/xunit/abstractions.xunit/tree/489a2b3949a9ae8587201a22a8ecfc1d791fa1f3) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/e9dcf94cc4864ba47aeaaec879cfabaf3feadb813c77af480c3ad14c6de0ed79.txt) |
| xunit.analyzers/1.18.0 | Apache-2.0 | [2620a4ddabcb](https://github.com/xunit/xunit.analyzers/tree/2620a4ddabcba16ceb64ea9ad19c566252015fe8) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/918c2d961b0b79dd3aaf50d00afa237940436dac80fee83de1ed646efc599d56.txt) |
| xunit.assert/2.9.3 | Apache-2.0 | [9712244020d3](https://github.com/xunit/xunit/tree/9712244020d385955d33136b3fe3e87de43539cd) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/e9dcf94cc4864ba47aeaaec879cfabaf3feadb813c77af480c3ad14c6de0ed79.txt) |
| xunit.core/2.9.3 | Apache-2.0 | [9712244020d3](https://github.com/xunit/xunit/tree/9712244020d385955d33136b3fe3e87de43539cd) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/e9dcf94cc4864ba47aeaaec879cfabaf3feadb813c77af480c3ad14c6de0ed79.txt) |
| xunit.extensibility.core/2.9.3 | Apache-2.0 | [9712244020d3](https://github.com/xunit/xunit/tree/9712244020d385955d33136b3fe3e87de43539cd) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/e9dcf94cc4864ba47aeaaec879cfabaf3feadb813c77af480c3ad14c6de0ed79.txt) |
| xunit.extensibility.execution/2.9.3 | Apache-2.0 | [9712244020d3](https://github.com/xunit/xunit/tree/9712244020d385955d33136b3fe3e87de43539cd) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/e9dcf94cc4864ba47aeaaec879cfabaf3feadb813c77af480c3ad14c6de0ed79.txt) |
| xunit.runner.visualstudio/3.1.4 | Apache-2.0 | [50e68bbb8b9d](https://github.com/xunit/visualstudio.xunit/tree/50e68bbb8b9ddd4b1bbb95d062b62010caf99909) | [text](licenses/2db414dcd8f29ebc03790a1386e0a6ded2bb8813ba6763bd5df100d6f78c1b3d.txt), [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt) |
| xunit/2.9.3 | Apache-2.0 | [9712244020d3](https://github.com/xunit/xunit/tree/9712244020d385955d33136b3fe3e87de43539cd) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/e9dcf94cc4864ba47aeaaec879cfabaf3feadb813c77af480c3ad14c6de0ed79.txt) |

MIT requires preserving copyright/permission text with copies or substantial
portions. Apache-2.0 requires its license and applicable attribution/NOTICE,
and notices of modifications where relevant. Complete original text governs;
these descriptions are operational reminders, not replacement license terms.

### Historical xunit.abstractions 2.0.3

The original archive explicitly points to `xunit/xunit/master/license.txt`.
The official abstractions repository has a `2.0.3` tag pointing to
`489a2b3949a9ae8587201a22a8ecfc1d791fa1f3`. That frozen source still has
a 2.0.1 nuspec/assembly version; it is not asserted to reproduce the 2.0.3
package build. Its tag and metadata are retained, not concealed. The linked
xUnit license history before the August 26, 2018 package publication identifies
`8d0cdc1cf585f5cf285d221e0c8ff793e6aeea30`; the original license text states
Apache-2.0 and .NET Foundation attribution. It also contains MIT notices for
specified runner code; retaining that full text does not assert that those
runner sources belong to the abstractions assembly. This is historical
publisher evidence, not a retrospective application of a modern xUnit license.
Full Apache-2.0 terms are retained alongside these attribution statements.

SharpFuzz.Common 2.2.0 declares MIT without a commit. Its official annotated
`v2.2.0` tag resolves to `28c353b41a1ff60039bf78293dbd5edd9d7c3014`; the tag
record, tag object and exact source license are retained. No build-reproduction
or package-signature qualification is inferred.

## Generator bundle: all 26 unique identities

Each shipped net8/net9/net10 graph contains 25 identities including the tool.
Their union is 26: Humanizer.Core 2.14.1 belongs to net8, while 3.0.1 belongs
to net9/net10. All 117 bundled DLL files, including localized resources,
match exact archive entries by SHA-256; dependent DLLs must match a dependency
archive in their target graph, not merely the enclosing CycloneDX archive.
The tool package SHA-512 and all 123 shipped runtime DLL/config hashes remain
recorded in generation-tool.json. This is binary/archive correspondence, not
proof of an independently reproducible upstream build.

| Component/version | Reviewed license/terms | Version-specific source | Legal texts |
|---|---|---|---|
| CycloneDX.Core/12.1.1 | Apache-2.0 | [27b9b5acbbb2](https://github.com/CycloneDX/cyclonedx-dotnet-library/tree/27b9b5acbbb2198b43489cc840d1ad632d0124d8) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/86928cfe51621f728c3d63c8de0a84be47750d3353145eefa89e728f7f6a26e0.txt) |
| CycloneDX/6.3.1 | Apache-2.0 | [6938a02734b0](https://github.com/CycloneDX/cyclonedx-dotnet/tree/6938a02734b0c30e1a59976f5bd592f1ee8be1eb) | [text](licenses/35d78ed4a70916bf503ef5cd2ca1fd1115b1631545239902988d40195b6ab558.txt), [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt) |
| Humanizer.Core/2.14.1 | MIT | [3ebc38de585f](https://github.com/Humanizr/Humanizer/tree/3ebc38de585fc641a04b0e78ed69468453b0f8a1) | [text](licenses/e24ec7f2eea5c5f4b3e2c08308109ba99b49b4b56ea7ff0e0a178f2bdd4bbce8.txt) |
| Humanizer.Core/3.0.1 | MIT | [6e54d3786f4c](https://github.com/Humanizr/Humanizer/tree/6e54d3786f4c4fe2cf665fa41d74a6e79bf9a85f) | [text](licenses/e24ec7f2eea5c5f4b3e2c08308109ba99b49b4b56ea7ff0e0a178f2bdd4bbce8.txt) |
| Json.More.Net/3.0.0 | OSMFEULA.txt | [47232fecef1d](https://github.com/json-everything/json-everything/tree/47232fecef1d1aab883acf6a0fa142c75f9a31da) | [text](licenses/869642e6f3fc119d01364d609e0a79dc4239751a92444496469794f05f819c80.txt), [text](licenses/baebca0309090f4eca1b7a82c836cc91e48b2b92139c2280fb0ff69af922c2ae.txt), [text](licenses/baebca0309090f4eca1b7a82c836cc91e48b2b92139c2280fb0ff69af922c2ae.txt) |
| JsonPointer.Net/7.0.0 | OSMFEULA.txt | [47232fecef1d](https://github.com/json-everything/json-everything/tree/47232fecef1d1aab883acf6a0fa142c75f9a31da) | [text](licenses/869642e6f3fc119d01364d609e0a79dc4239751a92444496469794f05f819c80.txt), [text](licenses/baebca0309090f4eca1b7a82c836cc91e48b2b92139c2280fb0ff69af922c2ae.txt), [text](licenses/baebca0309090f4eca1b7a82c836cc91e48b2b92139c2280fb0ff69af922c2ae.txt) |
| JsonSchema.Net/9.1.1 | OSMFEULA.txt | [519ad0e0d6be](https://github.com/json-everything/json-everything/tree/519ad0e0d6bebd440580d497b2137c7ea730a5ef) | [text](licenses/869642e6f3fc119d01364d609e0a79dc4239751a92444496469794f05f819c80.txt), [text](licenses/baebca0309090f4eca1b7a82c836cc91e48b2b92139c2280fb0ff69af922c2ae.txt) |
| Newtonsoft.Json/13.0.3 | MIT | [0a2e291c0d9c](https://github.com/JamesNK/Newtonsoft.Json/tree/0a2e291c0d9c0c7675d445703e51750363a549ef) | [text](licenses/b98a397897ff55f76cddc380041507a95a896b2946e0188703bf0496793d6516.txt), [text](licenses/d7f83bbea3264a8e23d7739e44d9db1a4994b357274df6f00876eb2e623827ad.txt) |
| NuGet.Common/7.6.0 | Apache-2.0 | [f333d9ade72a](https://github.com/NuGet/NuGet.Client/tree/f333d9ade72a3409b54795f649efcac8f362054d) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/f1e3c903178e459ebd0c25d2dc65ebf11163c0a0d3ae46690943dffd5813040e.txt) |
| NuGet.Configuration/7.6.0 | Apache-2.0 | [f333d9ade72a](https://github.com/NuGet/NuGet.Client/tree/f333d9ade72a3409b54795f649efcac8f362054d) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/f1e3c903178e459ebd0c25d2dc65ebf11163c0a0d3ae46690943dffd5813040e.txt) |
| NuGet.DependencyResolver.Core/7.6.0 | Apache-2.0 | [f333d9ade72a](https://github.com/NuGet/NuGet.Client/tree/f333d9ade72a3409b54795f649efcac8f362054d) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/f1e3c903178e459ebd0c25d2dc65ebf11163c0a0d3ae46690943dffd5813040e.txt) |
| NuGet.Frameworks/7.6.0 | Apache-2.0 | [f333d9ade72a](https://github.com/NuGet/NuGet.Client/tree/f333d9ade72a3409b54795f649efcac8f362054d) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/f1e3c903178e459ebd0c25d2dc65ebf11163c0a0d3ae46690943dffd5813040e.txt) |
| NuGet.LibraryModel/7.6.0 | Apache-2.0 | [f333d9ade72a](https://github.com/NuGet/NuGet.Client/tree/f333d9ade72a3409b54795f649efcac8f362054d) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/f1e3c903178e459ebd0c25d2dc65ebf11163c0a0d3ae46690943dffd5813040e.txt) |
| NuGet.Packaging/7.6.0 | Apache-2.0 | [f333d9ade72a](https://github.com/NuGet/NuGet.Client/tree/f333d9ade72a3409b54795f649efcac8f362054d) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/f1e3c903178e459ebd0c25d2dc65ebf11163c0a0d3ae46690943dffd5813040e.txt) |
| NuGet.ProjectModel/7.6.0 | Apache-2.0 | [f333d9ade72a](https://github.com/NuGet/NuGet.Client/tree/f333d9ade72a3409b54795f649efcac8f362054d) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/f1e3c903178e459ebd0c25d2dc65ebf11163c0a0d3ae46690943dffd5813040e.txt) |
| NuGet.Protocol/7.6.0 | Apache-2.0 | [f333d9ade72a](https://github.com/NuGet/NuGet.Client/tree/f333d9ade72a3409b54795f649efcac8f362054d) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/f1e3c903178e459ebd0c25d2dc65ebf11163c0a0d3ae46690943dffd5813040e.txt) |
| NuGet.Versioning/7.6.0 | Apache-2.0 | [f333d9ade72a](https://github.com/NuGet/NuGet.Client/tree/f333d9ade72a3409b54795f649efcac8f362054d) | [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt), [text](licenses/f1e3c903178e459ebd0c25d2dc65ebf11163c0a0d3ae46690943dffd5813040e.txt) |
| protobuf-net.Core/3.2.56 | Apache-2.0 | [dfdfce61a739](https://github.com/protobuf-net/protobuf-net/tree/dfdfce61a739cfd76f05fcdacf8a4b3b9e94e684) | [text](licenses/2054377b5c04fb70c67fe91ca12417e9d413e533169f1270ef69bd815f3053f3.txt), [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt) |
| protobuf-net/3.2.56 | Apache-2.0 | [dfdfce61a739](https://github.com/protobuf-net/protobuf-net/tree/dfdfce61a739cfd76f05fcdacf8a4b3b9e94e684) | [text](licenses/2054377b5c04fb70c67fe91ca12417e9d413e533169f1270ef69bd815f3053f3.txt), [text](licenses/6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae.txt) |
| System.CommandLine/2.0.8 | MIT | [94ea82652cdd](https://github.com/dotnet/dotnet/tree/94ea82652cdd4e0f8046b5bd5becbd11461482ca) | [text](licenses/4bef6a7e7b359869d751c9ff70274a80a0f626927afa5a0681583c1b1861267b.txt), [text](licenses/ae48df11a335dc1a615f4f938b69cba73bcf4485c4f97af49b38efb0f216353b.txt) |
| System.IO.Abstractions/22.2.0 | MIT | [ec68061d1cf1](https://github.com/TestableIO/System.IO.Abstractions/tree/ec68061d1cf181d50f089f979301f874885bf0be) | [text](licenses/9b4f46dc8e65bb14714479eb4de76b7a1e2e7f9eff7ab1eb0e9dea712f19b1ed.txt) |
| System.Security.Cryptography.Pkcs/8.0.1 | MIT | [81cabf2857a0](https://github.com/dotnet/runtime/tree/81cabf2857a01351e5ab578947c7403a5b128ad1) | [text](licenses/97c1a7b3da6a4c6ad516448719f45114b41a4d4c5aa300a944476e2e4f5da438.txt), [text](licenses/b60b2912da28eaa6518593c9e2efb5334ee062d3c42e80d8fdfa806b3dc52977.txt), [text](licenses/cfc21f5e8bd655ae997eec916138b707b1d290b83272c02a95c9f821b8c87310.txt), [text](licenses/d7a68596ab69b06f51ca278a6545148e4269a9381c26d597c13df5d88e08cf5b.txt) |
| System.Security.Cryptography.ProtectedData/8.0.0 | MIT | [5535e31a7123](https://github.com/dotnet/runtime/tree/5535e31a712343a63f5d7d796cd874e563e5ac14) | [text](licenses/19c19dcac9f3ee6302cfbc6745bb8e79f08efc4c933eb8dc5509bf14b88347ec.txt), [text](licenses/7238e7fd468427aa3fe45b1d0cee1c3e2d93ff96692820768521e9780225d473.txt), [text](licenses/cfc21f5e8bd655ae997eec916138b707b1d290b83272c02a95c9f821b8c87310.txt), [text](licenses/d7a68596ab69b06f51ca278a6545148e4269a9381c26d597c13df5d88e08cf5b.txt) |
| TestableIO.System.IO.Abstractions.Wrappers/22.2.0 | MIT | [ec68061d1cf1](https://github.com/TestableIO/System.IO.Abstractions/tree/ec68061d1cf181d50f089f979301f874885bf0be) | [text](licenses/9b4f46dc8e65bb14714479eb4de76b7a1e2e7f9eff7ab1eb0e9dea712f19b1ed.txt) |
| TestableIO.System.IO.Abstractions/22.2.0 | MIT | [ec68061d1cf1](https://github.com/TestableIO/System.IO.Abstractions/tree/ec68061d1cf181d50f089f979301f874885bf0be) | [text](licenses/9b4f46dc8e65bb14714479eb4de76b7a1e2e7f9eff7ab1eb0e9dea712f19b1ed.txt) |
| Testably.Abstractions.FileSystem.Interface/10.3.0 | MIT | [9b967ee024ca](https://github.com/Testably/Testably.Abstractions/tree/9b967ee024caa2d47e3ac27a23d26ea55cd20ded) | [text](licenses/024f2e034ea52d1b47be884bda1bd9b776ceddbfb2a7cc4d49f79255c8ed483c.txt) |

### Owner-confirmed non-revenue-generating use

Json.More.Net 3.0.0, JsonPointer.Net 7.0.0 and JsonSchema.Net 9.1.1 declare
`OSMFEULA.txt` in their exact package manifests. The retained agreement
distinguishes binary use generating revenue from non-revenue-generating use
and discusses MIT source licensing and redistribution. Source MIT text and
the full packaged agreement are both retained; these packages are not
misclassified as unconditionally Apache-2.0 or as having only MIT terms.

On 2026-10-31, after disclosure of these exact components and terms, the owner
confirmed: **Owner confirms non-revenue-generating use and accepts the
documented terms.** This resolves the current use decision, not a general
legal opinion, revenue-generating use approval or commitment to pay fees.
Reassess before revenue-generating use, changing the tool/version/terms or
distributing a different build. No account, payment or upstream communication
was initiated. No other B12 licensing decision remains unresolved.

## Reproducibility and review procedure

Normal regeneration consumes this reviewed register and preserved bytes
offline; it never replaces them with current-branch text. Inventory checks
reviewed archive hashes before reconciling packaged-metadata findings. The
validator checks manifest identity/declarations, evidence hashes, exact review
coverage, tool target graphs and binary hashes. Original five negative cases
remain, with two additional evidence-tampering and review-identity cases.
See [PP-DEP-001](README.md) for commands and [PP-AUD-042](../reviews/Post-BF-1054-Batch-B12-Dependency-Provenance-and-Sbom-Completion-Report.md)
for exact validation results and disposition. When versions change, first
review original archives and immutable publisher sources, retain the new
evidence and hashes, review terms/obligations and update this register/matrix.
Do not clear findings by editing generated metadata or using a modern license
for an older version. Hosted CI is unverified until integration.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-10-31 | Reconcile all 13 repository findings and 26 generator identities using retained version-specific legal evidence, binary correspondence and owner-confirmed non-revenue-generating use. |
