namespace BlackFlag.Engine.Writing;

/// <summary>Provides the immutable preceding PDF revision required by an incremental update.</summary>
public interface IPdfOriginalSourceRevision
{
    /// <summary>Gets the number of bytes in the complete preceding revision.</summary>
    long Length { get; }

    /// <summary>Gets the offset of the preceding revision's active cross-reference section.</summary>
    long CrossReferenceOffset { get; }

    /// <summary>Gets the cross-reference representation used by the preceding revision.</summary>
    PdfCrossReferenceWriteMode CrossReferenceMode { get; }

    /// <summary>Copies the complete preceding revision to the destination without closing it.</summary>
    void CopyTo(Stream destination);
}
