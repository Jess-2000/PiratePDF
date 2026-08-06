# Building BlackFlag

| Field | Value |
|---|---|
| **Document ID** | PP-HIST-001 |
| **Category** | Developer Guide |
| **Version** | 0.1 |
| **Status** | In Development |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1000, BF-1039, BF-1040 |
| **Last Updated** | 2026-08-06 |
| **Next Review** | 2026-11-06 |

> Dedicated to everyone who believes professional software should be built
> with care.

## Purpose

This living document preserves how BlackFlag Engine is built, what the project
learns, and why its engineering process evolves. It complements the Project
Charter, ADRs, milestone specifications, and release records.

## The Builder's Promise

We build carefully. We test thoroughly. We document honestly. We protect our
users. We respect standards. We admit mistakes. We improve continuously. We
do not trade long-term quality for short-term convenience.

## Repository-First Development

Every milestone begins from a current repository snapshot. Architecture is
reviewed before implementation, production packages are repository-integrated,
and release gates validate the complete solution rather than isolated files.

## Milestone Lifecycle

1. Repository analysis
2. Architecture proposal and approval
3. Foundation contracts and governance
4. Core implementation
5. Integration testing
6. Documentation validation
7. Release verification
8. Commit, merge, tag, and publish
9. Milestone reflection

## BF-1039 Reflection

### What We Set Out to Do

Add exact raw PDF stream parsing without mixing binary payload handling into
the general object parser.

### What We Learned

Separating `PdfStreamParser` from `PdfObjectParser` simplified testing and
created a clean boundary for filter decoding. The milestone also exposed
release-tool compatibility issues that strengthened future package templates.

### What This Taught Us

Architecture is measured not only by code organization, but by how much
future complexity it prevents.

## BF-1040 Opening

BF-1040 begins the stream-filter framework and introduces bounded decoding,
memory architecture, passive conversion governance, and pure decoder
contracts. It is the first milestone in which parser capability, platform
architecture, security, and memory policy are developed together.

## Future Chapters

Each completed milestone will add:

- what was attempted;
- what was delivered;
- what failed or surprised us;
- what the project learned; and
- how the next milestone changed as a result.

## Chapter 100 — The Next Decision

Every milestone begins with a decision. This section is reserved for the next
one.

## Revision History

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-08-06 | Established the living BlackFlag engineering history. |
