namespace BlackFlag.Engine.Filters;

/// <summary>
/// Creates standard BlackFlag PDF filter registrations.
/// </summary>
public static class PdfFilterRegistrations
{
    /// <summary>
    /// Creates the default registry available in BF-1040.
    /// </summary>
    /// <returns>
    /// A registry containing the production Flate decoder.
    /// </returns>
    public static PdfFilterRegistry CreateDefault()
    {
        return new PdfFilterRegistry(
            [
                new PdfFlateFilterDecoder()
            ]);
    }
}
