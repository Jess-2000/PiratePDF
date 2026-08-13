using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Reporting;

namespace BlackFlag.Engine.Services.Security;

/// <summary>
/// Contains the shared assessment and optional ephemeral report produced by
/// one security-service evaluation.
/// </summary>
public sealed record DocumentSecurityServiceResult(
    DocumentSecurityAssessment Assessment,
    DocumentSecurityReport? Report);
