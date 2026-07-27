using System.Text;

namespace PiratePDF.DocGen.Tests.TestInfrastructure;

internal sealed class TemporaryDirectory : IDisposable
{
    private static readonly UTF8Encoding Utf8WithoutBom =
        new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    private bool disposed;

    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "PiratePDF.DocGen.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string CreateFile(
        string relativePath,
        string content)
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            relativePath);

        ArgumentNullException.ThrowIfNull(content);

        var filePath = System.IO.Path.Combine(
            Path,
            relativePath);

        var directory = System.IO.Path.GetDirectoryName(
            filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            filePath,
            content,
            Utf8WithoutBom);

        return filePath;
    }

    public string ReadFile(
        string relativePath)
    {
        ObjectDisposedException.ThrowIf(
            disposed,
            this);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            relativePath);

        return File.ReadAllText(
            System.IO.Path.Combine(
                Path,
                relativePath),
            Utf8WithoutBom);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (Directory.Exists(Path))
        {
            Directory.Delete(
                Path,
                recursive: true);
        }
    }
}