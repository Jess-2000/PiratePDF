# Document Form JavaScript Policy

| Field | Value |
|---|---|
| **Document ID** | PP-GOV-006 |
| **Category** | Governance |
| **Version** | 1.0 |
| **Status** | Approved |
| **Owner** | PiratePDF Project |
| **Related BF Work Items** | BF-1049, BF-1050, BF-1052, BF-8000 |
| **Last Updated** | 2026-11-08 |
| **Next Review** | 2027-02-08 |

## Authority and implementation status

The owner's revised B13 assignment approves this security model for interactive
PDF forms. This document is the canonical policy for PDF-embedded JavaScript
across BlackFlag Engine, SDK, Services, plugins and PiratePDF desktop integration.
References in other current documents defer to this policy.

The inspected implementation provides passive PDF action/script discovery,
shared security assessment, SDK reporting and structural form editing.
A sandboxed document-JavaScript runtime, interactive event dispatcher and
desktop Safe Mode require separately scoped implementation and qualification.
Until the required enforcement exists, document scripts remain inert and
unsupported scripted effects fail safely.

## Permitted local form behavior

Execution is conditional on supported events, sandbox enforcement and policy
admission. Permitted effects are confined to local document/form state.

- User-initiated button actions that operate on local document/form state.
- Local field calculations, formatting and validation.
- Dropdown, checkbox, radio-button and other interactive form behavior.
- Conditional field visibility and dependent field updates.
- Local autofill using values already available in the document/form.
- Supported form event handling whose effects remain within this local scope.

User interaction establishes event intent. It never grants unrestricted
permissions, and script APIs must never silently acquire additional privileges.
Form controls should use supported non-scripted behavior wherever practical.

## Prohibited effects

The following restrictions apply in every document-editing mode.

- Automatic document-open, document-close or unattended actions that execute
  document JavaScript.
- Network access, HTTP requests, remote APIs or external resource retrieval.
- Sending form values, document contents or private information to external
  destinations.
- Arbitrary filesystem access or reading user files outside the document's
  permitted scope.
- Launching external applications, commands, processes or scripts.
- Arbitrary code execution, dynamic module loading or sandbox escape.
- Privileged operations, security-policy modification or consent bypass.

Local autofill must use existing document/form values without reading external
sources. User consent to a button click cannot override these restrictions.
Unsupported and prohibited actions fail safely before executing their effects.

## Safe Mode and passive operations

Safe Mode disables document JavaScript execution and preserves non-scripted
form controls wherever supported. Importing, opening for inspection, rendering,
saving or inspecting a PDF must never trigger embedded JavaScript execution.
Opening a locally stored PDF confers no trust on its embedded scripts.
A previously recorded document-trust or fingerprint decision never grants
script execution capabilities or overrides these restrictions.

Passive source-document conversion continues to omit executable behavior under
ADR-0011. The form policy governs deliberately supported local PDF form events.
It supplies no script execution side effect to inspection or conversion.

## Enforcement and document authority

Document JavaScript is untrusted input. Future execution requires a bounded,
qualified sandbox that enforces execution/work, recursion and memory limits,
supports cancellation and restricts its API surface to admitted local effects.
A .NET process or collectible AssemblyLoadContext alone cannot provide that
boundary. Selecting a runtime or isolation architecture requires its own scope.

Script-originated field updates must pass through existing document ownership,
validation and transactional Editing boundaries. The authoritative PdfDocument,
rollback, undo/redo and preservation-aware Writing contracts also govern those
updates. The desktop must retain one engine-backed document state.

SDK and Services integration must enforce the same policy before admitting
effects. Inspection results such as AllowAndReport describe assessment and
reporting. Execution requires a separately implemented authorization boundary.
Plugin grants cannot override document policy or convert embedded scripts into
trusted application code.

Trusted PiratePDF application operations initiated explicitly by the user,
such as importing a selected local CSV file, have their own application
authorization and validation. Embedded scripts cannot invoke those operations
to obtain filesystem, network or process privileges.

## Qualification requirements

Future implementation acceptance must exercise supported local field events,
non-scripted controls, Safe Mode and consistent SDK/Services/desktop behavior.
It must directly reject every prohibited effect, automatic execution from each
passive operation, privilege acquisition and sandbox escapes. Resource limits,
transaction rollback and failed/unsupported event handling require evidence.
Existing passive inspection tests establish their own implemented scope.

## Related guidance

- [Project Charter](../../PROJECT-CHARTER.md)
- [Passive conversion](../Architecture/PASSIVE-DOCUMENT-CONVERSION.md)
- [SDK specification](../Technical-Specifications/BlackFlag-Engine/SDK.md)
- [Plugin contract](../04-Developer/Plugin-Development.md)

## Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-11-08 | Record the owner-approved local form-event policy, prohibited effects, Safe Mode, shared enforcement requirements and separately qualified runtime work. |
