namespace BlackFlag.Engine.Writing;

/// <summary>Identifies how a writer should represent the cross-reference structure.</summary>
public enum PdfCrossReferenceWriteMode
{
    /// <summary>Preserves the source representation when source-revision metadata makes that possible.</summary>
    PreserveSource = 0,

    /// <summary>Uses a traditional cross-reference table.</summary>
    TraditionalTable = 1,

    /// <summary>Uses a PDF cross-reference stream.</summary>
    CrossReferenceStream = 2
}
