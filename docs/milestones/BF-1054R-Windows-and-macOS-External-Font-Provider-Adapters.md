# BF-1054R — Windows and macOS External Font Provider Adapters

| Field | Value |
|---|---|
| **Document ID** | BF-1054R |
| **Category** | Milestone |
| **Version** | 1.0 |
| **Status** | Implemented |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1054 |
| **Last Updated** | 2026-09-05 |
| **Next Review** | 2026-12-05 |

## Objective

Connect BF-1054H/Q's platform-neutral external-font contracts to the officially targeted PiratePDF v1 desktop platforms without placing operating-system font discovery inside BlackFlag.Engine.Fonts.

## Assembly Boundary

R adds:

`BlackFlag.Engine.Fonts.Platform`

The assembly depends on `BlackFlag.Engine.Fonts`.

`BlackFlag.Engine.Fonts` does not depend on the platform assembly.

This preserves the portable font-domain boundary.

## Providers

R adds:

- `PdfWindowsExternalFontProvider`;
- `PdfMacOSExternalFontProvider`;
- `PdfPlatformExternalFontProviders`.

The default factory returns the provider appropriate for the current supported operating system.

Other operating systems return no default provider in v1.

## Windows Discovery

The Windows provider searches:

- the system Fonts special folder;
- the current user's `%LOCALAPPDATA%\Microsoft\Windows\Fonts`.

## macOS Discovery

The macOS provider searches:

- `/System/Library/Fonts`;
- `/Library/Fonts`;
- `~/Library/Fonts`.

R supports standalone `.ttf` and `.otf` files.

Font collections and web-font containers remain outside the v1 provider path because the sanitizer accepts standalone SFNT programs only.

## Sanitizer-Backed Discovery

Installed font files are still untrusted input.

Each candidate is passed through `PdfOpenTypeFontSanitizer` before its metadata or glyph coverage is exposed by the provider.

Malformed, unreadable, unsupported, or sanitizer-rejected candidates are omitted.

The provider does not execute font bytecode.

Q re-sanitizes the raw font stream before embedding, preserving the mandatory trust boundary at the actual use site.

## Sanitized Metadata Inspector

R adds `PdfSanitizedExternalFontMetadataInspector` to the portable Fonts assembly.

It reads only sanitized programs and exposes:

- family name;
- face name;
- weight;
- stretch;
- slant;
- embedding rights;
- Unicode scalar coverage.

The inspector reuses the existing validated SFNT and cmap parsing architecture rather than adding platform-specific binary parsing.

## Provider Cache

Providers lazily build an in-memory snapshot containing:

- immutable face descriptors;
- canonical file paths;
- sanitized metadata.

No persistent font cache is added.

The cache contains no raw font bytes.

`OpenFontProgram` opens the original installed font only when the existing `IPdfExternalFontProvider` contract is used.

## Stable Face Identity

Face IDs are deterministic SHA-256 hashes of canonical font paths within the provider.

Provider IDs remain stable platform identifiers.

The path itself is not exposed through `PdfExternalFontFace`.

## Failure Policy

Installed-font enumeration is best effort.

Individual candidate failures do not make the entire system font catalog unavailable.

Unsupported files are omitted rather than bypassing validation.

A face supplied to the wrong provider is rejected.

A stale/unrecognized face ID is rejected.

## Security and Data Protection

R introduces no network access and no persistent PiratePDF cache.

It does not copy installed fonts to temporary files.

The project-wide data protection invariant remains:

- protected data in transit uses TLS 1.2 or higher;
- protected PiratePDF-managed persisted data uses AES-256 authenticated encryption.

The operating system's installed-font files remain OS-managed source files; PiratePDF does not reclassify them as PiratePDF-managed persisted copies merely by reading them.

## Acceptance Criteria

1. Portable Fonts remains platform-agnostic.
2. Platform adapters live in a separate assembly.
3. Windows system fonts can be discovered.
4. Windows per-user fonts can be discovered.
5. macOS system fonts can be discovered.
6. macOS user fonts can be discovered.
7. Only standalone TTF/OTF candidates are considered.
8. Candidate metadata is exposed only after sanitization.
9. Invalid candidates are omitted.
10. Family and face names are read from sanitized name tables.
11. Weight/stretch/slant are derived from sanitized metadata.
12. Embedding rights are surfaced.
13. Unicode coverage is available through `SupportsRune`.
14. Provider raw streams remain untrusted.
15. Q continues to sanitize again before embedding.
16. Face IDs are deterministic and path text is not exposed.
17. The default provider factory is OS-aware.
18. No persistent font cache or temporary copy is introduced.
19. No network path is introduced.
20. The full PiratePDF release gate passes.

## Deferred Work

- TTC/OTC collection-face selection;
- WOFF/WOFF2;
- Linux platform providers;
- dedicated complex-script shaping;
- superscript/subscript, color, compound operators, and decorations;
- whole-document orchestration and width-aware local adjustment;
- public SDK exposure;
- final BF-1054 hardening.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-09-05 | Added sanitizer-backed Windows/macOS installed-font providers, portable sanitized metadata inspection, OS-aware provider factory, and in-memory-only catalog caching. |
