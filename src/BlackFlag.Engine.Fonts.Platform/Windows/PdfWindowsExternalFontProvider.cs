using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Platform.Windows;

/// <summary>
/// Discovers standalone TTF/OTF faces installed in standard Windows font
/// directories.
/// </summary>
public sealed class PdfWindowsExternalFontProvider :
    PdfFileSystemExternalFontProvider
{
    public const string ProviderId =
        "windows-system-fonts";

    public PdfWindowsExternalFontProvider()
        : base(
            ProviderId,
            GetDefaultDirectories())
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "The Windows external-font provider can run only on Windows.");
        }
    }

    public PdfWindowsExternalFontProvider(
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
        string systemFonts =
            Environment.GetFolderPath(
                Environment.SpecialFolder.Fonts);

        if (!string.IsNullOrWhiteSpace(systemFonts))
        {
            yield return systemFonts;
        }

        string local =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        if (!string.IsNullOrWhiteSpace(local))
        {
            yield return Path.Combine(
                local,
                "Microsoft",
                "Windows",
                "Fonts");
        }
    }
}
