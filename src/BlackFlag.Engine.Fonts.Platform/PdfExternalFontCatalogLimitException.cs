namespace BlackFlag.Engine.Fonts.Platform;

/// <summary>Reports aggregate discovery exhaustion; no partial catalog is published.</summary>
public sealed class PdfExternalFontCatalogLimitException : InvalidOperationException
{
    public PdfExternalFontCatalogLimitException(string limitName, long limit)
        : base($"Font catalog exceeded {limitName} ({limit}). Reduce the search scope or explicitly configure a larger finite limit.")
    {
        LimitName = limitName;
        Limit = limit;
    }

    public string LimitName { get; }

    public long Limit { get; }
}
