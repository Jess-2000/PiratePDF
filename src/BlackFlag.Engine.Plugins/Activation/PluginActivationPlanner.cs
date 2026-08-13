using BlackFlag.Engine.Plugins.Discovery;
using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.Plugins.Activation;

/// <summary>Builds a deterministic dependency-safe activation plan without loading plugin code.</summary>
public static class PluginActivationPlanner
{
    private enum VisitState { Unvisited, Visiting, Ready, Blocked }

    public static PluginActivationPlan Plan(PluginCatalog catalog, Version hostSdkVersion, PluginActivationPlanningOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog); ArgumentNullException.ThrowIfNull(hostSdkVersion);
        options ??= new PluginActivationPlanningOptions(); options.Validate();
        PluginDescriptor[] plugins = [.. catalog.Plugins.OrderBy(static p => p.Manifest.Id.Value, StringComparer.Ordinal)];
        if (plugins.Length > options.MaximumPlugins)
        {
            var limitIssues = plugins.Select(static p => new PluginActivationIssue(PluginActivationIssueKind.PluginLimitExceeded, p.Manifest.Id, null, "Plugin count exceeds the configured activation-planning limit.")).ToArray();
            return new PluginActivationPlan([], plugins, limitIssues);
        }
        var byId = plugins.ToDictionary(static p => p.Manifest.Id);
        var states = plugins.ToDictionary(static p => p.Manifest.Id, static _ => VisitState.Unvisited);
        var issues = new List<PluginActivationIssue>();
        var order = new List<PluginDescriptor>(plugins.Length);
        var stack = new List<PluginId>();
        var cycleIds = new HashSet<PluginId>();

        bool Visit(PluginDescriptor plugin, int depth)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PluginId id = plugin.Manifest.Id;
            if (states[id] == VisitState.Ready) return true;
            if (states[id] == VisitState.Blocked) return false;
            if (states[id] == VisitState.Visiting)
            {
                int start = stack.FindIndex(candidate => candidate == id);
                if (start < 0) start = 0;
                for (int i = start; i < stack.Count; i++) cycleIds.Add(stack[i]);
                cycleIds.Add(id);
                return false;
            }
            if (depth > options.MaximumDependencyDepth)
            {
                states[id] = VisitState.Blocked;
                issues.Add(new(PluginActivationIssueKind.DependencyDepthExceeded, id, null, "Plugin dependency depth exceeds the configured limit."));
                return false;
            }
            if (!PluginCompatibility.IsCompatible(plugin.Manifest, hostSdkVersion))
            {
                states[id] = VisitState.Blocked;
                issues.Add(new(PluginActivationIssueKind.SdkIncompatible, id, null, "Plugin is incompatible with the host SDK version."));
                return false;
            }
            states[id] = VisitState.Visiting; stack.Add(id);
            bool ready = true;
            foreach (PluginDependency dependency in plugin.Manifest.Dependencies.OrderBy(static d => d.Id.Value, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!byId.TryGetValue(dependency.Id, out PluginDescriptor? target))
                {
                    issues.Add(new(PluginActivationIssueKind.MissingDependency, id, dependency.Id, $"Required plugin dependency '{dependency.Id}' is missing.")); ready = false; continue;
                }
                if (!dependency.Accepts(target.Manifest.Version))
                {
                    issues.Add(new(PluginActivationIssueKind.DependencyVersionIncompatible, id, dependency.Id, $"Plugin dependency '{dependency.Id}' has an incompatible version.")); ready = false; continue;
                }
                if (!Visit(target, depth + 1)) ready = false;
            }
            stack.RemoveAt(stack.Count - 1);
            if (cycleIds.Contains(id)) ready = false;
            if (!ready)
            {
                states[id] = VisitState.Blocked;
                if (!cycleIds.Contains(id) && !issues.Any(issue => issue.PluginId == id))
                    issues.Add(new(PluginActivationIssueKind.DependencyBlocked, id, null, "A required plugin dependency is blocked."));
                return false;
            }
            states[id] = VisitState.Ready; order.Add(plugin); return true;
        }

        foreach (PluginDescriptor plugin in plugins) Visit(plugin, 1);
        foreach (PluginId id in cycleIds.OrderBy(static value => value.Value, StringComparer.Ordinal))
        {
            states[id] = VisitState.Blocked;
            if (!issues.Any(issue => issue.Kind == PluginActivationIssueKind.DependencyCycle && issue.PluginId == id))
                issues.Add(new(PluginActivationIssueKind.DependencyCycle, id, null, "Plugin participates in a dependency cycle."));
        }

        // A cycle can be discovered after a dependency was provisionally marked ready. Propagate
        // those late blocked states through reverse dependency edges once, avoiding repeated graph scans.
        var dependents = new Dictionary<PluginId, List<PluginId>>();
        foreach (PluginDescriptor plugin in plugins)
        {
            foreach (PluginDependency dependency in plugin.Manifest.Dependencies)
            {
                if (!byId.ContainsKey(dependency.Id)) continue;
                if (!dependents.TryGetValue(dependency.Id, out List<PluginId>? list)) dependents.Add(dependency.Id, list = []);
                list.Add(plugin.Manifest.Id);
            }
        }
        var blockedQueue = new Queue<PluginId>(states.Where(static pair => pair.Value == VisitState.Blocked).Select(static pair => pair.Key));
        var propagated = new HashSet<PluginId>(blockedQueue);
        while (blockedQueue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PluginId blockedId = blockedQueue.Dequeue();
            if (!dependents.TryGetValue(blockedId, out List<PluginId>? dependentIds)) continue;
            foreach (PluginId dependentId in dependentIds)
            {
                if (states[dependentId] != VisitState.Ready) continue;
                states[dependentId] = VisitState.Blocked;
                if (!issues.Any(issue => issue.PluginId == dependentId))
                    issues.Add(new(PluginActivationIssueKind.DependencyBlocked, dependentId, blockedId, "A required plugin dependency is blocked."));
                if (propagated.Add(dependentId)) blockedQueue.Enqueue(dependentId);
            }
        }

        order.RemoveAll(plugin => states[plugin.Manifest.Id] != VisitState.Ready);
        PluginDescriptor[] blocked = [.. plugins.Where(plugin => states[plugin.Manifest.Id] == VisitState.Blocked)];
        issues.Sort(static (a,b) => { int c=StringComparer.Ordinal.Compare(a.PluginId.Value,b.PluginId.Value); if(c!=0)return c; c=a.Kind.CompareTo(b.Kind); return c!=0?c:StringComparer.Ordinal.Compare(a.DependencyId?.Value,b.DependencyId?.Value); });
        return new PluginActivationPlan(order, blocked, issues);
    }
}
