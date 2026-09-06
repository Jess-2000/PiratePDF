using BlackFlag.Engine.Fonts.Platform.MacOS;
using BlackFlag.Engine.Fonts.Platform.Windows;

namespace BlackFlag.Engine.Fonts.Platform;

/// <summary>
/// Creates the supported v1 platform provider set for the current operating
/// system.
/// </summary>
public static class PdfPlatformExternalFontProviders
{
    public static IReadOnlyList<IPdfExternalFontProvider> CreateDefault() =>
        OperatingSystem.IsWindows()
            ? [new PdfWindowsExternalFontProvider()]
            : OperatingSystem.IsMacOS()
                ? [new PdfMacOSExternalFontProvider()]
                : [];
}
