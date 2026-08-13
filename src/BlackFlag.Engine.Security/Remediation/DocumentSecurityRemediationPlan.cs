namespace BlackFlag.Engine.Security.Remediation;

/// <summary>Describes the deterministic remediation boundary for one sanitizing operation.</summary>
public sealed class DocumentSecurityRemediationPlan
{
    private readonly DocumentSecurityRemediationAction[] actions;

    /// <summary>Initializes a remediation plan.</summary>
    public DocumentSecurityRemediationPlan(
        IEnumerable<DocumentSecurityRemediationAction> actions,
        bool isBlocked)
    {
        ArgumentNullException.ThrowIfNull(actions);
        this.actions = actions.ToArray();
        IsBlocked = isBlocked;
    }

    /// <summary>Gets the ordered remediation actions.</summary>
    public IReadOnlyList<DocumentSecurityRemediationAction> Actions => actions;

    /// <summary>Gets whether the requested operation must not proceed.</summary>
    public bool IsBlocked { get; }

    /// <summary>Gets whether content must be removed or neutralized.</summary>
    public bool RequiresMutation =>
        actions.Any(static action =>
            action.Intent != DocumentSecurityRemediationIntent.RejectOperation);
}
