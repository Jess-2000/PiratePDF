
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Policy;

/// <summary>
/// Applies shared BlackFlag policy to format-normalized security observations.
/// </summary>
public interface IDocumentSecurityPolicy
{
    /// <summary>Evaluates one observation for the requested operation.</summary>
    DocumentSecurityPolicyDecision Evaluate(
        DocumentSecurityObservation observation,
        DocumentSecurityOperation operation);
}
