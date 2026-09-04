namespace BlackFlag.Engine.Fonts;

/// <summary>Pairs a matched face with the provider that can open its program.</summary>
public sealed class PdfExternalFontMatch
{
    internal PdfExternalFontMatch(
        PdfExternalFontFace face,
        IPdfExternalFontProvider provider)
    {
        Face = face;
        Provider = provider;
    }

    public PdfExternalFontFace Face { get; }

    internal IPdfExternalFontProvider Provider { get; }

    /// <summary>Opens the matched external font program.</summary>
    public Stream OpenFontProgram() =>
        Provider.OpenFontProgram(Face);
}
