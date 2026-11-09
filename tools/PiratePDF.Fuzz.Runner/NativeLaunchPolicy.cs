using System.Globalization;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Closed argument/environment policy; no raw driver fragments or inherited environment.</summary>
internal static class NativeLaunchPolicy
{
    internal static void ValidatePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path.Length > 4096 || !Path.IsPathFullyQualified(path) || path.StartsWith(@"\\", StringComparison.Ordinal) ||
            path.Any(char.IsControl) || path.Contains('"') || path.AsSpan(2).Contains(':') ||
            path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part =>
                part is "." or ".." || part.EndsWith(' ') || part.EndsWith('.') || IsDeviceName(part)))
            throw new ArgumentException("An ordinary absolute local path is required.", nameof(path));
    }

    private static bool IsDeviceName(string component)
    {
        string name = component.Split('.')[0].ToUpperInvariant();
        return name is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" ||
            name.Length == 4 && (name.StartsWith("COM", StringComparison.Ordinal) || name.StartsWith("LPT", StringComparison.Ordinal)) &&
            "123456789\u00b9\u00b2\u00b3".Contains(name[3]);
    }

    internal static string[] Arguments(int runs, int seconds, int maximumLength, int seed, string corpus, string artifacts)
    {
        if (runs is < 1 or > 1000000 || seconds is < 1 or > 300 || maximumLength is < 1 or > 1048576 || seed < 0)
            throw new ArgumentOutOfRangeException(nameof(runs));
        ValidatePath(corpus);
        ValidatePath(artifacts);
        return ["-runs=" + runs.ToString(CultureInfo.InvariantCulture), "-max_total_time=" + seconds.ToString(CultureInfo.InvariantCulture),
            "-max_len=" + maximumLength.ToString(CultureInfo.InvariantCulture), "-seed=" + seed.ToString(CultureInfo.InvariantCulture),
            "-artifact_prefix=" + artifacts + Path.DirectorySeparatorChar, corpus];
    }

    internal static string[] EnvironmentBlock(string windows, string temporary)
    {
        ValidatePath(windows);
        ValidatePath(temporary);
        // Values are first-party constructed. No PATH, profiler, startup hook, credentials or caller extensions.
        return ["DOTNET_DbgEnableMiniDump=0", "DOTNET_EnableDiagnostics=0", "SystemRoot=" + windows,
            "TEMP=" + temporary, "TMP=" + temporary, "USERPROFILE=" + temporary, "windir=" + windows];
    }

    internal static string Quote(string argument)
    {
        if (argument.Contains('\0') || argument.Any(char.IsControl)) throw new ArgumentException("Invalid argument.", nameof(argument));
        // CreateProcess receives a command line, not argv. Escape backslashes only before quotes/end quotes.
        var text = new System.Text.StringBuilder("\"");
        int slashes = 0;
        foreach (char character in argument)
        {
            if (character == '\\') { slashes++; continue; }
            text.Append('\\', character == '"' ? checked(slashes * 2 + 1) : slashes);
            text.Append(character);
            slashes = 0;
        }
        text.Append('\\', checked(slashes * 2));
        return text.Append('"').ToString();
    }
}
