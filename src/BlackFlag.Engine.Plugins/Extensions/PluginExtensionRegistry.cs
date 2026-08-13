using BlackFlag.Engine.SDK.Plugins;
using BlackFlag.Engine.SDK.Plugins.Extensions;

namespace BlackFlag.Engine.Plugins.Extensions;

/// <summary>Host-owned typed extension registry with plugin ownership tracking.</summary>
public sealed class PluginExtensionRegistry
{
    private readonly object sync = new();
    private readonly Dictionary<string, List<Entry>> entries = new(StringComparer.Ordinal);
    public IReadOnlyList<T> GetExtensions<T>(PluginExtensionPoint<T> extensionPoint) where T : class, IPluginExtension
    {
        ArgumentNullException.ThrowIfNull(extensionPoint);
        lock (sync)
        {
            if (!entries.TryGetValue(extensionPoint.Id, out List<Entry>? list)) return [];
            return list.Where(static e => e.ContractType == typeof(T)).Select(static e => (T)e.Extension).ToArray();
        }
    }
    internal void Commit(IReadOnlyList<PendingExtension> pending)
    {
        lock (sync)
        {
            foreach (IGrouping<string, PendingExtension> group in pending.GroupBy(static item => item.PointId, StringComparer.Ordinal))
            {
                Type contractType = group.First().ContractType;
                if (group.Any(item => item.ContractType != contractType) ||
                    (entries.TryGetValue(group.Key, out List<Entry>? existing) && existing.Any(e => e.ContractType != contractType)))
                    throw new InvalidOperationException($"Extension point '{group.Key}' has an incompatible contract type.");
            }
            foreach (PendingExtension item in pending)
            {
                if (!entries.TryGetValue(item.PointId, out List<Entry>? list)) entries.Add(item.PointId, list = []);
                list.Add(new(item.Owner, item.ContractType, item.Extension));
            }
        }
    }
    internal void RemoveOwnedBy(PluginId owner)
    {
        lock (sync)
        {
            foreach (string key in entries.Keys.ToArray())
            {
                entries[key].RemoveAll(e => e.Owner == owner);
                if (entries[key].Count == 0) entries.Remove(key);
            }
        }
    }
    private sealed record Entry(PluginId Owner, Type ContractType, object Extension);
}

internal sealed record PendingExtension(PluginId Owner, string PointId, Type ContractType, object Extension);
