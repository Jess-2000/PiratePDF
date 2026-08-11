# ADR-0013 — Use One Free and Open-Source PiratePDF Edition

| Field | Value |
|---|---|
| **Document ID** | PP-ADR-013 |
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

PiratePDF was originally planned as two commercial editions sharing one
codebase and one BlackFlag Engine. The project direction has changed:
PiratePDF should provide the complete professional-capability feature set to
every user and should be developed and distributed as free and open-source
software.

Maintaining commercial feature tiers would add entitlement logic, product
matrices, packaging differences, upgrade paths, UI distinctions, and testing
combinations that do not contribute to document quality.

## Decision

PiratePDF will have one complete edition.

All implemented PiratePDF functionality—including advanced production,
prepress, automation, integration, reporting, color, OCR, editing, and other
specialized capabilities—belongs to the same product and is available without
paid-tier feature gating.

PiratePDF and BlackFlag Engine are committed to being free and open source.
The project shall not introduce:

- Commercial product editions.
- Premium-only feature sets.
- Subscription-gated functionality.
- Paid entitlement checks.
- Reduced engine quality for a lower tier.
- Commercial upgrade prompts whose purpose is to unlock implemented features.

BlackFlag Engine shall remain product-policy agnostic. Core engine APIs must
not depend on licensing tiers or entitlement state.

The exact open-source license is intentionally deferred to a separate explicit
decision. This ADR establishes the free/open-source product model but does not
select or imply a specific license.

## Consequences

- One product capability matrix and one primary distribution.
- Advanced production functionality is available to every PiratePDF user.
- No edition-gating architecture is required.
- UI and documentation do not need commercial-tier comparisons.
- Testing does not need tier-specific feature combinations.
- Plugin and SDK capabilities are available according to technical support and
  security policy rather than commercial entitlement.
- Third-party software, fonts, codecs, SDKs, and integrations may still have
  their own licenses and technical restrictions.
- A separate project decision must select the actual open-source license
  before the project's licensing terms are finalized.

## Supersedes

This ADR supersedes the edition-based product decision in ADR-0005. ADR-0005
remains in the repository as historical architectural context.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-08-09 | Established one complete free and open-source PiratePDF edition and removed paid-tier feature gating from the product model. |
