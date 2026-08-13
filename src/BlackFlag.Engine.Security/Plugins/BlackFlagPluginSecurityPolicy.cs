namespace BlackFlag.Engine.Security.Plugins;

/// <summary>Default deny-by-default policy for privileged plugin resources.</summary>
public sealed class BlackFlagPluginSecurityPolicy : IPluginSecurityPolicy
{
    public const int MaximumGrants = 4096;
    private readonly HashSet<GrantKey> grants;

    public BlackFlagPluginSecurityPolicy(IEnumerable<PluginSecurityGrant>? grants = null)
    {
        this.grants = new HashSet<GrantKey>();
        if (grants is null) return;
        foreach (PluginSecurityGrant grant in grants)
        {
            ArgumentNullException.ThrowIfNull(grant);
            if (this.grants.Count >= MaximumGrants) throw new ArgumentOutOfRangeException(nameof(grants), $"Plugin security grants cannot exceed {MaximumGrants} entries.");
            this.grants.Add(new(grant.PluginId, grant.Operation, grant.ResourceId));
        }
    }

    public PluginSecurityDecision Evaluate(PluginSecurityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Sensitivity == PluginSecuritySensitivity.Standard) return PluginSecurityDecision.Allow();
        return grants.Contains(new(request.PluginId, request.Operation, request.ResourceId))
            ? PluginSecurityDecision.Allow("Explicit privileged plugin grant matched.")
            : PluginSecurityDecision.Deny($"Plugin '{request.PluginId}' is not authorized for privileged {request.Operation} resource '{request.ResourceId}'.");
    }

    private readonly record struct GrantKey(string PluginId, PluginSecurityOperation Operation, string ResourceId);
}
