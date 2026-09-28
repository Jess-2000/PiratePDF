namespace PiratePDF.Fuzz.Runner;

/// <summary>Argument transport for the pinned Windows libfuzzer-dotnet launcher, not a shell command builder.</summary>
public static class WindowsLibFuzzerArguments
{
    public static string[] Create(string dotnetHost, string assembly, string target)
    {
        ValidatePath(dotnetHost, ".exe");
        ValidatePath(assembly, ".dll");
        if (!Path.GetFileName(dotnetHost).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The installed dotnet host is required.", nameof(dotnetHost));
        if (target is not ("filter-asciihex" or "probe-abnormal" or "probe-harness" or "probe-timeout"))
            throw new ArgumentException("Unknown smoke target or probe.", nameof(target));

        // v2025.05.02.0904 takes only the first target_arg and concatenates it verbatim
        // after target_path for CreateProcess. Preserve inner quotes through ArgumentList.
        // These are validated file paths: no quotes or terminal backslash can require escaping.
        return ["--target_path=\"" + dotnetHost + "\"", "--target_arg=\"" + assembly + "\" " + target];
    }

    private static void ValidatePath(string path, string extension)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (path.Length > 4096 || !Path.IsPathFullyQualified(path) || path.StartsWith(@"\\", StringComparison.Ordinal)
            || path.AsSpan(2).Contains(':') || path.IndexOfAny(Path.GetInvalidPathChars()) >= 0
            || path.Contains('"') || path.Contains(';') || path.Any(char.IsControl)
            || !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Expected an ordinary absolute Windows file path.", nameof(path));
    }
}
