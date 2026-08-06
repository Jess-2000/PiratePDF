namespace BlackFlag.Engine.Filters;

/// <summary>
/// Creates standard BlackFlag PDF filter registrations.
/// </summary>
public static class PdfFilterRegistrations
{
    /// <summary>
    /// Creates the default registry available in BF-1041.
    /// </summary>
    /// <returns>
    /// A registry containing the production ASCIIHex, ASCII85, RunLength,
    /// LZW, and Flate decoders.
    /// </returns>
    public static PdfFilterRegistry CreateDefault()
    {
        return new PdfFilterRegistry(
            [
                new PdfAsciiHexFilterDecoder(),
                new PdfAscii85FilterDecoder(),
                new PdfRunLengthFilterDecoder(),
                new PdfLzwFilterDecoder(),
                new PdfFlateFilterDecoder()
            ]);
    }
}
