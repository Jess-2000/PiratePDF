# BlackFlag Engine Reference Plugin

This project is the canonical BF-1050 SDK-only plugin example. It references only `BlackFlag.Engine.SDK` and demonstrates the supported manifest, lifecycle, optional capability retrieval, typed extension registration, cancellation, and cleanup contract.

It deliberately does not reference the plugin runtime, Security, Document, Parsing, Services, Rendering, or PiratePDF UI assemblies. Host applications discover and activate it through the runtime; plugin code itself does not gain host internals.

See `docs/04-Developer/Plugin-Development.md` for the complete developer contract and security limitations.
