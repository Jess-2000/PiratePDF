# Post-BF-1054 Batch B11 - Packaged Native Resolution and Fallback

| Field | Value |
|---|---|
| **Document ID** | PP-AUD-041 |
| **Category** | Engineering Review |
| **Version** | 1.3 |
| **Status** | Review |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050, BF-1054 |
| **Last Updated** | 2026-10-31 |
| **Next Review** | 2027-01-31 |

## Disposition

**B11 PARTIAL — WINDOWS QUALIFICATION REMAINS**

POST-P2-004 remains open. On 2026-10-31 the owner approved Windows 10/11 x64
as the sole v1 target, accepted process-resident native modules and authorized
narrow CI alignment (B11-D1 through D4, [PP-GOV-005](../05-Governance/Platform-Support-and-Native-Lifetime.md)).
Linux/macOS qualification and deterministic native-image unloading are no
longer v1 blockers. **Actual application distribution/startup remains absent.**
The owner confirmed: **"There is no desktop entry project yet"**. No owner
policy decision remains pending; implementing the desktop application is
outside this maintenance scope.

The owner subsequently confirmed **"The full release gate passed"** on
2026-10-31 in response to the requested current rerun. This clears the current
Windows gate result as **user-reported evidence**. No log, TRX, exact count or
execution-context details were supplied. Earlier failed agent runs remain
unchanged; actual application distribution/startup qualification is still open.

## October 31 Windows qualification and reconciliation

### Baseline and preservation

Branch remains `maintenance/post-BF-1054-repository-review`, HEAD remains
`f51824dbc031b58497a38a6292ee11568b9ffeda`. Evidence is in
`artifacts/fuzz/b11-windows-final-20261031-01/`: entry status, per-file SHA-256
hashes, HEAD, project inventory and new TRXs. A verbatim copy of v1.1 is
retained as `PP-AUD-041-v1.1-entry.md`; October 27 evidence is unchanged.
The entry inventory has 140 status entries and SHA-256 hashes for 156
preexisting modified/untracked files.
Final SHA-256 comparison reports **zero unexpected changes** outside the
authorized current documentation edits. All preexisting production/test code,
solution changes and unrelated evidence remain byte-identical to entry.
No production or test source correction was needed or applied in this
continuation. No native ownership change, dependency addition, new app, sandbox,
H2b qualification, BF-1055 or B16 implementation occurred. No Git mutation
outside ordinary working-file edits was performed.

### Actual application and native-asset inventory

Inventory found **47 C# projects**. All **19 `src/` projects are engine
libraries**. Six executable projects are the DocGen, fuzz runner, SharpFuzz
and native provisioning tools, plus the fuzz test executable and TestPlugin
probe. None is the PiratePDF desktop application. No Avalonia package
reference, App.axaml, desktop Program.cs or application publish profile exists.
The only publish profile found is `StationOnly.pubxml` for deferred native
provisioning; it is not an application deployment target and was not executed.

**Published application inventory: none; no application was published.**
There is consequently no application executable, managed distribution set,
runtime configuration or packaged application native asset to hash or launch.
Selecting a framework-dependent or self-contained win-x64 model for a library
or test would not satisfy B11-D1. No final distribution format is invented.
The existing Skia 4.150.1 native dependency inventory in the October 27 section
remains valid for engine/test fixtures only.

### Windows x64 distribution qualification matrix

| Required check | Result | Evidence or exact gap |
|---|---|---|
| A: actual distribution completeness | Not executable | No desktop entry/publish project; no actual distribution inventory exists |
| B: bounded application startup/readiness | Not executable | No application executable or application-ready signal exists |
| C: application native paths and missing asset | Not executable at application level | Existing clean-process engine probes verify packaged module paths, unrelated writable CWD and missing dependencies; these remain test fixtures |
| D: representative PDF/font/image functionality | Engine integration passes; app operation unavailable | Services 553/553 and Skia 25/25 pass using existing public service/SDK and renderer boundaries; no desktop PDF-open seam exists |
| E: deployment isolation | Application check unavailable | Local host is Windows NT 10.0.26200.0, x64; not a clean Windows 10/11 VM. Existing probes use unrelated CWDs and bounded children |
| F: controlled native failures | Engine fixtures pass; app behavior unverified | Missing assets fail predictably in fresh fixture processes; no application behavior is inferred |
| Required distribution CI step | Not implemented | No real app to publish/start; no passing substitute or always-failing placeholder added |
| Managed/plugin lifecycle and physical containment | Pass | Plugin suite 48/48, including all 17 B11 cases; no loader redesign |
| Native process residency | Accepted by owner | No deterministic unloading requirement; original exploratory lock evidence retained |

The strongest existing PDF integration parses owned original/saved bytes,
edits through service/SDK boundaries and reopens/renders embedded outlines.
The Services suite also contains narrower conformance tests; its passing total
is not a desktop application result. Windows 10 and clean-machine application
execution are not claimed. No missing application check is marked passed,
skipped, waived or replaced by a build result.

### Lifetime acceptance

Managed plugin deactivation, disposal and collectible unload requests remain
required. Native DLLs may remain resident for the application's lifetime.
Immediate deterministic native-image release is not required for v1. Process
termination is the reliable final boundary for process-owned native resources;
replacing a loaded native dependency may require an application restart.
The October 27 file-lock experiment remains accurate historical evidence and
does not require FreeLibrary, a custom native-handle registry or a production
ownership change. Runtime/system-native fallback remains intact.

### CI alignment and validation

The existing Windows Build and Test job retains unfiltered tests, analyzers,
TRX capture, fuzz-category validation, bounded failure artifacts and repository
cleanliness checks. DocGen generation/validation, health and fail-closed
zero-change repair preview now run as blocking Windows steps. The Linux job
is retained with workflow-level `continue-on-error` as an optional compatibility
signal. No full-gate duplication or release-script change is introduced.
Source-size validation remains in the unchanged gate; CI parity stays with
POST-P2-008. Remote branch protection and hosted Actions execution are not
verified or changed. Required remote checks must align with Windows controls.

No distribution-specific test or CI publish/startup job is fabricated in the
absence of an application. Existing engine tests, rather than test-only new
application hooks, provide the strongest available evidence. The owner-supplied
success of the prior full Windows gate remains **user-reported historical
evidence**, not a new agent-observed successful run.

Current continuation commands (repository root, regular PowerShell):

```powershell
dotnet test tests/BlackFlag.Engine.Plugins.Tests/BlackFlag.Engine.Plugins.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=BlackFlag.Engine.Plugins.Tests.trx' --results-directory artifacts/fuzz/b11-windows-final-20261031-01
dotnet test tests/BlackFlag.Engine.Rendering.Skia.Tests/BlackFlag.Engine.Rendering.Skia.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=BlackFlag.Engine.Rendering.Skia.Tests.trx' --results-directory artifacts/fuzz/b11-windows-final-20261031-01
dotnet test tests/BlackFlag.Engine.Services.Tests/BlackFlag.Engine.Services.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=BlackFlag.Engine.Services.Tests.trx' --results-directory artifacts/fuzz/b11-windows-final-20261031-01
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

Focused/affected suites passed **48/48, 25/25 and 553/553**, respectively,
zero failures/skips, exit 0. This includes the 17 existing B11 tests and 29
owned parse/edit/save/reopen/render cases in the Services conformance directory.

The new Windows CI PowerShell block was extracted from the current workflow,
parsed with the installed PowerShell AST parser and executed successfully.
Its exact repair guard accepted zero counts and rejected nonzero Changes,
nonzero Files and missing counts (four checks). Workflow structure and retained
security controls were inspected locally. No full YAML-parser/actionlint tool
was available; hosted Actions execution is unverified. No dependency was
installed solely to validate YAML.

Standalone DocGen `all`, `health` and `repair` passed: **218 documents, zero
validation errors/warnings/information findings; health 91.4% (A-); zero
proposed repair changes/files/diagnostics**. Whitespace validation passed after
removing two newly introduced trailing-space Markdown breaks.

The unchanged release gate was run on 2026-10-31:

- Sandboxed attempt: source-size validation passed; restore failed reading
  the user NuGet configuration (exit 1, `release-gate-sandbox.log`/`.exit`).
- Permitted-context retry: restore/build/source sizes passed, **zero build
  warnings/errors**; tests had **3,428 total, 3,423 passed, five failed, zero
  skipped**. Exit 1, `release-gate-permitted.log`/`.exit`. It stopped before
  documentation; the standalone checks above do not make this gate pass.
- All five failures are the same preexisting symbolic-link fixture-creation
  privilege errors listed in the October 27 evidence. They remain failures,
  not skipped or weakened assertions. The previously confirmed successful
  Windows gate is still user-reported historical evidence, not this run.

The requested current followup used **Administrator PowerShell** instructions
because the observed fixtures need a privilege absent from the agent process:

```powershell
Set-Location -LiteralPath 'C:\Development\PiratePDF'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

The owner confirmed that the full release gate passed. This is a new
user-reported successful result, separate from the October 27 confirmation
and the failed agent run above. It is not an agent-observed privileged run;
no exact test count, warning count or execution context is inferred. No repeat
confirmation is needed. It does not fix the absent desktop target, qualify
hosted CI or authorize H2b execution.

Current edits are limited to the workflow, root instructions/README, current
product plan, roadmaps, governance, plugin developer lifecycle guidance, audit,
handoff, this report and generated documentation. The release-gate SHA-256
remains `7D555BF893C2C2485969C67E2D95E00FE6CE119000B1E0688BFD7094BCFFFE38`.

### Remaining Windows acceptance and B12 readiness

Obtain an implemented, approved desktop entry project, then publish its actual
Windows x64 distribution; inventory required managed/runtime/native assets,
verify bounded startup and owned native-backed functionality in a clean
Windows environment, test missing/invalid assets and intended module paths,
and add real required distribution CI qualification. Those checks cannot be
completed against the current checkout. B11 is not closed by D2/D3 alone.
B12 may be scoped separately for dependency/provenance work; no B12 work starts
here and no B11-complete handoff is asserted. B16 remains reserved after
POST-P2-008 with its existing cleanup/profile and preservation requirements.

## October 27 disposition and implementation evidence (historical)

The v1.1 disposition was partial with platform, packaging and lifetime gaps.
The following records its original scope; the October 31 decisions above
supersede its platform and deterministic-unloading acceptance requirements.

Windows x64 tests verify packaged plugin-native and runtime-managed Skia
resolution, clean-process missing-library behavior, system-library fallback,
path containment, Windows directory junctions and managed-context lifetime.
The owner explicitly approved physical containment with unresolved-name
fallback retained during this assignment. Linux and macOS execution and
official application-package qualification remain outstanding. The published
test assembly is a packaging fixture, not a signed desktop release artifact.
Native-image release after managed unload is not established.

On 2026-10-27 the owner confirmed: **"The full release gate passed. No
failures or warnings"** in response to the requested Windows rerun. This
clears the outstanding Windows release-gate result as **user-reported**
evidence. No rerun log, TRX, exact test count or execution-context details
were supplied. The agent-observed failures below remain historical evidence;
the platform/package/lifetime acceptance gaps keep B11 partial.

Native H2b remains deferred, disabled and unqualified under PP-GOV-004.
B9 / POST-P2-002 remains open. No native fuzzing or BF-1055 work occurred.
B16 is reserved separately as the final planned maintenance batch after
POST-P2-008; none of its implementation is included here.

## Baseline and preservation

- Branch: `maintenance/post-BF-1054-repository-review`.
- HEAD: `f51824dbc031b58497a38a6292ee11568b9ffeda`.
- Entry status contained 134 modified/untracked status entries. Individual
  hashes cover 150 preexisting files, plus two newly created evidence files.
- Evidence: `artifacts/fuzz/b11-native-20261027-01/`, intentionally under the
  existing ignored evidence root. `entry-status.txt`, `entry-head.txt` and
  `entry-hashes.json` preserve the entry inventory. The two self-recorded
  inventory paths retain their original temporary evidence-directory spelling;
  they are not preexisting user files.
- Existing solution changes, B9 code/reports, the 15 corpus seeds and all
  unrelated edits are preserved. No reset, clean, stash, stage, commit, push,
  merge, tag or branch switch was performed.
- Final SHA-256 comparison found **zero unexpected changes** among the
  preexisting files. Only the three authorized current roadmap/handoff files
  and three regenerated documentation outputs differ from their entry hashes.
- Authority reviewed: root AGENTS.md, charter, maintenance handoff,
  PP-GOV-004, current C# audit, B10 report, BF-1050D, plugin developer contract,
  earlier plugin boundary report, engineering standards and release gate.

## Native inventory and existing behavior

The rendering backend references **SkiaSharp 4.150.1** and
**SkiaSharp.NativeAssets.Linux 4.150.1** centrally. The SkiaSharp package
transitively declares NativeAssets.Win32 and NativeAssets.macOS at 4.150.1.
Installed NuGet manifests identify Microsoft, MIT licensing and upstream
release commit `c3e4f4c20e1f23ab74d31a8838a5bd6dc55365f2`. Versions, package
references and production project dependencies were not changed.

| Surface | Packaged/native dependency | Resolution and lifetime before B11 |
|---|---|---|
| Skia rendering, DCT JPEG and font realization | Windows `libSkiaSharp.dll`, macOS `libSkiaSharp.dylib`, Linux `libSkiaSharp.so` | SkiaSharp/.NET/OS-managed loading; no PiratePDF custom resolver or explicit native-module unload |
| Windows package assets | win-x64, win-x86, win-arm64 | RID native assets in restore/build output; x64 executed here |
| macOS package asset | osx universal dylib | Asset present locally; no macOS execution here |
| Linux package assets | linux arm, arm64, x64, x86, loongarch64, riscv64; musl arm, arm64, x64, loongarch64, riscv64; bionic arm64, x64 | Explicit Linux asset package; no Linux execution here |
| Plugin-owned dependencies | Manifest entry assembly and its `.deps.json` native assets | `AssemblyDependencyResolver`; resolved paths were checked lexically, then native images were path-loaded |
| Unresolved plugin native names | Legitimate system APIs and other names not found in plugin assets | Returns zero to .NET; runtime/OS fallback remains possible, including already-loaded modules |
| Runtime itself | Framework/runtime native components | Owned by .NET installation/deployment; not plugin-owned assets |

Static binary import inspection with installed LLVM tools produced
`windows-imports.txt`, `linux-needed.txt` and `macos-needed.txt` in evidence.
These are representative dependency declarations, not proof that every
transitive dependency was loaded securely on every platform:

- Windows x64: ole32, USER32, FONTSUB, KERNEL32, d3d12 and D3DCOMPILER_47 DLLs.
- Linux x64: loader, libc, libdl, libfontconfig, libm, libpthread, librt and
  libstdc++. CI additionally installs fontconfig, FreeType and HarfBuzz.
- macOS: system CoreServices, Cocoa, CoreFoundation, CoreGraphics, Metal,
  MetalKit, CoreText and Foundation frameworks; libc++, libSystem and libobjc.

These system dependencies remain permitted through their established loaders;
this inventory does not introduce a new global allowlist. OS transitive
loading can be influenced by platform loader configuration. A top-level
package check does not constrain every transitive native import.

The observed Windows x64 Skia asset SHA-256 is
`C8770C219E0D3CD9BB119FAD46F2D00AE1855B11315816E86E909E3332826212`.
No arbitrary binary was downloaded, no native compiler was required for the tests,
and no NuGet/native/runtime dependency was introduced or upgraded.

### Image/font fallback and ownership

`SkiaDctImageCodec` validates encoded and decoded bounds and scopes SKData,
SKCodec and SKBitmap disposal. Missing Skia is a dependency-load failure,
not a request for an alternate JPEG decoder. `SkiaFontRealizer` caches
typefaces by resource identity, attempts embedded programs first, and reports
`FONT_SUBSTITUTED` when it uses family/face/default system fonts. Its disposal
releases cached non-default typefaces and clears the cache. Font substitution
is content fallback; it does not replace a missing native Skia dependency.
Neither production rendering file needed a demonstrated B11 correction.

Plugin session disposal deactivates/disposes plugins and requests collectible
context unload. Managed DLLs are stream-loaded; native DLLs remain path-loaded.
An unload request is not immediate collection or native-file release. Native
libraries and their own initialization can have process-level lifetime.

## Existing versus approved policy

The earlier Batch 1 explicitly established lexical containment only; its
historical report is unchanged. During B11 the owner selected **"Approve
physical containment; retain fallback"** after being told this is an additional
restriction and cannot prevent concurrent replacement or sandbox plugins.

| Case | Before B11 | Approved/current behavior |
|---|---|---|
| Resolved package-local native or managed dependency | Allowed after lexical check | Allowed after lexical/canonical-root admission and physical containment |
| Relative candidate | Resolved against current directory and checked lexically | Same normalization, then physical check; not implicitly relative to package root |
| Absolute outside path, traversal or sibling-prefix escape | Rejected lexically | Still rejected |
| Link/junction resolving inside physical package | Lexically admitted without resolving link | Allowed, canonical target passed to loader |
| Link/junction resolving outside physical package | Lexically admitted | Rejected with FileLoadException before loading |
| Package selected through directory link | No distinct physical-root identity | Selected physical root captured once; canonical paths within that root allowed |
| Package path redirected outside captured root after context creation | Lexical check did not detect replacement | Rejected when observed before load |
| Excessive link chain or unresolved reparse type | No explicit link policy | Bounded traversal; unresolved redirection fails closed |
| Missing declared native asset | Resolver returns null/loader returns zero | Unchanged; runtime fallback may succeed or P/Invoke may fail |
| Legitimate system native library | Runtime fallback | Unchanged; actual process-ID P/Invoke covered |
| Arbitrary unresolved absolute/native name | Runtime/OS resolution | Unchanged; no new allowlist or global denial policy |
| Same-name file in unrelated writable current directory | Platform/runtime-dependent fallback surface | Declared assets win in tested fresh Windows processes; universal fallback confinement is not claimed |
| Native-image ownership/release | No host-owned module registry/free policy | Unchanged; no speculative freeing of borrowed handles |

The smallest correction extends the existing internal path helper with BCL
file attributes and link resolution. It adds no service, interface, dependency,
privilege or platform-specific Engine code. Each redirected target is rescanned
from its root so target ancestors are checked too; at most 40 redirects are
accepted. Missing ordinary paths retain downstream loader failure semantics.
Resolved entry paths are validated before resolver construction.

Rejected alternatives: blanket denial of unresolved names would break system
native compatibility; a general native broker/allowlist or sandbox would exceed
this batch; unconditional rejection of all links would reject legitimate
within-package links and redirected installation roots. Handle-based race-proof
loading is not provided by this path validation and was not approved here.

## Reproduced findings and fixtures

`b11-before-correction.trx` records **7 passed, 2 failed** before production
edits: an outside directory junction was accepted, and an internal junction
was returned without canonicalization. Earlier characterization TRXs retain
fixture-development mistakes (native metadata flattening and cross-test native
module reuse); those failures are not represented as production defects.

The existing SDK-only TestPlugin fixture now also has an executable probe
entry point. Its compile-time dependency remains SDK-only. The test project
references the existing Skia rendering project to reuse its already-approved
native asset, copied under a test alias with tiny owned dependency metadata.
It uses reflection to exercise the real internal loader without adding a
production friend declaration or exposing APIs. No third-party source was
copied. Child probes are trusted fixtures with a 15-second timeout; they are
not hostile-input sandboxes and do not run PDFs or native fuzzing.

| New fixture group | Cases | Windows x64 result | Other-platform status |
|---|---:|---|---|
| Runtime-managed Skia: unrelated CWD/invalid same-name decoy; missing native asset | 2 | Pass in fresh processes; actual loaded module path checked for success | Linux/macOS execution pending |
| Plugin native: packaged asset versus CWD decoy; missing asset | 2 | Pass in fresh processes; module path checked, missing asset throws predictably | Linux/macOS execution pending |
| System-native compatibility | 1 | Actual kernel32 process ID matches current process | libc/libSystem branches unexecuted |
| Normalized absolute traversal/sibling/inside paths and relative candidates | 4 | Pass | Portable implementation, execution pending |
| Internal/external redirected directory; post-creation package replacement for managed/native loading | 4 | Pass using nonprivileged directory junctions | Unix symbolic-link branches unexecuted |
| Package root selected through directory link | 1 | Pass | Unix execution pending |
| Excessive redirection | 1 | Pass; 42-link chain rejected | Unix execution pending |
| Borrowed native handle usable; unload event; eventual managed-context collection | 2 | Pass | Native image release not established; other OS execution pending |
| **Total new cases** | **17** | **17 passed, zero failed/skipped** | **No cross-platform acceptance** |

Windows junctions avoid the symbolic-link privilege while exercising directory
reparse handling. File-specific Windows symbolic links and unfamiliar reparse
providers remain unqualified; no privilege elevation was needed for new tests.
The test collection serializes its current-directory case. Fresh child
processes prevent native module caches from invalidating missing-asset checks.

### Lifetime investigation retained accurately

`b11-lifetime.trx` records a failed exploratory expectation that a native DLL
would become exclusively writable after the managed context was collected.
It remained locked even after ten GC/finalizer cycles. BF-1050D explicitly
excludes native libraries from its managed-file lifetime guarantee. Therefore
the final test asserts the established managed collection contract rather than
inventing an immediate native-file release contract; the failed experiment
remains evidence and native release remains unresolved. No production handle
freeing or validation suppression was introduced. The final lifetime probes
run in child processes; the parent additionally verifies exclusive access to
the native fixture after child exit. Final fixture cleanup is unconditional
and does not suppress locked-file exceptions or retain package directories.

The distinction between initiating unload and completed unload follows the
[official .NET unloadability documentation](https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability).
The project's native lifetime limitation is based on the local probe, not a
claim that that document promises native-module release.

## Exact changes

- `src/BlackFlag.Engine.Plugins/Runtime/PluginLoadContext.cs`: capture physical
  package identity; validate entry/dependency paths; bounded physical resolution.
- `tests/BlackFlag.Engine.Plugins.Tests/Runtime/PackagedNativeResolutionTests.cs`:
  17 deterministic integration/containment/lifetime cases and scoped fixture.
- `tests/BlackFlag.Engine.Plugins.Tests/BlackFlag.Engine.Plugins.Tests.csproj`:
  reuse existing rendering project/assets in tests only.
- `tests/BlackFlag.Engine.Plugins.TestPlugin/NativeResolutionProbe.cs` and its
  project file: executable clean-process probe, native exports and system APIs.
- Active roadmap index, Future Roadmap and maintenance handoff: B11 status and
  final planned B16 reservation, full cleanup/profile scope and guardrails.
- This report and DocGen-derived index/validation/health outputs as applicable.

Production public/protected APIs, Skia adapters, dependency versions, H2b code,
fuzz seeds and release-gate configuration are unchanged. No speed claim is made.

## Validation commands and results

All commands were run from `C:\Development\PiratePDF` in regular PowerShell.
TRXs and publish/import logs are under the evidence directory above.

```powershell
dotnet test tests/BlackFlag.Engine.Plugins.Tests/BlackFlag.Engine.Plugins.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=b11-plugins-final-03.trx' --results-directory artifacts/fuzz/b11-native-20261027-01
dotnet test tests/BlackFlag.Engine.Rendering.Skia.Tests/BlackFlag.Engine.Rendering.Skia.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=b11-skia-suite.trx' --results-directory artifacts/fuzz/b11-native-20261027-01
dotnet publish tests/BlackFlag.Engine.Plugins.Tests/BlackFlag.Engine.Plugins.Tests.csproj -c Release --no-build --no-restore -o artifacts/fuzz/b11-native-20261027-01/publish-final-03
dotnet vstest artifacts/fuzz/b11-native-20261027-01/publish-final-03/BlackFlag.Engine.Plugins.Tests.dll --TestCaseFilter:FullyQualifiedName~PackagedNativeResolutionTests --logger:'trx;LogFileName=b11-published-final-03.trx' --ResultsDirectory:artifacts/fuzz/b11-native-20261027-01
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1
```

- Plugin suite: **48 passed, zero failed/skipped** (31 existing plus 17 new).
- Skia suite: **25 passed, zero failed/skipped**.
- Initial published fixture: **16 passed, zero failed/skipped**, before the
  selected-package-root regression was added. An intermediate published run
  passed all 17 cases. The final cleanup publish also passed **17/17, zero
  failures/skips** (`b11-published-final-03.trx`); publish itself succeeded.
- Initial sandboxed gate: source sizes passed; restore could not read the
  existing user NuGet configuration. The permitted-context reruns preserve
  the gate unchanged and do not confer Windows Administrator privilege.
- First permitted-context gate: **3,428 total, 3,422 passed, six failed, zero
  skipped**. Build: zero warnings/errors. Five failures were existing Windows
  symbolic-link fixture privilege errors; the sixth was the existing campaign
  timeout test `TimedOutWorkerAndDescendantAreReapedWithImmutableFinding`,
  missing its `tree.root` marker. Its focused followup passed 1/1; the failed
  broad run is retained, and no root cause or production repair is claimed.
- Final permitted-context gate after fixture cleanup: **3,428 total, 3,423
  passed, five failed, zero skipped**. Build: zero warnings/errors. The campaign
  timeout case passed in this full run. All five failures are fixture-creation
  privilege errors before the relevant security validators execute. Evidence:
  `release-gate-final.log` and `.exit` (exit 1). The gate stopped before DocGen.
- Documentation `all` was executed independently because the gate stopped at
  tests: **217 documents, zero errors/warnings/information findings**. Health:
  **91.4%, A-, zero errors/warnings**. Repair preview: **zero proposed changes,
  zero files, zero diagnostics**. `git diff --check` passed. These standalone
  results do not convert the failed gate into a passing gate.

The five failing tests in the agent's final run were unchanged:

1. `CorpusManifestTests.SeedLinkEscapeIsRejected`.
2. `FindingStorageTests.ArtifactLinkEscapeIsRejected`.
3. `CampaignTicketAdmissionTests.TicketDirectoryLinkIsRejectedAndForeignBytesPreserved`.
4. `CampaignRunnerTests.CampaignWorkspaceLinkIsRejected`.
5. `H2bStationAclApplicabilityTests.ReparseSubstitutionIsRejectedBeforeOwnerAdmission`.

None was skipped, weakened or waived. The requested followup used the
following **Administrator PowerShell** instructions because the observed
fixtures required symbolic-link privilege unavailable to the agent process:

```powershell
Set-Location -LiteralPath 'C:\Development\PiratePDF'
dotnet test .\tests\PiratePDF.Fuzz.Tests\PiratePDF.Fuzz.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~SeedLinkEscapeIsRejected|FullyQualifiedName~ArtifactLinkEscapeIsRejected|FullyQualifiedName~TicketDirectoryLinkIsRejectedAndForeignBytesPreserved|FullyQualifiedName~CampaignWorkspaceLinkIsRejected|FullyQualifiedName~ReparseSubstitutionIsRejectedBeforeOwnerAdmission' --logger 'trx;LogFileName=b11-link-fixtures-admin.trx' --results-directory .\artifacts\fuzz\b11-native-20261027-01\admin
if ($LASTEXITCODE -eq 0) { powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Invoke-PiratePDFReleaseGate.ps1 }
```

The owner subsequently reported full release-gate success with no failures
or warnings. This is not an agent-observed privileged run and does not establish
that the separate focused command was run. No additional rerun is requested
solely to repeat that confirmation. The success does not qualify or enable
native H2b and does not substitute for other-platform evidence.

## October 27 acceptance work (historical; superseded above)

1. Execute the new suite and applicable packaged-native checks on macOS and
   Linux with their actual system dependencies; retain platform-specific paths
   and failures. Static Mach-O/ELF inspection is not runtime validation.
2. Qualify official application packaging, architectures, transitive native
   dependency search and redirected-file cases. This repository's test publish
   does not supply official signed Windows/macOS packages.
3. Decide whether deterministic native-image release is required beyond the
   existing managed unload contract. Any explicit handle ownership/freeing
   correction needs a supported lifetime design and evidence.
4. Preserve the user-reported successful Windows gate and earlier failed
   attempts separately. Do not accept B11 or close POST-P2-004 from Windows-only
   tests, and do not turn retained fallback into a confinement guarantee.

No additional policy change is implemented or presumed approved. The current
physical containment approval does not authorize a global fallback allowlist,
privileged provisioning, transitive-loader redesign or H2b resumption.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.3 | 2026-10-31 | Record owner-confirmed success of the current full release gate as user-reported evidence; preserve failed runs and the missing application distribution/startup qualification gap. |
| 1.2 | 2026-10-31 | Record B11-D1 through D4, accepted native residency and Windows-only v1; verify existing engine suites, reconcile CI, and retain missing actual desktop distribution/startup as the Windows acceptance gap. |
| 1.1 | 2026-10-27 | Record owner-confirmed full release-gate success with no failures or warnings as user-reported evidence; preserve earlier failures and remaining platform, packaging and native-lifetime acceptance gaps. |
| 1.0 | 2026-10-27 | Record B11 characterization, owner-approved physical containment, Windows fixtures, unresolved platform/lifetime acceptance and final planned B16 reservation. |
