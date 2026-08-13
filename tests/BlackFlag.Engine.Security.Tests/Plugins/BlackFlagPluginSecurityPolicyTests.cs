using BlackFlag.Engine.Security.Plugins;

namespace BlackFlag.Engine.Security.Tests.Plugins;

public sealed class BlackFlagPluginSecurityPolicyTests
{
    [Fact]
    public void StandardResourceIsAllowedWithoutGrant()
    {
        var policy = new BlackFlagPluginSecurityPolicy();
        PluginSecurityDecision decision = policy.Evaluate(new("org.sample.plugin", PluginSecurityOperation.CapabilityAccess, "document.read", PluginSecuritySensitivity.Standard));
        Assert.True(decision.Allowed);
    }

    [Fact]
    public void PrivilegedResourceIsDeniedWithoutGrant()
    {
        var policy = new BlackFlagPluginSecurityPolicy();
        PluginSecurityDecision decision = policy.Evaluate(new("org.sample.plugin", PluginSecurityOperation.CapabilityAccess, "security.inspect", PluginSecuritySensitivity.Privileged));
        Assert.False(decision.Allowed);
    }

    [Fact]
    public void PrivilegedGrantMatchesExactPluginOperationAndResource()
    {
        var policy = new BlackFlagPluginSecurityPolicy([new PluginSecurityGrant("org.sample.plugin", PluginSecurityOperation.CapabilityAccess, "security.inspect")]);
        Assert.True(policy.Evaluate(new("org.sample.plugin", PluginSecurityOperation.CapabilityAccess, "security.inspect", PluginSecuritySensitivity.Privileged)).Allowed);
        Assert.False(policy.Evaluate(new("org.other.plugin", PluginSecurityOperation.CapabilityAccess, "security.inspect", PluginSecuritySensitivity.Privileged)).Allowed);
        Assert.False(policy.Evaluate(new("org.sample.plugin", PluginSecurityOperation.ExtensionRegistration, "security.inspect", PluginSecuritySensitivity.Privileged)).Allowed);
    }
}
