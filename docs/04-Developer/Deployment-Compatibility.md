# Deployment Compatibility and B14 Experiments

| Field | Value |
|---|---|
| **Document ID** | PP-DEV-003 |
| **Category** | Developer Documentation |
| **Version** | 1.2 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054, BF-1056, BF-1058, BF-8000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Current recommendation

PP-GOV-007 now governs runtime/dependency and artifact currency across all
deployment modes. B14A is complete. B14B is complete with owner-reported final source-serviced full-gate success. B14B inventories current/local/historical
outputs, rebuilds or blocks outdated artifacts, services SkiaSharp to 4.150.7 and
supplies reusable local currency verification before B15's hosted integration.
The owner reported elevated patched-toolchain gate success. Retain the original B14
and B14A reports as their accepted checkpoints.
[Currency policy](../05-Governance/Runtime-Dependency-and-Artifact-Currency.md).

Use tools/dependencies/Test-ArtifactCurrency.ps1 with a maintainer-created
artifact manifest and reviewed security baseline. It verifies selected SDK,
compiler restore auditing, runtimeconfig floors, self-contained runtime hashes,
approved native bytes, NuGet audit coverage and complete artifact file hashes.
Missing or failed evidence stops verification. Capture fresh publisher metadata
and audits at the policy's servicing cadence. A local verification pass retains
its stated development scope. Final desktop and signed-distribution acceptance
remains under B11 and PP-REL-001.

The current SBOM producer is the owner-approved source-serviced development
variant 6.3.1-piratepdf.b14b.1, built from the exact original commit with only
the Pkcs 8.0.2 central dependency pin and explicit build identity/runtime
requirements. Its two isolated runtime-file builds match. The original vendor
6.3.1 bundle is blocked because its net8/net9/net10 payloads contain Pkcs 8.0.1.
Build-ServicedSbomTool.ps1 provides checksum-pinned source acquisition and
audited rebuilding. Export-Sbom.ps1 validates the current producer before
execution. [Current procedure](../dependencies/README.md).

Retain the existing framework-dependent, untrimmed JIT developer baseline.
Evaluate untrimmed self-contained deployment when the Windows executable and
installer requirements exist, including runtime servicing responsibilities.
Defer trimming until its actual metadata/dynamic-loading diagnostics are
resolved and the final public/plugin surfaces are qualified. Reject Native AOT
for the current runtime-loaded managed-plugin host architecture. Closed-world
tools may be reconsidered separately after security and metadata remediation.

These are B14 recommendations from scoped developer/fixture experiments.
B14A selects SDK 10.0.403 and runtime 10.0.14 for ordinary net10.0 builds.
The future desktop distribution still
requires Windows 10/11 x64 installation/startup, Skia/native dependency,
signing/provenance and actual application qualification under B11/PP-REL-001.

## Evidence and scope

B14 publishes the existing DocGen executable and an isolated console workload
that directly invokes 29 existing B10 conformance cases plus six existing
security/plugin checks. The console is retained under ignored experiment
evidence and consumes existing test projects/packages. Its artifact size
includes xUnit/test-platform assemblies and debug files. The workload exercises
parse/render, text and embedded-font edits, traditional/stream xrefs, full and
incremental save/reopen, preservation, rollback/undo/redo, SDK target validity,
plugin activation/disposal and privileged capability/extension denial.

Separate font runs exercise the existing Windows provider against installed
fonts under its existing bounds. The measurements identify this machine's
font count and timing without retaining font names or document content.
Developer tool validation uses a private copy of the documentation library.
Current B13 evidence remains the accepted checkpoint for its original scope.

Full commands, diagnostics, raw repetitions, hashes and acceptance are recorded
in the B14 report and ignored evidence directory.
[B14 report](../reviews/Post-BF-1054-Batch-B14-Deployment-Compatibility-Report.md).

## Observed compatibility boundaries

| Area | Concrete finding | Qualification consequence |
|---|---|---|
| DocGen metadata | HistoricalMetadataSupplements.Apply uses JsonSerializer.Deserialize with options at line 33 | Trim publish fails IL2026 under existing warning policy. A separately scoped source-generated metadata change is a candidate repair |
| Plugin loading | PluginLoadContext.LoadManagedAssembly calls collectible AssemblyLoadContext.LoadFromStream | Trim analysis reports IL2026. Native AOT's dynamic assembly-loading limitation conflicts with this host model |
| Plugin activation | PluginRuntimeHost.TryActivateAsync resolves a manifest type name, checks its constructor and invokes Activator.CreateInstance | Trim analysis reports IL2026, IL2075 and IL2072. Future trimming needs a deliberate external SDK/plugin preservation contract |
| Manifest inspection | PluginManifestReader uses bounded JsonDocument parsing | This differs from reflection-based generic object deserialization and remains exercised by successful plugin admission/activation |
| Writing and editing | Static object serializers and existing transactions/history | Existing public-boundary conformance cases pass in untrimmed supported experiments |
| Fonts and Skia | Explicit file providers, sanitizer/embedding contracts and packaged libSkiaSharp.dll | Published workload validation passes with the original Windows x64 native hash. Final desktop packaging still requires its own qualification |
| Test workload trimming | Referenced test projects also produce NETSDK1191 RID inference errors | The fixture graph has a separate experiment limitation, additional to observed production plugin diagnostics |
| AOT toolchain | SDK-selected Microsoft.DotNet.ILCompiler 10.0.10 restore fails NU1903 | Compilation and runtime measurements do not occur for AOT. Preserve the audit failure and address toolchain servicing separately |

Native AOT supports selected reflection patterns, but runtime loading of
previously unknown managed assemblies conflicts with its documented model.
Trimming requires analysis of dynamically reached members and external plugins.
An arbitrary reflection annotation or warning suppression cannot qualify the
plugin host's supported behavior.
[Microsoft Native AOT limitations](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).

## Toolchain security finding

B14-SEC-001 records NuGet's high-severity report for
Microsoft.DotNet.ILCompiler 10.0.10, GHSA-p3jp-9v83-76w9 / CVE-2026-111497.
The publisher identifies patched .NET 10 runtime/compiler 10.0.14 and associated
SDK servicing. The measured baseline host/runtime and self-contained outputs
use 10.0.10. Official deployment requires review and remediation of the
affected toolchain/runtime inputs. Updating a shared runtime alone cannot
service embedded self-contained or Native AOT runtime bytes.
[Publisher advisory](https://github.com/dotnet/runtime/security/advisories/GHSA-p3jp-9v83-76w9).

The original B14 report and measurements retain their .NET 10.0.10 identities.
B14A obtains the publisher-checksummed SDK 10.0.403 in an ignored local directory,
pins CI to that SDK and selects runtime 10.0.14. The narrow AOT restore now
resolves ILCompiler 10.0.14. B14A is complete with passing publish/regression/
audit checks and owner-reported elevated patched-toolchain gate success.
The retained agent gate has five link-privilege failures.
[B14A security report](../reviews/Post-BF-1054-Batch-B14A-Runtime-Toolchain-Security-Report.md).

## Developer and CI toolchain setup

Install the stable .NET 10 SDK selected by global.json through the publisher's
normal process, or use a checksum-verified local SDK ZIP. global.json permits
later patches in the selected feature band. CI uses the exact 10.0.403 SDK.
Ordinary generated runtimeconfig files require at least 10.0.14 and retain
default patch roll-forward. The station-only provisioning artifact retains
its separate exact-runtime approval requirement.

For the retained B14A local setup, use regular PowerShell from
C:\Development\PiratePDF before build, restore or publish commands.

```powershell
$env:DOTNET_ROOT = Join-Path $PWD 'artifacts\fuzz\b14a-security-20261108-01\dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
dotnet --info
```

Confirm SDK 10.0.403 and runtime 10.0.14. Existing global runtimes require
their own owner-managed servicing. Rebuild self-contained artifacts with the
patched runtime and record their actual embedded bytes. Refresh SDK/runtime
identities and audit evidence during future servicing.

For B14A's retained gate evidence, the ignored local launcher binds dotnet
directly to that executable and checks the selected SDK before invoking the
unchanged release gate. This avoids a shell selecting the global SDK instead.
Run in Administrator PowerShell when validating the required link fixtures.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\artifacts\fuzz\b14a-security-20261108-01\Invoke-PatchedReleaseGate.ps1
```

## Reproducing controlled publishes

Use regular PowerShell from C:\Development\PiratePDF. The existing executable
can be published with isolated build/intermediate/output roots. The following
framework-dependent command uses the currently selected toolchain/runtime.

```powershell
dotnet publish .\tools\PiratePDF.DocGen\PiratePDF.DocGen.csproj -c Release -r win-x64 --self-contained false --artifacts-path .\artifacts\fuzz\b14-reproduce\build\fdd -o .\artifacts\fuzz\b14-reproduce\publish\fdd -p:PublishTrimmed=false -p:PublishAot=false -p:PublishReadyToRun=false
```

For self-contained comparison, use a distinct build/output root and
`--self-contained true`. The trim experiment adds `-p:PublishTrimmed=true`.
The AOT experiment additionally sets `-p:PublishAot=true`. Preserve all
nonzero exits and diagnostics. Current trim and AOT attempts fail as described
above for the original B14 toolchain. B14A's AOT restore result supersedes the
compiler security failure for the patched inputs while retaining the plugin
host's architectural deployment decision.

The retained Publish-Modes.ps1, EngineWorkload project and Measure-Modes.ps1
under `artifacts/fuzz/b14-deployment-20261108-01/` reproduce the exact fixtures
and repeated measurements. The isolated workload uses existing repository
test dependencies and is excluded from the production project/SBOM inventory.

## Future Windows desktop and security requirements

Avalonia has Native AOT support with compiled XAML/bindings and preservation
requirements. Its documentation is relevant to future work, while the actual
Avalonia project, versions, controls, resources and plugins require their own
publish tests. No Avalonia package exists in this checkpoint's production graph.
[Avalonia XAML compilation](https://docs.avaloniaui.net/docs/xaml/compilation),
[Compiled bindings](https://docs.avaloniaui.net/docs/data-binding/compiled-bindings).

Each deployment choice must preserve public SDK behavior, plugin capability
enforcement, native resolution/fallback policy, document ownership, exceptions,
cancellation, diagnostics and preservation. PP-GOV-006's bounded local form
policy applies to future runtimes. AOT, trimming and symbol stripping are
deployment choices whose benefits require measurements and compatibility evidence.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.2 | 2026-11-08 | Apply B14B permanent artifact currency policy and servicing/verification guidance. |
| 1.1 | 2026-11-08 | Record B14A patched toolchain selection and local/CI setup while preserving original B14 experiments. |
| 1.0 | 2026-11-08 | Record B14 scoped publish evidence, dynamic/metadata boundaries, toolchain audit finding and deployment recommendations. |
