namespace BlackFlag.Engine.Parsing.ContentStreams;

/// <summary>Reports invalid or unsupported PDF content-stream syntax.</summary>
public sealed class PdfContentParseException : FormatException
{
    /// <summary>Initializes a content-stream parsing exception.</summary>
    public PdfContentParseException(
        PdfContentParseFailure failure,
        int streamIndex,
        long offset,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
        StreamIndex = streamIndex;
        Offset = offset;
    }

    /// <summary>Gets the failure category.</summary>
    public PdfContentParseFailure Failure { get; }

    /// <summary>Gets the zero-based stream index.</summary>
    public int StreamIndex { get; }

    /// <summary>Gets the decoded-byte offset.</summary>
    public long Offset { get; }
}
