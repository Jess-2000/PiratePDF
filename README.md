# PiratePDF

PiratePDF is a free and open-source PDF and document-workflow project built around **BlackFlag Engine**, a modular .NET engine for parsing, inspecting, rendering, securing, editing, and writing PDF documents.

The project aims to make everyday PDF work easier while still supporting demanding professional and print-production workflows. PiratePDF is being designed as one complete product rather than a reduced free edition paired with a paid feature tier.

> **Project status:** PiratePDF is under active development. BlackFlag Engine already contains substantial parser, document-model, rendering, security, plugin, editing, and preservation-aware writing foundations, including completed BF-1054 existing-text editing/typography foundations, while the complete end-user application and additional workflows remain in development.

## What PiratePDF Aims to Do

PiratePDF is intended to become a complete document workspace for tasks such as:

- creating, opening, editing, and writing PDF documents;
- converting supported documents to PDF through controlled conversion paths;
- inspecting document structure, health, security, and embedded or active content;
- repairing damaged or malformed PDFs where recovery is possible;
- resizing pages and editing page geometry, metadata, annotations, forms, outlines, and reusable page decorations;
- rendering PDF content with fonts, images, graphics, color, transparency, patterns, shadings, and Type 3 content;
- optimizing documents and preparing them for print and production workflows;
- extending the application through a controlled plugin architecture; and
- preserving untargeted document structures whenever an edit does not require them to change.

Not every planned feature above is complete today. The roadmap and milestone documentation distinguish implemented foundations from future work.

## BlackFlag Engine

BlackFlag Engine is the document-processing core behind PiratePDF. Its architecture separates concerns such as parsing, document modeling, security analysis, rendering, editing, writing, services, SDK exposure, and plugin hosting so those responsibilities can evolve without binding the engine to the desktop UI.

A central design goal is **preservation-aware editing and writing**. Editing determines what the logical document becomes; Writing determines how that result is serialized. This separation helps PiratePDF avoid unnecessary document reconstruction and retain unrelated structures and object identities where practical.

The engine is designed to remain platform-agnostic. PiratePDF Version 1.0 targets Windows and macOS, while broader Linux support is planned for a later release.

## Current Engineering Areas

Development to date includes substantial work in these areas:

- PDF lexical, object, stream, cross-reference, and document loading infrastructure;
- damaged cross-reference recovery;
- content-stream parsing, resource resolution, graphics interpretation, and display lists;
- rendering for text, fonts, images, XObjects, color, transparency, patterns, shadings, and Type 3 content;
- document security analysis, passive remediation, standard-security authentication, and decryption foundations;
- plugin discovery, compatibility, lifecycle, capabilities, extension registration, and SDK isolation boundaries;
- transactional editing, undo/redo, structural editing, preservation contracts, and SDK/service boundaries; and
- preservation-aware full-rewrite and incremental PDF serialization, including traditional cross-reference tables and cross-reference streams; and
- existing-text discovery/editing, find/replace, typography/style/decorations, font embedding/subsetting paths, width-aware adjustment, and SDK/service exposure completed through BF-1054.


## Security Posture

PiratePDF is offline-first and treats documents, embedded resources, plugins, paths, metadata, and native/runtime boundaries as untrusted. Security work prioritizes bounded parsing and resource use, passive handling of active PDF content, fuzzing/adversarial corpus testing, least privilege, controlled native-library loading, plugin/SDK isolation, and minimizing sensitive document-data lifetime.

Official release hardening is expected to include dependency/SBOM review, artifact provenance and hashes, Windows Authenticode signing, and macOS code signing/notarization. Native AOT may be evaluated for deployment hardening and performance characteristics, but it is **not** treated as an obfuscation guarantee or security boundary.

## Documentation

The repository documentation is maintained as part of the product. Good starting points are:

- [Project Charter](docs/Project-Charter.md) — mission, product direction, engineering commitments, and success criteria.
- [Master Blueprint](docs/Blueprint/README.md) — entry point for the project vision, architecture, application specification, standards, testing strategy, and future roadmap.
- [Vision and Product Strategy](docs/Blueprint/Volume-I-Vision-and-Product-Strategy.md) — product goals and experience principles.
- [BlackFlag Engine Architecture](docs/Blueprint/Volume-II-BlackFlag-Engine-Architecture.md) — engine boundaries and architectural direction.
- [PiratePDF Application Specification](docs/Blueprint/Volume-III-PiratePDF-Application-Specification.md) — application-level requirements and behavior.
- [Engineering Standards](docs/Blueprint/Volume-IV-Engineering-Standards.md) — source quality, maintainability, and engineering expectations.
- [Testing and QA](docs/Blueprint/Volume-V-Testing-and-QA.md) — testing and release-quality strategy.
- [Future Roadmap](docs/Blueprint/Volume-VI-Future-Roadmap.md) — planned capabilities and deferred work.
- [Roadmaps](docs/02-Roadmaps/README.md) — roadmap documentation and work-planning entry point.
- [Architecture Decision Records](docs/ADR/README.md) — significant architectural decisions and their rationale.
- [Technical Specifications](docs/Technical-Specifications/README.md) — subsystem specifications and contracts.
- [Plugin Development](docs/04-Developer/Plugin-Development.md) — supported BlackFlag plugin authoring contract.
- [Building BlackFlag](docs/BUILDING-BLACKFLAG.md) — living engineering history and milestone-development practices.
- [Milestone Documentation](docs/milestones/) — detailed records of completed BlackFlag work slices.

The broader documentation library is available from [docs/README.md](docs/README.md).

## Repository Layout

```text
src/        BlackFlag Engine and PiratePDF source projects
tests/      Automated test projects
tools/      Repository tooling, including documentation generation
samples/    Reference samples and plugin examples
docs/       Architecture, roadmap, governance, specifications, and milestones
```

## Open Source and Contributions

PiratePDF is intended to remain fully free and open source. Contributions should follow the repository's architecture, documentation, testing, preservation, and independent-implementation standards. In particular, project code should be independently implemented from specifications and PiratePDF's own contracts rather than copied wholesale from external repositories.

Before contributing, review the [Project Charter](docs/Project-Charter.md), [Engineering Standards](docs/Blueprint/Volume-IV-Engineering-Standards.md), [Architecture Decision Records](docs/ADR/README.md), and the documentation relevant to the subsystem you plan to change.
