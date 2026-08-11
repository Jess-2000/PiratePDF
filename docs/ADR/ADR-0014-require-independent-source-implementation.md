# ADR-0014 — Require Independent Source Implementation

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-014 |
| **Category** | Architecture Decision Record |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-1048, BF-8000 |
| **Last Updated** | 2026-08-09 |
| **Next Review** | 2026-11-09 |

---

**Status:** Accepted  
**Date:** August 9, 2026

## Context

PiratePDF and BlackFlag Engine are intended to be free and open-source
software, with the MIT License planned for the project's own source code.

The project needs a clear source-provenance rule so that implementation work
does not unintentionally incorporate code with incompatible licensing terms,
uncertain provenance, or obligations that were not reviewed.

This is especially important for areas such as PDF parsing and rendering,
fonts, character encodings, CMaps, glyph mapping, text shaping, image codecs,
color management, ICC processing, SVG and EPS support, OCR, and other
subsystems for which existing open-source implementations are readily
available.

At the same time, software development routinely uses common programming
idioms, short conventional snippets, framework patterns, and standard UI
templates that do not represent meaningful copying of a distinctive external
implementation. PiratePDF also uses Avalonia, whose normal application and UI
patterns should remain practical to use.

A limited exception is therefore required without weakening the project's
general independent-implementation policy.

## Decision

PiratePDF and BlackFlag Engine shall use independent implementation as the
default development model.

External source repositories shall not ordinarily be used as implementation
sources. Project code shall be designed and implemented from permitted
technical sources and PiratePDF's own architecture rather than by copying,
porting, translating, transcribing, or reconstructing another project's
implementation.

### Permitted implementation sources

Development may rely on:

- Applicable specifications and standards.
- Official framework, platform, library, and API documentation.
- Authoritative registries and technical data sources whose use and
  redistribution terms have been reviewed when necessary.
- PiratePDF and BlackFlag Engine source code, tests, contracts, architecture,
  and governed project documentation.
- Independently developed algorithms, experiments, interoperability tests,
  and test cases.
- Reviewed external libraries consumed as dependencies without copying their
  implementations into PiratePDF.

### Routine code and framework-pattern exception

Standard short code snippets, routine programming idioms, conventional
framework usage, and common Avalonia UI patterns or templates may be used
without project-specific source attribution when they are genuinely
commonplace and do not reproduce meaningful distinctive authorship.

Examples can include ordinary:

- Avalonia bindings and control declarations.
- Common control-template and styling patterns.
- Event-handler wiring.
- Dependency-injection registration.
- Property declarations and routine validation guards.
- Standard collection iteration.
- Conventional asynchronous API usage.
- Framework initialization and configuration patterns.

This exception is based on the routine or commonplace nature of the material.
It must not be interpreted as declaring external source code to be public
domain.

### Limited MIT-source exception

When adequate specifications, official documentation, or other suitable
resources are unavailable, developers may reference an MIT-licensed
repository and may use a small code snippet from it when reasonably necessary.

This exception is subject to all of the following:

1. The external repository must be verified as MIT-licensed for the material
   being referenced.
2. The amount used must remain small and proportionate to the need.
3. The exception must not be used to copy a subsystem, substantial
   implementation, or distinctive architecture.
4. Routine/common snippets do not require project-specific attribution merely
   because a similar example appears in an MIT repository.
5. When copied material is sufficiently distinctive or nontrivial for the MIT
   license conditions to apply, its applicable copyright and MIT license
   notice must be preserved and its provenance recorded.
6. Uncertain licensing or provenance disqualifies the material from this
   exception until the uncertainty is resolved.

### Prohibited implementation sources

Unless a later approved ADR explicitly changes this policy, PiratePDF source
must not incorporate implementation code copied, adapted, ported, translated,
or substantially derived from:

- GPL, AGPL, LGPL, MPL, Apache, BSD, proprietary, source-available, or other
  non-MIT repositories.
- Repositories whose applicable license cannot be established.
- Code fragments of uncertain provenance.
- Decompiled or reverse-engineered third-party source implementations.
- Large or substantial portions of an MIT-licensed implementation under the
  limited-snippet exception.

The presence of a permissive license does not by itself make substantial
copying consistent with PiratePDF's independent-development policy.

## External Dependencies

Using an external package or library as a dependency is distinct from copying
its implementation into PiratePDF.

Dependencies may use licenses other than MIT when their licenses and
distribution requirements are compatible with PiratePDF's intended
distribution model and have been reviewed.

The project shall maintain a third-party dependency record covering, as
appropriate:

- Dependency name.
- Version.
- Purpose.
- License.
- Required copyright or attribution notices.
- Native or transitive components that introduce additional distribution
  obligations.

Third-party dependency licensing does not change the license applied to
PiratePDF's independently authored source code.

## Specifications, Registries, and Data Tables

Implementing behavior defined by a specification is permitted, but source code
from another implementation of that specification is not automatically a
permitted implementation source.

Normative or authoritative data such as character mappings, glyph-name data,
CMaps, encoding tables, ICC data, registries, or similar datasets must have a
known source and an established basis for use and redistribution before being
committed when copyright or licensing may apply.

Where practical, generated representations should be produced from an
authoritative permitted data source rather than copied from another software
project's generated tables.

## Contributions

Contributed code must have sufficiently clear provenance to comply with this
policy.

Code should not be merged when there is a material unresolved question about
whether it was copied or substantially derived from an incompatible or
unapproved source.

Normal code review does not require developers to document the origin of
routine programming constructs or common Avalonia patterns.

## Remediation

If existing or future code is found to violate this policy, the affected code
or data shall be isolated and evaluated.

When necessary, it shall be removed and independently reimplemented from
permitted sources without consulting the disallowed implementation during the
rewrite.

Required third-party notices shall be restored when the issue is missing
attribution for material that is otherwise permitted.

## Consequences

- BlackFlag Engine maintains a strong independent implementation history.
- The project reduces the risk of accidentally incorporating incompatible
  source-code licensing obligations.
- Developers may continue using specifications and official documentation
  normally.
- Routine programming constructs and common Avalonia UI patterns do not create
  unnecessary provenance paperwork.
- Small MIT-licensed snippets remain available as a narrow fallback when other
  adequate resources do not exist.
- Distinctive MIT-licensed material retains applicable notice obligations.
- Third-party dependencies are reviewed separately from project source-code
  provenance.
- Future font, rendering, codec, color, and OCR work must consider provenance
  before importing tables, data, or implementation details.
- The policy intentionally favors independent implementation even where a
  third-party license might legally permit broader copying.

## Related Decisions

- ADR-0000 — Treat the Project Charter as the Governing Project Document.
- ADR-0013 — Use One Free and Open-Source PiratePDF Edition.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-09 | Established independent implementation as the default source-provenance policy, with narrow exceptions for routine/common Avalonia patterns and small MIT-licensed snippets when adequate alternative resources are unavailable. |
