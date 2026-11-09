# BlackFlag Engine and PiratePDF Project Charter

**Version:** 1.3
**Status:** Founding Charter
**Adopted:** 2026-07-31
**Last Updated:** 2026-11-08

> Dedicated to everyone who believes professional software should be built
> with care.

## Purpose

BlackFlag Engine exists to provide a professional PDF platform founded on
correctness, security, transparency, dependable performance, and long-term
maintainability.

PiratePDF exists to place that platform in the hands of individuals,
educators, businesses, print professionals, governments, and organizations
that need software they can trust with important documents.

The project does not ask users to choose between powerful features, reliable
engineering, and strong protection.

## Vision

Build one of the world's most trusted PDF platforms by combining:

- specification correctness,
- professional production capability,
- transparent security,
- predictable user control,
- responsive performance, and
- maintainable engineering.

The goal is not merely to create another PDF editor. The goal is to create
software that professionals can confidently rely upon every day.

## Mission

Design software that treats every document with care and every user with
honesty.

BlackFlag Engine faithfully interprets PDF data while treating document
content as untrusted input. PiratePDF provides professional capabilities
without compromising safety, transparency, or user control.

## Core Values

### Correctness

The project implements standards carefully and avoids deliberate shortcuts
that undermine interoperability, safety, or long-term reliability.

### Transparency

When BlackFlag blocks an action, reports a warning, repairs a structure, or
changes a document, the user should be able to understand what happened and
why.

### Security

Every document, embedded resource, plugin input, native/runtime boundary, path,
and external dependency is treated as untrusted until safely validated.

Security is a guarantee, not an upgrade. It comes from reviewable architecture,
bounded inputs, least privilege, strong isolation, and verifiable engineering—not
from obscurity.

### User Control

Documents do not make security-sensitive decisions for users. Explicit user
intent governs external, destructive, network, extraction, and sanitization
actions.

### Maintainability

Today's engineering decisions should simplify tomorrow's development.
Responsibilities remain focused, architecture remains modular, and important
decisions are documented.

### Professionalism

Features are judged by reliability, predictability, usability, performance,
and long-term value—not merely by whether they appear to work in a narrow
demonstration.

## Product Promise

PiratePDF is one complete product. Every feature that belongs to PiratePDF is
available to every user without paid tiers, edition gates, subscriptions, or
feature entitlements.

PiratePDF is committed to being free and open source. The exact open-source
license is governed by a separate project decision and must be selected
explicitly before a release is represented as licensed under that specific
license.

### Safety

Every user receives the complete BlackFlag security protections. Packaging,
distribution, or licensing decisions never weaken the security boundary.

### Transparency

Every user can inspect security findings, active content, embedded files,
embedded objects, embedded media, and the protections applied by BlackFlag.

Knowledge is not a premium feature.

### Reliability

Every user receives the same BlackFlag Engine, the same document fidelity, and
the same production capabilities.

> Every user deserves the same level of protection, the same level of
> transparency, and access to the complete PiratePDF capability set.

## BlackFlag Security Principles

### Passive by Default

A document is data, not executable instructions.

Document-open, document-close and unattended document-JavaScript execution are
prohibited. Supported local form events may execute only within the qualified
sandbox and policy scope defined by PP-GOV-006. Safe Mode disables document
scripts while preserving supported non-scripted controls. Importing, rendering,
saving and inspection remain passive. Document scripts cannot acquire external
I/O, transmission, filesystem, process, code-loading or privileged authority.
[Document form policy](docs/05-Governance/Document-Form-JavaScript-Policy.md).

### Explicit User Intent

Opening a document never launches software, contacts a network destination,
submits data, opens an attachment, or extracts embedded content automatically.

### Preservation Before Modification

Opening a document does not alter the original file.

Inspection, protection, removal, repair, and sanitization are separate
operations. A sanitized or repaired copy is created only through an explicit
user command.

### Explainable Security

Every security decision should be explainable. Users should be able to see
what was detected, what was blocked, and why the policy exists.

### Layered Security

Raw scanning, structural parsing, security analysis, policy enforcement, and
the application interface remain separate layers with focused
responsibilities.

### Verifiable Distribution

Official PiratePDF releases should be traceable to reviewed source and approved
build inputs. Release processes protect signing credentials, record artifact
identity and provenance, validate bundled dependencies, and use platform trust
mechanisms appropriate to the supported operating systems.

Windows 10/11 x64 is the official v1 release target under PP-GOV-005.
The Windows signing/provenance contract records the required publisher,
source/build identity, final SHA-256 hashes and protected credential boundary.
Production provisioning and actual signed-release qualification are future work.
[Release signing and provenance](docs/releases/Windows-Release-Signing-and-Provenance.md).

Release hardening may include native compilation, trimming, or symbol reduction,
but those mechanisms never replace secure design, validation, or transparency.

### Universal Protection

Every PiratePDF user receives the same passive protection, security preflight,
folder scanning, rule customization, document inventory, security summary,
high-volume reporting capabilities, workflow automation capabilities, and
other implemented security or production features.

## Product Philosophy

PiratePDF has one complete edition with the full capability set.

Capabilities intended for demanding production environments—such as
variable-data integration, advanced color management, high-volume automation,
production reporting, managed deployment, and production-system
integration—are normal PiratePDF functionality rather than paid-tier
differentiators.

The project does not intentionally restrict implemented functionality behind
commercial editions, premium tiers, subscriptions, or entitlement checks.

## Engineering Standards

A milestone is not complete merely because code compiles.

Where applicable, each milestone includes:

- production-ready implementation,
- automated tests,
- analyzer compliance,
- a milestone specification,
- Architectural Decision Records,
- documentation validation,
- reproducible release verification,
- security/adversarial validation proportional to the affected risk surface,
- dependency and release-artifact provenance where applicable,
- release automation, and
- a traceable release tag.

## Documentation Hierarchy

The repository should answer four questions:

- **Why does the project exist?** — this Charter.
- **Why was an architecture chosen?** — Architectural Decision Records.
- **What was implemented?** — milestone specifications.
- **How was it verified and released?** — release records and generated
  documentation.

## Definition of Success

Success is not measured solely by feature count, lines of code, or release
frequency.

Success means PiratePDF remains:

- correct,
- secure,
- transparent,
- maintainable,
- performant,
- verifiably distributed, and
- trusted.

## The North Star Test

Before accepting a significant decision, ask:

1. Does this make PiratePDF more trustworthy?
2. Does this preserve or increase user control?
3. Is the behavior transparent and explainable?
4. Can this be maintained five years from now?
5. Would we make the same decision if millions of users relied on it?
6. Can users and contributors verify that official releases came from the
   project and were produced through a controlled, reviewable process?

A negative answer requires the design to be reconsidered.

## What We Will Not Compromise

The project will not intentionally sacrifice:

- user safety,
- engineering integrity,
- transparency,
- maintainability, or
- specification correctness

for short-term convenience.

Features may evolve. Architecture may improve. Technology will change. These
principles remain.

## Motto

> **Trust the specification. Trust the user. Never trust the document.**

## The Builder's Promise

> We will build carefully.
> We will test thoroughly.
> We will document honestly.
> We will protect our users.
> We will respect the standards.
> We will admit mistakes.
> We will improve continuously.
> We will never sacrifice long-term quality for short-term convenience.
> Because professional software deserves professional craftsmanship.

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.3 | 2026-11-08 | Reconcile owner-approved local form-event policy, passive-operation restrictions and Windows release provenance requirements. |
| 1.1 | 2026-08-09 | Founding charter baseline for PiratePDF and BlackFlag Engine. |
| 1.2 | 2026-09-05 | Strengthened durable security principles for native/runtime/dependency trust boundaries, verifiable distribution, release provenance, and risk-proportional adversarial validation. |

## Closing Statement

BlackFlag Engine and PiratePDF represent a commitment to professional tools
that users can rely upon with confidence.

Every parser, editor, security feature, release, and architectural decision
should move the project closer to that goal.

When a future contributor faces a difficult decision, the answer should begin
here.
