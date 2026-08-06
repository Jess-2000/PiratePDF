# BlackFlag Engine and PiratePDF Project Charter

**Version:** 1.0
**Status:** Founding Charter
**Adopted:** 2026-07-31

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

Every document is treated as untrusted until safely analyzed.

Security is a guarantee, not an upgrade.

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

Every PiratePDF edition provides the same core promises.

### Safety

Every user receives the complete BlackFlag security protections. Licensing
never weakens the security boundary.

### Transparency

Every user can inspect security findings, active content, embedded files,
embedded objects, embedded media, and the protections applied by BlackFlag.

Knowledge is not a premium feature.

### Reliability

Every edition uses the same BlackFlag Engine. Standard and Professional differ
through specialized workflow and production capability—not through
trustworthiness.

> Every user deserves the same level of protection, the same level of
> transparency, and the same confidence that opening a document will never
> compromise their system. Professional editions expand capability—not trust.

## BlackFlag Security Principles

### Passive by Default

A document is data, not executable instructions.

BlackFlag does not automatically execute document-supplied JavaScript, open
actions, additional actions, launch actions, automatic form submissions,
external navigation, embedded executables, rich media, or multimedia.

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

### Universal Protection

Standard and Professional users receive the same passive protection, security
preflight, folder scanning, rule customization, document inventory, and
security summary.

Professional editions may add high-volume reporting, enterprise
administration, and workflow automation, but never stronger basic protection.

## Product Philosophy

PiratePDF Standard is complete. PiratePDF Professional is specialized.

Standard includes the capabilities required to safely inspect, understand,
edit, repair, sanitize, and produce professional PDF documents.

Professional extends Standard with specialized production and organizational
workflows such as variable-data integration, advanced color management,
high-volume automation, enterprise reporting, managed deployment, and
production-system integration.

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
- performant, and
- trusted.

## The North Star Test

Before accepting a significant decision, ask:

1. Does this make PiratePDF more trustworthy?
2. Does this preserve or increase user control?
3. Is the behavior transparent and explainable?
4. Can this be maintained five years from now?
5. Would we make the same decision if millions of users relied on it?

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

## Closing Statement

BlackFlag Engine and PiratePDF represent a commitment to professional tools
that users can rely upon with confidence.

Every parser, editor, security feature, release, and architectural decision
should move the project closer to that goal.

When a future contributor faces a difficult decision, the answer should begin
here.
