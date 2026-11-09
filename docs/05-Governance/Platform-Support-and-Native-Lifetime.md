# PiratePDF v1 Platform Support and Native Lifetime

| Field | Value |
|---|---|
| **Document ID** | PP-GOV-005 |
| **Category** | Governance |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1050, BF-1054 |
| **Last Updated** | 2026-10-31 |
| **Next Review** | 2027-01-31 |

## Owner-approved v1 decisions

The owner's B11 final Windows qualification assignment establishes decisions
B11-D1 through B11-D4 on 2026-10-31. These supersede earlier current product
plans that included macOS in v1 qualification. Historical reports retain their
original platform requirements and results; this decision does not invent
validation evidence or retrospectively change those results.

| Decision | Approved scope | Qualification consequence |
|---|---|---|
| B11-D1 | Windows 10/11 x64 application distribution | Qualify the real application, its required assets, bounded startup, native loading and representative functionality. Installer, signing, certificates and publication are outside B11. |
| B11-D2 | Existing managed unload/disposal contract; native modules may remain process-resident | Immediate deterministic native-image unloading is not required. No manual FreeLibrary calls, custom handle registries or speculative release of runtime-owned handles. |
| B11-D3 | Windows 10/11 x64 is the sole official v1 target | Linux/macOS are deferred to future community-led support, not v1 release blockers. Preserve platform-neutral architecture and practical compatibility. |
| B11-D4 | Narrow Windows-first CI alignment | Windows quality/security controls remain blocking. Linux compatibility may be optional; no weakened assertions or duplicated full-gate execution. |

## Support and qualification matrix

| Platform | Official v1 support scope | Current qualification status |
|---|---|---|
| Windows 10 x64 | Required | Application distribution/startup unverified; no desktop entry project exists in this checkout |
| Windows 11 x64 | Required | Engine/plugin/native fixture integration verified on the local Windows x64 host; application distribution/startup unverified |
| Windows x86/ARM64 | Not an official v1 target | Asset availability is not application qualification |
| Linux/macOS | Deferred community-led support | Optional compatibility signals only; existing cross-platform implementation is retained |

The owner confirmed on 2026-10-31: **"There is no desktop entry project yet"**.
Source/project inventory corroborates this: `src/` contains engine libraries;
executable projects are tests or tools. There is no application publish profile,
executable, app-ready signal or desktop PDF-open operation to qualify. A
published test fixture or native provisioning tool must not be represented as
PiratePDF's application distribution. No deployment model is selected in the
absence of that application; building an application is outside B11 scope.

## Native lifetime and fallback

Managed plugin deactivation, disposal and collectible-context unload requests
remain required. Native modules may remain loaded for the application process's
lifetime. Process termination is the reliable final release boundary for
process-owned native resources. Replacing an already-loaded native dependency
may require restarting the application. The original exploratory native-file
lock result remains historical evidence, not an unresolved v1 unloading gate.

Retain canonical physical package-root admission, rejection of observed
outside-package link/junction escapes, valid within-package redirection,
bounded traversal, unresolved-name runtime fallback and legitimate system-native
loading. Path validation is not race-proof loading, an OS sandbox or universal
containment of transitive native imports. No production ownership behavior is
changed by this decision.

## CI and continuation

The existing Windows Build and Test job retains all tests without a platform
filter, analyzers through the solution build, TRX/category validation and
cleanliness checks. Documentation generation, health and zero-change repair
checks now run there as blocking checks. The Linux job remains present with
workflow-level `continue-on-error` as an optional compatibility signal.
Remote branch-protection requirements and hosted workflow results have not
been inspected or changed in this local assignment. Administrators must ensure
required checks select Windows controls rather than optional Linux signals.

There is no real application to publish in a distribution-qualification CI
step. That step remains a documented gap, not a passing placeholder. Once an
approved desktop project exists, qualify its actual win-x64 distribution
outside repository/build paths, including missing/invalid native assets,
intended module paths, a bounded ready signal and owned native-backed PDF
fixtures. A clean Windows 10/11 environment remains necessary evidence.

The unchanged authoritative release gate remains mandatory. Existing source
size validation is retained there; CI parity remains POST-P2-008. Native H2b
stays deferred, disabled and unqualified under PP-GOV-004. B9 remains open.
B16 remains the final planned maintenance batch after POST-P2-008 for semantic
dead/obsolete-code cleanup and measured managed-fuzzer optimization, with
unchanged API, replay, seed, coverage, resource-limit and security guarantees.
B16 is not implemented here. B12 needs a separate scoped assignment.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-10-31 | Record owner-approved B11-D1 through D4, Windows-only v1 scope, process-resident native lifetime, missing desktop qualification target and narrow CI alignment. |
