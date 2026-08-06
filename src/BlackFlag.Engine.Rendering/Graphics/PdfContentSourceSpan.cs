namespace BlackFlag.Engine.Rendering.Graphics;

/// <summary>Identifies a byte range within one decoded PDF content stream.</summary>
public readonly record struct PdfContentSourceSpan
{
    /// <summary>Initializes a content-stream source span.</summary>
    public PdfContentSourceSpan(int streamIndex, long startOffset, long endOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(streamIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(startOffset);
        ArgumentOutOfRangeException.ThrowIfLessThan(endOffset, startOffset);

        StreamIndex = streamIndex;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    /// <summary>Gets the zero-based content-stream sequence index.</summary>
    public int StreamIndex { get; }

    /// <summary>Gets the inclusive decoded-byte offset.</summary>
    public long StartOffset { get; }

    /// <summary>Gets the exclusive decoded-byte offset.</summary>
    public long EndOffset { get; }

    /// <summary>Gets the number of bytes covered by this span.</summary>
    public long Length => EndOffset - StartOffset;
}
