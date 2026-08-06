namespace BlackFlag.Engine.Parsing.Documents;

/// <summary>Reports malformed or unsafe PDF page-tree structures.</summary>
public sealed class PdfPageTreeLoadException : IOException
{
    public PdfPageTreeLoadException(string message)
        : base(message)
    {
    }

    public PdfPageTreeLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
