namespace PiratePDF.Fuzz.Runner;

internal static class InputFile
{
    internal static async Task<ReadOnlyMemory<byte>> ReadAsync(string path, int maximumBytes, CancellationToken cancellationToken)
    {
        // Unix FileAttributes cannot distinguish a FIFO before an open that may block.
        // Fail closed until portable regular-file admission can be implemented and validated.
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        string fullPath = Path.GetFullPath(path);
        if (fullPath.StartsWith(@"\\", StringComparison.Ordinal) ||
            fullPath.AsSpan(2).Contains(':'))
        {
            throw new NotSupportedException();
        }

        // Only ordinary local paths. Reject links in every component without enumerating directories.
        string? current = fullPath;
        while (current is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string name = Path.GetFileName(current);
            int dot = name.IndexOf('.', StringComparison.Ordinal);
            if (dot >= 0) name = name[..dot];
            name = name.TrimEnd(' ');
            if (IsDeviceName(name))
            {
                throw new NotSupportedException();
            }

            FileAttributes attributes = File.GetAttributes(current);
            if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Device)) != 0 ||
                (current == fullPath && (attributes & FileAttributes.Directory) != 0))
            {
                throw new NotSupportedException();
            }

            current = Path.GetDirectoryName(current);
        }

        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 1, FileOptions.Asynchronous);
        if (!stream.CanSeek || (File.GetAttributes(stream.SafeFileHandle) &
            (FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.Device)) != 0)
        {
            throw new NotSupportedException();
        }

        if (stream.Length > maximumBytes)
        {
            throw new InputTooLargeException();
        }

        // The registered ceiling, never a file's advertised length, bounds the payload allocation.
        byte[] bytes = new byte[maximumBytes];
        int count = 0;
        while (count < bytes.Length)
        {
            int read = await stream.ReadAsync(bytes.AsMemory(count), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            count += read;
        }

        // Probe beyond the ceiling even if the length changed after admission; never grow the buffer.
        byte[] probe = new byte[1];
        if (await stream.ReadAsync(probe, cancellationToken).ConfigureAwait(false) != 0)
        {
            throw new InputTooLargeException();
        }

        cancellationToken.ThrowIfCancellationRequested();
        return bytes.AsMemory(0, count);
    }

    private static bool IsDeviceName(string name) =>
        name.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("CONIN$", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("CONOUT$", StringComparison.OrdinalIgnoreCase) ||
        (name.Length == 4 &&
            (name.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) &&
            "123456789\u00b9\u00b2\u00b3".Contains(name[3]));
}

internal sealed class InputTooLargeException : IOException;
