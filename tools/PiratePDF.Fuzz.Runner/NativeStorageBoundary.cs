using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Retains a verified live fixed-volume boundary and its controller-owned workspace directories.</summary>
internal sealed class NativeStorageBoundary : IDisposable
{
    private readonly WindowsVirtualDisk disk;
    private readonly List<SafeFileHandle> directories = [];
    internal string Root { get; }
    internal string Corpus => Path.Combine(Root, "corpus");
    internal string Artifacts => Path.Combine(Root, "artifacts");
    internal string Temporary => Path.Combine(Root, "temp");

    internal NativeStorageBoundary(NativeProvisioningPlan plan, NativeExecutionPolicy policy)
    {
        if (plan.Capacity != policy.StorageBytes) throw new IOException("Requested capacity differs from the provisioned boundary.");
        disk = new(plan.BackingPath, plan.BackingIdentity, plan.DiskId, plan.MountRoot, plan.VolumeName, plan.Capacity);
        Root = Path.Combine(plan.MountRoot, "pph2b_" + plan.RunId);
        try
        {
            foreach (string path in new[] { plan.MountRoot, Root, Corpus, Artifacts, Temporary })
            {
                RequireWithin(plan.MountRoot, path);
                directories.Add(WindowsNativeMethods.OpenDirectory(path));
            }
            string[] contents = Directory.EnumerateFileSystemEntries(Root).Take(4).ToArray();
            if (contents.Length != 3 || contents.Any(path => Path.GetFileName(path) is not ("corpus" or "artifacts" or "temp")))
                throw new IOException("Execution workspace contains foreign entries.");
            foreach (string path in new[] { Corpus, Artifacts, Temporary })
                if (Directory.EnumerateFileSystemEntries(path).Any()) throw new IOException("Execution directories must be fresh; uncertain volumes are never reused.");
        }
        catch { Dispose(); throw; }
    }

    internal static void RequireWithin(string root, string path)
    {
        NativeLaunchPolicy.ValidatePath(root);
        NativeLaunchPolicy.ValidatePath(path);
        string expected = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string actual = Path.GetFullPath(path);
        if (!actual.Equals(expected.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) &&
            !actual.Equals(expected, StringComparison.OrdinalIgnoreCase) && !actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Path is outside its approved ownership root.");
    }

    public void Dispose()
    {
        for (int index = directories.Count - 1; index >= 0; index--) directories[index].Dispose();
        directories.Clear();
        disk.Dispose(); // The attached volume and evidence survive; no destructive automatic finalization.
    }
}
