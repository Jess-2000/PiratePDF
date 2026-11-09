using System.Security.Cryptography;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Retains read-only file and ancestor leases. ACL/environment verification remains a separate requirement.</summary>
internal sealed class TrustedNativeImage : IDisposable
{
    private readonly List<IDisposable> leases = [];
    internal string Path { get; }
    internal string Hash { get; }
    internal string Identity { get; }

    internal TrustedNativeImage(string path, string expectedHash, long maximumBytes)
    {
        NativeLaunchPolicy.ValidatePath(path);
        if (expectedHash.Length != 64 || !expectedHash.All(Uri.IsHexDigit) || maximumBytes is < 1 or > 268435456)
            throw new ArgumentException("A trusted SHA256 identity and bounded image size are required.");
        Path = System.IO.Path.GetFullPath(path);
        try
        {
            string? ancestor = System.IO.Path.GetDirectoryName(Path);
            while (ancestor is not null)
            {
                if ((File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected image ancestor.");
                leases.Add(WindowsNativeMethods.OpenDirectory(ancestor));
                ancestor = System.IO.Path.GetDirectoryName(ancestor);
            }
            if ((File.GetAttributes(Path) & (FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.Device)) != 0)
                throw new IOException("Image is not an ordinary file.");
            var handle = WindowsNativeMethods.OpenImage(Path);
            leases.Add(handle);
            Identity = WindowsNativeMethods.FileIdentity(handle);
            var stream = new FileStream(handle, FileAccess.Read);
            leases.Add(stream);
            if (stream.Length > maximumBytes) throw new IOException("Image exceeds admission work budget.");
            Hash = Convert.ToHexString(SHA256.HashData(stream));
            if (!Hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase)) throw new IOException("Executable identity mismatch.");
        }
        catch { Dispose(); throw; }
    }

    public void Dispose()
    {
        for (int index = leases.Count - 1; index >= 0; index--) leases[index].Dispose();
        leases.Clear();
    }
}
