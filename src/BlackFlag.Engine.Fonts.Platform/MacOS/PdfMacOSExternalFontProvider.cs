using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Platform.MacOS;

/// <summary>
/// Discovers standalone TTF/OTF faces installed in standard macOS font
/// directories.
/// </summary>
public sealed class PdfMacOSExternalFontProvider :
    PdfFileSystemExternalFontProvider
{
    public const string ProviderId =
        "macos-system-fonts";

    public PdfMacOSExternalFontProvider()
        : base(
            ProviderId,
            GetDefaultDirectories())
    {
        if (!OperatingSystem.IsMacOS())
        {
            throw new PlatformNotSupportedException(
                "The macOS external-font provider can run only on macOS.");
        }
    }

    public PdfMacOSExternalFontProvider(
        IEnumerable<string> searchDirectories,
        IPdfExternalFontSanitizer? sanitizer = null)
        : base(
            ProviderId,
            searchDirectories,
            sanitizer)
    {
        ArgumentNullException.ThrowIfNull(searchDirectories);
    }

    private static IEnumerable<string> GetDefaultDirectories()
    {
        yield return "/System/Library/Fonts";
        yield return "/Library/Fonts";

        string home =
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);

        if (!string.IsNullOrWhiteSpace(home))
        {
            yield return Path.Combine(
                home,
                "Library",
                "Fonts");
        }
    }
}
